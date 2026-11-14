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
    /// 第二幕 Boss「对页 The Verso」。
    ///
    /// 每一「页」对应玩家谱系里权重最高的一种战术，它在权重前二的
    /// 战术间轮流翻页；当玩家用「第三战术」命中时，书页短暂空白（硬直窗口）。
    /// 硬直由玩家行为触发，不靠计时器，也不靠数值压制。
    /// </summary>
    public class Verso : ModNPC
    {
        private int _timer;
        private int _page;
        private int _blankTimer;
        private StrikeSchool _pageA = StrikeSchool.Melee;
        private StrikeSchool _pageB = StrikeSchool.Ranged;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            Music = MusicID.Boss2;
        }

        public override void SetDefaults()
        {
            NPC.width = 58; NPC.height = 58;
            NPC.aiStyle = -1;
            NPC.boss = true;
            NPC.noGravity = true; NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;

            // 设计案第 3 节：血肉墙（8000）的 0.95 倍，加载期断言守住 0.90–1.05。
            const int classicLife = 7600;
            Balance.AssertBossStats("对页 The Verso", classicLife, Balance.AnchorWallOfFlesh);
            NPC.lifeMax = Balance.ScaleBossLife(classicLife);
            NPC.damage = 34;
            NPC.defense = 10;
            NPC.value = Item.buyPrice(gold: 6);
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

            // 每次翻页重新读取权重前二战术 —— 换打法会立刻改变书页内容。
            if (_timer % 180 == 0 || _timer == 1)
            {
                var tops = ledger.TopSchools(2);
                _pageA = tops.Count > 0 ? tops[0] : StrikeSchool.Melee;
                _pageB = tops.Count > 1 ? tops[1] : StrikeSchool.Ranged;
                _page = 1 - _page;
            }

            // 第三战术命中 → 书页空白。窗口由玩家行为触发。
            // 必须排除 Other：谱系还没建立时 LastStrikeSchool 的默认值就是 Other，
            // 否则开场第一帧就会白送一次硬直。
            StrikeSchool last = ledger.LastStrikeSchool;
            if (last != StrikeSchool.Other && last != _pageA && last != _pageB && _blankTimer <= 0)
            {
                _blankTimer = 150;
                Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.VersoBlank"),
                    new Color(210, 205, 190));
            }
            if (_blankTimer > 0) _blankTimer--;

            // 悬浮：空白期明显放慢（行为变化，数值不动）。
            float sweep = _blankTimer > 0 ? 0.006f : 0.018f;
            Vector2 hover = target.Center
                            + new Vector2(MathF.Sin(_timer * sweep) * 300f, -220f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (hover - NPC.Center) * 0.02f, 0.07f);

            if (_blankTimer > 0 || _timer % 70 != 0) return;

            bool deep = AfterechoConfig.Instance.DeepResonance;
            var (count, damage) = Balance.ScaleVolley(3, Balance.InkBlotDamage, deep);

            // 页 A 扇形 / 页 B 平行线：两种弹幕形状，伤害完全相同。
            for (int k = 0; k < count; k++)
            {
                Vector2 vel;
                if (_page == 0)
                {
                    float a = MathF.PI * (0.25f + 0.5f * k / Math.Max(1, count - 1));
                    vel = new Vector2(MathF.Cos(a), MathF.Sin(a)) * 7f;
                }
                else
                {
                    vel = new Vector2(MathF.Sign(target.Center.X - NPC.Center.X) * 7f,
                                      (k - count / 2f) * 1.4f);
                }
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                    ModContent.ProjectileType<InkBlot>(), damage, 3f, Main.myPlayer, 1f);
            }
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<EchoShard>(), Main.rand.Next(8, 14));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<RescorePage>(), Main.rand.Next(4, 7));
        }
    }
}
