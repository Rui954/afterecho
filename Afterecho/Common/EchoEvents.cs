using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Afterecho.Content.Items;

namespace Afterecho
{
    public enum EchoEventKind { None, BlankWalk, Offhand, Counterscore, Blank }

    /// <summary>
    /// 设计案第 4 节的「可选回响事件」：每幕一个，非 Boss，是玩法小挑战。
    ///
    /// 设计约束：
    ///  - 全部可跳过：不参加、走开、或者让它超时，都只是「没发生」，不扣任何东西；
    ///  - 不参与主线解锁：奖励只有回声砂 / 残谱，不设置任何进度标记；
    ///  - 不改数值：事件只读取玩家行为（移动落点、伤害类型、是否出手），
    ///    不修改任何 NPC 的生命 / 伤害 / 防御；
    ///  - Boss 战期间自动打断，避免和四幕 Boss 的节奏互相干扰。
    ///
    /// 多人：事件状态由服务端推进（客户端只负责本地表现），
    /// 奖励由服务端发放，避免重复领取。
    /// </summary>
    public class EchoEvents : ModSystem
    {
        public static EchoEventKind Current { get; private set; } = EchoEventKind.None;
        public static int TicksLeft { get; private set; }

        private static int _offerTimer;
        private static bool _wasInReverie;
        private static int _rotation;

        // 空走：依次踩过的落点
        private static readonly Vector2[] _marks = new Vector2[Balance.EchoEventMarkers];
        private static int _markCount;
        private static int _markIndex;

        // 换手 / 逆谱
        private static StrikeSchool _dominantAtStart;
        private static int _hitCount;
        private static readonly System.Collections.Generic.HashSet<StrikeSchool> _schoolsSeen
            = new System.Collections.Generic.HashSet<StrikeSchool>();

        // 留白
        private static bool _struck;

        public override void PostUpdateEverything()
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null || !cfg.Enabled) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            if (ReveriePlayerCount() == 0)
            {
                if (_wasInReverie) HardReset();
                _wasInReverie = false;
                return;
            }

            if (!_wasInReverie)
            {
                _wasInReverie = true;
                _offerTimer = 60 * Balance.EchoEventFirstDelaySeconds;
            }

            // Boss 在场 / 事件被关掉：立刻收掉当前事件，不打扰 Boss 节奏。
            if (ReverieSystem.BossActive || !cfg.EnableEchoEvents)
            {
                if (Current != EchoEventKind.None) Cancel();
                return;
            }

            if (Current == EchoEventKind.None)
            {
                if (--_offerTimer > 0) return;
                Start(ChooseKind());
                return;
            }

            TicksLeft--;
            UpdateActive();
            if (TicksLeft <= 0 && Current != EchoEventKind.None) Fail();
        }

        /// <summary>命中钩子：所有有效的武器 / 弹幕命中都会到这里。</summary>
        public static void OnStrike(Player player, StrikeSchool school)
        {
            var cfg = AfterechoConfig.Instance;
            if (cfg == null || !cfg.Enabled) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (Current == EchoEventKind.None || school == StrikeSchool.Other) return;

            switch (Current)
            {
                case EchoEventKind.Offhand:
                    // 主导战术在事件开始时固定，事件中途换打法也算「换了」。
                    if (_dominantAtStart == StrikeSchool.Other || school != _dominantAtStart)
                        _hitCount++;
                    if (_hitCount >= 3) Complete();
                    break;

                case EchoEventKind.Counterscore:
                    _schoolsSeen.Add(school);
                    if (_schoolsSeen.Count >= 3) Complete();
                    break;

                case EchoEventKind.Blank:
                    // 留白：出手即失败 —— 这是整场事件唯一的「代价」。
                    _struck = true;
                    break;
            }
        }

        public static void Cancel()
        {
            if (Current == EchoEventKind.None) return;
            Main.NewText(Language.GetTextValue("Mods.Afterecho.Events.Cancelled"), new Color(180, 172, 155));
            HardReset();
            // Boss 战打断后重新排队，避免 Boss 一死就立刻弹出下一个事件。
            _offerTimer = 60 * Balance.EchoEventIntervalSeconds;
        }

        // ── 内部 ─────────────────────────────────────────────

        private static int ReveriePlayerCount()
        {
            int n = 0;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead && p.GetModPlayer<LedgerPlayer>().InReverie) n++;
            }
            return n;
        }

        private static Player FirstParticipant()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead && p.GetModPlayer<LedgerPlayer>().InReverie) return p;
            }
            return null;
        }

        private static EchoEventKind ChooseKind()
        {
            int tier = 1;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead || !p.GetModPlayer<LedgerPlayer>().InReverie) continue;
                tier = Math.Max(tier, p.GetModPlayer<LedgerPlayer>().ReverieTier);
            }
            tier = Utils.Clamp(tier, 1, 4);

            // 已解锁的事件里轮换，避免高层玩家永远只看到同一个。
            _rotation++;
            return (tier) switch
            {
                1 => EchoEventKind.BlankWalk,
                2 => _rotation % 2 == 0 ? EchoEventKind.BlankWalk : EchoEventKind.Offhand,
                3 => (_rotation % 3) switch
                {
                    0 => EchoEventKind.BlankWalk,
                    1 => EchoEventKind.Offhand,
                    _ => EchoEventKind.Counterscore
                },
                _ => (_rotation % 4) switch
                {
                    0 => EchoEventKind.BlankWalk,
                    1 => EchoEventKind.Offhand,
                    2 => EchoEventKind.Counterscore,
                    _ => EchoEventKind.Blank
                }
            };
        }

        private static void Start(EchoEventKind kind)
        {
            if (kind == EchoEventKind.None) return;

            Current = kind;
            ResetProgress();

            int duration = kind == EchoEventKind.Blank
                ? Balance.EchoEventBlankDurationSeconds
                : Balance.EchoEventDurationSeconds;
            TicksLeft = 60 * duration;

            string name = Localized(kind, "Name");
            Main.NewText(Language.GetText("Mods.Afterecho.Events.Started").Format(name),
                new Color(215, 208, 190));
            Main.NewText(Localized(kind, "Start"), new Color(178, 172, 156));

            if (kind == EchoEventKind.BlankWalk) BuildWalkMarks();
            if (kind == EchoEventKind.Offhand)
            {
                Player p = FirstParticipant();
                _dominantAtStart = p != null
                    ? p.GetModPlayer<LedgerPlayer>().DominantStrike().School
                    : StrikeSchool.Other;
            }

            Player anchor = FirstParticipant();
            if (anchor != null) SoundEngine.PlaySound(SoundID.Item4, anchor.Center);
        }

        private static void UpdateActive()
        {
            switch (Current)
            {
                case EchoEventKind.BlankWalk:
                    UpdateBlankWalk();
                    break;
                case EchoEventKind.Blank:
                    if (_struck) Fail();
                    break;
            }
        }

        private static void UpdateBlankWalk()
        {
            Player p = FirstParticipant();
            if (p == null) return;

            for (int i = 0; i < _markCount; i++)
            {
                bool next = i == _markIndex;
                if (!Main.rand.NextBool(next ? 2 : 7)) continue;
                Vector2 jitter = new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-10f, 10f));
                Dust.NewDustPerfect(_marks[i] + jitter,
                    next ? DustID.MagicMirror : DustID.Smoke,
                    Vector2.Zero, 0, default, next ? 1.2f : 0.8f);
            }

            if (_markIndex >= _markCount) { Complete(); return; }

            if (Vector2.Distance(p.Center, _marks[_markIndex]) > 44f) return;
            _markIndex++;
            SoundEngine.PlaySound(SoundID.Item4, p.Center);
            if (_markIndex >= _markCount) Complete();
        }

        private static void BuildWalkMarks()
        {
            Player p = FirstParticipant();
            if (p == null) return;

            float baseAngle = Main.rand.NextFloat(MathF.PI * 2f);
            for (int i = 0; i < Balance.EchoEventMarkers; i++)
            {
                float a = baseAngle + i * MathF.PI * 2f / Balance.EchoEventMarkers
                          + Main.rand.NextFloat(-0.3f, 0.3f);
                float r = Main.rand.NextFloat(190f, 330f);
                Vector2 guess = p.Center + new Vector2(MathF.Cos(a) * r, MathF.Sin(a) * r * 0.55f);
                _marks[i] = SnapToAir(guess);
            }
            _markCount = Balance.EchoEventMarkers;
            _markIndex = 0;
        }

        /// <summary>把落点吸到最近的空气格，避免埋在墙里导致事件必然失败。</summary>
        private static Vector2 SnapToAir(Vector2 guess)
        {
            int tx = (int)(guess.X / 16f);
            int startTy = (int)(guess.Y / 16f);
            for (int ty = Math.Max(10, startTy); ty < startTy + 24; ty++)
            {
                if (!WorldGen.InWorld(tx, ty, 2)) break;
                Tile tile = Main.tile[tx, ty];
                Tile below = Main.tile[tx, ty + 1];
                if (tile.HasTile) continue;
                if (below.HasTile) return new Vector2(tx * 16f + 8f, ty * 16f + 8f);
            }
            return guess;
        }

        private static void Complete()
        {
            if (Current == EchoEventKind.None) return;
            EchoEventKind kind = Current;
            string name = Localized(kind, "Name");

            Player p = FirstParticipant();
            if (p != null)
            {
                int itemType = kind == EchoEventKind.BlankWalk || kind == EchoEventKind.Blank
                    ? ModContent.ItemType<EchoShard>()
                    : ModContent.ItemType<RescorePage>();
                int count = kind switch
                {
                    EchoEventKind.BlankWalk => 3,
                    EchoEventKind.Offhand => 1,
                    EchoEventKind.Counterscore => 2,
                    EchoEventKind.Blank => 5,
                    _ => 1
                };
                Item.NewItem(p.GetSource_FromThis(), p.Center, itemType, count);
            }

            Main.NewText(Language.GetText("Mods.Afterecho.Events.Completed").Format(name),
                new Color(215, 205, 185));
            End();
        }

        private static void Fail()
        {
            if (Current == EchoEventKind.None) return;
            string name = Localized(Current, "Name");
            Main.NewText(Language.GetText("Mods.Afterecho.Events.Failed").Format(name),
                new Color(170, 165, 150));
            End();
        }

        private static void End()
        {
            Current = EchoEventKind.None;
            TicksLeft = 0;
            ResetProgress();
            _offerTimer = 60 * Balance.EchoEventIntervalSeconds;
        }

        private static void HardReset()
        {
            Current = EchoEventKind.None;
            TicksLeft = 0;
            ResetProgress();
        }

        private static void ResetProgress()
        {
            _markCount = 0;
            _markIndex = 0;
            _hitCount = 0;
            _schoolsSeen.Clear();
            _struck = false;
            _dominantAtStart = StrikeSchool.Other;
        }

        private static string Localized(EchoEventKind kind, string field)
            => Language.GetTextValue($"Mods.Afterecho.Events.{kind}.{field}");

        public override void Unload()
        {
            Current = EchoEventKind.None;
            TicksLeft = 0;
            _wasInReverie = false;
            _offerTimer = 0;
            _rotation = 0;
            ResetProgress();
        }
    }
}
