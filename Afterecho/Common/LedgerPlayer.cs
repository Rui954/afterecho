using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Afterecho.Content.Projectiles;

namespace Afterecho
{
    public enum StrikeSchool { Melee, Ranged, Magic, Summon, Other }
    public enum StrikeRange { Contact, Near, Mid, Far }
    public enum StrikeMotion { Still, Walking, Dashing, Flying }
    public enum StrikeApproach { Front, Flank, Back }

    public readonly struct LedgerStrike
    {
        public readonly StrikeSchool School;
        public readonly StrikeRange Range;
        public readonly StrikeMotion Motion;
        public readonly StrikeApproach Approach;

        public LedgerStrike(StrikeSchool school, StrikeRange range, StrikeMotion motion, StrikeApproach approach)
        {
            School = school; Range = range; Motion = motion; Approach = approach;
        }

        public override string ToString() => LedgerText.Strike(this);
    }

    /// <summary>
    /// 谱系 + 回响庭的玩家侧状态。
    ///
    /// 宪法 6：全部字段都是运行时数据，不实现 SaveData / LoadData，
    /// 不写入世界存档，卸载模组后存档零残留。
    /// 稳定度不在这里扣减，由 ReverieSystem 以「共享单一真源」统一管理，
    /// 多人游戏因此不会各自扣减而叠加惩罚。
    /// </summary>
    public class LedgerPlayer : ModPlayer
    {
        private readonly Queue<LedgerStrike> _window = new Queue<LedgerStrike>(Balance.LedgerWindow);
        private LedgerStrike[] _snapshot = Array.Empty<LedgerStrike>();
        private int _sampleTimer;
        private int _idleTicks;                 // 距离上一次「精确命中」的 tick 数
        private int _lastThrottledTick = -999;
        private int _lastPostureTick = -999;
        private bool _snapshotTaken;

        // ── 战斗轨迹（描摹者用）──────────────────────────────
        private readonly List<Vector2> _currentTrace = new List<Vector2>();
        private List<Vector2> _lastFightTrace = new List<Vector2>();
        private int _traceTimer;

        // ── 当前姿态 ─────────────────────────────────────────
        public PostureKind Posture { get; private set; } = PostureKind.Normal;

        /// <summary>最近一次有效命中的伤害类型。对页用它在权重前二之外识别「第三战术」。</summary>
        public StrikeSchool LastStrikeSchool { get; private set; } = StrikeSchool.Other;

        // ── 相位层状态 ───────────────────────────────────────
        public bool InReverie { get; private set; }
        public int PhaseLensCooldown { get; private set; }
        public float Stability => ReverieSystem.SharedStability;

        /// <summary>由原版进度决定的回响庭层（0 = 尚未解锁）。宪法 7「不越阶」。</summary>
        public int ReverieTier
        {
            get
            {
                if (!NPC.downedBoss1) return 0;
                if (NPC.downedMoonlord) return 4;
                if (NPC.downedPlantBoss) return 3;
                if (Main.hardMode) return 2;
                return 1;
            }
        }

        // ── 装备标志（每帧由 ResetEffects 清零）────────────────
        public bool HasSilentPendant, HasTwoWayMirror, HasUnspokenPendant,
                     HasFolioBoots, HasBlankRing, HasFoldblade, HasOffsetBow, HasUnfinishedPage;

        // ── 饰品 / 装备运行时效果 ─────────────────────────────
        public bool RollbackReady { get; private set; } = true;
        public int DeviationCount { get; private set; }
        public int BlankRingCooldown { get; private set; }
        public bool IgnoreNextResistance { get; set; }
        public int UnspokenStacks { get; private set; }

        // 折光刃：延迟回响斩
        public bool EchoSlashPending { get; private set; }
        private Vector2 _echoSlashPos;
        private int _echoSlashDamage;
        private int _echoSlashTimer;

        // 错位弦：上一支箭的落点
        public Vector2 LastArrowLanding { get; set; }
        public bool HasArrowLanding { get; set; }

        // 缄口：被「收录」的武器
        public int SilencedItemType { get; private set; } = -1;
        public int SilenceTimer { get; private set; }
        public bool IsSilenced => SilenceTimer > 0 && SilencedItemType >= 0;
        private int _lastSilenceAnim;

        // 折页靴残影（承接一次回响斩）
        public Vector2 AfterimagePosition { get; set; }
        public bool HasAfterimage { get; set; }

        /// <summary>有效滑动窗口长度：静默坠把它缩短一半，更快摆脱被针对。</summary>
        public int WindowSize => HasSilentPendant ? Balance.LedgerWindow / 2 : Balance.LedgerWindow;

        public override void ResetEffects()
        {
            HasSilentPendant = HasTwoWayMirror = HasUnspokenPendant =
                HasFolioBoots = HasBlankRing = HasFoldblade = HasOffsetBow = HasUnfinishedPage = false;
        }

        public override void PostUpdateEquips()
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null) return;

            if (!cfg.Enabled)
            {
                if (InReverie) ExitReverie(true);
                return;
            }

            // 死亡即离开：稳定度是共享真源，尸体留在庭内没有意义，
            // 也会让复活后的第一帧出现「稳定度为 0 却仍在庭内」的错位。
            if (Player.dead)
            {
                if (InReverie) ExitReverie(false);
                return;
            }

            _idleTicks++;
            // 兜底采样：只有长时间没有精确命中记录（例如纯跑图）才估计战术倾向，
            // 避免把假样本灌进谱系。正式路径是命中钩子调用 RecordStrike。
            if (_idleTicks > 360 && _window.Count == 0 && ++_sampleTimer >= 30)
            {
                _sampleTimer = 0;
                RecordStrike(SampleFromCurrentState(), precise: false);
            }

            if (PhaseLensCooldown > 0) PhaseLensCooldown--;
            if (BlankRingCooldown > 0) BlankRingCooldown--;

            TickEchoSlash();
            TickSilence();
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (!HasTwoWayMirror || !InReverie || !RollbackReady) return;
            if (AfterechoKeybinds.RollbackLedger == null) return;
            if (!AfterechoKeybinds.RollbackLedger.JustPressed) return;

            if (Stability < Balance.TwoWayMirrorStabilityCost)
            {
                Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.RollbackLowStability"),
                    new Color(190, 150, 130));
                return;
            }

            RollbackSnapshot();
            ReverieSystem.SharedStability = Math.Max(0f,
                ReverieSystem.SharedStability - Balance.TwoWayMirrorStabilityCost);
            RollbackReady = false;
            Main.NewText(Language.GetText("Mods.Afterecho.Messages.RollbackDone")
                    .Format((int)Balance.TwoWayMirrorStabilityCost),
                new Color(210, 205, 190));
        }

        // ── 谱系核心 ─────────────────────────────────────────

        public void RecordStrike(in LedgerStrike strike, bool precise = true)
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null || !cfg.Enabled) return;
            if (precise) _idleTicks = 0;
            LastStrikeSchool = strike.School;

            // 留白戒：连续 3 次不使用主导战术 → 无视一次软抗性。
            var dominant = DominantStrike();
            if (strike.School != dominant.School)
            {
                DeviationCount++;
                if (HasBlankRing && BlankRingCooldown <= 0 && DeviationCount >= 3)
                {
                    IgnoreNextResistance = true;
                    BlankRingCooldown = 8 * 60;
                    DeviationCount = 0;
                }
            }
            else DeviationCount = 0;

            _window.Enqueue(strike);
            TrimWindow();
            RefreshPosture();
        }

        /// <summary>召唤物/高频弹幕专用，按 tick 节流，防止谱系被刷屏覆盖。</summary>
        public void RecordStrikeThrottled(in LedgerStrike strike)
        {
            int tick = (int)Main.GameUpdateCount;
            if (tick - _lastThrottledTick < 6) return;
            _lastThrottledTick = tick;
            RecordStrike(strike, precise: true);
        }

        private void TrimWindow()
        {
            while (_window.Count > WindowSize) _window.Dequeue();
        }

        public LedgerStrike DominantStrike()
        {
            if (_window.Count == 0)
                return new LedgerStrike(StrikeSchool.Other, StrikeRange.Mid, StrikeMotion.Still, StrikeApproach.Front);

            int[] school = new int[5], range = new int[4], motion = new int[4], approach = new int[3];
            int skip = Math.Max(0, _window.Count - Balance.DominanceWindow);
            int index = 0;
            foreach (var s in _window)
            {
                if (index++ < skip) continue;
                school[(int)s.School]++;
                range[(int)s.Range]++;
                motion[(int)s.Motion]++;
                approach[(int)s.Approach]++;
            }
            return new LedgerStrike(
                (StrikeSchool)ArgMax(school),
                (StrikeRange)ArgMax(range),
                (StrikeMotion)ArgMax(motion),
                (StrikeApproach)ArgMax(approach));
        }

        /// <summary>姿态与具体行为一一对应；这里只做「描述」，绝不改数值。</summary>
        public static PostureKind DescribePosture(in LedgerStrike d) => d.Range switch
        {
            StrikeRange.Far when d.Motion == StrikeMotion.Still => PostureKind.Skimmer,
            StrikeRange.Contact when d.Motion == StrikeMotion.Dashing => PostureKind.Waller,
            StrikeRange.Mid when d.School == StrikeSchool.Magic => PostureKind.Folder,
            _ => PostureKind.Normal
        };

        /// <summary>多人共享实例时取队伍姿态均值，避免单人被针对性针对。</summary>
        public PostureKind CurrentPosture()
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null || !cfg.Enabled || !cfg.EnableCounterform) return PostureKind.Normal;
            return ReverieSystem.SquadPlayerCount > 1 ? ReverieSystem.SquadPosture : Posture;
        }

        public PostureKind CurrentPostureRaw() => Posture;

        /// <summary>
        /// 权重最高的伤害类型，供「对页」决定翻哪两页。
        /// 与主导战术同窗口（最近 DominanceWindow 次），页内容才会随换打法立刻变化。
        /// </summary>
        public List<StrikeSchool> TopSchools(int count = 2)
        {
            int[] tally = new int[5];
            int skip = Math.Max(0, _window.Count - Balance.DominanceWindow);
            int index = 0;
            foreach (var s in _window)
            {
                if (index++ < skip) continue;
                tally[(int)s.School]++;
            }
            var order = new List<int> { 0, 1, 2, 3, 4 };
            order.Sort((a, b) => tally[b].CompareTo(tally[a]));
            var result = new List<StrikeSchool>();
            for (int i = 0; i < order.Count && result.Count < count; i++)
                if (tally[order[i]] > 0) result.Add((StrikeSchool)order[i]);
            return result;
        }

        /// <summary>出现频次最高的 5 组动作，供终局拓本与残影抽取。</summary>
        public List<LedgerAction> TopActions(int count = 5)
        {
            var tally = new Dictionary<string, int>();
            var samples = new Dictionary<string, LedgerStrike>();
            foreach (var s in _window)
            {
                string key = $"{s.School}·{s.Range}·{s.Motion}";
                tally[key] = tally.TryGetValue(key, out int n) ? n + 1 : 1;
                samples[key] = s;
            }
            return tally.OrderByDescending(kv => kv.Value)
                        .ThenBy(kv => kv.Key)
                        .Take(count)
                        .Select(kv =>
                        {
                            var sample = samples[kv.Key];
                            return new LedgerAction(kv.Key,
                                LedgerText.Action(sample.School, sample.Range, sample.Motion));
                        })
                        .ToList();
        }

        /// <summary>全谱回响：主动擦除一条自己的记录，只影响称号外观，不给属性。</summary>
        public bool EraseAction(string key)
        {
            if (string.IsNullOrEmpty(key) || _window.Count == 0) return false;
            var kept = _window.Where(s => $"{s.School}·{s.Range}·{s.Motion}" != key).ToArray();
            if (kept.Length == _window.Count) return false;
            _window.Clear();
            foreach (var s in kept) _window.Enqueue(s);
            RecomputePosture();
            return true;
        }

        // ── 快照（双向镜）────────────────────────────────────

        private void TakeSnapshot()
        {
            _snapshot = _window.ToArray();
            _snapshotTaken = true;
        }

        private void RollbackSnapshot()
        {
            if (!_snapshotTaken) { RecomputePosture(); return; }
            _window.Clear();
            foreach (var s in _snapshot) _window.Enqueue(s);
            TrimWindow();
            RecomputePosture();
        }

        private void RecomputePosture()
        {
            var dominant = DominantStrike();
            Posture = DescribePosture(dominant);
            if (AfterechoConfig.Instance?.DebugShowLedger == true)
            {
                Main.NewText(Language.GetText("Mods.Afterecho.Messages.DebugLedger")
                    .Format(dominant.ToString(), LedgerText.Posture(Posture)),
                    new Color(150, 180, 200));
            }
        }

        /// <summary>
        /// 姿态每 15 tick 最多重算一次。400 条窗口的重算很便宜，但没必要
        /// 每发弹幕都跑一遍；这也让「换打法 ≈ 几秒内改变世界的回应」成立。
        /// </summary>
        private void RefreshPosture()
        {
            int tick = (int)Main.GameUpdateCount;
            if (_lastPostureTick >= 0 && tick - _lastPostureTick < 15) return;
            _lastPostureTick = tick;
            RecomputePosture();
        }

        // ── 战斗轨迹 ─────────────────────────────────────────

        public IReadOnlyList<Vector2> LastFightTrace => _lastFightTrace;

        public void BeginFightTrace()
        {
            if (_currentTrace.Count > 0) _lastFightTrace = new List<Vector2>(_currentTrace);
            _currentTrace.Clear();
            _traceTimer = 0;
        }

        public void EndFightTrace()
        {
            if (_currentTrace.Count > 0) _lastFightTrace = new List<Vector2>(_currentTrace);
            _currentTrace.Clear();
        }

        /// <summary>由描摹者 AI 调用，每 12 tick 记一个点。</summary>
        public void AppendTrace(Vector2 position)
        {
            if (++_traceTimer % 12 != 0) return;
            _currentTrace.Add(position);
            if (_currentTrace.Count > 240) _currentTrace.RemoveAt(0);
        }

        // ── 折光刃：0.8 秒后在「上一秒所在位置」生成回响斩 ────

        public void QueueEchoSlash(Vector2 position, int damage)
        {
            EchoSlashPending = true;
            _echoSlashPos = position;
            _echoSlashDamage = damage;
            _echoSlashTimer = 48; // 0.8 秒
        }

        private void TickEchoSlash()
        {
            if (!EchoSlashPending) return;
            if (--_echoSlashTimer > 0) return;

            EchoSlashPending = false;
            Vector2 spawn = HasAfterimage ? AfterimagePosition : _echoSlashPos;
            HasAfterimage = false;

            // 残影承接一次回响斩（折页靴）：只是换生成点，不放大伤害。
            Projectile.NewProjectile(
                Player.GetSource_FromThis(), spawn, Vector2.Zero,
                ModContent.ProjectileType<EchoSlash>(),
                _echoSlashDamage, 3f, Player.whoAmI);
        }

        // ── 缄口：收录武器 / 未言层数 / 切武器释放 ──────────────

        public void ApplySilence(int itemType, int seconds)
        {
            SilencedItemType = itemType;
            SilenceTimer = 60 * seconds + (HasUnspokenPendant ? 60 : 0); // 代价：收录时长 +1 秒
            UnspokenStacks = 0;
            _lastSilenceAnim = 0;
        }

        public void AddUnspoken()
        {
            UnspokenStacks += HasUnspokenPendant ? 2 : 1;
        }

        private void TickSilence()
        {
            if (!IsSilenced) return;
            SilenceTimer--;

            // 被封印期间的每次挥击都积累「未言」。用 itemAnimation 防重复计数。
            if (Player.HeldItem.type == SilencedItemType && Player.itemAnimation > 0 && _lastSilenceAnim == 0)
                AddUnspoken();
            _lastSilenceAnim = Player.itemAnimation > 0 ? 1 : 0;

            bool switched = Player.HeldItem.type != SilencedItemType;
            if (SilenceTimer <= 0 || switched)
                ReleaseSilence();
        }

        private void ReleaseSilence()
        {
            int stacks = UnspokenStacks;
            UnspokenStacks = 0;
            SilencedItemType = -1;
            SilenceTimer = 0;
            _lastSilenceAnim = 0;

            if (stacks <= 0) return;
            // 层数一次性释放为范围回响；伤害按层累加但有硬上限，避免变成数值奖励。
            int damage = Math.Min(stacks * 6, 90);
            Projectile.NewProjectile(
                Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                ModContent.ProjectileType<PageBurst>(),
                damage, 4f, Player.whoAmI);
        }

        // ── 回响庭进出 ───────────────────────────────────────

        public bool EnterReverie()
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null || !cfg.Enabled || InReverie) return false;
            if (PhaseLensCooldown > 0)
            {
                Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.PhaseLensCooldown"),
                    new Color(180, 172, 155));
                return false;
            }

            if (ReverieTier == 0)
            {
                Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.ReverieLocked"),
                    new Color(190, 185, 170));
                return false;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && n.boss)
                {
                    Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.ReverieBossBlocked"),
                        new Color(190, 185, 170));
                    return false;
                }
            }

            InReverie = true;
            ReverieSystem.SharedStability = Balance.StabilityMax;
            PhaseLensCooldown = 60 * Balance.PhaseLensCooldownSeconds;
            RollbackReady = true;
            _snapshotTaken = false;
            TakeSnapshot();
            RecomputePosture();
            SoundEngine.PlaySound(SoundID.Item6, Player.Center);
            Main.NewText(Language.GetText("Mods.Afterecho.Messages.ReverieEntered").Format(ReverieTier),
                new Color(210, 205, 190));
            return true;
        }

        public bool ExitReverie(bool keepLoot = true)
        {
            if (!InReverie) return false;
            InReverie = false;
            EndFightTrace();
            EchoSlashPending = false;
            HasArrowLanding = false;
            SilencedItemType = -1;
            SilenceTimer = 0;
            UnspokenStacks = 0;
            _lastSilenceAnim = 0;
            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
            // keepLoot = true：已获材料留在背包，不做任何回收。
            // 主世界 tile / 掉落表 / 刷怪表从未被修改，无需任何还原操作。
            Main.NewText(Language.GetTextValue(keepLoot
                    ? "Mods.Afterecho.Messages.ReverieExited"
                    : "Mods.Afterecho.Messages.ReverieExitedPlain"),
                new Color(190, 185, 170));
            return true;
        }

        // ── 辅助 ─────────────────────────────────────────────

        private LedgerStrike SampleFromCurrentState()
        {
            var held = Player.HeldItem;
            var school = AfterechoGlobalItem.SchoolOf(held);
            var motion = MotionOf(Player);
            return new LedgerStrike(school, RangeOf(Player, null), motion, StrikeApproach.Front);
        }

        internal static StrikeMotion MotionOf(Player p)
        {
            float speed = p.velocity.Length();
            return p.wingTime > 0 || p.velocity.Y < -2f ? StrikeMotion.Flying
                : speed > 7f ? StrikeMotion.Dashing
                : speed > 1f ? StrikeMotion.Walking
                : StrikeMotion.Still;
        }

        internal static StrikeRange RangeOf(Player p, NPC target)
        {
            float nearest = float.MaxValue;
            if (target != null && target.active)
                nearest = Vector2.Distance(p.Center, target.Center);
            else
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (!n.active || n.friendly || n.lifeMax <= 5) continue;
                    float d = Vector2.Distance(p.Center, n.Center);
                    if (d < nearest) nearest = d;
                }
            }
            return nearest == float.MaxValue ? StrikeRange.Mid
                : nearest < 64f ? StrikeRange.Contact
                : nearest < 240f ? StrikeRange.Near
                : nearest < 560f ? StrikeRange.Mid
                : StrikeRange.Far;
        }

        internal static StrikeApproach ApproachOf(Player p, NPC target)
        {
            if (target == null) return StrikeApproach.Front;
            float dx = target.Center.X - p.Center.X;
            bool behind = MathF.Abs(dx) > 24f && MathF.Sign(dx) != MathF.Sign(target.direction);
            if (behind) return StrikeApproach.Back;
            if (MathF.Abs(target.Center.Y - p.Center.Y) > 64f) return StrikeApproach.Flank;
            return StrikeApproach.Front;
        }

        private static int ArgMax(int[] values)
        {
            int best = 0;
            for (int i = 1; i < values.Length; i++)
                if (values[i] > values[best]) best = i;
            return best;
        }
    }
}
