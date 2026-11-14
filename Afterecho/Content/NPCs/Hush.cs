using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Afterecho.Content.Items;
using Afterecho.Content.Projectiles;

namespace Afterecho.Content.NPCs
{
    /// <summary>
    /// 第三幕 Boss「缄口 The Hush」。
    ///
    /// 周期性「收录」玩家当前武器的音效：被收录的武器 6 秒内不造成伤害，
    /// 但每次挥击积累「未言」层数；切换武器后层数一次性释放为范围回响。
    /// 强制换武器，却把惩罚变成资源 —— 被封印的 6 秒不是空窗，是蓄力。
    /// 收录由 LedgerPlayer.ApplySilence 实现，封印期间不造成任何数值削弱。
    /// </summary>
    public class Hush : ModNPC
    {
        private int _timer;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            Music = MusicID.Boss3;
        }

        public override void SetDefaults()
        {
            NPC.width = 64; NPC.height = 64;
            NPC.aiStyle = -1;
            NPC.boss = true;
            NPC.noGravity = true; NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;

            // 设计案第 3 节：世纪之花（30000）的 0.95 倍，加载期断言守住 0.90–1.05。
            const int classicLife = 28500;
            Balance.AssertBossStats("缄口 The Hush", classicLife, Balance.AnchorPlantera);
            NPC.lifeMax = Balance.ScaleBossLife(classicLife);
            NPC.damage = 52;
            NPC.defense = 22;
            NPC.value = Item.buyPrice(gold: 10);
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
        }

        public override void AI()
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers) NPC.TargetClosest();
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers) { NPC.active = false; return; }
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead) { NPC.active = false; return; }

            var ledger = target.GetModPlayer<LedgerPlayer>();
            _timer++;

            PostureKind posture = ledger.CurrentPosture();
            float sweep = posture == PostureKind.Folder ? 0.026f : 0.017f;
            Vector2 hover = target.Center
                            + new Vector2(MathF.Cos(_timer * sweep) * 260f, -240f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (hover - NPC.Center) * 0.02f, 0.06f);

            // 每 9 秒收录一次，且只在玩家没被封印时触发，避免连锁锁死。
            if (_timer % 540 == 0 && !ledger.IsSilenced)
            {
                int held = target.HeldItem?.type ?? -1;
                if (held > 0)
                {
                    ledger.ApplySilence(held, 6);
                    Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.HushSilence"),
                        new Color(210, 195, 170));

                    bool deep = AfterechoConfig.Instance.DeepResonance;
                    var (count, damage) = Balance.ScaleVolley(2, Balance.SilenceWaveDamage, deep);
                    for (int k = 0; k < count; k++)
                    {
                        float a = MathF.PI * 2f * k / Math.Max(1, count);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                            new Vector2(MathF.Cos(a), MathF.Sin(a)) * 3.2f,
                            ModContent.ProjectileType<SilenceWave>(), damage, 2f, Main.myPlayer);
                    }
                }
            }

            // 常规弹幕：只改编排，不改数值。
            if (_timer % 96 != 0) return;
            var (n, dmg) = Balance.ScaleVolley(2, Balance.InkBlotDamage,
                AfterechoConfig.Instance.DeepResonance);
            for (int k = 0; k < n; k++)
            {
                Vector2 toPlayer = Vector2.Normalize(target.Center - NPC.Center) * 6f;
                toPlayer = toPlayer.RotatedBy((k - (n - 1) * 0.5f) * 0.28f);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, toPlayer,
                    ModContent.ProjectileType<InkBlot>(), dmg, 3f, Main.myPlayer);
            }
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<EchoShard>(), Main.rand.Next(14, 22));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<RescorePage>(), Main.rand.Next(7, 11));
        }
    }
}
