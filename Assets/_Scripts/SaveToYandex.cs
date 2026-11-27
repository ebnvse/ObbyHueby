using UnityEngine;
using System;
using YG;

public class SaveToYandex : MonoBehaviour
{
    public PlayerStats playerStats;
    public Money money;

    public static event Action OnLoadStats;

    public void UpdateSavesStats()
    {
        if (money != null)
            money.WriteMoneyToSaves();

        if (playerStats != null)
        {
            playerStats.SaveStats();
        }
        else
        {
            YG2.SaveProgress();
        }

        OnLoadStats?.Invoke();
    }

    public static void NotifyStatsLoaded()
    {
        OnLoadStats?.Invoke();
    }
}