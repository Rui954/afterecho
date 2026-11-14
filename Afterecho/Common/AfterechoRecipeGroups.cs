using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Afterecho
{
    /// <summary>
    /// 配方组：解决「金/银锭硬编码」的世界生成问题。
    ///
    /// 一个泰拉瑞亚世界只会生成 金 或 铂金、银 或 钨 中的一种。
    /// 如果折光刃写死金锭、留白戒写死银锭，另一半世界就永远做不出来。
    /// 用配方组把两种等价金属都算进去，配方本身的数量与代价不变。
    /// </summary>
    public class AfterechoRecipeGroups : ModSystem
    {
        public const string GoldBarGroup = "Afterecho:GoldBar";
        public const string SilverBarGroup = "Afterecho:SilverBar";

        public override void AddRecipeGroups()
        {
            RecipeGroup gold = new RecipeGroup(
                () => Language.GetTextValue("Mods.Afterecho.RecipeGroups.GoldBar"),
                ItemID.GoldBar, ItemID.PlatinumBar);
            RecipeGroup.RegisterGroup(GoldBarGroup, gold);

            RecipeGroup silver = new RecipeGroup(
                () => Language.GetTextValue("Mods.Afterecho.RecipeGroups.SilverBar"),
                ItemID.SilverBar, ItemID.TungstenBar);
            RecipeGroup.RegisterGroup(SilverBarGroup, silver);
        }
    }
}
