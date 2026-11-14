using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Afterecho.Content.Items.Weapons;

namespace Afterecho
{
    /// <summary>
    /// 精确谱系入口（物品命中）。设计案 2.1 的四维指纹在这里取到真实的
    /// 距离、来向、位移与伤害类型。
    /// </summary>
    public class AfterechoGlobalItem : GlobalItem
    {
        /// <summary>
        /// 缄口「收录」的武器在封印期间伤害归零，但仍然可以挥击 ——
        /// 挥击本身在积累「未言」层数（见 LedgerPlayer.TickSilence）。
        ///
        /// 注意：这里**不能**用 CanUseItem 返回 false。那会把挥击动画一起禁掉，
        /// 玩家永远攒不到层数，「被封印的 6 秒是蓄力而不是空窗」这条设计会失效。
        /// </summary>
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            var ledger = player.GetModPlayer<LedgerPlayer>();
            if (ledger.IsSilenced && item.type == ledger.SilencedItemType)
                damage *= 0f;
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null || !cfg.Enabled || damageDone <= 0) return;

            var ledger = player.GetModPlayer<LedgerPlayer>();
            var strike = BuildStrike(player, item, target);
            ledger.RecordStrike(strike, precise: true);
            EchoEvents.OnStrike(player, strike.School);

            // 折光刃：本体伤害打七折，0.8 秒后在「上一秒所在位置」补一记回响斩。
            // 这条玩法不限于回响庭：它只是位移记账，不依赖谱系判定。
            if (item.type == ModContent.ItemType<Foldblade>())
            {
                Vector2 past = player.Center - player.velocity * 60f;
                ledger.QueueEchoSlash(past, System.Math.Max(1, (int)(item.damage * 0.42f)));
            }
        }

        internal static StrikeSchool SchoolOf(Item item)
        {
            if (item == null || item.damage <= 0) return StrikeSchool.Other;
            if (item.DamageType == DamageClass.Melee) return StrikeSchool.Melee;
            if (item.DamageType == DamageClass.Ranged) return StrikeSchool.Ranged;
            if (item.DamageType == DamageClass.Magic) return StrikeSchool.Magic;
            if (item.DamageType == DamageClass.Summon) return StrikeSchool.Summon;
            return StrikeSchool.Other;
        }

        /// <summary>
        /// 弹幕的伤害类型取弹幕自身，而不是玩家手里的武器 ——
        /// 否则「拿着剑放魔法弹」会被记成近战，谱系立刻失真。
        /// </summary>
        internal static StrikeSchool SchoolOfProjectile(Projectile projectile)
        {
            if (projectile == null) return StrikeSchool.Other;
            if (projectile.DamageType == DamageClass.Melee) return StrikeSchool.Melee;
            if (projectile.DamageType == DamageClass.Ranged) return StrikeSchool.Ranged;
            if (projectile.DamageType == DamageClass.Magic) return StrikeSchool.Magic;
            if (projectile.DamageType == DamageClass.Summon) return StrikeSchool.Summon;
            return StrikeSchool.Other;
        }

        internal static LedgerStrike BuildStrike(Player player, Item item, NPC target)
            => new LedgerStrike(
                SchoolOf(item),
                LedgerPlayer.RangeOf(player, target),
                LedgerPlayer.MotionOf(player),
                LedgerPlayer.ApproachOf(player, target));
    }
}
