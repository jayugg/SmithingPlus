using System.Collections.Generic;
using System.Linq;
using SmithingPlus.Compat;
using SmithingPlus.ToolRecovery;
using SmithingPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace SmithingPlus.SmithWithBits;

public class ItemWorkableRod : Item, IAnvilWorkable
{
    public bool[,,] RecipeVoxels;
    
    public override void OnLoaded(ICoreAPI coreApi)
    {
        base.OnLoaded(coreApi);
        LoadVoxels(coreApi);
    }

    private void LoadVoxels(ICoreAPI coreApi)
    {
        var smallestSmithingRecipe = coreApi.GetSmithingRecipes()
            .FindAll(recipe => recipe.Output.Matches(coreApi.World, new ItemStack(this))
                               && recipe.Voxels.VoxelCount() != 0)
            .OrderBy(recipe => recipe.Voxels.VoxelCount())
            .FirstOrDefault();
        Core.Logger.VerboseDebug("[ItemWorkableRod#LoadVoxels] Loaded recipe with {0} voxels for {1} from {2}",
            smallestSmithingRecipe?.Voxels.VoxelCount() ?? 0,
            smallestSmithingRecipe?.Output.Code,
            smallestSmithingRecipe?.Ingredient
            );
        RecipeVoxels = smallestSmithingRecipe?.Voxels;
    }

    public string MetalVariant => GetMetalStack().Collectible.Variant["metal"];
    // Support for ExtraCode mod
    public bool IsBlisterSteelLike => IsBlisterSteel || Attributes["blisterSteelLike"].AsBool();
    private bool IsBlisterSteel => GetMetalStack().Collectible.Variant["metal"] == "blistersteel" &&
                                   GetMetalStack().Collectible.Code.Domain == "game";
    
    public int GetRequiredAnvilTier(ItemStack stack)
    {
        string key = this.Variant["metal"];
        int defaultValue = 0;
        MetalPropertyVariant metalPropertyVariant;
        if (this.api.ModLoader.GetModSystem<SurvivalCoreSystem>().metalsByCode.TryGetValue(key, out metalPropertyVariant))
            defaultValue = metalPropertyVariant.Tier - 1;
        JsonObject attributes = stack.Collectible.Attributes;
        if ((attributes != null ? (attributes["requiresAnvilTier"].Exists ? 1 : 0) : 0) != 0)
            defaultValue = stack.Collectible.Attributes["requiresAnvilTier"].AsInt(defaultValue);
        return defaultValue;
    }

    public List<SmithingRecipe> GetMatchingRecipes(ItemStack stack)
    {
        return this.api.GetSmithingRecipes().Where<SmithingRecipe>((System.Func<SmithingRecipe, bool>) (r => r.Ingredient.SatisfiesAsIngredient(stack))).OrderBy<SmithingRecipe, AssetLocation>((System.Func<SmithingRecipe, AssetLocation>) (r => r.Output.ResolvedItemstack.Collectible.Code)).ToList<SmithingRecipe>();
    }
    
    public List<SmithingRecipe> GetMatchingRecipes(ICoreAPI coreApi)
    {
        var ingotStack = this.Attributes[ModAttributes.IsPureMetal].AsBool() &&
                         this.CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible is ItemIngot itemIngot
            ? new ItemStack(itemIngot)
            : new ItemStack(this);
        
        return coreApi.GetSmithingRecipes()
            .Where(r => 
                r.Ingredient.Code.Equals(ingotStack.Collectible.Code) &&
                !r.Output.ResolvedItemstack.Collectible.Code.Equals(this.Code) &&
                !(r.Ingredient.RecipeAttributes?[ModRecipeAttributes.NuggetRecipe]?.AsBool() ?? false) &&
                !(r.Ingredient.RecipeAttributes?[ModRecipeAttributes.RepairOnly]?.AsBool() ?? false)
            )
            .OrderBy(r => r.Output.ResolvedItemstack.Collectible.Code)
            .Select(r =>
            {
                var p = r.Clone();
                p.Voxels = r.Voxels;
                p.Ingredient.Code = this.Code;
                p.Ingredient.Type = this.ItemClass;
                p.Ingredient.ResolvedItemstack = new ItemStack(this);
                return p;
            })
            .Distinct()
            .ToList();
    }

    private ItemStack GetMetalStack()
    {
        string metalOrMaterial = this.GetMetalOrMaterial();
        return new ItemStack(api.World.GetItem(new AssetLocation(this.Code.Domain, "ingot-" + metalOrMaterial)));
    }

    public bool CanWork(ItemStack stack)
    {
        float temperature = stack.Collectible.GetTemperature(api.World, stack);
        var ingotStack = GetMetalStack();
        float meltingPoint = ingotStack.Collectible.GetMeltingPoint(api.World, null, new DummySlot(ingotStack));
        JsonObject attributes = GetMetalStack().ItemAttributes;
        return (attributes != null ? attributes["workableTemperature"].Exists ? 1 : 0 : 0) != 0 ? stack.Collectible.Attributes["workableTemperature"].AsFloat(meltingPoint / 2f) <= (double) temperature : temperature >= meltingPoint / 2.0;
    }

    public ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        if (!CanWork(stack) || (beAnvil.WorkItemStack != null && !beAnvil.CanWorkCurrent))
            return null;
        Item obj = api.World.GetItem(new AssetLocation("workitem-" + MetalVariant));
        if (obj == null)
            return null;
        ItemStack itemstack = new ItemStack(obj);
        itemstack.Collectible.SetTemperature(api.World, itemstack, stack.Collectible.GetTemperature(api.World, stack));
        if (beAnvil.WorkItemStack == null)
        {
            if (!Core.Config.SmithWithBits) return null;
            CreateVoxelsFromRod(ref beAnvil.Voxels, RecipeVoxels ?? new bool[16, 6, 16]);
            if (ThriftySmithingCompat.ThriftySmithingLoaded) itemstack.AddToCustomWorkData(beAnvil.Voxels.MaterialCount());
        }
        else
        {
            if (!Core.Config.BitsTopUp) return null;
            Core.Logger.VerboseDebug("[ItemWorkableRod#TryPlaceOn] rod base material: {0}, workItem base material: {1}", stack.GetBaseMaterial().Collectible.Code, beAnvil.WorkItemStack.GetBaseMaterial().Collectible.Code);
            if (!beAnvil.WorkItemStack.GetBaseMaterial().CodeMatches(stack.GetBaseMaterial()))
            {
                if (api.Side == EnumAppSide.Client)
                    ((ICoreClientAPI)api).TriggerIngameError(this, "notequal", Lang.Get("Must be the same metal to add voxels"));
                return null;
            }
            var addedVoxelCount = AddVoxelsFromRod(ref beAnvil.Voxels, RecipeVoxels ?? new bool[16, 6, 16]);
            if (addedVoxelCount != 0)
            {
                if (ThriftySmithingCompat.ThriftySmithingLoaded) beAnvil.WorkItemStack.AddToCustomWorkData(addedVoxelCount);
                return itemstack;
            }
            if (api.Side == EnumAppSide.Client)
                ((ICoreClientAPI)api).TriggerIngameError(this, "requireshammering", Lang.Get("Try hammering down before adding additional voxels"));
            return null;
        }
        return itemstack;
    }

    public static void CreateVoxelsFromRod(ref byte[,,] voxels, bool[,,] recipeVoxels)
    {
        voxels = new byte[16, 6, 16];
        for (int x = 0; x < voxels.GetLength(0); x++)
        {
            for (int z = 0; z < voxels.GetLength(2); z++)
            {
                for (int y = 0; y < voxels.GetLength(1); y++)
                {
                    voxels[x, y, z] = recipeVoxels[x, y, z] ? (byte)1 : (byte)0;
                }
            }
        }
    }

    public static int AddVoxelsFromRod(ref byte[,,] voxels, bool[,,] recipeVoxels)
    {
        int num = 0;
        // place voxels over existing voxels (if place is occupied, go to next y value)
        for (int x = 0; x < voxels.GetLength(0); x++)
        {
            for (int z = 0; z < voxels.GetLength(2); z++)
            {
                for (int y = 0; y < voxels.GetLength(1); y++)
                {
                    if (recipeVoxels[x, y, z])
                    {
                        var ny = y;
                        while (ny < voxels.GetLength(1) && voxels[x, ny, z] != 0)
                        {
                            ny++;
                        }

                        if (ny >= voxels.GetLength(1)) continue;
                        voxels[x, ny, z] = 1;
                        num++;
                    }
                }
            }
        }
        return num;
    }

    public ItemStack GetBaseMaterial(ItemStack stack)
    {
        Core.Logger.VerboseDebug("[ItemWorkableRod#GetBaseMaterial] {0}", GetMetalStack().Collectible.Code);
        if (stack.Collectible is ItemWorkableRod)
        {
            if (!IsBlisterSteelLike) return GetMetalStack();
            var refinedVariant = GetMetalStack().Collectible.Attributes["refinedVariant"].AsString("steel");
            return new ItemStack(this.api.World.GetItem(GetMetalStack().Collectible.CodeWithVariant("metal", refinedVariant)));
        }
        Core.Logger.Warning("[ItemWorkableRod#GetBaseMaterial] Item {0} is not a workable rod", stack.Collectible.Code);
        return stack;
    }

    public EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil)
    {
        return EnumHelveWorkableMode.NotWorkable;
    }
}