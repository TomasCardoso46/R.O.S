using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class PathFollower : NetworkBehaviour
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
    public float cornerSpeed;
    public TireType tireType = TireType.Medium;

    [Header("Lap Counter")]
    public NetworkVariable<int> raceLap = new();
    public NetworkVariable<int> tireLap = new();

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
    private float baseCornerSpeed;
    private float pushCornerSpeed;
    public bool isPushing = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        baseCornerSpeed = cornerSpeed;
        pushCornerSpeed = baseCornerSpeed / 1.25f;

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
        if (!IsServer) return;

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

        if (Input.GetKeyDown(KeyCode.S)) { SoftTiresServerRpc(); }
        if (Input.GetKeyDown(KeyCode.M)) { MediumTiresServerRpc(); }
        if (Input.GetKeyDown(KeyCode.H)) { HardTiresServerRpc(); }
        if (Input.GetKeyDown(KeyCode.P)) { TogglePushServerRpc(); }
    }

    float GetLapTimeModifier(TireType type)
    {
        return type switch
        {
            TireType.Soft => -0.45f,
            TireType.Medium => 0f,
            TireType.Hard => 0.45f,
            _ => 0f
        };
    }

    float GetDegradationModifier(TireType type)
    {
        float modifier = type switch
        {
            TireType.Soft => 0.003f,
            TireType.Medium => 0.002f,
            TireType.Hard => 0.001f,
            _ => 0.003f
        };

        if (isPushing) modifier *= 1.75f;
        return modifier;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Checkpoint"))
        {
            tireDegradationPenalty += GetDegradationModifier(tireType);
            speed = noDegSpeed - tireDegradationPenalty;
        }

        if (other.CompareTag("StartFinishLine"))
        {
            raceLap.Value++;
            tireLap.Value++;
        }
        else if (other.CompareTag("BreakingPoint"))
        {
            speed = speed / cornerSpeed;
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

    [ServerRpc]
    public void SoftTiresServerRpc() { pitTire = TireType.Soft; pitRequested = true; }

    [ServerRpc]
    public void MediumTiresServerRpc() { pitTire = TireType.Medium; pitRequested = true; }

    [ServerRpc]
    public void HardTiresServerRpc() { pitTire = TireType.Hard; pitRequested = true; }

    [ServerRpc]
    public void TogglePushServerRpc()
    {
        isPushing = !isPushing;
        if (isPushing)
        {
            cornerSpeed = pushCornerSpeed;
        }
        else
        {
            cornerSpeed = baseCornerSpeed;
        }
    }
}
