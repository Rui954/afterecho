using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Afterecho.Content.NPCs;

namespace Afterecho
{
    /// <summary>
    /// 回响庭的「共享实例」层。
    ///
    /// 稳定度是单一真源：多人游戏不会因为每个玩家各自扣减而叠加惩罚（设计案第 7 节）。
    /// 敌怪生成只在有玩家主动进入庭内时发生，且全部打上 ReverieOwned 标记；
    /// 一旦无人留在庭内，立即清理，保证主世界刷怪表零侵入（宪法 2）。
    /// 谱系不写世界存档，因此这里不实现 SaveWorldData / LoadWorldData。
    /// </summary>
    public class ReverieSystem : ModSystem
    {
        public static float SharedStability = Balance.StabilityMax;
        public static PostureKind SquadPosture = PostureKind.Normal;
        public static int SquadPlayerCount;
        public static bool BossActive { get; private set; }

        private static int _spawnTimer;
        private static bool _wasActive;
        private static readonly List<Player> _participants = new List<Player>();

        /// <summary>当前身处回响庭的玩家（共享实例的参与者）。</summary>
        public static IReadOnlyList<Player> Participants => _participants;

        public override void PostUpdateEverything()
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null) return;

            _participants.Clear();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead && p.GetModPlayer<LedgerPlayer>().InReverie)
                    _participants.Add(p);
            }

            SquadPlayerCount = _participants.Count;
            if (SquadPlayerCount == 0)
            {
                if (_wasActive)
                {
                    SharedStability = 0f;
                    SquadPosture = PostureKind.Normal;
                    BossActive = false;
                    CleanupReverieNPCs();
                    EchoEvents.Cancel();
                    _wasActive = false;
                }
                return;
            }
            _wasActive = true;

            BossActive = AnyBossAlive();

            // 单一真源：不按人数倍乘，也不各自扣减 —— 这就是「按队伍人数归一化」。
            // 静默坠的代价是让全队这一条稳定度消耗更快（只乘一次，不叠加）。
            float drain = Balance.StabilityDrainPerSecond * cfg.StabilityDrainScale;
            for (int i = 0; i < _participants.Count; i++)
            {
                if (_participants[i].GetModPlayer<LedgerPlayer>().HasSilentPendant)
                {
                    drain *= Balance.SilentPendantDrainMultiplier;
                    break;
                }
            }
            SharedStability -= drain / 60f;

            if (SharedStability <= 0f)
            {
                SharedStability = 0f;
                for (int i = 0; i < _participants.Count; i++)
                    _participants[i].GetModPlayer<LedgerPlayer>().ExitReverie(true);
                CleanupReverieNPCs();
                EchoEvents.Cancel();
                SquadPosture = PostureKind.Normal;
                SquadPlayerCount = 0;
                _wasActive = false;
                return;
            }

            SquadPosture = ComputeSquadPosture(_participants);

            // 纯表现层：相位层里飘一层很淡的纸灰，让「这里不是主世界」看得见。
            // 只在客户端生成，不参与任何判定，也不会写进世界。
            if (Main.netMode != NetmodeID.Server && Main.GameUpdateCount % 6 == 0)
                SpawnAmbience();

            // 生成只由非客户端执行；客户端跳过，避免多人重复刷怪。
            if (Main.netMode == NetmodeID.MultiplayerClient || !cfg.SpawnReverieEnemies) return;
            if (--_spawnTimer > 0) return;
            _spawnTimer = Balance.SpawnIntervalTicks;
            TrySpawn(_participants[Main.rand.Next(_participants.Count)]);
        }

        private static void SpawnAmbience()
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                Player p = _participants[i];
                Vector2 pos = p.Center + new Vector2(
                    Main.rand.NextFloat(-280f, 280f),
                    Main.rand.NextFloat(-200f, 120f));
                if (!WorldGen.InWorld((int)(pos.X / 16f), (int)(pos.Y / 16f), 2)) continue;
                Dust.NewDustPerfect(pos, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -0.25f),
                    200, new Color(190, 185, 170), 0.7f);
            }
        }

        private static bool AnyBossAlive()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].boss) return true;
            return false;
        }

        private static PostureKind ComputeSquadPosture(List<Player> players)
        {
            if (players.Count == 1)
                return players[0].GetModPlayer<LedgerPlayer>().CurrentPostureRaw();

            // 对每个人的主导战术逐维取众数，得到「队伍均值姿态」。
            var strikes = new List<LedgerStrike>();
            for (int i = 0; i < players.Count; i++)
                strikes.Add(players[i].GetModPlayer<LedgerPlayer>().DominantStrike());

            int[] school = new int[5], range = new int[4], motion = new int[4], approach = new int[3];
            foreach (var s in strikes)
            {
                school[(int)s.School]++;
                range[(int)s.Range]++;
                motion[(int)s.Motion]++;
                approach[(int)s.Approach]++;
            }
            var mean = new LedgerStrike(
                (StrikeSchool)ArgMax(school),
                (StrikeRange)ArgMax(range),
                (StrikeMotion)ArgMax(motion),
                (StrikeApproach)ArgMax(approach));
            return LedgerPlayer.DescribePosture(mean);
        }

        private static void TrySpawn(Player anchor)
        {
            // Boss 战期间不生成普通敌怪，避免干扰弹幕学习节奏。
            if (AnyBossAlive()) return;

            int existing = 0;
            int wispType = ModContent.NPCType<EchoWisp>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && n.type == wispType
                    && n.GetGlobalNPC<ReverieGlobalNPC>().ReverieOwned) existing++;
            }
            if (existing >= Balance.MaxReverieEnemies) return;

            // 在玩家视野外的环形带生成，避免「凭空贴脸出现」。
            float angle = Main.rand.NextFloat(MathF.PI * 2f);
            float radius = Main.rand.NextFloat(620f, 900f);
            int x = (int)(anchor.Center.X + MathF.Cos(angle) * radius);
            int y = (int)(anchor.Center.Y + MathF.Sin(angle) * radius);
            x = Utils.Clamp(x, 160, Main.maxTilesX * 16 - 160);
            y = Utils.Clamp(y, 160, Main.maxTilesY * 16 - 160);

            int idx = NPC.NewNPC(anchor.GetSource_FromThis(), x, y, wispType);
            if (idx < 0 || idx >= Main.maxNPCs) return;

            NPC spawned = Main.npc[idx];
            spawned.GetGlobalNPC<ReverieGlobalNPC>().ReverieOwned = true;
            spawned.target = anchor.whoAmI;
        }

        /// <summary>
        /// 无人留在庭内时清理；这是宪法 2 的兜底闸门。
        /// 敌对弹幕也要一起清掉，否则稳定度归零把玩家弹出后，
        /// 它们会继续留在主世界打人。
        /// </summary>
        private static void CleanupReverieNPCs()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active) continue;
                if (!n.GetGlobalNPC<ReverieGlobalNPC>().ReverieOwned) continue;
                n.active = false;
                n.netUpdate = true;
            }

            var mod = ModContent.GetInstance<Afterecho>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || !p.hostile) continue;
                if (p.ModProjectile != null && p.ModProjectile.Mod == mod) p.Kill();
            }
        }

        private static int ArgMax(int[] values)
        {
            int best = 0;
            for (int i = 1; i < values.Length; i++)
                if (values[i] > values[best]) best = i;
            return best;
        }

        /// <summary>
        /// 卸载时清空静态状态。`_participants` 里存的是 Player 引用，
        /// 而 Player → ModPlayer → Mod 是一条强引用链；不清掉的话，
        /// tModLoader 重载模组时会报「mod class still using memory」。
        /// </summary>
        public override void Unload()
        {
            _participants.Clear();
            SharedStability = Balance.StabilityMax;
            SquadPosture = PostureKind.Normal;
            SquadPlayerCount = 0;
            BossActive = false;
            _spawnTimer = 0;
            _wasActive = false;
        }
    }
}
