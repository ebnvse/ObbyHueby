using UnityEngine;
using TMPro;

public class PlayerGeerScore : MonoBehaviour
{
    public PlayerStats playerStats;
    public int geerScore_Player;
    public TextMeshProUGUI geerScore_text;

    private void OnEnable()
    {
        PlayerStats.OnStatsUpdated += CalculateGeerScore;
    }

    private void Start()
    {
        CalculateGeerScore();
    }

    private void OnDisable()
    {
        PlayerStats.OnStatsUpdated -= CalculateGeerScore;
    }

    public void RefreashGeerScore()
    {
        if (geerScore_text != null)
            geerScore_text.text = $"Боевая мощь: {geerScore_Player}";
    }

    public void CalculateGeerScore()
    {
        if (playerStats == null) return;

        int atkSpeedToInt = Mathf.RoundToInt(playerStats.AtkSpped_Player);
        int speedToInt = Mathf.RoundToInt(playerStats.Speed_Player);

        geerScore_Player = playerStats.AtkDmg_Player + playerStats.Def_Player
            + playerStats.currentLevelPlayer + atkSpeedToInt + speedToInt;

        RefreashGeerScore();
    }
}