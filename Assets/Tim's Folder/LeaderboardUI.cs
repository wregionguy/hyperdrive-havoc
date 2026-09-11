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

    [Header("Appearance")]
    public Color normalTextColor = Color.white;
    public Color firstPlaceColor = Color.yellow;
    public Color secondPlaceColor = Color.white;
    public Color thirdPlaceColor =
        new Color(1f, 0.6f, 0.2f);

    [Header("Finished")]
    public string finishedText = "FINISHED";
    public Color finishedTextColor = Color.green;

    private GameObject titleObject;

    private void Start()
    {
        if (raceManager == null)
        {
            raceManager =
                FindFirstObjectByType<RaceManager>();
        }

        CreateTitle();
    }

    private void Update()
    {
        if (raceManager == null)
            return;

        UpdateLeaderboard();
    }

    private void CreateTitle()
    {
        GameObject obj =
            new GameObject(
                "Leaderboard Title"
            );

        obj.transform.SetParent(
            leaderboardPanel,
            false
        );

        titleObject = obj;

        RectTransform rect =
            obj.AddComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.anchoredPosition =
            new Vector2(0f, 0f);

        rect.sizeDelta =
            new Vector2(
                0f,
                rowHeight
            );

        TextMeshProUGUI text =
            obj.AddComponent<TextMeshProUGUI>();

        text.text =
            "LEADERBOARD";

        text.fontSize =
            fontSize + 6;

        text.alignment =
            TextAlignmentOptions.Center;

        text.color =
            normalTextColor;
    }

    private void UpdateLeaderboard()
    {
        ClearRows();

        int count =
            raceManager.GetRacerCount();

        for (
            int i = 1;
            i <= count;
            i++
        )
        {
            string racerName =
                raceManager.GetRacerName(i);

            if (
                string.IsNullOrEmpty(
                    racerName
                )
            )
            {
                continue;
            }

            bool isFinished =
                raceManager.IsPositionFinished(
                    i
                );

            bool isPlayer =
                raceManager.IsPlayerAtPositionInLeaderboard(
                    i
                );

            CreateRow(
                racerName,
                i,
                isFinished,
                isPlayer
            );
        }
    }

    private void CreateRow(
        string racerName,
        int position,
        bool isFinished,
        bool isPlayer
    )
    {
        GameObject row =
            new GameObject(
                "Leaderboard Row " +
                position
            );

        row.transform.SetParent(
            leaderboardPanel,
            false
        );

        RectTransform rect =
            row.AddComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        float y =
            -(rowHeight + rowSpacing) *
            position;

        rect.anchoredPosition =
            new Vector2(
                0f,
                y
            );

        rect.sizeDelta =
            new Vector2(
                0f,
                rowHeight
            );

        // ==========================================
        // NAAM
        // ==========================================

        TextMeshProUGUI text =
            row.AddComponent<TextMeshProUGUI>();

        text.text =
            position +
            "   " +
            racerName;

        text.fontSize =
            fontSize;

        text.alignment =
            TextAlignmentOptions.Left;

        text.verticalAlignment =
            VerticalAlignmentOptions.Top;

        text.color =
            GetPositionColor(
                position
            );

        // ==========================================
        // FINISHED TEKST
        // ==========================================

        if (isFinished)
        {
            GameObject finishedObject =
                new GameObject(
                    "Finished Text"
                );

            finishedObject.transform.SetParent(
                row.transform,
                false
            );

            RectTransform finishedRect =
                finishedObject.AddComponent<RectTransform>();

            finishedRect.anchorMin =
                new Vector2(
                    0f,
                    0f
                );

            finishedRect.anchorMax =
                new Vector2(
                    1f,
                    0f
                );

            finishedRect.pivot =
                new Vector2(
                    0.5f,
                    0f
                );

            finishedRect.anchoredPosition =
                new Vector2(
                    0f,
                    0f
                );

            finishedRect.sizeDelta =
                new Vector2(
                    0f,
                    rowHeight * 0.45f
                );

            TextMeshProUGUI finishedTextUI =
                finishedObject.AddComponent<TextMeshProUGUI>();

            finishedTextUI.text =
                finishedText;

            finishedTextUI.fontSize =
                Mathf.Max(
                    12,
                    fontSize - 8
                );

            finishedTextUI.alignment =
                TextAlignmentOptions.Left;

            finishedTextUI.verticalAlignment =
                VerticalAlignmentOptions.Middle;

            finishedTextUI.color =
                finishedTextColor;
        }
    }

    private Color GetPositionColor(
        int position
    )
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
        for (
            int i =
                leaderboardPanel.childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                leaderboardPanel.GetChild(i);

            if (
                child.gameObject ==
                titleObject
            )
            {
                continue;
            }

            Destroy(
                child.gameObject
            );
        }
    }
}