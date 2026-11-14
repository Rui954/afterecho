using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Afterecho.Content.Items;

namespace Afterecho.Content.NPCs
{
    /// <summary>
    /// 回响庭内唯一的普通敌怪「拓影」。
    /// 宪法 2：它只由 ReverieSystem 在玩家主动进入庭内时生成，
    /// 并被标记 ReverieOwned；主世界刷怪表完全不受影响。
    /// </summary>
    public class EchoWisp : ModNPC
    {
        private int _timer;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 28; NPC.height = 28;
            NPC.aiStyle = -1;
            NPC.noGravity = true; NPC.noTileCollide = true;
            NPC.lifeMax = 120;
            NPC.damage = 14;
            NPC.defense = 2;
            NPC.value = 0;
            NPC.alpha = 70;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
        }

        public override void AI()
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers) NPC.TargetClosest();
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers) return;
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead) return;

            _timer++;
            Vector2 wanted = target.Center
                             + new Vector2(MathF.Sin(_timer * 0.03f) * 120f, -100f);
            NPC.velocity = Vector2.Lerp(NPC.velocity,
                Vector2.Normalize(wanted - NPC.Center) * 2.2f, 0.03f);
            NPC.rotation += 0.02f;
        }

        public override void OnKill()
        {
            // 回声砂只在稳定度低于 30% 时掉落：材料来自压线探索，不是平地刷取。
            Player nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead || !p.GetModPlayer<LedgerPlayer>().InReverie) continue;
                float d = Vector2.Distance(p.Center, NPC.Center);
                if (d < best) { best = d; nearest = p; }
            }
            if (nearest == null) return;

            var ledger = nearest.GetModPlayer<LedgerPlayer>();
            if (ledger.Stability < Balance.StabilityMax * Balance.LowStabilityThreshold)
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                    ModContent.ItemType<EchoShard>(), Main.rand.Next(1, 4));
            else if (Main.rand.NextBool(6))
                Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                    ModContent.ItemType<RescorePage>(), 1);
        }
    }
}
