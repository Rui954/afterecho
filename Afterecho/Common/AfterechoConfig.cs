using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Afterecho
{
    /// <summary>
    /// 服务端统一配置，避免多人游戏各客户端谱系判定不一致。
    ///
    /// 说明文字全部走 Localization（Mods.Afterecho.Configs.AfterechoConfig.*），
    /// 不写 [Tooltip] 字面量 —— 新版本 tModLoader 已把它标记为过时。
    /// </summary>
    public class AfterechoConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        public static AfterechoConfig Instance => ModContent.GetInstance<AfterechoConfig>();

        [DefaultValue(true)]
        [ReloadRequired]
        public bool Enabled { get; set; } = true;

        [DefaultValue(false)]
        public bool DeepResonance { get; set; } = false;

        [DefaultValue(true)]
        public bool EnableCounterform { get; set; } = true;

        [DefaultValue(true)]
        public bool SpawnReverieEnemies { get; set; } = true;

        [DefaultValue(true)]
        public bool EnableEchoEvents { get; set; } = true;

        [DefaultValue(1.0f)]
        [Range(0.5f, 2.0f)]
        public float StabilityDrainScale { get; set; } = 1.0f;

        [DefaultValue(false)]
        public bool DebugShowLedger { get; set; } = false;
    }
}
