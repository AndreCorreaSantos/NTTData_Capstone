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

    public Canvas canvas;
    public GameObject boundingBoxPrefab;

    private List<GameObject> boundingBoxes;

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

        boundingBoxes = new List<GameObject>();
        

    }

    void Update()
    {

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

    //thanks to AuriMoogie at https://discussions.unity.com/t/how-can-i-draw-something-in-screen-pixel-coordinates-in-a-ondrawgizmos-in-monobehaviour/165323/3
    private static Vector2 PixelToCameraClipPlane(
        Camera camera,
        Canvas canvas,
        Vector2 screenPos)
    {
        // The canvas scale factor affects the
        // screen position of all UI elements.

        Vector2 ViewportPosition = screenPos;
        RectTransform CanvasRect = canvas.GetComponent<RectTransform>();
        screenPos = new Vector2(screenPos.x, screenPos.y);

        Vector2 WorldObject_ScreenPosition = new Vector2(
        ((ViewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f)),
        ((ViewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f)));

        return WorldObject_ScreenPosition;
    }
}
