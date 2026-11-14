using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Afterecho.Content.NPCs;

namespace Afterecho
{
    /// <summary>
    /// 《余响》入口。
    /// 不注册任何 ModTile、不参与世界生成、不改原版配方/掉落/进度。
    /// </summary>
    public class Afterecho : Mod
    {
        public override void PostSetupContent()
        {
            // 第一道闸门：数值层被改动就直接阻止加载，而不是留到实机失衡。
            Balance.Validate();

            // 弱引用对接：没装 Boss Checklist 时完全跳过，不产生硬依赖。
            RegisterBossChecklist();
        }

        private void RegisterBossChecklist()
        {
            if (!ModLoader.HasMod("BossChecklist")) return;

            Mod bossChecklist;
            try { bossChecklist = ModLoader.GetMod("BossChecklist"); }
            catch (Exception e)
            {
                Logger.Info($"[余响] 未取得 Boss Checklist 实例，跳过图鉴登记：{e.Message}");
                return;
            }
            if (bossChecklist == null) return;

            // 四幕都挂在原版进度之后（宪法 7「不越阶」），因此可用原版 downed
            // 判定作为「可挑战」条件，不需要本模组自己的世界存档标记。
            TryLogBoss(bossChecklist, "Tracer", 1.2f, () => NPC.downedBoss1,
                ModContent.NPCType<Tracer>());
            TryLogBoss(bossChecklist, "Verso", 5.5f, () => Main.hardMode,
                ModContent.NPCType<Verso>());
            TryLogBoss(bossChecklist, "Hush", 11.0f, () => NPC.downedPlantBoss,
                ModContent.NPCType<Hush>());
            TryLogBoss(bossChecklist, "Afterecho", 15.0f, () => NPC.downedMoonlord,
                ModContent.NPCType<AfterechoBoss>());
        }

        /// <summary>
        /// Boss Checklist 的 LogBoss 参数表在不同版本之间有过调整，
        /// 因此每条登记单独 try/catch：失败只是少一条图鉴，不影响模组本身。
        /// </summary>
        private void TryLogBoss(Mod bossChecklist, string name, float difficulty,
            Func<bool> downed, int npcType)
        {
            try
            {
                bossChecklist.Call("LogBoss", this, name, difficulty, downed, npcType);
            }
            catch (Exception e)
            {
                Logger.Info($"[余响] Boss Checklist 登记 {name} 失败（API 版本差异），已跳过：{e.Message}");
            }
        }
    }
}
