using UnityEngine;

public class ComputeColors : MonoBehaviour
{
    public setColors setColors;
    public Camera UICamera;
    public ComputeShader computeShader;

    private RenderTexture computeTexture;
    private Texture2D texture2D;
    private ComputeBuffer resultBuffer;
    private float scale_factor = 100000.0f;
    private int width;
    private int height;

    void Start()
    {
        if (setColors == null)
        {
            setColors = GetComponent<setColors>();
        }

        if (UICamera == null)
        {
            UICamera = GameObject.Find("UICamera").GetComponent<Camera>();
        }

        InitializeResources();
    }

    void InitializeResources()
    {
        computeTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        computeTexture.enableRandomWrite = true;
        computeTexture.Create();
        UICamera.targetTexture = computeTexture;

        texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);


        width = computeTexture.width;
        height = computeTexture.height;


        resultBuffer = new ComputeBuffer(3, sizeof(uint));

        computeShader.SetBuffer(0, "ResultBuffer", resultBuffer);
        computeShader.SetFloat("scale_factor", scale_factor);
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
    }

    void Update()
    {
        CaptureCameraOutput();
        ExecuteComputeShader();
        // ReadFromComputeTexture();
        ReadComputeBuffer();
    }

    void CaptureCameraOutput()
    {
        RenderTexture.active = computeTexture;
        UICamera.Render();
        RenderTexture.active = null;
    }

    void ExecuteComputeShader()
    {

        uint[] zeroArray = new uint[3] { 0, 0, 0 };
        resultBuffer.SetData(zeroArray);

        computeShader.SetTexture(0, "Result", computeTexture);
        computeShader.Dispatch(0, Mathf.CeilToInt(width / 8.0f), Mathf.CeilToInt(height / 8.0f), 1);
    }

    void ReadFromComputeTexture()
    {
        RenderTexture.active = computeTexture;
        texture2D.ReadPixels(new Rect(0, 0, computeTexture.width, computeTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        // string path = Application.dataPath + "/ComputeColors.png";
        // System.IO.File.WriteAllBytes(path, texture2D.EncodeToPNG());
        // Debug.Log("Saved image to " + path);
    }

    void ReadComputeBuffer()
    {

        uint[] resultData = new uint[resultBuffer.count];
        resultBuffer.GetData(resultData);

        float averageR = (float)resultData[0] / (scale_factor * width * height);
        float averageG = (float)resultData[1] / (scale_factor * width * height);
        float averageB = (float)resultData[2] / (scale_factor * width * height);

        Color backgroundColor = new Color(averageR, averageG, averageB,1);
        Color textColor = new Color(1 - averageR, 1 - averageG, 1 - averageB,1);
        Debug.Log("Background color: " + backgroundColor);
        setColors.SetColor(backgroundColor, textColor);
    }

    private void OnDestroy()
    {
        if (computeTexture != null)
        {
            computeTexture.Release();
        }

        if (resultBuffer != null)
        {
            resultBuffer.Release();
        }
    }
}
