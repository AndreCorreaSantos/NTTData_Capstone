// using System.Collections;
// using System.Collections.Generic;
// using Unity.Sentis.Layers;
// using UnityEngine;
// using Unity.Sentis;

// public class runModel : MonoBehaviour
// {

//     [SerializeField]
//     public ModelAsset modelAsset;
//     private Model runtimeModel;
//     private IWorker worker;
//     private TensorFloat inputTensor;
    
//     private float[] results;
//     private string[] labels;

//     void Start()
//     {
//         runtimeModel = ModelLoader.Load(modelAsset);
//         worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);
//         //read labels_new.txt
//         labels = System.IO.File.ReadAllLines("Assets/labels_new.txt");

//     }

//     public string RunInference(Texture2D inputTexture)
//     {
//         inputTensor?.Dispose();
//         inputTensor = TextureConverter.ToTensor(inputTexture,244,244,3);
//         worker.Execute(inputTensor);

//         TensorFloat outputTensor = worker.PeekOutput() as TensorFloat;
//         outputTensor.MakeReadable();
//         results =  outputTensor.ToReadOnlyArray();
//         outputTensor.Dispose();
//         // get the index of the max value
//         int maxIndex = 0;
//         for (int i = 0; i < results.Length; i++)
//         {
//             if (results[i] > results[maxIndex])
//             {
//                 maxIndex = i;
//             }
//         }
//         string prediction = labels[maxIndex];
//         return prediction;
//     }
//     private void OnDisable()
//     {
//         inputTensor?.Dispose();
//         worker.Dispose();
//     }
// }
