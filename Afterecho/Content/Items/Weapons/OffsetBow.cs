using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Afterecho.Content.Projectiles;

namespace Afterecho.Content.Items.Weapons
{
    /// <summary>
    /// 错位弦：下一支箭从上一支箭的落点射出（落点链由 OffsetArrow 实现）。
    /// 代价：单发伤害 −25%（同阶 24 → 18），且不享受暴击加成。
    /// Item.crit 设为 −100 用于抵消玩家的暴击加成，是零 API 风险的写法。
    /// </summary>
    public class OffsetBow : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 42;
            Item.damage = 18;
            Item.DamageType = DamageClass.Ranged;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 26;
            Item.useAnimation = 26;
            Item.useAmmo = AmmoID.Arrow;
            Item.shoot = ModContent.ProjectileType<OffsetArrow>();
            Item.shootSpeed = 9f;
            Item.knockBack = 2f;
            Item.crit = -100;
            Item.value = Item.sellPrice(silver: 45);
            Item.rare = ItemRarityID.Blue;
            Item.autoReuse = true;
        }

        /// <summary>
        /// 走 Shoot 而不是让引擎自己发射：这样「消耗箭矢」仍然由原版
        /// PickAmmo 处理（不消耗箭的弓等于无限弹药，会破坏横向平衡），
        /// 而弹道换成错位箭。return false 阻止默认发射，避免一箭双发。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity,
                ModContent.ProjectileType<OffsetArrow>(), damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Wood, 20)
                .AddIngredient(ItemID.Lens, 2)
                .AddIngredient(ModContent.ItemType<EchoShard>(), 6)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
