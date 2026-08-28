using UnityEngine;
using TMPro;

public class LapTrackerUI : MonoBehaviour
{
    [Header("Race Manager")]
    public RaceManager raceManager;

    [Header("Player")]
    public PlayerRaceController player;

    [Header("UI")]
    public TextMeshProUGUI lapText;

    private int lastLap = 0;

    private void Start()
    {
        if (raceManager == null)
            raceManager = FindFirstObjectByType<RaceManager>();

        if (player == null)
            player = FindFirstObjectByType<PlayerRaceController>();

        UpdateLapText();
    }

    private void Update()
    {
        if (raceManager == null || player == null)
            return;

        UpdateLapText();
    }

    private void UpdateLapText()
    {
        int currentLap = player.GetCurrentLap();
        int totalLaps = raceManager.totalLaps;

        if (currentLap > totalLaps)
            currentLap = totalLaps;

        lapText.text = "LAP " + currentLap + "/" + totalLaps;

        if (currentLap != lastLap)
        {
            lastLap = currentLap;
        }
    }
}