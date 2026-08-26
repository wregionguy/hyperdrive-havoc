using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    [Header("Race Settings")]
    public bool findOpponentsAutomatically = true;
    public int totalLaps = 3;

    [Header("Leaderboard")]
    public List<SpaceShipAI> racers = new List<SpaceShipAI>();

    private List<SpaceShipAI> finishedRacers =
        new List<SpaceShipAI>();

    private void Start()
    {
        if (findOpponentsAutomatically)
        {
            FindRacers();
        }

        SetRaceLaps();
    }

    private void Update()
    {
        UpdateFinishedRacers();
        SortRacers();
    }

    public void FindRacers()
    {
        racers.Clear();

        GameObject[] objects =
            GameObject.FindGameObjectsWithTag("Opponent");

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
                racer.totalLaps = totalLaps;
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

            finishedRacers.Add(racer);
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

        if (finishedRacers.Contains(racer))
        {
            return finishedRacers.IndexOf(racer) + 1;
        }

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

            float otherProgress =
                other.GetPublicRaceProgress();

            if (otherProgress > myProgress)
            {
                position++;
            }
        }

        return position;
    }

    public SpaceShipAI GetRacer(int position)
    {
        if (position < 1 ||
            position > racers.Count)
        {
            return null;
        }

        SortRacers();

        return racers[position - 1];
    }

    public int GetRacerCount()
    {
        return racers.Count;
    }

    public int GetRacerLap(SpaceShipAI racer)
    {
        if (racer == null)
            return 0;

        return racer.GetCurrentLap();
    }

    public bool IsRaceFinished()
    {
        return finishedRacers.Count >= racers.Count &&
               racers.Count > 0;
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
}