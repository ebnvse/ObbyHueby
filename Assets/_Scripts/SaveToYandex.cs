using UnityEngine;
using TMPro;
using System;

public class SaveToYandex : MonoBehaviour
{
    public PlayerStats playerStats;

    public static event Action OnLoadStats;

    public void UpdateSavesStats()
    {
        OnLoadStats?.Invoke();
    }

}
