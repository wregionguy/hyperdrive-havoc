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
        new List<SpaceShipAI>(); // Lijst met racers in de volgorde waarin ze finishen.

    private void Start()
    {
        if (findOpponentsAutomatically) // Controleert of opponents automatisch gezocht moeten worden.
        {
            FindRacers(); // Zoekt alle opponents.
        }

        SetRaceLaps(); // Geeft het aantal rondes aan alle racers.
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

            if (ai != null) // Controleert of het script bestaat.
            {
                racers.Add(ai); // Voegt de racer toe aan de lijst.
            }
        }

        SortRacers(); // Sorteert de racers.
    }

    private void SetRaceLaps()
    {
        foreach (SpaceShipAI racer in racers) // Gaat door alle racers.
        {
            if (racer != null) // Controleert of de racer bestaat.
            {
                racer.totalLaps = totalLaps; // Geeft het aantal rondes aan de racer.
            }
        }
    }

    private void UpdateFinishedRacers()
    {
        foreach (SpaceShipAI racer in racers) // Controleert alle racers.
        {
            if (racer == null) // Controleert of de racer bestaat.
                continue;

            if (!racer.HasFinished()) // Controleert of de racer nog niet klaar is.
                continue;

            if (finishedRacers.Contains(racer)) // Controleert of de racer al in de finishlijst staat.
                continue;

            finishedRacers.Add(racer); // Voegt de racer toe in finishvolgorde.
        }
    }

    public void SortRacers()
    {
        racers.RemoveAll(racer => racer == null); // Verwijdert racers die niet meer bestaan.

        racers.Sort((a, b) =>
        {
            bool aFinished = finishedRacers.Contains(a); // Controleert of racer A gefinisht is.
            bool bFinished = finishedRacers.Contains(b); // Controleert of racer B gefinisht is.

            if (aFinished && !bFinished) // Als A gefinisht is en B niet.
                return -1;

            if (!aFinished && bFinished) // Als B gefinisht is en A niet.
                return 1;

            if (aFinished && bFinished) // Als beide gefinisht zijn.
            {
                int aFinishPosition =
                    finishedRacers.IndexOf(a); // Haalt de finishpositie van A op.

                int bFinishPosition =
                    finishedRacers.IndexOf(b); // Haalt de finishpositie van B op.

                return aFinishPosition.CompareTo(
                    bFinishPosition
                ); // Sorteert op finishvolgorde.
            }

            float progressA =
                a.GetPublicRaceProgress(); // Haalt de racevoortgang van A op.

            float progressB =
                b.GetPublicRaceProgress(); // Haalt de racevoortgang van B op.

            return progressB.CompareTo(progressA); // Zet de racer met meer voortgang bovenaan.
        });
    }

    public int GetPosition(SpaceShipAI racer)
    {
        if (racer == null) // Controleert of de racer bestaat.
            return 0;

        if (finishedRacers.Contains(racer)) // Controleert of de racer gefinisht is.
        {
            return finishedRacers.IndexOf(racer) + 1; // Geeft de definitieve finishpositie.
        }

        int position = 1; // Begint op positie 1.

        float myProgress =
            racer.GetPublicRaceProgress(); // Haalt de eigen racevoortgang op.

        foreach (SpaceShipAI other in racers) // Controleert alle andere racers.
        {
            if (other == null || other == racer) // Slaat lege racers en zichzelf over.
                continue;

            if (finishedRacers.Contains(other)) // Gefinishte racers staan automatisch voor hem.
            {
                position++; // Verhoogt de positie.
                continue;
            }

            float otherProgress =
                other.GetPublicRaceProgress(); // Haalt de racevoortgang van de andere racer op.

            if (otherProgress > myProgress) // Controleert of de andere racer verder is.
            {
                position++; // Verhoogt de positie.
            }
        }

        return position; // Geeft de huidige positie terug.
    }

    public SpaceShipAI GetRacer(int position)
    {
        if (position < 1 ||
            position > racers.Count) // Controleert of de positie geldig is.
        {
            return null;
        }

        SortRacers(); // Sorteert de racers opnieuw.

        return racers[position - 1]; // Geeft de racer op die positie terug.
    }

    public int GetRacerCount()
    {
        return racers.Count; // Geeft het aantal racers terug.
    }

    public int GetRacerLap(SpaceShipAI racer)
    {
        if (racer == null) // Controleert of de racer bestaat.
            return 0;

        return racer.GetCurrentLap(); // Geeft de huidige ronde van de racer terug.
    }

    public bool IsRaceFinished()
    {
        return finishedRacers.Count >= racers.Count &&
               racers.Count > 0; // Controleert of alle racers gefinisht zijn.
    }

    public SpaceShipAI GetWinner()
    {
        if (finishedRacers.Count == 0) // Controleert of er al iemand gefinisht is.
            return null;

        return finishedRacers[0]; // Geeft de winnaar terug.
    }

    public int GetFinishPosition(SpaceShipAI racer)
    {
        if (racer == null) // Controleert of de racer bestaat.
            return 0;

        int index =
            finishedRacers.IndexOf(racer); // Zoekt de racer in de finishlijst.

        if (index == -1) // Controleert of de racer niet gevonden is.
            return 0;

        return index + 1; // Geeft de definitieve finishpositie terug.
    }
}