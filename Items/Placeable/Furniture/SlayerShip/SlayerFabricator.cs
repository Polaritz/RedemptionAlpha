using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Redemption.Tiles.Furniture.SlayerShip;

namespace Redemption.Items.Placeable.Furniture.SlayerShip
{
    public class SlayerFabricator : ModItem
	{
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cyber Fabricator");
            Tooltip.SetDefault("Used to craft from Cyberscrap"
                + "\nFound on Slayer's Crashed Ship" +
                "\n[c/ff0000:Unbreakable (500% Pickaxe Power)]");
        }
        public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 34;
			Item.maxStack = 99;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.value = Item.value = Item.sellPrice(0, 8, 0, 0);
			Item.rare = ItemRarityID.Cyan;
			Item.createTile = ModContent.TileType<SlayerFabricatorTile>();
		}
	}
}