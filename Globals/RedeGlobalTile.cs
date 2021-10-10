using Redemption.Items.Materials.PreHM;
using Redemption.NPCs.Critters;
using Redemption.Tiles.Furniture.Misc;
using Redemption.Tiles.Natural;
using Redemption.Tiles.Plants;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Redemption.Globals
{
    public class RedeGlobalTile : GlobalTile
    {
        public override bool Drop(int i, int j, int type)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && !WorldGen.noTileActions && !WorldGen.gen)
            {
                if (type == TileID.Trees && Main.tile[i, j + 1].type == TileID.Grass)
                {
                    if (Main.rand.NextBool(6))
                        Projectile.NewProjectile(new ProjectileSource_TileBreak(i, j), i * 16, (j - 10) * 16, -4 + Main.rand.Next(0, 7), -3 + Main.rand.Next(-3, 0), ModContent.ProjectileType<TreeBugFall>(), 0, 0);
                }
                if (type == TileID.PalmTree && Main.tile[i, j + 1].type == TileID.Sand)
                {
                    if (Main.rand.NextBool(6))
                        Projectile.NewProjectile(new ProjectileSource_TileBreak(i, j), i * 16, (j - 10) * 16, -4 + Main.rand.Next(0, 7), -3 + Main.rand.Next(-3, 0), ModContent.ProjectileType<CoastScarabFall>(), 0, 0);
                }
            }
            if ((type == TileID.LeafBlock || type == TileID.LivingMahoganyLeaves) && Main.rand.NextBool(4))
                Item.NewItem(i * 16, j * 16, 16, 16, ModContent.ItemType<LivingTwig>());

            return base.Drop(i, j, type);
        }
        public override void RandomUpdate(int i, int j, int type)
        {
            if (type == TileID.Grass && !Main.dayTime && RedeBossDowned.downedThorn)
            {
                if (!Framing.GetTileSafely(i, j - 1).IsActive && Main.tile[i, j].IsActive && Main.tile[i, j - 1].LiquidAmount == 0 && Main.tile[i, j - 1].wall == 0)
                {
                    if (Main.rand.NextBool(300))
                    {
                        WorldGen.PlaceTile(i, j - 1, ModContent.TileType<NightshadeTile>(), true);
                    }
                }
            }
            if (type == TileID.Grass && Main.dayTime && RedeBossDowned.downedThorn)
            {
                if (!Framing.GetTileSafely(i, j - 1).IsActive && !Framing.GetTileSafely(i, j - 2).IsActive && !Framing.GetTileSafely(i + 1, j - 1).IsActive && !Framing.GetTileSafely(i + 1, j - 2).IsActive && Main.tile[i, j].IsActive && Main.tile[i + 1, j].IsActive && Main.tile[i, j - 1].LiquidAmount == 0 && Main.tile[i, j - 1].wall == 0)
                {
                    if (Main.rand.NextBool(6000))
                    {
                        WorldGen.PlaceTile(i, j - 1, ModContent.TileType<AnglonicMysticBlossomTile>(), true);
                    }
                }
            }
            if (Terraria.NPC.downedBoss3 && (type == TileID.SnowBlock || TileID.Sets.Conversion.Ice[type]))
            {
                bool tileUp = !Framing.GetTileSafely(i, j - 1).IsActive;
                bool tileDown = !Framing.GetTileSafely(i, j + 1).IsActive;
                bool tileLeft = !Framing.GetTileSafely(i - 1, j).IsActive;
                bool tileRight = !Framing.GetTileSafely(i + 1, j).IsActive;
                if (Main.rand.NextBool(400))
                {
                    if (tileUp)
                    {
                        WorldGen.PlaceObject(i, j - 1, ModContent.TileType<CryoCrystalTile>(), true);
                        NetMessage.SendObjectPlacment(-1, i, j - 1, ModContent.TileType<CryoCrystalTile>(), 0, 0, -1, -1);
                    }
                    else if (tileDown)
                    {
                        WorldGen.PlaceObject(i, j + 1, ModContent.TileType<CryoCrystalTile>(), true);
                        NetMessage.SendObjectPlacment(-1, i, j + 1, ModContent.TileType<CryoCrystalTile>(), 0, 0, -1, -1);
                    }
                    else if (tileLeft)
                    {
                        WorldGen.PlaceObject(i - 1, j, ModContent.TileType<CryoCrystalTile>(), true);
                        NetMessage.SendObjectPlacment(-1, i - 1, j, ModContent.TileType<CryoCrystalTile>(), 0, 0, -1, -1);
                    }
                    else if (tileRight)
                    {
                        WorldGen.PlaceObject(i + 1, j, ModContent.TileType<CryoCrystalTile>(), true);
                        NetMessage.SendObjectPlacment(-1, i + 1, j, ModContent.TileType<CryoCrystalTile>(), 0, 0, -1, -1);
                    }
                }
            }
        }

        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            if (Main.tile[i, j - 1].IsActive && (Main.tile[i, j - 1].type == ModContent.TileType<AnglonPortalTile>()))
                return false;
            return base.CanKillTile(i, j, type, ref blockDamaged);
        }

        public override bool CanExplode(int i, int j, int type)
        {
            if (Main.tile[i, j - 1].IsActive && (Main.tile[i, j - 1].type == ModContent.TileType<AnglonPortalTile>()))
                return false;
            return base.CanExplode(i, j, type);
        }

        public override bool Slope(int i, int j, int type)
        {
            if (Main.tile[i, j - 1].IsActive && (Main.tile[i, j - 1].type == ModContent.TileType<AnglonPortalTile>()))
                return false;
            return base.Slope(i, j, type);
        }
    }
}