using UnityEngine;

public class ComputeColors : MonoBehaviour
{
    public setColors setColors;
    public Camera UICamera;
    public ComputeShader computeShader;

    private RenderTexture renderTexture;
    private RenderTexture computeTexture;
    private Texture2D texture2D;

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

        InitializeRenderTextures();
    }

    void InitializeRenderTextures()
    {
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        UICamera.targetTexture = renderTexture;

        computeTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        computeTexture.enableRandomWrite = true;
        computeTexture.Create();

        texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
    }

    void Update()
    {
        CaptureCameraOutput();
        ExecuteComputeShader();
        ReadFromComputeTexture();
    }

    void CaptureCameraOutput()
    {
        RenderTexture.active = renderTexture;
        UICamera.Render();
        RenderTexture.active = null;
    }

    void ExecuteComputeShader()
    {
        computeShader.SetTexture(0, "Result", computeTexture);
        computeShader.Dispatch(0, computeTexture.width / 8, computeTexture.height / 8, 1);
    }

    void ReadFromComputeTexture()
    {
        RenderTexture.active = computeTexture;
        texture2D.ReadPixels(new Rect(0, 0, computeTexture.width, computeTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        Debug.Log(texture2D.GetPixel(0, 0));
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        if (computeTexture != null)
        {
            computeTexture.Release();
        }
    }
}
