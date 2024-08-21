using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class runSegmentation : MonoBehaviour
{
    public GameObject ui;
    private Vector3[] screenCorners = new Vector3[4];

    void Start()
    {
        RectTransform rectTransform = ui.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        for (int i = 0; i < 4; i++)
        {
            screenCorners[i] = Camera.main.WorldToScreenPoint(corners[i]);
        }
    }


    void Update()
    {
        // Optionally update the screenCorners if the UI element moves
        RectTransform rectTransform = ui.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        for (int i = 0; i < 4; i++)
        {
            screenCorners[i] = Camera.main.WorldToScreenPoint(corners[i]);
        }
    }
}
