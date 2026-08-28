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
    public List<SpaceShipAI> racers = new List<SpaceShipAI>(); // Lijst met alle AI racers.

    private List<SpaceShipAI> finishedRacers =
        new List<SpaceShipAI>(); // Lijst met AI racers in finishvolgorde.

    private bool playerFinishedAdded; // Houdt bij of de player als gefinisht geregistreerd is.

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
            player.totalLaps = totalLaps; // Geeft het aantal rondes aan de player.
    }

    private void Update()
    {
        UpdateFinishedRacers(); // Controleert welke racers klaar zijn.
        SortRacers(); // Sorteert de racers op racepositie.
    }

    public void FindRacers()
    {
        racers.Clear(); // Maakt de huidige lijst leeg.

        GameObject[] objects =
            GameObject.FindGameObjectsWithTag("Opponent"); // Zoekt alle objects met de tag Opponent.

        foreach (GameObject obj in objects)
        {
            SpaceShipAI ai =
                obj.GetComponent<SpaceShipAI>(); // Zoekt het SpaceShipAI script.

            if (ai != null)
            {
                racers.Add(ai); // Voegt de racer toe aan de lijst.
            }
        }

        SortRacers(); // Sorteert de racers.
    }

    private void SetRaceLaps()
    {
        foreach (SpaceShipAI racer in racers)
        {
            if (racer != null)
            {
                racer.totalLaps = totalLaps; // Geeft het aantal rondes aan de racer.
            }
        }
    }

    private void UpdateFinishedRacers()
    {
        foreach (SpaceShipAI racer in racers)
        {
            if (racer == null)
                continue;

            if (!racer.HasFinished())
                continue;

            if (finishedRacers.Contains(racer))
                continue;

            finishedRacers.Add(racer); // Voegt de racer toe in finishvolgorde.
        }

        if (player != null &&
            player.HasFinished() &&
            !playerFinishedAdded)
        {
            playerFinishedAdded = true;
        }
    }

    public void SortRacers()
    {
        racers.RemoveAll(racer => racer == null);

        racers.Sort((a, b) =>
        {
            bool aFinished = finishedRacers.Contains(a);
            bool bFinished = finishedRacers.Contains(b);

            if (aFinished && !bFinished)
                return -1;

            if (!aFinished && bFinished)
                return 1;

            if (aFinished && bFinished)
            {
                int aFinishPosition =
                    finishedRacers.IndexOf(a);

                int bFinishPosition =
                    finishedRacers.IndexOf(b);

                return aFinishPosition.CompareTo(
                    bFinishPosition
                );
            }

            float progressA =
                a.GetPublicRaceProgress();

            float progressB =
                b.GetPublicRaceProgress();

            return progressB.CompareTo(progressA);
        });
    }

    public int GetPosition(SpaceShipAI racer)
    {
        if (racer == null)
            return 0;

        int position = 1;

        float myProgress =
            racer.GetPublicRaceProgress();

        foreach (SpaceShipAI other in racers)
        {
            if (other == null || other == racer)
                continue;

            if (finishedRacers.Contains(other))
            {
                position++;
                continue;
            }

            if (other.GetPublicRaceProgress() > myProgress)
            {
                position++;
            }
        }

        if (player != null &&
            !player.HasFinished() &&
            player.GetRaceProgress() > myProgress)
        {
            position++;
        }

        return position;
    }

    public int GetPlayerPosition()
    {
        if (player == null)
            return 0;

        int position = 1;

        float playerProgress =
            player.GetRaceProgress();

        foreach (SpaceShipAI racer in racers)
        {
            if (racer == null)
                continue;

            if (finishedRacers.Contains(racer) ||
                racer.GetPublicRaceProgress() > playerProgress)
            {
                position++;
            }
        }

        return position;
    }

    public SpaceShipAI GetRacer(int position)
    {
        SortRacers();

        int aiIndex = 0;

        for (int i = 1; i <= GetRacerCount(); i++)
        {
            if (GetPosition(racers[aiIndex]) == position)
                return racers[aiIndex];

            aiIndex++;

            if (aiIndex >= racers.Count)
                break;
        }

        return null;
    }

    public int GetRacerCount()
    {
        return racers.Count +
               (player != null ? 1 : 0);
    }

    public int GetRacerLap(SpaceShipAI racer)
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

    public float GetProgress(SpaceShipAI racer)
    {
        if (racer == null)
            return 0f;

        return racer.GetPublicRaceProgress();
    }

    public bool IsPlayerAtPosition(int position)
    {
        return GetPlayerPosition() == position;
    }

    public string GetRacerName(int position)
    {
        if (player != null &&
            GetPlayerPosition() == position)
        {
            return player.gameObject.name;
        }

        List<RacerEntry> entries =
            GetSortedEntries();

        if (position < 1 ||
            position > entries.Count)
        {
            return "";
        }

        return entries[position - 1].name;
    }

    private List<RacerEntry> GetSortedEntries()
    {
        List<RacerEntry> entries =
            new List<RacerEntry>();

        foreach (SpaceShipAI racer in racers)
        {
            if (racer != null)
            {
                entries.Add(
                    new RacerEntry
                    {
                        name = racer.gameObject.name,
                        progress = racer.GetPublicRaceProgress(),
                        finished =
                            finishedRacers.Contains(racer)
                    }
                );
            }
        }

        if (player != null)
        {
            entries.Add(
                new RacerEntry
                {
                    name = player.gameObject.name,
                    progress = player.GetRaceProgress(),
                    finished = player.HasFinished()
                }
            );
        }

        entries.Sort((a, b) =>
        {
            if (a.finished && !b.finished)
                return -1;

            if (!a.finished && b.finished)
                return 1;

            return b.progress.CompareTo(a.progress);
        });

        return entries;
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

    public int GetFinishPosition(SpaceShipAI racer)
    {
        if (racer == null)
            return 0;

        int index =
            finishedRacers.IndexOf(racer);

        if (index == -1)
            return 0;

        return index + 1;
    }

    private class RacerEntry
    {
        public string name;
        public float progress;
        public bool finished;
    }
}