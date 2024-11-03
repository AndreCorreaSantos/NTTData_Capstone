using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEditor.Experimental.GraphView;
// using UnityEngine.UIElements;
// using UnityEngine.UIElements;

public enum State
{
    FollowerCentralized,
    HorizontalSway,
    VerticalSway,
    Stable,
    AdjustToPlayerView
}

public class GUIMovementStateMachine
{
    private State currentState;
    public float desiredMotionVal;

    public GUIMovementStateMachine()
    {
        currentState = State.FollowerCentralized; // Initial state
        desiredMotionVal = 0;
    }

    public void TransitionState(float angleFromCameraFustrum, int collisionCountMain, int collisionCountSide, float maxSwayAngleFromCameraFustrum = 40f, float maxAngleFromCameraFustrum = 70f, float minAngleFromCameraFustrumAfterAdjust = 5f)
    {
        switch (currentState) {
            case State.FollowerCentralized:
                Debug.Log("q1");
                if (collisionCountMain > 1) currentState = State.HorizontalSway; break;
            case State.HorizontalSway:
                Debug.Log("q2");
                if (collisionCountMain == 0) currentState = State.Stable;
                else if (angleFromCameraFustrum > maxAngleFromCameraFustrum) currentState = State.VerticalSway;
                break;
            case State.VerticalSway:
                Debug.Log("q3");
                if (collisionCountMain == 0) currentState = State.Stable;
                if (angleFromCameraFustrum > maxAngleFromCameraFustrum) currentState = State.AdjustToPlayerView;
                break;
            case State.Stable:
                Debug.Log("q4");
                if (angleFromCameraFustrum > maxAngleFromCameraFustrum) currentState = State.AdjustToPlayerView;
                else if (collisionCountMain > 0) currentState = State.HorizontalSway;
                break;
            case State.AdjustToPlayerView:
                Debug.Log("q5");
                if (angleFromCameraFustrum < minAngleFromCameraFustrumAfterAdjust) currentState = State.FollowerCentralized;
                break;
        }
    }

    public State GetCurrentState()
    {
        return currentState;
    }

    public void AnalyzeAndMoveGUI(GameObject gui, Transform CameraTransform, List<string> uiObstructedObjectsMain, List<string> uiObstructedObjectsSide, Dictionary<string, GameObject> anchors, float uiFollowSpeed)
    {
        float distanceFromPlayer = 2.0f; // Adjust this value as needed
        Vector3 forwardDirection = CameraTransform.forward;
        Vector3 baseDesiredPosition = CameraTransform.position + forwardDirection * distanceFromPlayer;

        Vector3 desiredPosition = baseDesiredPosition;

        // Direction from player to current UI position
        Vector3 dirToCanvas = gui.transform.position - CameraTransform.position;
        dirToCanvas.y = 0;
        dirToCanvas.Normalize();

        // Direction from player to average anchor position
        Vector3 avgAnchorPos = GetAvgAnchorPos(anchors);
        Vector3 dirToAvgAnchor = avgAnchorPos - CameraTransform.position;
        dirToAvgAnchor.y = 0;
        dirToAvgAnchor.Normalize();

        // Cross product to determine rotation direction
        Vector3 cross = Vector3.Cross(dirToCanvas, dirToAvgAnchor);

        // Determine rotation direction based on cross product
        float rotationDirection = cross.y > 0 ? -1f : 1f;

        // Adjust desired position by rotating it around the player
        float rotationSpeed = 5.0f; // Degrees per second, adjust as needed

        //calculate angle in 2D XZ plane between camera and object
        Vector3 CameraPosition = new Vector3(CameraTransform.position.x, CameraTransform.position.y, CameraTransform.position.z);
        Vector3 CameraDirection = new Vector3(CameraTransform.forward.x, 0, CameraTransform.forward.z);
        Vector3 dirToCanvasNoY = new Vector3(dirToCanvas.x, 0, dirToCanvas.z);

        float angleInDegrees = Vector3.Angle(CameraDirection, dirToCanvas);
        TransitionState(angleInDegrees, uiObstructedObjectsMain.Count, uiObstructedObjectsSide.Count);
        switch (currentState)
        {
            case State.FollowerCentralized:
                desiredPosition = CameraPosition + forwardDirection * distanceFromPlayer;
                break;
            case State.HorizontalSway:
                float angularSpeed = 5f * rotationDirection;
                desiredPosition = RotateAroundPoint(
                    gui.transform.position,
                    CameraPosition,
                    Quaternion.Euler(0, angularSpeed, 0)
                );

                break;
            case State.VerticalSway:
                desiredPosition = gui.transform.position;
                desiredPosition.y = CameraPosition.y + 1;
                break;
            case State.Stable:
                desiredPosition = gui.transform.position;
                break;
            case State.AdjustToPlayerView:
                desiredPosition = CameraPosition + forwardDirection * distanceFromPlayer;
                break;
        }
        float positionLerpSpeed = uiFollowSpeed * Time.deltaTime;
        gui.transform.position = Vector3.Lerp(
            gui.transform.position,
            desiredPosition,
            positionLerpSpeed
        );

    }
    Vector3 GetAvgAnchorPos(Dictionary<string, GameObject> anchors)
    {
        Vector3 avgAnchorPos = Vector3.zero;
        foreach (var anchor in anchors.Values)
        {
            avgAnchorPos += anchor.transform.position;
        }
        avgAnchorPos /= anchors.Count;
        return avgAnchorPos;
    }
    private Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }

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
    private GameObject booleanToggle;

    private byte[] colorImageBytes;
    private byte[] depthImageBytes;

    private float timeSinceLastSend = 0f;
    private float sendInterval = 0.5f;

    private Vector3[] UIScreenCorners = new Vector3[4];
    [SerializeField] private bool flipColors = false;

    public Dictionary<string, GameObject> anchors = new Dictionary<string, GameObject>();

    public GameObject debugobj;

    // Variables for smooth UI movement
    public float uiFollowSpeed = 5f; // Adjust this value to control follow speed

    // Variables for moving the UI out of the way
    private float uiVerticalOffset = 0f;
    private float uiVerticalOffsetVelocity = 0f;
    public float uiMoveAmount = 1.0f; // Distance to move the UI when obstructed
    public float uiMoveDuration = 0.5f; // Time to move the UI when obstructed
    public List<string> uiObstructedObjectsMain = new List<string>();
    public List<string> uiObstructedObjectsSide = new List<string>();

    public GameObject redDot;

    private float cumulativeRotationAngle = 0f; // Current cumulative rotation angle in degrees
    public float maxRotationAngle = 90f;        // Maximum rotation angle to prevent over-rotation

    private GUIMovementStateMachine guiMovementStateMachine;


    void Start()
    {
        guiMovementStateMachine = new GUIMovementStateMachine();
        debugobj = Instantiate(redDot, new Vector3(0, 0, 0), Quaternion.identity);
        StartWebSocket();
        SpawnUI();
        connection.OnServerMessage += HandleServerMessage;
        // redDot = Instantiate(redDot, new Vector3(0, 0, 0), Quaternion.identity);
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
                //Debug.Log($"Screen Corner {i}: {UIscreenCorner}");
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

    private Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        return rotation * (point - pivot) + pivot;
    }
    private void UpdateUIPosition()
    {
        if (uiCanvasInstance != null && playerCamera != null)
        {
            guiMovementStateMachine.AnalyzeAndMoveGUI(uiCanvasInstance, playerCamera.transform, uiObstructedObjectsMain, uiObstructedObjectsSide, anchors, uiFollowSpeed);
            /*
            Debug.Log(guiMovementStateMachine.GetCurrentState());
            // Desired position directly in front of the player
            float distanceFromPlayer = 2.0f; // Adjust this value as needed
            Vector3 forwardDirection = playerCamera.transform.forward;
            Vector3 baseDesiredPosition = playerCamera.transform.position + forwardDirection * distanceFromPlayer;

            Vector3 desiredPosition = baseDesiredPosition;

            // Direction from player to current UI position
            Vector3 dirToCanvas = uiCanvasInstance.transform.position - playerCamera.transform.position;
            dirToCanvas.y = 0;
            dirToCanvas.Normalize();

            // Direction from player to average anchor position
            Vector3 avgAnchorPos = GetAvgAnchorPos();
            Vector3 dirToAvgAnchor = avgAnchorPos - playerCamera.transform.position;
            dirToAvgAnchor.y = 0;
            dirToAvgAnchor.Normalize();

            // Cross product to determine rotation direction
            Vector3 cross = Vector3.Cross(dirToCanvas, dirToAvgAnchor);

            // Determine rotation direction based on cross product
            float rotationDirection = cross.y > 0 ? -1f : 1f;

            // Adjust desired position by rotating it around the player
            float rotationSpeed = 5.0f; // Degrees per second, adjust as needed

            //calculate angle in 2D XZ plane between camera and object
            Vector3 CameraPosition = new Vector3(playerCamera.transform.position.x, playerCamera.transform.position.y, playerCamera.transform.position.z);
            Vector3 CameraDirection = new Vector3(playerCamera.transform.forward.x, 0, playerCamera.transform.forward.z);
            Vector3 dirToCanvasNoY = new Vector3(dirToCanvas.x, 0, dirToCanvas.z);

            float angleInDegrees = Vector3.Angle(CameraDirection, dirToCanvas);
            print(angleInDegrees);


            if (guiMovementStateMachine.GetCurrentState() == State.AdjustToPlayerView)
            {
                Debug.Log("A");
                desiredPosition = playerCamera.transform.position + forwardDirection * distanceFromPlayer;
                Debug.Log(angleInDegrees);
                if (angleInDegrees < 5f)
                {
                    guiMovementStateMachine.TransitionTo(State.NotBlocked);
                }

            }
            else if (uiObstructedObjectsMain.Count > 0)
            {
                Debug.Log("B");

                //CREATE A STATE MACHINE TO MAKE THIS BS CLEARER

                Debug.Log(angleInDegrees)
                if (angleInDegrees < 40f)
                {
                    guiMovementStateMachine.TransitionTo(State.Rotating);
                    // Increase cumulative rotation angle over time
                    cumulativeRotationAngle += rotationSpeed * Time.deltaTime;

                    // Limit the cumulative rotation angle to the maximum allowed
                    cumulativeRotationAngle = Mathf.Min(cumulativeRotationAngle, maxRotationAngle);

                    // Calculate the total rotation angle with direction
                    float totalRotationAngle = cumulativeRotationAngle * rotationDirection;

                    // Rotate the desired position around the player's position
                    desiredPosition = RotateAroundPoint(
                        baseDesiredPosition,
                        playerCamera.transform.position,
                        Quaternion.Euler(0, totalRotationAngle, 0)
                    );
                    //Debug.Log($"Obstructed: Rotating UI by cumulative angle {cumulativeRotationAngle} degrees");
                }
                else if (angleInDegrees > 45f && angleInDegrees <= 65f)
                {
                    cumulativeRotationAngle = 0;
                    guiMovementStateMachine.TransitionTo(State.Suspended);

                    float targetVerticalOffset = 1f;
                    float smoothTime = uiMoveDuration;
                    uiVerticalOffset = Mathf.SmoothDamp(uiVerticalOffset, targetVerticalOffset, ref uiVerticalOffsetVelocity, smoothTime);

                    // Apply vertical offset
                    desiredPosition = uiCanvasInstance.transform.position;
                    desiredPosition.y = playerCamera.transform.position.y + 1;

                }
                else
                {
                    guiMovementStateMachine.TransitionTo(State.AdjustToPlayerView);
                }

            }
            else if (uiObstructedObjectsSide.Count > 0)
            {
                Debug.Log("C");

                //calculate angle in 2D XZ plane between camera and object
                //check if held outside of camera

                // Rotate the desired position around the player's position
                cumulativeRotationAngle = 0;
                desiredPosition = uiCanvasInstance.transform.position;

                if (angleInDegrees > 45f && angleInDegrees <= 65f)
                {
                    guiMovementStateMachine.TransitionTo(State.Suspended);
                    desiredPosition = uiCanvasInstance.transform.position;
                    desiredPosition.y = playerCamera.transform.position.y + 1;
                }
                if (angleInDegrees > 65f)
                {
                    guiMovementStateMachine.TransitionTo(State.AdjustToPlayerView);
                }
            }
            else
            {
                Debug.Log("D");
                
                cumulativeRotationAngle = 0;
                desiredPosition = playerCamera.transform.position + forwardDirection * distanceFromPlayer;
            }

            // Smoothly move the UI towards the adjusted desired position
            float positionLerpSpeed = uiFollowSpeed * Time.deltaTime;
            uiCanvasInstance.transform.position = Vector3.Lerp(
                uiCanvasInstance.transform.position,
                desiredPosition,
                positionLerpSpeed
            );

            // Optionally, make the UI face the player if desired
            // Uncomment if you want the UI to always face the player
            // uiCanvasInstance.transform.LookAt(playerCamera.transform.position);

            Debug.Log($"UI Position updated: {uiCanvasInstance.transform.position}");
            */
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
            foreach (ObjectData objData in frameData.objects)
            {
                SpawnAnchor(objData);
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

    // Vector3 GetWorldPositionFromScreenSpace(Vector3 screenPos,Matrix4x4 invMat) {
    //     // Convert screen position to normalized device coordinates (NDC)
    //     float ndcX = (screenPos.x - (Screen.width * 0.5f)) / (Screen.width * 0.5f);  // Range [-1, 1]
    //     float ndcY = (screenPos.y - (Screen.height * 0.5f)) / (Screen.height * 0.5f); // Range [-1, 1]

    //     // Create a point in NDC space (using the near clip plane for z = -1)
    //     Vector4 nearClipPoint = new Vector4(ndcX, -ndcY, -1.0f, 1.0f);

    //     // Transform the NDC point to world space using the inverse matrix
    //     Vector4 worldNear = invMat * nearClipPoint;

    //     // Perform perspective divide to convert the Vector4 to Vector3
    //     Vector3 worldNearPos = new Vector3(worldNear.x / worldNear.w, worldNear.y / worldNear.w, worldNear.z / worldNear.w);

    //     // Get the camera's forward direction to compute ray direction
    //     Vector3 rayDirection = (worldNearPos - playerCamera.transform.position).normalized;

    //     // Scale the ray direction by the provided depth value (world space depth)
    //     Vector3 worldPosition = playerCamera.transform.position + rayDirection * screenPos.z;

    //     return worldPosition;
    // }
    private void SpawnAnchor(ObjectData objData)
    {

        Vector3 worldPosition = new Vector3(objData.x, objData.y, objData.z);
        Vector3 localScale = new Vector3(objData.width, objData.height, objData.width);
        string id = objData.id;

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
            // Destroy(anchors[id]);
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

        Matrix4x4 invMat = (playerCamera.projectionMatrix * playerCamera.worldToCameraMatrix).inverse;
        ImageDataMessage dataObject = new ImageDataMessage
        {
            type = imageType,
            data = new ObjectData { x = pos.x, y = pos.y, z = pos.z, id = "Null", height = 0, width = 0 },
            invMat = invMat,
            imageData = System.Convert.ToBase64String(imageBytes),
            UIScreenCorners = UIScreenCorners,
            flipColors = flipColors,
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
            if (uiCanvasInstance == null)
            {
                Debug.LogError("Failed to instantiate UI Canvas.");
                return;
            }
            /* booleanToggle = uiCanvasInstance.transform.GetChild(1).GetChild(0).GetChild(3).GetChild(0).GetChild(0).GetChild(4).GetChild(1).GetChild(0).gameObject; */
            booleanToggle = GameObject.FindWithTag("BooleanToggleTag");
            booleanToggle.GetComponent<Toggle>().onValueChanged.AddListener(delegate {
                FlipColors();
            });


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

    // public void MoveUIOutOfWay()
    // {
    //     uiObstructedCount++;
    // }

    // public void ReturnUIToOriginalPosition()
    // {
    //     uiObstructedCount = Mathf.Max(0, uiObstructedCount - 1);
    // }

    public void FlipColors()
    {
        flipColors = !flipColors;
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
        public float width;
        public float height;
    }


    [System.Serializable]
    public class ImageDataMessage
    {
        public string type;
        public ObjectData data;
        public Matrix4x4 invMat;
        public string imageData;
        public float fx;
        public float fy;
        public float cx;
        public float cy;
        public Vector3[] UIScreenCorners;
        public bool flipColors;
    }
}
