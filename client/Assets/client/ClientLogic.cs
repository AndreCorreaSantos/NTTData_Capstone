using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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

    private byte[] colorImageBytes;
    private byte[] depthImageBytes;

    private float timeSinceLastSend = 0f;
    private float sendInterval = 0.5f;

    private Vector3[] UIScreenCorners = new Vector3[4];

    public Dictionary<string, GameObject> anchors = new Dictionary<string, GameObject>();

    // Variables for smooth UI movement
    public float uiFollowSpeed = 5f; // Adjust this value to control follow speed

    // Variables for moving the UI out of the way
    private float uiVerticalOffset = 0f;
    private float uiVerticalOffsetVelocity = 0f;
    public float uiMoveAmount = 1.0f; // Distance to move the UI when obstructed
    public float uiMoveDuration = 0.5f; // Time to move the UI when obstructed
    private int uiObstructedCount = 0;

    void Start()
    {
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

            Vector3[] UIWorldCorners = new Vector3[4];
            uiCanvasInstance.transform.GetChild(1).GetChild(0).gameObject.GetComponent<RectTransform>().GetWorldCorners(UIWorldCorners);
            for (int i = 0; i < UIWorldCorners.Length; i++)
            {
                Vector3 UIscreenCorner = playerCamera.WorldToScreenPoint(UIWorldCorners[i]);
                UIscreenCorner.x /= Screen.width;
                UIscreenCorner.y /= Screen.height;
                UIScreenCorners[i] = UIscreenCorner;
                Debug.Log($"Screen Corner {i}: {UIscreenCorner}");
            }

            Texture2D colorTexture = ConvertToTexture2D(colorImage.texture);
            Texture2D depthTexture = ConvertToTexture2D(depthImage.texture);

            if (colorTexture != null)
            {
                colorImageBytes = colorTexture.EncodeToJPG();
            }

            if (depthTexture != null)
            {
                depthImageBytes = depthTexture.EncodeToJPG();
            }

            SendDataAsync();
        }

        UpdateUIPosition();
        UpdateUIRotation();
    }

    private void UpdateUIPosition()
    {
        if (uiCanvasInstance != null && playerCamera != null)
        {
            // Desired position in front of the player
            float distanceFromPlayer = 2.0f; // Adjust this value as needed
            Vector3 forwardDirection = playerCamera.transform.forward;
            Vector3 desiredPosition = playerCamera.transform.position + forwardDirection * distanceFromPlayer;

            // Adjust vertical position using SmoothDamp
            float targetVerticalOffset = uiObstructedCount > 0 ? uiMoveAmount : 0f;
            float smoothTime = uiMoveDuration;

            uiVerticalOffset = Mathf.SmoothDamp(uiVerticalOffset, targetVerticalOffset, ref uiVerticalOffsetVelocity, smoothTime);

            // Apply vertical offset
            desiredPosition.y += uiVerticalOffset;

            // Smoothly move the UI towards the desired position
            float positionLerpSpeed = uiFollowSpeed * Time.deltaTime;
            uiCanvasInstance.transform.position = Vector3.Lerp(
                uiCanvasInstance.transform.position,
                desiredPosition,
                positionLerpSpeed
            );
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
            foreach (ObjectData ObjectData in frameData.objects)
            {
                if (ObjectData != null)
                {
                    Vector3 objectPosition = new Vector3(
                        ObjectData.x,
                        ObjectData.y,
                        ObjectData.z
                    );

                    string id = ObjectData.id;
                    SpawnAnchor(objectPosition, id);
                }
                else
                {
                    Debug.LogWarning("No object positions received.");
                }
            }
        }
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

    private void SpawnAnchor(Vector3 position, string id)
    {
        Vector3 worldPosition = position;

        // Check if anchor already exists --> set position
        if (anchors.ContainsKey(id))
        {
            Debug.Log("Anchor already exists");
            GameObject anchor = anchors[id];
            anchor.transform.position = worldPosition;

            // Make the anchor face the player horizontally
            Vector3 targetPosition = new Vector3(playerCamera.transform.position.x,
                                                anchor.transform.position.y,
                                                playerCamera.transform.position.z);

            anchor.transform.LookAt(targetPosition);
            return;
        }

        GameObject newAnchor = Instantiate(anchorPrefab, worldPosition, Quaternion.identity);
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

    private async void SendDataAsync()
    {
        if (colorImageBytes != null && colorImage.texture is Texture2D colorTex)
        {
            await SendImageDataAsync("color", colorImageBytes, colorTex.width, colorTex.height);
        }

        if (depthImageBytes != null && depthImage.texture is Texture2D depthTex)
        {
            await SendImageDataAsync("depth", depthImageBytes, depthTex.width, depthTex.height);
        }
    }

    private async Task SendImageDataAsync(string imageType, byte[] imageBytes, int imageWidth, int imageHeight)
    {
        Vector3 pos = playerCamera.transform.position;
        Quaternion rot = playerCamera.transform.rotation;

        // Calculate camera intrinsics
        float verticalFOV = playerCamera.fieldOfView; // in degrees
        float aspectRatio = playerCamera.aspect; // width / height

        // Convert FOV from degrees to radians
        float verticalFOVRad = verticalFOV * Mathf.Deg2Rad;

        // Compute focal lengths
        float fy = (imageHeight / 2f) / Mathf.Tan(verticalFOVRad / 2f);
        float fx = fy * aspectRatio;

        // Principal points (assuming center of the image)
        float cx = imageWidth / 2f;
        float cy = imageHeight / 2f;

        ImageDataMessage dataObject = new ImageDataMessage
        {
            type = imageType,
            data = new ObjectData { x = pos.x, y = pos.y, z = pos.z, id = "Null" },
            rotation = new RotationData { x = rot.x, y = rot.y, z = rot.z, w = rot.w },
            imageData = System.Convert.ToBase64String(imageBytes),
            fx = fx,
            fy = fy,
            cx = cx,
            cy = cy,
            UIScreenCorners = UIScreenCorners
        };

        string jsonString = JsonUtility.ToJson(dataObject);
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

    // Methods to move the UI out of the way when obstructed by anchor raycasts

    public void MoveUIOutOfWay()
    {
        uiObstructedCount++;
    }

    public void ReturnUIToOriginalPosition()
    {
        uiObstructedCount = Mathf.Max(0, uiObstructedCount - 1);
    }

    // Serializable classes for JSON deserialization

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
    }

    [System.Serializable]
    public class RotationData
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [System.Serializable]
    public class ImageDataMessage
    {
        public string type;
        public ObjectData data;
        public RotationData rotation;
        public string imageData;

        public float fx;
        public float fy;
        public float cx;
        public float cy;
        public Vector3[] UIScreenCorners;
    }
}
