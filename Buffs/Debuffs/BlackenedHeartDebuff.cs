using Redemption.Globals.NPC;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Redemption.Buffs.Debuffs
{
    public class BlackenedHeartDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soulless");
            Description.SetDefault("...");
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.statLifeMax2 -= 400;
            player.lifeRegen -= 400;
            player.blind = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<BuffNPC>().blackHeart = true;
        }
    }
}
