using System;
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
        for (var x = 0; x < voxels.GetLength(0); x++)
        for (var y = 0; y < voxels.GetLength(1); y++)
        for (var z = 0; z < voxels.GetLength(2); z++)
            byteVoxels[x, y, z] = voxels[x, y, z] ? (byte)1 : (byte)0;
        return byteVoxels;
    }

    public static bool[,,] ToBoolArray(this byte[,,] voxels)
    {
        var boolVoxels = new bool[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];
        for (var x = 0; x < voxels.GetLength(0); x++)
        for (var y = 0; y < voxels.GetLength(1); y++)
        for (var z = 0; z < voxels.GetLength(2); z++)
            boolVoxels[x, y, z] = voxels[x, y, z] == 1;
        return boolVoxels;
    }

    public static byte[,,] Union(this byte[,,] voxels1, byte[,,] voxels2)
    {
        var result = new byte[voxels1.GetLength(0), voxels1.GetLength(1), voxels1.GetLength(2)];
        for (var x = 0; x < voxels1.GetLength(0); x++)
        for (var y = 0; y < voxels1.GetLength(1); y++)
        for (var z = 0; z < voxels1.GetLength(2); z++)
            result[x, y, z] = (byte)(voxels1[x, y, z] | voxels2[x, y, z]);
        return result;
    }

    public static void ErodeLayer(this byte[,,] byteVoxels, int layer, ref int currentVoxelCount, int targetVoxelCount)
    {
        for (var x = layer; x < byteVoxels.GetLength(0) - layer; x++)
        for (var y = layer; y < byteVoxels.GetLength(1) - layer; y++)
        for (var z = layer; z < byteVoxels.GetLength(2) - layer; z++)
        {
            if (!IsEdgeVoxel(x, y, z, layer, byteVoxels) || byteVoxels[x, y, z] != 1) continue;
            byteVoxels[x, y, z] = 0;
            currentVoxelCount--;
            if (currentVoxelCount <= targetVoxelCount) return;
        }
    }

    private static bool IsEdgeVoxel(int x, int y, int z, int layer, byte[,,] voxels)
    {
        return x == layer || x == voxels.GetLength(0) - layer - 1 ||
               y == layer || y == voxels.GetLength(1) - layer - 1 ||
               z == layer || z == voxels.GetLength(2) - layer - 1;
    }

    private static bool IsContourVoxel(int x, int y, int z, byte[,,] voxels)
    {
        if (voxels[x, y, z] != 0) return false;
        int dimX = voxels.GetLength(0), dimY = voxels.GetLength(1), dimZ = voxels.GetLength(2);
        var offsets = new[]
        {
            new[] { -1, 0, 0 },
            new[] { 1, 0, 0 },
            new[] { 0, -1, 0 },
            new[] { 0, 1, 0 },
            new[] { 0, 0, -1 },
            new[] { 0, 0, 1 }
        };
        foreach (var offset in offsets)
        {
            int nx = x + offset[0], ny = y + offset[1], nz = z + offset[2];
            if (nx >= 0 && ny >= 0 && nz >= 0 &&
                nx < dimX && ny < dimY && nz < dimZ &&
                voxels[nx, ny, nz] == 1)
                return true;
        }

        return false;
    }

    public static void AddSlag(this byte[,,] byteVoxels, int desiredSlagCount, Random random)
    {
        var currentSlagCount = 0;
        while (byteVoxels.SlagCount() < desiredSlagCount)
            for (var x = 0; x < byteVoxels.GetLength(0); x++)
            for (var y = 0; y < byteVoxels.GetLength(1); y++)
            for (var z = 0; z < byteVoxels.GetLength(2); z++)
            {
                if (!IsContourVoxel(x, y, z, byteVoxels) || random.NextSingle() > 0.01) continue;
                byteVoxels[x, y, z] = 2;
                currentSlagCount++;
                if (currentSlagCount >= desiredSlagCount) return;
            }
    }
}