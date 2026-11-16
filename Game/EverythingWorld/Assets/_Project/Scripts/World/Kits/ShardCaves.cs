using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Where the Dubstep Shards' caves are, for ShardLife (it darkens the sky while you're inside one).
    public class ShardCaveMap
    {
        public readonly List<(Vector2 c, float r)> rooms = new List<(Vector2, float)>();
        public readonly List<(Vector2 a, Vector2 b, float hw)> tubes = new List<(Vector2, Vector2, float)>();
        public bool Inside(Vector3 p)
        {
            var q = new Vector2(p.x, p.z);
            foreach (var (c, r) in rooms) if ((q - c).sqrMagnitude < r * r) return true;
            foreach (var (a, b, hw) in tubes) if (Seg(q, a, b) < hw) return true;
            return false;
        }
        public static float Seg(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; var t = ab.sqrMagnitude < .01f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return Vector2.Distance(p, a + ab * t); }
        public static float SegSeg(Vector2 a, Vector2 b, Vector2 c, Vector2 d) => Mathf.Min(Mathf.Min(Seg(a, c, d), Seg(b, c, d)), Mathf.Min(Seg(c, a, b), Seg(d, a, b)));
    }

    /// The Dubstep Shards as a cave system (the owner's idea, 2026-11-15): the whole land is a black mountain bristling
    /// with crystal, hollowed into caverns joined by tunnels. You arrive in the Drop Zone, a vast cavern with the Speaker
    /// Cliff; the Rift, a wide tunnel, runs from it to the Moon Hall in the middle, Virtual Riot's (ShardRiot.cs); round
    /// the Moon Hall, tunnels lead off to four more caverns, each with its own group of artists: the Bass Cavern
    /// (Bassnectar), the Green Cavern (Skrillex), the Crystal Grotto (the small crystal artists) and the Deep. Rock mouths
    /// open from the caverns to canyons that run out to the bridges. Inside, crystals hang from the roof and stand round
    /// the walls, all pulsing with the wobble, and the sky goes dark.
    public partial class ShardKit
    {
        class Cave
        {
            public string name; public Vector2 c; public float R, Hw, H, ring, pathR; public Color tint;
            public readonly List<string> artists = new List<string>();
            public readonly List<(Vector2 dir, float hw, float wallH, float roofH)> holes = new List<(Vector2, float, float, float)>();
        }
        class Tube { public int a, b; public float hw, wallH, roofH; }
        class Mouth { public int cave; public Vector2 dir, end; public float endY; public const float hw = 7f, wallH = 8f, roofH = 11f; }
        readonly List<Cave> caves = new List<Cave>(); readonly List<Tube> tubes = new List<Tube>(); readonly List<Mouth> mouths = new List<Mouth>();
        bool caved; ShardCaveMap caveMap; readonly List<Vector3> caveClear = new List<Vector3>();
        const float PorchL = 10f;

        // ---------------------------------------------------------------- laying out the caverns (from ShapeGround)
        bool LayOutCaves(IslandWorldData data, int index, IslandWorldData.Island isl)
        {
            caves.Clear(); tubes.Clear(); mouths.Clear();
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            const float Rdz = 40f, Rh = 54f;
            // the Moon Hall straight ahead of the Drop Zone, as near as it fits
            var found = false; Vector2 dz = default, hall = default; var bestD = 999f;
            for (var k = 0; k < 16; k++)
            {
                var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 8f), Mathf.Cos(k * Mathf.PI / 8f)); var dzc = heart2 - dir * 6f;
                if (!Inside(dzc, Rdz + 12f)) continue;
                for (var d = 100f; d < 170f; d += 6f) { var h = heart2 + dir * d; if (Vector2.Distance(h, dzc) < Rdz + Rh + 16f) continue; if (Inside(h, Rh + 30f)) { if (d < bestD) { bestD = d; D2 = dir; dz = dzc; hall = h; found = true; } break; } }
            }
            if (!found) return false;
            S2 = new Vector2(-D2.y, D2.x);
            caves.Add(new Cave { name = "The Drop Zone", c = dz, R = Rdz, Hw = 16f, tint = Violet, pathR = 20f });
            caves.Add(new Cave { name = "The Moon Hall", c = hall, R = Rh, Hw = 18f, tint = White, pathR = 31f, ring = 0f }); caves[1].artists.Add("Virtual Riot");
            tubes.Add(new Tube { a = 0, b = 1, hw = 7f, wallH = 7f, roofH = 10f });
            // the other caverns and who lives in them; anyone new joins the emptiest
            var groups = new List<(string name, Color tint, List<string> who)>
            {
                ("The Bass Cavern", Magenta, new List<string> { "Bassnectar", "Freestylers", "Teminite" }),
                ("The Green Cavern", Lime, new List<string> { "Skrillex", "Of The Trees", "Panda Eyes" }),
                ("The Crystal Grotto", Cyan, new List<string> { "Prismatic", "Quartzone", "ZEOL" }),
                ("The Mirror Cave", White, new List<string> { "Eccentric", "Careless Castle", "Skybreak" }),
                ("The Deep", new Color(.3f, .4f, 1f), new List<string> { "NGHTMRE", "Subtronics", "Black Tiger Sex Machine", "Omnitica" }),
            };
            var here = new Dictionary<string, IslandWorldData.Artist>(); foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId) here[a.name] = a;
            foreach (var g in groups) g.who.RemoveAll(n => !here.ContainsKey(n));
            foreach (var n in here.Keys)
            {
                if (n == "Virtual Riot") continue; var placed = false; foreach (var g in groups) if (g.who.Contains(n)) placed = true; if (placed) continue;
                var least = groups[0]; foreach (var g in groups) if (g.who.Count < least.who.Count) least = g; least.who.Add(n);
            }
            foreach (var g in groups)
            {
                if (g.who.Count == 0) continue;
                float circ = 0f, foot = 8f; foreach (var n in g.who) { circ += 2f * Mathf.Max(here[n].r, Foot(here[n]) + 6f) + 8f; foot = Mathf.Max(foot, Foot(here[n])); }
                circ += 64f;   // room for a tunnel and a mouth between the artists
                var ring = Mathf.Max(22f, circ / (2f * Mathf.PI)); var R = ring + foot + 7f;
                var best = float.MaxValue; var at = Vector2.zero; var from = -1;
                for (var pi = 0; pi < caves.Count; pi++)
                    for (var deg = 0; deg < 360; deg += 15)
                    {
                        var dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
                        for (var Lt = 22f; Lt < 76f; Lt += 6f)
                        {
                            var p = caves[pi].c + dir * (caves[pi].R + R + Lt);
                            if (!Inside(p, R + 26f)) continue;
                            var ok = true; foreach (var o in caves) if (Vector2.Distance(p, o.c) < R + o.R + 22f) ok = false; if (!ok) continue;
                            Vector2 a0 = caves[pi].c + dir * caves[pi].R, a1 = p - dir * R;
                            for (var j = 0; j < caves.Count && ok; j++) if (j != pi && ShardCaveMap.Seg(caves[j].c, a0, a1) < caves[j].R + 8f) ok = false;
                            foreach (var t in tubes) if (ok && t.a != pi && t.b != pi && ShardCaveMap.SegSeg(a0, a1, caves[t.a].c, caves[t.b].c) < 18f) ok = false;
                            foreach (var t in tubes) if (ok && (t.a == pi || t.b == pi)) { var other = caves[t.a == pi ? t.b : t.a].c - caves[pi].c; if (Vector2.Angle(other, dir) < 40f) ok = false; }
                            if (!ok) continue;
                            var score = Lt + (pi == 1 ? 0f : 30f); if (score < best) { best = score; at = p; from = pi; }
                            break;
                        }
                    }
                if (from < 0) { Debug.LogWarning($"The Dubstep Shards: no room for {g.name}; the caves are off"); return false; }
                var cv = new Cave { name = g.name, c = at, R = R, Hw = 14f, tint = g.tint, ring = ring, pathR = Mathf.Max(8f, ring - foot - 3.5f) }; cv.artists.AddRange(g.who); caves.Add(cv);
                tubes.Add(new Tube { a = from, b = caves.Count - 1, hw = 5f, wallH = 5.5f, roofH = 8f });
            }
            foreach (var cv in caves) cv.H = cv.Hw + cv.R * .55f;
            foreach (var t in tubes) { var dir = (caves[t.b].c - caves[t.a].c).normalized; caves[t.a].holes.Add((dir, t.hw, t.wallH, t.roofH)); caves[t.b].holes.Add((-dir, t.hw, t.wallH, t.roofH)); }
            // mouths: from the nearest cavern out to each bridge, along a canyon
            bool Clear(int ci, Vector2 dir, Vector2 a0, Vector2 a1)
            {
                for (var j = 0; j < caves.Count; j++) if (j != ci && ShardCaveMap.Seg(caves[j].c, a0, a1) < caves[j].R + 14f) return false;
                foreach (var t in tubes) { if (t.a != ci && t.b != ci && ShardCaveMap.SegSeg(a0, a1, caves[t.a].c, caves[t.b].c) < 16f) return false; }
                foreach (var h in caves[ci].holes) if (Vector2.Angle(h.dir, dir) < 38f) return false;
                foreach (var m in mouths) if (ShardCaveMap.SegSeg(a0, a1, caves[m.cave].c + m.dir * caves[m.cave].R, m.end) < 20f) return false;
                return true;
            }
            foreach (var b in data.layout.bridges)
            {
                if (b.a != index && b.b != index) continue; var E = b.a == index ? new Vector2(b.ax, b.az) : new Vector2(b.bx, b.bz);
                var bi = -1; var bl = float.MaxValue; var bd = Vector2.zero;
                for (var i = 0; i < caves.Count; i++) { var dir = (E - caves[i].c).normalized; var a0 = caves[i].c + dir * caves[i].R; var len = Vector2.Distance(a0, E); if (len < bl && len > PorchL + 6f && Clear(i, dir, a0, E)) { bl = len; bi = i; bd = dir; } }
                if (bi < 0) continue;
                mouths.Add(new Mouth { cave = bi, dir = bd, end = E, endY = data.HeightAt(E.x, E.y) }); caves[bi].holes.Add((bd, Mouth.hw, Mouth.wallH, Mouth.roofH));
            }
            if (mouths.Count == 0)
                for (var k = 0; k < 16 && mouths.Count == 0; k++)
                {
                    var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 8f), Mathf.Cos(k * Mathf.PI / 8f)); var a0 = caves[0].c + dir * caves[0].R; var E = a0;
                    while (data.IslandAt(E.x, E.y) == isl && Vector2.Distance(E, a0) < 500f) E += dir * 6f;
                    if (Vector2.Distance(E, a0) > PorchL + 6f && Clear(0, dir, a0, E)) { mouths.Add(new Mouth { cave = 0, dir = dir, end = E, endY = Mathf.Max(data.HeightAt(E.x, E.y), waterY + 1f) }); caves[0].holes.Add((dir, Mouth.hw, Mouth.wallH, Mouth.roofH)); }
                }
            // the artists round each cavern, clear of the openings, facing the middle
            foreach (var cv in caves)
            {
                if (cv.ring <= 0f) continue; var n = cv.artists.Count; var chosen = new List<float>();
                var blocked = new List<(float ang, float half)>(); foreach (var h in cv.holes) blocked.Add((Mathf.Atan2(h.dir.y, h.dir.x), Mathf.Asin(Mathf.Min(1f, (h.hw + 10f) / cv.ring))));
                float Clearance(float a) { var m = 9f; foreach (var (b, half) in blocked) m = Mathf.Min(m, Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, b * Mathf.Rad2Deg)) * Mathf.Deg2Rad - half); foreach (var c in chosen) m = Mathf.Min(m, Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, c * Mathf.Rad2Deg)) * Mathf.Deg2Rad * .5f); return m; }
                for (var k = 0; k < n; k++) { var ba = 0f; var bc = float.MinValue; for (var d = 0; d < 360; d += 2) { var a = d * Mathf.Deg2Rad; var cl = Clearance(a); if (cl > bc) { bc = cl; ba = a; } } chosen.Add(ba); }
                for (var k = 0; k < n; k++) { var a = here[cv.artists[k]]; var dir = new Vector2(Mathf.Cos(chosen[k]), Mathf.Sin(chosen[k])); var p = cv.c + dir * cv.ring; a.x = p.x; a.z = p.y; }
            }
            if (here.TryGetValue("Virtual Riot", out var vr)) { vr.x = hall.x; vr.z = hall.y; }
            var msg = ""; foreach (var cv in caves) msg += $"{cv.name} (R {cv.R:0}, {cv.artists.Count} artists), "; Debug.Log($"The Dubstep Shards caves: {msg}{tubes.Count} tunnels, {mouths.Count} mouths to the bridges");
            return true;
        }

        /// How far a point is outside the caves, tunnels and canyons (negative inside), and the floor height there.
        float CaveDistance(Vector2 p, out float floorY)
        {
            var best = float.MaxValue; floorY = plazaY;
            foreach (var cv in caves) { var d = Vector2.Distance(p, cv.c) - cv.R; if (d < best) { best = d; floorY = plazaY; } }
            foreach (var t in tubes) { var d = ShardCaveMap.Seg(p, caves[t.a].c, caves[t.b].c) - (t.hw + 1f); if (d < best) { best = d; floorY = plazaY; } }
            foreach (var m in mouths)
            {
                var a0 = caves[m.cave].c + m.dir * caves[m.cave].R; var d = ShardCaveMap.Seg(p, a0, m.end) - 8f;
                if (d < best) { best = d; var len = Vector2.Distance(a0, m.end); var t = Mathf.Clamp01((Vector2.Dot(p - a0, m.dir) - PorchL - 4f) / Mathf.Max(1f, len - PorchL - 4f)); floorY = Mathf.Lerp(plazaY, m.endY, t * t * (3f - 2f * t)); }
            }
            return best;
        }

        bool InCaveZone(float x, float z, float pad) => caved && CaveDistance(new Vector2(x, z), out _) < pad;

        /// The mountain: the caves' floors levelled, and the rock rising round them, gently enough to walk over, back down
        /// to the old ground near the coast.
        void ShapeMountain(IslandWorldData data, int index, IslandWorldData.Island isl)
        {
            float Border(float x, float z) { for (var r = 8f; r <= 56f; r += 8f) for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; if (data.IslandAt(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r) != isl) return r; } return 60f; }
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var x = col * data.Step; var d = CaveDistance(new Vector2(x, z), out var fy);
                    if (d < 3f) { data.heights[i] = fy; continue; }
                    var f = Mathf.Max(Mathf.Clamp01((Border(x, z) - 12f) / 40f), Mathf.Clamp01(1f - (d - 3f) / 25f));
                    var hm = fy + Mathf.Min(22f, (d - 3f) * .6f) + (Mathf.PerlinNoise(x * .021f + 3f, z * .021f + 7f) - .5f) * 5f * Mathf.Clamp01((d - 3f) / 14f);
                    data.heights[i] = Mathf.Lerp(data.heights[i], hm, f);
                }
            }
        }

        // ---------------------------------------------------------------- building the caves (from Dress)
        Vector3 CaveAt(Cave cv, Vector2 dir, float r) => new Vector3(cv.c.x + dir.x * r, plazaY, cv.c.y + dir.y * r);
        static Vector3 V3(Vector2 v) => new Vector3(v.x, 0, v.y);

        void BuildCaves(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(2019);
            var inner = WorldMaterials.Surface(new Color(.34f, .29f, .42f), Surface.Stone); var outer = WorldMaterials.Surface(new Color(.26f, .23f, .32f), Surface.Stone);
            caveMap = new ShardCaveMap(); life.caves = caveMap;
            foreach (var cv in caves) caveMap.rooms.Add((cv.c, cv.R - 1f));
            foreach (var t in tubes) caveMap.tubes.Add((caves[t.a].c, caves[t.b].c, t.hw));
            foreach (var m in mouths) caveMap.tubes.Add((caves[m.cave].c, caves[m.cave].c + m.dir * (caves[m.cave].R + PorchL), Mouth.hw));
            // the caverns
            for (var i = 0; i < caves.Count; i++)
            {
                var cv = caves[i]; var c = new Vector3(cv.c.x, plazaY, cv.c.y);
                Shell(mk, solid, c, cv.R, cv.Hw, cv.H, cv.holes, inner, outer, 100 + i, 3.2f);
                foreach (var h in cv.holes) Collar(mk, solid, c + V3(h.dir) * (cv.R - .6f), V3(h.dir), h.hw, h.wallH, h.roofH, outer);
                Stalactites(mk, c, cv, rng);
                IndoorSpaces.Add(c + up * cv.Hw / 2f, Quaternion.identity, new Vector3(cv.R * 1.42f, cv.Hw, cv.R * 1.42f));
                ArtistBits.PointLight(ctx.Root, c + up * (cv.Hw * .7f), cv.tint, cv.R * 1.3f, .9f);
            }
            // the tunnels
            for (var i = 0; i < tubes.Count; i++)
            {
                var t = tubes[i]; Cave A = caves[t.a], B = caves[t.b]; var dir = (B.c - A.c).normalized;
                var p0 = CaveAt(A, dir, A.R - 2.5f); var p1 = CaveAt(B, -dir, B.R - 2.5f);
                Tunnel(ctx, mk, solid, p0, p1, t.hw, t.wallH, t.roofH, inner, outer, 300 + i, i == 0);
                var label = B.name; var la = CaveAt(A, dir, A.R - 3f) + up * (t.roofH + 6.4f); Sign(la, -V3(dir), label.ToUpper(), .9f, B.tint);
                var lb = CaveAt(B, -dir, B.R - 3f) + up * (t.roofH + 6.4f); Sign(lb, V3(dir), A.name.ToUpper(), .9f, A.tint);
            }
            // the mouths, out to the canyons and the bridges
            for (var i = 0; i < mouths.Count; i++)
            {
                var m = mouths[i]; var cv = caves[m.cave]; var c = new Vector3(cv.c.x, plazaY, cv.c.y); var d3 = V3(m.dir);
                var p0 = CaveAt(cv, m.dir, cv.R - 2.5f); var p1 = CaveAt(cv, m.dir, cv.R + PorchL);
                Tunnel(ctx, mk, solid, p0, p1, Mouth.hw, Mouth.wallH, Mouth.roofH, inner, outer, 500 + i, false);
                Collar(mk, solid, p1, -d3, Mouth.hw, Mouth.wallH, Mouth.roofH, outer);
                var side = Vector3.Cross(up, d3);
                foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) { var sp = p1 + side * e * (Mouth.hw + 4f + k * 2.4f) + d3 * (1.5f + k * 1.6f); Shard(ctx.Root, mk, ctx.OnGround(sp.x, sp.z), 7f + k * 3f, 1.1f - k * .2f, (side * e + d3 * .4f) * .4f, (k + i) % 2 == 0 ? shardM : shardM2, solid); }
                Sign(p1 + up * (Mouth.roofH + 3f) + d3 * .8f, d3, "THE DUBSTEP SHARDS", 1.2f, Cyan);
                Shot(ctx, "a mouth of the shards", p1 + d3 * 24f + up * 4f, p1 + up * 6f);
            }
            Massif(ctx, rng);
            CavePaths(ctx, mk);
            foreach (var cv in caves) CaveFloor(ctx, mk, solid, cv, rng);
        }

        /// A cavern's rock: a round wall rising straight to 'hw', then a dome to 'h', rough and jagged, seen from inside and
        /// out, with openings cut where the tunnels and mouths come in.
        void Shell(MeshKit mk, MeshKit solid, Vector3 c, float R, float hw, float h, List<(Vector2 dir, float hw, float wallH, float roofH)> holes, Material inner, Material outer, int seed, float jag)
        {
            var nAz = Mathf.Max(12, Mathf.CeilToInt(2f * Mathf.PI * R / 3.6f)); var ys = new List<float>(); var rs = new List<float>();
            for (var y = -2.5f; y < hw - .1f; y += 3.4f) { ys.Add(y); rs.Add(R); }
            var m = Mathf.Max(3, Mathf.CeilToInt(Mathf.PI * R / 2f / 4f));
            for (var k = 0; k < m; k++) { var phi = k * Mathf.PI / 2f / m; ys.Add(hw + (h - hw) * Mathf.Sin(phi)); rs.Add(R * Mathf.Cos(phi)); }
            var nr = ys.Count; var V = new Vector3[nr, nAz];
            for (var j = 0; j < nr; j++)
                for (var i = 0; i < nAz; i++)
                {
                    var a = i * Mathf.PI * 2f / nAz; var n = (Mathf.PerlinNoise(i * .37f + seed, j * .41f + seed * .3f) - .5f) * 2f * jag * (j == 0 ? .4f : 1f);
                    var r = Mathf.Max(.5f, rs[j] + n); V[j, i] = c + new Vector3(Mathf.Cos(a) * r, ys[j] + (j > 0 && j < nr - 1 ? n * .3f : 0f), Mathf.Sin(a) * r);
                }
            var apex = c + Vector3.up * h;
            for (var j = 0; j < nr; j++)
                for (var i = 0; i < nAz; i++)
                {
                    var i1 = (i + 1) % nAz; var top = j + 1 < nr;
                    Vector3 a = V[j, i], b = V[j, i1], cc = top ? V[j + 1, i1] : apex, d = top ? V[j + 1, i] : apex;
                    var mid = (a + b + cc + d) / 4f; var flat = new Vector2(mid.x - c.x, mid.z - c.z); var rc = flat.magnitude; var yc = mid.y - c.y; var cut = false;
                    foreach (var hl in holes) { var ang = Vector2.Angle(flat, hl.dir) * Mathf.Deg2Rad; if (ang < Mathf.PI / 2f && rc * Mathf.Sin(ang) < hl.hw + 2f && yc < hl.roofH + 2f) cut = true; }
                    if (cut) continue;
                    var o = new Vector3(c.x, Mathf.Min(mid.y, c.y + hw), c.z); var outN = (mid - o).normalized;
                    if (top) { mk.Quad(inner, a, b, cc, d, mid + outN * 4f); mk.Quad(outer, a, b, cc, d, mid - outN * 4f); solid.Quad(inner, a, b, cc, d, mid + outN * 4f); }
                    else { mk.Tri(inner, a, b, apex, mid + outN * 4f); mk.Tri(outer, a, b, apex, mid - outN * 4f); solid.Tri(inner, a, b, apex, mid + outN * 4f); }
                }
        }

        /// A rock frame round an opening, so the edges of the hole in the cavern wall never show.
        void Collar(MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir, float hw, float wallH, float roofH, Material m)
        {
            var side = Vector3.Cross(Vector3.up, dir).normalized; var up = Vector3.up;
            Vector3 P(float x, float y) => at + side * x + up * y;
            var inn = new[] { P(-hw, -1.5f), P(-hw, wallH), P(-hw + 2.2f, roofH), P(hw - 2.2f, roofH), P(hw, wallH), P(hw, -1.5f) };
            var outr = new[] { P(-hw - 5.5f, -1.5f), P(-hw - 5.5f, wallH + 3f), P(-hw - 2.5f, roofH + 5f), P(hw + 2.5f, roofH + 5f), P(hw + 5.5f, wallH + 3f), P(hw + 5.5f, -1.5f) };
            for (var k = 0; k < 5; k++)
            {
                var mid = (inn[k] + inn[k + 1] + outr[k] + outr[k + 1]) / 4f;
                mk.Quad(m, inn[k], inn[k + 1], outr[k + 1], outr[k], mid + dir * 3f); mk.Quad(m, inn[k], inn[k + 1], outr[k + 1], outr[k], mid - dir * 3f); solid.Quad(m, inn[k], inn[k + 1], outr[k + 1], outr[k], mid + dir * 3f);
                if (k > 0 && k < 4) { var e0 = inn[k] - dir * .15f; var e1 = inn[k + 1] - dir * .15f; mk.Box(k == 2 ? shardM : shardM2, (e0 + e1) / 2f, new Vector3(.18f, .18f, Vector3.Distance(e0, e1)), Quaternion.LookRotation(e1 - e0)); mk.Box(k == 2 ? shardM : shardM2, (e0 + e1) / 2f + dir * .3f, new Vector3(.18f, .18f, Vector3.Distance(e0, e1)), Quaternion.LookRotation(e1 - e0)); }
            }
        }

        /// A rough tunnel from p0 to p1 (both on the floor): straight walls, a chamfered roof, crystal veins and lamps.
        void Tunnel(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p0, Vector3 p1, float hw, float wallH, float roofH, Material inner, Material outer, int seed, bool rift)
        {
            var up = Vector3.up; var dir = p1 - p0; dir.y = 0; var len = dir.magnitude; dir /= len; var side = Vector3.Cross(up, dir);
            var n = Mathf.Max(2, Mathf.CeilToInt(len / 3f)); var prof = new[] { new Vector2(-hw, -1.5f), new Vector2(-hw, wallH), new Vector2(-hw + 2.2f, roofH), new Vector2(hw - 2.2f, roofH), new Vector2(hw, wallH), new Vector2(hw, -1.5f) };
            var V = new Vector3[n + 1, 6];
            for (var s = 0; s <= n; s++)
            {
                var o = Vector3.Lerp(p0, p1, s / (float)n); o.y = plazaY;
                for (var k = 0; k < 6; k++)
                {
                    var jit = (s == 0 || s == n || k == 0 || k == 5) ? 0f : (Mathf.PerlinNoise(s * .45f + seed, k * 1.7f) - .5f) * 1.6f;
                    var outward = new Vector2(Mathf.Sign(prof[k].x) * .8f, prof[k].y > wallH - .1f ? .6f : 0f);
                    V[s, k] = o + side * (prof[k].x + outward.x * jit) + up * (prof[k].y + outward.y * jit);
                }
            }
            for (var s = 0; s < n; s++)
                for (var k = 0; k < 5; k++)
                {
                    Vector3 a = V[s, k], b = V[s + 1, k], cc = V[s + 1, k + 1], d = V[s, k + 1]; var mid = (a + b + cc + d) / 4f; var axis = Vector3.Lerp(p0, p1, (s + .5f) / n); axis.y = plazaY + roofH * .4f;
                    var outN = mid - axis; outN.y *= .5f; outN.Normalize();
                    mk.Quad(inner, a, b, cc, d, mid + outN * 3f); mk.Quad(outer, a, b, cc, d, mid - outN * 3f); solid.Quad(inner, a, b, cc, d, mid + outN * 3f);
                }
            // crystal veins in the walls and a lamp every so often
            var rng = new System.Random(seed); var lamps = 0;
            for (var d = 3f; d < len - 2f; d += 2.5f)
            {
                var o = p0 + dir * d; o.y = plazaY;
                if (rng.NextDouble() < .5) { var e = rng.NextDouble() < .5 ? -1f : 1f; var y = 1f + (float)rng.NextDouble() * (wallH - 1.5f); mk.Box(rng.NextDouble() < .5 ? shardM : shardM2, o + side * e * (hw - .25f) + up * y, new Vector3(.1f, .8f + (float)rng.NextDouble() * 1.6f, .1f), Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, (float)rng.NextDouble() * 70f - 35f)); }
                if (rng.NextDouble() < .25) { var hang = o + side * ((float)rng.NextDouble() - .5f) * (hw * 1.4f - 4.4f) + up * roofH; mk.Prism(rng.NextDouble() < .5 ? shardM : shardM2, hang + up * .3f, Vector3.down, .25f, .8f + (float)rng.NextDouble() * 1.4f, 5, .05f); }
            }
            for (var d = 6f; d < len - 4f; d += 11f, lamps++) { var e = lamps % 2 == 0 ? 1f : -1f; var lp = p0 + dir * d + side * e * (hw - 1.3f); Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), lamps % 2 == 0); }
            if (rift) for (var d = 4f; d < len - 2f; d += 3.2f) { var o = p0 + dir * d; mk.Box((int)(d / 3.2f) % 2 == 0 ? shardM : shardM2, ctx.OnGround(o.x, o.z) + up * .14f, new Vector3(hw * 1.4f, .02f, .07f), Quaternion.LookRotation(dir) * Quaternion.Euler(0, (d * 37f) % 30f - 15f, 0)); }
            IndoorSpaces.Add((p0 + p1) / 2f + up * (roofH / 2f), Quaternion.LookRotation(dir), new Vector3(hw * 2f, roofH, len));
        }

        /// How high the mountain stands over a point: one great peak over the Moon Hall, every cavern and tunnel well
        /// under its rock, craggy, falling away to the coast, and notched into a canyon where each mouth comes out.
        float Mountain(Vector2 p)
        {
            var m = Mathf.Max(plazaY + 95f - Vector2.Distance(p, caves[1].c) * .5f, Cover(p));
            m += (Mathf.PerlinNoise(p.x * .012f + 11f, p.y * .012f + 4f) - .5f) * 16f + (Mathf.PerlinNoise(p.x * .045f + 2f, p.y * .045f + 9f) - .5f) * 6f;
            return m;
        }

        /// The rock that must stay over every cavern, tunnel and porch, wherever it is.
        float Cover(Vector2 p)
        {
            var f = plazaY; var m = float.MinValue;
            foreach (var cv in caves) m = Mathf.Max(m, f + cv.H + 10f - Mathf.Max(0f, Vector2.Distance(p, cv.c) - cv.R * .6f) * .55f);
            foreach (var t in tubes) m = Mathf.Max(m, f + t.roofH + 9f - Mathf.Max(0f, ShardCaveMap.Seg(p, caves[t.a].c, caves[t.b].c) - t.hw) * .6f);
            return m;
        }

        bool UnderMountain(LandContext ctx, float x, float z) => caved && caves.Count > 1 && !InCanyon(new Vector2(x, z), 0f) && MountainAt(ctx, x, z) > ctx.Ground(x, z) + .5f;

        /// The gorge cut into the mountain in front of each mouth: as wide as the mouth's rock frame, straight-walled.
        const float CutW = Mouth.hw + 5.5f;
        (float along, float lat, float len) CanyonCoords(Mouth mo, Vector2 p)
        {
            var cv = caves[mo.cave]; var o = cv.c + mo.dir * (cv.R + PorchL); var rel = p - o; var perp = new Vector2(-mo.dir.y, mo.dir.x);
            return (Vector2.Dot(rel, mo.dir), Vector2.Dot(rel, perp), Mathf.Min(220f, Vector2.Distance(o, mo.end)));
        }
        bool InCanyon(Vector2 p, float pad) { foreach (var mo in mouths) { var (al, la, len) = CanyonCoords(mo, p); if (al > -pad && al < len + pad && Mathf.Abs(la) < CutW + pad) return true; } return false; }
        /// Inside the patch of mountain built in each canyon's own frame (the main grid leaves these out).
        bool InCanyonPatch(Vector2 p, float shrink) { foreach (var mo in mouths) { var (al, la, len) = CanyonCoords(mo, p); if (al > -12f + shrink && al < len - shrink && Mathf.Abs(la) < CutW + 8f - shrink) return true; } return false; }

        float MountainAt(LandContext ctx, float x, float z)
        {
            var cd = 0f; for (var r = 12f; r <= 72f; r += 12f) { if (ctx.IsLand(x, z, r)) cd = r; else break; }
            var t = ctx.Ground(x, z); var k = Mathf.Clamp01(cd / 72f); k = k * k * (3f - 2f * k);
            var q = new Vector2(x, z); var cover = Cover(q) + (Mathf.PerlinNoise(x * .045f + 2f, z * .045f + 9f) - .5f) * 4f;
            return Mathf.Max(Mathf.Lerp(t - 1f, Mountain(q), k), cover);
        }

        /// The mountain itself: a rough rock surface laid over the whole cave system (you can walk on it; the caves are
        /// hollow inside it), crystals growing out of its sides and a great crystal on the summit.
        void Massif(LandContext ctx, System.Random rng)
        {
            var up = Vector3.up; var hall = caves[1].c; const float Step = 4f;
            var rockA = WorldMaterials.Surface(new Color(.3f, .26f, .38f), Surface.Stone); var rockB = WorldMaterials.Surface(new Color(.38f, .33f, .46f), Surface.Stone); var rockC = WorldMaterials.Surface(new Color(.22f, .19f, .28f), Surface.Stone);
            float x0 = hall.x - 200f, x1 = hall.x + 200f, z0 = hall.y - 200f, z1 = hall.y + 200f;
            foreach (var cv in caves) { x0 = Mathf.Min(x0, cv.c.x - cv.R - 80f); x1 = Mathf.Max(x1, cv.c.x + cv.R + 80f); z0 = Mathf.Min(z0, cv.c.y - cv.R - 80f); z1 = Mathf.Max(z1, cv.c.y + cv.R + 80f); }
            x0 = Mathf.Max(ctx.MinX, x0); x1 = Mathf.Min(ctx.MaxX, x1); z0 = Mathf.Max(ctx.MinZ, z0); z1 = Mathf.Min(ctx.MaxZ, z1);
            int nx = Mathf.CeilToInt((x1 - x0) / Step), nz = Mathf.CeilToInt((z1 - z0) / Step); var Y = new float[nx + 1, nz + 1]; var above = new bool[nx + 1, nz + 1];
            for (var i = 0; i <= nx; i++) for (var j = 0; j <= nz; j++)
            {
                float x = x0 + i * Step, z = z0 + j * Step; var t = ctx.Ground(x, z); var m = MountainAt(ctx, x, z);
                above[i, j] = m > t + .2f; Y[i, j] = above[i, j] ? m : t - .6f;
            }
            var mk = new MeshKit(); var solid = new MeshKit(); var cells = 0;
            for (var i = 0; i < nx; i++) for (var j = 0; j < nz; j++)
            {
                if (!above[i, j] && !above[i + 1, j] && !above[i, j + 1] && !above[i + 1, j + 1]) continue;
                Vector3 a = new Vector3(x0 + i * Step, Y[i, j], z0 + j * Step), b = new Vector3(x0 + (i + 1) * Step, Y[i + 1, j], z0 + j * Step), c = new Vector3(x0 + (i + 1) * Step, Y[i + 1, j + 1], z0 + (j + 1) * Step), d = new Vector3(x0 + i * Step, Y[i, j + 1], z0 + (j + 1) * Step);
                if (InCanyonPatch(new Vector2(a.x, a.z), 4.5f) || InCanyonPatch(new Vector2(b.x, b.z), 4.5f) || InCanyonPatch(new Vector2(c.x, c.z), 4.5f) || InCanyonPatch(new Vector2(d.x, d.z), 4.5f)) continue;
                var mid = (a + b + c + d) / 4f; var steep = Mathf.Max(Mathf.Abs(a.y - c.y), Mathf.Abs(b.y - d.y)) / (Step * 1.41f);
                var n = Mathf.PerlinNoise(mid.x * .03f, mid.z * .03f); var mat = steep > .9f ? rockC : n > .55f ? rockB : rockA;
                mk.Quad(mat, a, b, c, d, mid - up * 6f); solid.Quad(mat, a, b, c, d, mid - up * 6f); cells++;
            }
            foreach (var mo in mouths) Canyon(ctx, mk, solid, mo, rockA, rockC);
            mk.Build("The mountain", ctx.Root, false); solid.BuildCollider("The mountain rock", ctx.Root);
            // crystals growing out of the mountainside
            var cr = new MeshKit(); var clusters = 0;
            for (var k = 0; k < 900 && clusters < 150; k++)
            {
                float x = x0 + (float)rng.NextDouble() * (x1 - x0), z = z0 + (float)rng.NextDouble() * (z1 - z0); var t = ctx.Ground(x, z); var m = MountainAt(ctx, x, z); if (m < t + 3f || InCanyon(new Vector2(x, z), 3f)) continue;
                var p = new Vector3(x, m, z); var s = .6f + (float)rng.NextDouble() * 1.2f; var n = 2 + rng.Next(3);
                for (var j = 0; j < n; j++) { var dir = (up + Random3n(rng) * .45f).normalized; cr.Prism(j % 2 == 0 ? shardM : shardM2, p - up * 1f + Random3n(rng) * s, dir, s * (j == 0 ? 1f : .6f), (j == 0 ? 6f : 3f) * s + 2f, 5, .05f, (float)rng.NextDouble() * 70f); }
                ArtistBits.PostCollider(ctx.Root, p, 3f, s); if (clusters % 4 == 0) halos.Add(p + up * 3f * s, 3f * s, clusters % 8 == 0 ? Cyan : Magenta, .25f); clusters++;
            }
            // the summit crystal, seen from all over the realm
            var top = new Vector3(hall.x, MountainAt(ctx, hall.x, hall.y), hall.y);
            cr.Prism(shardM, top - up * 3f, (up + new Vector3(.08f, 0, .05f)).normalized, 4.5f, 40f, 6, .04f, 15f);
            for (var j = 0; j < 7; j++) { var d = Quaternion.Euler(0, j * 51f, 0) * Vector3.forward; cr.Prism(j % 2 == 0 ? shardM2 : shardM, top - up * 2f + d * 3.5f, (up + d * .5f).normalized, 2f, 14f + j * 2.4f, 5, .05f, j * 17f); }
            ArtistBits.PostCollider(ctx.Root, top, 8f, 5f); halos.Add(top + up * 18f, 26f, Cyan, .3f);
            cr.Build("The mountain crystals", ctx.Root, false);
            Debug.Log($"The Dubstep Shards mountain: {cells} cells of rock, {clusters} crystal clusters, summit {top.y - plazaY:0} m over the caves");
            Shot(ctx, "the mountain", new Vector3(hall.x, plazaY, hall.y) + new Vector3(-170f, 70f, -170f), top - up * 30f);
        }

        /// The mountain round one mouth, built in the canyon's own frame so its edges are straight: the slopes either side,
        /// the rock over the porch, sheer walls down to the canyon floor and a rock face above the mouth's frame.
        void Canyon(LandContext ctx, MeshKit mk, MeshKit solid, Mouth mo, Material rock, Material wall)
        {
            var up = Vector3.up; var cv = caves[mo.cave]; var o = cv.c + mo.dir * (cv.R + PorchL); var perp = new Vector2(-mo.dir.y, mo.dir.x); var len = Mathf.Min(220f, Vector2.Distance(o, mo.end));
            Vector2 W2(float al, float la) => o + mo.dir * al + perp * la;
            Vector3 V(float al, float la, out bool hi) { var q = W2(al, la); var t = ctx.Ground(q.x, q.y); var m = MountainAt(ctx, q.x, q.y); hi = m > t + .2f; return new Vector3(q.x, hi ? m : t - .6f, q.y); }
            void Patch(float al0, float al1, float la0, float la1, int nA, int nL)
            {
                for (var i = 0; i < nA; i++) for (var j = 0; j < nL; j++)
                {
                    float a0 = Mathf.Lerp(al0, al1, i / (float)nA), a1 = Mathf.Lerp(al0, al1, (i + 1) / (float)nA), l0 = Mathf.Lerp(la0, la1, j / (float)nL), l1 = Mathf.Lerp(la0, la1, (j + 1) / (float)nL);
                    var a = V(a0, l0, out var h1); var b = V(a1, l0, out var h2); var c = V(a1, l1, out var h3); var d = V(a0, l1, out var h4); if (!h1 && !h2 && !h3 && !h4) continue;
                    var mid = (a + b + c + d) / 4f; mk.Quad(rock, a, b, c, d, mid - up * 6f); mk.Quad(rock, a, b, c, d, mid + up * 6f); solid.Quad(rock, a, b, c, d, mid - up * 6f);
                }
            }
            var nLen = Mathf.Max(2, Mathf.CeilToInt((len + 12f) / 2f));
            Patch(-12f, len, -CutW - 8f, -CutW, nLen, 4); Patch(-12f, len, CutW, CutW + 8f, nLen, 4); Patch(-12f, 0f, -CutW, CutW, 6, 12);
            void Wall(Vector3 b0, Vector3 b1, Vector3 t1, Vector3 t0, Vector3 face) { if (t0.y <= b0.y + .05f && t1.y <= b1.y + .05f) return; t0.y = Mathf.Max(t0.y, b0.y); t1.y = Mathf.Max(t1.y, b1.y); var mid = (b0 + b1 + t1 + t0) / 4f; mk.Quad(wall, b0, b1, t1, t0, mid - face * 3f); mk.Quad(wall, b0, b1, t1, t0, mid + face * 3f); solid.Quad(wall, b0, b1, t1, t0, mid - face * 3f); }
            // the sheer sides
            foreach (var e in new[] { -1f, 1f })
                for (var i = 0; i < nLen; i++)
                {
                    float a0 = Mathf.Max(0f, -12f + i * (len + 12f) / nLen), a1 = Mathf.Max(0f, -12f + (i + 1) * (len + 12f) / nLen); if (a1 <= a0) continue;
                    var q0 = W2(a0, e * CutW); var q1 = W2(a1, e * CutW);
                    var t0 = V(a0, e * CutW, out var h0); var t1 = V(a1, e * CutW, out var h1); if (!h0 && !h1) continue;
                    Wall(new Vector3(q0.x, ctx.Ground(q0.x, q0.y) - .4f, q0.y), new Vector3(q1.x, ctx.Ground(q1.x, q1.y) - .4f, q1.y), t1, t0, V3(perp) * -e);
                }
            // the face over the mouth: from the top of the rock frame up to the mountain
            float FrameTop(float la) { var x = Mathf.Abs(la); return plazaY + (x <= Mouth.hw + 2.5f ? Mouth.roofH + 5f : Mathf.Lerp(Mouth.roofH + 5f, Mouth.wallH + 3f, (x - Mouth.hw - 2.5f) / 3f)); }
            for (var j = 0; j < 12; j++)
            {
                float l0 = Mathf.Lerp(-CutW, CutW, j / 12f), l1 = Mathf.Lerp(-CutW, CutW, (j + 1) / 12f); var q0 = W2(-.05f, l0); var q1 = W2(-.05f, l1);
                var t0 = V(0f, l0, out _); var t1 = V(0f, l1, out _); t0 = new Vector3(q0.x, t0.y, q0.y); t1 = new Vector3(q1.x, t1.y, q1.y);
                Wall(new Vector3(q0.x, FrameTop(l0) - .3f, q0.y), new Vector3(q1.x, FrameTop(l1) - .3f, q1.y), t1, t0, V3(mo.dir));
            }
        }

        /// Giant crystals bursting out through a cavern's dome, so from outside the mountain bristles.
        void OuterCrystals(LandContext ctx, MeshKit mk, Vector3 c, Cave cv, System.Random rng)
        {
            var n = Mathf.RoundToInt(cv.R / 6f);
            for (var k = 0; k < n; k++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var rr = cv.R * (.25f + (float)rng.NextDouble() * .6f); var phi = Mathf.Acos(Mathf.Clamp01(rr / cv.R));
                var p = c + new Vector3(Mathf.Cos(a) * rr, cv.Hw + (cv.H - cv.Hw) * Mathf.Sin(phi) - 1.5f, Mathf.Sin(a) * rr);
                var dir = (new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (rr / cv.R) + Vector3.up * 1.2f).normalized;
                var h = 8f + (float)rng.NextDouble() * 12f; var m = k % 2 == 0 ? shardM : shardM2;
                mk.Prism(m, p, dir, 1.2f + (float)rng.NextDouble() * 1.2f, h, 5, .05f, (float)rng.NextDouble() * 70f);
                for (var j = 0; j < 2; j++) mk.Prism(m, p + Random3n(rng) * 1.5f, (dir + Random3n(rng) * .5f).normalized, .6f, h * .5f, 5, .05f);
                halos.Add(p + dir * h * .5f, h * .4f, k % 2 == 0 ? Cyan : Magenta, .2f);
            }
        }

        /// Crystals hanging from a cavern's roof and standing round its walls.
        void Stalactites(MeshKit mk, Vector3 c, Cave cv, System.Random rng)
        {
            var up = Vector3.up; var n = Mathf.RoundToInt(cv.R * 1.1f);
            for (var k = 0; k < n; k++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var rr = Mathf.Sqrt((float)rng.NextDouble()) * (cv.R - 6f); var phi = Mathf.Acos(Mathf.Clamp01(rr / cv.R));
                var y = rr < cv.R - 6f ? cv.Hw + (cv.H - cv.Hw) * Mathf.Sin(phi) - 2.6f : cv.Hw; var p = c + new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr);
                var l = 1.5f + (float)rng.NextDouble() * 4.5f; if (y - l < 7f) continue;
                mk.Prism(k % 2 == 0 ? shardM : shardM2, p + up * 2.6f, Vector3.down, .35f + (float)rng.NextDouble() * .6f, l + 2.6f, 5, .04f, (float)rng.NextDouble() * 70f);
            }
        }

        /// A cavern's floor: crystals round the wall (clear of the openings and artists), something in the middle, seats.
        void CaveFloor(LandContext ctx, MeshKit mk, MeshKit solid, Cave cv, System.Random rng)
        {
            var up = Vector3.up; var c = ctx.OnGround(cv.c.x, cv.c.y);
            var nb = Mathf.CeilToInt(2f * Mathf.PI * (cv.R - 2.5f) / 7f);
            for (var k = 0; k < nb; k++)
            {
                var a = k * Mathf.PI * 2f / nb + (float)rng.NextDouble() * .1f; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var bad = false;
                foreach (var h in cv.holes) { var ang = Vector2.Angle(dir, h.dir) * Mathf.Deg2Rad; if (ang < Mathf.PI / 2f && (cv.R - 2.5f) * Mathf.Sin(ang) < h.hw + 6.5f) bad = true; }
                var p = CaveAt(cv, dir, cv.R - 2.4f - (float)rng.NextDouble() * 1.2f); if (bad || InFootprint(ctx, p.x, p.z, 3f) || NearCaveClear(p, 2f) || NearPath(p.x, p.z, 2.5f)) continue;
                Shard(ctx.Root, mk, ctx.OnGround(p.x, p.z), 3f + (float)rng.NextDouble() * 6f, .7f + (float)rng.NextDouble() * .6f, -V3(dir) * .3f, k % 2 == 0 ? shardM : shardM2, solid);
            }
            if (cv.name == "The Drop Zone" || cv.name == "The Moon Hall") return;
            var ins = cv.pathR - 2.5f;
            switch (cv.name)
            {
                case "The Bass Cavern":
                    // a ring of speaker stacks facing out round a magenta pit
                    mk.Prism(GlowM(Magenta, .9f), c + up * .06f, up, Mathf.Min(5f, ins * .45f), .04f, 24); mk.Prism(rockD, c - up * .3f, up, Mathf.Min(5.6f, ins * .5f), .38f, 24);
                    for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * Vector3.forward; var sp = c + d * Mathf.Min(7f, ins * .7f); var rot = Quaternion.LookRotation(d); for (var y = 0; y < 2; y++) { mk.Box(dark, sp + up * (.75f + y * 1.5f), new Vector3(1.6f, 1.45f, 1.1f), rot); mk.Prism(coneM, sp + up * (.75f + y * 1.5f) + d * .56f, d, .5f, .02f, 14); } BoxCol(ctx.Root, sp + up * 1.5f, new Vector3(1.6f, 3f, 1.1f), rot); }
                    break;
                case "The Green Cavern":
                    // one great lime crystal, cracked open, over a lime pool
                    var lime = new Material(WorldMaterials.Glow(Lime, 1.2f)) { name = "Shard lime" }; mk.Prism(GlowM(Lime, .6f), c + up * .06f, up, Mathf.Min(5f, ins * .5f), .04f, 20);
                    for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + 10f, 0) * Vector3.forward; Shard(ctx.Root, mk, c + d * (k == 0 ? 0f : 1.6f), k == 0 ? 11f : 5f + k, k == 0 ? 1.6f : .8f, d * (k == 0 ? .05f : .5f), lime, solid); }
                    break;
                case "The Crystal Grotto":
                case "The Mirror Cave":
                    // crystals turning slowly in the air over a cyan pool
                    mk.Prism(GlowM(Cyan, .7f), c + up * .06f, up, Mathf.Min(6f, ins * .55f), .04f, 24);
                    for (var k = 0; k < 5; k++) { var fm = new MeshKit(); fm.Prism(k % 2 == 0 ? shardM : shardM2, Vector3.zero, up, .7f, 2.4f, 5, .02f); fm.Prism(k % 2 == 0 ? shardM : shardM2, Vector3.zero, Vector3.down, .7f, 1.4f, 5, .02f); var fg = fm.BuildSingle("Turning crystal", ctx.Root, false); var home = c + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * (k == 0 ? 0f : 3f) + up * (4f + k * .9f); fg.transform.position = home; life.spinners.Add((fg.transform, home)); }
                    break;
                default:
                    // the Deep: a black pit with a blue glow far down, drips of light
                    mk.Prism(dark, c + up * .05f, up, Mathf.Min(5.5f, ins * .5f), .04f, 24); mk.Prism(GlowM(new Color(.3f, .4f, 1f), .7f), c + up * .07f, up, Mathf.Min(2.4f, ins * .25f), .03f, 18);
                    for (var k = 0; k < 10; k++) { var d = Quaternion.Euler(0, k * 36f, 0) * Vector3.forward; mk.Blob(rockD, c + d * Mathf.Min(6f, ins * .55f) + up * .3f, new Vector3(1f, .6f, 1f), rng, .3f); }
                    BoxCol(ctx.Root, c + up * .5f, new Vector3(1f, 1f, 1f), Quaternion.identity);
                    break;
            }
            for (var k = 0; k < 3; k++)
            {
                var a = k * 120f + 30f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var bq = c + d * Mathf.Min(10f, ins); bq = ctx.OnGround(bq.x, bq.z);
                if (InFootprint(ctx, bq.x, bq.z, 1.5f) || NearPath(bq.x, bq.z, 2f)) continue; Bench(ctx.Root, mk, bq, -d);
            }
            Shot(ctx, cv.name.ToLower(), c + V3((cv.holes.Count > 0 ? cv.holes[0].dir : Vector2.up)) * (cv.R - 8f) + up * 4f, c + up * 4f);
        }

        bool NearCaveClear(Vector3 p, float pad) { foreach (var q in caveClear) { float dx = p.x - q.x, dz = p.z - q.z, r = q.y + pad; if (dx * dx + dz * dz < r * r) return true; } return false; }

        /// The walks: a ring round each cavern, a spoke to each artist, the floor of every tunnel, the canyons out.
        void CavePaths(LandContext ctx, MeshKit mk)
        {
            var walk = 10; var strips = 0;
            void Lay(List<Vector3> pts, float w) { if (pts.Count < 2) return; Strip(mk, pts, path, w, .1f + (strips++ % 5) * .012f, ctx); foreach (var p in pts) Remember(p, walk); walk++; }
            List<Vector3> Line(Vector3 a, Vector3 b, float step = 1.5f) { var l = new List<Vector3>(); var len = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)); var n = Mathf.Max(1, Mathf.CeilToInt(len / step)); for (var i = 0; i <= n; i++) { var q = Vector3.Lerp(a, b, i / (float)n); l.Add(ctx.OnGround(q.x, q.z)); } return l; }
            foreach (var cv in caves)
            {
                if (cv.name == "The Drop Zone") continue;
                var ring = new List<Vector3>(); var n = Mathf.CeilToInt(2f * Mathf.PI * cv.pathR / 2f); for (var k = 0; k <= n; k++) { var a = k * Mathf.PI * 2f / n; ring.Add(ctx.OnGround(cv.c.x + Mathf.Cos(a) * cv.pathR, cv.c.y + Mathf.Sin(a) * cv.pathR)); }
                Lay(ring, 3f);
                foreach (var name in cv.artists)
                {
                    if (name == "Virtual Riot") continue; IslandWorldData.Artist art = null; foreach (var a in ctx.Artists) if (a.name == name) art = a; if (art == null) continue;
                    var dir = new Vector2(art.x - cv.c.x, art.z - cv.c.y).normalized; facing[name] = -V3(dir);
                    Lay(Line(CaveAt(cv, dir, cv.pathR + 1.2f), CaveAt(cv, dir, cv.ring - Foot(art) * .8f)), 2.6f);
                }
            }
            foreach (var t in tubes) { Cave A = caves[t.a], B = caves[t.b]; var dir = (B.c - A.c).normalized; Lay(Line(CaveAt(A, dir, A.pathR + .8f), CaveAt(B, -dir, B.pathR + .8f)), t.hw >= 7f ? 6f : 4f); }
            var lamps = 0;
            foreach (var m in mouths)
            {
                var cv = caves[m.cave]; var pts = Line(CaveAt(cv, m.dir, cv.pathR + .8f), new Vector3(m.end.x, 0, m.end.y), 2f); var keep = new List<Vector3>(); foreach (var p in pts) if (ctx.IsLand(p.x, p.z, 0f)) keep.Add(p); else break;
                Lay(keep, 4f); var side = Vector3.Cross(Vector3.up, V3(m.dir));
                for (var i = 0; i < keep.Count; i += 7) { var r = Vector2.Distance(new Vector2(keep[i].x, keep[i].z), cv.c); if (r < cv.R + PorchL + 3f) continue; var e = lamps++ % 2 == 0 ? 1f : -1f; var lp = keep[i] + side * e * 3.4f; if (ctx.IsLand(lp.x, lp.z, 1f)) Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), lamps % 3 == 0); }
            }
        }
    }
}
