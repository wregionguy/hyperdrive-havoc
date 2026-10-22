
using UnityEngine;

public class Pauzemenu : MonoBehaviour
{
    public MainMenu mainMenuscript;
    public GameObject menu;
    public GameObject IngameCanvas;
    public GameObject SettingsMenu;

    public bool menuActive = false;

    private void Start()
    {
        // Zorg dat de menu-status overeenkomt met de Inspector.
        if (menu != null)
            menu.SetActive(menuActive);

        if (IngameCanvas != null)
            IngameCanvas.SetActive(!menuActive);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // Als het settingsmenu open is, doet Escape niets.
        if (SettingsMenu != null && SettingsMenu.activeSelf)
            return;

        if (menuActive)
        {
            StartTime();
        }
        else
        {
            Stop();
        }
    }

    public void StartTime()
    {
        if (!menuActive)
            return;

        // Voorkom dat het pauzemenu sluit als Settings openstaat.
        if (SettingsMenu != null && SettingsMenu.activeSelf)
            return;

        if (mainMenuscript != null)
        {
            mainMenuscript.TimeContinue();
        }
        else
        {
            Debug.LogError("Pauzemenu: MainMenu is niet gekoppeld!");
        }

        if (menu != null)
            menu.SetActive(false);

        if (IngameCanvas != null)
            IngameCanvas.SetActive(true);

        menuActive = false;
    }

    private void Stop()
    {
        if (menuActive)
            return;

        // Open het pauzemenu niet via Escape als Settings al openstaat.
        if (SettingsMenu != null && SettingsMenu.activeSelf)
            return;

        if (mainMenuscript != null)
        {
            mainMenuscript.TimeFreeze();
        }
        else
        {
            Debug.LogError("Pauzemenu: MainMenu is niet gekoppeld!");
        }

        if (menu != null)
            menu.SetActive(true);

        if (IngameCanvas != null)
            IngameCanvas.SetActive(false);

        menuActive = true;
    }
}