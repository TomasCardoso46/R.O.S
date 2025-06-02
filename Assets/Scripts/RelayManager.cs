using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.Threading.Tasks;

public class MultiplayerRelayManager : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinInput;
    [SerializeField] private TextMeshProUGUI codeText;

    private const int MaxConnections = 4;

    private async void Start()
    {
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(JoinGame);

        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in anonymously.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services or authenticate: {e.Message}");
        }
    }

    private async void HostGame()
    {
        try
        {
            Debug.Log("Creating Relay allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            Debug.Log("Allocation created.");

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Join code generated: {joinCode}");

            // Setup transport with Relay data
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.ConnectionData, // hostConnectionData same as connectionData for host
                true // use DTLS
            );

            codeText.text = $"Join Code: {joinCode}";

            bool started = NetworkManager.Singleton.StartHost();
            Debug.Log($"Host started: {started}");
            if (!started)
                Debug.LogError("Failed to start host!");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"Relay error while hosting: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error while hosting: {ex.Message}");
        }
    }

    private async void JoinGame()
    {
        string joinCode = joinInput.text.Trim();

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("Join code is empty.");
            return;
        }

        try
        {
            Debug.Log($"Trying to join with code: {joinCode}");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log("JoinAllocation received successfully.");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                true // use DTLS
            );
            Debug.Log("SetRelayServerData called successfully.");

            bool started = NetworkManager.Singleton.StartClient();
            Debug.Log($"StartClient called, success? {started}");
            if (!started)
                Debug.LogError("Failed to start client!");
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"Relay error while joining: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error while joining: {ex.Message}");
        }
    }
}
