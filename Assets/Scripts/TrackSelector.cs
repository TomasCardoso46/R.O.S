using UnityEngine;

public class TrackSelector : MonoBehaviour
{
    [Header("List of prefab tracks to choose from")]
    public GameObject[] trackPrefabs;

    [Header("Location to spawn the selected track")]
    public Transform spawnPoint;

    void Start()
    {
        if (trackPrefabs.Length == 0 || spawnPoint == null)
        {
            Debug.LogWarning("TrackSelector: Missing track prefabs or spawn point.");
            return;
        }

        // Randomly select a track prefab
        int randomIndex = Random.Range(0, trackPrefabs.Length);
        GameObject selectedTrack = trackPrefabs[randomIndex];

        // Instantiate the selected track at the spawn point
        Instantiate(selectedTrack, spawnPoint.position, Quaternion.identity);
    }
}
