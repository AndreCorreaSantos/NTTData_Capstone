using UnityEngine;

public class ComputeColors : MonoBehaviour
{
    public setColors setColors;
    public Camera UICamera;
    public ComputeShader computeShader;

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

        InitializeRenderTexture();
    }

    void InitializeRenderTexture()
    {
        computeTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        computeTexture.enableRandomWrite = true;
        computeTexture.Create();
        UICamera.targetTexture = computeTexture;

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
        RenderTexture.active = computeTexture;
        UICamera.Render();
        RenderTexture.active = null;

        Debug.Log("Captured camera output to compute texture.");
    }

    void ExecuteComputeShader()
    {
        computeShader.SetTexture(0, "Result", computeTexture);
        computeShader.Dispatch(0, computeTexture.width / 8, computeTexture.height / 8, 1);

        Debug.Log("Executed compute shader.");
    }

    void ReadFromComputeTexture()
    {
        RenderTexture.active = computeTexture;
        texture2D.ReadPixels(new Rect(0, 0, computeTexture.width, computeTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        string path = Application.dataPath + "/ComputeColors.png";
        System.IO.File.WriteAllBytes(path, texture2D.EncodeToPNG());
        Debug.Log("Saved image to " + path);
    }

    private void OnDestroy()
    {
        if (computeTexture != null)
        {
            computeTexture.Release();
        }
    }
}
