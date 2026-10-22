
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

public class MainMenu : MonoBehaviour
{
    public void BackToStart()
    {
        SceneManager.LoadScene("Scene Justin");
        Time.timeScale = 1;
    }
    public void PlayScene1()
    {
        SceneManager.LoadScene("Tim's scene");
    }
    public void PlayScene2()
    {
        SceneManager.LoadScene("baan 2");
    }
    public void PlayScene3()
    {
        SceneManager.LoadScene("bbaan 3");
    }
    public void Quit()
    {
        Application.Quit();
    }
    public void TimeFreeze()
    {
        Time.timeScale = 0;
    }
    public void TimeContinue()
    {
        Time.timeScale = 1;
    }
}