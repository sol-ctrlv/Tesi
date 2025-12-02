using Edgar.Legacy.Utils;
using System.Collections.Generic;
using UnityEngine;

public class RewardsManager : MonoBehaviour
{
    public static RewardsManager Instance { get; private set; }
    [SerializeField] private GameObject[] powerUpPrefabs;
    public Queue<GameObject> availablePowerUps;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance);
        }

        Instance = this;

        powerUpPrefabs.Shuffle(new System.Random());

        availablePowerUps = new Queue<GameObject>();

        for (int i = 0; i < powerUpPrefabs.Length; i++)
        {
            availablePowerUps.Enqueue(powerUpPrefabs[i]);
        }

    }

    public GameObject GetPowerUp()
    {
        return availablePowerUps.Dequeue();
    }

}
