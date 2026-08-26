using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Race Manager")]
    public RaceManager raceManager;

    [Header("UI")]
    public RectTransform leaderboardPanel;

    [Header("Row Settings")]
    public float rowHeight = 45f;
    public float rowSpacing = 2f;
    public int fontSize = 24;

    [Header("Appearance")]
    public Color normalTextColor = Color.white;
    public Color firstPlaceColor = Color.yellow;
    public Color secondPlaceColor = Color.white;
    public Color thirdPlaceColor = new Color(1f, 0.6f, 0.2f);

    private GameObject titleObject;

    private void Start()
    {
        if (raceManager == null)
        {
            raceManager = FindFirstObjectByType<RaceManager>();
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
        GameObject obj = new GameObject("Leaderboard Title");

        obj.transform.SetParent(leaderboardPanel, false);

        titleObject = obj;

        RectTransform rect = obj.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(0f, rowHeight);

        TextMeshProUGUI text =
            obj.AddComponent<TextMeshProUGUI>();

        text.fontSize = fontSize + 6;
        text.alignment = TextAlignmentOptions.Center;
        text.color = normalTextColor;
    }

    private void UpdateLeaderboard()
    {
        ClearRows();

        int count = raceManager.GetRacerCount();

        for (int i = 0; i < count; i++)
        {
            SpaceShipAI racer =
                raceManager.GetRacer(i + 1);

            if (racer == null)
                continue;

            CreateRow(racer, i + 1);
        }
    }

    private void CreateRow(
        SpaceShipAI racer,
        int position)
    {
        GameObject row =
            new GameObject("Leaderboard Row " + position);

        row.transform.SetParent(
            leaderboardPanel,
            false
        );

        RectTransform rect =
            row.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        float y =
            -(rowHeight + rowSpacing) *
            position;

        rect.anchoredPosition =
            new Vector2(0f, y);

        rect.sizeDelta =
            new Vector2(0f, rowHeight);

        TextMeshProUGUI text =
            row.AddComponent<TextMeshProUGUI>();

        string racerName =
            racer.gameObject.name;

        text.text =
            position + "   " + racerName;

        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Left;
        text.verticalAlignment =
            VerticalAlignmentOptions.Middle;

        text.color =
            GetPositionColor(position);
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
        for (
            int i = leaderboardPanel.childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                leaderboardPanel.GetChild(i);

            if (child.gameObject == titleObject)
                continue;

            Destroy(child.gameObject);
        }
    }
}