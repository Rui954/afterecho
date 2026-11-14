using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Afterecho.Content.Items
{
    /// <summary>
    /// 相位镜：切入 / 退出回响庭。
    /// 宪法 2 / 7：所有内容都必须由玩家主动使用才会出现；
    /// 可进入的层数由原版进度直接决定，未击败克苏鲁之眼时连第一层都不开放。
    /// 配方只用原版材料，不新增矿物、不新增制作站。
    /// </summary>
    public class PhaseLens : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName / Tooltip 见 Localization
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 30;
            Item.maxStack = 1;
            Item.value = Item.sellPrice(silver: 30);
            Item.rare = ItemRarityID.Blue;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useTurn = true;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player)
        {
            var ledger = player.GetModPlayer<LedgerPlayer>();
            return ledger.InReverie ? ledger.ExitReverie(true) : ledger.EnterReverie();
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Lens, 1)
                .AddIngredient(ItemID.FallenStar, 3)
                .AddIngredient(ItemID.Glass, 10)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
