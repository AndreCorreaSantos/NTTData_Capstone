using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Sentis;

public class getImage : MonoBehaviour
{
    public Camera _mCamera;

    public ModelAsset modelAsset;
    private Model runtimeModel;
    private IWorker worker;
    private TensorFloat inputTensor;

    public RenderTexture renderTexture;
    private Preprocessor prepro;

    private string[] labels;
    public string[] topPredictions;

    void Start()
    {
        _mCamera = Camera.main;
        prepro = new Preprocessor();
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        }

        //read labels_new.txt
        labels = System.IO.File.ReadAllLines("Assets/labels_new.txt");
    }

    void Update()
    {
        topPredictions = CallModel();
    }

    private string[] CallModel()
    {
        _mCamera.targetTexture = renderTexture;
        _mCamera.Render();

        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = null;
        _mCamera.targetTexture = null;

        Texture2D input = prepro.Preprocess(texture2D, 244, 244);
        saveToAssets(input);
        float[] results = RunInference(input);

        // Get the indices of the top 5 results
        int[] topIndices = results
            .Select((value, index) => new { Value = value, Index = index })
            .OrderByDescending(item => item.Value)
            .Take(5)
            .Select(item => item.Index)
            .ToArray();

        // Return the corresponding labels
        return topIndices.Select(index => labels[index]).ToArray();
    }

    public float[] RunInference(Texture2D inputTexture)
    {
        inputTensor?.Dispose();
        saveToAssets(inputTexture);

        inputTensor = TextureConverter.ToTensor(inputTexture, 244, 244, 3);
        worker.Execute(inputTensor);

        TensorFloat outputTensor = worker.PeekOutput() as TensorFloat;
        outputTensor.MakeReadable();
        float[] results = outputTensor.ToReadOnlyArray();
        outputTensor.Dispose();
        return results;
    }

    void saveToAssets(Texture2D input)
    {
        // write the texture to a 244x244 png on assets folder
        byte[] bytes = input.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/input.png", bytes);
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}
