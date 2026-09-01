using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpaceShipAI : MonoBehaviour
{
    [Header("Race")]
    public RacePath racePath;
    public int startingWaypoint = 0;
    public int totalLaps = 3;

    [Header("Starting Grid")]
    public Transform startingGridPosition;
    public float gridReachDistance = 2f;
    public float gridMoveSpeed = 20f;
    public bool placeOnGridAtStart = true;

    [Header("Speed")]
    public float maxSpeed = 100f;
    public float acceleration = 30f;
    public float braking = 50f;

    [Header("AI Skill")]
    [Range(0.5f, 1.5f)]
    public float skill = 1f;
    [Range(0f, 0.2f)]
    public float performanceVariation = 0.05f;
    [Range(0.5f, 1.2f)]
    public float corneringAbility = 1f;

    [Header("Overtaking")]
    public float battleDistance = 35f;
    public float overtakingDistance = 18f;
    public float overtakingOffset = 8f; // Hoe ver de opponent opzij gaat bij het inhalen.
    public float overtakingCommitTime = 3f; // Hoe lang de opponent zijn inhaalactie uitvoert.
    public float overtakingCooldown = 2f; // Tijd voordat de opponent opnieuw probeert in te halen.

    [Header("Collision Avoidance")]
    public float avoidanceStrength = 5f; // Hoe sterk de opponent uitwijkt.
    public float avoidanceDistance = 8f; // Afstand waarop de opponent begint uit te wijken.

    [Header("Racing Line")]
    public float randomRacingLineOffset = 4f; // Zorgt ervoor dat opponents niet exact dezelfde lijn rijden.

    [Header("Rubber Banding")]
    public bool useRubberBanding = true;

    [Range(0f, 0.2f)]
    public float firstPlaceSpeedReduction = 0.05f;

    [Range(0f, 0.2f)]
    public float lastPlaceSpeedBoost = 0.05f;

    [Header("Rubber Band Timing")]
    public float rubberBandActivationDelay = 5f;
    public float rubberBandMinimumDuration = 4f;
    public float rubberBandSmoothness = 1f;

    [Header("Movement")]
    public bool useRigidbodyVelocity = true; // Bepaalt welke manier gebruikt wordt om te bewegen.

    private Rigidbody rb;

    private int currentWaypoint;
    private int currentLap = 1;

    private float currentSpeed;
    private float actualMaxSpeed;
    private float actualAcceleration;
    private float actualBraking;
    private float actualCornering;
    private float racingLineOffset;

    private GameObject targetRacer;

    private float overtakeTimer;
    private float overtakeCooldownTimer;
    private float overtakeSide; // Bepaalt aan welke kant de opponent inhaalt.

    private float currentRubberBandMultiplier = 1f;
    private int rubberBandState = 0;
    private float positionTimer;
    private float activeEffectTimer;

    private bool hasStarted;

    // ================================
    // FINISH SYSTEM
    // ================================

    private bool raceFinished;
    private bool finishLapActive;

    // Hiermee rijdt de AI nog één extra ronde nadat hij zijn laatste normale lap heeft voltooid.
    private int finishLapStartWaypoint;

    // ================================
    // UNITY
    // ================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true; // Voorkomt dat de Rigidbody vanzelf roteert.
    }

    private void Start()
    {
        if (racePath == null) // Controleert of er een racebaan is ingesteld.
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
        ); // Kiest de eerste waypoint.

        currentLap = 1; // Zet de eerste ronde op 1.

        CreateIndividualPerformance(); // Geeft de opponent zijn eigen prestaties.

        if (placeOnGridAtStart && startingGridPosition != null) // Controleert of de opponent op de startpositie moet staan.
        {
            rb.position = startingGridPosition.position;
            rb.rotation = startingGridPosition.rotation;
        }

        hasStarted = true;
    }

    private void CreateIndividualPerformance()
    {
        float variation = Random.Range(
            1f - performanceVariation,
            1f + performanceVariation
        ); // Maakt een willekeurige prestatie.

        actualMaxSpeed = maxSpeed * skill * variation;

        actualAcceleration =
            acceleration *
            skill *
            Random.Range(0.95f, 1.05f);

        actualBraking =
            braking *
            Random.Range(0.95f, 1.05f);

        actualCornering =
            corneringAbility *
            Random.Range(0.95f, 1.05f);

        racingLineOffset =
            Random.Range(
                -randomRacingLineOffset,
                randomRacingLineOffset
            ); // Geeft een willekeurige race-lijn.
    }

    private void FixedUpdate()
    {
        if (!hasStarted)
            return;

        // Als de extra finishronde klaar is,
        // gaat de opponent terug naar zijn starting block.
        if (raceFinished)
        {
            ReturnToStartingGrid();
            return;
        }

        if (racePath == null)
            return;

        if (overtakeCooldownTimer > 0f)
        {
            overtakeCooldownTimer -= Time.fixedDeltaTime;
        }

        // Gefinishte racers kunnen niet meer inhalen.
        if (!finishLapActive)
        {
            UpdateRubberBanding();
            UpdateBattleTarget();
        }
        else
        {
            // Tijdens de laatste extra ronde:
            // geen rubber banding en geen inhalen.
            targetRacer = null;
            overtakeTimer = 0f;
        }

        RaceWaypoint waypoint =
            racePath.GetWaypoint(currentWaypoint);

        if (waypoint == null)
            return;

        Vector3 targetPosition =
            GetTargetPosition(waypoint);

        Vector3 direction =
            targetPosition -
            rb.position;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = transform.forward;
        }
        else
        {
            direction.Normalize();
        }

        Vector3 avoidance =
            GetAvoidanceDirection();

        direction +=
            avoidance *
            avoidanceStrength;

        direction.Normalize();

        RotateShip(direction);

        float desiredSpeed =
            CalculateDesiredSpeed(
                waypoint,
                direction
            );

        UpdateSpeed(desiredSpeed);

        MoveShip();

        if (HasReachedWaypoint(waypoint))
        {
            NextWaypoint();
        }
    }

    // ============================================================
    // SPEED
    // ============================================================

    private float CalculateDesiredSpeed(
        RaceWaypoint waypoint,
        Vector3 direction)
    {
        float desiredSpeed =
            actualMaxSpeed;

        desiredSpeed *=
            waypoint.speedMultiplier;

        float turnAngle =
            Vector3.Angle(
                transform.forward,
                direction
            );

        float turnFactor =
            Mathf.InverseLerp(
                90f,
                0f,
                turnAngle
            );

        float cornerMultiplier =
            Mathf.Lerp(
                0.45f,
                1f,
                turnFactor *
                actualCornering
            );

        desiredSpeed *=
            cornerMultiplier;

        // Tijdens de extra finishronde geen rubber banding.
        if (!finishLapActive)
        {
            desiredSpeed *=
                currentRubberBandMultiplier;
        }

        return desiredSpeed;
    }

    private void UpdateSpeed(float desiredSpeed)
    {
        if (currentSpeed < desiredSpeed)
        {
            currentSpeed +=
                actualAcceleration *
                Time.fixedDeltaTime;

            currentSpeed =
                Mathf.Min(
                    currentSpeed,
                    desiredSpeed
                );
        }
        else
        {
            currentSpeed -=
                actualBraking *
                Time.fixedDeltaTime;

            currentSpeed =
                Mathf.Max(
                    currentSpeed,
                    desiredSpeed
                );
        }
    }

    // ============================================================
    // RUBBER BANDING
    // ============================================================

    private void UpdateRubberBanding()
    {
        if (!useRubberBanding)
        {
            rubberBandState = 0;

            currentRubberBandMultiplier =
                Mathf.Lerp(
                    currentRubberBandMultiplier,
                    1f,
                    rubberBandSmoothness *
                    Time.fixedDeltaTime
                );

            return;
        }

        GameObject[] racers =
            GameObject.FindGameObjectsWithTag(
                "Opponent"
            );

        if (racers.Length <= 1)
            return;

        int position =
            GetRacePosition(racers);

        bool isFirst =
            position == 1;

        bool isLast =
            position == racers.Length;

        if (rubberBandState != 0)
        {
            activeEffectTimer +=
                Time.fixedDeltaTime;

            if (activeEffectTimer <
                rubberBandMinimumDuration)
            {
                ApplyCurrentRubberBandState();
                return;
            }

            if (rubberBandState == 1 &&
                isFirst)
            {
                ApplyCurrentRubberBandState();
                return;
            }

            if (rubberBandState == 2 &&
                isLast)
            {
                ApplyCurrentRubberBandState();
                return;
            }

            rubberBandState = 0;
            positionTimer = 0f;
        }

        if (isFirst || isLast)
        {
            positionTimer +=
                Time.fixedDeltaTime;

            if (positionTimer >=
                rubberBandActivationDelay)
            {
                if (isFirst)
                    rubberBandState = 1;

                else if (isLast)
                    rubberBandState = 2;

                activeEffectTimer = 0f;
                positionTimer = 0f;
            }
        }
        else
        {
            positionTimer = 0f;
        }

        ApplyCurrentRubberBandState();
    }

    private void ApplyCurrentRubberBandState()
    {
        float targetMultiplier = 1f;

        if (rubberBandState == 1)
        {
            targetMultiplier =
                1f -
                firstPlaceSpeedReduction;
        }
        else if (rubberBandState == 2)
        {
            targetMultiplier =
                1f +
                lastPlaceSpeedBoost;
        }

        currentRubberBandMultiplier =
            Mathf.Lerp(
                currentRubberBandMultiplier,
                targetMultiplier,
                rubberBandSmoothness *
                Time.fixedDeltaTime
            );
    }

    private int GetRacePosition(
        GameObject[] racers)
    {
        float myProgress =
            GetRaceProgress();

        int position = 1;

        foreach (GameObject racer in racers)
        {
            if (racer == gameObject)
                continue;

            SpaceShipAI other =
                racer.GetComponent<SpaceShipAI>();

            if (other == null)
                continue;

            // Gefinishte racers worden niet meer
            // meegenomen in de normale positie.
            if (other.HasFinished())
                continue;

            float otherProgress =
                other.GetRaceProgress();

            if (otherProgress >
                myProgress)
            {
                position++;
            }
        }

        return position;
    }

    // ============================================================
    // RACE PROGRESS
    // ============================================================

    private float GetRaceProgress()
    {
        if (racePath == null)
            return 0f;

        int waypointCount =
            racePath.WaypointCount;

        if (waypointCount == 0)
            return 0f;

        RaceWaypoint current =
            racePath.GetWaypoint(
                currentWaypoint
            );

        RaceWaypoint next =
            racePath.GetWaypoint(
                (currentWaypoint + 1) %
                waypointCount
            );

        if (current == null ||
            next == null)
        {
            return
                ((currentLap - 1) *
                waypointCount) +
                currentWaypoint;
        }

        float segmentLength =
            Vector3.Distance(
                current.transform.position,
                next.transform.position
            );

        if (segmentLength < 0.01f)
        {
            return
                ((currentLap - 1) *
                waypointCount) +
                currentWaypoint;
        }

        float distanceFromWaypoint =
            Vector3.Distance(
                transform.position,
                current.transform.position
            );

        float segmentProgress =
            1f -
            Mathf.Clamp01(
                distanceFromWaypoint /
                segmentLength
            );

        return
            ((currentLap - 1) *
            waypointCount) +
            currentWaypoint +
            segmentProgress;
    }

    public float GetPublicRaceProgress()
    {
        return GetRaceProgress();
    }

    public int GetCurrentLap()
    {
        return currentLap;
    }

    public int GetCurrentWaypoint()
    {
        return currentWaypoint;
    }

    public int GetTotalLaps()
    {
        return totalLaps;
    }

    public bool HasFinished()
    {
        return raceFinished;
    }

    // ============================================================
    // OVERTAKING
    // ============================================================

    private void UpdateBattleTarget()
    {
        if (finishLapActive)
            return;

        if (overtakeTimer > 0f)
        {
            overtakeTimer -=
                Time.fixedDeltaTime;

            return;
        }

        if (overtakeCooldownTimer > 0f)
            return;

        GameObject closest =
            FindClosestRacerAhead();

        targetRacer =
            closest;

        if (targetRacer == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                targetRacer.transform.position
            );

        if (distance <=
            overtakingDistance)
        {
            overtakeSide =
                Random.value > 0.5f
                ? 1f
                : -1f;

            overtakeTimer =
                overtakingCommitTime;

            overtakeCooldownTimer =
                overtakingCommitTime +
                overtakingCooldown;
        }
    }

    private GameObject FindClosestRacerAhead()
    {
        GameObject[] racers =
            GameObject.FindGameObjectsWithTag(
                "Opponent"
            );

        GameObject closest = null;

        float closestDistance =
            battleDistance;

        float myProgress =
            GetRaceProgress();

        foreach (GameObject racer in racers)
        {
            if (racer == gameObject)
                continue;

            SpaceShipAI other =
                racer.GetComponent<SpaceShipAI>();

            if (other == null)
                continue;

            // Gefinishte racers mogen niet meer worden ingehaald.
            if (other.HasFinished())
                continue;

            float otherProgress =
                other.GetRaceProgress();

            float difference =
                otherProgress -
                myProgress;

            if (difference <= 0f)
                continue;

            if (difference >
                racePath.WaypointCount / 2f)
                continue;

            float distance =
                Vector3.Distance(
                    transform.position,
                    racer.transform.position
                );

            if (distance <
                closestDistance)
            {
                closestDistance =
                    distance;

                closest =
                    racer;
            }
        }

        return closest;
    }

    // ============================================================
    // TARGET POSITION
    // ============================================================

    private Vector3 GetTargetPosition(
        RaceWaypoint waypoint)
    {
        Vector3 waypointPosition =
            waypoint.transform.position;

        Vector3 trackDirection =
            GetTrackDirection();

        if (trackDirection.sqrMagnitude <
            0.01f)
        {
            return waypointPosition;
        }

        trackDirection.Normalize();

        float distanceToWaypoint =
            Vector3.Distance(
                rb.position,
                waypointPosition
            );

        float waypointSafetyDistance =
            Mathf.Max(
                waypoint.reachDistance * 3f,
                4f
            );

        if (distanceToWaypoint <=
            waypointSafetyDistance)
        {
            return waypointPosition;
        }

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                trackDirection
            ).normalized;

        float offset =
            racingLineOffset;

        if (!finishLapActive &&
            overtakeTimer > 0f &&
            targetRacer != null)
        {
            float safetyFactor =
                Mathf.InverseLerp(
                    waypointSafetyDistance * 2f,
                    waypointSafetyDistance,
                    distanceToWaypoint
                );

            float overtakeOffset =
                overtakingOffset *
                safetyFactor;

            offset +=
                overtakeSide *
                overtakeOffset;
        }

        float maximumOffset =
            overtakingOffset +
            randomRacingLineOffset;

        offset =
            Mathf.Clamp(
                offset,
                -maximumOffset,
                maximumOffset
            );

        Vector3 target =
            waypointPosition +
            side *
            offset;

        float maxTargetDistance =
            Mathf.Max(
                waypoint.reachDistance * 2f,
                8f
            );

        float targetDistance =
            Vector3.Distance(
                waypointPosition,
                target
            );

        if (targetDistance >
            maxTargetDistance)
        {
            target =
                waypointPosition +
                (target -
                 waypointPosition)
                .normalized *
                maxTargetDistance;
        }

        return target;
    }

    private Vector3 GetTrackDirection()
    {
        int nextIndex =
            (currentWaypoint + 1) %
            racePath.WaypointCount;

        RaceWaypoint current =
            racePath.GetWaypoint(
                currentWaypoint
            );

        RaceWaypoint next =
            racePath.GetWaypoint(
                nextIndex
            );

        if (current == null ||
            next == null)
        {
            return transform.forward;
        }

        Vector3 direction =
            next.transform.position -
            current.transform.position;

        if (direction.sqrMagnitude <
            0.01f)
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    // ============================================================
    // WAYPOINT
    // ============================================================

    private bool HasReachedWaypoint(
        RaceWaypoint waypoint) // Controleert of het ruimteschip het waypoint heeft bereikt.
    {
        Vector3 waypointPosition =
            waypoint.transform.position; // Haalt de positie van het waypoint op.

        float distance =
            Vector3.Distance(
                rb.position,
                waypointPosition
            ); // Berekent de afstand tussen het schip en het waypoint.

        if (distance <=
            waypoint.reachDistance) // Als het schip binnen de reachDistance is, is het waypoint bereikt.
        {
            return true;
        }

        if (distance <=
            waypoint.reachDistance * 1.5f) // Extra controle als het schip iets verder van het waypoint is.
        {
            Vector3 toWaypoint =
                waypointPosition -
                rb.position; // Berekent de richting van het schip naar het waypoint.

            if (toWaypoint.sqrMagnitude >
                0.01f) // Controleert of de afstand groot genoeg is.
            {
                float dot =
                    Vector3.Dot(
                        transform.forward,
                        toWaypoint.normalized
                    ); // Controleert of het schip richting het waypoint kijkt.

                if (dot > 0.1f) // Als het schip richting het waypoint kijkt, telt het als bereikt.
                {
                    return true;
                }
            }
        }

        return false; // Het waypoint is nog niet bereikt.
    }

    // ============================================================
    // COLLISION AVOIDANCE
    // ============================================================

    private Vector3 GetAvoidanceDirection()
    {
        GameObject[] racers =
            GameObject.FindGameObjectsWithTag(
                "Opponent"
            );

        Vector3 avoidance =
            Vector3.zero;

        foreach (GameObject racer in racers)
        {
            if (racer == gameObject)
                continue;

            Vector3 difference =
                transform.position -
                racer.transform.position;

            float distance =
                difference.magnitude;

            if (distance < 0.01f)
                continue;

            if (distance <
                avoidanceDistance)
            {
                float strength =
                    1f -
                    distance /
                    avoidanceDistance;

                avoidance +=
                    difference.normalized *
                    strength;
            }
        }

        return avoidance; // Geeft de richting terug waarin de opponent moet uitwijken.
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    private void RotateShip(
        Vector3 direction) // zorgt ervoor dat de opponent roteert.
    {
        if (direction.sqrMagnitude <
            0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        Quaternion newRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                3f *
                Time.fixedDeltaTime
            );

        rb.MoveRotation(newRotation); // Draait de opponent naar de gewenste richting.
    }

    private void MoveShip() // zorgt ervoor dat de opponent beweegt.
    {
        Vector3 velocity =
            transform.forward *
            currentSpeed;

        if (useRigidbodyVelocity)
        {
            rb.linearVelocity =
                velocity; // Beweegt de opponent met de Rigidbody.
        }
        else
        {
            rb.MovePosition(
                rb.position +
                velocity *
                Time.fixedDeltaTime
            ); // Verplaatst de opponent handmatig.
        }
    }

    // ============================================================
    // LAP / FINISH
    // ============================================================

    private void NextWaypoint() // checkt naar welke waypoint hij moet. finished de race als het klaar is.
    {
        currentWaypoint++; // Gaat naar de volgende waypoint.

        if (currentWaypoint >=
            racePath.WaypointCount) // Controleert of het einde van de baan bereikt is.
        {
            currentWaypoint = 0; // Gaat terug naar de eerste waypoint.
            currentLap++; // Gaat naar de volgende ronde.

            // ------------------------------------------------
            // NORMALE RACE KLAAR
            // ------------------------------------------------

            if (!finishLapActive &&
                currentLap > totalLaps)
            {
                StartFinishLap();
            }

            // ------------------------------------------------
            // EXTRA FINISHRONDE KLAAR
            // ------------------------------------------------

            else if (finishLapActive &&
                     currentLap >
                     totalLaps + 1)
            {
                FinishRace();
            }
        }
    }

    private void StartFinishLap()
    {
        // De AI heeft zijn normale race voltooid.
        // Hij krijgt nu nog één extra volledige ronde.

        finishLapActive = true;

        finishLapStartWaypoint =
            currentWaypoint;

        // Geen inhaalactie meer.
        targetRacer = null;
        overtakeTimer = 0f;
        overtakeCooldownTimer = 0f;

        // Geen rubber banding meer.
        rubberBandState = 0;
        positionTimer = 0f;
        activeEffectTimer = 0f;
        currentRubberBandMultiplier = 1f;

        Debug.Log(
            gameObject.name +
            " heeft de race gefinisht en rijdt nog 1 extra ronde."
        );
    }

    private void FinishRace() // zorgt ervoor dat de opponent finished. RaceManager.cs gebruikt dit.
    {
        raceFinished = true;

        finishLapActive = false;

        currentSpeed = 0f;

        targetRacer = null;

        overtakeTimer = 0f;

        if (rb != null)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }

        Debug.Log(
            gameObject.name +
            " is volledig klaar en gaat terug naar zijn starting block."
        );
    }

    // ============================================================
    // RETURN TO GRID
    // ============================================================

    private void ReturnToStartingGrid() // hiermee gaat de Opponent terug naar zijn starting point.
    {
        if (startingGridPosition == null) // Controleert of er een startpositie is.
        {
            StopShip();
            return;
        }

        Vector3 target =
            startingGridPosition.position;

        Vector3 difference =
            target -
            rb.position;

        float distance =
            difference.magnitude;

        if (distance <=
            gridReachDistance)
        {
            StopShip();

            Quaternion rotation =
                Quaternion.Slerp(
                    rb.rotation,
                    startingGridPosition.rotation,
                    3f *
                    Time.fixedDeltaTime
                );

            rb.MoveRotation(rotation);

            return;
        }

        Vector3 direction =
            difference.normalized;

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        Quaternion newRotation =
            Quaternion.Slerp(
                rb.rotation,
                desiredRotation,
                3f *
                Time.fixedDeltaTime
            );

        rb.MoveRotation(newRotation);

        rb.linearVelocity =
            direction *
            gridMoveSpeed;
    }

    private void StopShip()
    {
        currentSpeed = 0f;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected() // Laat zien welke Waypoint de opponent wilt bereiken.
    {
        if (racePath != null &&
            racePath.WaypointCount > 0)
        {
            RaceWaypoint waypoint =
                racePath.GetWaypoint(
                    currentWaypoint
                );

            if (waypoint != null)
            {
                Gizmos.color =
                    Color.red;

                Gizmos.DrawLine(
                    transform.position,
                    waypoint.transform.position
                );

                Gizmos.DrawWireSphere(
                    transform.position,
                    battleDistance
                );
            }
        }

        if (startingGridPosition != null)
        {
            Gizmos.color =
                Color.green;

            Gizmos.DrawWireSphere(
                startingGridPosition.position,
                gridReachDistance
            );
        }
    }
}