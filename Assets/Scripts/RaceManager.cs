using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class RaceManager : NetworkBehaviour
{
    public int lapToWin = 50;

    private TextMeshProUGUI player1VictoryText;
    private TextMeshProUGUI player2VictoryText;

    private bool gameEnded = false;

    private List<PathFollower> cars = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        player1VictoryText = FindTextByTag("Player1Victory");
        player2VictoryText = FindTextByTag("Player2Victory"); // note: check spelling of this tag in your scene!

        if (player1VictoryText != null) player1VictoryText.gameObject.SetActive(false);
        if (player2VictoryText != null) player2VictoryText.gameObject.SetActive(false);

        cars = new List<PathFollower>(FindObjectsOfType<PathFollower>());
    }

    private TextMeshProUGUI FindTextByTag(string tag)
    {
        GameObject obj = GameObject.FindGameObjectWithTag(tag);
        if (obj != null)
        {
            return obj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning($"No GameObject found with tag '{tag}'.");
            return null;
        }
    }

    private void Update()
    {
        if (!IsServer || gameEnded) return;

        foreach (PathFollower car in cars)
        {
            if (car.raceLap.Value >= lapToWin)
            {
                ulong winnerId = car.OwnerClientId;
                EndRace(winnerId);
                break;
            }
        }
    }

    private void EndRace(ulong winnerClientId)
    {
        gameEnded = true;
        // Show victory UI
        if (winnerClientId == 0)
        {
            Debug.Log("Blue Wins");
            player1VictoryText.gameObject.SetActive(true);
        }
        else if (winnerClientId == 1)
        {
            Debug.Log("Red Wins");
            player2VictoryText.gameObject.SetActive(true);
        }
    }
}
