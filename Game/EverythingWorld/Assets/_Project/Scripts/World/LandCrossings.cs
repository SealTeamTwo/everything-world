using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Where two neighbouring lands meet for walking across: the point on their shared border nearest the straight line
    /// between their centres. Both lands ask the same question and get the same answer, so a gate in one lines up with
    /// a door in the other (the Far Shores' pages, the Jazz Quarter's globe...). Null if they don't share a border.
    public static class LandCrossings
    {
        static readonly Dictionary<(int, int), Vector3?> cache = new Dictionary<(int, int), Vector3?>();
        static IslandWorldData cachedFor;

        public static Vector3? Point(IslandWorldData data, IslandWorldData.Island a, IslandWorldData.Island b)
        {
            if (a == null || b == null || a == b) return null;
            if (cachedFor != data) { cache.Clear(); cachedFor = data; }
            var key = a.sceneId < b.sceneId ? (a.sceneId, b.sceneId) : (b.sceneId, a.sceneId);
            if (cache.TryGetValue(key, out var got)) return got;
            int ia = -1, ib = -1; for (var i = 0; i < data.layout.islands.Length; i++) { if (data.layout.islands[i].sceneId == key.Item1) ia = i; if (data.layout.islands[i].sceneId == key.Item2) ib = i; }
            if (ia < 0 || ib < 0) return cache[key] = null;
            var la = new Vector2(data.layout.islands[ia].labelX, data.layout.islands[ia].labelZ); var lb = new Vector2(data.layout.islands[ib].labelX, data.layout.islands[ib].labelZ);
            var water = data.layout.waterLevel + .5f; Vector3? best = null; var bestD = float.MaxValue;
            for (var r = 1; r < data.Nz - 1; r++)
                for (var c = 1; c < data.Nx - 1; c++)
                {
                    var i = r * data.Nx + c; if (data.islandIds[i] != ia || data.heights[i] < water) continue;
                    foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        var j = (r + dr) * data.Nx + c + dc; if (data.islandIds[j] != ib || data.heights[j] < water) continue;
                        var p = new Vector2((c + dc * .5f) * data.Step, data.layout.depth - (r + dr * .5f) * data.Step);
                        var d = ShardCaveMap.Seg(p, la, lb); if (d < bestD) { bestD = d; best = new Vector3(p.x, 0, p.y); }
                    }
                }
            return cache[key] = best;
        }
    }
}
