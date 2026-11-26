using System.Collections.Generic;
using UnityEngine;

namespace YG
{
    [System.Serializable]
    public partial class SavesYG
    {
        public int idSave;
        public List<string> boughtSwordsIDs = new List<string>();
        public int equippedSwordIndex_Model = 0;
        public int currentSharpening_index = 0;

        // Характеристики игрока (стартовые значения для нового игрока)
        public int level = 1;
        public int skillPoints = 0;

        public int currentExp = 0;
        public int expToNextLevel = 50;
        public int AtkDmg_Player = 20;
        public int Def_Player = 10;
        public float AtkSpped_Player = 1f;
        public float Speed_Player = 5f;
    }
}
