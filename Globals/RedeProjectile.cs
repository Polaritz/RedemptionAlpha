using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Redemption.BaseExtension;
using Terraria.DataStructures;
using System.Collections.Generic;
using Terraria.ID;
using System.Linq;
using System;
using Terraria.Enums;
using Redemption.Buffs;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Redemption.Globals
{
    public class RedeProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool TechnicallyMelee;
        public bool IsHammer;
        public bool IsAxe;
        public bool RitDagger;
        public bool EnergyBased;
        public bool ParryBlacklist;
        public bool friendlyHostile;
        public int DissolveTimer;
        public float ReflectDamageIncrease;
        public Rectangle swordHitbox;
        public override void SetDefaults(Projectile projectile)
        {
            if (ProjectileLists.IsTechnicallyMelee.Contains(projectile.type))
                TechnicallyMelee = true;
        }
        public override bool PreAI(Projectile projectile)
        {
            if ((projectile.DamageType == DamageClass.Melee || projectile.DamageType == DamageClass.SummonMeleeSpeed) && Main.player[projectile.owner].HasBuff<ExplosiveFlaskBuff>())
            {
                if (Main.rand.NextBool(3))
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Smoke);
                if (Main.rand.NextBool(10))
                    Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.InfernoFork);
                projectile.GetGlobalProjectile<ElementalProjectile>().OverrideElement[ElementID.Explosive] = 1;
            }
            return base.PreAI(projectile);
        }
        public override void ModifyHitNPC(Projectile projectile, Terraria.NPC target, ref Terraria.NPC.HitModifiers modifiers)
        {
            if (ReflectDamageIncrease is 0)
                return;
            modifiers.FinalDamage *= ReflectDamageIncrease;
        }
        private readonly int[] bannedArenaProjs = new int[]
        {
            ProjectileID.SandBallGun,
            ProjectileID.EbonsandBallGun,
            ProjectileID.PearlSandBallGun,
            ProjectileID.CrimsandBallGun,
            ProjectileID.SandBallFalling,
            ProjectileID.EbonsandBallFalling,
            ProjectileID.PearlSandBallFalling,
            ProjectileID.CrimsandBallFalling,
            ProjectileID.Bomb,
            ProjectileID.StickyBomb,
            ProjectileID.BouncyBomb,
            ProjectileID.Dynamite,
            ProjectileID.StickyDynamite,
            ProjectileID.BouncyDynamite,
            ProjectileID.SnowBallHostile,
            ProjectileID.IceBlock,
            ProjectileID.AntiGravityHook,
            ProjectileID.StaticHook,
            ProjectileID.PortalGunBolt,
            ProjectileID.PortalGunGate
        };
        public override void AI(Projectile projectile)
        {
            if (ArenaWorld.arenaActive && bannedArenaProjs.Any(x => x == projectile.type) && projectile.Hitbox.Intersects(new Rectangle((int)ArenaWorld.arenaTopLeft.X, (int)ArenaWorld.arenaTopLeft.Y, (int)ArenaWorld.arenaSize.X, (int)ArenaWorld.arenaSize.Y)))
                projectile.Kill();

            if (ArenaWorld.arenaActive && projectile.aiStyle == 7 && !projectile.Hitbox.Intersects(new Rectangle((int)ArenaWorld.arenaTopLeft.X, (int)ArenaWorld.arenaTopLeft.Y, (int)ArenaWorld.arenaSize.X, (int)ArenaWorld.arenaSize.Y)))
                projectile.Kill();
        }
        public static void Decapitation(Terraria.NPC target, ref int damage, ref bool crit, int chance = 200)
        {
            bool humanoid = NPCLists.SkeletonHumanoid.Contains(target.type) || NPCLists.Humanoid.Contains(target.type);
            if (target.life < target.lifeMax && target.life < damage * 100 && humanoid)
            {
                if (Main.rand.NextBool(chance))
                {
                    CombatText.NewText(target.getRect(), Color.Orange, "Decapitated!");
                    target.Redemption().decapitated = true;
                    crit = true;
                    target.StrikeInstantKill();
                }
            }
        }
        public static bool SwordClashFriendly(Projectile projectile, Entity player, ref bool parried)
        {
            Rectangle projectileHitbox = projectile.Hitbox;
            if (projectile.Redemption().swordHitbox != default)
                projectileHitbox = projectile.Redemption().swordHitbox;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                Terraria.NPC npc = Main.npc[i];
                if (!npc.active)
                    continue;
                Rectangle target = npc.Redemption().parryHitbox;
                if (target == Rectangle.Empty)
                    continue;

                if (!parried && target.Intersects(projectileHitbox))
                {
                    if (player is Terraria.Player p)
                    {
                        p.immune = true;
                        p.immuneTime = 60;
                        p.AddBuff(BuffID.ParryDamageBuff, 120);
                    }
                    player.velocity.X += 4 * player.RightOfDir(npc);
                    RedeDraw.SpawnExplosion(RedeHelper.CenterPoint(projectile.Center, target.Center.ToVector2()), Color.White, shakeAmount: 0, scale: 1f, noDust: true, tex: Redemption.HolyGlow2.Value);

                    if (!Main.dedServ)
                        SoundEngine.PlaySound(CustomSounds.SwordClash, projectile.position);
                    DustHelper.DrawCircle(RedeHelper.CenterPoint(projectile.Center, target.Center.ToVector2()), DustID.SilverCoin, 1, 4, 4, nogravity: true);
                    parried = true;
                    return true;
                }
            }
            return false;
        }
        public static bool SwordClashHostile(Projectile projectile, Terraria.NPC npc, ref bool parried)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Terraria.Player player = Main.player[i];
                if (!player.active || player.dead)
                    continue;
                Rectangle target = player.Redemption().parryHitbox;
                if (target == Rectangle.Empty)
                    continue;

                if (!parried && target.Intersects(projectile.Redemption().swordHitbox))
                {
                    npc.velocity.X += 4 * npc.RightOfDir(player);
                    parried = true;
                    return true;
                }
            }
            return false;
        }
        public static bool SwordClashHostile(Rectangle rect, Terraria.NPC npc, ref bool parried)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Terraria.Player player = Main.player[i];
                if (!player.active || player.dead)
                    continue;
                Rectangle target = player.Redemption().parryHitbox;
                if (target == Rectangle.Empty)
                    continue;

                if (!parried && target.Intersects(rect))
                {
                    npc.velocity.X += 4 * npc.Center.RightOfDir(target.Center.ToVector2());
                    parried = true;
                    return true;
                }
            }
            return false;
        }
        public static Dictionary<int, (Entity entity, IEntitySource source)> projOwners = new();
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            Entity attacker = null;
            if (source is EntitySource_ItemUse item && projectile.friendly && !projectile.hostile)
                attacker = item.Entity;
            else if (source is EntitySource_Buff buff && projectile.friendly && !projectile.hostile)
                attacker = buff.Entity;
            else if (source is EntitySource_ItemUse_WithAmmo itemAmmo && projectile.friendly && !projectile.hostile)
                attacker = itemAmmo.Entity;
            else if (source is EntitySource_Mount mount && projectile.friendly && !projectile.hostile)
                attacker = mount.Entity;
            else if (source is EntitySource_Parent parent)
            {
                if (parent.Entity is Projectile proj)
                    attacker = Main.player[proj.owner];
                else
                    attacker = parent.Entity;
            }
            if (attacker != null)
            {
                if (projOwners.ContainsKey(projectile.whoAmI))
                    projOwners.Remove(projectile.whoAmI);
                projOwners.Add(projectile.whoAmI, (attacker, source));
            }
        }
        #region Wasteland Conversion
        public override void PostAI(Projectile projectile)
        {
            if ((projectile.type != 10 && projectile.type != 145 && projectile.type != 147 && projectile.type != 149 && projectile.type != 146) || projectile.owner != Main.myPlayer)
                return;
            int x = (int)(projectile.Center.X / 16f);
            int y = (int)(projectile.Center.Y / 16f);

            Tile tile = Main.tile[x, y];
            int type = tile.TileType;
            int wallType = tile.WallType;

            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (!WorldGen.InWorld(x, y, 1))
                        return;

                    if (projectile.type == 145 || projectile.type == 10)
                    {
                        if (Main.tile[x, y] != null)
                        {
                            if (type == ModContent.TileType<Tiles.Tiles.IrradiatedDirtTile>())
                                tile.TileType = 0;
                            else if (type == ModContent.TileType<Tiles.Tiles.IrradiatedSnowTile>())
                                tile.TileType = 147;
                            else if (type == ModContent.TileType<Tiles.Tiles.IrradiatedLivingWoodTile>())
                                tile.TileType = 191;
                        }
                        if (Main.tile[x, y] != null)
                        {
                            if (wallType == ModContent.WallType<Walls.IrradiatedDirtWallTile>())
                                Main.tile[x, y].WallType = 2;
                        }
                    }
                    if (projectile.type == 147)
                    {
                        if (Main.tile[x, y] != null)
                        {
                            if (type == ModContent.TileType<Tiles.Tiles.IrradiatedDirtTile>())
                                tile.TileType = 0;
                            else if (type == ModContent.TileType<Tiles.Tiles.IrradiatedSnowTile>())
                                tile.TileType = 147;
                            else if (type == ModContent.TileType<Tiles.Tiles.IrradiatedLivingWoodTile>())
                                tile.TileType = 191;
                        }
                        if (Main.tile[x, y] != null)
                        {
                            if (wallType == ModContent.WallType<Walls.IrradiatedDirtWallTile>())
                                Main.tile[x, y].WallType = 2;
                        }
                    }
                    if (projectile.type == 149)
                    {
                        if (Main.tile[x, y] != null)
                        {
                            if (type == ModContent.TileType<Tiles.Tiles.IrradiatedDirtTile>())
                                tile.TileType = 0;
                            else if (type == ModContent.TileType<Tiles.Tiles.IrradiatedSnowTile>())
                                tile.TileType = 147;
                            else if (type == ModContent.TileType<Tiles.Tiles.IrradiatedLivingWoodTile>())
                                tile.TileType = 191;
                        }
                        if (Main.tile[x, y] != null)
                        {
                            if (wallType == ModContent.WallType<Walls.IrradiatedDirtWallTile>())
                                Main.tile[x, y].WallType = 2;
                        }
                    }
                    if (projectile.type == 146)
                    {
                        if (Main.tile[x, y] != null)
                        {
                            if (type == ModContent.TileType<Tiles.Tiles.IrradiatedDirtTile>())
                                tile.TileType = 0;
                            else if (type == ModContent.TileType<Tiles.Tiles.IrradiatedSnowTile>())
                                tile.TileType = 147;
                            else if (type == ModContent.TileType<Tiles.Tiles.IrradiatedLivingWoodTile>())
                                tile.TileType = 191;
                        }
                        if (Main.tile[x, y] != null)
                        {
                            if (wallType == ModContent.WallType<Walls.IrradiatedDirtWallTile>())
                                Main.tile[x, y].WallType = 2;
                        }
                    }
                    NetMessage.SendTileSquare(-1, i, j, 1, 1);
                }
            }
            #endregion
        }
    }
    public abstract class TrueMeleeProjectile : ModProjectile
    {
        public float SetSwingSpeed(float speed)
        {
            Terraria.Player player = Main.player[Projectile.owner];
            return speed / player.GetAttackSpeed(DamageClass.Melee);
        }

        public virtual void SetSafeDefaults() { }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.Redemption().TechnicallyMelee = true;
            SetSafeDefaults();
        }
    }
    public abstract class LaserProjectile : ModProjectile
    {
        public float AITimer
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }
        public float Frame
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }
        public float LaserLength = 0;
        public float LaserScale = 0;
        public int LaserSegmentLength = 10;
        public int LaserWidth = 20;
        public int LaserEndSegmentLength = 22;

        public const float FirstSegmentDrawDist = 7;

        public int MaxLaserLength = 2000;
        public int maxLaserFrames = 1;
        public int LaserFrameDelay = 5;
        public bool StopsOnTiles = true;

        public virtual void SetSafeStaticDefaults() { }
        public override void SetStaticDefaults()
        {
            SetSafeStaticDefaults();
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }
        public virtual void SetSafeDefaults() { }
        public override void SetDefaults()
        {
            Projectile.width = LaserWidth;
            Projectile.height = LaserWidth;
            Projectile.Redemption().ParryBlacklist = true;
            SetSafeDefaults();
        }
        public virtual void EndpointTileCollision()
        {
            for (LaserLength = FirstSegmentDrawDist; LaserLength < MaxLaserLength; LaserLength += LaserSegmentLength)
            {
                Vector2 start = Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * LaserLength;
                if (!Collision.CanHitLine(Projectile.Center, 1, 1, start, 1, 1))
                {
                    LaserLength -= LaserSegmentLength;
                    break;
                }
            }
        }
        public float Hitscan(Projectile projectile) //modified, code from exampleLastPrism
        {
            Vector2 samplingPoint = projectile.Center;
            float[] laserScanResults = new float[3];
            Collision.LaserScan(samplingPoint, Projectile.velocity, Projectile.scale, MaxLaserLength, laserScanResults);
            float averageLengthSample = 0f;

            for (int i = 0; i < laserScanResults.Length; ++i)
            {
                averageLengthSample += laserScanResults[i];
            }
            averageLengthSample /= 3;

            return averageLengthSample;
        } 
        public float LengthSetting(Projectile projectile)
        {
            float hitscanBeamLength = Hitscan(projectile);
            LaserLength = MathHelper.Lerp(LaserLength, hitscanBeamLength, 0.75f);
            return LaserLength;
        }
        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 unit = new Vector2(1.5f, 0).RotatedBy(Projectile.rotation);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * LaserLength, (Projectile.width + 16) * Projectile.scale, DelegateMethods.CutTiles);
        }
        public virtual void CastLights(Vector3 color)
        {
            // Cast a light along the line of the Laser
            DelegateMethods.v3_1 = color;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + new Vector2(1f, 0).RotatedBy(Projectile.rotation) * LaserLength, 26, DelegateMethods.CastLight);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) //modified, code from exampleLastPrism
        {
            // If the target is touching the beam's hitbox (which is a small rectangle vaguely overlapping the host Prism), that's good enough.
            if (projHitbox.Intersects(targetHitbox))
            {
                return true;
            }

            // Otherwise, perform an AABB line collision check to check the whole beam.
            float _ = float.NaN;
            Vector2 beamEndPos = Projectile.Center + Projectile.velocity * LaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, beamEndPos, LaserWidth * Projectile.scale, ref _);
        }
    }
