using UnityEngine;

public class RectBehaviour : MonoBehaviour
{
    public RectTransform topLine;
    public RectTransform botLine;
    public RectTransform rLine;
    public RectTransform lLine;

    public void SetRectSize(Rect r)
    {
        topLine.sizeDelta = new Vector2(r.width, topLine.sizeDelta.y);
        topLine.anchoredPosition = new Vector3(r.width / 2, 0, 0);

        botLine.sizeDelta = new Vector2(r.width, botLine.sizeDelta.y);
        botLine.anchoredPosition = new Vector3(r.width / 2, r.height, 0);

        rLine.sizeDelta = new Vector2(rLine.sizeDelta.x, r.height);
        rLine.anchoredPosition = new Vector3(r.width, r.height / 2, 0);

        lLine.sizeDelta = new Vector2(lLine.sizeDelta.x, r.height);
        lLine.anchoredPosition = new Vector3(0, r.height / 2, 0);
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
