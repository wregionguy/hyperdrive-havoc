
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
 
public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("Tim's scene");
    }

    public void Quit()
    {
        Application.Quit();
    }
}