using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Afterecho.Content.Items.Weapons
{
    /// <summary>
    /// 折光刃：本体伤害是同阶剑的 70%，但每次命中 0.8 秒后会在
    /// 「玩家上一秒所在位置」补一记回响斩（由 AfterechoGlobalItem 触发）。
    /// 代价：本体伤害低；收益：走位本身变成输出。纯横向，不是数值上位替代。
    /// </summary>
    public class Foldblade : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 40;
            Item.damage = 20;           // 同阶 28 × 0.70
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.knockBack = 4f;
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Blue;
            Item.autoReuse = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                // 金锭或铂金锭：世界只会生成其中一种，配方组保证两边都能做。
                .AddRecipeGroup(AfterechoRecipeGroups.GoldBarGroup, 8)
                .AddIngredient(ModContent.ItemType<EchoShard>(), 6)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
