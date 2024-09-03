using UnityEngine;

public class ClientLogic : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Connection connection;
    public CaptureFrames captureFrames;


    void Update()
    {
        Texture2D image = captureFrames.GetCameraImage();
        // encode image to byte array
        byte[] img = image.EncodeToPNG();
        connection.SendWebSocketMessage(img);
    }
}
