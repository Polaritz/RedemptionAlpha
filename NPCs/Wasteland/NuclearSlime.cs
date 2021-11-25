using Microsoft.Xna.Framework;
using Redemption.Biomes;
using Redemption.Buffs.Debuffs;
using Redemption.Dusts;
using Redemption.Items.Materials.PreHM;
using Redemption.Items.Placeable.Banners;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Redemption.NPCs.Wasteland
{
    public class NuclearSlime : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 2;
            NPCID.Sets.DebuffImmunitySets.Add(Type, new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Poisoned
                }
            });

            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0);

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }
        public override void SetDefaults()
        {
            NPC.width = 46;
            NPC.height = 30;
            NPC.friendly = false;
            NPC.damage = 60;
            NPC.defense = 0;
            NPC.lifeMax = 300;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 400f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = 1;
            NPC.alpha = 80;
            AIType = NPCID.IlluminantSlime;
            AnimationType = NPCID.SlimeMasked;
            SpawnModBiomes = new int[1] { ModContent.GetInstance<WastelandPurityBiome>().Type };
        }
        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            if (Main.rand.NextBool(2) || Main.expertMode)
                target.AddBuff(ModContent.BuffType<GreenRashesDebuff>(), Main.rand.Next(200, 500));
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<XenomiteShard>(), 2, 6, 8));
            npcLoot.Add(ItemDropRule.Common(ItemID.Gel, 1, 3, 8));
            npcLoot.Add(ItemDropRule.Common(ItemID.SlimeStaff, 10000));
        }
        public override void PostAI()
        {
            Lighting.AddLight(NPC.Center, NPC.Opacity * 0.1f, NPC.Opacity, NPC.Opacity * 0.1f);
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement(
                    "")
            });
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                CombatText.NewText(NPC.getRect(), Color.Orange, "BOOM!", true, false);
                for (int i = 0; i < 15; i++)
                {
                    int dustIndex2 = Dust.NewDust(NPC.position + NPC.velocity, NPC.width, NPC.height, ModContent.DustType<SludgeDust>(), Scale: 3f);
                    Main.dust[dustIndex2].velocity *= 5f;
                }
                SoundEngine.PlaySound(SoundID.Item14, NPC.position);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        Projectile.NewProjectile(NPC.GetProjectileSpawnSource(), NPC.Center.X, NPC.Center.Y, -8 + Main.rand.Next(0, 17), -3 + Main.rand.Next(-11, 0), ProjectileID.DD2BetsyFireball, 70, 3);
                    }
                }
                for (int i = 0; i < 30; i++)
                {
                    int dustIndex3 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, Scale: 5f);
                    Main.dust[dustIndex3].velocity *= 2f;
                }
                // Fire Dust spawn
                for (int i = 0; i < 40; i++)
                {
                    int dustIndex4 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, Scale: 3f);
                    Main.dust[dustIndex4].noGravity = true;
                    Main.dust[dustIndex4].velocity *= 5f;
                    dustIndex4 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, Scale: 2f);
                    Main.dust[dustIndex4].velocity *= 3f;
                }
                // Large Smoke Gore spawn
                for (int g = 0; g < 8; g++)
                {
                    int goreIndex = Gore.NewGore(NPC.Center, default, Main.rand.Next(61, 64));
                    Main.gore[goreIndex].scale = 1.5f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1.5f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1.5f;
                }
            }
            int dustIndex = Dust.NewDust(NPC.position + NPC.velocity, NPC.width, NPC.height, ModContent.DustType<SludgeDust>(), Scale: 2f);
            Main.dust[dustIndex].velocity *= 2f;
        }
    }
}