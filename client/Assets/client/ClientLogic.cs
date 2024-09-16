using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ClientLogic : MonoBehaviour
{
    public Connection connection;
    private bool isWebSocketConnected = false;

    public RawImage colorImage;
    public RawImage depthImage;
    public Transform playerTransform;
    public GameObject UICanvas;
    public GameObject anchorPrefab;

    private GameObject uiCanvasInstance;

    private byte[] colorImageBytes;
    private byte[] depthImageBytes;

    private float timeSinceLastSend = 0f;
    private float sendInterval = 0.5f;

    private List<GameObject> anchors = new List<GameObject>();

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

    private void HandleServerMessage(string message)
    {
        Debug.Log("Received from server: " + message);

        FrameDataMessage frameData = JsonUtility.FromJson<FrameDataMessage>(message);
        if (frameData == null || frameData.type != "frame_data")
        {
            Debug.LogWarning("Invalid message received from server.");
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

        // Handle object position
        if (frameData.object_position != null)
        {
            Vector3 objectPosition = new Vector3(
                frameData.object_position.x,
                frameData.object_position.y,
                frameData.object_position.z
            );
            SpawnAnchor(objectPosition);
        }
        else
        {
            // No object detected in this frame; handle accordingly if needed
        }
    }

    private void SpawnAnchor(Vector3 position)
    {
        if (anchorPrefab != null)
        {
            bool anchorNearby = false;

            foreach (GameObject anchor in anchors)
            {
                if (anchor != null)
                {
                    float distance = Vector3.Distance(position, anchor.transform.position);
                    if (distance <= 1.0f)
                    {
                        anchorNearby = true;
                        break;
                    }
                }
            }

            if (!anchorNearby)
            {
                GameObject newAnchor = Instantiate(anchorPrefab, position, Quaternion.identity);
                newAnchor.layer = 30;

                Anchor anchorScript = newAnchor.GetComponent<Anchor>();
                if (anchorScript != null)
                {
                    anchorScript.playerTransform = playerTransform;
                }
                else
                {
                    Debug.LogWarning("Anchor component not found on the instantiated prefab.");
                }

                anchors.Add(newAnchor);
            }
            else
            {
                Debug.Log("An anchor already exists within 1.0 units. Not spawning a new one.");
            }
        }
        else
        {
            Debug.LogWarning("anchorPrefab is not assigned in the Inspector.");
        }
    }

    private async void SendDataAsync()
    {
        if (colorImageBytes != null)
        {
            await SendImageDataAsync("color", colorImageBytes);
        }

        if (depthImageBytes != null)
        {
            await SendImageDataAsync("depth", depthImageBytes);
        }
    }

    private async Task SendImageDataAsync(string imageType, byte[] imageBytes)
    {
        Vector3 pos = playerTransform.position;
        Quaternion rot = playerTransform.rotation;

        ImageDataMessage dataObject = new ImageDataMessage
        {
            type = imageType,
            position = new PositionData { x = pos.x, y = pos.y, z = pos.z },
            rotation = new RotationData { x = rot.x, y = rot.y, z = rot.z, w = rot.w },
            imageData = System.Convert.ToBase64String(imageBytes)
        };

        string jsonString = JsonUtility.ToJson(dataObject);
        await connection.SendTextAsync(jsonString);
    }

    private void SpawnUI()
    {
        uiCanvasInstance = Instantiate(UICanvas, playerTransform.position + playerTransform.forward * 2, playerTransform.rotation);
        uiCanvasInstance.transform.LookAt(playerTransform);

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
    public PositionData object_position;
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
}
