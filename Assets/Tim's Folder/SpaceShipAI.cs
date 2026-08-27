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

    private float currentRubberBandMultiplier = 1f; // Huidige rubber band snelheid.
    private int rubberBandState = 0; // Houdt bij welke rubber banding actief is.
    private float positionTimer; 
    private float activeEffectTimer; 

    private bool hasStarted; 
    private bool raceFinished; 

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

        currentWaypoint = Mathf.Clamp(startingWaypoint, 0, racePath.WaypointCount - 1); // Kiest de eerste waypoint.
        currentLap = 1; // Zet de eerste ronde op 1.

        CreateIndividualPerformance(); // Geeft de opponent zijn eigen prestaties.

        if (placeOnGridAtStart && startingGridPosition != null) // Controleert of de opponent op de startpositie moet staan.
        {
            rb.position = startingGridPosition.position; // Zet de positie op de startpositie.
            rb.rotation = startingGridPosition.rotation; // Zet de rotatie op de startpositie.
        }

        hasStarted = true; // Laat weten dat de opponent gestart is.
    }

    private void CreateIndividualPerformance()
    {
        float variation = Random.Range(1f - performanceVariation, 1f + performanceVariation); // Maakt een willekeurige prestatie.

        actualMaxSpeed = maxSpeed * skill * variation; // Berekent de echte maximale snelheid.
        actualAcceleration = acceleration * skill * Random.Range(0.95f, 1.05f); // Berekent de echte acceleratie.
        actualBraking = braking * Random.Range(0.95f, 1.05f); // Berekent de echte remkracht.
        actualCornering = corneringAbility * Random.Range(0.95f, 1.05f); // Berekent de echte bochtvaardigheid.
        racingLineOffset = Random.Range(-randomRacingLineOffset, randomRacingLineOffset); // Geeft een willekeurige race-lijn.
    }

    private void FixedUpdate()
    {
        if (!hasStarted) // Controleert of de race gestart is.
            return;

        if (raceFinished) // Controleert of de opponent klaar is.
        {
            ReturnToStartingGrid(); // Laat de opponent teruggaan naar de startpositie.
            return;
        }

        if (racePath == null) // Controleert of er een racebaan is.
            return;

        if (overtakeCooldownTimer > 0f) // Controleert of de inhaal cooldown nog actief is.
            overtakeCooldownTimer -= Time.fixedDeltaTime; // Telt de cooldown af.

        UpdateRubberBanding(); // Controleert de rubber banding.

        RaceWaypoint waypoint = racePath.GetWaypoint(currentWaypoint); // Haalt de huidige waypoint op.

        if (waypoint == null) // Controleert of de waypoint bestaat.
            return;

        UpdateBattleTarget(); // Zoekt naar een opponent om in te halen.

        Vector3 targetPosition = GetTargetPosition(waypoint); // Berekent de doelpositie.
        Vector3 direction = targetPosition - rb.position; // Berekent de richting naar het doel.

        if (direction.sqrMagnitude < 0.01f) // Controleert of de richting bijna nul is.
            direction = transform.forward; // Gebruikt de huidige richting.
        else
            direction.Normalize(); // Maakt de richting normaal.

        Vector3 avoidance = GetAvoidanceDirection(); // Berekent hoe de opponent moet uitwijken.
        direction += avoidance * avoidanceStrength; // Voegt het uitwijken toe aan de richting.
        direction.Normalize(); // Maakt de nieuwe richting normaal.

        RotateShip(direction); // Draait het ruimteschip naar de richting.

        float desiredSpeed = CalculateDesiredSpeed(waypoint, direction); // Berekent de gewenste snelheid.

        UpdateSpeed(desiredSpeed); // Past de snelheid aan.
        MoveShip(); // Laat het ruimteschip bewegen.

        if (HasReachedWaypoint(waypoint)) // Controleert of de waypoint bereikt is.
            NextWaypoint(); // Gaat naar de volgende waypoint.
    }

    private float CalculateDesiredSpeed(RaceWaypoint waypoint, Vector3 direction)
    {
        float desiredSpeed = actualMaxSpeed; // Begint met de maximale snelheid.

        desiredSpeed *= waypoint.speedMultiplier; // Past de snelheid van de waypoint toe.

        float turnAngle = Vector3.Angle(transform.forward, direction); // Berekent hoe scherp de bocht is.

        float turnFactor = Mathf.InverseLerp(90f, 0f, turnAngle); // Berekent hoe recht het ruimteschip rijdt.

        float cornerMultiplier = Mathf.Lerp(
            0.45f,
            1f,
            turnFactor * actualCornering
        ); // Berekent de snelheid voor de bocht.

        desiredSpeed *= cornerMultiplier; // Past de bocht snelheid toe.
        desiredSpeed *= currentRubberBandMultiplier; // Past rubber banding toe.

        return desiredSpeed; // Geeft de gewenste snelheid terug.
    }

    private void UpdateSpeed(float desiredSpeed)
    {
        if (currentSpeed < desiredSpeed) // Controleert of het ruimteschip moet versnellen.
        {
            currentSpeed += actualAcceleration * Time.fixedDeltaTime; // Versnelt het ruimteschip.
            currentSpeed = Mathf.Min(currentSpeed, desiredSpeed); // Voorkomt dat het te snel gaat.
        }
        else
        {
            currentSpeed -= actualBraking * Time.fixedDeltaTime; // Remt het ruimteschip.
            currentSpeed = Mathf.Max(currentSpeed, desiredSpeed); // Voorkomt dat het te langzaam gaat.
        }
    }

    private void UpdateRubberBanding()
    {
        if (!useRubberBanding) // Controleert of rubber banding uit staat.
        {
            rubberBandState = 0; // Zet rubber banding uit.

            currentRubberBandMultiplier = Mathf.Lerp(
                currentRubberBandMultiplier,
                1f,
                rubberBandSmoothness * Time.fixedDeltaTime
            ); // Brengt de snelheid terug naar normaal.

            return;
        }

        GameObject[] racers = GameObject.FindGameObjectsWithTag("Opponent"); // Zoekt alle opponents.

        if (racers.Length <= 1) // Controleert of er genoeg opponents zijn.
            return;

        int position = GetRacePosition(racers); // Berekent de racepositie.

        bool isFirst = position == 1; // Controleert of de opponent eerste staat.
        bool isLast = position == racers.Length; // Controleert of de opponent laatste staat.

        if (rubberBandState != 0) // Controleert of er al rubber banding actief is.
        {
            activeEffectTimer += Time.fixedDeltaTime; // Telt de actieve tijd op.

            if (activeEffectTimer < rubberBandMinimumDuration) // Controleert of het effect lang genoeg actief is.
            {
                ApplyCurrentRubberBandState(); // Houdt het effect actief.
                return;
            }

            if (rubberBandState == 1 && isFirst) // Controleert of de eerste plaats nog steeds eerste staat.
            {
                ApplyCurrentRubberBandState(); // Houdt de snelheidsvermindering actief.
                return;
            }

            if (rubberBandState == 2 && isLast) // Controleert of de laatste plaats nog steeds laatste staat.
            {
                ApplyCurrentRubberBandState(); // Houdt de snelheidsboost actief.
                return;
            }

            rubberBandState = 0; // Zet het huidige effect uit.
            positionTimer = 0f; // Reset de positie timer.
        }

        if (isFirst || isLast) // Controleert of de opponent eerste of laatste staat.
        {
            positionTimer += Time.fixedDeltaTime; // Telt hoe lang hij daar staat.

            if (positionTimer >= rubberBandActivationDelay) // Controleert of de vertraging voorbij is.
            {
                if (isFirst)
                    rubberBandState = 1; // Activeert de vertraging voor de eerste plaats.
                else if (isLast)
                    rubberBandState = 2; // Activeert de boost voor de laatste plaats.

                activeEffectTimer = 0f; // Reset de effect timer.
                positionTimer = 0f; // Reset de positie timer.
            }
        }
        else
        {
            positionTimer = 0f; // Reset de timer als hij niet eerste of laatste staat.
        }

        ApplyCurrentRubberBandState(); // Past het rubber banding effect toe.
    }

    private void ApplyCurrentRubberBandState()
    {
        float targetMultiplier = 1f; // Normale snelheid.

        if (rubberBandState == 1)
            targetMultiplier = 1f - firstPlaceSpeedReduction; // Verlaagt de snelheid van de eerste plaats.
        else if (rubberBandState == 2)
            targetMultiplier = 1f + lastPlaceSpeedBoost; // Verhoogt de snelheid van de laatste plaats.

        currentRubberBandMultiplier = Mathf.Lerp(
            currentRubberBandMultiplier,
            targetMultiplier,
            rubberBandSmoothness * Time.fixedDeltaTime
        ); // Verandert de snelheid soepel.
    }

    private int GetRacePosition(GameObject[] racers)
    {
        float myProgress = GetRaceProgress(); // Haalt de voortgang van dit ruimteschip op.
        int position = 1; // Begint op de eerste positie.

        foreach (GameObject racer in racers) // Controleert iedere opponent.
        {
            if (racer == gameObject) // Controleert of het zichzelf is.
                continue;

            SpaceShipAI other = racer.GetComponent<SpaceShipAI>(); // Haalt het AI script van de andere opponent op.

            if (other == null) // Controleert of de andere opponent een AI script heeft.
                continue;

            float otherProgress = other.GetRaceProgress(); // Haalt de voortgang van de andere opponent op.

            if (otherProgress > myProgress) // Controleert of de andere opponent verder is.
                position++; // Verhoogt de positie.
        }

        return position; // Geeft de racepositie terug.
    }

    private float GetRaceProgress()
    {
        if (racePath == null) // Controleert of er een racebaan is.
            return 0f;

        int waypointCount = racePath.WaypointCount; // Haalt het aantal waypoints op.

        if (waypointCount == 0) // Controleert of er waypoints zijn.
            return 0f;

        RaceWaypoint current = racePath.GetWaypoint(currentWaypoint); // Haalt de huidige waypoint op.
        RaceWaypoint next = racePath.GetWaypoint((currentWaypoint + 1) % waypointCount); // Haalt de volgende waypoint op.

        if (current == null || next == null) // Controleert of beide waypoints bestaan.
        {
            return ((currentLap - 1) * waypointCount) + currentWaypoint; // Geeft de basis voortgang terug.
        }

        float segmentLength = Vector3.Distance(
            current.transform.position,
            next.transform.position
        ); // Berekent de afstand tussen twee waypoints.

        if (segmentLength < 0.01f) // Controleert of de afstand bijna nul is.
        {
            return ((currentLap - 1) * waypointCount) + currentWaypoint; // Geeft de basis voortgang terug.
        }

        float distanceFromWaypoint = Vector3.Distance(
            transform.position,
            current.transform.position
        ); // Berekent de afstand tot de huidige waypoint.

        float segmentProgress = 1f - Mathf.Clamp01(
            distanceFromWaypoint / segmentLength
        ); // Berekent hoeveel van het stuk is afgelegd.

        return ((currentLap - 1) * waypointCount) +
               currentWaypoint +
               segmentProgress; // Geeft de totale race voortgang terug.
    }

    public float GetPublicRaceProgress()
    {
        return GetRaceProgress(); // Geeft de race voortgang aan andere scripts.
    }

    public int GetCurrentLap()
    {
        return currentLap; // Geeft de huidige ronde terug.
    }

    public int GetCurrentWaypoint()
    {
        return currentWaypoint; // Geeft de huidige waypoint terug.
    }

    public int GetTotalLaps()
    {
        return totalLaps; // Geeft het totale aantal rondes terug.
    }

    public bool HasFinished()
    {
        return raceFinished; // Geeft aan of de race klaar is.
    }

    private void UpdateBattleTarget()
    {
        if (overtakeTimer > 0f) // Controleert of de opponent al aan het inhalen is.
        {
            overtakeTimer -= Time.fixedDeltaTime; // Telt de inhaaltijd af.
            return;
        }

        if (overtakeCooldownTimer > 0f) // Controleert of de inhaal cooldown actief is.
            return;

        GameObject closest = FindClosestRacerAhead(); // Zoekt de dichtstbijzijnde opponent voor zich.

        targetRacer = closest; // Slaat de gevonden opponent op.

        if (targetRacer == null) // Controleert of er geen doelwit is.
            return;

        float distance = Vector3.Distance(
            transform.position,
            targetRacer.transform.position
        ); // Berekent de afstand tot het doelwit.

        if (distance <= overtakingDistance) // Controleert of het doelwit dichtbij genoeg is.
        {
            overtakeSide = Random.value > 0.5f ? 1f : -1f; // Kiest willekeurig een kant om in te halen.
            overtakeTimer = overtakingCommitTime; // Start de inhaalactie.
            overtakeCooldownTimer = overtakingCommitTime + overtakingCooldown; // Start de cooldown.
        }
    }

    private GameObject FindClosestRacerAhead()
    {
        GameObject[] racers = GameObject.FindGameObjectsWithTag("Opponent"); // Zoekt alle opponents.

        GameObject closest = null; // Slaat de dichtstbijzijnde opponent op.
        float closestDistance = battleDistance; // Gebruikt battleDistance als maximale zoekafstand.
        float myProgress = GetRaceProgress(); // Haalt de eigen race voortgang op.

        foreach (GameObject racer in racers) // Controleert iedere opponent.
        {
            if (racer == gameObject) // Controleert of het zichzelf is.
                continue;

            SpaceShipAI other = racer.GetComponent<SpaceShipAI>(); // Haalt het AI script op.

            if (other == null) // Controleert of er geen AI script is.
                continue;

            float otherProgress = other.GetRaceProgress(); // Haalt de race voortgang van de andere op.
            float difference = otherProgress - myProgress; // Berekent het verschil in voortgang.

            if (difference <= 0f) // Controleert of de andere racer niet voor hem rijdt.
                continue;

            if (difference > racePath.WaypointCount / 2f) // Controleert of de andere racer niet te ver vooruit is.
                continue;

            float distance = Vector3.Distance(
                transform.position,
                racer.transform.position
            ); // Berekent de afstand tot de andere racer.

            if (distance < closestDistance) // Controleert of deze racer dichterbij is.
            {
                closestDistance = distance; // Slaat de nieuwe kortste afstand op.
                closest = racer; // Slaat deze racer op als doelwit.
            }
        }

        return closest; // Geeft de dichtstbijzijnde racer terug.
    }

    private Vector3 GetTargetPosition(RaceWaypoint waypoint)
    {
        Vector3 waypointPosition = waypoint.transform.position; // Haalt de positie van het waypoint op.
        Vector3 trackDirection = GetTrackDirection(); // Haalt de richting van de racebaan op.

        if (trackDirection.sqrMagnitude < 0.01f) // Controleert of de richting bijna nul is.
            return waypointPosition; // Gebruikt direct het waypoint.

        trackDirection.Normalize(); // Maakt de richting normaal.

        float distanceToWaypoint = Vector3.Distance(
            rb.position,
            waypointPosition
        ); // Berekent de afstand tot het waypoint.

        float waypointSafetyDistance = Mathf.Max(
            waypoint.reachDistance * 3f,
            4f
        ); // Berekent een veilige afstand rond het waypoint.

        if (distanceToWaypoint <= waypointSafetyDistance) // Controleert of het schip dicht bij het waypoint is.
            return waypointPosition; // Gaat direct naar het waypoint.

        Vector3 side = Vector3.Cross(
            Vector3.up,
            trackDirection
        ).normalized; // Berekent de zijkant van de racebaan.

        float offset = racingLineOffset; // Gebruikt de willekeurige race-lijn.

        if (overtakeTimer > 0f && targetRacer != null) // Controleert of de opponent aan het inhalen is.
        {
            float safetyFactor = Mathf.InverseLerp(
                waypointSafetyDistance * 2f,
                waypointSafetyDistance,
                distanceToWaypoint
            ); // Berekent hoe sterk de inhaal offset moet zijn.

            float overtakeOffset = overtakingOffset * safetyFactor; // Berekent hoeveel de opponent opzij gaat.

            offset += overtakeSide * overtakeOffset; // Voegt de inhaalrichting toe.
        }

        float maximumOffset =
            overtakingOffset +
            randomRacingLineOffset; // Berekent de maximale offset.

        offset = Mathf.Clamp(
            offset,
            -maximumOffset,
            maximumOffset
        ); // Zorgt ervoor dat de offset niet te groot wordt.

        Vector3 target =
            waypointPosition +
            side * offset; // Berekent de uiteindelijke doelpositie.

        float maxTargetDistance = Mathf.Max(
            waypoint.reachDistance * 2f,
            8f
        ); // Berekent de maximale afstand van het doel.

        float targetDistance = Vector3.Distance(
            waypointPosition,
            target
        ); // Berekent de afstand van het doel tot het waypoint.

        if (targetDistance > maxTargetDistance) // Controleert of het doel te ver van het waypoint ligt.
        {
            target =
                waypointPosition +
                (target - waypointPosition).normalized *
                maxTargetDistance; // Brengt het doel dichter naar het waypoint.
        }

        return target; // Geeft de uiteindelijke doelpositie terug.
    }

    private Vector3 GetTrackDirection()
    {
        int nextIndex =
            (currentWaypoint + 1) %
            racePath.WaypointCount; // Berekent de volgende waypoint index.

        RaceWaypoint current =
            racePath.GetWaypoint(currentWaypoint); // Haalt de huidige waypoint op.

        RaceWaypoint next =
            racePath.GetWaypoint(nextIndex); // Haalt de volgende waypoint op.

        if (current == null || next == null) // Controleert of de waypoints bestaan.
            return transform.forward; // Gebruikt de huidige richting.

        Vector3 direction =
            next.transform.position -
            current.transform.position; // Berekent de richting van de baan.

        if (direction.sqrMagnitude < 0.01f) // Controleert of de richting bijna nul is.
            return transform.forward; // Gebruikt de huidige richting.

        return direction.normalized; // Geeft de normale baanrichting terug.
    }

    private bool HasReachedWaypoint(RaceWaypoint waypoint) // Controleert of het ruimteschip het waypoint heeft bereikt.
    {
        Vector3 waypointPosition = waypoint.transform.position; // Haalt de positie van het waypoint op.

        float distance = Vector3.Distance(rb.position, waypointPosition); // Berekent de afstand tussen het schip en het waypoint.

        if (distance <= waypoint.reachDistance) // Als het schip binnen de reachDistance is, is het waypoint bereikt.
            return true;

        if (distance <= waypoint.reachDistance * 1.5f) // Extra controle als het schip iets verder van het waypoint is.
        {
            Vector3 toWaypoint = waypointPosition - rb.position; // Berekent de richting van het schip naar het waypoint.

            if (toWaypoint.sqrMagnitude > 0.01f) // Controleert of de afstand groot genoeg is.
            {
                float dot = Vector3.Dot(transform.forward, toWaypoint.normalized); // Controleert of het schip richting het waypoint kijkt.

                if (dot > 0.1f) // Als het schip richting het waypoint kijkt, telt het als bereikt.
                    return true;
            }
        }

        return false; // Het waypoint is nog niet bereikt.
    }

    private Vector3 GetAvoidanceDirection() // zorgt ervoor dat de opponents elkaar niet beuken en uitwijken als ze dichtbij komen.
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

        return avoidance; // Geeft de richting terug waarin de opponent moet uitwijken.
    }

    private void RotateShip(Vector3 direction) // zorgt ervoor dat de opponent roteert.
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

        rb.MoveRotation(newRotation); // Draait de opponent naar de gewenste richting.
    }

    private void MoveShip() // zorgt ervoor dat de opponent beweegt.
    {
        Vector3 velocity =
            transform.forward *
            currentSpeed;

        if (useRigidbodyVelocity)
        {
            rb.linearVelocity = velocity; // Beweegt de opponent met de Rigidbody.
        }
        else
        {
            rb.MovePosition(
                rb.position +
                velocity * Time.fixedDeltaTime
            ); // Verplaatst de opponent handmatig.
        }
    }

    private void NextWaypoint() // checkt naar welke waypoint hij moet. finished de race als het klaar is.
    {
        currentWaypoint++; // Gaat naar de volgende waypoint.

        if (currentWaypoint >= racePath.WaypointCount) // Controleert of het einde van de baan bereikt is.
        {
            currentWaypoint = 0; // Gaat terug naar de eerste waypoint.
            currentLap++; // Gaat naar de volgende ronde.

            if (currentLap > totalLaps) // Controleert of alle rondes klaar zijn.
                FinishRace(); // Beëindigt de race.
        }
    }

    private void FinishRace() // zorgt ervoor dat de speler finished. RaceManager.cs gebruikt dit.
    {
        raceFinished = true; // Zet de race van deze opponent op finished.
        currentSpeed = 0f; // Zet de snelheid op nul.
        targetRacer = null; // Verwijdert het huidige doelwit.
        overtakeTimer = 0f; // Stopt de inhaalactie.

        if (rb != null) // Controleert of er een Rigidbody is.
        {
            rb.linearVelocity = Vector3.zero; // Stopt de beweging.
            rb.angularVelocity = Vector3.zero; // Stopt het draaien.
        }
    }

    private void ReturnToStartingGrid() // hiermee gaat de Opponent terug naar zijn starting point.
    {
        if (startingGridPosition == null) // Controleert of er een startpositie is.
        {
            StopShip(); // Stopt de opponent.
            return;
        }

        Vector3 target =
            startingGridPosition.position; // Haalt de startpositie op.

        Vector3 difference =
            target -
            rb.position; // Berekent de richting naar de startpositie.

        float distance =
            difference.magnitude; // Berekent de afstand tot de startpositie.

        if (distance <= gridReachDistance) // Controleert of de opponent bij de startpositie is.
        {
            StopShip(); // Stopt de opponent.

            Quaternion rotation =
                Quaternion.Slerp(
                    rb.rotation,
                    startingGridPosition.rotation,
                    3f * Time.fixedDeltaTime
                ); // Draait de opponent rustig naar de startrotatie.

            rb.MoveRotation(rotation); // Past de rotatie toe.
            return;
        }

        Vector3 direction =
            difference.normalized; // Berekent de richting naar de startpositie.

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            ); // Berekent de gewenste rotatie.

        Quaternion newRotation =
            Quaternion.Slerp(
                rb.rotation,
                desiredRotation,
                3f * Time.fixedDeltaTime
            ); // Draait de opponent rustig naar de startpositie.

        rb.MoveRotation(newRotation); // Past de rotatie toe.

        rb.linearVelocity =
            direction *
            gridMoveSpeed; // Beweegt de opponent terug naar de startpositie.
    }

    private void StopShip()
    {
        currentSpeed = 0f; // Zet de snelheid op nul.
        rb.linearVelocity = Vector3.zero; // Stopt de beweging.
        rb.angularVelocity = Vector3.zero; // Stopt het draaien.
    }

    private void OnDrawGizmosSelected() // Laat zien welke Waypoint de opponent wilt bereiken.
    {
        if (racePath != null &&
            racePath.WaypointCount > 0)
        {
            RaceWaypoint waypoint =
                racePath.GetWaypoint(currentWaypoint); // Haalt de huidige waypoint op.

            if (waypoint != null) // Controleert of de waypoint bestaat.
            {
                Gizmos.color = Color.red; // Maakt de Gizmo rood.

                Gizmos.DrawLine(
                    transform.position,
                    waypoint.transform.position
                ); // Laat een lijn naar het waypoint zien.

                Gizmos.DrawWireSphere(
                    transform.position,
                    battleDistance
                ); // Laat de battle afstand zien.
            }
        }

        if (startingGridPosition != null) // Controleert of er een startpositie is.
        {
            Gizmos.color = Color.green; // Maakt de Gizmo groen.

            Gizmos.DrawWireSphere(
                startingGridPosition.position,
                gridReachDistance
            ); // Laat de afstand van de startpositie zien.
        }
    }
}