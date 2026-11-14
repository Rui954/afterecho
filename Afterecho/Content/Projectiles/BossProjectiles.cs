using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Afterecho.Content.Projectiles
{
    /// <summary>描摹者把「玩家上一场轨迹」实体化后的伤害带。</summary>
    public class TraceRibbon : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 26; Projectile.height = 26;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 90;
            Projectile.penetrate = 1;
            Projectile.alpha = 60;
        }

        public override void AI()
        {
            Projectile.rotation += 0.06f;
            if (Main.rand.NextBool(2))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Smoke, 0f, 0f, 170, default, 0.9f);
        }
    }

    /// <summary>通用墨晕弹。ai[0] 只影响外观与轨迹，不影响伤害。</summary>
    public class InkBlot : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 18; Projectile.height = 18;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = 1;
        }

        public override void AI()
        {
            Projectile.rotation += 0.08f;
            if (Projectile.ai[0] > 0f) Projectile.velocity *= 0.995f; // 慢速变体
            if (Main.rand.NextBool(3))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Smoke, 0f, 0f, 190, default, 0.85f);
        }
    }

    /// <summary>缄口的收录波：大、慢、可见，给玩家足够反应时间。</summary>
    public class SilenceWave : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 36; Projectile.height = 36;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile.penetrate = 1;
            Projectile.alpha = 80;
        }

        public override void AI()
        {
            Projectile.alpha = 60 + (int)(30f * MathF.Sin(Projectile.timeLeft * 0.15f));
            Projectile.rotation += 0.04f;
            if (Main.rand.NextBool(2))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Cloud, 0f, 0f, 200, default, 1.0f);
        }
    }

    /// <summary>
    /// 终局残影的「复制招式」弹。ai[0] 决定弹道形状（直线 / 散射 / 轻追踪），
    /// 对应玩家 5 个高频动作中的一档；伤害由 Balance.ScaleVolley 统一预算。
    /// </summary>
    public class MirrorShade : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 16;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 200;
            Projectile.penetrate = 1;
        }

        public override void AI()
        {
            Projectile.rotation += 0.1f;

            // 轻追踪：只旋转速度方向，不加速，避免变成无法躲避的追踪弹。
            if (Projectile.ai[0] > 1.5f && Main.player != null)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 wanted = Vector2.Normalize(target.Center - Projectile.Center)
                                 * Projectile.velocity.Length();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, 0.012f);
            }
            if (Main.rand.NextBool(3))
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.MagicMirror, 0f, 0f, 180, default, 0.9f);
        }
    }
}
