using Terraria.ModLoader;

namespace TerrariaCompanionMod
{
    public class TerrariaCompanionMod : Mod
    {
        public static TerrariaCompanionMod Instance;

        public override void Load()
        {
            Instance = this;
            ItemStorage.Init(this);
            base.Load();
        }
        
        public override void Unload()
        {
            base.Unload();
        }
    }
}