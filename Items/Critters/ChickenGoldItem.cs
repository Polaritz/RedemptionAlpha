using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Redemption.NPCs.Critters;
using Terraria.DataStructures;

namespace Redemption.Items.Critters
{
    public class ChickenGoldItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Gold Chicken");

            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 26;
            Item.maxStack = 9999;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.LightRed;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
        }
        public override bool? UseItem(Player player)
        {
            int index = NPC.NewNPC(new EntitySource_SpawnNPC(), (int)(player.position.X + Main.rand.Next(-20, 20)), (int)(player.position.Y - 0f),
                ModContent.NPCType<ChickenGold>());

            if (Main.netMode == NetmodeID.Server && index < Main.maxNPCs)
                NetMessage.SendData(MessageID.SyncNPC, number: index);

            return true;
        }
    }
}
