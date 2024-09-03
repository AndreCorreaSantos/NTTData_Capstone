using UnityEngine;
using UnityEngine.Rendering;

public class CaptureFrames : MonoBehaviour
{
    public Camera renderCamera;
    private RenderTexture renderTexture;
    private Texture2D imageTexture;
    private bool isReadingPixels;

    void Start()
    {
        // Initialize render texture and texture2D only once
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Screen.width / 2, Screen.height / 2, 24);  // Using a lower resolution for optimization
        }

        if (imageTexture == null)
        {
            imageTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        }
    }

    public void GetCameraImageAsync(System.Action<Texture2D> callback)
    {
        if (isReadingPixels) return;

        StartCoroutine(CaptureCameraImage(callback));
    }

    private System.Collections.IEnumerator CaptureCameraImage(System.Action<Texture2D> callback)
    {
        isReadingPixels = true;

        // Temporarily set the target texture to the renderTexture
        RenderTexture previousRenderTexture = renderCamera.targetTexture;  // Save the previous targetTexture
        renderCamera.targetTexture = renderTexture;
        renderCamera.Render();

        // Asynchronously read the pixels from the GPU
        AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, request =>
        {
            if (request.hasError)
            {
                Debug.LogError("AsyncGPUReadback encountered an error.");
                isReadingPixels = false;
                return;
            }

            imageTexture.LoadRawTextureData(request.GetData<byte>());
            imageTexture.Apply();
            callback(imageTexture);

            isReadingPixels = false;
        });

        // Wait until the readback is done
        yield return new WaitUntil(() => !isReadingPixels);

        // Restore the previous target texture to ensure the camera renders to the screen
        renderCamera.targetTexture = previousRenderTexture;
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
