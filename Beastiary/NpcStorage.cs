using System.Collections.Generic;
using Terraria.ModLoader;

namespace TerrariaCompanionMod
{
    public class NpcStorage
    {
        private static NpcStorage _instance;
        public static NpcStorage Instance
        {
            get
            {
                if (_instance == null)
                    throw new System.Exception("NPCStorage has not been initialized. Call Init(Mod mod) first.");
                return _instance;
            }
        }
        private Dictionary<int, Dictionary<string, object>> _mainList;
        private Mod _mod;
    
        private NpcStorage(Mod mod)
        {
            
            _mod = mod;
            _mainList = new Dictionary<int, Dictionary<string, object>>();
            
        }

        public Dictionary<int, Dictionary<string, object>> GetMainList() => _mainList;


        public void SetMainList(Dictionary<int, Dictionary<string, object>> newList) => _mainList = newList;

        public void ClearMainList()
        {
            _mainList.Clear();
        }

        public static void Init(Mod mod)
        {
            if (_instance == null)
                _instance = new NpcStorage(mod);
        }
    }
}