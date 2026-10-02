using UnityEngine;
using UnityEngine.UI;

public class VolumeManeger : MonoBehaviour
{
    [SerializeField] Slider volumeSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PlayerPrefs.HasKey("musicVolume"))
        {
            PlayerPrefs.SetFloat("musicVolume", 1);
            load();
        }
        else
        {
            load();
        }
    }

    public void ChangeVolume () 
    {
        AudioListener.volume = volumeSlider.value;
        save();
    }
    private void save()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("musicVolume");
    }
    private void load()
    {
        PlayerPrefs.SetFloat("musicVolume", volumeSlider.value);
    }
}
