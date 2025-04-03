using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SmithingPlus.StoneSmithing;

[HarmonyPatchCategory(Core.StoneSmithingCategory)]
[HarmonyPatch(typeof(AnvilWorkItemRenderer), nameof(AnvilWorkItemRenderer.RegenMesh))]
public static class RegenMeshPostfixPatch
{
    [HarmonyPostfix]
    public static void RegenMesh_Postfix(
        AnvilWorkItemRenderer __instance,
        ref MeshRef ___workItemMeshRef,
        ref MeshRef ___recipeOutlineMeshRef,
        ref ICoreClientAPI ___api,
        ref int ___texId,
        ItemStack workitemStack,
        byte[,,] voxels,
        bool[,,] recipeToOutlineVoxels
    )
    {
        return;
        ___api?.Logger?.Warning("RegenMesh_Postfix called");
        var yOffset = 0.1f; // workitemStack.Attributes.GetFloat("yOffset");
        ___workItemMeshRef?.Dispose();
        ___recipeOutlineMeshRef?.Dispose();
        ___workItemMeshRef = null;
        ___recipeOutlineMeshRef = null;
        var workItemMeshData = ItemWorkItem.GenMesh(___api, workitemStack, voxels, out var texId);
        ___texId = texId;
        var recipeOutlineMeshData = GenOutlineMesh(___api, recipeToOutlineVoxels, voxels);
        for (var i = 0; i < workItemMeshData.xyz.Length; i += 3)
            workItemMeshData.xyz[i + 1] += yOffset;
        ___workItemMeshRef = ___api?.Render.UploadMesh(workItemMeshData);
        if (recipeOutlineMeshData.VerticesCount <= 0)
            return;
        ___recipeOutlineMeshRef = ___api?.Render.UploadMesh(recipeOutlineMeshData);
    }

    public static MeshData GenOutlineMesh(ICoreClientAPI capi, bool[,,] recipeToOutlineVoxels, byte[,,] voxels)
    {
        var data = new MeshData(24, 36, withUv: false, withFlags: false);
        data.SetMode(EnumDrawMode.Lines);
        var color1 = capi.ColorPreset.GetColor("anvilColorGreen");
        var color2 = capi.ColorPreset.GetColor("anvilColorRed");
        var cube1 = LineMeshUtil.GetCube(color1);
        var cube2 = LineMeshUtil.GetCube(color2);
        for (var index = 0; index < cube1.xyz.Length; ++index)
        {
            cube1.xyz[index] = (float)(cube1.xyz[index] / 32.0 + 1.0 / 32.0);
            cube2.xyz[index] = (float)(cube2.xyz[index] / 32.0 + 1.0 / 32.0);
        }

        var sourceMesh = cube1.Clone();
        var length = recipeToOutlineVoxels.GetLength(1);
        for (var index1 = 0; index1 < 16; ++index1)
        for (var index2 = 0; index2 < 6; ++index2)
        for (var index3 = 0; index3 < 16; ++index3)
        {
            var flag = index2 < length && recipeToOutlineVoxels[index1, index2, index3];
            var voxel = (EnumVoxelMaterial)voxels[index1, index2, index3];
            if ((flag && voxel == EnumVoxelMaterial.Metal) || (!flag && voxel == EnumVoxelMaterial.Empty)) continue;
            var num1 = index1 / 16f;
            var num2 = (float)(0.625 + index2 / 16.0);
            var num3 = index3 / 16f;
            for (var index4 = 0; index4 < cube1.xyz.Length; index4 += 3)
            {
                sourceMesh.xyz[index4] = num1 + cube1.xyz[index4];
                sourceMesh.xyz[index4 + 1] = num2 + cube1.xyz[index4 + 1];
                sourceMesh.xyz[index4 + 2] = num3 + cube1.xyz[index4 + 2];
            }

            sourceMesh.Rgba = !flag || voxel != EnumVoxelMaterial.Empty ? cube2.Rgba : cube1.Rgba;
            data.AddMeshData(sourceMesh);
        }

        return data;
    }
}