using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Afterecho.Content.Projectiles
{
    /// <summary>折光刃的延迟回响斩，生成在玩家上一秒所在位置（或折页靴残影处）。</summary>
    public class EchoSlash : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 46; Projectile.height = 46;
            Projectile.friendly = true; Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 22;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation += 0.22f;
            if (Main.rand.NextBool(3))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.MagicMirror, 0f, 0f, 180, default, 1.1f);
        }
    }

    /// <summary>
    /// 错位弦专用箭：从「上一支箭的落点」射出，落点又成为下一支的起点。
    /// 起点改写放在 OnSpawn，落点记录放在 OnKill，全程不碰伤害数值。
    /// </summary>
    public class OffsetArrow : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 10; Projectile.height = 10;
            Projectile.friendly = true; Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = 1;              // 原版箭 AI
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 600;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
            var ledger = Main.player[Projectile.owner].GetModPlayer<LedgerPlayer>();
            if (!ledger.HasArrowLanding) return;

            Vector2 start = ledger.LastArrowLanding
                            - new Vector2(Projectile.width * 0.5f, Projectile.height * 0.5f);
            Projectile.position = start;
            Projectile.netUpdate = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
            var ledger = Main.player[Projectile.owner].GetModPlayer<LedgerPlayer>();
            ledger.LastArrowLanding = Projectile.Center;
            ledger.HasArrowLanding = true;
        }
    }

    /// <summary>「未写完的一页」：3 秒内跟随玩家，到点补完为范围回响。</summary>
    public class PageSeed : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 20;
            // 不伤害任何人：只有补完后的 PageBurst 有伤害。
            Projectile.friendly = false; Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;          // 3 秒
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
            Player owner = Main.player[Projectile.owner];
            if (!owner.active) { Projectile.Kill(); return; }

            Projectile.Center = Vector2.Lerp(Projectile.Center,
                owner.Center + new Vector2(0f, -40f), 0.08f);
            Projectile.rotation += 0.03f;
            if (Main.rand.NextBool(6))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Cloud, 0f, 0f, 200, default, 0.9f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
            Player owner = Main.player[Projectile.owner];
            if (!owner.active) return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<PageBurst>(),
                Projectile.damage, 3f, Projectile.owner);
        }
    }

    /// <summary>补完的一页 / 缄口释放的范围回响。</summary>
    public class PageBurst : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 140; Projectile.height = 140;
            Projectile.friendly = true; Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 24;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.scale = 0.8f + Projectile.timeLeft / 120f;
            for (int i = 0; i < 2; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.MagicMirror, 0f, 0f, 160, default, 1.0f);
        }
    }

    /// <summary>折页靴留下的纯视觉残影，无伤害、无碰撞。</summary>
    public class Afterimage : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 20; Projectile.height = 34;
            Projectile.friendly = false; Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 40;
            Projectile.alpha = 120;
        }

        public override void AI()
        {
            Projectile.alpha = Utils.Clamp(Projectile.alpha + 5, 0, 255);
            if (Main.rand.NextBool(4))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Smoke, 0f, 0f, 220, default, 0.8f);
        }
    }
}
