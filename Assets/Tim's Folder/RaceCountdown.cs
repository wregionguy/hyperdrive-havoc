
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RaceCountdown : MonoBehaviour
{
    [Header("Countdown Settings")]
    [Min(0f)]
    public float countdownDuration = 3f; // Pas hier de totale countdownduur aan.

    [Min(0.1f)]
    public float secondsPerNumber = 1f; // Hoe lang elk getal zichtbaar blijft.

    public string goText = "GO!";

    public bool showGoText = false; // Wordt true nadat de countdown voorbij is.

    [Header("Ready Button")]
    public Button readyButton; // De knop waarmee de speler klaar aangeeft.

    [Header("Countdown UI")]
    public TextMeshProUGUI countdownText;
    public Color countdownColor = Color.white;
    public Color goColor = Color.green;
    public int fontSize = 100;

    [Header("Race Scripts")]
    public RaceManager raceManager;
    public PlayerRaceController player;

    private bool raceStarted;
    private bool countdownStarted;

    public bool RaceStarted
    {
        get { return raceStarted; }
    }

    private void Awake()
    {
        // Zoekt automatisch de RaceManager.
        if (raceManager == null)
        {
            raceManager = FindFirstObjectByType<RaceManager>();
        }

        // Zoekt automatisch de Player.
        if (player == null && raceManager != null)
        {
            player = raceManager.player;
        }

        // Zet de player tijdelijk uit tijdens het wachten.
        if (player != null)
        {
            player.enabled = false;
        }

        // Zoekt automatisch alle opponents met de tag "Opponent".
        SetOpponentsEnabled(false);

        // Zet de countdown tekst klaar.
        if (countdownText != null)
        {
            countdownText.fontSize = fontSize;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.text = "";
            countdownText.gameObject.SetActive(false);
        }

        // Verbindt automatisch de Ready-knop.
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonPressed);
        }
    }

    private void OnDestroy()
    {
        // Voorkomt dat de listener blijft bestaan.
        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(OnReadyButtonPressed);
        }
    }

    public void OnReadyButtonPressed()
    {
        // Voorkomt dat meerdere klikken de countdown opnieuw starten.
        if (countdownStarted || raceStarted)
            return;

        countdownStarted = true;

        // Verberg en blokkeer de knop na de eerste klik.
        if (readyButton != null)
        {
            readyButton.interactable = false;
            readyButton.gameObject.SetActive(false);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        float remainingTime = countdownDuration;

        // GO is nog niet actief.
        showGoText = false;

        while (remainingTime > 0f)
        {
            int number = Mathf.CeilToInt(remainingTime);

            ShowText(number.ToString(), countdownColor);

            yield return new WaitForSeconds(secondsPerNumber);

            remainingTime -= secondsPerNumber;
        }

        // De countdown is voorbij.
        showGoText = true;

        ShowText(goText, goColor);

        // Laat GO kort zien.
        yield return new WaitForSeconds(0.5f);

        // Start daarna de race.
        StartRace();
    }

    private void StartRace()
    {
        if (raceStarted)
            return;

        raceStarted = true;

        // Activeert alle opponents met de tag "Opponent".
        SetOpponentsEnabled(true);

        // Start de speler.
        if (player != null)
        {
            player.enabled = true;
        }

        // Verberg de countdown.
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void SetOpponentsEnabled(bool value)
    {
        // Zoek automatisch alle opponents met de tag "Opponent".
        GameObject[] opponentObjects =
            GameObject.FindGameObjectsWithTag("Opponent");

        foreach (GameObject opponentObject in opponentObjects)
        {
            SpaceShipAI ai = opponentObject.GetComponent<SpaceShipAI>();

            if (ai != null)
            {
                ai.enabled = value;
            }
        }
    }

    private void ShowText(string message, Color color)
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(true);
        countdownText.text = message;
        countdownText.color = color;
    }
}