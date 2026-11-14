using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Afterecho.Content.Projectiles;

namespace Afterecho.Content.Items.Accessories
{
    /// <summary>静默坠：谱系窗口缩短一半，更快摆脱被针对。</summary>
    public class SilentPendant : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22; Item.height = 26;
            Item.accessory = true;
            Item.value = Item.sellPrice(silver: 30);
            Item.rare = ItemRarityID.Blue;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
            => player.GetModPlayer<LedgerPlayer>().HasSilentPendant = true;

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.Chain, 2)
            .AddIngredient(ModContent.ItemType<EchoShard>(), 5)
            .AddTile(TileID.Tables).Register();
    }

    /// <summary>
    /// 双向镜：装备后按「回滚谱系快照」键（默认 P）手动回滚一次。
    /// 代价：稳定度 −15%（在 LedgerPlayer.ProcessTriggers 中扣除）。
    /// </summary>
    public class TwoWayMirror : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24; Item.height = 24;
            Item.accessory = true;
            Item.value = Item.sellPrice(silver: 40);
            Item.rare = ItemRarityID.Blue;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
            => player.GetModPlayer<LedgerPlayer>().HasTwoWayMirror = true;

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.Lens, 2)
            .AddIngredient(ModContent.ItemType<RescorePage>(), 2)
            .AddTile(TileID.Tables).Register();
    }

    /// <summary>
    /// 未言坠：被缄口收录时未言层数额外 +1。
    /// 代价：收录时长 +1 秒（在 LedgerPlayer.ApplySilence 中生效）。
    /// </summary>
    public class UnspokenPendant : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20; Item.height = 28;
            Item.accessory = true;
            Item.value = Item.sellPrice(silver: 45);
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
            => player.GetModPlayer<LedgerPlayer>().HasUnspokenPendant = true;

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.Chain, 2)
            .AddIngredient(ModContent.ItemType<RescorePage>(), 4)
            .AddTile(TileID.Tables).Register();
    }

    /// <summary>
    /// 折页靴：冲刺末段留一个短暂残影，残影可承接一次回响斩。
    /// 代价：移动速度 −10%（用 maxRunSpeed 近似冲刺速度，零 hook 风险）。
    /// </summary>
    public class FolioBoots : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 26; Item.height = 22;
            Item.accessory = true;
            Item.value = Item.sellPrice(silver: 55);
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var ledger = player.GetModPlayer<LedgerPlayer>();
            ledger.HasFolioBoots = true;
            player.maxRunSpeed *= 0.90f;

            // 高速位移时留下一个纯视觉残影，并记录承接点。
            if (player.velocity.Length() > 9f && (int)Main.GameUpdateCount % 30 == 0)
            {
                ledger.AfterimagePosition = player.Center - player.velocity * 8f;
                ledger.HasAfterimage = true;
                Projectile.NewProjectile(
                    player.GetSource_FromThis(), ledger.AfterimagePosition, Vector2.Zero,
                    ModContent.ProjectileType<Afterimage>(), 0, 0f, player.whoAmI);
            }
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.HermesBoots, 1)
            .AddIngredient(ModContent.ItemType<EchoShard>(), 10)
            .AddTile(TileID.Tables).Register();
    }

    /// <summary>
    /// 留白戒：连续 3 次不使用主导战术 → 无视一次软抗性。
    /// 代价：触发后 8 秒内无法再次触发（BlankRingCooldown）。
    /// </summary>
    public class BlankRing : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20; Item.height = 20;
            Item.accessory = true;
            Item.value = Item.sellPrice(silver: 60);
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
            => player.GetModPlayer<LedgerPlayer>().HasBlankRing = true;

        public override void AddRecipes() => CreateRecipe()
            // 银锭或钨锭：世界只会生成其中一种，配方组保证两边都能做。
            .AddRecipeGroup(AfterechoRecipeGroups.SilverBarGroup, 6)
            .AddIngredient(ModContent.ItemType<RescorePage>(), 3)
            .AddTile(TileID.Tables).Register();
    }
}
