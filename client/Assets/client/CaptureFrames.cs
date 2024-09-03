using UnityEngine;

public class CaptureFrames : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Camera renderCamera;
    private RenderTexture renderTexture;
    private Texture2D imageTexture;
    void Start()
    {
        renderCamera = Camera.main;
        if(renderCamera == null)
        {
            // get the first camera in the scene
            renderCamera = Camera.allCameras[0];
        }
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            renderCamera.targetTexture = renderTexture;
        }
        if (imageTexture == null)
        {
            imageTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        }
    }



    public Texture2D GetCameraImage()
    {
        renderCamera.targetTexture = renderTexture;
        renderCamera.Render();
        RenderTexture.active = renderTexture;
        imageTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        imageTexture.Apply();
        RenderTexture.active = null;
        renderCamera.targetTexture = null;
        return imageTexture;
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }

    void OnDisable()
{
    if (renderTexture != null)
    {
        renderTexture.Release();
        Destroy(renderTexture);
        renderTexture = null;
    }
}
}
