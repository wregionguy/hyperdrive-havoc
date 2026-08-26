using UnityEngine;

public class RaceWaypoint : MonoBehaviour
{
    [Header("AI Settings")]
    [Range(0.1f, 1f)]
    public float speedMultiplier = 1f;

    [Tooltip("How close the ship needs to get before moving to the next waypoint.")]
    public float reachDistance = 10f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 1f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, reachDistance);
    }
}