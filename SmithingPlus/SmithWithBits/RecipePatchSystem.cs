using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.SmithWithBits;

public class RecipePatchSystem : ModSystem
{
    public override double ExecuteOrder() => 1;
    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        if (api.Side.IsClient()) return;
        foreach (var collObj in api.World.Collectibles.Where(c => c?.Code != null))
        {
            if (Core.Config.SmithWithBits)
            {
                switch (collObj)
                {
                    case ItemWorkableRod workableRod:
                        api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes
                            .AddRange(workableRod.GetMatchingRecipes(api));
                        break;
                    case ItemWorkableNugget workableNugget:
                        api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes
                            .AddRange(workableNugget.GetMatchingRecipes(api));
                        break;
                }
            }
        }
    }
}