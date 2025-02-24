using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Redemption.Globals;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Redemption.BaseExtension;
using Redemption.Items.Weapons.PreHM.Summon;
using Redemption.Items.Materials.PreHM;
using Terraria.Audio;
using Terraria.Localization;

namespace Redemption.NPCs.Friendly
{
    public class SpiritNiricLady : ModNPC
    {
        public ref float AITimer => ref NPC.ai[1];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Old Spirit Lady");
            Main.npcFrameCount[NPC.type] = 4;
            NPCID.Sets.ActsLikeTownNPC[Type] = true;
            NPCID.Sets.NoTownNPCHappiness[Type] = true;

            NPCID.Sets.DebuffImmunitySets.Add(Type, new NPCDebuffImmunityData
            {
                ImmuneToAllBuffsThatAreNotWhips = true
            });
            NPCID.Sets.NPCBestiaryDrawModifiers value = new(0)
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }
        public override void SetDefaults()
        {
            NPC.friendly = true;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.width = 44;
            NPC.height = 48;
            NPC.lifeMax = 250;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0f;
            NPC.npcSlots = 0;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool CanHitNPC(NPC target) => false;

        public bool floatTimer;
        public override void AI()
        {
            Player player = Main.player[RedeHelper.GetNearestAlivePlayer(NPC)];
            if (NPC.target < 0 || NPC.target == 255 || player.dead || !player.active)
                NPC.TargetClosest();

            NPC.LookAtEntity(player);

            if (AITimer < 60)
                NPC.velocity *= 0.94f;

            if (AITimer++ == 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    int dustIndex = Dust.NewDust(NPC.Center, 2, 2, DustID.DungeonSpirit, 0f, 0f, 100, default, 2);
                    Main.dust[dustIndex].velocity *= 2f;
                    Main.dust[dustIndex].noGravity = true;
                }
                DustHelper.DrawDustImage(NPC.Center, DustID.DungeonSpirit, 0.5f, "Redemption/Effects/DustImages/DeadRingerDust", 2, true, 0);
            }
            NPC.alpha += Main.rand.Next(-10, 11);
            NPC.alpha = (int)MathHelper.Clamp(NPC.alpha, 80, 120);
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter >= 5)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= 4 * frameHeight)
                    NPC.frame.Y = 0;
            }
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
        public static int ChatNumber = 0;
        public static bool request;
        public override void SetChatButtons(ref string button, ref string button2)
        {
            bool offering = Main.LocalPlayer.HasItem(ItemID.Diamond);
            button = ChatNumber switch
            {
                1 => Language.GetTextValue("Mods.Redemption.DialogueBox.SpiritNiricLady.1"),
                2 => Language.GetTextValue("Mods.Redemption.DialogueBox.SpiritNiricLady.2"),
                3 => Language.GetTextValue("Mods.Redemption.DialogueBox.SpiritNiricLady.3"),
                4 => Language.GetTextValue("Mods.Redemption.DialogueBox.SpiritNiricLady.4"),
                5 => request && offering ? Language.GetTextValue("Mods.Redemption.DialogueBox.SpiritNiricLady.Offer") : Language.GetTextValue("Mods.Redemption.DialogueBox.SpiritNiricLady.Crux"),
                _ => Language.GetTextValue("Mods.Redemption.DialogueBox.SpiritNiricLady.5"),
            };
            button2 = Language.GetTextValue("Mods.Redemption.DialogueBox.CycleD");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                Main.npcChatText = ChitChat();
                if (ChatNumber == 5)
                {
                    int offering = Main.LocalPlayer.FindItem(ItemID.Diamond);
                    if (request && offering >= 0 && Main.LocalPlayer.inventory[offering].stack >= 6)
                    {
                        if (!Main.LocalPlayer.RedemptionAbility().SpiritwalkerActive)
                        {
                            Main.npcChatText = Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.NoRealmCruxDialogue");
                            ChatNumber = 4;
                            return;
                        }
                        int card = Main.LocalPlayer.FindItem(ModContent.ItemType<EmptyCruxCard>());
                        if (card >= 0)
                        {
                            Main.LocalPlayer.inventory[offering].stack -= 6;
                            if (Main.LocalPlayer.inventory[offering].stack <= 0)
                                Main.LocalPlayer.inventory[offering] = new Item();

                            Main.LocalPlayer.inventory[card].stack--;
                            if (Main.LocalPlayer.inventory[card].stack <= 0)
                                Main.LocalPlayer.inventory[card] = new Item();

                            Main.LocalPlayer.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<CruxCardGladestone>());
                            Main.npcChatText = Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.CruxDialogue");
                            Main.npcChatCornerItem = ModContent.ItemType<CruxCardGladestone>();
                            SoundEngine.PlaySound(SoundID.Chat);
                            ChatNumber = 4;
                        }
                        else
                        {
                            Main.npcChatText = Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.NoCruxDialogue");
                            Main.npcChatCornerItem = ModContent.ItemType<EmptyCruxCard>();
                        }
                    }
                    else
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.OfferCruxDialogue");
                        Main.npcChatCornerItem = ItemID.Diamond;
                    }
                    request = true;
                }
            }
            else
            {
                ChatNumber++;
                int max = 4;
                if (Main.LocalPlayer.RedemptionAbility().SpiritwalkerActive && !Main.LocalPlayer.HasItem(ModContent.ItemType<CruxCardGladestone>()))
                    max = 5;
                if (ChatNumber > max)
                    ChatNumber = 0;
            }
        }
        public static string ChitChat()
        {
            return ChatNumber switch
            {
                0 => Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.Chat1"),
                1 => Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.Chat2"),
                2 => Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.Chat3"),
                3 => Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.Chat4"),
                4 => Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.Chat5"),
                _ => "...",
            };
        }
        public override bool CanChat() => true;
        public override string GetChat()
        {
            return Language.GetTextValue("Mods.Redemption.Dialogue.SpiritNiricLady.Dialogue");
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var effects = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            int shader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.MirageDye);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            GameShaders.Armor.ApplySecondary(shader, Main.player[Main.myPlayer], null);

            spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, NPC.frame.Size() / 2, NPC.scale, effects, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
