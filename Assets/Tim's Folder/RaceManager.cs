using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [Header("Race Settings")]
    public bool findOpponentsAutomatically = true; // Zoekt automatisch alle opponents.
    public int totalLaps = 3; // Bepaalt hoeveel rondes de race heeft.

    [Header("Player")]
    public PlayerRaceController player; // De speler van de race.

    [Header("Leaderboard")]
    public List<SpaceShipAI> racers =
        new List<SpaceShipAI>(); // Lijst met alle AI racers.

    // Deze lijst bevat de racers in de exacte volgorde
    // waarin ze de officiële finish hebben bereikt.
    private List<SpaceShipAI> finishedRacers =
        new List<SpaceShipAI>();

    // De speler heeft geen SpaceShipAI, dus hiervoor
    // houden we apart bij of hij al gefinisht is.
    private bool playerFinishedAdded;

    // De positie waarop de speler is gefinisht.
    private int playerFinishPosition;

    private void Start()
    {
        if (player == null)
        {
            player =
                FindFirstObjectByType<PlayerRaceController>();
        }

        if (findOpponentsAutomatically)
        {
            FindRacers(); // Zoekt alle opponents.
        }

        SetRaceLaps(); // Geeft het aantal rondes aan alle racers.

        if (player != null)
        {
            player.totalLaps =
                totalLaps; // Geeft het aantal rondes aan de player.
        }
    }

    private void Update()
    {
        UpdateFinishedRacers();

        // Alleen racers die nog NIET gefinisht zijn
        // worden opnieuw gesorteerd.
        SortRacers();
    }

    public void FindRacers()
    {
        racers.Clear(); // Maakt de huidige lijst leeg.

        GameObject[] objects =
            GameObject.FindGameObjectsWithTag(
                "Opponent"
            ); // Zoekt alle objects met de tag Opponent.

        foreach (GameObject obj in objects)
        {
            SpaceShipAI ai =
                obj.GetComponent<SpaceShipAI>();

            if (ai != null)
            {
                racers.Add(ai);
            }
        }

        SortRacers();
    }

    private void SetRaceLaps()
    {
        foreach (SpaceShipAI racer in racers)
        {
            if (racer != null)
            {
                racer.totalLaps =
                    totalLaps;
            }
        }
    }

    private void UpdateFinishedRacers()
    {
        // ==========================================
        // AI FINISHES
        // ==========================================

        foreach (SpaceShipAI racer in racers)
        {
            if (racer == null)
                continue;

            if (!racer.HasFinished())
                continue;

            // Als hij al in deze lijst staat,
            // mag zijn finishpositie NOOIT meer veranderen.
            if (finishedRacers.Contains(racer))
                continue;

            finishedRacers.Add(racer);
        }

        // ==========================================
        // PLAYER FINISH
        // ==========================================

        if (
            player != null &&
            player.HasFinished() &&
            !playerFinishedAdded
        )
        {
            playerFinishedAdded = true;

            // Bereken zijn positie precies op het moment
            // dat hij finisht.
            playerFinishPosition =
                CalculatePlayerFinishPosition();
        }
    }

    private int CalculatePlayerFinishPosition()
    {
        int position = 1;

        float playerProgress =
            player.GetRaceProgress();

        // Alle AI die op het moment van finish
        // verder zijn, staan voor de speler.
        foreach (SpaceShipAI racer in racers)
        {
            if (racer == null)
                continue;

            if (
                racer.HasFinished() &&
                finishedRacers.Contains(racer)
            )
            {
                float racerProgress =
                    racer.GetPublicRaceProgress();

                if (
                    racerProgress >
                    playerProgress
                )
                {
                    position++;
                }
            }
            else if (
                racer.GetPublicRaceProgress() >
                playerProgress
            )
            {
                position++;
            }
        }

        return position;
    }

    public void SortRacers()
    {
        racers.RemoveAll(
            racer => racer == null
        );

        racers.Sort(
            (a, b) =>
            {
                bool aFinished =
                    finishedRacers.Contains(a);

                bool bFinished =
                    finishedRacers.Contains(b);

                // Gefinishte AI blijven boven
                // niet-gefinishte AI staan.
                if (
                    aFinished &&
                    !bFinished
                )
                {
                    return -1;
                }

                if (
                    !aFinished &&
                    bFinished
                )
                {
                    return 1;
                }

                // Als beide gefinisht zijn,
                // gebruiken we de vaste finishvolgorde.
                if (
                    aFinished &&
                    bFinished
                )
                {
                    int aFinishPosition =
                        finishedRacers.IndexOf(a);

                    int bFinishPosition =
                        finishedRacers.IndexOf(b);

                    return
                        aFinishPosition.CompareTo(
                            bFinishPosition
                        );
                }

                // Alleen actieve racers worden
                // normaal op progress gesorteerd.
                float progressA =
                    a.GetPublicRaceProgress();

                float progressB =
                    b.GetPublicRaceProgress();

                return
                    progressB.CompareTo(
                        progressA
                    );
            }
        );
    }

    public int GetPosition(
        SpaceShipAI racer
    )
    {
        if (racer == null)
            return 0;

        // Als deze AI gefinisht is,
        // is zijn positie permanent.
        if (finishedRacers.Contains(racer))
        {
            return
                GetFinishPosition(racer);
        }

        int position = 1;

        float myProgress =
            racer.GetPublicRaceProgress();

        foreach (SpaceShipAI other in racers)
        {
            if (
                other == null ||
                other == racer
            )
            {
                continue;
            }

            // Gefinishte racers staan voor actieve racers.
            if (finishedRacers.Contains(other))
            {
                position++;
                continue;
            }

            if (
                other.GetPublicRaceProgress() >
                myProgress
            )
            {
                position++;
            }
        }

        // De speler telt mee zolang hij nog actief is.
        if (
            player != null &&
            !player.HasFinished() &&
            player.GetRaceProgress() >
            myProgress
        )
        {
            position++;
        }

        return position;
    }

    public int GetPlayerPosition()
    {
        if (player == null)
            return 0;

        // Na finish altijd dezelfde positie teruggeven.
        if (playerFinishedAdded)
        {
            return playerFinishPosition;
        }

        int position = 1;

        float playerProgress =
            player.GetRaceProgress();

        foreach (SpaceShipAI racer in racers)
        {
            if (racer == null)
                continue;

            if (
                racer.GetPublicRaceProgress() >
                playerProgress
            )
            {
                position++;
            }
        }

        return position;
    }

    public SpaceShipAI GetRacer(
        int position
    )
    {
        SortRacers();

        List<RacerEntry> entries =
            GetSortedEntries();

        if (
            position < 1 ||
            position > entries.Count
        )
        {
            return null;
        }

        RacerEntry entry =
            entries[position - 1];

        return entry.ai;
    }

    public int GetRacerCount()
    {
        return
            racers.Count +
            (player != null ? 1 : 0);
    }

    public int GetRacerLap(
        SpaceShipAI racer
    )
    {
        if (racer == null)
            return 0;

        return racer.GetCurrentLap();
    }

    public int GetPlayerLap()
    {
        if (player == null)
            return 0;

        return player.GetCurrentLap();
    }

    public float GetProgress(
        SpaceShipAI racer
    )
    {
        if (racer == null)
            return 0f;

        return
            racer.GetPublicRaceProgress();
    }

    public bool IsPlayerAtPosition(
        int position
    )
    {
        return
            GetPlayerPosition() ==
            position;
    }

    public string GetRacerName(
        int position
    )
    {
        List<RacerEntry> entries =
            GetSortedEntries();

        if (
            position < 1 ||
            position > entries.Count
        )
        {
            return "";
        }

        return
            entries[position - 1].name;
    }

    // Geeft terug of de racer op deze positie
    // de speler is.
    public bool IsPlayerAtPositionInLeaderboard(
        int position
    )
    {
        List<RacerEntry> entries =
            GetSortedEntries();

        if (
            position < 1 ||
            position > entries.Count
        )
        {
            return false;
        }

        return
            entries[position - 1].isPlayer;
    }

    // Geeft terug of de racer op deze positie
    // officieel gefinisht is.
    public bool IsPositionFinished(
        int position
    )
    {
        List<RacerEntry> entries =
            GetSortedEntries();

        if (
            position < 1 ||
            position > entries.Count
        )
        {
            return false;
        }

        return
            entries[position - 1].finished;
    }

    public bool IsRaceFinished()
    {
        return
            player != null &&
            player.HasFinished() &&
            finishedRacers.Count >= racers.Count;
    }

    public SpaceShipAI GetWinner()
    {
        if (finishedRacers.Count == 0)
            return null;

        return finishedRacers[0];
    }

    public int GetFinishPosition(
        SpaceShipAI racer
    )
    {
        if (racer == null)
            return 0;

        int index =
            finishedRacers.IndexOf(
                racer
            );

        if (index == -1)
            return 0;

        return index + 1;
    }

    private List<RacerEntry>
        GetSortedEntries()
    {
        List<RacerEntry> entries =
            new List<RacerEntry>();

        // ==========================================
        // AI
        // ==========================================

        foreach (SpaceShipAI racer in racers)
        {
            if (racer != null)
            {
                entries.Add(
                    new RacerEntry
                    {
                        name =
                            racer.gameObject.name,

                        progress =
                            racer.GetPublicRaceProgress(),

                        finished =
                            finishedRacers.Contains(
                                racer
                            ),

                        finishOrder =
                            finishedRacers.IndexOf(
                                racer
                            ),

                        ai = racer,

                        isPlayer = false
                    }
                );
            }
        }

        // ==========================================
        // PLAYER
        // ==========================================

        if (player != null)
        {
            entries.Add(
                new RacerEntry
                {
                    name =
                        player.gameObject.name,

                    progress =
                        player.GetRaceProgress(),

                    finished =
                        player.HasFinished(),

                    finishOrder =
                        playerFinishedAdded
                        ? playerFinishPosition - 1
                        : -1,

                    ai = null,

                    isPlayer = true
                }
            );
        }

        // ==========================================
        // SORTEREN
        // ==========================================

        entries.Sort(
            (a, b) =>
            {
                // Als beide gefinisht zijn,
                // gebruiken we de vaste finishvolgorde.
                if (
                    a.finished &&
                    b.finished
                )
                {
                    return
                        a.finishOrder.CompareTo(
                            b.finishOrder
                        );
                }

                if (
                    a.finished &&
                    !b.finished
                )
                {
                    return -1;
                }

                if (
                    !a.finished &&
                    b.finished
                )
                {
                    return 1;
                }

                // Nog actieve racers worden
                // op race progress gesorteerd.
                return
                    b.progress.CompareTo(
                        a.progress
                    );
            }
        );

        return entries;
    }

    private class RacerEntry
    {
        public string name;
        public float progress;
        public bool finished;
        public int finishOrder;

        public SpaceShipAI ai;
        public bool isPlayer;
    }
}