using Unity.Netcode;
using UnityEngine;

public class CarManager : NetworkBehaviour
{
    [Header("List of Car Prefabs")]
    public GameObject[] carPrefabs;

    private Vector3 spawnPosition;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GameObject startFinishLine = GameObject.FindGameObjectWithTag("StartFinishLine");

            if (startFinishLine != null)
                spawnPosition = startFinishLine.transform.position;
            else
                spawnPosition = Vector3.zero;

            SpawnCarForClient(OwnerClientId);
        }
    }

    void SpawnCarForClient(ulong clientId)
    {
        int prefabIndex = 0; // You can add logic here to select different cars
        if (carPrefabs == null || carPrefabs.Length == 0)
        {
            Debug.LogError("CarManager: No car prefabs assigned.");
            return;
        }

        GameObject carPrefab = carPrefabs[prefabIndex];
        GameObject carInstance = Instantiate(carPrefab, spawnPosition, Quaternion.identity);

        NetworkObject netObj = carInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Car prefab must have a NetworkObject component.");
            Destroy(carInstance);
            return;
        }

        netObj.SpawnAsPlayerObject(clientId);
    }
}
