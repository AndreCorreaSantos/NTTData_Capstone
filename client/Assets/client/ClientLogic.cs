using UnityEngine;

public class ClientLogic : MonoBehaviour
{
    public Connection connection;  
    public CaptureFrames captureFrames; 

    private bool isWebSocketConnected = false;

    void Update()
    {
        if (captureFrames != null && connection != null && isWebSocketConnected)
        {
            // Call the asynchronous method to capture the camera image
            captureFrames.GetCameraImageAsync(OnImageCaptured);
        }
    }

    public void StartWebSocket()
    {
        connection.StartConnection();
        isWebSocketConnected = true;
    }

    // Callback method that gets called when the image capture is complete
    private void OnImageCaptured(Texture2D image)
    {
        if (image != null)
        {
            // Encode image to byte array
            byte[] img = image.EncodeToPNG();
            connection.SendWebSocketMessage(img);
        }
    }
}
