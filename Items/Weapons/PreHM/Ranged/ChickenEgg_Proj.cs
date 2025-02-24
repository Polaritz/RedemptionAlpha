using Microsoft.Xna.Framework;
using Redemption.NPCs.Critters;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Redemption.Items.Weapons.PreHM.Ranged
{
    public class ChickenEgg_Proj : ModProjectile
    {
        public override string Texture => "Redemption/Items/Weapons/PreHM/Ranged/ChickenEgg";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Chicken Egg");
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 300;
        }
        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X / 40 * Projectile.direction;
            Projectile.velocity.Y += 0.3f;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath11 with { Volume = .5f }, Projectile.position);
            for (int i = 0; i < 6; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.MothronEgg, Projectile.velocity.X * 0.3f,
                    Projectile.velocity.Y * 0.3f, Scale: 2);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(4) && Projectile.ai[0] == 0)
            {
                for (int i = 0; i < 16; i++)
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.MothronEgg, 0, 0, Scale: 2);

                int index = NPC.NewNPC(Projectile.GetSource_FromThis(), (int)Projectile.Center.X, (int)Projectile.Center.Y + 8, ModContent.NPCType<Chicken>());

                if (Main.netMode == NetmodeID.Server && index < Main.maxNPCs)
                    NetMessage.SendData(MessageID.SyncNPC, number: index);
            }
            return true;
        }
    }
}