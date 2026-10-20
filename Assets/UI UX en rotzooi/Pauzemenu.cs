using UnityEngine;

public class Pauzemenu : MonoBehaviour
{
    public MainMenu mainMenuscript;
    public GameObject menu;
    public bool menuActive = false;
    // Update is called once per frame
    void Update()
    {
        StartTime();
        Stop();
    }
    public void StartTime()
    {
        if (menuActive == true)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                mainMenuscript.TimeContinue();
                Cursor.lockState = CursorLockMode.Locked;
                menu.SetActive(false);
                menuActive = false;

            }
        }
    }
    private void Stop()
    {
        if(menuActive == false)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                mainMenuscript.TimeFreeze();
                Cursor.lockState = CursorLockMode.None;
                menu.SetActive(true);
                menuActive = true;
            }
        }
    }
}
