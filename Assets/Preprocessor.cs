using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Preprocessor : MonoBehaviour
{   
    public Texture2D Preprocess(Texture2D input, int width, int height)
    {
        // Resize the input image to the specified width and height
        Texture2D resizedImage = ResizeImage(input, width, height);

        // Convert the resized image to a Color array
        Color[] pixels = resizedImage.GetPixels();

        // Mean and standard deviation for normalization
        Vector3 mean = new Vector3(0.485f, 0.456f, 0.406f);
        Vector3 std = new Vector3(0.229f, 0.224f, 0.225f);

        // Normalize the pixels
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i].r = (pixels[i].r - mean.x) / std.x;
            pixels[i].g = (pixels[i].g - mean.y) / std.y;
            pixels[i].b = (pixels[i].b - mean.z) / std.z;
        }

        // Apply the normalized colors back to the texture
        Texture2D output = new Texture2D(width, height);
        output.SetPixels(pixels);
        output.Apply();

        return output;
    }
    private Texture2D ResizeImage(Texture2D sourceTexture, int targetWidth, int targetHeight)
    {
        // Create a RenderTexture with the target dimensions
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Bilinear;

        // Set the RenderTexture as the target for rendering
        RenderTexture.active = rt;

        // Draw the source texture to the RenderTexture, scaling it to fit the new size
        Graphics.Blit(sourceTexture, rt);

        // Create a new Texture2D with the desired size
        Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);

        // Copy the RenderTexture to the new Texture2D
        resizedTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resizedTexture.Apply();

        // Release the RenderTexture
        RenderTexture.ReleaseTemporary(rt);

        return resizedTexture;
    }
}
