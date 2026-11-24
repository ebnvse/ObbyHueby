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
    }
}
