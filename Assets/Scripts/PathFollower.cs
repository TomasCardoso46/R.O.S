using System.Collections;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public enum TireType
    {
        Soft,
        Medium,
        Hard
    }

    [Header("Path Settings")]
    public Transform[] waypoints;

    [Header("Lap Time Settings")]
    public float baseLapTime = 10f;
    public TireType tireType = TireType.Medium;

    [Header("Lap Counter")]
    public int raceLap = 0;
    public int tireLap = 0;

    [Header("Pit Info")]
    public bool pitRequested = false;
    public TireType pitTire;
    public float pitTime;

    private int currentIndex = 0;
    private float speed;
    private float noDegSpeed;
    private float totalDistance;
    private float[] segmentLengths;
    private float tireDegradationPenalty = 0;
    private float adjustedLapTime;

    void Awake()
    {
        // Auto-assign waypoints from a tagged GameObject
        GameObject waypointContainer = GameObject.FindGameObjectWithTag("Waypoints");
        if (waypointContainer != null)
        {
            int count = waypointContainer.transform.childCount;
            waypoints = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                waypoints[i] = waypointContainer.transform.GetChild(i);
            }
        }

        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning("Not enough waypoints set for PathFollower.");
            return;
        }

        // Calculate total path length and segment lengths
        totalDistance = 0f;
        segmentLengths = new float[waypoints.Length];

        for (int i = 0; i < waypoints.Length; i++)
        {
            int nextIndex = (i + 1) % waypoints.Length;
            float segmentLength = Vector2.Distance(waypoints[i].position, waypoints[nextIndex].position);
            segmentLengths[i] = segmentLength;
            totalDistance += segmentLength;
        }

        adjustedLapTime = baseLapTime + GetLapTimeModifier(tireType);
        noDegSpeed = totalDistance / adjustedLapTime;
        speed = noDegSpeed;
    }

    void Update()
    {
        adjustedLapTime = baseLapTime + GetLapTimeModifier(tireType);
        noDegSpeed = totalDistance / adjustedLapTime;

        if (waypoints == null || waypoints.Length < 2) return;

        Transform nextWaypoint = waypoints[(currentIndex + 1) % waypoints.Length];
        float step = speed * Time.deltaTime;

        transform.position = Vector2.MoveTowards(transform.position, nextWaypoint.position, step);

        if (Vector2.Distance(transform.position, nextWaypoint.position) < 0.01f)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SoftTires();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            MediumTires();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            HardTires();
        }
    }

    float GetLapTimeModifier(TireType type)
    {
        switch (type)
        {
            case TireType.Soft: return -0.45f;
            case TireType.Medium: return 0f;
            case TireType.Hard: return 0.45f;
            default: return 0f;
        }
    }

    float GetDegradationModifier(TireType type)
    {
        switch (type)
        {
            case TireType.Soft: return 0.003f;
            case TireType.Medium: return 0.002f;
            case TireType.Hard: return 0.001f;
            default: return 0.3f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.gameObject.layer == LayerMask.NameToLayer("Checkpoint"))
        {
            tireDegradationPenalty += GetDegradationModifier(tireType);
            speed = noDegSpeed - tireDegradationPenalty;
        }

        if (other.CompareTag("StartFinishLine"))
        {
            raceLap++;
            Debug.Log("Race Lap Count: " + raceLap);
            tireLap++;
            Debug.Log("Tire Lap Count: " + tireLap);

            //tireDegradationPenalty += GetDegradationModifier(tireType);
            //speed = noDegSpeed - tireDegradationPenalty;
        }

        else if (other.CompareTag("BreakingPoint"))
        {
            speed = speed / 2;
        }

        else if (other.CompareTag("CornerExit"))
        {
            speed = noDegSpeed - tireDegradationPenalty;
        }



        else if (other.CompareTag("Pit") && pitRequested)
        {
            PitStop(pitTire);
            pitRequested = false;
        }
    }

    IEnumerator Stop(float seconds)
    {
        speed = 0;
        yield return new WaitForSeconds(seconds);
        speed = noDegSpeed;
    }

    private void PitStop(TireType tireSet)
    {
        StartCoroutine(Stop(pitTime));
        tireType = tireSet;
        tireDegradationPenalty = 0;
    }

    public void SoftTires()
    {
        pitTire = TireType.Soft;
        pitRequested = true;
    }

    public void MediumTires()
    {
        pitTire = TireType.Medium;
        pitRequested = true;
    }

    public void HardTires()
    {
        pitTire = TireType.Hard;
        pitRequested = true;
    }
}
