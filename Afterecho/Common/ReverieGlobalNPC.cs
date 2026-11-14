using Terraria;
using Terraria.ModLoader;

namespace Afterecho
{
    /// <summary>
    /// 每个 NPC 独立挂一个「是否属于回响庭」标记。
    /// 宪法 2：庭内敌怪退出相位层时必须全部清理，绝不允许漏进主世界。
    /// </summary>
    public class ReverieGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool ReverieOwned;

        /// <summary>
        /// 软抗性：敌人摆出针对性「姿态」时统一获得 ≤12% 减伤；
        /// 玩家换打法让姿态回到「常态」，减伤立刻归零。
        ///
        /// 三层硬约束：
        ///  1) 上限写死 Balance.PostureResistanceCap，不叠加、不随阶段成长；
        ///  2) 只在 ReverieOwned 的敌怪上生效，主世界原版敌怪完全不受影响；
        ///  3) 留白戒的 IgnoreNextResistance 可跳过一次，但 8 秒内不能再触发。
        ///
        /// 版本提示：ModifyIncomingHit 是 1.4.4 的 NPC.HitModifiers 签名；
        /// 若本地 SDK 仍是旧的 (ref int damage, ...) 形式，见 README「版本敏感点」。
        /// </summary>
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (!ReverieOwned || AfterechoConfig.Instance?.Enabled != true) return;

            int pi = npc.lastInteraction;
            if (pi < 0 || pi >= Main.maxPlayers || !Main.player[pi].active) return;
            var ledger = Main.player[pi].GetModPlayer<LedgerPlayer>();

            if (ledger.IgnoreNextResistance)
            {
                ledger.IgnoreNextResistance = false;
                return;
            }

            if (ledger.CurrentPosture() == PostureKind.Normal) return;
            modifiers.FinalDamage *= 1f - Balance.PostureResistanceCap;
        }
    }
}
