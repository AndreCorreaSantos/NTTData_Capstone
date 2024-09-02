using UnityEngine;

public class ComputeColors : MonoBehaviour
{
    public setColors setColors;
    public Camera UICamera;

    private RenderTexture renderTexture;
    private Texture2D texture2D;

    void Start()
    {
        if (setColors == null)
        {
            setColors = GetComponent<setColors>();
            if (setColors == null)
            {
                Debug.LogError("SetColors component is not assigned and cannot be found on the GameObject.");
                return;
            }
        }

        if (UICamera == null)
        {
            UICamera = GameObject.Find("UICamera").GetComponent<Camera>();
            if (UICamera == null)
            {
                Debug.LogError("UICamera is not assigned and cannot be found in the scene.");
                return;
            }
        }

        InitializeResources();
    }

    void InitializeResources()
    {
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        UICamera.targetTexture = renderTexture;

        texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
    }

    void Update()
    {
        CaptureCameraOutput();

        Color backgroundColor = GetAverageColor();
        Vector3 backgroundCielab = ColorConversion.RGBToCIELAB(backgroundColor);

        Color textColor = Color.white;
        Vector3 textCielab = ColorConversion.RGBToCIELAB(textColor);


        backgroundCielab.x -=30;
        textCielab.x = backgroundCielab.x + 60;

        Color newBackgroundColor = ColorConversion.CIELABToRGB(backgroundCielab);
        Color newTextColor = ColorConversion.CIELABToRGB(textCielab);



        setColors.SetColor(newBackgroundColor, newTextColor);
    }

    private void CaptureCameraOutput()
    {
        RenderTexture.active = renderTexture;
        UICamera.Render();
        texture2D.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;
    }

    private Color GetAverageColor()
    {
        Color[] pixels = texture2D.GetPixels();
        Vector3 sum = Vector3.zero;

        foreach (Color pixel in pixels)
        {
            sum += new Vector3(pixel.r, pixel.g, pixel.b);
        }

        Vector3 average = sum / pixels.Length;
        return new Color(average.x, average.y, average.z);
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}
