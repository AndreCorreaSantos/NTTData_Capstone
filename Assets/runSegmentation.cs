using System.Collections;
using System.Collections.Generic;
using Sample;
using UnityEngine;
using System.IO;

public class runSegmentation : MonoBehaviour
{
    public GameObject ui;
    private Vector3[] screenCorners = new Vector3[4];

    public Camera renderCamera; 
    public RenderTexture renderTexture; 
    public RenderTexture depthTexture;
    public Texture2D imageTexture;

    public ObjectDetection objectDetect;

    void Start()
    {
        RectTransform rectTransform = ui.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        for (int i = 0; i < 4; i++)
        {
            screenCorners[i] = Camera.main.WorldToScreenPoint(corners[i]);
        }

        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            renderTexture.Create();
        }

        if(depthTexture == null)
        {
            depthTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth);
            depthTexture.Create();
        }

        if (imageTexture == null)
        {
            imageTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        }

        renderCamera.depthTextureMode = DepthTextureMode.Depth;

        Texture2D depthImage = GetDepthBuffer();

        // Create a Texture2D with normalized depth values for saving as PNG
        Texture2D normalizedDepthImage = new Texture2D(depthImage.width, depthImage.height, TextureFormat.RGB24, false);

        for (int y = 0; y < depthImage.height; y++)
        {
            for (int x = 0; x < depthImage.width; x++)
            {
                float depthValue = depthImage.GetPixel(x, y).r;
                
                // Normalize the depth value (optional, depending on your depth range)
                float normalizedValue = Mathf.Clamp01(depthValue); // Ensure the value is between 0 and 1
                normalizedDepthImage.SetPixel(x, y, new Color(normalizedValue, normalizedValue, normalizedValue));
            }
        }

        normalizedDepthImage.Apply();

        // Encode texture into PNG
        byte[] pngData = normalizedDepthImage.EncodeToPNG();

        // Save to file
        if (pngData != null)
        {
            
            string filePath = Application.dataPath + "/DepthImage.png";
            File.WriteAllBytes(filePath, pngData);
            Debug.Log("Depth image saved to " + filePath);
        }

        // Cleanup
        Destroy(normalizedDepthImage);
        Destroy(depthImage);
    }

    void Update()
    {
        // Optionally update the screenCorners if the UI element moves
        RectTransform rectTransform = ui.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        for (int i = 0; i < 4; i++)
        {
            screenCorners[i] = Camera.main.WorldToScreenPoint(corners[i]);
        }

        // Get the camera image and perform inference
        objectDetect.Inference(GetCameraImage());
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

    public Texture2D GetDepthBuffer()
    {
        renderCamera.targetTexture = depthTexture;

        // Set the camera to render depth
        renderCamera.clearFlags = CameraClearFlags.Depth;
        renderCamera.Render();

        // Set the active RenderTexture to the depthTexture
        RenderTexture.active = depthTexture;

        // Read the depth buffer into a Texture2D
        Texture2D depthImage = new Texture2D(depthTexture.width, depthTexture.height, TextureFormat.RFloat, false);
        depthImage.ReadPixels(new Rect(0, 0, depthTexture.width, depthTexture.height), 0, 0);
        depthImage.Apply();

        // Clean up
        RenderTexture.active = null;
        renderCamera.targetTexture = null;

        return depthImage;
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
        
        if (depthTexture != null)
        {
            depthTexture.Release();
            Destroy(depthTexture);
            depthTexture = null;
        }

        if (imageTexture != null)
        {
            Destroy(imageTexture);
            imageTexture = null;
        }
    }
}
