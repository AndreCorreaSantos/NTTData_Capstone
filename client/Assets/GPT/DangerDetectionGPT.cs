// using System;
// using System.Collections;
// using System.IO;
// using UnityEngine;
// using UnityEditor.Scripting.Python;
// using static UnityEngine.Rendering.HDROutputUtils;

// public class DangerDetectionGPT : MonoBehaviour
// {
//     bool isCollectingScreenshots = false;
//     bool hasCollectedScreenshots = false;
//     bool hasSentRequest = false;

//     enum DangerDetectionState
//     {
//         CollectScreenshots,
//         SendCollectedData,
//         ClearCollectedData,
//         Busy
//     }

//     DangerDetectionState CurrentState;

//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//         //ScreenCapture.CaptureScreenshot(Application.dataPath + "/CapturedImages/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png");
//         //UnityEditor.AssetDatabase.Refresh();
//         CurrentState = DangerDetectionState.ClearCollectedData;
//     }

//     void Update()
//     {
//         /*if (!isCollectingScreenshots)
//         {
//             isCollectingScreenshots = true;
//             StartCoroutine(TakeScreenshotPeriodically());
//         }
//         if (!isCollectingScreenshots && !hasSentRequest && hasCollectedScreenshots ) {
//             hasSentRequest = true;
//             StartCoroutine(SendBatchToOpenAI());
//         }
//         */
//         switch (CurrentState)
//         {
//             case DangerDetectionState.CollectScreenshots:
//                 CurrentState = DangerDetectionState.Busy;
//                 StartCoroutine(TakeScreenshotPeriodically());
//                 break;
//             case DangerDetectionState.SendCollectedData:
//                 CurrentState = DangerDetectionState.Busy;
//                 StartCoroutine(SendBatchToOpenAI());
//                 break;
//             case DangerDetectionState.ClearCollectedData:
//                 CurrentState = DangerDetectionState.Busy;
//                 StartCoroutine(ClearCapturedImages());
//                 break;
//             case DangerDetectionState.Busy:
//                 Debug.Log("State machine is currently busy");
//                 break;
//         }
//         //PythonRunner.RunFile($"{Application.dataPath}/GPT/SendBatchToGpt.py");
//     }

//     IEnumerator TakeScreenshotPeriodically()
//     {
//         Debug.Log("TakeScreenshotPeriodically");
//         for (int i = 0; i < 6; i++)
//         {
//             ScreenCapture.CaptureScreenshot(Application.dataPath + "/GPT/CapturedImages/" + "Screenshot" + i + ".png");
//             UnityEditor.AssetDatabase.Refresh();
//             yield return new WaitForSeconds(1f);
//         }
//         CurrentState = DangerDetectionState.SendCollectedData;
//     }

//     IEnumerator SendBatchToOpenAI()
//     {
//         Debug.Log("SendBatchToOpenAI");

//         //Responsability of sending and analyzing results to Python script, due to current version of Unity's C# not supporting the OpenAI API for dotnet.

//         // TODO: Check if async. If not, make the call to Python async, otherwise we're kinda fucked.

//         // TODO: Remove comment above once fixed
//         PythonRunner.RunFile($"{Application.dataPath}, /GPT/SendBatchToGpt.py");
//         yield return new WaitForSeconds(1f);
//         CurrentState = DangerDetectionState.ClearCollectedData;
//     }

//     IEnumerator ClearCapturedImages()
//     {
//         Debug.Log("ClearCapturedImages");
//         string[] filePaths = Directory.GetFiles(Application.dataPath + "/GPT/CapturedImages/");
//         foreach (string filePath in filePaths)
//             File.Delete(filePath);
//         CurrentState = DangerDetectionState.CollectScreenshots;
//         yield return null;
//     }
// }
