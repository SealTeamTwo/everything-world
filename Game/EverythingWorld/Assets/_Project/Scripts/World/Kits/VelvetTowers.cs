using System.Collections.Generic;
using EverythingWorld.Player;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Velvet Rooftops as a city standing in a hole with no bottom (part of VelvetKit). The whole land is the hole.
    /// - Every artist's place is the roof of its own tower, and so is the Pool where you arrive, on the tallest. The
    ///   towers go down past rows of lit windows into the dark; the roofs step down from the Pool toward the coast.
    /// - Walkways of lit glass run from roof to roof, more than one way round. There is no ground to walk on: the
    ///   glass, the roofs, and the air.
    /// - Step off anywhere and you fall past the windows into the dark, come out of the night sky and land in the
    ///   Pool (VelvetFall): falling is the fast way home, never a punishment.
    /// - The bridges from the neighbouring lands come in at the foot of the city, and a long ramp of glass climbs
    ///   from each to the nearest roof.
    /// - Dark towers with their own lit windows stand all through the hole, some lower than you and some far higher.
    /// How it stands up: each roof is the land's own ground raised as a flat-topped pillar (so an artist's place is
    /// built on it like anywhere else), with the tower's walls and a deck built round it to hide the pillar's sides.
    public partial class VelvetKit
    {
        class Roof
        {
            public Vector2 c; public float foot, a, half, y; public IslandWorldData.Artist artist; public bool pool, well;
            public Vector3 At => new Vector3(c.x, y, c.y);
        }
        readonly List<Roof> roofList = new List<Roof>(); readonly List<Vector2> landings = new List<Vector2>(); readonly List<(Vector3 a, Vector3 b)> ways = new List<(Vector3, Vector3)>();
        float floorY; const float WellOut = HoleR + 26f, Skin = 8.5f, FlatPast = 9f;

        static float RoofY(Vector2 p, Vector2 heart, string name)
        {
            var j = 0; if (name != null) foreach (var ch in name) j = (j * 31 + ch) & 0xffff;
            return Mathf.Clamp(90f - .1f * Vector2.Distance(p, heart), 50f, 90f) + (j % 7) - 3f;
        }

        bool OnRoofTop(float x, float z, float pad)
        {
            foreach (var r in roofList)
            {
                if (r.well) { var d = Vector2.Distance(new Vector2(x, z), r.c); if (d > HoleR + 1f + pad && d < HoleR + 13f - pad) return true; continue; }
                if (Mathf.Abs(x - r.c.x) < r.a - 8f - pad && Mathf.Abs(z - r.c.y) < r.a - 8f - pad) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel; floorY = waterY + 1.2f; roofList.Clear(); landings.Clear();
            bool In(Vector2 p) => data.IslandAt(p.x, p.y) == isl;
            bool Inside(Vector2 p, float m) { if (!In(p)) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (!In(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * m)) return false; } return true; }
            var mine = new List<IslandWorldData.Artist>(); foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId) mine.Add(a);
            float HalfOf(IslandWorldData.Artist a) => Foot(a) + FlatPast + Skin;
            var keep = new List<string>();
            void Put(string name, float u, float rr) { var p = P2(u, rr); foreach (var a in mine) if (a.name == name && Inside(p, HalfOf(a) + 14f)) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // the four big roofs round the Pool; the Punjabi terrace together, east
            Put("Rihanna", 60f, 98f); Put("Frank Ocean", -72f, 98f); Put("The Weeknd", 20f, -110f); Put("SZA", -94f, -66f);
            Put("Diljit Dosanjh", 4f, 196f); Put("Karan Aujla", 82f, 232f); Put("Prabh Singh", -74f, 236f);
            // Everyone else: whoever has a settled place keeps it exactly (ArtistPlaces.txt), so no tower ever moves when
            // the world is rebuilt or the playlist grows; anyone new takes the nearest open air to the Pool.
            const float PoolHalf = 32f + Skin; var set = new List<IslandWorldData.Artist>(); foreach (var a in mine) if (keep.Contains(a.name)) set.Add(a);
            foreach (var a in mine) { if (keep.Contains(a.name)) continue; if (ArtistPlaces.TryGet(a, out var at, out _) && Inside(at, HalfOf(a) + 6f)) { a.x = at.x; a.z = at.y; set.Add(a); } }
            foreach (var a in mine)
            {
                if (set.Contains(a)) continue; var found = false;
                for (var dist = 90f; dist <= 520f && !found; dist += 10f) for (var ang = 0f; ang < 360f && !found; ang += 6f)
                {
                    var p = heart2 + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * dist; if (!Inside(p, HalfOf(a) + 12f)) continue;
                    if (Mathf.Max(Mathf.Abs(p.x - heart2.x), Mathf.Abs(p.y - heart2.y)) < PoolHalf + HalfOf(a) + 18f) continue; var ok = true;
                    foreach (var o in set) if (Mathf.Max(Mathf.Abs(p.x - o.x), Mathf.Abs(p.y - o.z)) < HalfOf(a) + HalfOf(o) + 18f) { ok = false; break; }
                    if (ok) { a.x = p.x; a.z = p.y; found = true; }
                }
                if (!found) Debug.LogWarning($"The Velvet Rooftops: no open air was found for {a.name}'s tower"); set.Add(a);
            }
            hasHole = false;
            // the Well: the great round tower whose inside is the shaft, on the nearest ground with room for it
            var bestWell = float.MaxValue;
            for (var dist = 200f; dist <= 520f; dist += 16f) for (var ang = 0f; ang < 360f; ang += 8f)
            {
                var p = heart2 + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * dist; if (dist >= bestWell || !Inside(p, WellOut + 4f)) continue; var ok = true;
                foreach (var a in mine) if (Vector2.Distance(p, new Vector2(a.x, a.z)) < WellOut + HalfOf(a) * 1.42f + 8f) { ok = false; break; }
                if (ok) { bestWell = dist; hole2 = p; hasHole = true; }
            }

            // ---- the roofs
            roofList.Add(new Roof { c = heart2, foot = 23.5f, a = 32f, half = PoolHalf, y = 94f, pool = true }); plazaY = 94f;
            foreach (var a in mine) { a.r = Mathf.Max(a.r, Foot(a) + 6f); var c = new Vector2(a.x, a.z); roofList.Add(new Roof { c = c, foot = Foot(a), a = Foot(a) + FlatPast, half = HalfOf(a), y = RoofY(c, heart2, a.name), artist = a }); }
            if (hasHole) { rimY = RoofY(hole2, heart2, null); roofList.Add(new Roof { c = hole2, foot = HoleR + 14f, a = HoleR + 14f, half = WellOut, y = rimY, well = true }); }
            // ---- where the bridges from the other lands come in: the ground is left as it is there
            foreach (var b in data.layout.bridges)
                foreach (var e in new[] { new Vector2(b.ax, b.az), new Vector2(b.bx, b.bz) })
                { var toH = (heart2 - e).normalized; if (In(e) || In(e + toH * 16f)) landings.Add(e); }
            // ---- the ground: the floor of the hole everywhere, and a flat-topped pillar under every roof
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue; var p = new Vector2(col * data.Step, z); var orig = data.heights[i];
                    var w = 0f; foreach (var e in landings) w = Mathf.Max(w, 1f - Ease(30f, 62f, Vector2.Distance(p, e)));
                    var h = Mathf.Lerp(Mathf.Min(orig, floorY), Mathf.Min(orig, waterY + 7f), w);
                    foreach (var rf in roofList)
                    {
                        if (rf.well) { var d = Vector2.Distance(p, rf.c); if (d < HoleR + 1f) h = floorY; else if (d < HoleR + 14f) h = rf.y; }
                        else if (Mathf.Abs(p.x - rf.c.x) <= rf.a && Mathf.Abs(p.y - rf.c.y) <= rf.a) h = rf.y;
                    }
                    data.heights[i] = h;
                }
            }
            Debug.Log($"The Velvet Rooftops: {roofList.Count} roofs (the Pool at {plazaY:0} m, the lowest at {LowestRoof():0} m), the Well {(hasHole ? "sited" : "not sited")}, {landings.Count} bridges come in at the foot of the city");
        }

        float LowestRoof() { var lo = float.MaxValue; foreach (var r in roofList) lo = Mathf.Min(lo, r.y); return lo; }

        // ---------------------------------------------------------------- the towers under the roofs
        void Towers(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var wins = new[] { windowWarm, windowCool, windowRose, windowWarm }; var body = WorldMaterials.Solid(new Color(.07f, .07f, .12f)); var n = 0;
            foreach (var r in roofList)
            {
                n++; if (r.well) continue;
                var c = r.At; var h = r.y - floorY + 2f; var half = r.half;
                // the walls, down into the dark, banded with lit windows (its top is just under the roof, which is the ground itself)
                mk.Box(body, new Vector3(c.x, r.y - .5f - h / 2f, c.z), new Vector3(half * 2f, h, half * 2f), Quaternion.identity);
                for (var y = r.y - 4.5f; y > floorY + 3f; y -= 5f) if ((n * 3 + (int)(y / 5f)) % 5 != 0) mk.Box(wins[(n + (int)(y / 5f)) & 3], new Vector3(c.x, y, c.z), new Vector3(half * 2f + .24f, 1.5f, half * 2f + .24f), Quaternion.identity);
                // the deck round the artist's place, out to the edge: four slabs
                var inner = r.foot + .5f; var wide = half - inner;
                mk.Box(deckM, new Vector3(c.x, r.y - .085f, c.z), new Vector3(inner * 2f + .2f, .2f, inner * 2f + .2f), Quaternion.identity);   /* and a skin of deck over the roof itself, under the artist's own floor */
                foreach (var (x, zz, sx, sz) in new[] { (0f, 1f, half * 2f, wide), (0f, -1f, half * 2f, wide), (1f, 0f, wide, inner * 2f), (-1f, 0f, wide, inner * 2f) })
                {
                    var sc = new Vector3(c.x + x * (inner + wide / 2f), r.y - .26f, c.z + zz * (inner + wide / 2f)); var size = new Vector3(sx, .6f, sz);
                    mk.Box(deckM, sc, size, Quaternion.identity); solid.Box(deckM, sc, size, Quaternion.identity);
                }
                // a line of light round the edge, and nothing else between you and the air
                foreach (var (x, zz, sx, sz) in new[] { (0f, 1f, 1f, 0f), (0f, -1f, 1f, 0f), (1f, 0f, 0f, 1f), (-1f, 0f, 0f, 1f) }) mk.Box(neonM[(n + (int)(x + zz + 2)) % 5], c + new Vector3(x * (half - .1f), .1f, zz * (half - .1f)), new Vector3(sx * half * 2f + .2f, .2f, sz * half * 2f + .2f), Quaternion.identity);
                Mark(c, r.pool ? 24f : 0f);
            }
        }

        /// What stands out on a roof's deck, in its corners: put there after the walkways, so nothing blocks one.
        void RoofThings(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(2031); var n = 0; int tanks = 0, units = 0, lights = 0, masts = 0, seats = 0;
            foreach (var r in roofList)
            {
                n++; if (r.well) continue; var c = r.At + up * .04f; var off = (r.foot + .5f + r.half) / 2f + (r.pool ? 2f : 1.2f);
                for (var k = 0; k < 4; k++)
                {
                    var g = c + new Vector3((k % 2 == 0 ? 1f : -1f) * off, 0, (k < 2 ? 1f : -1f) * off); var busy = false; foreach (var (a, b) in ways) if (Vector2.Distance(new Vector2(a.x, a.z), new Vector2(g.x, g.z)) < 7f || Vector2.Distance(new Vector2(b.x, b.z), new Vector2(g.x, g.z)) < 7f) busy = true; if (busy) continue;
                    var face = new Vector3(c.x - g.x, 0, c.z - g.z).normalized; var rot = Quaternion.Euler(0, k * 90f, 0);
                    switch ((n * 2 + k * 3) % 7)
                    {
                        case 0: WaterTower(ctx, mk, g, 1.1f + (float)rng.NextDouble() * .3f, n + k); ArtistBits.PostCollider(ctx.Root, g, 3f, 1.7f); tanks++; break;
                        case 1: for (var q = 0; q < 3; q++) { var u = g + rot * new Vector3((q - 1) * 1.5f, 0, 0); mk.Box(concrete, u + up * .5f, new Vector3(1.2f, 1f, 1.2f), rot); mk.Prism(dark, u + up * 1f, up, .42f, .06f, 10); BoxCol(ctx.Root, u + up * .5f, new Vector3(1.2f, 1f, 1.2f), rot); } units++; break;
                        case 2: mk.Box(concrete, g + up * .2f, new Vector3(3.3f, .4f, 3.3f), rot); mk.Box(skylightM, g + up * .42f, new Vector3(2.9f, .06f, 2.9f), rot); BoxCol(ctx.Root, g + up * .2f, new Vector3(3.3f, .4f, 3.3f), rot); halos.Add(g + up * 1f, 3f, new Color(.7f, .85f, 1f), .3f); lights++; break;
                        case 3: Mast(ctx, mk, g, 10f + (float)rng.NextDouble() * 9f); ArtistBits.PostCollider(ctx.Root, g, 3f, .16f); masts++; break;
                        case 4: Sofa(rootT, mk, g, face, velvet); Lamp(ctx, mk, g - face * 1.4f + Vector3.Cross(up, face) * 1.8f); seats++; break;
                        case 5: Lounger(mk, g, face, velvet); Lounger(mk, g + Vector3.Cross(up, face) * 1.4f, face, velvet); seats++; break;
                        default: Lamp(ctx, mk, g, n % 3 == 0); break;
                    }
                }
            }
            Debug.Log($"The Velvet Rooftops: on the decks, {tanks} water towers, {units} air-conditioner banks, {lights} skylights, {masts} masts, {seats} places to sit");
        }

        // ---------------------------------------------------------------- the glass between the roofs
        Vector3 Attach(Roof r, Vector3 toward)
        {
            var d = new Vector3(toward.x - r.c.x, 0, toward.z - r.c.y).normalized;
            if (r.well) return new Vector3(r.c.x, r.y + .16f, r.c.y) + d * (WellOut - .6f);
            return new Vector3(r.c.x, r.y + .04f, r.c.y) + d * (r.half / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.z)) - .6f);
        }

        bool InRoof(Roof r, float x, float z, float pad) => r.well ? Vector2.Distance(new Vector2(x, z), r.c) < WellOut + pad : Mathf.Abs(x - r.c.x) < r.half + pad && Mathf.Abs(z - r.c.y) < r.half + pad;

        bool WayClear(Vector3 p, Vector3 q, Roof a, Roof b)
        {
            var len = Vector3.Distance(p, q); for (var d = 0f; d <= len; d += 3f) { var s = Vector3.Lerp(p, q, d / len); foreach (var r in roofList) if (r != a && r != b && InRoof(r, s.x, s.z, 3f)) return false; }
            foreach (var (wa, wb) in ways) if (Crosses(p, q, wa, wb)) return false;
            return true;
        }

        static bool Crosses(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            float O(Vector3 p, Vector3 q, Vector3 r) => (q.x - p.x) * (r.z - p.z) - (q.z - p.z) * (r.x - p.x);
            if ((a - c).sqrMagnitude < 9f || (a - d).sqrMagnitude < 9f || (b - c).sqrMagnitude < 9f || (b - d).sqrMagnitude < 9f) return false;
            return O(a, b, c) * O(a, b, d) < 0f && O(c, d, a) * O(c, d, b) < 0f;
        }

        /// A walkway of lit glass from p to q (both already at the height you step on), with rails of neon you can step
        /// over, lamps at its ends, and thin legs going down into the dark under a long one.
        void GlassWay(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 q, int colour)
        {
            var up = Vector3.up; var seg = q - p; var rot = Quaternion.LookRotation(seg.normalized, up); var len = seg.magnitude;
            mk.Box(dark, (p + q) / 2f - rot * Vector3.up * .14f, new Vector3(3.4f, .2f, len), rot); mk.Box(glassWalk, (p + q) / 2f, new Vector3(2.9f, .1f, len), rot); solid.Box(dark, (p + q) / 2f - rot * Vector3.up * .22f, new Vector3(3.4f, .5f, len + 1f), rot);
            foreach (var e in new[] { -1f, 1f })
            {
                var r0 = p + rot * Vector3.right * (e * 1.65f) + up * .5f; var r1 = q + rot * Vector3.right * (e * 1.65f) + up * .5f; mk.Prism(neonM[(colour + (e > 0 ? 1 : 3)) % 5], r0, r1 - r0, .05f, (r1 - r0).magnitude, 4);
                var n = Mathf.Max(2, Mathf.RoundToInt(len / 5f)); for (var k = 0; k <= n; k++) { var s = Vector3.Lerp(p, q, k / (float)n) + rot * Vector3.right * (e * 1.65f); mk.Prism(steel, s, up, .03f, .5f, 4); }
            }
            for (var d = 18f; d < len - 12f; d += 22f) { var s = Vector3.Lerp(p, q, d / len); mk.Prism(dark, new Vector3(s.x, floorY - 1f, s.z), up, .5f, s.y - floorY + .8f, 6); }
            var steps = Mathf.Max(3, Mathf.RoundToInt(len / 6f)); for (var k = 1; k < steps; k++) Remember(Vector3.Lerp(p, q, k / (float)steps) + up * .2f, 8);
            ways.Add((p, q));
        }

        /// The network: every roof joined to the nearest roof already joined, starting from the Pool; then second ways
        /// wherever two roofs are near and nothing is between them, so there is more than one way round.
        void Walkways(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            ways.Clear(); if (roofList.Count < 2) return; var joined = new List<Roof> { roofList[0] }; var links = new HashSet<(Roof, Roof)>(); var first = 0;
            (Vector3 p, Vector3 q) Ends(Roof a, Roof b) { var p = Attach(a, b.At); var q = Attach(b, p); p = Attach(a, q); return (p, q); }
            while (joined.Count < roofList.Count)
            {
                Roof ba = null, bb = null; var best = float.MaxValue; var bestClear = false;
                foreach (var a in joined) foreach (var b in roofList)
                {
                    if (joined.Contains(b)) continue; var (p, q) = Ends(a, b); var d = Vector3.Distance(p, q); var clear = WayClear(p, q, a, b) && Mathf.Abs(q.y - p.y) < d * .4f;
                    if ((clear && !bestClear) || (clear == bestClear && d < best)) { best = d; ba = a; bb = b; bestClear = clear; }
                }
                if (bb == null) break;
                var (wp, wq) = Ends(ba, bb); GlassWay(ctx, mk, solid, wp, wq, first++); joined.Add(bb); links.Add((ba, bb)); links.Add((bb, ba));
                if (bb.artist != null) facing[bb.artist.name] = new Vector3(wq.x - bb.c.x, 0, wq.z - bb.c.y).normalized;
                if (!bestClear) Debug.LogWarning($"The Velvet Rooftops: the only way to {(bb.artist != null ? bb.artist.name : "the Well")} is steep or passes another tower");
            }
            var second = 0;
            foreach (var a in roofList)
            {
                var cands = new List<(float d, Roof b)>(); foreach (var b in roofList) { if (b == a || links.Contains((a, b))) continue; var (p, q) = Ends(a, b); cands.Add((Vector3.Distance(p, q), b)); }
                cands.Sort((x, y) => x.d.CompareTo(y.d)); var added = 0;
                foreach (var (d, b) in cands)
                {
                    if (d > 96f || added >= 1) break; var (p, q) = Ends(a, b); if (Mathf.Abs(q.y - p.y) > d * .34f || !WayClear(p, q, a, b)) continue;
                    GlassWay(ctx, mk, solid, p, q, first + second); links.Add((a, b)); links.Add((b, a)); added++; second++;
                }
            }
            // a lamp either side wherever glass meets a roof
            foreach (var (p, q) in ways.ToArray()) foreach (var (e, o) in new[] { (p, q), (q, p) }) { var d = new Vector3(o.x - e.x, 0, o.z - e.z).normalized; var g = e - d * 1.4f + Vector3.Cross(Vector3.up, d) * 2.6f; g.y = e.y; Lamp(ctx, mk, g, false); }
            Debug.Log($"The Velvet Rooftops: {first} walkways join every roof to the Pool, and {second} more make other ways round");
        }

        /// Where each bridge from another land comes in, at the foot of the city: a lit deck, and a ramp of glass up to
        /// the nearest roof it can reach without being too steep.
        void Landings(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var n = 0;
            foreach (var e in landings)
            {
                var g = ctx.OnGround(e.x, e.y); Roof best = null; var bestScore = float.MaxValue; Vector3 bp = default, bq = default;
                foreach (var r in roofList)
                {
                    var q = Attach(r, g); var toward = new Vector3(q.x - g.x, 0, q.z - g.z).normalized; var s = ctx.OnGround(g.x + toward.x * 9f, g.z + toward.z * 9f) + up * .05f; var d = Vector3.Distance(new Vector3(s.x, 0, s.z), new Vector3(q.x, 0, q.z));
                    var slope = (q.y - s.y) / Mathf.Max(1f, d); var score = d + (slope > .4f ? 2000f * (slope - .4f) : 0f) + (WayClear(s, q, r, r) ? 0f : 400f); if (score < bestScore) { bestScore = score; best = r; bp = s; bq = q; }
                }
                if (best == null) continue;
                mk.Prism(deckM, g - up * .5f, up, 10f, .62f, 24); solid.Prism(deckM, g - up * 3f, up, 10f, 3.1f, 24); mk.Prism(neonM[n % 5], g - up * .56f, up, 10.3f, .6f, 24);
                foreach (var sd in new[] { -1f, 1f }) { var d = new Vector3(bq.x - g.x, 0, bq.z - g.z).normalized; Lamp(ctx, mk, g + d * 7f + Vector3.Cross(up, d) * (sd * 3.4f) + up * .12f, sd > 0); }
                bp.y = g.y + .1f; GlassWay(ctx, mk, solid, bp, bq, n + 2);
                Shot(ctx, n == 0 ? "coming in over a bridge, the ramp up" : "another bridge's ramp", g + up * 2.2f - new Vector3(bq.x - g.x, 0, bq.z - g.z).normalized * 4f, bq + up * 4f);
                n++;
            }
            Debug.Log($"The Velvet Rooftops: {n} ramps of glass climb from the bridges");
        }

        /// Towers that are nobody's, standing all through the hole with their own lit windows: some lower than the
        /// roofs you walk, some far higher. You can land on the low ones; the way off is to jump.
        void DarkTowers(LandContext ctx, MeshKit mk, System.Random rng)
        {
            var body = WorldMaterials.Solid(new Color(.07f, .07f, .12f)); var n = 0; const float cell = 58f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = rng.NextDouble(); var w = 13f + (float)rng.NextDouble() * 12f; var dp = 13f + (float)rng.NextDouble() * 12f; var h = 24f + (float)rng.NextDouble() * (roll < .12 ? 170f : 84f); var yaw = (float)rng.NextDouble() * 90f;
                if (roll > .62 || n >= 110) continue; if (ctx.Data.IslandAt(px, pz) != ctx.Island) continue; var reach = Mathf.Max(w, dp) * .75f;
                var clear = true; foreach (var r in roofList) if (InRoof(r, px, pz, reach + 7f)) clear = false; foreach (var e in landings) if (Vector2.Distance(e, new Vector2(px, pz)) < 44f) clear = false;
                foreach (var (a, b) in ways) { var ab = new Vector2(b.x - a.x, b.z - a.z); var t = Mathf.Clamp01(Vector2.Dot(new Vector2(px - a.x, pz - a.z), ab) / Mathf.Max(.01f, ab.sqrMagnitude)); if ((new Vector2(a.x, a.z) + ab * t - new Vector2(px, pz)).magnitude < reach + 5f) clear = false; }
                if (!clear) continue;
                var bse = new Vector3(px, floorY, pz); var rot = Quaternion.Euler(0, yaw, 0);
                mk.Box(body, bse + Vector3.up * (h / 2f - 2f), new Vector3(w, h + 4f, dp), rot); var win = n % 3 == 0 ? windowCool : n % 4 == 1 ? windowRose : windowWarm;
                for (var y = 5f; y < h - 2f; y += 5f) if (rng.NextDouble() < .66) mk.Box(win, bse + Vector3.up * y, new Vector3(w + .3f, 1.4f, dp + .3f), rot);
                mk.Box(neonM[n % 5], bse + Vector3.up * (h + 2.1f), new Vector3(w + .4f, .3f, dp + .4f), rot); if (h > 110f) { mk.Prism(body, bse + Vector3.up * (h + 2f), Vector3.up, .5f, 12f, 4, .2f); mk.Blob(beaconM, bse + Vector3.up * (h + 14f), Vector3.one * .9f, rng, 0f); }
                BoxCol(ctx.Root, bse + Vector3.up * (h / 2f), new Vector3(w, h + 4f, dp), rot); n++;
            }
            Debug.Log($"The Velvet Rooftops: {n} dark towers stand in the hole");
        }

        /// Fall anywhere in the city and the dark gives you back to the Pool.
        void TheDark(LandContext ctx)
        {
            var c0 = ctx.OnGround(center.x, center.z); var pc = c0 + R * 11f; var data = ctx.Data; var isl = ctx.Island; var ends = landings.ToArray();
            var fall = new GameObject("The dark under the city").AddComponent<VelvetFall>(); fall.transform.SetParent(life.transform, false);
            fall.center = c0; fall.radius = 4000f; fall.below = floorY + 3.4f; fall.landing = new Vector3(pc.x, c0.y + 150f, pc.z); fall.sayAt = pc + Vector3.up * 2.4f;
            fall.where = p => { if (data.IslandAt(p.x, p.z) != isl) return false; foreach (var e in ends) if ((new Vector2(p.x, p.z) - e).sqrMagnitude < 72f * 72f) return false; return true; };
            Shot(ctx, "the city in the hole, from the Pool's edge", c0 + U * 30f + R * 30f + Vector3.up * 2.4f, c0 + U * 220f + R * 200f + Vector3.up * -30f);
            Shot(ctx, "down past the windows into the dark", c0 - R * 39f + Vector3.up * 2.2f, c0 - R * 60f - Vector3.up * 70f);
            if (ways.Count > 0) { var (a, b) = ways[0]; Shot(ctx, "out on the glass", Vector3.Lerp(a, b, .3f) + Vector3.up * 2.2f, b + Vector3.up * 3f); }
            Roof far = null; foreach (var r in roofList) if (r.artist != null && (far == null || Vector2.Distance(r.c, heart2) > Vector2.Distance(far.c, heart2))) far = r;
            if (far != null) Shot(ctx, "back to the Pool from the furthest roof", far.At + Vector3.up * 2.4f + new Vector3(far.c.x - heart2.x, 0, far.c.y - heart2.y).normalized * (far.half - 2f), c0 + Vector3.up * 10f);
            Shot(ctx, "the city from above", c0 + new Vector3(260f, 240f, -260f), c0 - Vector3.up * 40f);
        }
    }
}
