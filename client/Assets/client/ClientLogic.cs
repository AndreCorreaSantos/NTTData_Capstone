using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMPro;

public class ClientLogic : MonoBehaviour
{
    public Connection connection;
    private bool isWebSocketConnected = false;

    public RawImage colorImage;
    public RawImage depthImage;
    public Camera playerCamera; // Updated to include playerCamera
    public GameObject UICanvas;
    public GameObject anchorPrefab;

    public GameObject NotiffBlock;
    public TMP_Text dangerLevel;
    public TMP_Text dangerSource;

    public GameObject debugPrefab;

    private GameObject uiCanvasInstance;

    private byte[] colorImageBytes;
    private byte[] depthImageBytes;

    private float timeSinceLastSend = 0f;
    private float sendInterval = 0.5f;

    private List<GameObject> anchors = new List<GameObject>();

    private GameObject debug;

    void Start()
    {
        StartWebSocket();
        SpawnUI();
        debug = Instantiate(debugPrefab, Vector3.zero, Quaternion.identity);
        connection.OnServerMessage += HandleServerMessage;
    }

    void Update()
    {
        timeSinceLastSend += Time.deltaTime;

        if (timeSinceLastSend >= sendInterval && colorImage != null && depthImage != null && isWebSocketConnected)
        {
            timeSinceLastSend = 0f;

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

        anchors.RemoveAll(anchor => anchor == null);
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
        //Debug.Log("Received from server: " + message);

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
            Color backgroundColor = new Color(
                frameData.gui_colors.background_color.r / 255f,
                frameData.gui_colors.background_color.g / 255f,
                frameData.gui_colors.background_color.b / 255f
            );
            Color textColor = new Color(
                frameData.gui_colors.text_color.r / 255f,
                frameData.gui_colors.text_color.g / 255f,
                frameData.gui_colors.text_color.b / 255f
            );

            setColors colorSetter = uiCanvasInstance.GetComponent<setColors>();
            if (colorSetter != null)
            {
                colorSetter.SetColor(backgroundColor, textColor);
            }
            else
            {
                Debug.LogWarning("setColors component not found on uiCanvasInstance");
            }
        }

        // Handle object positions
        if (frameData.object_positions != null && frameData.object_positions.Count > 0)
        {
            foreach (PositionData positionData in frameData.object_positions)
            {
                if (positionData != null)
                {
                    Vector3 objectPosition = new Vector3(
                        positionData.x,
                        positionData.y,
                        positionData.z
                    );

                    // Ignore positions at the origin
                    if (objectPosition != Vector3.zero)
                    {
                        debug.transform.position = objectPosition;
                        Debug.Log("ObjectPosition: " + objectPosition);
                        SpawnAnchor(objectPosition);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("No object positions received.");
        }
    }
    private void SpawnAnchor(Vector3 position)
    {
        // if (anchorPrefab != null)
        // {
        //     bool anchorNearby = false;

        //     foreach (GameObject anchor in anchors)
        //     {
        //         if (anchor != null)
        //         {
        //             float distance = Vector3.Distance(position, anchor.transform.position);
        //             if (distance <= 0.1f)
        //             {
        //                 anchorNearby = true;
        //                 break;
        //             }
        //         }
        //     }

            // if (!anchorNearby)
            // {
        Debug.Log("spawning anchor");
        Debug.Log("Position"+position);

        Vector3 worldPosition = playerCamera.ViewportToWorldPoint(position);
        GameObject newAnchor = Instantiate(anchorPrefab, worldPosition, Quaternion.identity);
        newAnchor.layer = 30;

        Anchor anchorScript = newAnchor.GetComponent<Anchor>();
        if (anchorScript != null)
        {
            anchorScript.playerTransform = playerCamera.transform; // Updated to use playerCamera
        }
        else
        {
            Debug.LogWarning("Anchor component not found on the instantiated prefab.");
        }

        // anchors.Add(newAnchor);
        //     }
        //     else
        //     {
        //         Debug.Log("An anchor already exists within 1.0 units. Not spawning a new one.");
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning("anchorPrefab is not assigned in the Inspector.");
        // }
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
;
    

        ImageDataMessage dataObject = new ImageDataMessage
        {
            type = imageType,
            position = new PositionData { x = pos.x, y = pos.y, z = pos.z },
            rotation = new RotationData { x = rot.x, y = rot.y, z = rot.z, w = rot.w },
            imageData = System.Convert.ToBase64String(imageBytes),
            fx = fx,
            fy = fy,
            cx = cx,
            cy = cy
        };

        string jsonString = JsonUtility.ToJson(dataObject);
        await connection.SendTextAsync(jsonString);
    }

    private void SpawnUI()
    {
        Vector3 pos = new Vector3(-0.75999999f,0.569999993f,0.460000008f);
        Vector3 rot = new Vector3(0.0f, 180.0f, 0.0f);
        uiCanvasInstance = Instantiate(UICanvas, pos, Quaternion.Euler(rot));
        // uiCanvasInstance.transform.LookAt(playerCamera.transform);

        SetLayerRecursively(uiCanvasInstance, 30);
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
}

// Serializable classes for JSON deserialization

[System.Serializable]
public class FrameDataMessage
{
    public string type;
    public GuiColorsData gui_colors;
    public List<PositionData> object_positions; // Updated to List
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
public class PositionData
{
    public float x;
    public float y;
    public float z;
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
    public PositionData position;
    public RotationData rotation;
    public string imageData;
    
    // New fields for camera intrinsics
    public float fx;
    public float fy;
    public float cx;
    public float cy;
}
