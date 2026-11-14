using Terraria.ModLoader;

namespace Afterecho
{
    /// <summary>
    /// 「双向镜」饰品需要一个主动按键，但饰品本身不能被 UseItem。
    /// 因此注册独立键位，由 LedgerPlayer.ProcessTriggers 在装备时消费。
    /// </summary>
    public class AfterechoKeybinds : ModSystem
    {
        public static ModKeybind RollbackLedger { get; private set; }

        public override void Load()
        {
            RollbackLedger = KeybindLoader.RegisterKeybind(Mod, "RollbackLedger", "P");
        }

        public override void Unload()
        {
            RollbackLedger = null;
        }
    }
}
