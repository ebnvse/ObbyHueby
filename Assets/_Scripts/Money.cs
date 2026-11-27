using UnityEngine;
using TMPro;
using YG;

public class Money : MonoBehaviour
{
    public TextMeshProUGUI money_text;
    public int current_money;

    private bool hasLoadedMoney;

    private void OnEnable()
    {
        YG2.onGetSDKData += LoadMoney;
    }

    private void OnDisable()
    {
        YG2.onGetSDKData -= LoadMoney;
    }

    private void Start()
    {
        if (YG2.isSDKEnabled || Application.isEditor || 
            Application.platform != RuntimePlatform.WebGLPlayer)
        {
            LoadMoney();
        }

        UpdateMoneyUI();
    }


    public void LoadMoney()
    {
        if (hasLoadedMoney) return;

        if (YG2.saves.hasMoneySave)
            current_money = Mathf.Max(0, YG2.saves.current_money);

        hasLoadedMoney = true;
        WriteMoneyToSaves();

        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        if (!hasLoadedMoney || amount <= 0) return;
        current_money = Mathf.Max(0, current_money + amount);
        SaveMoney();
    }

    public bool TrySpendMoney(int amount)
    {
        if (!hasLoadedMoney || amount <= 0 || current_money < amount)
            return false;

        current_money -= amount;
        SaveMoney();
        return true;
    }


    public void SaveMoney()
    {
        if (!hasLoadedMoney)
        {
            Debug.LogWarning("[Money] Сохранение валюты пропущено: данные ещё не загружены.");
            return;
        }

        WriteMoneyToSaves();
        YG2.SaveProgress();
        Debug.Log("[Money] Валюта сохранена.");
    }

    public void WriteMoneyToSaves()
    {
        if (!hasLoadedMoney) return;

        YG2.saves.current_money = Mathf.Max(0, current_money);
        YG2.saves.hasMoneySave = true;
        UpdateMoneyUI();
    }

    public void UpdateMoneyUI()
    {
        if (money_text != null)
            money_text.text = current_money.ToString();
    }
}