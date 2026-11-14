using Terraria;
using Terraria.ModLoader;

namespace Afterecho.Content.Biomes
{
    /// <summary>
    /// 回响庭：纯「场景效果」生物群系。
    ///
    /// 关键：不注册任何 ModTile、不参与世界生成，IsBiomeActive 只由
    /// 玩家是否主动切入相位层决定。因此不会像感染型生物群系那样扩散、
    /// 不会改写地形、不会污染存档（宪法 1、2、6）。
    /// </summary>
    public class ReverieBiome : ModBiome
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override bool IsBiomeActive(Player player)
            => AfterechoConfig.Instance?.Enabled == true
               && player.GetModPlayer<LedgerPlayer>().InReverie;

        // TODO（美术 / 音频，全部纯表现层，不影响任何玩法数值）：
        //  - 褪色拓片背景与主世界建筑的半透明叠绘
        //  - 随主导战术偏移的极简环境音（近战偏低频 / 魔法偏泛音）
        //  - 地面纹路随谱系走向缓慢重排，作为「不显示数值」的信息提示
        //  - 按 ReverieTier 切换四层色调：浅庭 / 褶庭 / 逆庭 / 全谱庭
        // 正式接入时再把 Music / MapBackground 绑定到资源；当前不引用不存在的资源。
    }
}
