using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using System.IO;
using Redemption.Globals;
using Terraria.GameContent;
using System.Collections.Generic;
using Terraria.Audio;
using Redemption.Base;
using Terraria.Graphics.Shaders;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Redemption.Items.Placeable.Trophies;
using Redemption.Items.Weapons.PreHM.Melee;
using Redemption.Items.Weapons.PreHM.Magic;
using Redemption.Items.Usable;
using Redemption.Items.Weapons.PreHM.Ranged;
using Redemption.Items.Armor.Vanity;
using Redemption.Items.Accessories.PreHM;
using Redemption.BaseExtension;
using ReLogic.Content;
using Redemption.UI.ChatUI;
using Redemption.Items.Weapons.PreHM.Summon;

namespace Redemption.NPCs.Bosses.Erhan
{
    [AutoloadBossHead]
    public class ErhanSpirit : ModNPC
    {
        private static Asset<Texture2D> ArmsTex;
        private static Asset<Texture2D> HeadTex;
        private static Asset<Texture2D> FallTex;
        private static Asset<Texture2D> GroundedTex;
        public override void Load()
        {
            ArmsTex = ModContent.Request<Texture2D>(Texture + "_Arms");
            HeadTex = ModContent.Request<Texture2D>(Texture + "_Head");
            FallTex = ModContent.Request<Texture2D>(Texture + "_Fall");
            GroundedTex = ModContent.Request<Texture2D>(Texture + "_Grounded");
        }
        public override void Unload()
        {
            ArmsTex = null;
            HeadTex = null;
            FallTex = null;
            GroundedTex = null;
        }
        public override string Texture => "Redemption/NPCs/Bosses/Erhan/Erhan";
        public enum ActionState
        {
            Begin,
            Idle,
            Attacks,
            Fallen,
            Bible,
            BibleAttacks
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
            // DisplayName.SetDefault("Erhan's Spirit");
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;
            NPCID.Sets.TrailingMode[NPC.type] = 1;

            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);

            NPCID.Sets.DebuffImmunitySets.Add(Type, new NPCDebuffImmunityData
            {
                ImmuneToAllBuffsThatAreNotWhips = true
            });

            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0)
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
            ElementID.NPCHoly[Type] = true;
            ElementID.NPCArcane[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 2600;
            NPC.damage = 23;
            NPC.defense = 6;
            NPC.knockBackResist = 0f;
            NPC.width = 34;
            NPC.height = 60;
            NPC.npcSlots = 10f;
            NPC.value = Item.buyPrice(0, 1, 25, 0);
            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.HitSound = SoundID.NPCHit36;
            NPC.DeathSound = SoundID.NPCDeath39;
            NPC.dontTakeDamage = true;
            voice = CustomSounds.Voice4 with { Pitch = -0.2f };
            bubble = ModContent.Request<Texture2D>("Redemption/UI/TextBubble_Epidotra").Value;
            if (!Main.dedServ)
                Music = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/BossErhan");

            NPC.GetGlobalNPC<ElementalNPC>().OverrideMultiplier[ElementID.Psychic] *= .9f;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool CanHitNPC(NPC target) => false;

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                    Dust.NewDust(NPC.position + NPC.velocity, NPC.width, NPC.height, DustID.GoldFlame, NPC.velocity.X * 0.5f, NPC.velocity.Y * 0.5f, Scale: 4);
            }
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 0.75f * balance * bossAdjustment);
            NPC.damage = (int)(NPC.damage * 0.8f);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ErhanBag>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ErhanTrophy>(), 10));

            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<ErhanRelic>()));

            npcLoot.Add(ItemDropRule.MasterModeDropOnAllPlayers(ModContent.ItemType<DevilsAdvocate>(), 4));

            LeadingConditionRule notExpertRule = new(new Conditions.NotExpert());

            notExpertRule.OnSuccess(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<ErhanHelmet>(), 7));

            notExpertRule.OnSuccess(ItemDropRule.OneFromOptions(1,
                ModContent.ItemType<Bindeklinge>(), ModContent.ItemType<HolyBible>(), ModContent.ItemType<HallowedHandGrenade>()));
            notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ErhanMagnifyingGlass>(), 2));

            npcLoot.Add(notExpertRule);
        }

        public override void OnKill()
        {
            NPC.SetEventFlagCleared(ref RedeBossDowned.downedErhan, -1);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            if (Main.netMode == NetmodeID.Server || Main.dedServ)
            {
                writer.Write(ID);
                writer.Write(AttackNumber);
                writer.Write(TimerRand2);
                writer.Write(Funny);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ID = reader.ReadInt32();
                AttackNumber = reader.ReadInt32();
                TimerRand2 = reader.ReadInt32();
                Funny = reader.ReadBoolean();
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

                if (ID == 4 && AttackNumber <= 5)
                    continue;

                AttackNumber++;
                attempts++;
            }
        }

        public List<int> AttackList = new() { 0, 1, 2, 3, 4 };
        public List<int> CopyList = null;

        private float move;
        private float speed = 6;
        private int AttackNumber;
        private bool floatTimer;
        private float TimerRand2;
        private bool Funny;
        private Vector2 playerOrigin;
        public readonly Vector2 modifier = new(0, -200);

        public int ID { get => (int)NPC.ai[3]; set => NPC.ai[3] = value; }

        private SoundStyle voice;
        private Texture2D bubble;
        public override void AI()
        {
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            Player player = Main.player[NPC.target];

            DespawnHandler();

            if (AIState is not ActionState.Fallen && AIState is not ActionState.Bible)
            {
                NPC.LookAtEntity(player);
                if (!floatTimer)
                {
                    NPC.velocity.Y += 0.03f;
                    if (NPC.velocity.Y > .5f)
                    {
                        floatTimer = true;
                        NPC.netUpdate = true;
                    }
                }
                else if (floatTimer)
                {
                    NPC.velocity.Y -= 0.03f;
                    if (NPC.velocity.Y < -.5f)
                    {
                        floatTimer = false;
                        NPC.netUpdate = true;
                    }
                }
            }

            switch (AIState)
            {
                case ActionState.Begin:
                    switch (TimerRand)
                    {
                        case 0:
                            if (!Main.dedServ)
                                Music = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/silence");

                            SoundEngine.PlaySound(SoundID.Item68, NPC.position);
                            Main.LocalPlayer.RedemptionScreen().ScreenShakeOrigin = NPC.Center;
                            Main.LocalPlayer.RedemptionScreen().ScreenShakeIntensity += 14;
                            HolyFlare = true;
                            TeleGlow = true;
                            TimerRand = 1;
                            NPC.netUpdate = true;
                            break;
                        case 1:
                            if (RedeBossDowned.erhanDeath < 4)
                            {
                                if (AITimer++ == 0 && Main.rand.NextBool(10))
                                {
                                    Funny = true;
                                    NPC.netUpdate = true;
                                }

                                if (Funny)
                                {
                                    if (AITimer == 1 && !Main.dedServ)
                                    {
                                        Dialogue d1 = new(NPC, Language.GetTextValue("Mods.Redemption.Cutscene.ErhanS.Funny"), Color.LightGoldenrodYellow, new Color(100, 86, 0), voice, .03f, 2f, .5f, true, null, bubble, null, modifier); // 228
                                        ChatUI.Visible = true;
                                        ChatUI.Add(d1);
                                    }
                                    if (AITimer >= 218)
                                    {
                                        if (!Main.dedServ)
                                            Music = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/BossErhan");

                                        if (RedeBossDowned.erhanDeath < 4 && Main.netMode != NetmodeID.MultiplayerClient)
                                        {
                                            RedeBossDowned.erhanDeath = 4;
                                            if (Main.netMode == NetmodeID.Server)
                                                NetMessage.SendData(MessageID.WorldData);
                                        }

                                        TimerRand = 0;
                                        AITimer = 0;
                                        NPC.dontTakeDamage = false;
                                        AIState = ActionState.Idle;
                                        NPC.netUpdate = true;
                                        if (Main.netMode == NetmodeID.Server && NPC.whoAmI < Main.maxNPCs)
                                            NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                                    }
                                }
                                else
                                {
                                    if (AITimer == 1 && !Main.dedServ)
                                    {
                                        DialogueChain chain = new();
                                        chain.Add(new(NPC, Language.GetTextValue("Mods.Redemption.Cutscene.ErhanS.1"), Color.LightGoldenrodYellow, new Color(100, 86, 0), voice, .03f, 2f, 0, false, null, bubble, null, modifier)) // 190
                                             .Add(new(NPC, Language.GetTextValue("Mods.Redemption.Cutscene.ErhanS.2"), Color.LightGoldenrodYellow, new Color(100, 86, 0), voice, .03f, 2f, .5f, true, null, bubble, null, modifier, 1)); // 324
                                        chain.OnEndTrigger += Chain_OnEndTrigger;
                                        ChatUI.Visible = true;
                                        ChatUI.Add(chain);
                                    }
                                    if (AITimer >= 1000)
                                    {
                                        if (!Main.dedServ)
                                            Music = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/BossErhan");

                                        if (RedeBossDowned.erhanDeath < 4 && Main.netMode != NetmodeID.MultiplayerClient)
                                        {
                                            RedeBossDowned.erhanDeath = 4;
                                            if (Main.netMode == NetmodeID.Server)
                                                NetMessage.SendData(MessageID.WorldData);
                                        }

                                        TimerRand = 0;
                                        AITimer = 0;
                                        NPC.dontTakeDamage = false;
                                        AIState = ActionState.Idle;
                                        NPC.netUpdate = true;
                                        if (Main.netMode == NetmodeID.Server && NPC.whoAmI < Main.maxNPCs)
                                            NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                                    }
                                }
                            }
                            else
                            {
                                if (AITimer++ == 0 && !Main.dedServ)
                                {
                                    Dialogue d1 = new(NPC, Language.GetTextValue("Mods.Redemption.Cutscene.ErhanS.3"), Color.LightGoldenrodYellow, new Color(100, 86, 0), voice, .03f, 2f, .5f, true, null, bubble, null, modifier); // 178
                                    ChatUI.Visible = true;
                                    ChatUI.Add(d1);
                                }
                                if (AITimer >= 150)
                                {
                                    if (!Main.dedServ)
                                        Music = MusicLoader.GetMusicSlot(Mod, "Sounds/Music/BossErhan");

                                    TimerRand = 0;
                                    AITimer = 0;
                                    NPC.dontTakeDamage = false;
                                    AIState = ActionState.Idle;
                                    NPC.netUpdate = true;
                                    if (Main.netMode == NetmodeID.Server && NPC.whoAmI < Main.maxNPCs)
                                        NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                                }
                            }
                            break;
                    }
                    break;
                case ActionState.Idle:
                    if (AITimer++ == 0)
                    {
                        move = NPC.Center.X;
                        speed = 9;
                    }
                    NPC.Move(new Vector2(move, player.Center.Y - 250), speed, 50, false);
                    MoveClamp();
                    if (NPC.DistanceSQ(player.Center) > 800 * 800)
                        speed *= 1.03f;
                    else if (NPC.velocity.Length() > 9 && NPC.DistanceSQ(player.Center) <= 800 * 800)
                        speed *= 0.96f;

                    if (AITimer > 80)
                    {
                        if (AttackNumber != 0 && AttackNumber % 5 == 0)
                        {
                            if (Main.expertMode)
                            {
                                TimerRand = 0;
                                AIState = ActionState.Bible;
                            }
                            else
                                AIState = ActionState.Fallen;
                        }
                        else
                        {
                            AttackChoice();
                            AIState = ActionState.Attacks;
                        }
                        AITimer = 0;
                        NPC.netUpdate = true;
                    }
                    break;
                case ActionState.Attacks:
                    switch (ID)
                    {
                        #region Lightmass
                        case 0:
                            AITimer++;
                            if (AITimer < 60)
                                NPC.Move(new Vector2(player.Center.X + (40 * NPC.spriteDirection), player.Center.Y - 250), 10, 40, false);
                            else
                                NPC.velocity *= 0.96f;

                            if (AITimer == 80)
                                ArmType = 1;
                            if (AttackNumber > 5)
                            {
                                if (AITimer >= 100 && AITimer % (AttackNumber > 10 ? 8 : 12) == 0 && AITimer <= 130)
                                {
                                    TeleGlow = true;
                                    TeleGlowTimer = 0;
                                    for (int i = 0; i < Main.rand.Next(4, 7); i++)
                                        NPC.Shoot(NPC.Center, ModContent.ProjectileType<Erhan_Lightmass>(), NPC.damage, new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-9, -5)), true, SoundID.Item101);
                                    NPC.netUpdate = true;
                                }
                            }
                            else
                            {
                                if (AITimer == 100 || (Main.rand.NextBool(2) ? AITimer == 120 : AITimer == -1))
                                {
                                    TeleGlow = true;
                                    TeleGlowTimer = 0;
                                    for (int i = 0; i < Main.rand.Next(4, 7); i++)
                                        NPC.Shoot(NPC.Center, ModContent.ProjectileType<Erhan_Lightmass>(), NPC.damage, new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-9, -5)), true, SoundID.Item101);
                                    NPC.netUpdate = true;
                                }
                                NPC.netUpdate = true;
                            }
                            if (AITimer == 140)
                                ArmType = 0;

                            if (AITimer >= 200)
                            {
                                AITimer = 0;
                                AIState = ActionState.Idle;
                                NPC.netUpdate = true;
                            }
                            break;
                        #endregion

                        #region Scorching Rays
                        case 1:
                            if (AITimer++ == 0)
                            {
                                move = NPC.Center.X;
                                speed = 7;
                            }
                            NPC.Move(new Vector2(move, player.Center.Y - 250), speed, 50, false);
                            MoveClamp();
                            if (NPC.DistanceSQ(player.Center) > 800 * 800)
                                speed *= 1.03f;
                            else if (NPC.velocity.Length() > 9 && NPC.DistanceSQ(player.Center) <= 800 * 800)
                                speed *= 0.96f;

                            if (AITimer == 20)
                            {
                                HeadFrameY = 1;
                                ArmType = 2;
                            }
                            if (AITimer == 40)
                            {
                                NPC.Shoot(new Vector2(player.Center.X, player.Center.Y - 600),
                                    ModContent.ProjectileType<ScorchingRay>(), (int)(NPC.damage * 1.5f),
                                    new Vector2(Main.rand.NextFloat(-1, 1), 10), true, SoundID.Item162);
                            }
                            if (AITimer >= 70 && AITimer % 30 == 0 && AITimer <= 220)
                            {
                                NPC.Shoot(new Vector2(player.Center.X + Main.rand.Next(-600, 600), player.Center.Y - 600),
                                    ModContent.ProjectileType<ScorchingRay>(), (int)(NPC.damage * 1.5f),
                                    new Vector2(Main.rand.NextFloat(-1, 1), 10), true, SoundID.Item162);
                                NPC.netUpdate = true;
                            }
                            if (AttackNumber > 5 && AITimer >= 80 && AITimer % 80 == 0 && AITimer <= 360)
                            {
                                TeleGlow = true;
                                TeleGlowTimer = 0;
                                for (int i = 0; i < Main.rand.Next(4, 7); i++)
                                    NPC.Shoot(NPC.Center, ModContent.ProjectileType<Erhan_Lightmass>(), NPC.damage,
                                        new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-9, -5)), true, SoundID.Item101);
                                NPC.netUpdate = true;
                            }
                            if (AITimer == 340)
                            {
                                HeadFrameY = 0;
                                ArmType = 0;
                            }

                            if (AITimer >= 350)
                            {
                                AITimer = 0;
                                AIState = ActionState.Idle;
                                NPC.netUpdate = true;
                            }
                            break;
                        #endregion

                        #region Holy Spears
                        case 2:
                            AITimer++;
                            if (AITimer < 80)
                                NPC.Move(new Vector2(player.Center.X + (40 * NPC.spriteDirection), player.Center.Y - 250), 10, 40, false);
                            else
                                NPC.velocity *= 0.5f;

                            if (AITimer == 80)
                                ArmType = 1;
                            if (AITimer >= 90 && AITimer % 5 == 0 && AITimer <= 130)
                            {
                                Main.LocalPlayer.RedemptionScreen().ScreenShakeOrigin = NPC.Center;
                                Main.LocalPlayer.RedemptionScreen().ScreenShakeIntensity += 2;
                                TimerRand += (float)Math.PI / 15;
                                if (TimerRand > (float)Math.PI)
                                {
                                    TimerRand -= (float)Math.PI * 2;
                                }
                                NPC.Shoot(NPC.Center, ModContent.ProjectileType<HolySpear_Proj>(), NPC.damage,
                                    new Vector2(0.1f, 0).RotatedBy(TimerRand + Math.PI / 2), true, SoundID.Item125);
                                NPC.Shoot(NPC.Center, ModContent.ProjectileType<HolySpear_Proj>(), NPC.damage,
                                    new Vector2(0.1f, 0).RotatedBy(-TimerRand + Math.PI / 2), true, SoundID.Item125);

                                NPC.Shoot(NPC.Center, ModContent.ProjectileType<HolySpear_Tele>(), 0,
                                    new Vector2(0.1f, 0).RotatedBy(TimerRand + Math.PI / 2), false, SoundID.Item1);
                                NPC.Shoot(NPC.Center, ModContent.ProjectileType<HolySpear_Tele>(), 0,
                                    new Vector2(0.1f, 0).RotatedBy(-TimerRand + Math.PI / 2), false, SoundID.Item1);
                            }
                            if (AttackNumber > 5)
                            {
                                if (AITimer > 130 && AITimer % 5 == 0 && AITimer <= 165)
                                {
                                    Main.LocalPlayer.RedemptionScreen().ScreenShakeOrigin = NPC.Center;
                                    Main.LocalPlayer.RedemptionScreen().ScreenShakeIntensity += 2;
                                    TimerRand -= (float)Math.PI / 13;
                                    if (TimerRand > (float)Math.PI)
                                    {
                                        TimerRand -= (float)Math.PI * 2;
                                    }
                                    NPC.Shoot(NPC.Center, ModContent.ProjectileType<HolySpear_Proj>(), NPC.damage,
                                        new Vector2(0.1f, 0).RotatedBy(TimerRand + Math.PI / 2), true, SoundID.Item125, 1);
                                    NPC.Shoot(NPC.Center, ModContent.ProjectileType<HolySpear_Proj>(), NPC.damage,
                                        new Vector2(0.1f, 0).RotatedBy(-TimerRand + Math.PI / 2), true, SoundID.Item125, 1);

                                    NPC.Shoot(NPC.Center, ModContent.ProjectileType<HolySpear_Tele>(), 0,
                                        new Vector2(0.1f, 0).RotatedBy(TimerRand + Math.PI / 2), false, SoundID.Item1);
                                    NPC.Shoot(NPC.Center, ModContent.ProjectileType<HolySpear_Tele>(), 0,
                                        new Vector2(0.1f, 0).RotatedBy(-TimerRand + Math.PI / 2), false, SoundID.Item1);
                                }
                            }
                            if (AttackNumber > 5 ? AITimer == 180 : AITimer == 150)
                                ArmType = 0;

                            if (AttackNumber > 5 ? AITimer >= 200 : AITimer >= 160)
                            {
                                TimerRand = 0;
                                AITimer = 0;
                                AIState = ActionState.Idle;
                                NPC.netUpdate = true;
                            }
                            break;
                        #endregion

                        #region Holy Phalanx
                        case 3:
                            AITimer++;
                            if (AITimer < 80)
                                NPC.Move(new Vector2(player.Center.X + (40 * NPC.spriteDirection), player.Center.Y - 270), 10, 40, false);
                            else
                                NPC.velocity *= 0.5f;

                            if (AITimer == 80)
                                ArmType = 1;
                            if (AITimer == 80)
                            {
                                for (int i = 0; i < 2; i++)
                                    NPC.Shoot(new Vector2(player.Center.X + 600 * (i == 0 ? -1 : 1), player.Center.Y - 600), ModContent.ProjectileType<ScorchingRay>(), (int)(NPC.damage * 1.5f), new Vector2(Main.rand.NextFloat(-1, 1), 10), true, SoundID.Item162);
                            }
                            if (AITimer >= 90 && AITimer % 7 == 0 && AITimer <= 130 && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (!Main.dedServ)
                                    SoundEngine.PlaySound(CustomSounds.Slice3, NPC.position);
                                SoundEngine.PlaySound(SoundID.Item125, NPC.Center);
                                int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<HolyPhalanx_Proj>(), NPCHelper.HostileProjDamage(NPC.damage), 3, Main.myPlayer, NPC.whoAmI, TimerRand * 60);
                                Main.projectile[p].localAI[0] += TimerRand * 7;
                                TimerRand++;
                            }
                            if (AttackNumber >= 5 ? AITimer == 130 : AITimer == 200)
                                ArmType = 0;

                            if (AttackNumber >= 5 ? AITimer == 150 : AITimer >= 220)
                            {
                                TimerRand = 0;
                                AITimer = 0;
                                AIState = ActionState.Idle;
                                NPC.netUpdate = true;
                            }
                            break;
                        #endregion

                        #region Ray of Guidance
                        case 4:
                            if (AITimer++ == 0)
                            {
                                move = NPC.Center.X;
                                speed = 7;
                            }
                            NPC.Move(new Vector2(move, player.Center.Y - 250), speed, 50, false);
                            MoveClamp();
                            if (NPC.DistanceSQ(player.Center) > 800 * 800)
                                speed *= 1.03f;
                            else if (NPC.velocity.Length() > 9 && NPC.DistanceSQ(player.Center) <= 800 * 800)
                                speed *= 0.96f;

                            if (AITimer == 20)
                            {
                                HeadFrameY = 1;
                                ArmType = 2;
                            }
                            if (AITimer == 40)
                            {
                                NPC.Shoot(new Vector2(player.Center.X + (Main.rand.NextBool(2) ? 300 : -300), player.Center.Y - 800),
                                    ModContent.ProjectileType<RayOfGuidance>(), (int)(NPC.damage * 2f),
                                    Vector2.Zero, true, SoundID.Item162);
                                NPC.netUpdate = true;
                            }
                            if (AttackNumber > 7)
                            {
                                if (AITimer >= 60 && AITimer % 60 == 0 && AITimer <= 360)
                                {
                                    NPC.Shoot(new Vector2(player.Center.X + (Main.rand.Next(600, 900) * (Main.rand.NextBool() ? -1 : 1)), player.Center.Y - 600), ModContent.ProjectileType<ScorchingRay>(), (int)(NPC.damage * 1.5f), new Vector2(Main.rand.NextFloat(-1, 1), 10), true, SoundID.Item162);
                                    NPC.netUpdate = true;
                                }
                            }
                            if (AITimer >= 80 && AITimer % 80 == 0 && AITimer <= 360)
                            {
                                TeleGlow = true;
                                TeleGlowTimer = 0;
                                for (int i = 0; i < Main.rand.Next(4, 7); i++)
                                    NPC.Shoot(NPC.Center, ModContent.ProjectileType<Erhan_Lightmass>(), NPC.damage,
                                        new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(-9, -5)), true, SoundID.Item101);
                                NPC.netUpdate = true;
                            }
                            if (AITimer == 420)
                            {
                                HeadFrameY = 0;
                                ArmType = 0;
                            }

                            if (AITimer >= 440)
                            {
                                AITimer = 0;
                                AIState = ActionState.Idle;
                                NPC.netUpdate = true;
                            }
                            break;
                            #endregion
                    }
                    break;
                case ActionState.Bible:
                    switch (TimerRand)
                    {
                        case 0:
                            if (AITimer++ == 0)
                            {
                                move = NPC.Center.X;
                                speed = 9;
                            }
                            if (AITimer < 50)
                            {
                                NPC.Move(new Vector2(move, player.Center.Y - 250), speed, 50, false);
                                MoveClamp();
                                if (NPC.DistanceSQ(player.Center) > 800 * 800)
                                    speed *= 1.03f;
                                else if (NPC.velocity.Length() > 9 && NPC.DistanceSQ(player.Center) <= 800 * 800)
                                    speed *= 0.96f;
                            }
                            else
                                NPC.velocity *= 0.8f;

                            if (AITimer == 60)
                            {
                                ArmType = 3;
                                HeadFrameY = 2;
                                NPC.Shoot(NPC.Center + new Vector2(80 * NPC.spriteDirection, 20), ModContent.ProjectileType<Erhan_Bible>(), NPC.damage, new Vector2(0, -1), true, CustomSounds.Choir, NPC.whoAmI);
                            }
                            if (AITimer == 180)
                            {
                                ArmType = 0;
                                HeadFrameY = 0;
                            }
                            break;
                        case 1:
                            NPC.LookAtEntity(player);
                            if (AITimer++ == 0)
                            {
                                move = NPC.Center.X;
                                speed = 9;
                            }
                            NPC.Move(new Vector2(move, player.Center.Y - 300), speed, 50, false);
                            MoveClamp();
                            if (NPC.DistanceSQ(player.Center) > 800 * 800)
                                speed *= 1.03f;
                            else if (NPC.velocity.Length() > 9 && NPC.DistanceSQ(player.Center) <= 800 * 800)
                                speed *= 0.96f;

                            if (AITimer >= 460)
                            {
                                TimerRand = 0;
                                AITimer = 0;
                                AIState = ActionState.Fallen;
                                NPC.netUpdate = true;
                            }
                            break;
                        case 2:
                            NPC.LookAtEntity(player);
                            if (AITimer++ == 0)
                            {
                                playerOrigin = player.Center;
                                for (int i = 0; i < 2; i++)
                                    NPC.Shoot(new Vector2(player.Center.X + 800 * (i == 0 ? -1 : 1), player.Center.Y - 600), ModContent.ProjectileType<ScorchingRay>(), (int)(NPC.damage * 1.5f), new Vector2(Main.rand.NextFloat(-1, 1), 10), true, SoundID.Item162);
                            }
                            if (AITimer == 100 || AITimer == 200 || AITimer == 300)
                            {
                                for (int i = 0; i < 2; i++)
                                    NPC.Shoot(new Vector2(playerOrigin.X + 800 * (i == 0 ? -1 : 1), playerOrigin.Y - 600), ModContent.ProjectileType<ScorchingRay>(), (int)(NPC.damage * 1.5f), new Vector2(Main.rand.NextFloat(-1, 1), 10), true, SoundID.Item162);
                            }
                            if (AITimer < 120)
                                NPC.Move(new Vector2(playerOrigin.X + 600, player.Center.Y - 270), 18, 20, false);
                            else if (AITimer >= 80 && AITimer < 220)
                                NPC.velocity *= 0.5f;

                            if (AITimer == 120)
                                ArmType = 1;
                            if (AITimer >= 130 && AITimer % 7 == 0 && AITimer <= 170 && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                if (!Main.dedServ)
                                    SoundEngine.PlaySound(CustomSounds.Slice3, NPC.position);
                                SoundEngine.PlaySound(SoundID.Item125, NPC.Center);
                                int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<HolyPhalanx_Proj>(), NPCHelper.HostileProjDamage(NPC.damage), 3, Main.myPlayer, NPC.whoAmI, TimerRand2 * 60);
                                Main.projectile[p].localAI[0] += TimerRand2 * 7;
                                TimerRand2++;
                            }
                            if (AITimer >= 220)
                            {
                                NPC.Move(new Vector2(playerOrigin.X - 600, player.Center.Y - 270), 6, 40, false);
                            }
                            if (AITimer == 240)
                                ArmType = 0;
                            if (AITimer >= 460)
                            {
                                TimerRand = 0;
                                TimerRand2 = 0;
                                AITimer = 0;
                                AIState = ActionState.Fallen;
                                NPC.netUpdate = true;
                            }
                            break;
                    }
                    break;

                case ActionState.Fallen:
                    switch (TimerRand)
                    {
                        case 0:
                            NPC.velocity *= 0.96f;
                            if (NPC.velocity.Length() <= 1)
                            {
                                NPC.velocity.X = 0;
                                TimerRand = 1;
                                NPC.netUpdate = true;
                            }
                            break;
                        case 1:
                            AITimer++;
                            NPC.noGravity = false;
                            NPC.noTileCollide = false;
                            if ((AITimer > 5 && NPC.velocity.Y == 0) || AITimer > 300)
                            {
                                AITimer = 0;
                                TimerRand = 2;
                                NPC.netUpdate = true;

                            }
                            break;
                        case 2:
                            AITimer++;
                            if (AITimer < 60)
                            {
                                for (int k = 0; k < 3; k++)
                                {
                                    Vector2 vector;
                                    double angle = Main.rand.NextDouble() * 2d * Math.PI;
                                    vector.X = (float)(Math.Sin(angle) * 100);
                                    vector.Y = (float)(Math.Cos(angle) * 100);
                                    Dust dust2 = Main.dust[Dust.NewDust(NPC.Center + vector, 2, 2, DustID.GoldFlame, Scale: 2)];
                                    dust2.noGravity = true;
                                    dust2.velocity = dust2.position.DirectionTo(NPC.Center) * 15f;
                                }
                            }

                            if (AITimer % 20 == 0 && AITimer < 60)
                            {
                                SoundEngine.PlaySound(SoundID.Item28 with { Pitch = TimerRand2 }, NPC.position);
                                TimerRand2 += 0.1f;
                                NPC.netUpdate = true;
                            }

                            if (AITimer == 60)
                            {
                                DustHelper.DrawCircle(NPC.Center, DustID.GoldFlame, 5, 5, 5, 1, 2, nogravity: true);
                                for (int i = 0; i < 4; i++)
                                    NPC.Shoot(NPC.Center, ModContent.ProjectileType<Erhan_HolyShield>(), 0, Vector2.Zero, true, SoundID.Item29, NPC.whoAmI, i);
                            }

                            if (AITimer >= 360)
                            {
                                if (RedeBossDowned.erhanDeath < 2 && Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    RedeBossDowned.erhanDeath = 2;
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.WorldData);
                                }

                                AttackNumber++;
                                NPC.noGravity = true;
                                NPC.noTileCollide = true;
                                AITimer = 0;
                                TimerRand = 0;
                                TimerRand2 = 0;
                                AIState = ActionState.Idle;
                                NPC.netUpdate = true;
                            }
                            break;
                    }
                    break;
            }
        }
        private void Chain_OnEndTrigger(Dialogue dialogue, int ID)
        {
            AITimer = 1000;
        }
        private int ArmFrameY;
        private int ArmType;
        private int HeadFrameY;
        private bool HolyFlare;
        private int HolyFlareTimer;
        private bool TeleGlow;
        private int TeleGlowTimer;

        public override void FindFrame(int frameHeight)
        {
            for (int k = NPC.oldPos.Length - 1; k > 0; k--)
            {
                oldrot[k] = oldrot[k - 1];
            }
            oldrot[0] = NPC.rotation;

            if (HolyFlare)
            {
                HolyFlareTimer++;
                if (HolyFlareTimer > 60)
                {
                    HolyFlare = false;
                    HolyFlareTimer = 0;
                }
            }
            if (TeleGlow)
            {
                TeleGlowTimer += 3;
                if (TeleGlowTimer > 60)
                {
                    TeleGlow = false;
                    TeleGlowTimer = 0;
                }
            }

            if (AIState is ActionState.Fallen && TimerRand != 0)
            {
                if (++NPC.frameCounter >= 10)
                {
                    NPC.frameCounter = 0;
                    ArmFrameY++;
                    if (ArmFrameY > 3)
                        ArmFrameY = 0;
                }
                return;
            }

            ArmFrameY = (NPC.frame.Y / frameHeight) + (6 * ArmType);

            if (++NPC.frameCounter >= 5)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y > 5 * frameHeight)
                    NPC.frame.Y = 0 * frameHeight;
            }
        }

        public void MoveClamp()
        {
            Player player = Main.player[NPC.target];
            int xFar = 400;
            if (NPC.Center.X < player.Center.X)
            {
                if (move < player.Center.X - xFar)
                {
                    move = player.Center.X - xFar;
                }
                else if (move > player.Center.X - 200)
                {
                    move = player.Center.X - 200;
                }
            }
            else
            {
                if (move > player.Center.X + xFar)
                {
                    move = player.Center.X + xFar;
                }
                else if (move < player.Center.X + 200)
                {
                    move = player.Center.X + 200;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var effects = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            int shader = ContentSamples.CommonlyUsedContentSamples.ColorOnlyShaderIndex;
            Color shaderColor = BaseUtility.MultiLerpColor(Main.LocalPlayer.miscCounter % 100 / 100f, Color.Yellow, Color.Goldenrod * 0.7f, Color.Yellow);

            if (AIState is ActionState.Fallen && TimerRand != 0)
            {
                if (TimerRand <= 1)
                {
                    Rectangle rectFall = new(0, 0, FallTex.Value.Width, FallTex.Value.Height);
                    Vector2 originFall = new(FallTex.Value.Width / 2f, FallTex.Value.Height / 2f);
                    spriteBatch.Draw(FallTex.Value, NPC.Center - screenPos, new Rectangle?(rectFall), NPC.GetAlpha(drawColor), NPC.rotation, originFall, NPC.scale, effects, 0);
                }
                else
                {
                    int heightGrounded = GroundedTex.Value.Height / 4;
                    int yGrounded = heightGrounded * ArmFrameY;
                    Rectangle rectGrounded = new(0, yGrounded, GroundedTex.Value.Width, heightGrounded);
                    Vector2 originGrounded = new(GroundedTex.Value.Width / 2f, heightGrounded / 2f);
                    spriteBatch.Draw(GroundedTex.Value, NPC.Center - screenPos + new Vector2(0, 10), new Rectangle?(rectGrounded), NPC.GetAlpha(drawColor), NPC.rotation, originGrounded, NPC.scale, effects, 0);
                }
                return false;
            }

            int heightHead = HeadTex.Value.Height / 3;
            int yHead = heightHead * HeadFrameY;
            Rectangle rectHead = new(0, yHead, HeadTex.Value.Width, heightHead);
            Vector2 originHead = new(HeadTex.Value.Width / 2f, heightHead / 2f);
            if (!NPC.IsABestiaryIconDummy)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                GameShaders.Armor.ApplySecondary(shader, Main.player[Main.myPlayer], null);

                for (int i = 0; i < NPCID.Sets.TrailCacheLength[NPC.type]; i++)
                {
                    Vector2 oldPos = NPC.oldPos[i];
                    spriteBatch.Draw(HeadTex.Value, oldPos + NPC.Size / 2f - screenPos - new Vector2(-2 * NPC.spriteDirection, 33), new Rectangle?(rectHead), NPC.GetAlpha(shaderColor) * 0.5f, oldrot[i], originHead, NPC.scale + 0.1f, effects, 0);
                }
                for (int i = 0; i < NPCID.Sets.TrailCacheLength[NPC.type]; i++)
                {
                    Vector2 oldPos = NPC.oldPos[i];
                    spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, oldPos + NPC.Size / 2f - screenPos + new Vector2(0, NPC.gfxOffY), NPC.frame, NPC.GetAlpha(shaderColor) * 0.5f, oldrot[i], NPC.frame.Size() / 2, NPC.scale + 0.1f, effects, 0);
                }

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }

            spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0);

            spriteBatch.Draw(HeadTex.Value, NPC.Center - screenPos - new Vector2(-2 * NPC.spriteDirection, 33), new Rectangle?(rectHead), NPC.GetAlpha(drawColor), NPC.rotation, originHead, NPC.scale, effects, 0);

            int heightArms = ArmsTex.Value.Height / 24;
            int yArms = heightArms * ArmFrameY;
            Rectangle rectArms = new(0, yArms, ArmsTex.Value.Width, heightArms);
            Vector2 originArms = new(ArmsTex.Value.Width / 2f, heightArms / 2f);
            spriteBatch.Draw(ArmsTex.Value, NPC.Center - screenPos + new Vector2(-2 * NPC.spriteDirection, -10), new Rectangle?(rectArms), NPC.GetAlpha(drawColor), NPC.rotation, originArms, NPC.scale, effects, 0);

            return false;
        }

        public override Color? GetAlpha(Color drawColor)
        {
            return new Color(1, 1, 0.1f, 0);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D flare = Redemption.HolyGlow2.Value;
            Rectangle rect = new(0, 0, flare.Width, flare.Height);
            Vector2 origin = new(flare.Width / 2, flare.Height / 2);
            Vector2 position = NPC.Center - screenPos;
            Color colour = Color.Lerp(Color.White, Color.White, 1f / HolyFlareTimer * 10f) * (1f / HolyFlareTimer * 10f);
            if (HolyFlare)
            {
                spriteBatch.Draw(flare, position, new Rectangle?(rect), colour, NPC.rotation, origin, 3f, SpriteEffects.None, 0);
                spriteBatch.Draw(flare, position, new Rectangle?(rect), colour * 0.4f, NPC.rotation, origin, 2.5f, SpriteEffects.None, 0);
            }

            Texture2D teleportGlow = Redemption.HolyGlow3.Value;
            Rectangle rect2 = new(0, 0, teleportGlow.Width, teleportGlow.Height);
            Vector2 origin2 = new(teleportGlow.Width / 2, teleportGlow.Height / 2);
            Vector2 position2 = NPC.Center - screenPos;
            Color colour2 = Color.Lerp(Color.White, Color.White, 1f / TeleGlowTimer * 10f) * (1f / TeleGlowTimer * 10f);
            if (TeleGlow)
            {
                spriteBatch.Draw(teleportGlow, position2, new Rectangle?(rect2), colour2, NPC.rotation, origin2, 2f, SpriteEffects.None, 0);
                spriteBatch.Draw(teleportGlow, position2, new Rectangle?(rect2), colour2 * 0.4f, NPC.rotation, origin2, 2f, SpriteEffects.None, 0);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (AIState is ActionState.Fallen && TimerRand == 2 && item.DamageType == DamageClass.Melee)
                modifiers.FinalDamage *= 2;
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (AIState is ActionState.Fallen && TimerRand == 2 && projectile.Redemption().TechnicallyMelee)
                modifiers.FinalDamage *= 2;
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
