using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Afterecho.Content.Items
{
    /// <summary>
    /// 全谱拓本：终局纪念品，纯外观 / 称号，无任何属性。
    /// 它把整局被记录下来的 5 个最高频动作写在 tooltip 里 ——
    /// 这是模组唯一一处「把谱系数据明文给玩家看」的地方，且发生在通关之后。
    /// </summary>
    public class FullScoreCodex : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 1);
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.consumable = false;
        }

        /// <summary>
        /// 全谱回响：主动擦除一条自己被记住的动作。
        /// 只影响称号 / 外观，不提供任何属性奖励 —— 玩家自己决定难度。
        /// </summary>
        public override bool? UseItem(Player player)
        {
            var ledger = player.GetModPlayer<LedgerPlayer>();
            var actions = ledger.TopActions(5);
            if (actions.Count == 0)
            {
                Main.NewText(Language.GetTextValue("Mods.Afterecho.Messages.CodexNothing"),
                    new Color(190, 185, 170));
                return false;
            }
            if (!ledger.EraseAction(actions[0].Key)) return false;

            Main.NewText(Language.GetText("Mods.Afterecho.Messages.CodexErased").Format(actions[0].Display),
                new Color(215, 205, 185));
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var actions = Main.LocalPlayer.GetModPlayer<LedgerPlayer>().TopActions(5);
            if (actions.Count == 0)
            {
                tooltips.Add(new TooltipLine(Mod, "AfterechoEmpty",
                    Language.GetTextValue("Mods.Afterecho.Tooltips.CodexEmpty"))
                {
                    OverrideColor = new Color(150, 145, 135)
                });
                return;
            }

            tooltips.Add(new TooltipLine(Mod, "AfterechoHeader",
                Language.GetTextValue("Mods.Afterecho.Tooltips.CodexHeader"))
            {
                OverrideColor = new Color(180, 150, 110)
            });
            tooltips.Add(new TooltipLine(Mod, "AfterechoUsage",
                Language.GetTextValue("Mods.Afterecho.Tooltips.CodexUsage"))
            {
                OverrideColor = new Color(150, 145, 135)
            });
            for (int i = 0; i < actions.Count; i++)
            {
                tooltips.Add(new TooltipLine(Mod, "AfterechoAction" + i, $"  {i + 1}. {actions[i].Display}")
                {
                    OverrideColor = new Color(120, 118, 112)
                });
            }
        }
    }
}
