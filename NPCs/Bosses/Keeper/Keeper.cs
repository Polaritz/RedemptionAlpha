using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.IO;
using Redemption.Items.Weapons.PreHM.Melee;
using Redemption.Items.Weapons.PreHM.Ranged;
using Redemption.Items.Armor.Vanity;
using Redemption.Items.Placeable.Trophies;
using Redemption.Items.Usable;
using Redemption.Globals;
using Terraria.GameContent;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.Audio;
using Redemption.Base;
using Terraria.Graphics.Shaders;

namespace Redemption.NPCs.Bosses.Keeper
{
    [AutoloadBossHead]
    public class Keeper : ModNPC
    {
        public static int secondStageHeadSlot = -1;
        public override void Load()
        {
            string texture = BossHeadTexture + "_Unveiled";
            secondStageHeadSlot = Mod.AddBossHeadTexture(texture, -1);
        }

        public override void BossHeadSlot(ref int index)
        {
            int slot = secondStageHeadSlot;
            if (Unveiled && slot != -1)
            {
                index = slot;
            }
        }

        public enum ActionState
        {
            Begin,
            Idle,
            Attacks,
            Unveiled,
            Death
        }

        public ActionState AIState
        {
            get => (ActionState)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float AITimer => ref NPC.ai[1];

        public ref float TimerRand => ref NPC.ai[2];

        public float[] oldrot = new float[5];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Keeper");
            Main.npcFrameCount[NPC.type] = 9;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;
            NPCID.Sets.TrailingMode[NPC.type] = 1;

            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);

            NPCDebuffImmunityData debuffData = new()
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);

            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0)
            {
                Position = new Vector2(0, 44),
                PortraitPositionYOverride = 0
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 3500;
            NPC.damage = 30;
            NPC.defense = 10;
            NPC.knockBackResist = 0f;
            NPC.width = 52;
            NPC.height = 128;
            NPC.npcSlots = 10f;
            NPC.SpawnWithHigherTime(30);
            NPC.alpha = 255;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.HitSound = SoundID.NPCHit13;
            NPC.DeathSound = SoundID.NPCDeath19;
            if (!Main.dedServ)
                Music = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/BossKeeper");
            //BossBag = ModContent.ItemType<KeeperBag>();
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    int dustIndex = Dust.NewDust(NPC.position + NPC.velocity, NPC.width, NPC.height, DustID.Blood, Scale: 3);
                    Main.dust[dustIndex].velocity *= 4f;
                }
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 0.6f * bossLifeScale);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,

                new FlavorTextBestiaryInfoElement("A powerful fallen who had learnt forbidden necromancy, its prolonged usage having mutated her body.")
            });
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(BossBag));

            LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());

            //int itemType = ModContent.ItemType<ExampleItem>();

            //notExpertRule.OnSuccess((itemType));

            //Finally add the leading rule
            //npcLoot.Add(notExpertRule);
        }

        public override void OnKill()
        {
            if (!RedeBossDowned.downedKeeper)
            {
                RedeWorld.alignment++;
                for (int p = 0; p < Main.maxPlayers; p++)
                {
                    Player player = Main.player[p];
                    if (!player.active)
                        continue;

                    CombatText.NewText(player.getRect(), Color.Gold, "+1", true, false);

                    if (!player.HasItem(ModContent.ItemType<AlignmentTeller>()))
                        continue;

                    if (!Main.dedServ)
                        RedeSystem.Instance.ChaliceUIElement.DisplayDialogue("An undead... disgusting. Good thing you killed it.", 120, 30, 0, Color.DarkGoldenrod);

                }
            }
            NPC.SetEventFlagCleared(ref RedeBossDowned.downedKeeper, -1);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            if (Main.netMode == NetmodeID.Server || Main.dedServ)
            {
                writer.Write(ID);
                writer.Write(Unveiled);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ID = reader.ReadInt32();
                Unveiled = reader.ReadBoolean();
            }
        }

        void AttackChoice()
        {
            int attempts = 0;
            while (attempts == 0)
            {
                if (CopyList == null || CopyList.Count == 0)
                    CopyList = new List<int>(AttackList);
                ID = CopyList[Main.rand.Next(0, CopyList.Count)];
                CopyList.Remove(ID);
                NPC.netUpdate = true;

                attempts++;
            }
        }

        public List<int> AttackList = new() { 0, 1 };
        public List<int> CopyList = null;

        private bool Unveiled;
        private Vector2 origin;
        private float move;
        private float speed = 6;

        public int ID { get => (int)NPC.ai[3]; set => NPC.ai[3] = value; }

        public override void AI()
        {
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            Player player = Main.player[NPC.target];

            Rectangle SlashHitbox = new((int)(NPC.spriteDirection == -1 ? NPC.Center.X - 64 : NPC.Center.X + 26), (int)(NPC.Center.Y - 38), 38, 86);

            DespawnHandler();

            if (AIState != ActionState.Death && AIState != ActionState.Unveiled && AIState != ActionState.Attacks)
                NPC.LookAtEntity(player);

            switch (AIState)
            {
                case ActionState.Begin:
                    if (AITimer++ == 0)
                    {
                        if (!Main.dedServ)
                            RedeSystem.Instance.TitleCardUIElement.DisplayTitle("The Keeper", 60, 90, 0.8f, 0, Color.MediumPurple, "Octavia von Gailon");

                        NPC.position = new Vector2(Main.rand.NextBool(2) ? player.Center.X - 160 : player.Center.X + 160, player.Center.Y - 90);
                        NPC.netUpdate = true;
                    }
                    NPC.alpha -= 2;
                    if (NPC.alpha <= 0)
                    {
                        AITimer = 0;
                        AIState = ActionState.Idle;
                        NPC.netUpdate = true;
                    }
                    break;
                case ActionState.Idle:
                    if (AITimer++ == 0)
                    {
                        move = NPC.Center.X;
                        speed = 6;
                    }
                    NPC.Move(new Vector2(move, player.Center.Y - 50), speed, 20, false);
                    MoveClamp();
                    if (NPC.DistanceSQ(player.Center) > 400 * 400)
                        speed *= 1.03f;
                    if (!Unveiled && NPC.life < NPC.lifeMax / 2)
                    {
                        NPC.velocity *= 0;
                        AIState = ActionState.Unveiled;
                        NPC.netUpdate = true;
                        break;
                    }
                    if (AITimer > 60)
                    {
                        AttackChoice();
                        AITimer = 0;
                        AIState = ActionState.Attacks;
                        NPC.netUpdate = true;
                    }
                    break;
                case ActionState.Attacks:
                    if (!Unveiled && NPC.life < NPC.lifeMax / 2)
                    {
                        AITimer = 0;
                        TimerRand = 0;
                        NPC.velocity *= 0;
                        AIState = ActionState.Unveiled;
                        NPC.netUpdate = true;
                        break;
                    }
                    switch (ID)
                    {
                        #region Reaper Slash
                        case 0:
                            int alphaTimer = Main.expertMode ? 20 : 10;
                            AITimer++;
                            if (AITimer < 100)
                            {
                                if (AITimer < 40)
                                {
                                    NPC.LookAtEntity(player);
                                    NPC.velocity *= 0.9f;
                                }
                                if (AITimer == 40)
                                {
                                    SoundEngine.PlaySound(SoundID.Zombie, (int)NPC.position.X, (int)NPC.position.Y, 83, 1, 0.3f);
                                    NPC.velocity.Y = 0;
                                    NPC.velocity.X = -6f * NPC.spriteDirection;
                                }
                                if (AITimer >= 40)
                                {
                                    NPC.alpha += alphaTimer;
                                    NPC.velocity *= 0.96f;
                                }
                                if (NPC.alpha >= 255)
                                {
                                    NPC.velocity *= 0f;
                                    NPC.position = new Vector2(Main.rand.NextBool(2) ? player.Center.X - 160 : player.Center.X + 160, player.Center.Y - 70);
                                    AITimer = 100;
                                }
                            }
                            else
                            {
                                if (AITimer == 100)
                                {
                                    NPC.velocity.X = 6f * NPC.spriteDirection;
                                }
                                if (AITimer >= 100 && AITimer < 200)
                                {
                                    NPC.LookAtEntity(player);
                                    NPC.alpha -= alphaTimer;
                                    NPC.velocity *= 0.96f;
                                }
                                if (NPC.alpha <= 0 && AITimer < 200)
                                {
                                    AITimer = 200;
                                    NPC.frameCounter = 0;
                                    NPC.frame.Y = 0;
                                }
                                if (AITimer >= 200 && NPC.frame.Y >= 4 * 142 && NPC.frame.Y <= 6 * 142 && player.Hitbox.Intersects(SlashHitbox))
                                {
                                    int hitDirection = NPC.Center.X > player.Center.X ? -1 : 1;
                                    BaseAI.DamagePlayer(player, NPC.damage, 3, hitDirection, NPC);
                                    player.AddBuff(BuffID.Bleeding, 600);
                                }
                                if (AITimer >= 235)
                                {
                                    NPC.velocity *= 0f;
                                    if (TimerRand >= (Main.expertMode ? 2 : 1) + (Unveiled ? 1 : 0))
                                    {
                                        TimerRand = 0;
                                        AITimer = 0;
                                        AIState = ActionState.Idle;
                                        NPC.netUpdate = true;
                                    }
                                    else
                                    {
                                        TimerRand++;
                                        AITimer = 30;
                                        NPC.netUpdate = true;
                                    }
                                }
                            }
                            break;
                        #endregion

                        #region Blood Wave
                        case 1:
                            NPC.LookAtEntity(player);

                            NPC.velocity *= 0.96f;

                            if (++AITimer == 30)
                                NPC.velocity = player.Center.DirectionTo(NPC.Center) * 6;

                            if (AITimer == 60)
                            {
                                BaseAI.DamageNPC(NPC, 50, 0, player, false, true);
                                for (int i = 0; i < 6; i++)
                                {
                                    NPC.Shoot(NPC.Center, ModContent.ProjectileType<KeeperBloodWave>(), NPC.damage,
                                        RedeHelper.PolarVector(Main.rand.NextFloat(8, 16), (player.Center - NPC.Center).ToRotation() + Main.rand.NextFloat(-0.3f, 0.3f)),
                                        false, SoundID.NPCDeath19, "", NPC.whoAmI);
                                }
                                for (int i = 0; i < 30; i++)
                                {
                                    int dustIndex = Dust.NewDust(NPC.position + NPC.velocity, NPC.width, NPC.height, DustID.Blood, Scale: 3);
                                    Main.dust[dustIndex].velocity *= 5f;
                                }
                            }
                            if (AITimer >= 90)
                            {
                                TimerRand = 0;
                                AITimer = 0;
                                AIState = ActionState.Idle;
                                NPC.netUpdate = true;
                            }
                            break;
                        #endregion

                        #region Shadow Bolts
                        case 2:

                            break;
                        #endregion

                        #region Soul Charge
                        case 3:
                            break;
                        #endregion

                        #region Dread Coil
                        case 4:
                            break;
                        #endregion

                        #region Rupture
                        case 5:
                            break;
                            #endregion
                    }
                    break;
                case ActionState.Unveiled:
                    NPC.alpha = 0;
                    NPC.dontTakeDamage = true;
                    player.GetModPlayer<ScreenPlayer>().ScreenFocusPosition = NPC.Center;
                    player.GetModPlayer<ScreenPlayer>().lockScreen = true;
                    player.GetModPlayer<ScreenPlayer>().ScreenShakeIntensity = 3;

                    Unveiled = true;

                    if (AITimer++ == 0)
                        NPC.Shoot(new Vector2(NPC.Center.X - 1, NPC.Center.Y - 37), ModContent.ProjectileType<VeilFX>(), 0, Vector2.Zero, false, SoundID.Item1.WithVolume(0));

                    if (AITimer >= 220)
                    {
                        NPC.dontTakeDamage = false;
                        AITimer = 0;
                        AIState = ActionState.Idle;
                        NPC.netUpdate = true;
                    }
                    break;
            }
        }

        public void MoveClamp()
        {
            Player player = Main.player[NPC.target];
            if (NPC.Center.X < player.Center.X)
            {
                if (move < player.Center.X - 240)
                {
                    move = player.Center.X - 240;
                }
                else if (move > player.Center.X - 120)
                {
                    move = player.Center.X - 120;
                }
            }
            else
            {
                if (move > player.Center.X + 240)
                {
                    move = player.Center.X + 240;
                }
                else if (move < player.Center.X + 120)
                {
                    move = player.Center.X + 120;
                }
            }
        }

        public override bool CheckDead()
        {
            if (AIState is ActionState.Death)
                return true;
            else
            {

                SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.position);
                NPC.life = 1;
                AITimer = 0;
                AIState = ActionState.Death;
                return false;
            }
        }

        private int VeilFrameY;
        private int VeilCounter;
        public override void FindFrame(int frameHeight)
        {
            for (int k = NPC.oldPos.Length - 1; k > 0; k--)
            {
                oldrot[k] = oldrot[k - 1];
            }
            oldrot[0] = NPC.rotation;

            if (++VeilCounter >= 5)
            {
                VeilCounter = 0;
                VeilFrameY++;
                if (VeilFrameY > 5)
                    VeilFrameY = 0;
            }

            NPC.frame.Width = TextureAssets.Npc[NPC.type].Value.Width / 4;
            if (AIState is ActionState.Attacks && ID == 0 && AITimer >= 200)
            {
                NPC.frame.X = NPC.frame.Width;
                if (++NPC.frameCounter >= 5)
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y += frameHeight;
                    NPC.velocity *= 0.8f;
                    if (NPC.frame.Y == 4 * frameHeight)
                    {
                        SoundEngine.PlaySound(SoundID.Item71, NPC.position);
                        NPC.velocity.X = 25 * NPC.spriteDirection;
                    }
                    if (NPC.frame.Y > 7 * frameHeight)
                        NPC.frame.Y = 0 * frameHeight;
                }
                return;
            }
            else
                NPC.frame.X = 0;

            if (AIState is ActionState.Unveiled)
            {
                if (NPC.frame.Y < 6 * frameHeight)
                    NPC.frame.Y = 6 * frameHeight;

                if (++NPC.frameCounter >= 10)
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y += frameHeight;
                    if (NPC.frame.Y > 8 * frameHeight)
                        NPC.frame.Y = 7 * frameHeight;
                }
                return;
            }
            if (++NPC.frameCounter >= 5)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y > 5 * frameHeight)
                    NPC.frame.Y = 0 * frameHeight;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D glow = ModContent.Request<Texture2D>("Redemption/NPCs/Bosses/Keeper/" + GetType().Name + "_Glow").Value;
            Texture2D veilTex = ModContent.Request<Texture2D>("Redemption/NPCs/Bosses/Keeper/VeilFX").Value;
            var effects = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            int shader = ContentSamples.CommonlyUsedContentSamples.ColorOnlyShaderIndex;
            Color angryColor = BaseUtility.MultiLerpColor(Main.LocalPlayer.miscCounter % 100 / 100f, Color.DarkSlateBlue, Color.DarkRed * 0.7f, Color.DarkSlateBlue);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            GameShaders.Armor.ApplySecondary(shader, Main.player[Main.myPlayer], null);

            for (int i = 0; i < NPCID.Sets.TrailCacheLength[NPC.type]; i++)
            {
                Vector2 oldPos = NPC.oldPos[i];
                Main.spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, oldPos + NPC.Size / 2f - screenPos + new Vector2(0, NPC.gfxOffY), NPC.frame, NPC.GetAlpha(Unveiled ? angryColor : Color.DarkSlateBlue) * 0.5f, oldrot[i], NPC.frame.Size() / 2, NPC.scale + 0.1f, effects, 0);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);

            spriteBatch.Draw(glow, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);

            int height = veilTex.Height / 6;
            int y = height * VeilFrameY;
            Rectangle rect = new(0, y, veilTex.Width, height);
            Vector2 origin = new(veilTex.Width / 2f, height / 2f);
            Vector2 VeilPos = new(NPC.Center.X - 1, NPC.Center.Y - 37);
            if (!Unveiled && NPC.life > NPC.lifeMax / 2)
                Main.spriteBatch.Draw(veilTex, VeilPos - screenPos, new Rectangle?(rect), NPC.GetAlpha(drawColor), NPC.rotation, origin, NPC.scale, effects, 0);

            return false;
        }

        private void DespawnHandler()
        {
            Player player = Main.player[NPC.target];
            if (!player.active || player.dead)
            {
                NPC.TargetClosest(false);
                player = Main.player[NPC.target];
                if (!player.active || player.dead)
                {
                    NPC.alpha += 2;
                    if (NPC.alpha >= 255)
                        NPC.active = false;
                    if (NPC.timeLeft > 10)
                        NPC.timeLeft = 10;
                    return;
                }
            }
        }
    }
}