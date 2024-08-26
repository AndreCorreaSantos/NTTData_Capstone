using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class setColors : MonoBehaviour
{
    public Image Background;
    public Color BackgroundColor;
    public Color TextColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
// run in editor
    [ExecuteInEditMode]
    void Update()
    {
        Background.color = BackgroundColor;
        setColorsRecursive(transform);
    }

    // traverse childs and get textMeshPro components
    void setColorsRecursive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.GetComponent<TextMeshProUGUI>() != null)
            {
                Debug.Log("teste");
                child.GetComponent<TextMeshProUGUI>().color = TextColor;
            }
            setColorsRecursive(child);
        }
    }

}