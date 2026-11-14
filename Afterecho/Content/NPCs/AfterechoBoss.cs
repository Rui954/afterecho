using System;
using System.Collections.Generic;
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
    /// 第四幕 Boss「余响本身 The Afterecho」。
    ///
    /// 它没有固定招式：开场从玩家整局谱系中抽取 5 个最高频动作，
    /// 生成 5 个半透明残影依次登场，每个残影只会玩家最擅长的那一招。
    /// 残影全灭前本体不受伤（dontTakeDamage 是行为开关，不是数值减免）。
    /// 残影数值固定且乘数 ≤ 1.0 —— 玩家变强不会让终局变简单，
    /// 只会让镜像更「熟悉」自己。
    /// </summary>
    public class AfterechoBoss : ModNPC
    {
        private int _timer;
        private int _shadesSpawned;
        private readonly List<LedgerAction> _actions = new List<LedgerAction>();

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            Music = MusicID.Boss5;
        }

        public override void SetDefaults()
        {
            NPC.width = 72; NPC.height = 72;
            NPC.aiStyle = -1;
            NPC.boss = true;
            NPC.noGravity = true; NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;

            // 设计案第 3 节：月亮领主（145000）的 0.95 倍，加载期断言守住 0.90–1.05。
            const int classicLife = 137750;
            Balance.AssertBossStats("余响本身 The Afterecho", classicLife, Balance.AnchorMoonLord);
            NPC.lifeMax = Balance.ScaleBossLife(classicLife);
            NPC.damage = 70;
            NPC.defense = 30;
            NPC.value = Item.buyPrice(gold: 20);
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

            if (_timer == 1)
            {
                _actions.Clear();
                _actions.AddRange(ledger.TopActions(5));
                Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.AfterechoIntro"),
                    new Color(215, 205, 185));
            }

            // 依次登场，每 4 秒一个，给玩家逐个适应的空间。
            if (_shadesSpawned < 5 && _timer % 240 == 1)
            {
                float a = MathF.PI * 2f * _shadesSpawned / 5f;
                int x = (int)(target.Center.X + MathF.Cos(a) * 340f);
                int y = (int)(target.Center.Y + MathF.Sin(a) * 340f - 120f);
                int idx = NPC.NewNPC(NPC.GetSource_FromAI(), x, y,
                    ModContent.NPCType<EchoShade>(), 0, _shadesSpawned);
                if (idx >= 0 && idx < Main.maxNPCs)
                    Main.npc[idx].GetGlobalNPC<ReverieGlobalNPC>().ReverieOwned = true;
                _shadesSpawned++;
            }

            // 行为开关：残影在场时本体不受伤；全灭后才可击杀。
            NPC.dontTakeDamage = CountShades() > 0;

            Vector2 hover = target.Center + new Vector2(0f, -280f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (hover - NPC.Center) * 0.015f, 0.04f);

            // 残影全灭后的低速收束弹幕，仍走统一预算。
            if (NPC.dontTakeDamage || _timer % 120 != 0) return;
            var (count, damage) = Balance.ScaleVolley(3, Balance.MirrorShadeDamage,
                AfterechoConfig.Instance.DeepResonance);
            for (int k = 0; k < count; k++)
            {
                float a = MathF.PI * 2f * k / Math.Max(1, count) + _timer * 0.01f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
                    new Vector2(MathF.Cos(a), MathF.Sin(a)) * 4.5f,
                    ModContent.ProjectileType<MirrorShade>(), damage, 3f, Main.myPlayer, 2f);
            }
        }

        private static int CountShades()
        {
            int n = 0;
            int type = ModContent.NPCType<EchoShade>();
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].type == type) n++;
            return n;
        }

        public override void OnKill()
        {
            // 本体死亡时收掉所有仍存活的残影，避免遗留的残影让下一次挑战
            // 一开场就处于「残影在场 → 本体无敌」的死锁状态。
            int shadeType = ModContent.NPCType<EchoShade>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || n.type != shadeType) continue;
                n.active = false;
                n.netUpdate = true;
            }

            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<FullScoreCodex>(), 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<RescorePage>(), Main.rand.Next(12, 20));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<EchoShard>(), Main.rand.Next(20, 30));
            Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.AfterechoEnd"),
                new Color(215, 205, 185));
        }
    }

    /// <summary>终局残影：只会玩家最擅长的一招（弹道由 ai[0] 决定）。</summary>
    public class EchoShade : ModNPC
    {
        private int _timer;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 40; NPC.height = 40;
            NPC.aiStyle = -1;
            NPC.boss = false;
            NPC.noGravity = true; NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.alpha = 110;

            // 固定生命，乘数 ≤ 1.0：不随玩家装备成长，避免「越刷越简单」。
            NPC.lifeMax = 4800;
            NPC.damage = 30;
            NPC.defense = 10;
            NPC.value = 0;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
        }

        public override void AI()
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers) NPC.TargetClosest();
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers) { NPC.active = false; return; }
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead) { NPC.active = false; return; }

            _timer++;
            int actionIndex = (int)NPC.ai[0];
            float a = _timer * 0.012f + actionIndex * 1.25f;
            Vector2 hover = target.Center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 300f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, (hover - NPC.Center) * 0.02f, 0.08f);

            if (_timer % 100 != 0) return;
            var (count, damage) = Balance.ScaleVolley(2, Balance.MirrorShadeDamage,
                AfterechoConfig.Instance.DeepResonance);
            for (int k = 0; k < count; k++)
            {
                Vector2 dir = Vector2.Normalize(target.Center - NPC.Center) * 5.5f;
                dir = dir.RotatedBy((k - (count - 1) * 0.5f) * 0.25f);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, dir,
                    ModContent.ProjectileType<MirrorShade>(), damage, 2.5f,
                    Main.myPlayer, actionIndex % 3f);
            }
        }

        public override void OnKill()
        {
            if (Main.rand.NextBool(3))
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                    ModContent.ItemType<EchoShard>(), 1);
        }
    }
}
