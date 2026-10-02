using System.Collections;
using UnityEngine;
using TMPro;

public class RaceCountdown : MonoBehaviour
{
    [Header("Countdown Settings")]
    [Min(0f)]
    public float countdownDuration = 3f; // Pas hier de totale countdownduur aan.

    [Min(0.1f)]
    public float secondsPerNumber = 1f; // Hoe lang elk getal zichtbaar blijft.

    public string goText = "GO!";

    public bool showGoText = false; // Wordt automatisch true nadat de countdown voorbij is.

    [Header("Countdown UI")]
    public TextMeshProUGUI countdownText;
    public Color countdownColor = Color.white;
    public Color goColor = Color.green;
    public int fontSize = 100;

    [Header("Race Scripts")]
    public RaceManager raceManager;
    public PlayerRaceController player;

    private bool raceStarted;

    public bool RaceStarted
    {
        get { return raceStarted; }
    }

    private void Awake()
    {
        // Zoekt automatisch de RaceManager.
        if (raceManager == null)
        {
            raceManager =
                FindFirstObjectByType<RaceManager>();
        }

        // Zoekt automatisch de Player.
        if (player == null && raceManager != null)
        {
            player =
                raceManager.player;
        }

        // Zet de player tijdelijk uit tijdens de countdown.
        if (player != null)
        {
            player.enabled = false;
        }

        // Zoekt automatisch alle opponents met de tag "Opponent".
        GameObject[] opponentObjects =
            GameObject.FindGameObjectsWithTag(
                "Opponent"
            );

        foreach (GameObject opponentObject in opponentObjects)
        {
            SpaceShipAI ai =
                opponentObject.GetComponent<SpaceShipAI>();

            if (ai != null)
            {
                ai.enabled = false;
            }
        }

        // Zet de countdown tekst klaar.
        if (countdownText != null)
        {
            countdownText.fontSize =
                fontSize;

            countdownText.alignment =
                TextAlignmentOptions.Center;

            countdownText.gameObject.SetActive(true);

            countdownText.text = "";
        }
    }

    private void Start()
    {
        StartCoroutine(
            CountdownRoutine()
        );
    }

    private IEnumerator CountdownRoutine()
    {
        float remainingTime =
            countdownDuration;

        // Zorg ervoor dat GO nog niet zichtbaar is.
        showGoText = false;

        while (remainingTime > 0f)
        {
            int number =
                Mathf.CeilToInt(
                    remainingTime
                );

            ShowText(
                number.ToString(),
                countdownColor
            );

            yield return new WaitForSeconds(
                secondsPerNumber
            );

            remainingTime -=
                secondsPerNumber;
        }

        // De countdown is voorbij.
        // Vanaf hier wordt showGoText automatisch true.
        showGoText = true;

        // Laat GO! zien.
        if (showGoText)
        {
            ShowText(
                goText,
                goColor
            );

            yield return new WaitForSeconds(
                0.5f
            );
        }

        // Start daarna de race.
        StartRace();
    }

    private void StartRace()
    {
        if (raceStarted)
            return;

        raceStarted = true;

        // Zoek opnieuw alle opponents met de tag "Opponent".
        // Hierdoor hoef je ze nooit handmatig toe te voegen.
        GameObject[] opponentObjects =
            GameObject.FindGameObjectsWithTag(
                "Opponent"
            );

        foreach (GameObject opponentObject in opponentObjects)
        {
            SpaceShipAI ai =
                opponentObject.GetComponent<SpaceShipAI>();

            if (ai != null)
            {
                ai.enabled = true;
            }
        }

        // Start de player.
        if (player != null)
        {
            player.enabled = true;
        }

        // Verberg de countdown.
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(
                false
            );
        }
    }

    private void ShowText(
        string message,
        Color color
    )
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(
            true
        );

        countdownText.text =
            message;

        countdownText.color =
            color;
    }
}