using Redemption.Tiles.Trophies;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Redemption.Items.Placeable.Trophies
{
    public class OmegaGigaporaTrophy : ModItem
	{
		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("Omega Gigapora Trophy");
            Item.ResearchUnlockCount = 1;
        }
		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<OmegaGigaporaTrophyTile>(), 0);
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(0, 1, 33, 0);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
