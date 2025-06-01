using Unity.Netcode;
using UnityEngine;

public class CarManager : NetworkBehaviour
{
    public GameObject carPrefab; // assign prefab with PathFollower + NetworkObject

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
        GameObject carInstance = Instantiate(carPrefab);

        // Find start/finish line position
        GameObject startFinish = GameObject.FindGameObjectWithTag("StartFinishLine");
        if (startFinish != null)
        {
            carInstance.transform.position = startFinish.transform.position;
            carInstance.transform.rotation = startFinish.transform.rotation;
        }
        else
        {
            Debug.LogWarning("StartFinishLine not found in scene. Using default spawn position.");
        }

        NetworkObject netObj = carInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId);
        }
        else
        {
            Debug.LogError("Car prefab missing NetworkObject component.");
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
