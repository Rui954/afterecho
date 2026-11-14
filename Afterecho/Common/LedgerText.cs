using Terraria.Localization;

namespace Afterecho
{
    /// <summary>姿态的内部标识。显示名走本地化，判断永远用枚举。</summary>
    public enum PostureKind { Normal, Skimmer, Waller, Folder }

    /// <summary>
    /// 谱系维度的显示名。
    ///
    /// 内部标识（Key）始终用英文枚举名，只有显示名走本地化 ——
    /// 否则中文客户端记下的「近战·近·行走」和英文客户端的
    /// 「Melee·Near·Walking」会被当成两条不同的动作。
    /// </summary>
    public static class LedgerText
    {
        public static string School(StrikeSchool s)
            => Language.GetTextValue($"Mods.Afterecho.Ledger.School.{s}");

        public static string Range(StrikeRange r)
            => Language.GetTextValue($"Mods.Afterecho.Ledger.Range.{r}");

        public static string Motion(StrikeMotion m)
            => Language.GetTextValue($"Mods.Afterecho.Ledger.Motion.{m}");

        public static string Approach(StrikeApproach a)
            => Language.GetTextValue($"Mods.Afterecho.Ledger.Approach.{a}");

        public static string Posture(PostureKind p)
            => Language.GetTextValue($"Mods.Afterecho.Ledger.Posture.{p}");

        /// <summary>「近战·近·行走」—— 未含来向，用于拓本与事件。</summary>
        public static string Action(StrikeSchool s, StrikeRange r, StrikeMotion m)
            => $"{School(s)}·{Range(r)}·{Motion(m)}";

        /// <summary>「近战·近·行走·正面」—— 四维全名，用于调试输出。</summary>
        public static string Strike(in LedgerStrike s)
            => $"{School(s.School)}·{Range(s.Range)}·{Motion(s.Motion)}·{Approach(s.Approach)}";
    }

    /// <summary>
    /// 一条被记录的动作：Key 用于内部比较与擦除（不随语言变化），
    /// Display 是本地化后的显示名。
    /// </summary>
    public readonly struct LedgerAction
    {
        public readonly string Key;
        public readonly string Display;

        public LedgerAction(string key, string display)
        {
            Key = key;
            Display = display;
        }
    }
}
