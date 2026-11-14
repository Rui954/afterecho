using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Afterecho.Content.Items;
using Afterecho.Content.Projectiles;

namespace Afterecho.Content.NPCs
{
    /// <summary>
    /// 第一幕 Boss「描摹者 The Tracer」。
    ///
    /// 原创机制：几乎不主动攻击，而是重演「玩家上一场战斗的移动轨迹」，
    /// 轨迹逐点实体化为伤害带。玩家必须刻意走出与以往不同的路线。
    /// 若这是本局第一场战斗，则退化为实时描摹当前轨迹并延迟实体化。
    ///
    /// ★ 宪法 5：阶段切换只改 AI 参数（采样延迟 / 悬浮速率），
    ///   绝不修改 lifeMax / damage / defense / 免疫帧 / 速度上限。
    /// </summary>
    public class Tracer : ModNPC
    {
        private readonly Queue<(Vector2 pos, int tick)> _trace = new Queue<(Vector2, int)>();
        private int _timer;
        private int _phase;
        private int _materializeDelay = 180;
        private int _ghostIndex;
        private readonly int[] _delays = { 180, 120, 72 };

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            Music = MusicID.Boss1;           // 原版音乐占位，正式版换原创环境音
            // TODO: 图鉴条目 / Boss 血条样式 / Boss Checklist 弱引用登记
        }

        public override void SetDefaults()
        {
            NPC.width = 46; NPC.height = 46;
            NPC.aiStyle = -1;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;

            // 设计案第 3 节：克苏鲁之眼（2800）的 0.95 倍，加载期断言守住 0.90–1.05。
            const int classicLife = 2660;
            Balance.AssertBossStats("描摹者 The Tracer", classicLife, Balance.AnchorEyeOfCthulhu);
            NPC.lifeMax = Balance.ScaleBossLife(classicLife);
            NPC.damage = 15;
            NPC.defense = 4;
            NPC.value = Item.buyPrice(gold: 2);
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
        }

        public override void AI()
        {
            Player target = GetTarget(out LedgerPlayer ledger);
            if (target == null) { NPC.active = false; return; }
            _timer++;

            // ── 阶段：只改行为参数 ──────────────────────────────
            float lifeRatio = (float)NPC.life / NPC.lifeMax;
            int wanted = lifeRatio < 0.33f ? 2 : lifeRatio < 0.66f ? 1 : 0;
            if (wanted != _phase)
            {
                _phase = wanted;
                _materializeDelay = _delays[_phase];
            }

            // ── 姿态：只读，不写回任何数值 ──────────────────────
            PostureKind posture = ledger.CurrentPosture();
            float sweep = posture == PostureKind.Skimmer ? 0.028f
                        : posture == PostureKind.Waller ? 0.015f
                        : 0.020f;
            Vector2 hover = target.Center
                            + new Vector2(MathF.Sin(_timer * sweep) * 200f, -180f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (hover - NPC.Center) * 0.02f, 0.08f);

            ledger.AppendTrace(target.Center);
            if (_timer % 12 == 0) _trace.Enqueue((target.Center, _timer));

            // ── 轨迹实体化 ─────────────────────────────────────
            bool deep = AfterechoConfig.Instance.DeepResonance;
            var (count, damage) = Balance.ScaleVolley(1, Balance.TraceRibbonDamage, deep);

            int emitInterval = Math.Max(12, _materializeDelay / 12);
            if (_timer % emitInterval == 0)
            {
                // 优先重演「上一场」的路线；没有历史时才用当前轨迹。
                var history = ledger.LastFightTrace;
                Vector2 point;
                if (history.Count > 0)
                {
                    point = history[_ghostIndex % history.Count];
                    _ghostIndex++;
                }
                else if (_trace.Count > 0)
                {
                    point = _trace.Peek().pos;
                }
                else point = target.Center;

                for (int k = 0; k < count; k++)
                {
                    Vector2 jitter = new Vector2(Main.rand.NextFloat(-18f, 18f), Main.rand.NextFloat(-18f, 18f));
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(), point + jitter, Vector2.Zero,
                        ModContent.ProjectileType<TraceRibbon>(),
                        damage, 2f, Main.myPlayer);
                }
            }

            while (_trace.Count > 0 && _timer - _trace.Peek().tick > _delays[_phase] * 3)
                _trace.Dequeue();
        }

        private Player GetTarget(out LedgerPlayer ledger)
        {
            ledger = null;
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers
                || !Main.player[NPC.target].active || Main.player[NPC.target].dead)
                NPC.TargetClosest();
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers) return null;
            Player p = Main.player[NPC.target];
            if (!p.active || p.dead) return null;
            ledger = p.GetModPlayer<LedgerPlayer>();
            return p;
        }

        public override void OnKill()
        {
            // 只掉本模组材料，不修改任何原版 NPC 的掉落表（宪法 1）。
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<EchoShard>(), Main.rand.Next(4, 8));
            Item.NewItem(NPC.GetSource_Loot(), NPC.Center,
                ModContent.ItemType<RescorePage>(), Main.rand.Next(2, 4));

            for (int i = 0; i < Main.maxPlayers; i++)
                if (Main.player[i].active)
                    Main.player[i].GetModPlayer<LedgerPlayer>().EndFightTrace();
        }
    }
}
