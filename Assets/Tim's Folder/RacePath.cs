using UnityEngine;

public class RacePath : MonoBehaviour
{
    [Header("Waypoints")]
    public RaceWaypoint[] waypoints;

    public int WaypointCount
    {
        get { return waypoints.Length; }
    }

    public RaceWaypoint GetWaypoint(int index)
    {
        if (waypoints.Length == 0)
            return null;

        return waypoints[index % waypoints.Length];
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = Color.green;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            RaceWaypoint current = waypoints[i];
            RaceWaypoint next = waypoints[(i + 1) % waypoints.Length];

            if (next != null)
            {
                Gizmos.DrawLine(
                    current.transform.position,
                    next.transform.position
                );
            }
        }
    }
}