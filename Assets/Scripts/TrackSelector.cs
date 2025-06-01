using UnityEngine;
using Unity.Netcode;

public class TrackSelector : NetworkBehaviour
{
    [Header("List of prefab tracks to choose from")]
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
        if (trackPrefabs == null || trackPrefabs.Length == 0)
        {
            Debug.LogWarning("TrackSelector: No track prefabs assigned.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("TrackSelector: Spawn point not assigned.");
            return;
        }

        int randomIndex = Random.Range(0, trackPrefabs.Length);
        GameObject selectedTrack = trackPrefabs[randomIndex];

        GameObject track = Instantiate(selectedTrack, spawnPoint.position, Quaternion.identity);

        NetworkObject netObj = track.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Track prefab must have a NetworkObject component.");
            Destroy(track);
            return;
        }

        netObj.Spawn();
    }
}
