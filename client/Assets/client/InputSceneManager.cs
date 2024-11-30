using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class InputSceneManager : MonoBehaviour
{
    public TextMeshProUGUI inputField;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Transition(){
        // Load the next scene
        PlayerPrefs.SetString("ip", "ws://" + inputField.text + ":8000"); // Save the IP address to the PlayerPrefs
        SceneManager.LoadScene("clientScene");
    }
}
