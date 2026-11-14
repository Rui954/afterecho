using System;
using System.Collections.Generic;

namespace Afterecho
{
    /// <summary>
    /// 「平衡宪法」的代码化（设计案第 3 节）。
    ///
    /// 整个模组唯一允许定义数值的地方。任何敌人 AI、姿态、深谱模式
    /// 都只能读取行为相关常量，禁止自行放大生命 / 伤害 / 防御。
    /// 加载期由 Afterecho.PostSetupContent → Validate() 断言全局约束，
    /// 每个 Boss 的 SetDefaults 再用 AssertBossStats 对齐原版锚点。
    /// </summary>
    public static class Balance
    {
        // ── 数值天花板（宪法 3、4）─────────────────────────────
        public const float EquipmentCeilingRatio = 1.05f;
        public const float BossStatFloor = 0.90f;
        public const float BossStatCeiling = 1.05f;

        // ── 行为层（宪法 5）────────────────────────────────────
        /// <summary>姿态软抗性绝对上限。不叠加、不随阶段成长。</summary>
        public const float PostureResistanceCap = 0.12f;

        public const float DeepResonanceStatMultiplier = 1.00f;
        public const float DeepProjectileCountBonus = 1.25f;
        public const float DeepProjectileDamageScale = 0.80f;

        // ── 谱系与回响庭（宪法 1、2、6）─────────────────────────
        public const int LedgerWindow = 400;

        /// <summary>
        /// 主导战术只看最近这么多次命中。设计案承诺「换打法约 20 秒内
        /// 就能改变世界的回应方式」；400 次窗口在低速武器下要一分多钟，
        /// 因此姿态判定读最近的 120 次，完整 400 次仍用于高频动作统计。
        /// </summary>
        public const int DominanceWindow = 120;

        public const float StabilityMax = 100f;
        public const float StabilityDrainPerSecond = 0.35f;
        public const int PhaseLensCooldownSeconds = 20;

        /// <summary>稳定度低于此比例时，庭内敌怪才掉落回声砂。</summary>
        public const float LowStabilityThreshold = 0.30f;

        /// <summary>回响庭内同时存在的普通敌怪上限（共享实例）。</summary>
        public const int MaxReverieEnemies = 8;

        /// <summary>生成节流（tick）。</summary>
        public const int SpawnIntervalTicks = 210;

        // ── 装备代价（每件装备写死的一条代价，宪法 4）─────────────
        /// <summary>静默坠的代价：稳定度消耗 +20%。</summary>
        public const float SilentPendantDrainMultiplier = 1.20f;

        /// <summary>折光刃的代价：本体伤害为同阶剑的 70%。</summary>
        public const float FoldbladeBodyRatio = 0.70f;

        /// <summary>错位弦的代价：单发伤害 −25%。</summary>
        public const float OffsetBowDamageRatio = 0.75f;

        /// <summary>残页的代价：魔力消耗 ×1.6。</summary>
        public const float UnfinishedPageManaRatio = 1.60f;

        /// <summary>留白戒的代价：触发后 8 秒无法再次触发。</summary>
        public const int BlankRingLockoutSeconds = 8;

        /// <summary>双向镜的代价：每次回滚扣 15 稳定度。</summary>
        public const float TwoWayMirrorStabilityCost = 15f;

        /// <summary>折页靴的代价：移动速度 −10%。</summary>
        public const float FolioBootsSpeedMultiplier = 0.90f;

        // ── 弹幕伤害锚点（按幕对齐原版同进度）───────────────────
        public const int TraceRibbonDamage = 18;   // 第一幕
        public const int InkBlotDamage = 24;       // 第二幕
        public const int SilenceWaveDamage = 34;   // 第三幕
        public const int MirrorShadeDamage = 46;   // 第四幕

        // ── 原版生命锚点（设计案第 3 节数值锚点表）───────────────
        public const int AnchorEyeOfCthulhu = 2800;    // 克苏鲁之眼，经典难度
        public const int AnchorWallOfFlesh = 8000;     // 血肉墙
        public const int AnchorPlantera = 30000;       // 世纪之花
        public const int AnchorMoonLord = 145000;      // 月亮领主

        // ── 可选回响事件（设计案第 4 节）───────────────────────
        public const int EchoEventFirstDelaySeconds = 45;
        public const int EchoEventIntervalSeconds = 90;
        public const int EchoEventDurationSeconds = 25;
        public const int EchoEventBlankDurationSeconds = 12;
        public const int EchoEventMarkers = 5;

        /// <summary>
        /// 深谱模式的弹幕预算。条数上升、单发伤害反比下调，
        /// 并保证 count × damage 不高于原预算（总承伤压力恒定 ≤ 1.00）。
        /// 所有 Boss / 弹幕都必须通过这里计算，禁止直接写死倍率。
        /// </summary>
        public static (int count, int damage) ScaleVolley(int baseCount, int baseDamage, bool deep)
        {
            if (!deep) return (baseCount, baseDamage);

            int count = Math.Max(1, (int)MathF.Round(baseCount * DeepProjectileCountBonus));
            int damage = Math.Max(1, (int)MathF.Floor(baseDamage * DeepProjectileDamageScale));
            int budget = baseCount * baseDamage;
            // 浮点取整可能让总量略超预算，逐点回退单发伤害直到守恒。
            while (count * damage > budget && damage > 1) damage--;
            return (count, damage);
        }

        /// <summary>
        /// Boss 数值必须在同锚点原版 Boss 的 0.90–1.05 倍之间（宪法 3、7）。
        /// 由每个 Boss 的 SetDefaults 调用：超区间直接抛异常阻止加载，
        /// 而不是留到实机平衡崩坏后才发现。
        /// </summary>
        public static void AssertBossStats(string boss, int lifeMax, int anchorLife)
        {
            if (anchorLife <= 0) return;
            float ratio = lifeMax / (float)anchorLife;
            if (ratio < BossStatFloor - 0.001f || ratio > BossStatCeiling + 0.001f)
            {
                throw new InvalidOperationException(
                    $"[余响/平衡宪法] {boss} 的生命 {lifeMax} 是原版锚点 {anchorLife} 的 {ratio:P0}，" +
                    $"超出 {BossStatFloor:P0}–{BossStatCeiling:P0} 区间。");
            }
        }

        /// <summary>
        /// 把「经典难度锚点」换算成当前难度的生命值。
        ///
        /// 原版 Boss 在专家 / 大师下会被放大，模组 Boss 不会自动获得这份放大；
        /// 这里按整体比例补上，保证「同锚点原版 Boss 的 0.90–1.05 倍」在
        /// 三个难度下都大致成立。放大只作用于生命，不改变任何行为参数。
        /// </summary>
        public static int ScaleBossLife(int classicLife)
        {
            if (!Terraria.Main.expertMode) return classicLife;
            return (int)(classicLife * (Terraria.Main.masterMode ? 1.60f : 1.35f));
        }

        public static bool Validate()
        {
            var failures = new List<string>();
            void Check(bool condition, string message)
            {
                if (!condition) failures.Add(message);
            }

            Check(EquipmentCeilingRatio <= 1.05f + float.Epsilon,
                "装备系数超过 1.05，请改为横向玩法变化并附带代价。");
            Check(Math.Abs(DeepResonanceStatMultiplier - 1.00f) < float.Epsilon,
                "深谱模式不得改变生命/伤害倍率，只允许改 AI 与弹幕编排。");
            Check(DeepProjectileCountBonus * DeepProjectileDamageScale <= 1.00f + 0.001f,
                "深谱模式的总承伤压力必须恒定为 1.00，请下调单发伤害。");
            Check(PostureResistanceCap <= 0.12f + float.Epsilon,
                "软抗性上限被突破，姿态将变成数值压制。");
            Check(BossStatFloor >= 0.90f && BossStatCeiling <= 1.05f,
                "Boss 数值超出同锚点原版区间。");
            Check(LedgerWindow >= 100,
                "谱系窗口过短，玩家将无法通过换打法破解。");
            Check(DominanceWindow > 0 && DominanceWindow <= LedgerWindow,
                "主导战术窗口必须落在 1–LedgerWindow 之间。");
            Check(MaxReverieEnemies <= 8,
                "庭内敌怪上限被抬高，主世界虽不生成但庭内会变成刷怪压力。");
            Check(LowStabilityThreshold < 0.5f,
                "低稳定度阈值过高会让材料获取变成强制压线，属于隐性数值惩罚。");

            // 宪法 4：每件装备必须有一条写死的代价，代价不能被调成「免费」。
            Check(SilentPendantDrainMultiplier > 1.00f,
                "静默坠缺少代价：稳定度消耗倍率必须 > 1.00。");
            Check(FoldbladeBodyRatio < 1.00f,
                "折光刃缺少代价：本体伤害系数必须 < 1.00。");
            Check(OffsetBowDamageRatio < 1.00f,
                "错位弦缺少代价：单发伤害系数必须 < 1.00。");
            Check(UnfinishedPageManaRatio > 1.00f,
                "残页缺少代价：魔力消耗系数必须 > 1.00。");
            Check(BlankRingLockoutSeconds > 0,
                "留白戒缺少代价：触发后必须有锁定时间。");
            Check(TwoWayMirrorStabilityCost > 0f,
                "双向镜缺少代价：回滚必须消耗稳定度。");
            Check(FolioBootsSpeedMultiplier < 1.00f,
                "折页靴缺少代价：移动速度系数必须 < 1.00。");

            // 深谱弹幕预算逐档验证，避免有人调了常量却没发现超模。
            foreach (int baseCount in new[] { 1, 3, 5, 8 })
            foreach (int baseDamage in new[] { 18, 24, 34, 46 })
            {
                var (c, d) = ScaleVolley(baseCount, baseDamage, true);
                Check(c * d <= baseCount * baseDamage,
                    $"深谱弹幕预算超模：{baseCount}×{baseDamage} → {c}×{d}。");
            }

            if (failures.Count > 0)
                throw new InvalidOperationException(
                    "[余响/平衡宪法] 数值层约束被突破：\n - " + string.Join("\n - ", failures));

            return true;
        }
    }
}
