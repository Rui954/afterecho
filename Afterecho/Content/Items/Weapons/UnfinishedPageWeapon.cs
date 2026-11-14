using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Afterecho.Content.Projectiles;

namespace Afterecho.Content.Items.Weapons
{
    /// <summary>
    /// 残页：放出「未写完的一页」，3 秒后补完为范围伤害，
    /// 补完位置会一直跟随玩家移动（PageSeed.AI）。
    /// 代价：魔力消耗 ×1.6、无法连续吟唱（autoReuse = false）。
    /// </summary>
    public class UnfinishedPageWeapon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 30;
            Item.damage = 30;
            Item.DamageType = DamageClass.Magic;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.mana = 22;             // 同阶 14 × 1.6
            Item.shoot = ModContent.ProjectileType<PageSeed>();
            Item.shootSpeed = 0f;
            Item.knockBack = 2f;
            Item.autoReuse = false;      // 无法连续吟唱
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Book, 1)
                .AddIngredient(ItemID.FallenStar, 5)
                .AddIngredient(ModContent.ItemType<EchoShard>(), 8)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
