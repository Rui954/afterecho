using Terraria;
using Terraria.ModLoader;

namespace Afterecho
{
    /// <summary>
    /// 精确谱系入口（弹幕命中）。召唤物与高频弹幕走节流版本，
    /// 否则谱系会被 minion 的每帧命中刷成单一维度。
    /// </summary>
    public class AfterechoGlobalProjectile : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null || !cfg.Enabled || damageDone <= 0) return;
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return;

            Player owner = Main.player[projectile.owner];
            if (!owner.active) return;

            var school = AfterechoGlobalItem.SchoolOfProjectile(projectile);
            var strike = new LedgerStrike(
                school,
                LedgerPlayer.RangeOf(owner, target),
                LedgerPlayer.MotionOf(owner),
                LedgerPlayer.ApproachOf(owner, target));

            var ledger = owner.GetModPlayer<LedgerPlayer>();
            if (projectile.minion || projectile.sentry)
                ledger.RecordStrikeThrottled(strike);
            else
                ledger.RecordStrike(strike, precise: true);

            EchoEvents.OnStrike(owner, school);
        }
    }
}
