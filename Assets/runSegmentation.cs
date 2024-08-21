using System.Collections;
using System.Collections.Generic;
using Sample;
using UnityEngine;

public class runSegmentation : MonoBehaviour
{
    public GameObject ui;
    private Vector3[] screenCorners = new Vector3[4];
    public Camera renderCamera; 
    public RenderTexture renderTexture; 
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
            renderCamera.targetTexture = renderTexture;
        }
        if (imageTexture == null)
        {
            imageTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        }
       
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

        //TODO: DRAW BOUNDING BOX ON screen Corners

        // Get the camera image and perform inference
        List<ObjectDetectionResult> detectionResults = objectDetect.Inference(GetCameraImage());
        foreach(var obj in detectionResults)
        {
            Rect r = obj.rect;
            // DRAW BOUNDING BOX IN DETECTED OBJECTS
        }
        // DRAW BOUNDING BOX IN SCREEN CORNERS

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
}
