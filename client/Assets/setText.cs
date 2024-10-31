using UnityEngine;
using TMPro;

public class setText : MonoBehaviour
{

    public string[] stringList;
    public int index = 0;
    public TextMeshProUGUI textMeshPro;

    void Start()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
        textMeshPro.text = stringList[index];
        
    }

    public void NextString(){
        index++;
        if(index >= stringList.Length){
            index = 0;
        }
        textMeshPro.text = stringList[index];
        
    }
}
