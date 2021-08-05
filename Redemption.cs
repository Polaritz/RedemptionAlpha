using Redemption.Globals;
using Terraria.ModLoader;

namespace Redemption
{
	public class Redemption : Mod
	{
		public override void AddRecipes() => RedeRecipes.Load(this);

		public static Redemption Instance { get; private set; }

		public Redemption()
		{
			Instance = this;
		}
    }
}