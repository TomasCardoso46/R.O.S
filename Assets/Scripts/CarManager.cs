using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarManager : MonoBehaviour
{
    [Header("List of Car Prefabs")]
    public List<GameObject> carPrefabs;

    private Vector2 spawnPosition;
    public float seconds;

    void Start()
    {
        StartCoroutine(SpawnCarsAfterDelay(seconds));
    }

    IEnumerator SpawnCarsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameObject startFinishLine = GameObject.FindGameObjectWithTag("StartFinishLine");

        if (startFinishLine != null)
        {
            spawnPosition = startFinishLine.transform.position;
        }
        else
        {
            Debug.LogWarning("No object with tag 'StartFinishLine' found. Using (0, 0) as fallback position.");
            spawnPosition = Vector2.zero;
        }

        foreach (GameObject carPrefab in carPrefabs)
        {
            if (carPrefab != null)
            {
                Instantiate(carPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }
}
