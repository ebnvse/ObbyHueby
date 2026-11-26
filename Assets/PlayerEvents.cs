using System;
using UnityEngine;

public class PlayerEvents : MonoBehaviour
{
    public static event Action<int> OnPlayerLevelChanged;

    public static void InvokePlayerLevelChanged(int newPlayerLevel)
    {
        OnPlayerLevelChanged?.Invoke(newPlayerLevel);
    }
}
