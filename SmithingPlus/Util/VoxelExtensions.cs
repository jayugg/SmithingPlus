using System.Linq;

namespace SmithingPlus.Util;

public static class VoxelExtensions
{
    public static int VoxelCount(this bool[,,] voxels)
    {
        return voxels.Cast<bool>().Count(voxel => voxel);
    }
    
    public static int MaterialCount(this byte[,,] voxels)
    {
        return voxels.Cast<byte>().Count(voxel => voxel == 1);
    }
    
    public static int SlagCount(this byte[,,] voxels)
    {
        return voxels.Cast<byte>().Count(voxel => voxel == 2);
    }
    
    public static byte[,,] ToByteArray(this bool[,,] voxels)
    {
        var byteVoxels = new byte[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];
        for (int x = 0; x < voxels.GetLength(0); x++)
        {
            for (int y = 0; y < voxels.GetLength(1); y++)
            {
                for (int z = 0; z < voxels.GetLength(2); z++)
                {
                    byteVoxels[x, y, z] = voxels[x, y, z] ? (byte)1 : (byte)0;
                }
            }
        }
        return byteVoxels;
    }
    
    public static void ErodeLayer(this byte[,,] byteVoxels, int layer, ref int currentVoxelCount, int targetVoxelCount)
    {
        for (int x = layer; x < byteVoxels.GetLength(0) - layer; x++)
        {
            for (int y = layer; y < byteVoxels.GetLength(1) - layer; y++)
            {
                for (int z = layer; z < byteVoxels.GetLength(2) - layer; z++)
                {
                    if (!IsEdgeVoxel(x, y, z, layer, byteVoxels) || byteVoxels[x, y, z] != 1) continue;
                    byteVoxels[x, y, z] = 0;
                    currentVoxelCount--;
                    if (currentVoxelCount <= targetVoxelCount) return;
                }
            }
        }
    }

    private static bool IsEdgeVoxel(int x, int y, int z, int layer, byte[,,] voxels)
    {
        return x == layer || x == voxels.GetLength(0) - layer - 1 ||
               y == layer || y == voxels.GetLength(1) - layer - 1 ||
               z == layer || z == voxels.GetLength(2) - layer - 1;
    }
}