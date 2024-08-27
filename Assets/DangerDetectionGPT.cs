using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DangerDetectionGPT : MonoBehaviour
{
    bool isCollectingScreenshots = false;
    bool hasCollectedScreenshots = false;
    bool hasSentRequest = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ScreenCapture.CaptureScreenshot(Application.dataPath + "/CapturedImages/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png");
        UnityEditor.AssetDatabase.Refresh();
    }
    void Update()
    {
        if (!isCollectingScreenshots)
        {
            isCollectingScreenshots = true;
            StartCoroutine(TakeScreenshotPeriodically());
        }
        if (!isCollectingScreenshots && !hasSentRequest && hasCollectedScreenshots ) {
            hasSentRequest = true;
            StartCoroutine(SendBatchToOpenAI());
        }
    }

    IEnumerator TakeScreenshotPeriodically()
    {
        for (int i = 0; i < 10; i++)
        {
            ScreenCapture.CaptureScreenshot(Application.dataPath + "/CapturedImages/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png");
            yield return new WaitForSeconds(1f);
        }
        hasCollectedScreenshots = true;
        isCollectingScreenshots = false;
    }

    IEnumerator SendBatchToOpenAI()
    {
        hasCollectedScreenshots = false;
        // send batch of images to chatgpt
        yield return new WaitForSeconds(10f);
        ClearCapturedImages();

        //delete batch from local registry
        yield return new WaitForSeconds(10f);
    }

    void ClearCapturedImages()
    {
        string[] filePaths = Directory.GetFiles(Application.dataPath + "/CapturedImages/");
        foreach (string filePath in filePaths)
            File.Delete(filePath);
    }
}
