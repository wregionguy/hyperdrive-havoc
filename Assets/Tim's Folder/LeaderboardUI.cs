
using UnityEngine;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Race Manager")]
    public RaceManager raceManager;

    [Header("UI")]
    public RectTransform leaderboardPanel;

    [Header("Row Settings")]
    public float rowHeight = 60f;
    public float rowSpacing = 2f;
    public int fontSize = 24;

    [Header("Title Settings")]
    public string titleText = "LEADERBOARD";
    public int titleFontSize = 30;

    [Header("Font Settings")]
    public TMP_FontAsset leaderboardFont;

    [Header("Appearance")]
    public Color normalTextColor = Color.white;
    public Color firstPlaceColor = Color.yellow;
    public Color secondPlaceColor = Color.white;
    public Color thirdPlaceColor = new Color(1f, 0.6f, 0.2f);

    [Header("Player Appearance")]
    public Color playerTextColor = Color.cyan;

    [Header("Finished Appearance")]
    public Color finishedNameColor = Color.green;

    [Header("Center Leaderboard After Player Finish")]
    public bool centerAfterPlayerFinishes = true;

    [Tooltip("Extra positieverschuiving vanaf het midden van de Canvas.")]
    public Vector2 finishedLeaderboardPosition = Vector2.zero;

    [Tooltip("Schaal van het leaderboard nadat de speler is gefinisht. 1.15 = 15% groter.")]
    [Range(1f, 2f)]
    public float finishedLeaderboardScale = 1.15f;

    private GameObject titleObject;
    private bool playerHasFinished;

    private Vector3 originalScale;

    private void Start()
    {
        if (raceManager == null)
        {
            raceManager = FindFirstObjectByType<RaceManager>();
        }

        if (leaderboardPanel == null)
        {
            Debug.LogError(
                "LeaderboardUI: Leaderboard Panel is niet ingesteld!"
            );
            return;
        }

        // Bewaar de oorspronkelijke schaal.
        originalScale = leaderboardPanel.localScale;

        CreateTitle();
    }

    private void Update()
    {
        if (raceManager == null || leaderboardPanel == null)
            return;

        UpdateLeaderboard();
        CheckPlayerFinished();
    }

    private void CheckPlayerFinished()
    {
        if (playerHasFinished || !centerAfterPlayerFinishes)
            return;

        int count = raceManager.GetRacerCount();

        for (int i = 1; i <= count; i++)
        {
            bool isPlayer =
                raceManager.IsPlayerAtPositionInLeaderboard(i);

            bool isFinished =
                raceManager.IsPositionFinished(i);

            if (isPlayer && isFinished)
            {
                playerHasFinished = true;
                CenterLeaderboard();
                return;
            }
        }
    }

    private void CenterLeaderboard()
    {
        if (leaderboardPanel == null)
            return;

        // Zoek de Canvas waar het leaderboard onder valt.
        Canvas canvas = leaderboardPanel.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning(
                "LeaderboardUI: Geen Canvas gevonden voor het leaderboard."
            );
            return;
        }

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        // Bereken het midden van de Canvas in wereldcoördinaten.
        Vector3 canvasCenter = canvasRect.TransformPoint(
            canvasRect.rect.center
        );

        // Verplaats alleen het panel; verander geen anchors of pivot.
        Vector3 targetWorldPosition = canvasCenter;

        // Behoud de oorspronkelijke diepte van het panel.
        targetWorldPosition.z = leaderboardPanel.position.z;

        leaderboardPanel.position = targetWorldPosition;

        // Extra verschuiving instelbaar vanuit de Inspector.
        Vector3 offset = new Vector3(
            finishedLeaderboardPosition.x,
            finishedLeaderboardPosition.y,
            0f
        );

        leaderboardPanel.position += offset;

        // Vergroot het leaderboard na de finish.
        leaderboardPanel.localScale =
            originalScale * finishedLeaderboardScale;

        Debug.Log(
            "LeaderboardUI: Leaderboard naar het midden verplaatst en vergroot."
        );
    }

    private void CreateTitle()
    {
        GameObject obj = new GameObject("Leaderboard Title");
        obj.transform.SetParent(leaderboardPanel, false);

        titleObject = obj;

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, rowHeight);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();

        if (leaderboardFont != null)
            text.font = leaderboardFont;

        text.text = titleText;
        text.fontSize = titleFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = normalTextColor;
    }

    private void UpdateLeaderboard()
    {
        ClearRows();

        int count = raceManager.GetRacerCount();

        for (int i = 1; i <= count; i++)
        {
            string racerName = raceManager.GetRacerName(i);

            if (string.IsNullOrEmpty(racerName))
                continue;

            bool isFinished = raceManager.IsPositionFinished(i);

            bool isPlayer =
                raceManager.IsPlayerAtPositionInLeaderboard(i);

            CreateRow(racerName, i, isFinished, isPlayer);
        }
    }

    private void CreateRow(
        string racerName,
        int position,
        bool isFinished,
        bool isPlayer
    )
    {
        GameObject row = new GameObject(
            "Leaderboard Row " + position
        );

        row.transform.SetParent(leaderboardPanel, false);

        RectTransform rect = row.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        float y = -(rowHeight + rowSpacing) * position;

        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(0f, rowHeight);

        // ==========================================
        // NAAM
        // ==========================================

        TextMeshProUGUI text = row.AddComponent<TextMeshProUGUI>();

        if (leaderboardFont != null)
            text.font = leaderboardFont;

        text.text = position + "   " + racerName;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Left;
        text.verticalAlignment = VerticalAlignmentOptions.Top;

        // ==========================================
        // KLEUR VAN DE NAAM
        // ==========================================

        if (isFinished)
        {
            text.color = finishedNameColor;
        }
        else if (isPlayer)
        {
            text.color = playerTextColor;
        }
        else
        {
            text.color = GetPositionColor(position);
        }
    }

    private Color GetPositionColor(int position)
    {
        if (position == 1)
            return firstPlaceColor;

        if (position == 2)
            return secondPlaceColor;

        if (position == 3)
            return thirdPlaceColor;

        return normalTextColor;
    }

    private void ClearRows()
    {
        for (int i = leaderboardPanel.childCount - 1; i >= 0; i--)
        {
            Transform child = leaderboardPanel.GetChild(i);

            if (child.gameObject == titleObject)
                continue;

            Destroy(child.gameObject);
        }
    }
}