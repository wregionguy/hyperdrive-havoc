using UnityEngine;

public class PlayerRaceController : MonoBehaviour
{
    [Header("Race")]
    public RacePath racePath; // Dezelfde racebaan als de opponents.
    public int startingWaypoint = 0; // Bepaalt bij welk waypoint de player begint.
    public int totalLaps = 3; // Het aantal rondes van de race.

    private int currentWaypoint; // Houdt bij bij welk waypoint de player is.
    private int currentLap = 1; // Houdt bij in welke ronde de player zit.
    private bool raceFinished; // Houdt bij of de player gefinisht is.

    private Rigidbody rb; // Rigidbody van de player.

    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); // Haalt de Rigidbody van de player op.
    }

    private void Start()
    {
        if (racePath == null) // Controleert of er een racebaan is.
        {
            Debug.LogError(gameObject.name + " has no Race Path assigned.");
            enabled = false;
            return;
        }

        if (racePath.WaypointCount == 0) // Controleert of de racebaan waypoints heeft.
        {
            Debug.LogError("Race Path has no waypoints.");
            enabled = false;
            return;
        }

        currentWaypoint =
            Mathf.Clamp(
                startingWaypoint,
                0,
                racePath.WaypointCount - 1
            ); // Kiest het start waypoint.
    }

    private void Update()
    {
        if (raceFinished) // Controleert of de race klaar is.
            return;

        if (racePath == null) // Controleert of er een racebaan is.
            return;

        RaceWaypoint waypoint =
            racePath.GetWaypoint(currentWaypoint); // Haalt het huidige waypoint op.

        if (waypoint == null) // Controleert of het waypoint bestaat.
            return;

        if (HasReachedWaypoint(waypoint)) // Controleert of de player het waypoint bereikt heeft.
            NextWaypoint(); // Gaat naar het volgende waypoint.
    }

    private bool HasReachedWaypoint(RaceWaypoint waypoint)
    {
        Vector3 waypointPosition =
            waypoint.transform.position; // Haalt de positie van het waypoint op.

        Vector3 playerPosition =
            rb != null
                ? rb.position
                : transform.position; // Gebruikt de Rigidbody positie.

        float distance =
            Vector3.Distance(
                playerPosition,
                waypointPosition
            ); // Berekent de afstand tot het waypoint.

        if (distance <= waypoint.reachDistance) // Controleert de normale bereikafstand.
            return true;

        if (distance <= waypoint.reachDistance * 1.5f) // Extra controle zoals bij de AI.
        {
            Vector3 toWaypoint =
                waypointPosition -
                playerPosition; // Richting naar het waypoint.

            if (toWaypoint.sqrMagnitude > 0.01f)
            {
                float dot =
                    Vector3.Dot(
                        transform.forward,
                        toWaypoint.normalized
                    ); // Controleert of de player naar het waypoint kijkt.

                if (dot > 0.1f)
                    return true;
            }
        }

        return false; // Waypoint is nog niet bereikt.
    }

    private void NextWaypoint()
    {
        currentWaypoint++; // Gaat naar de volgende waypoint.

        if (currentWaypoint >= racePath.WaypointCount) // Controleert of het einde van de baan bereikt is.
        {
            currentWaypoint = 0; // Gaat terug naar de eerste waypoint.
            currentLap++; // Gaat naar de volgende ronde.

            if (currentLap > totalLaps) // Controleert of alle rondes klaar zijn.
            {
                FinishRace(); // Beëindigt de race.
            }
        }
    }

    private void FinishRace()
    {
        raceFinished = true; // Zet de player op finished.

        if (rb != null) // Controleert of er een Rigidbody is.
        {
            rb.linearVelocity = Vector3.zero; // Stopt de snelheid.
            rb.angularVelocity = Vector3.zero; // Stopt het draaien.
        }
    }

    public float GetRaceProgress()
    {
        if (racePath == null ||
            racePath.WaypointCount == 0)
        {
            return 0f;
        }

        RaceWaypoint current =
            racePath.GetWaypoint(currentWaypoint); // Huidige waypoint.

        RaceWaypoint next =
            racePath.GetWaypoint(
                (currentWaypoint + 1) %
                racePath.WaypointCount
            ); // Volgende waypoint.

        if (current == null || next == null)
        {
            return
                ((currentLap - 1) *
                racePath.WaypointCount) +
                currentWaypoint;
        }

        float segmentLength =
            Vector3.Distance(
                current.transform.position,
                next.transform.position
            ); // Lengte van het huidige baanstuk.

        if (segmentLength < 0.01f)
        {
            return
                ((currentLap - 1) *
                racePath.WaypointCount) +
                currentWaypoint;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                current.transform.position
            ); // Afstand tot huidige waypoint.

        float segmentProgress =
            1f -
            Mathf.Clamp01(
                distance / segmentLength
            ); // Berekent de voortgang op het baanstuk.

        return
            ((currentLap - 1) *
            racePath.WaypointCount) +
            currentWaypoint +
            segmentProgress; // Geeft totale race voortgang.
    }

    public int GetCurrentLap()
    {
        return currentLap; // Geeft de huidige ronde terug.
    }

    public int GetCurrentWaypoint()
    {
        return currentWaypoint; // Geeft het huidige waypoint terug.
    }

    public bool HasFinished()
    {
        return raceFinished; // Geeft aan of de player gefinisht is.
    }
}