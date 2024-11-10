using System;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.IO;
using Newtonsoft.Json;

public enum State
{
    FollowerCentralized,
    HorizontalSway,
    VerticalSway,
    Stable,
    AdjustToPlayerView
}

public class ClientLogic : MonoBehaviour
{
    public Connection connection;
    private bool isWebSocketConnected = false;

    public RawImage colorImage;
    public RawImage depthImage;
    public Camera playerCamera;
    public GameObject UICanvas;
    public GameObject anchorPrefab;

    public GameObject NotiffBlock;
    public TMP_Text dangerLevel;
    public TMP_Text dangerSource;

    private GameObject uiCanvasInstance;

    private GUIMovementStateMachine gui_sm;

    private byte[] colorImageBytes;
    private byte[] depthImageBytes;

    private float timeSinceLastSend = 0f;
    private float sendInterval = 1.0f;

    private Vector3[] UIScreenCorners = new Vector3[4];
    [SerializeField] private bool flipColors = false;
    public List<Vector3> mainHitPositions = new List<Vector3>();

    public Dictionary<string, GameObject> anchors = new Dictionary<string, GameObject>();

    // Variables for smooth UI movement
    public float uiFollowSpeed = 5f; // Adjust this value to control follow speed

    // Variables for moving the UI out of the way
    private float uiVerticalOffset = 0f;
    private float uiVerticalOffsetVelocity = 0f;
    public float uiMoveAmount = 1.0f; // Distance to move the UI when obstructed
    public float uiMoveDuration = 0.5f; // Time to move the UI when obstructed

    // Use HashSet to track obstructing anchors
    public HashSet<string> mainObstructingAnchors = new HashSet<string>();
    public HashSet<string> sideObstructingAnchors = new HashSet<string>();

    // Property to get the obstruction count
    public int mainUiObstructedCount
    {
        get { return mainObstructingAnchors.Count; }
    }

    public int sideUiObstructedCount
    {
        get { return sideObstructingAnchors.Count; }
    }

    // Variables to manage offset direction and obstruction timing
    private Vector2 lastOffsetDirection = Vector2.zero;
    private float timeSinceLastObstruction = 0f;
    private float obstructionGracePeriod = 0.1f; // Adjust as needed to smooth out jitter

    public GameObject debugPrefab;

    public GameObject redDot;
    private int first = 0;

    void Start()
    {
        gui_sm = new GUIMovementStateMachine();
        StartWebSocket();
        SpawnUI();
        connection.OnServerMessage += HandleServerMessage;
    }

    void Update()
    {
        timeSinceLastSend += Time.deltaTime;

        if (timeSinceLastSend >= sendInterval && colorImage != null && depthImage != null && isWebSocketConnected)
        {
            timeSinceLastSend = 0f;

            // Get UIScreenCorners
            Vector3[] UIWorldCorners = new Vector3[4];
            uiCanvasInstance.transform.GetChild(1).GetChild(0).gameObject.GetComponent<RectTransform>().GetWorldCorners(UIWorldCorners);
            for (int i = 0; i < UIWorldCorners.Length; i++)
            {
                Vector3 UIscreenCorner = playerCamera.WorldToScreenPoint(UIWorldCorners[i]);
                UIscreenCorner.x /= Screen.width;
                UIscreenCorner.y /= Screen.height;
                UIScreenCorners[i] = UIscreenCorner;
            }

            Texture2D colorTexture = ConvertToTexture2D(colorImage.texture);

            if (colorTexture != null)
            {
                colorImageBytes = colorTexture.EncodeToJPG();
            }

            SendDataAsync();
        }
    }

    void LateUpdate()
    {
        UpdateUIPosition();
        UpdateUIRotation();
    }

    private void UpdateUIPosition()
    {
        if (uiCanvasInstance != null && playerCamera != null)
        {
            gui_sm.AnalyzeAndMoveGUI(uiCanvasInstance, playerCamera.transform, mainObstructingAnchors, sideObstructingAnchors, anchors);
        }
    }

    private void UpdateUIRotation()
    {
        if (uiCanvasInstance != null && playerCamera != null)
        {
            // Compute the direction to the player, ignoring Y-axis differences
            Vector3 directionToPlayer = playerCamera.transform.position - uiCanvasInstance.transform.position;
            directionToPlayer.y = 0f; // Ignore vertical component to rotate only around Y-axis

            // Check to avoid zero-length direction vector
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                // Compute the desired rotation only around Y-axis
                Quaternion desiredRotation = Quaternion.LookRotation(directionToPlayer);

                // Keep the original X and Z rotations
                Vector3 currentEulerAngles = uiCanvasInstance.transform.eulerAngles;
                float desiredYRotation = desiredRotation.eulerAngles.y;
                Vector3 newEulerAngles = new Vector3(currentEulerAngles.x, desiredYRotation, currentEulerAngles.z);

                // Smoothly rotate to the desired rotation
                Quaternion targetRotation = Quaternion.Euler(newEulerAngles);
                uiCanvasInstance.transform.rotation = Quaternion.Lerp(
                    uiCanvasInstance.transform.rotation,
                    targetRotation,
                    Time.deltaTime * uiFollowSpeed
                );
            }
        }
    }

    private void HandleServerMessage(string message)
    {
        FrameDataMessage frameData = JsonUtility.FromJson<FrameDataMessage>(message);
        if (frameData == null || frameData.type != "frame_data")
        {
            Debug.LogWarning("Invalid message received from server.");
            HandeServerMessageDangerDetection(message);
            return;
        }

        // Handle GUI colors
        if (frameData.gui_colors != null)
        {
            Color targetBackgroundColor = new Color(
                frameData.gui_colors.background_color.r / 255f,
                frameData.gui_colors.background_color.g / 255f,
                frameData.gui_colors.background_color.b / 255f
            );
            Color targetTextColor = new Color(
                frameData.gui_colors.text_color.r / 255f,
                frameData.gui_colors.text_color.g / 255f,
                frameData.gui_colors.text_color.b / 255f
            );

            setColors colorSetter = uiCanvasInstance.GetComponent<setColors>();
            if (colorSetter != null)
            {
                StartCoroutine(LerpColors(colorSetter, targetBackgroundColor, targetTextColor, 0.5f));
            }
            else
            {
                Debug.LogWarning("setColors component not found on uiCanvasInstance");
            }
        }

        // Handle object positions
        if (frameData.objects != null && frameData.objects.Count > 0)
        {
            foreach (ObjectData objData in frameData.objects)
            {
                SpawnAnchor(objData);
            }
        }
    }

    private void HandeServerMessageDangerDetection(string message)
    {
        Debug.LogWarning("Received from server: " + message);

        DangerDataMessage dangerData = JsonUtility.FromJson<DangerDataMessage>(message);
        if (dangerData == null)
        {
            Debug.LogWarning("Invalid danger analysis data received from server");
            return;
        }
        if (dangerData.danger_level != "LOW DANGER")
        {
            NotiffBlock.SetActive(true);
        }
        dangerLevel.text = dangerData.danger_level;
        dangerSource.text = dangerData.danger_source;
    }

    private IEnumerator LerpColors(setColors colorSetter, Color targetBackgroundColor, Color targetTextColor, float duration)
    {
        Color startBackgroundColor = colorSetter.Background.color;
        Color startTextColor = colorSetter.textObjects[0].color;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // Lerp the colors
            colorSetter.SetColor(
                Color.Lerp(startBackgroundColor, targetBackgroundColor, t),
                Color.Lerp(startTextColor, targetTextColor, t)
            );

            yield return null; // Wait for the next frame
        }

        // Ensure the final colors are set
        colorSetter.SetColor(targetBackgroundColor, targetTextColor);
    }

    private void SpawnAnchor(ObjectData objData)
    {
        Vector3 worldPosition = new Vector3(objData.x, objData.y, objData.z);
        Vector3 localScale = new Vector3(objData.width, objData.height, objData.width);
        string id = objData.id;

        // Check if anchor already exists --> set position
        if (anchors.ContainsKey(id))
        {
            GameObject anchor = anchors[id];
            anchor.transform.position = worldPosition;

            // Make the anchor face the player horizontally
            Vector3 targetPosition = new Vector3(playerCamera.transform.position.x,
                                                anchor.transform.position.y,
                                                playerCamera.transform.position.z);

            anchor.transform.LookAt(targetPosition);
            anchor.transform.localScale = localScale;
            return;
        }

        GameObject newAnchor = Instantiate(anchorPrefab, worldPosition, Quaternion.identity);
        newAnchor.transform.localScale = localScale;
        Anchor anchorScript = newAnchor.GetComponent<Anchor>();
        anchorScript.id = id;
        anchorScript.client = this;
        anchorScript.playerTransform = playerCamera.transform; // Set playerTransform
        anchors.Add(id, newAnchor);
        newAnchor.layer = LayerMask.NameToLayer("Default"); // Adjust layer as needed

        if (anchorScript != null)
        {
            anchorScript.playerTransform = playerCamera.transform; // Updated to use playerCamera
        }
        else
        {
            Debug.LogWarning("Anchor component not found on the instantiated prefab.");
        }
    }

    public void DeleteAnchor(string id)
    {
        if (anchors.ContainsKey(id))
        {
            Destroy(anchors[id]);
            anchors.Remove(id);
        }
    }

    private void SendDataAsync()
    {
        if (colorImageBytes != null && colorImage.texture is Texture2D colorTex)
        {
            // Capture Unity data and convert to serializable forms
            var pos = new SerializableVector3(playerCamera.transform.position);
            var invMat = new SerializableMatrix4x4((playerCamera.projectionMatrix * playerCamera.worldToCameraMatrix).inverse);

            SerializableVector3[] uiScreenCornersCopy = new SerializableVector3[UIScreenCorners.Length];
            for (int i = 0; i < UIScreenCorners.Length; i++)
            {
                uiScreenCornersCopy[i] = new SerializableVector3(UIScreenCorners[i]);
            }
            bool flipColorsCopy = flipColors;

            // Prepare data for background thread
            var unityData = new UnityDataForBackground
            {
                pos = pos,
                invMat = invMat,
                uiScreenCorners = uiScreenCornersCopy,
                flipColors = flipColorsCopy,
            };

            // Start a task to perform heavy computations and send data
            Task.Run(async () =>
            {
                try
                {
                    await SendImageDataAsync(unityData, colorImageBytes);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error in background task: " + ex);
                }
            });
        }
    }

    private class UnityDataForBackground
    {
        public SerializableVector3 pos;
        public SerializableMatrix4x4 invMat;
        public SerializableVector3[] uiScreenCorners;
        public bool flipColors;
    }

    private async Task SendImageDataAsync(UnityDataForBackground unityData, byte[] imageBytes)
    {
        // Base64 encode the image bytes
        string base64ImageData = System.Convert.ToBase64String(imageBytes);

        // Create data object
        ImageDataMessage dataObject = new ImageDataMessage
        {
            type = "color",
            data = new ObjectData { x = unityData.pos.x, y = unityData.pos.y, z = unityData.pos.z, id = "Null", height = 0, width = 0 },
            invMat = unityData.invMat,
            imageData = base64ImageData,
            UIScreenCorners = unityData.uiScreenCorners,
            flipColors = unityData.flipColors,
        };

        // Serialize to JSON using a thread-safe serializer
        string jsonString = JsonConvert.SerializeObject(dataObject);

        // Send the data asynchronously
        await connection.SendTextAsync(jsonString);
    }

    private void SpawnUI()
    {
        if (playerCamera != null)
        {
            // Initial position in front of the player
            float distanceFromPlayer = 2.0f; // Adjust this value as needed
            Vector3 forwardDirection = playerCamera.transform.forward;
            Vector3 initialPosition = playerCamera.transform.position + forwardDirection * distanceFromPlayer;

            uiCanvasInstance = Instantiate(UICanvas, initialPosition, Quaternion.identity);

            // Make the UI face the player
            uiCanvasInstance.transform.rotation = Quaternion.LookRotation(uiCanvasInstance.transform.position - playerCamera.transform.position);

            SetLayerRecursively(uiCanvasInstance, LayerMask.NameToLayer("UI"));
        }
        else
        {
            Debug.LogError("Player Camera is not assigned.");
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void StartWebSocket()
    {
        connection.StartConnection();
        isWebSocketConnected = true;
    }

    private Texture2D ConvertToTexture2D(Texture texture)
    {
        if (texture is Texture2D tex2D)
        {
            return tex2D;
        }
        else if (texture is RenderTexture renderTex)
        {
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = renderTex;

            Texture2D newTexture = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGBA32, false);
            newTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            newTexture.Apply();

            RenderTexture.active = currentRT;
            return newTexture;
        }
        return null;
    }

    public void FlipColors()
    {
        flipColors = !flipColors;
    }

    // Serializable classes for JSON deserialization and serialization

    [System.Serializable]
    public class FrameDataMessage
    {
        public string type;
        public GuiColorsData gui_colors;
        public List<ObjectData> objects;
    }

    [System.Serializable]
    public class DangerDataMessage
    {
        public string type;
        public string danger_level;
        public string danger_source;
    }

    [System.Serializable]
    public class GuiColorsData
    {
        public ColorData background_color;
        public ColorData text_color;
    }

    [System.Serializable]
    public class ColorData
    {
        public int r;
        public int g;
        public int b;
    }

    [System.Serializable]
    public class ObjectData
    {
        public float x;
        public float y;
        public float z;
        public string id;
        public float width;
        public float height;
    }

    [System.Serializable]
    public class ImageDataMessage
    {
        public string type;
        public ObjectData data;
        public SerializableMatrix4x4 invMat;
        public string imageData;
        public float fx;
        public float fy;
        public float cx;
        public float cy;
        public SerializableVector3[] UIScreenCorners;
        public bool flipColors;
    }

    [System.Serializable]
    public class SerializableMatrix4x4
    {
        public float[] elements; // 16 elements

        public SerializableMatrix4x4(Matrix4x4 matrix)
        {
            elements = new float[16];
            elements[0] = matrix.m00; elements[1] = matrix.m01; elements[2] = matrix.m02; elements[3] = matrix.m03;
            elements[4] = matrix.m10; elements[5] = matrix.m11; elements[6] = matrix.m12; elements[7] = matrix.m13;
            elements[8] = matrix.m20; elements[9] = matrix.m21; elements[10] = matrix.m22; elements[11] = matrix.m23;
            elements[12] = matrix.m30; elements[13] = matrix.m31; elements[14] = matrix.m32; elements[15] = matrix.m33;
        }
    }

    [System.Serializable]
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
    }
}
