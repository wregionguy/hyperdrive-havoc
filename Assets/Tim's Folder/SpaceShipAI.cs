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
    public float overtakingOffset = 8f;
    public float overtakingCommitTime = 3f;
    public float overtakingCooldown = 2f;

    [Header("Collision Avoidance")]
    public float avoidanceStrength = 5f;
    public float avoidanceDistance = 8f;

    [Header("Racing Line")]
    public float randomRacingLineOffset = 4f;

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
    public bool useRigidbodyVelocity = true;

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
    private float overtakeSide;

    private float currentRubberBandMultiplier = 1f;
    private int rubberBandState = 0;
    private float positionTimer;
    private float activeEffectTimer;

    private bool hasStarted;
    private bool raceFinished;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        if (racePath == null)
        {
            Debug.LogError(gameObject.name + " has no Race Path assigned.");
            enabled = false;
            return;
        }

        if (racePath.WaypointCount == 0)
        {
            Debug.LogError("Race Path has no waypoints.");
            enabled = false;
            return;
        }

        currentWaypoint = Mathf.Clamp(startingWaypoint, 0, racePath.WaypointCount - 1);
        currentLap = 1;

        CreateIndividualPerformance();

        if (placeOnGridAtStart && startingGridPosition != null)
        {
            rb.position = startingGridPosition.position;
            rb.rotation = startingGridPosition.rotation;
        }

        hasStarted = true;
    }

    private void CreateIndividualPerformance()
    {
        float variation = Random.Range(1f - performanceVariation, 1f + performanceVariation);

        actualMaxSpeed = maxSpeed * skill * variation;
        actualAcceleration = acceleration * skill * Random.Range(0.95f, 1.05f);
        actualBraking = braking * Random.Range(0.95f, 1.05f);
        actualCornering = corneringAbility * Random.Range(0.95f, 1.05f);
        racingLineOffset = Random.Range(-randomRacingLineOffset, randomRacingLineOffset);
    }

    private void FixedUpdate()
    {
        if (!hasStarted)
            return;

        if (raceFinished)
        {
            ReturnToStartingGrid();
            return;
        }

        if (racePath == null)
            return;

        if (overtakeCooldownTimer > 0f)
            overtakeCooldownTimer -= Time.fixedDeltaTime;

        UpdateRubberBanding();

        RaceWaypoint waypoint = racePath.GetWaypoint(currentWaypoint);

        if (waypoint == null)
            return;

        UpdateBattleTarget();

        Vector3 targetPosition = GetTargetPosition(waypoint);
        Vector3 direction = targetPosition - rb.position;

        if (direction.sqrMagnitude < 0.01f)
            direction = transform.forward;
        else
            direction.Normalize();

        Vector3 avoidance = GetAvoidanceDirection();
        direction += avoidance * avoidanceStrength;
        direction.Normalize();

        RotateShip(direction);

        float desiredSpeed = CalculateDesiredSpeed(waypoint, direction);

        UpdateSpeed(desiredSpeed);
        MoveShip();

        if (HasReachedWaypoint(waypoint))
            NextWaypoint();
    }

    private float CalculateDesiredSpeed(RaceWaypoint waypoint, Vector3 direction)
    {
        float desiredSpeed = actualMaxSpeed;

        desiredSpeed *= waypoint.speedMultiplier;

        float turnAngle = Vector3.Angle(transform.forward, direction);

        float turnFactor = Mathf.InverseLerp(90f, 0f, turnAngle);

        float cornerMultiplier = Mathf.Lerp(
            0.45f,
            1f,
            turnFactor * actualCornering
        );

        desiredSpeed *= cornerMultiplier;
        desiredSpeed *= currentRubberBandMultiplier;

        return desiredSpeed;
    }

    private void UpdateSpeed(float desiredSpeed)
    {
        if (currentSpeed < desiredSpeed)
        {
            currentSpeed += actualAcceleration * Time.fixedDeltaTime;
            currentSpeed = Mathf.Min(currentSpeed, desiredSpeed);
        }
        else
        {
            currentSpeed -= actualBraking * Time.fixedDeltaTime;
            currentSpeed = Mathf.Max(currentSpeed, desiredSpeed);
        }
    }

    private void UpdateRubberBanding()
    {
        if (!useRubberBanding)
        {
            rubberBandState = 0;

            currentRubberBandMultiplier = Mathf.Lerp(
                currentRubberBandMultiplier,
                1f,
                rubberBandSmoothness * Time.fixedDeltaTime
            );

            return;
        }

        GameObject[] racers = GameObject.FindGameObjectsWithTag("Opponent");

        if (racers.Length <= 1)
            return;

        int position = GetRacePosition(racers);

        bool isFirst = position == 1;
        bool isLast = position == racers.Length;

        if (rubberBandState != 0)
        {
            activeEffectTimer += Time.fixedDeltaTime;

            if (activeEffectTimer < rubberBandMinimumDuration)
            {
                ApplyCurrentRubberBandState();
                return;
            }

            if (rubberBandState == 1 && isFirst)
            {
                ApplyCurrentRubberBandState();
                return;
            }

            if (rubberBandState == 2 && isLast)
            {
                ApplyCurrentRubberBandState();
                return;
            }

            rubberBandState = 0;
            positionTimer = 0f;
        }

        if (isFirst || isLast)
        {
            positionTimer += Time.fixedDeltaTime;

            if (positionTimer >= rubberBandActivationDelay)
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
            targetMultiplier = 1f - firstPlaceSpeedReduction;
        else if (rubberBandState == 2)
            targetMultiplier = 1f + lastPlaceSpeedBoost;

        currentRubberBandMultiplier = Mathf.Lerp(
            currentRubberBandMultiplier,
            targetMultiplier,
            rubberBandSmoothness * Time.fixedDeltaTime
        );
    }

    private int GetRacePosition(GameObject[] racers)
    {
        float myProgress = GetRaceProgress();
        int position = 1;

        foreach (GameObject racer in racers)
        {
            if (racer == gameObject)
                continue;

            SpaceShipAI other = racer.GetComponent<SpaceShipAI>();

            if (other == null)
                continue;

            float otherProgress = other.GetRaceProgress();

            if (otherProgress > myProgress)
                position++;
        }

        return position;
    }

    private float GetRaceProgress()
    {
        if (racePath == null)
            return 0f;

        int waypointCount = racePath.WaypointCount;

        if (waypointCount == 0)
            return 0f;

        RaceWaypoint current = racePath.GetWaypoint(currentWaypoint);
        RaceWaypoint next = racePath.GetWaypoint((currentWaypoint + 1) % waypointCount);

        if (current == null || next == null)
        {
            return ((currentLap - 1) * waypointCount) + currentWaypoint;
        }

        float segmentLength = Vector3.Distance(
            current.transform.position,
            next.transform.position
        );

        if (segmentLength < 0.01f)
        {
            return ((currentLap - 1) * waypointCount) + currentWaypoint;
        }

        float distanceFromWaypoint = Vector3.Distance(
            transform.position,
            current.transform.position
        );

        float segmentProgress = 1f - Mathf.Clamp01(
            distanceFromWaypoint / segmentLength
        );

        return ((currentLap - 1) * waypointCount) +
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

    private void UpdateBattleTarget()
    {
        if (overtakeTimer > 0f)
        {
            overtakeTimer -= Time.fixedDeltaTime;
            return;
        }

        if (overtakeCooldownTimer > 0f)
            return;

        GameObject closest = FindClosestRacerAhead();

        targetRacer = closest;

        if (targetRacer == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            targetRacer.transform.position
        );

        if (distance <= overtakingDistance)
        {
            overtakeSide = Random.value > 0.5f ? 1f : -1f;
            overtakeTimer = overtakingCommitTime;
            overtakeCooldownTimer = overtakingCommitTime + overtakingCooldown;
        }
    }

    private GameObject FindClosestRacerAhead()
    {
        GameObject[] racers = GameObject.FindGameObjectsWithTag("Opponent");

        GameObject closest = null;
        float closestDistance = battleDistance;
        float myProgress = GetRaceProgress();

        foreach (GameObject racer in racers)
        {
            if (racer == gameObject)
                continue;

            SpaceShipAI other = racer.GetComponent<SpaceShipAI>();

            if (other == null)
                continue;

            float otherProgress = other.GetRaceProgress();
            float difference = otherProgress - myProgress;

            if (difference <= 0f)
                continue;

            if (difference > racePath.WaypointCount / 2f)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                racer.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = racer;
            }
        }

        return closest;
    }

    private Vector3 GetTargetPosition(RaceWaypoint waypoint)
    {
        Vector3 waypointPosition = waypoint.transform.position;
        Vector3 trackDirection = GetTrackDirection();

        if (trackDirection.sqrMagnitude < 0.01f)
            return waypointPosition;

        trackDirection.Normalize();

        float distanceToWaypoint = Vector3.Distance(
            rb.position,
            waypointPosition
        );

        float waypointSafetyDistance = Mathf.Max(
            waypoint.reachDistance * 3f,
            4f
        );

        if (distanceToWaypoint <= waypointSafetyDistance)
            return waypointPosition;

        Vector3 side = Vector3.Cross(
            Vector3.up,
            trackDirection
        ).normalized;

        float offset = racingLineOffset;

        if (overtakeTimer > 0f && targetRacer != null)
        {
            float safetyFactor = Mathf.InverseLerp(
                waypointSafetyDistance * 2f,
                waypointSafetyDistance,
                distanceToWaypoint
            );

            float overtakeOffset = overtakingOffset * safetyFactor;

            offset += overtakeSide * overtakeOffset;
        }

        float maximumOffset =
            overtakingOffset +
            randomRacingLineOffset;

        offset = Mathf.Clamp(
            offset,
            -maximumOffset,
            maximumOffset
        );

        Vector3 target =
            waypointPosition +
            side * offset;

        float maxTargetDistance = Mathf.Max(
            waypoint.reachDistance * 2f,
            8f
        );

        float targetDistance = Vector3.Distance(
            waypointPosition,
            target
        );

        if (targetDistance > maxTargetDistance)
        {
            target =
                waypointPosition +
                (target - waypointPosition).normalized *
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
            racePath.GetWaypoint(currentWaypoint);

        RaceWaypoint next =
            racePath.GetWaypoint(nextIndex);

        if (current == null || next == null)
            return transform.forward;

        Vector3 direction =
            next.transform.position -
            current.transform.position;

        if (direction.sqrMagnitude < 0.01f)
            return transform.forward;

        return direction.normalized;
    }

    private bool HasReachedWaypoint(RaceWaypoint waypoint) // Controleert of het ruimteschip het waypoint heeft bereikt.
    {
        Vector3 waypointPosition = waypoint.transform.position; // Haalt de positie van het waypoint op.

        float distance = Vector3.Distance(rb.position,waypointPosition); // Berekent de afstand tussen het schip en het waypoint.

        if (distance <= waypoint.reachDistance) // Als het schip binnen de reachDistance is, is het waypoint bereikt.
            return true;

        if (distance <= waypoint.reachDistance * 1.5f)  // Extra controle als het schip iets verder van het waypoint is.
        {
            Vector3 toWaypoint = waypointPosition - rb.position; // Berekent de richting van het schip naar het waypoint.

            if (toWaypoint.sqrMagnitude > 0.01f) // Controleert of de afstand groot genoeg is om de richting te berekenen.
            {
                float dot = Vector3.Dot(transform.forward,toWaypoint.normalized); // Controleert of het schip ongeveer richting het waypoint kijkt.

                if (dot > 0.1f) // Als het schip richting het waypoint kijkt, telt het als bereikt.
                    return true;
            }
        }

        return false; // Het waypoint is nog niet bereikt.
    }

    private Vector3 GetAvoidanceDirection() //zorgt ervoor dat de opponent niet elkaar beuken en wijken uit als ze dichtbij komen.
    {
        GameObject[] racers =
            GameObject.FindGameObjectsWithTag("Opponent");

        Vector3 avoidance = Vector3.zero;

        foreach (GameObject racer in racers)
        {
            if (racer == gameObject)
                continue;

            Vector3 difference =
                transform.position -
                racer.transform.position;

            float distance = difference.magnitude;

            if (distance < 0.01f)
                continue;

            if (distance < avoidanceDistance)
            {
                float strength =
                    1f -
                    distance / avoidanceDistance;

                avoidance +=
                    difference.normalized *
                    strength;
            }
        }

        return avoidance;
    }

    private void RotateShip(Vector3 direction) //zorgt ervoor dat de opponent roteert.
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        Quaternion newRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                3f * Time.fixedDeltaTime
            );

        rb.MoveRotation(newRotation);
    }

    private void MoveShip() //zorgt ervoor dat de opponent beweegt.
    {
        Vector3 velocity =
            transform.forward *
            currentSpeed;

        if (useRigidbodyVelocity)
        {
            rb.linearVelocity = velocity;
        }
        else
        {
            rb.MovePosition(
                rb.position +
                velocity * Time.fixedDeltaTime
            );
        }
    }

    private void NextWaypoint() // checkt naar welke waypoint hij moet. finished de race als het klaar is. 
    {
        currentWaypoint++;

        if (currentWaypoint >= racePath.WaypointCount)
        {
            currentWaypoint = 0;
            currentLap++;

            if (currentLap > totalLaps)
                FinishRace();
        }
    }

    private void FinishRace() //zorgt ervoor dat de speler finished. RaceManager.cs gebruikt dit.
    {
        raceFinished = true;
        currentSpeed = 0f;
        targetRacer = null;
        overtakeTimer = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ReturnToStartingGrid() //hiermee gaat de Opponent terug naar zijn starting point
    {
        if (startingGridPosition == null)
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

        if (distance <= gridReachDistance)
        {
            StopShip();

            Quaternion rotation =
                Quaternion.Slerp(
                    rb.rotation,
                    startingGridPosition.rotation,
                    3f * Time.fixedDeltaTime
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
                3f * Time.fixedDeltaTime
            );

        rb.MoveRotation(newRotation);

        rb.linearVelocity =
            direction *
            gridMoveSpeed;
    }

    private void StopShip()
    {
        currentSpeed = 0f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    private void OnDrawGizmosSelected() // Laat zien welke Waypoint the opponent wilt bereiken.
    {
        if (racePath != null &&
            racePath.WaypointCount > 0)
        {
            RaceWaypoint waypoint =
                racePath.GetWaypoint(currentWaypoint);

            if (waypoint != null)
            {
                Gizmos.color = Color.red;

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
            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(
                startingGridPosition.position,
                gridReachDistance
            );
        }
    }
}