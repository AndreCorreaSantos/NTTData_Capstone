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
        // Optionally update the screenCorners if the UI element moves
        RectTransform rectTransform = ui.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        for (int i = 0; i < 4; i++)
        {
            screenCorners[i] = Camera.main.WorldToScreenPoint(corners[i]);
            //Debug.Log("ScreenCorners"+i+screenCorners[i]);
        }

        float minXUI = Mathf.Min(screenCorners[0].x,screenCorners[1].x);
        float maxXUI = Mathf.Max(screenCorners[2].x,screenCorners[3].x);
        float widthUI = maxXUI - minXUI;
        float minYUI = Mathf.Min(screenCorners[0].y,screenCorners[1].y,screenCorners[2].y,screenCorners[3].y);
        float maxYUI = Mathf.Max(screenCorners[1].y,screenCorners[2].y);
        float heightUI = maxYUI - minYUI;
        float centerXUI = maxXUI - widthUI/2;
        float centerYUI = maxYUI - heightUI/2;

        // Draw UI bounding rectangle on screen:

        Rect rectUI = new Rect (minXUI,minYUI,widthUI,heightUI);
        GameObject boundingBoxUI;
        boundingBoxUI = Instantiate(boundingBoxPrefab);
        boundingBoxes.Add(boundingBoxUI);
        boundingBoxUI.transform.SetParent(canvas.transform, false);
        RectBehaviour rbUI = boundingBoxUI.GetComponent<RectBehaviour>();

        //Get project from camera to canvas
        Vector2 pointOnCameraUI = new Vector2(rectUI.x, rectUI.y);
        Vector2 pointOnCanvasUI = PixelToCameraClipPlane(renderCamera, canvas, pointOnCameraUI);
        Rect canvasRectUI = new Rect(pointOnCanvasUI.x, pointOnCanvasUI.y, rectUI.width, rectUI.height);
        boundingBoxUI.GetComponent<RectTransform>().anchoredPosition = new Vector3(rectUI.x + rectUI.width/4 -  canvas.GetComponent<RectTransform>().rect.width / 2, rectUI.y - rectUI.height/2 - canvas.GetComponent<RectTransform>().rect.height / 2, 0);
        rbUI.SetRectSize(canvasRectUI);
        
        //TODO: DRAW BOUNDING BOX ON screen Corners

        // Get the camera image and perform inference
        List<ObjectDetectionResult> detectionResults = objectDetect.Inference(GetCameraImage());
        for (int i = 0; i < detectionResults.Count; i++)
        {
            Rect r = detectionResults[i].rect;
            //Debug.Log(r);
            GameObject boundingBox;

            if (i >= boundingBoxes.Count)
            {
                //Instantiate object on canvas
                boundingBox = Instantiate(boundingBoxPrefab);
                boundingBoxes.Add(boundingBox);
            } else
            {
                boundingBox = boundingBoxes[i];
            }
            boundingBox.transform.SetParent(canvas.transform, false);
            RectBehaviour rb = boundingBox.GetComponent<RectBehaviour>();

            //Get project from camera to canvas
            Vector2 pointOnCamera = new Vector2(r.x, r.y);
            Vector2 pointOnCanvas = PixelToCameraClipPlane(renderCamera, canvas, pointOnCamera);
            Rect canvasRect = new Rect(pointOnCanvas.x, pointOnCanvas.y, r.width, r.height);
            boundingBox.GetComponent<RectTransform>().anchoredPosition = new Vector3(r.x + r.width/4 -  canvas.GetComponent<RectTransform>().rect.width / 2, r.y - r.height/2 - canvas.GetComponent<RectTransform>().rect.height / 2, 0);
            rb.SetRectSize(canvasRect);


            float centerX = r.x+r.width/2;
            float centerY = r.y+r.height/2;

            // Detect overlap with interface
            bool overlap = maxXUI>r.x && minXUI<r.x+r.width && maxYUI>r.y && minYUI<r.y+r.height;
            if (overlap){
                ui.transform.RotateAround(renderCamera.transform.position,Vector3.up,Mathf.Sign(centerXUI-centerX)*20*Time.deltaTime);
            }


            // DRAW BOUNDING BOX IN DETECTED OBJECTS
            //ScreenGizmos.DrawRect(GetComponent<Camera>(), canvas, r);
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

    void OnDisable() // geepeetee
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
