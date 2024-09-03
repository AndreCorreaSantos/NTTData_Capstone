using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class ScreenGizmos
{
    private const float zOffset = 0.0001f;
    public static void DrawLineOnScreen(Camera camera, Canvas canvas, Vector2 startPixel, Vector2 endPixel)
    {
        if (camera == null)
        {
            Debug.Log("Camera object not passed correctly, must not be NULL");
            return;
        }
        if (canvas == null)
        {
            Debug.Log("Canvas object not passed correctly, must not be NULL");
            return;
        }

        Vector3 _3dStartPos = new Vector3(startPixel.x, startPixel.y, 0);
        _3dStartPos *= canvas.scaleFactor;
        _3dStartPos.z = camera.nearClipPlane + zOffset;
        Vector3 _3dEndPos = new Vector3(endPixel.x, endPixel.y, 0);
        _3dEndPos *= canvas.scaleFactor;
        _3dEndPos.z = camera.nearClipPlane + zOffset;

        Vector3 worldPostStart = camera.ScreenToWorldPoint(startPixel);
        Vector3 worldPostend = camera.ScreenToWorldPoint(endPixel);
    }
    public static void DrawRect(Camera camera, Canvas canvas, Rect r)
    {
        if (r == null)
        {
            Debug.LogError("Rect given is null");
        }
        
        Vector2 r1 = new Vector2(r.x, r.y);
        Vector2 r2 = new Vector2(r.x + r.width, r.y);
        Vector2 r3 = new Vector2(r.x + r.width, r.y + r.height);
        Vector2 r4 = new Vector2(r.x, r.y + r.height);
        /*
        DrawLineOnScreen(camera, canvas, r1, r2);
        DrawLineOnScreen(camera, canvas, r2, r3);
        DrawLineOnScreen(camera, canvas, r3, r4);
        DrawLineOnScreen(camera, canvas, r4, r1);
        */
    }
}