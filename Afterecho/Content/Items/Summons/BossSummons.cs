using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Afterecho.Content.NPCs;

namespace Afterecho.Content.Items.Summons
{
    /// <summary>
    /// 召唤物通用门控（宪法 7「不越阶」）：
    /// 1) 必须先主动进入回响庭；2) 层数 ≥ 该幕；3) 当前无 Boss 存活。
    /// 三个条件都在代码里，而非靠配方或说明文字约束。
    /// </summary>
    internal static class SummonGate
    {
        internal static bool CanSummon(Player player, int requiredTier, out string reasonKey)
        {
            var ledger = player.GetModPlayer<LedgerPlayer>();
            if (!ledger.InReverie)
            {
                reasonKey = "Mods.Afterecho.Messages.SummonNeedsReverie";
                return false;
            }
            if (ledger.ReverieTier < requiredTier)
            {
                reasonKey = "Mods.Afterecho.Messages.SummonTierLocked";
                return false;
            }
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!Main.npc[i].active || !Main.npc[i].boss) continue;
                reasonKey = "Mods.Afterecho.Messages.SummonBossPresent";
                return false;
            }
            reasonKey = null;
            return true;
        }

        internal static bool DoSummon(Player player, int requiredTier, int npcType)
        {
            if (!CanSummon(player, requiredTier, out string reasonKey))
            {
                Main.NewText(Language.GetTextValue(reasonKey), new Color(190, 185, 170));
                return false;
            }
            int idx = NPC.NewNPC(player.GetSource_FromThis(),
                (int)player.Center.X, (int)player.Center.Y, npcType, 0, 0f, 0f, 0f, player.whoAmI);
            if (idx >= 0 && idx < Main.maxNPCs)
                Main.npc[idx].GetGlobalNPC<ReverieGlobalNPC>().ReverieOwned = true;
            player.GetModPlayer<LedgerPlayer>().BeginFightTrace();
            return true;
        }
    }

    public class TracerSummon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22; Item.height = 26;
            Item.maxStack = 20; Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45; Item.useAnimation = 45;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 20);
        }

        public override bool? UseItem(Player player)
            => SummonGate.DoSummon(player, 1, ModContent.NPCType<Tracer>());

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.Lens, 1)
            .AddIngredient(ItemID.FallenStar, 3)
            .AddIngredient(ItemID.Glass, 8)
            .AddTile(TileID.WorkBenches).Register();
    }

    public class VersoSummon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22; Item.height = 26;
            Item.maxStack = 20; Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45; Item.useAnimation = 45;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 40);
        }

        public override bool? UseItem(Player player)
            => SummonGate.DoSummon(player, 2, ModContent.NPCType<Verso>());

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ModContent.ItemType<EchoShard>(), 12)
            .AddIngredient(ModContent.ItemType<RescorePage>(), 3)
            .AddIngredient(ItemID.SoulofLight, 5)
            .AddTile(TileID.WorkBenches).Register();
    }

    public class HushSummon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22; Item.height = 26;
            Item.maxStack = 20; Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45; Item.useAnimation = 45;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override bool? UseItem(Player player)
            => SummonGate.DoSummon(player, 3, ModContent.NPCType<Hush>());

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ModContent.ItemType<EchoShard>(), 20)
            .AddIngredient(ModContent.ItemType<RescorePage>(), 8)
            .AddIngredient(ItemID.SoulofFright, 5)
            .AddTile(TileID.WorkBenches).Register();
    }

    public class AfterechoSummon : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22; Item.height = 26;
            Item.maxStack = 20; Item.consumable = true;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 45; Item.useAnimation = 45;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 2);
        }

        public override bool? UseItem(Player player)
            => SummonGate.DoSummon(player, 4, ModContent.NPCType<AfterechoBoss>());

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ModContent.ItemType<EchoShard>(), 30)
            .AddIngredient(ModContent.ItemType<RescorePage>(), 15)
            .AddIngredient(ItemID.SoulofMight, 5)
            .AddIngredient(ItemID.FragmentSolar, 8)
            .AddTile(TileID.WorkBenches).Register();
    }
}
