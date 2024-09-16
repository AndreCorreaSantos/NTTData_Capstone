using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[ExecuteInEditMode]
public class setColors : MonoBehaviour
{
    public Image Background;
    private List<TextMeshProUGUI> textObjects = new List<TextMeshProUGUI>();

    void Start()
    {
        FindTextObjects(transform);
    }
    void FindTextObjects(Transform parent)
    {

        foreach (Transform child in parent)
        {
            if (child.GetComponent<TextMeshProUGUI>() != null)
            {
                textObjects.Add(child.GetComponent<TextMeshProUGUI>());
            }
            FindTextObjects(child);
        }
    }
    public void SetColor(Color BackgroundColor, Color TextColor)
    {
        Background.color = BackgroundColor;
        foreach (TextMeshProUGUI text in textObjects)
        {
            text.color = TextColor;
        }
    }

}
