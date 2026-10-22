
using UnityEngine;

public class PlayerRaceController : MonoBehaviour
{
    [Header("Race")]
    public RacePath racePath; // Dezelfde racebaan als de opponents.
    public int startingWaypoint = 0; // Bepaalt bij welk waypoint de player begint.
    public int totalLaps = 3; // Het aantal rondes van de race.

    [Header("Automatic Driving After Finish")]
    public bool autoDriveAfterFinish = true; // Laat de player doorrijden na de finish.
    public float autoDriveSpeed = 20f; // Snelheid waarmee de player automatisch rijdt.
    public float autoDriveTurnSpeed = 5f; // Hoe snel de player naar het volgende waypoint draait.
    public float autoDriveReachDistance = 3f; // Afstand waarop het volgende waypoint wordt gekozen.

    private int currentWaypoint; // Houdt bij bij welk waypoint de player is.
    private int currentLap = 1; // Houdt bij in welke ronde de player zit.
    private bool raceFinished; // Houdt bij of de player gefinisht is.

    private Rigidbody rb; // Rigidbody van de player.
    private bool autoDriving; // Houdt bij of de player automatisch rijdt.

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

        currentWaypoint = Mathf.Clamp(
            startingWaypoint,
            0,
            racePath.WaypointCount - 1
        ); // Kiest het start waypoint.
    }

    private void Update()
    {
        if (racePath == null)
            return;

        // Na de finish neemt de automatische besturing het over.
        if (autoDriving)
        {
            AutoDrive();
            return;
        }

        if (raceFinished)
            return;

        RaceWaypoint waypoint =
            racePath.GetWaypoint(currentWaypoint); // Haalt het huidige waypoint op.

        if (waypoint == null)
            return;

        if (HasReachedWaypoint(waypoint))
            NextWaypoint(); // Gaat naar het volgende waypoint.

        // Testtoets om de finish te activeren.
        if (Input.GetKeyDown(KeyCode.P))
        {
            FinishRace();
        }
    }

    private void FixedUpdate()
    {
        // De automatische beweging wordt via de Rigidbody uitgevoerd.
        // De speler blijft tijdens de normale race door je eigen
        // bestaande besturingsscript bestuurd worden.
    }

    private bool HasReachedWaypoint(RaceWaypoint waypoint)
    {
        Vector3 waypointPosition = waypoint.transform.position;

        Vector3 playerPosition = rb != null
            ? rb.position
            : transform.position;

        float distance = Vector3.Distance(
            playerPosition,
            waypointPosition
        );

        if (distance <= waypoint.reachDistance)
            return true;

        if (distance <= waypoint.reachDistance * 1.5f)
        {
            Vector3 toWaypoint = waypointPosition - playerPosition;

            if (toWaypoint.sqrMagnitude > 0.01f)
            {
                float dot = Vector3.Dot(
                    transform.forward,
                    toWaypoint.normalized
                );

                if (dot > 0.1f)
                    return true;
            }
        }

        return false;
    }

    private void NextWaypoint()
    {
        currentWaypoint++;

        if (currentWaypoint >= racePath.WaypointCount)
        {
            currentWaypoint = 0;
            currentLap++;

            if (currentLap > totalLaps)
            {
                FinishRace();
            }
        }
    }

    private void FinishRace()
    {
        if (raceFinished)
            return;

        raceFinished = true;

        if (autoDriveAfterFinish)
        {
            // Schakel de normale spelerbesturing uit.
            DisablePlayerControls();

            // Start automatisch rijden zonder de huidige snelheid
            // abrupt te stoppen.
            autoDriving = true;

            Debug.Log("Player gefinisht: automatische besturing gestart.");
        }
        else
        {
            // Oud gedrag: de speler stopt na de finish.
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void AutoDrive()
    {
        if (racePath == null || racePath.WaypointCount == 0)
            return;

        RaceWaypoint waypoint =
            racePath.GetWaypoint(currentWaypoint);

        if (waypoint == null)
            return;

        Vector3 targetPosition = waypoint.transform.position;

        // Houd de rijrichting horizontaal om onbedoeld omhoog of
        // omlaag sturen te voorkomen.
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction.normalized, Vector3.up);

            Quaternion newRotation = Quaternion.Slerp(
                rb != null ? rb.rotation : transform.rotation,
                targetRotation,
                autoDriveTurnSpeed * Time.fixedDeltaTime
            );

            if (rb != null)
                rb.MoveRotation(newRotation);
            else
                transform.rotation = newRotation;
        }

        // Rijd vooruit in de richting waarin de speler kijkt.
        Vector3 forwardMovement = transform.forward * autoDriveSpeed;

        if (rb != null)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = forwardMovement.x;
            velocity.z = forwardMovement.z;
            rb.linearVelocity = velocity;
        }
        else
        {
            transform.position +=
                forwardMovement * Time.deltaTime;
        }

        // Kies het volgende waypoint zodra de speler dichtbij genoeg is.
        float distance = Vector3.Distance(
            transform.position,
            targetPosition
        );

        if (distance <= Mathf.Max(
            autoDriveReachDistance,
            waypoint.reachDistance
        ))
        {
            currentWaypoint++;

            if (currentWaypoint >= racePath.WaypointCount)
                currentWaypoint = 0;
        }
    }

    private void DisablePlayerControls()
    {
        // Schakel andere scripts op de speler uit die invoer verwerken.
        // Dit script zelf blijft actief voor de automatische besturing.
        MonoBehaviour[] scripts =
            GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script == this)
                continue;

            // Schakel alleen scripts uit die als spelerbesturing
            // kunnen functioneren. Pas deze namen aan als nodig.
            string scriptName = script.GetType().Name.ToLower();

            if (scriptName.Contains("controller") ||
                scriptName.Contains("input") ||
                scriptName.Contains("movement") ||
                scriptName.Contains("playerdrive"))
            {
                script.enabled = false;
            }
        }
    }

    public float GetRaceProgress()
    {
        if (racePath == null || racePath.WaypointCount == 0)
            return 0f;

        RaceWaypoint current =
            racePath.GetWaypoint(currentWaypoint);

        RaceWaypoint next =
            racePath.GetWaypoint(
                (currentWaypoint + 1) % racePath.WaypointCount
            );

        if (current == null || next == null)
        {
            return ((currentLap - 1) * racePath.WaypointCount)
                + currentWaypoint;
        }

        float segmentLength = Vector3.Distance(
            current.transform.position,
            next.transform.position
        );

        if (segmentLength < 0.01f)
        {
            return ((currentLap - 1) * racePath.WaypointCount)
                + currentWaypoint;
        }

        float distance = Vector3.Distance(
            transform.position,
            current.transform.position
        );

        float segmentProgress = 1f - Mathf.Clamp01(
            distance / segmentLength
        );

        return ((currentLap - 1) * racePath.WaypointCount)
            + currentWaypoint
            + segmentProgress;
    }

    public int GetCurrentLap()
    {
        return currentLap;
    }

    public int GetCurrentWaypoint()
    {
        return currentWaypoint;
    }

    public bool HasFinished()
    {
        return raceFinished;
    }
}