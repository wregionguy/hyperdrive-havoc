using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [Header("Race Settings")]
    public bool findOpponentsAutomatically = true; // Zoekt automatisch alle opponents.
    public int totalLaps = 3; // Bepaalt hoeveel normale rondes de race heeft.

    [Header("Player")]
    public PlayerRaceController player; // De speler van de race.

    [Header("Leaderboard")]
    public List<SpaceShipAI> racers =
        new List<SpaceShipAI>(); // Lijst met alle AI racers.

    // Deze lijst bevat ALLE racers in de volgorde waarin ze
    // hun race definitief hebben voltooid.
    private List<object> finishedRacers =
        new List<object>();

    private bool playerFinishedAdded;

    private void Start()
    {
        if (player == null)
        {
            player =
                FindFirstObjectByType<PlayerRaceController>();
        }

        if (findOpponentsAutomatically)
        {
            FindRacers();
        }

        SetRaceLaps();

        if (player != null)
        {
            player.totalLaps =
                totalLaps;
        }
    }

    private void Update()
    {
        UpdateFinishedRacers();

        // Zolang de race nog bezig is,
        // worden de niet-gefinishte racers op progress gesorteerd.
        SortRacers();
    }

    // ============================================================
    // FIND RACERS
    // ============================================================

    public void FindRacers()
    {
        racers.Clear();

        GameObject[] objects =
            GameObject.FindGameObjectsWithTag(
                "Opponent"
            );

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

    // ============================================================
    // FINISH REGISTRATIE
    // ============================================================

    private void UpdateFinishedRacers()
    {
        // --------------------------------------------------------
        // AI
        // --------------------------------------------------------

        foreach (SpaceShipAI racer in racers)
        {
            if (racer == null)
                continue;

            if (!racer.HasFinished())
                continue;

            if (finishedRacers.Contains(racer))
                continue;

            // Deze racer wordt NU definitief geregistreerd.
            finishedRacers.Add(racer);
        }

        // --------------------------------------------------------
        // PLAYER
        // --------------------------------------------------------

        if (player != null &&
            player.HasFinished() &&
            !playerFinishedAdded)
        {
            playerFinishedAdded = true;

            // De player wordt toegevoegd op het moment
            // dat hij zijn laatste normale lap heeft voltooid.
            finishedRacers.Add(player);
        }
    }

    // ============================================================
    // LEADERBOARD SORTING
    // ============================================================

    public void SortRacers()
    {
        racers.RemoveAll(
            racer => racer == null
        );

        racers.Sort((a, b) =>
        {
            bool aFinished =
                finishedRacers.Contains(a);

            bool bFinished =
                finishedRacers.Contains(b);

            // Een gefinishte racer blijft altijd boven
            // een racer die nog rijdt.
            if (aFinished && !bFinished)
                return -1;

            if (!aFinished && bFinished)
                return 1;

            // Als beide gefinisht zijn:
            // gebruik de echte finishvolgorde.
            if (aFinished && bFinished)
            {
                int aIndex =
                    finishedRacers.IndexOf(a);

                int bIndex =
                    finishedRacers.IndexOf(b);

                return
                    aIndex.CompareTo(bIndex);
            }

            // Beide rijden nog:
            // sorteer op race progress.
            float progressA =
                a.GetPublicRaceProgress();

            float progressB =
                b.GetPublicRaceProgress();

            return
                progressB.CompareTo(progressA);
        });
    }

    // ============================================================
    // POSITIONS
    // ============================================================

    public int GetPosition(
        SpaceShipAI racer)
    {
        if (racer == null)
            return 0;

        // Als deze racer al definitief gefinisht is,
        // is zijn positie permanent.
        int finishedIndex =
            finishedRacers.IndexOf(racer);

        if (finishedIndex >= 0)
        {
            return finishedIndex + 1;
        }

        int position = 1;

        float myProgress =
            racer.GetPublicRaceProgress();

        foreach (SpaceShipAI other in racers)
        {
            if (other == null ||
                other == racer)
                continue;

            // Gefinishte AI's staan al voor deze racer.
            if (finishedRacers.Contains(other))
            {
                position++;
                continue;
            }

            if (other.GetPublicRaceProgress() >
                myProgress)
            {
                position++;
            }
        }

        // Player die nog rijdt kan ook voor de AI staan.
        if (player != null &&
            !player.HasFinished() &&
            player.GetRaceProgress() >
            myProgress)
        {
            position++;
        }

        return position;
    }

    public int GetPlayerPosition()
    {
        if (player == null)
            return 0;

        // BELANGRIJK:
        // Als de player gefinisht is, blijft deze positie vast.
        int finishedIndex =
            finishedRacers.IndexOf(player);

        if (finishedIndex >= 0)
        {
            return finishedIndex + 1;
        }

        int position = 1;

        float playerProgress =
            player.GetRaceProgress();

        foreach (SpaceShipAI racer in racers)
        {
            if (racer == null)
                continue;

            if (finishedRacers.Contains(racer))
            {
                position++;
                continue;
            }

            if (racer.GetPublicRaceProgress() >
                playerProgress)
            {
                position++;
            }
        }

        return position;
    }

    // ============================================================
    // GET RACER
    // ============================================================

    public SpaceShipAI GetRacer(
        int position)
    {
        SortRacers();

        foreach (SpaceShipAI racer in racers)
        {
            if (GetPosition(racer) ==
                position)
            {
                return racer;
            }
        }

        return null;
    }

    public int GetRacerCount()
    {
        return racers.Count +
            (player != null ? 1 : 0);
    }

    // ============================================================
    // LAPS
    // ============================================================

    public int GetRacerLap(
        SpaceShipAI racer)
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

    // ============================================================
    // PROGRESS
    // ============================================================

    public float GetProgress(
        SpaceShipAI racer)
    {
        if (racer == null)
            return 0f;

        return racer.GetPublicRaceProgress();
    }

    public bool IsPlayerAtPosition(
        int position)
    {
        return
            GetPlayerPosition() ==
            position;
    }

    // ============================================================
    // LEADERBOARD NAME
    // ============================================================

    public string GetRacerName(
        int position)
    {
        List<RacerEntry> entries =
            GetSortedEntries();

        if (position < 1 ||
            position > entries.Count)
        {
            return "";
        }

        return
            entries[position - 1].name;
    }

    private List<RacerEntry>
        GetSortedEntries()
    {
        List<RacerEntry> entries =
            new List<RacerEntry>();

        // --------------------------------------------------------
        // AI
        // --------------------------------------------------------

        foreach (SpaceShipAI racer in racers)
        {
            if (racer != null)
            {
                entries.Add(
                    new RacerEntry
                    {
                        racer = racer,
                        name =
                            racer.gameObject.name,
                        progress =
                            racer.GetPublicRaceProgress(),
                        finished =
                            finishedRacers.Contains(
                                racer
                            )
                    }
                );
            }
        }

        // --------------------------------------------------------
        // PLAYER
        // --------------------------------------------------------

        if (player != null)
        {
            entries.Add(
                new RacerEntry
                {
                    player = player,
                    name =
                        player.gameObject.name,
                    progress =
                        player.GetRaceProgress(),
                    finished =
                        finishedRacers.Contains(
                            player
                        )
                }
            );
        }

        // --------------------------------------------------------
        // SORT
        // --------------------------------------------------------

        entries.Sort((a, b) =>
        {
            bool aFinished =
                a.finished;

            bool bFinished =
                b.finished;

            if (aFinished && !bFinished)
                return -1;

            if (!aFinished && bFinished)
                return 1;

            // Als beide gefinisht zijn,
            // gebruik de echte finishvolgorde.
            if (aFinished && bFinished)
            {
                int aIndex =
                    GetFinishedIndex(a);

                int bIndex =
                    GetFinishedIndex(b);

                return
                    aIndex.CompareTo(bIndex);
            }

            // Nog niet gefinisht:
            // hoogste progress eerst.
            return
                b.progress.CompareTo(
                    a.progress
                );
        });

        return entries;
    }

    private int GetFinishedIndex(
        RacerEntry entry)
    {
        if (entry.racer != null)
        {
            return
                finishedRacers.IndexOf(
                    entry.racer
                );
        }

        if (entry.player != null)
        {
            return
                finishedRacers.IndexOf(
                    entry.player
                );
        }

        return int.MaxValue;
    }

    // ============================================================
    // RACE STATUS
    // ============================================================

    public bool IsRaceFinished()
    {
        if (player == null)
            return false;

        return
            player.HasFinished() &&
            finishedRacers.Count >=
            racers.Count + 1;
    }

    public SpaceShipAI GetWinner()
    {
        if (finishedRacers.Count == 0)
            return null;

        if (finishedRacers[0] is SpaceShipAI ai)
            return ai;

        return null;
    }

    public int GetFinishPosition(
        SpaceShipAI racer)
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

    public bool HasPlayerFinished()
    {
        return
            player != null &&
            finishedRacers.Contains(
                player
            );
    }

    public int GetPlayerFinishPosition()
    {
        if (player == null)
            return 0;

        int index =
            finishedRacers.IndexOf(
                player
            );

        if (index == -1)
            return 0;

        return index + 1;
    }

    // ============================================================
    // ENTRY
    // ============================================================

    private class RacerEntry
    {
        public SpaceShipAI racer;
        public PlayerRaceController player;

        public string name;
        public float progress;
        public bool finished;
    }
}