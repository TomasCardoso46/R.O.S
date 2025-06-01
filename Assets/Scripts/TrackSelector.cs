using UnityEngine;
using Unity.Netcode;

public class TrackSelector : NetworkBehaviour
{
    [Header("List of Track Prefabs (Must be in NetworkManager's Network Prefabs)")]
    public GameObject[] trackPrefabs;

    [Header("Location to spawn the selected track")]
    public Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SelectAndSpawnTrack();
        }
    }

    void SelectAndSpawnTrack()
    {
        if (trackPrefabs.Length == 0 || spawnPoint == null)
        {
            Debug.LogWarning("TrackSelector: Missing track prefabs or spawn point.");
            return;
        }

        int randomIndex = Random.Range(0, trackPrefabs.Length);
        GameObject selectedTrack = trackPrefabs[randomIndex];

        GameObject trackInstance = Instantiate(selectedTrack, spawnPoint.position, Quaternion.identity);
        NetworkObject netObj = trackInstance.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("Track prefab is missing a NetworkObject component!");
            return;
        }

        Debug.Log("[TrackSelector] Track spawned successfully.");
        netObj.Spawn();
    }
}
