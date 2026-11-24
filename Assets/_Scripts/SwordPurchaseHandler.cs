using System;
using System.Collections.Generic;
using UnityEngine;
using YG;
using YG.Utils.Pay;

public class SwordPurchaseHandler : MonoBehaviour
{
    [Serializable]
    public class SwordSlot
    {
        [Tooltip("Точный ID из компонента PurchaseYG (для стартового меча оставь пустым)")]
        public string productID;
        [Tooltip("Модель меча в руке игрока")]
        public GameObject swordModel;
        [Tooltip("Объект ауры на мече (необязательно)")]
        public GameObject auraObject;
        [Tooltip("Материал ауры из папки Assets")]
        public Material auraMaterial;
        public int swordDamage;
    }

    [Header("Список всех мечей игрока")]
    [Tooltip("Элемент 0 — стартовый меч, Элемент 1 и далее — платные мечи")]
    public SwordSlot[] swords;
    public PlayerStats playerStats;
    public Sharpening sharpening;

    private void OnEnable()
    {
        YG2.onPurchaseSuccess += OnPurchaseSuccess;
        YG2.onGetSDKData += LoadAndEquipSavedSword;
    }

    private void OnDisable()
    {
        YG2.onPurchaseSuccess -= OnPurchaseSuccess;
        YG2.onGetSDKData -= LoadAndEquipSavedSword;
    }

    private void Start()
    {
        if (YG2.isSDKEnabled)
        {
            LoadAndEquipSavedSword();
        }
    }

    private void OnPurchaseSuccess(string id)
    {
        if (this == null || swords == null) return;

        for (int i = 0; i < swords.Length; i++)
        {
            if (swords[i].productID == id)
            {
                if (YG2.saves.boughtSwordsIDs == null)
                    YG2.saves.boughtSwordsIDs = new List<string>();

                if (!YG2.saves.boughtSwordsIDs.Contains(id))
                    YG2.saves.boughtSwordsIDs.Add(id);

                YG2.saves.equippedSwordIndex_Model = i;
                YG2.SaveProgress();

                if (sharpening != null)
                {
                    sharpening.ResetUpgrade();
                }

                EquipSwordByIndex(i);
                break;
            }
        }
    }

    private void LoadAndEquipSavedSword()
    {
        if (this == null || swords == null || swords.Length == 0) return;

        if (YG2.saves.boughtSwordsIDs == null)
            YG2.saves.boughtSwordsIDs = new List<string>();

        bool needSave = false;

        for (int i = 0; i < swords.Length; i++)
        {
            string id = swords[i].productID;
            if (string.IsNullOrEmpty(id)) continue;

            Purchase purchase = YG2.PurchaseByID(id);
            if (purchase != null && !purchase.consumed && !YG2.saves.boughtSwordsIDs.Contains(id))
            {
                YG2.saves.boughtSwordsIDs.Add(id);
                YG2.saves.equippedSwordIndex_Model = i;
                needSave = true;
            }
        }

        if (needSave)
        {
            YG2.SaveProgress();
        }

        int indexToEquip = Mathf.Clamp(YG2.saves.equippedSwordIndex_Model, 0, swords.Length - 1);
        EquipSwordByIndex(indexToEquip);
    }

    public void EquipSwordByIndex(int index)
    {
        if (this == null || swords == null || index < 0 || index >= swords.Length) return;

        for (int i = 0; i < swords.Length; i++)
        {
            if (swords[i].swordModel != null)
            {
                swords[i].swordModel.SetActive(i == index);
            }
        }

        if (playerStats != null)
        {
            if (swords[index].swordDamage > 0)
            {
                playerStats.AtkDmg_Player = swords[index].swordDamage;
            }
            playerStats.UpdatePlayerStatsUI();
        }

        if (sharpening != null)
        {
            sharpening.bladeAuraObject = swords[index].auraObject;
            sharpening.bladeAuraMaterial = swords[index].auraMaterial;
            sharpening.RefreshWeaponGlow();
        }
    }
}
