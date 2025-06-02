using Unity.Netcode;
using UnityEngine;

public class CarManager : NetworkBehaviour
{
    public GameObject hostCarPrefab;   // Prefab for the host
    public GameObject clientCarPrefab; // Prefab for clients

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnCarForClient(clientId);
            }

            NetworkManager.Singleton.OnClientConnectedCallback += SpawnCarForClient;
        }
    }

    private void SpawnCarForClient(ulong clientId)
    {
        GameObject prefabToSpawn;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            prefabToSpawn = hostCarPrefab;
        }
        else
        {
            prefabToSpawn = clientCarPrefab;
        }

        GameObject carInstance = Instantiate(prefabToSpawn);

        // Set position to Start/Finish line if found
        GameObject startFinish = GameObject.FindGameObjectWithTag("StartFinishLine");
        if (startFinish != null)
        {
            carInstance.transform.position = startFinish.transform.position;
            carInstance.transform.rotation = startFinish.transform.rotation;
        }
        else
        {
            Debug.LogWarning("StartFinishLine not found. Using default spawn position.");
        }

        NetworkObject netObj = carInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId);
        }
        else
        {
            Debug.LogError("Car prefab is missing a NetworkObject component.");
        }
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnCarForClient;
        }
    }
}
