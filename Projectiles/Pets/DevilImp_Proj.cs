using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Redemption.Buffs.Pets;
using Redemption.Globals;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Redemption.Projectiles.Pets
{
	public class DevilImp_Proj : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Devil Imp");
			Main.projFrames[Projectile.type] = 3;
			Main.projPet[Projectile.type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 26;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Player player = Main.player[Projectile.owner];
			CheckActive(player);

			Projectile.rotation = Projectile.velocity.X * 0.05f;
			Projectile.frameCounter++;
			if (Projectile.frameCounter >= 5)
			{
				Projectile.frameCounter = 0;
				Projectile.frame++;

				if (Projectile.frame >= Main.projFrames[Projectile.type])
					Projectile.frame = 0;
			}
			if (Projectile.velocity.X < -1 || Projectile.velocity.X > 1)
				Projectile.LookByVelocity();
			else
				Projectile.spriteDirection = player.direction;

			Projectile.Move(new Vector2(player.Center.X - (30 * -player.direction), player.Center.Y - 26), 10, 40);

			if (Main.myPlayer == player.whoAmI && Projectile.DistanceSQ(player.Center) > 2000 * 2000)
			{
				Projectile.position = player.Center;
				Projectile.velocity *= 0.1f;
				Projectile.netUpdate = true;
			}
		}

		private void CheckActive(Player player)
		{
			if (!player.dead && player.HasBuff(ModContent.BuffType<ErhanPetBuff>()))
				Projectile.timeLeft = 2;
		}
	}
}