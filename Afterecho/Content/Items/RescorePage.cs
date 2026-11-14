using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Afterecho.Content.Items
{
    /// <summary>
    /// 残谱：Boss 掉落，用于召唤物与装备重铸「代价」条目。
    /// 不新增矿物、不改写世界生成，只走 Boss 掉落链，因此天然带进度门控。
    /// </summary>
    public class RescorePage : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 22;
            Item.maxStack = 999;
            Item.value = Item.sellPrice(silver: 12);
            Item.rare = ItemRarityID.Green;
            Item.material = true;
        }
    }
}
