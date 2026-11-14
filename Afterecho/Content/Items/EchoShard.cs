using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Afterecho.Content.Items
{
    /// <summary>
    /// 回声砂：主来源是回响庭敌怪在稳定度低于 30% 时的掉落（见 EchoWisp.OnKill），
    /// 鼓励压线探索；另外「空走 / 留白」两个可选回响事件会给少量。
    /// 骨架期那条「落星合成」配方已移除 —— 材料必须来自庭内行为，
    /// 否则会变成可无限刷的平地资源，破坏「稳定度」这条节奏轴。
    /// </summary>
    public class EchoShard : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(copper: 20);
            Item.rare = ItemRarityID.White;
            Item.material = true;
        }
    }
}
