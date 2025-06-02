using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PathFollower : NetworkBehaviour
{
    public enum TireType { Soft, Medium, Hard }

    [Header("Path Settings")]
    public Transform[] waypoints;

    [Header("Lap Time Settings")]
    public float baseLapTime = 10f;
    public float cornerSpeed;
    public NetworkVariable<TireType> tireType = new(TireType.Medium, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Lap Counter")]
    public NetworkVariable<int> raceLap = new();
    public NetworkVariable<int> tireLap = new();

    [Header("Pit Info")]
    public NetworkVariable<bool> pitRequested = new();
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
    private NetworkVariable<bool> isPushing = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool canMove = false;
    private bool hasWon = false;

    // UI Texts (local)
    private TextMeshProUGUI pitStatusText;
    private TextMeshProUGUI pushStatusText;
    private TextMeshProUGUI tireCompoundText;
    private TextMeshProUGUI tireAgeText;
    private TextMeshProUGUI currentPlayerText;

    // UI Texts (shared)
    private TextMeshProUGUI player1LapText;
    private TextMeshProUGUI player2LapText;

    // Victory UI roots
    private GameObject player1Victory;
    private GameObject player2Victory;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            pitStatusText = FindTextByTag("PitText");
            pushStatusText = FindTextByTag("PushText");
            tireCompoundText = FindTextByTag("TireText");
            tireAgeText = FindTextByTag("TireAgeText");
            currentPlayerText = FindTextByTag("CurrentPlayer");

            player1LapText = FindTextByTag("Player1Lap");
            player2LapText = FindTextByTag("Player2Lap");

            player1Victory = GameObject.FindGameObjectWithTag("Player1Victory");
            player2Victory = GameObject.FindGameObjectWithTag("Player2Victory");

            if (currentPlayerText != null)
                currentPlayerText.text = IsHost ? "Blue" : "Red";

            if (player1Victory != null && player1Victory.transform.childCount > 0)
                player1Victory.transform.GetChild(0).gameObject.SetActive(false);

            if (player2Victory != null && player2Victory.transform.childCount > 0)
                player2Victory.transform.GetChild(0).gameObject.SetActive(false);
        }

        if (!IsServer) return;

        baseCornerSpeed = cornerSpeed;
        pushCornerSpeed = baseCornerSpeed / 1.25f;

        GameObject waypointContainer = GameObject.FindGameObjectWithTag("Waypoints");
        if (waypointContainer != null)
        {
            int count = waypointContainer.transform.childCount;
            waypoints = new Transform[count];
            for (int i = 0; i < count; i++)
                waypoints[i] = waypointContainer.transform.GetChild(i);
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

        adjustedLapTime = baseLapTime + GetLapTimeModifier(tireType.Value);
        noDegSpeed = totalDistance / adjustedLapTime;
        speed = noDegSpeed;

        AssignSpawnPosition();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        CheckPlayersConnected();
    }

    private TextMeshProUGUI FindTextByTag(string tag)
    {
        GameObject obj = GameObject.FindGameObjectWithTag(tag);
        if (obj != null)
        {
            var text = obj.GetComponent<TextMeshProUGUI>();
            if (text == null)
                Debug.LogWarning($"{tag} object found but no TextMeshProUGUI attached.");
            return text;
        }
        else
        {
            Debug.LogWarning($"No GameObject found with tag '{tag}'.");
            return null;
        }
    }

    private void AssignSpawnPosition()
    {
        Transform startFinish = null;
        foreach (var wp in waypoints)
        {
            if (wp.CompareTag("StartFinishLine"))
            {
                startFinish = wp;
                break;
            }
        }

        if (startFinish == null)
        {
            Debug.LogWarning("StartFinishLine waypoint not found! Using first waypoint as fallback.");
            startFinish = waypoints[0];
        }

        Vector3 basePos = startFinish.position;

        if (OwnerClientId == 0)
            transform.position = basePos;
        else if (OwnerClientId == 1)
            transform.position = basePos + new Vector3(1f, 0f, 0f);
        else
            transform.position = basePos;
    }

    private void OnClientConnected(ulong clientId)
    {
        CheckPlayersConnected();
    }

    private void CheckPlayersConnected()
    {
        canMove = NetworkManager.Singleton.ConnectedClientsList.Count >= 2;
    }

    void Update()
    {
        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.S)) SoftTiresServerRpc();
            if (Input.GetKeyDown(KeyCode.M)) MediumTiresServerRpc();
            if (Input.GetKeyDown(KeyCode.H)) HardTiresServerRpc();
            if (Input.GetKeyDown(KeyCode.P)) TogglePushServerRpc();

            if (pitStatusText != null)
                pitStatusText.text = pitRequested.Value ? "In" : "Out";

            if (pushStatusText != null)
                pushStatusText.text = isPushing.Value ? "Pushing" : "Race Pace";

            if (tireCompoundText != null)
            {
                tireCompoundText.text = tireType.Value switch
                {
                    TireType.Soft => "Softs",
                    TireType.Medium => "Mediums",
                    TireType.Hard => "Hards",
                    _ => ""
                };
            }

            if (tireAgeText != null)
                tireAgeText.text = tireLap.Value.ToString();

            if (player1LapText != null)
                player1LapText.text = $"{raceLap.Value}";

            if (player2LapText != null)
                player2LapText.text = $"{raceLap.Value}";
        }

        if (!IsServer || !canMove || waypoints == null || waypoints.Length < 2 || hasWon)
            return;

        adjustedLapTime = baseLapTime + GetLapTimeModifier(tireType.Value);
        noDegSpeed = totalDistance / adjustedLapTime;

        Transform nextWaypoint = waypoints[(currentIndex + 1) % waypoints.Length];
        float step = speed * Time.deltaTime;

        transform.position = Vector2.MoveTowards(transform.position, nextWaypoint.position, step);

        if (Vector2.Distance(transform.position, nextWaypoint.position) < 0.01f)
            currentIndex = (currentIndex + 1) % waypoints.Length;
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

        if (isPushing.Value) modifier *= 1.75f;
        return modifier;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || hasWon) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Checkpoint"))
        {
            tireDegradationPenalty += GetDegradationModifier(tireType.Value);
            speed = noDegSpeed - tireDegradationPenalty;
        }

        if (other.CompareTag("StartFinishLine"))
        {
            raceLap.Value++;
            tireLap.Value++;

            if (raceLap.Value >= 50 && !hasWon)
            {
                hasWon = true;
                speed = 0;
                ShowVictoryClientRpc(IsHost);
            }
        }
        else if (other.CompareTag("BreakingPoint"))
        {
            speed = speed / cornerSpeed;
        }
        else if (other.CompareTag("CornerExit"))
        {
            speed = noDegSpeed - tireDegradationPenalty;
        }
        else if (other.CompareTag("Pit") && pitRequested.Value)
        {
            PitStop(pitTire);
            pitRequested.Value = false;
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
        tireType.Value = tireSet;
        tireDegradationPenalty = 0;
        tireLap.Value = 0;
    }

    [ServerRpc] public void SoftTiresServerRpc() => RequestPit(TireType.Soft);
    [ServerRpc] public void MediumTiresServerRpc() => RequestPit(TireType.Medium);
    [ServerRpc] public void HardTiresServerRpc() => RequestPit(TireType.Hard);

    private void RequestPit(TireType type)
    {
        pitTire = type;
        pitRequested.Value = true;
    }

    [ServerRpc]
    public void TogglePushServerRpc()
    {
        isPushing.Value = !isPushing.Value;
        cornerSpeed = isPushing.Value ? pushCornerSpeed : baseCornerSpeed;
    }

    [ClientRpc]
    private void ShowVictoryClientRpc(bool isPlayer1)
    {
        if (isPlayer1)
        {
            if (player1Victory != null && player1Victory.transform.childCount > 0)
                player1Victory.transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {
            if (player2Victory != null && player2Victory.transform.childCount > 0)
                player2Victory.transform.GetChild(0).gameObject.SetActive(true);
        }

        canMove = false;
        speed = 0;
    }
}
