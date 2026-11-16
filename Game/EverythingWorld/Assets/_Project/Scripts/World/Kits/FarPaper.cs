using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Far Shores as a pop-up book of the world (part of FarKit, 2026-11-16): the walkable heart is the Compass, the
    /// Promenade and the beach, the Caravan Track and camp, every artist's port of call and the lanes between, smoothed
    /// into rounded rooms, with a gateway out to each neighbouring land. Round it the book's pages fold up: rows of
    /// cut-out palms and folded paper hills, with a paper sun or cloud on a stick here and there, that you can't walk
    /// through. Beyond them, out in the rest of the land, giant folded mountains and palm cut-outs to look at, and a
    /// fleet of giant paper boats offshore. Paper palms in the heart, origami cranes overhead.
    public partial class FarKit
    {
        const float HC = 4f;                      // the heart's grid, metres
        bool[] heart; float[] heartDist; int hnx, hnz; float hx0, hz0; List<(Vector3 from, Vector3 to)> gates = new List<(Vector3, Vector3)>();
        static readonly Color[] PaperTints = { new Color(.97f, .94f, .86f), new Color(.64f, .82f, .94f), new Color(.6f, .86f, .76f), new Color(.97f, .66f, .56f), new Color(1f, .86f, .5f), new Color(.55f, .78f, .46f), new Color(.32f, .62f, .66f) };
        Material paperCream, paperEdge, frondA, frondB, trunkP;
        Material PaperM(Color c) => WorldMaterials.Surface(c, Surface.Paper);

        void PaperMaterials()
        {
            paperCream = PaperM(PaperTints[0]); paperEdge = PaperM(new Color(.99f, .98f, .95f));
            frondA = PaperM(new Color(.42f, .7f, .4f)); frondB = PaperM(new Color(.6f, .82f, .5f)); trunkP = PaperM(new Color(.82f, .66f, .46f));
        }

        /// Within 'pad' metres of the walkable heart (pad 0 = inside it).
        bool InHeart(float x, float z, float pad = 0f)
        {
            if (heart == null) return true;
            int i = Mathf.FloorToInt((x - hx0) / HC), j = Mathf.FloorToInt((z - hz0) / HC);
            if (i < 0 || j < 0 || i >= hnx || j >= hnz) return false;
            return heartDist[i + j * hnx] <= pad;
        }

        // ---------------------------------------------------------------- the heart
        void BuildHeart(LandContext ctx, MeshKit mk)
        {
            hx0 = ctx.MinX - 20f; hz0 = ctx.MinZ - 20f; hnx = Mathf.CeilToInt((ctx.MaxX - ctx.MinX + 40f) / HC); hnz = Mathf.CeilToInt((ctx.MaxZ - ctx.MinZ + 40f) / HC);
            var n = hnx * hnz; var seed = new bool[n];
            void Capsule(Vector3 a, Vector3 b, float r)
            {
                float x0 = Mathf.Min(a.x, b.x) - r, x1 = Mathf.Max(a.x, b.x) + r, z0 = Mathf.Min(a.z, b.z) - r, z1 = Mathf.Max(a.z, b.z) + r;
                var a2 = new Vector2(a.x, a.z); var b2 = new Vector2(b.x, b.z);
                for (var j = Mathf.Max(0, Mathf.FloorToInt((z0 - hz0) / HC)); j <= Mathf.Min(hnz - 1, Mathf.FloorToInt((z1 - hz0) / HC)); j++)
                for (var i = Mathf.Max(0, Mathf.FloorToInt((x0 - hx0) / HC)); i <= Mathf.Min(hnx - 1, Mathf.FloorToInt((x1 - hx0) / HC)); i++)
                {
                    var p = new Vector2(hx0 + (i + .5f) * HC, hz0 + (j + .5f) * HC);
                    if (ShardCaveMap.Seg(p, a2, b2) <= r) seed[i + j * hnx] = true;
                }
            }
            void Disc(Vector3 c, float r) => Capsule(c, c, r);
            var beach = new Vector3(beach2.x, 0, beach2.y); var camp = new Vector3(camp2.x, 0, camp2.y);
            Disc(center, 45f); Capsule(center, beach, 22f); Disc(beach, 42f); Capsule(center, camp, 16f);
            foreach (var a in ctx.Artists) Disc(new Vector3(a.x, 0, a.z), Foot(a) + 16f);
            foreach (var (p, w) in walkPoints) if (w >= 10) Disc(p, 9f);
            // a gateway toward each neighbouring land: from the nearest point of the shared border to the nearest path
            gates = Gateways(ctx); foreach (var (from, to) in gates) { Capsule(from, to, 10f); GateLane(ctx, mk, from, to); }
            // rounded rooms: close the gaps between nearby pieces (grow 20 m, then shrink back 20 m)
            var grow = Dist(seed); var big = new bool[n]; for (var k = 0; k < n; k++) big[k] = grow[k] <= 20f;
            var outside = new bool[n]; for (var k = 0; k < n; k++) outside[k] = !big[k];
            var shrink = Dist(outside); heart = new bool[n]; for (var k = 0; k < n; k++) heart[k] = seed[k] || (big[k] && shrink[k] > 20f);
            heartDist = Dist(heart);
            var cells = 0; for (var k = 0; k < n; k++) if (heart[k] && ctx.IsLand(hx0 + (k % hnx + .5f) * HC, hz0 + (k / hnx + .5f) * HC, 0f)) cells++;
            Debug.Log($"The Far Shores: walkable heart {cells * HC * HC / 1e4f:0.0} ha");
        }

        /// Metres from every grid cell to the nearest 'on' cell (a two-pass chamfer distance).
        float[] Dist(bool[] on)
        {
            var n = hnx * hnz; var d = new float[n]; const float D1 = HC, D2 = HC * 1.4142f;
            for (var k = 0; k < n; k++) d[k] = on[k] ? 0f : 1e9f;
            for (var j = 0; j < hnz; j++) for (var i = 0; i < hnx; i++)
            {
                var k = i + j * hnx; var v = d[k];
                if (i > 0) v = Mathf.Min(v, d[k - 1] + D1);
                if (j > 0) { v = Mathf.Min(v, d[k - hnx] + D1); if (i > 0) v = Mathf.Min(v, d[k - hnx - 1] + D2); if (i < hnx - 1) v = Mathf.Min(v, d[k - hnx + 1] + D2); }
                d[k] = v;
            }
            for (var j = hnz - 1; j >= 0; j--) for (var i = hnx - 1; i >= 0; i--)
            {
                var k = i + j * hnx; var v = d[k];
                if (i < hnx - 1) v = Mathf.Min(v, d[k + 1] + D1);
                if (j < hnz - 1) { v = Mathf.Min(v, d[k + hnx] + D1); if (i < hnx - 1) v = Mathf.Min(v, d[k + hnx + 1] + D2); if (i > 0) v = Mathf.Min(v, d[k + hnx - 1] + D2); }
                d[k] = v;
            }
            return d;
        }

        /// For each land this one borders over ground: the border point nearest the Compass, and the nearest path to it.
        List<(Vector3 from, Vector3 to)> Gateways(LandContext ctx)
        {
            var best = new Dictionary<string, (Vector3 p, float d)>(); var isles = new Dictionary<string, IslandWorldData.Island>();
            for (var x = ctx.MinX; x <= ctx.MaxX; x += HC)
            for (var z = ctx.MinZ; z <= ctx.MaxZ; z += HC)
            {
                if (ctx.Data.IslandAt(x, z) != ctx.Island || ctx.Ground(x, z) < waterY + .5f) continue;
                foreach (var (dx, dz) in new[] { (HC, 0f), (-HC, 0f), (0f, HC), (0f, -HC) })
                {
                    var o = ctx.Data.IslandAt(x + dx, z + dz); if (o == null || o == ctx.Island || ctx.Ground(x + dx, z + dz) < waterY + .5f) continue;
                    var d = Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z));
                    if (!best.TryGetValue(o.name, out var b) || d < b.d) best[o.name] = (new Vector3(x + dx * .5f, 0, z + dz * .5f), d); isles[o.name] = o;
                }
            }
            var list = new List<(Vector3, Vector3)>();
            foreach (var kv in best)
            {
                var p = LandCrossings.Point(ctx.Data, ctx.Island, isles[kv.Key]) ?? kv.Value.p;   // the crossing the neighbour uses too
                var to = center; var bd = float.MaxValue;
                foreach (var (q, _) in walkPoints) { var d = (new Vector2(q.x, q.z) - new Vector2(p.x, p.z)).sqrMagnitude; if (d < bd) { bd = d; to = q; } }
                list.Add((p, new Vector3(to.x, 0, to.z)));
            }
            Debug.Log($"The Far Shores: {list.Count} gateways to neighbouring lands ({string.Join(", ", best.Keys)})");
            return list;
        }

        /// A lane from a gateway in to the nearest path.
        void GateLane(LandContext ctx, MeshKit mk, Vector3 from, Vector3 to)
        {
            var dir = to - from; dir.y = 0; var len = dir.magnitude; if (len < 4f) return; dir /= len; var side = Vector3.Cross(Vector3.up, dir);
            var pts = new List<Vector3>();
            for (var d = 0f; d <= len; d += 1.5f) { var q = from + dir * d + side * Mathf.Sin(d / 16f) * Mathf.Min(2.5f, len * .03f); if (!ctx.IsLand(q.x, q.z, 0f)) continue; pts.Add(ctx.OnGround(q.x, q.z)); }
            if (pts.Count < 2) return;
            Strip(mk, pts, stone, PathW, .13f, ctx);
            foreach (var p in pts) Remember(p, 30);
        }

        // ---------------------------------------------------------------- card helpers
        /// A flat piece of card standing upright: a convex outline (x along 'right', y up), 'thick' thick, its front
        /// facing 'n', with a white cut edge all round.
        void Card(MeshKit mk, Material front, Material back, Vector3 o, Vector3 right, Vector3 n, IList<Vector2> poly, float thick = .06f)
        {
            var up = Vector3.up; var hn = n * (thick / 2f);
            Vector3 P(Vector2 q) => o + right * q.x + up * q.y;
            var mid = Vector3.zero; foreach (var q in poly) mid += P(q); mid /= poly.Count;
            for (var i = 1; i + 1 < poly.Count; i++)
            {
                mk.Tri(front, P(poly[0]) + hn, P(poly[i]) + hn, P(poly[i + 1]) + hn, mid - n * 3f);
                mk.Tri(back, P(poly[0]) - hn, P(poly[i]) - hn, P(poly[i + 1]) - hn, mid + n * 3f);
            }
            for (var i = 0; i < poly.Count; i++) { var a = P(poly[i]); var b = P(poly[(i + 1) % poly.Count]); mk.Quad(paperEdge, a + hn, b + hn, b - hn, a - hn, mid); }
        }

        static Vector2[] Circle(Vector2 c, float r, int sides, float a0 = 0f, float a1 = 360f)
        {
            var full = Mathf.Approximately(a1 - a0, 360f); var n = full ? sides : sides + 1; var pts = new Vector2[n];
            for (var k = 0; k < n; k++) { var a = (a0 + (a1 - a0) * k / sides) * Mathf.Deg2Rad; pts[k] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
            return pts;
        }

        /// A cut-out palm: a curved paper trunk and a crown of leaf shapes, flat like a pop-up page.
        void CutoutPalm(MeshKit mk, Vector3 o, Vector3 right, Vector3 n, float s, System.Random rng)
        {
            var bend = ((float)rng.NextDouble() - .5f) * 1.6f; var h = (6.5f + (float)rng.NextDouble() * 3f) * s; Vector2 top = default;
            for (var k = 0; k < 6; k++)
            {
                float t0 = k / 6f, t1 = (k + 1) / 6f; float x0 = Mathf.Sin(t0 * 1.6f) * bend * s, x1 = Mathf.Sin(t1 * 1.6f) * bend * s, w0 = Mathf.Lerp(.32f, .18f, t0) * s, w1 = Mathf.Lerp(.32f, .18f, t1) * s;
                Card(mk, trunkP, paperCream, o, right, n, new[] { new Vector2(x0 - w0, t0 * h - .3f), new Vector2(x0 + w0, t0 * h - .3f), new Vector2(x1 + w1, t1 * h), new Vector2(x1 - w1, t1 * h) });
                top = new Vector2(x1, t1 * h);
            }
            for (var k = 0; k < 7; k++)
            {
                var a = (165f - k * 25f + ((float)rng.NextDouble() - .5f) * 10f) * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .55f - .25f).normalized; var p = new Vector2(-d.y, d.x);
                var L = (2.6f + (float)rng.NextDouble()) * s; var w = .5f * s; var o2 = o + n * (k % 2 == 0 ? .05f : -.05f);
                Card(mk, k % 2 == 0 ? frondA : frondB, frondB, o2, right, n, new[] { top, top + d * L * .45f - p * w, top + d * L, top + d * L * .45f + p * w });
            }
            Card(mk, PaperM(new Color(.55f, .38f, .24f)), paperCream, o + n * .08f, right, n, Circle(top + new Vector2(0, -.35f * s), .28f * s, 8));
        }

        /// A folded paper hill: one sheet pleated like a fan, standing up from the ground to a peak.
        void FoldedHill(MeshKit mk, Material front, Material back, Vector3 o, Vector3 right, Vector3 n, float w, float h, int pleats, float depth)
        {
            var up = Vector3.up; var apex = o + up * h;
            for (var k = 0; k < pleats; k++)
            {
                Vector3 B(int i) => o + right * (-w / 2f + w * i / pleats) + n * ((i % 2 == 0 ? 1f : -1f) * depth) - up * .3f;
                var a = B(k); var b = B(k + 1); var mid = (apex + a + b) / 3f; var nrm = Vector3.Cross(a - apex, b - apex).normalized; if (Vector3.Dot(nrm, n) < 0f) nrm = -nrm;
                mk.Tri(front, apex, a, b, mid - nrm); mk.Tri(back, apex, a, b, mid + nrm);
            }
        }

        // ---------------------------------------------------------------- the pages folding up round the heart
        void PaperEdges(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pieces = 0, palms = 0, hills = 0, waves = 0, suns = 0;
            var stick = WorldMaterials.Surface(new Color(.7f, .56f, .38f), Surface.Wood);
            for (var j = 1; j < hnz - 1; j++)
            for (var i = 1; i < hnx - 1; i++)
            {
                var k = i + j * hnx; if (heart[k]) continue;
                // an edge cell: on this land, just outside the heart; its pieces face in
                var inward = Vector2.zero; var touches = false;
                for (var dj = -1; dj <= 1; dj++) for (var di = -1; di <= 1; di++) if (heart[k + di + dj * hnx]) { touches = true; inward += new Vector2(di, dj); }
                if (!touches || inward.sqrMagnitude < .01f) continue;
                float x = hx0 + (i + .5f) * HC, z = hz0 + (j + .5f) * HC;
                if (ctx.Data.IslandAt(x, z) != ctx.Island || ctx.Ground(x, z) < waterY + .3f) continue;
                inward.Normalize(); var n = new Vector3(inward.x, 0, inward.y);
                Page(ctx, mk, solid, x, z, n, k, stick, ref palms, ref hills, ref waves, ref suns); pieces++;
            }
            // the land's borders with its neighbours get pages too, facing out, except where a gateway comes through:
            // so the land beyond the pages can't be reached from next door either, only seen
            var border = 0;
            for (var j = 1; j < hnz - 1; j++)
            for (var i = 1; i < hnx - 1; i++)
            {
                var k = i + j * hnx; if (heartDist[k] <= HC) continue;
                float x = hx0 + (i + .5f) * HC, z = hz0 + (j + .5f) * HC;
                if (ctx.Data.IslandAt(x, z) != ctx.Island || ctx.Ground(x, z) < waterY + .3f) continue;
                var outward = Vector2.zero;
                foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) })
                {
                    var o = ctx.Data.IslandAt(x + dx * HC, z + dz * HC); if (o != null && o != ctx.Island && ctx.Ground(x + dx * HC, z + dz * HC) > waterY + .3f) outward += new Vector2(dx, dz);
                }
                if (outward.sqrMagnitude < .01f) continue;
                outward.Normalize(); Page(ctx, mk, solid, x, z, new Vector3(outward.x, 0, outward.y), k + 1000003, stick, ref palms, ref hills, ref waves, ref suns); border++;
            }
            foreach (var (from, _) in gates) GateArch(ctx, mk, solid, from);
            Debug.Log($"The Far Shores pages: {pieces} pieces round the heart and {border} along the borders with other lands ({palms} palm cut-outs, {hills} folded hills, {waves} paper waves, {suns} suns and clouds on sticks), {gates.Count} gateways");
        }

        /// One piece of the pages at grid cell (x, z), its front facing 'n': paper waves by the sea, else cut-out palms
        /// or folded hills by turns, now and then a paper sun or cloud on a stick behind; solid.
        void Page(LandContext ctx, MeshKit mk, MeshKit solid, float x, float z, Vector3 n, int k, Material stick, ref int palms, ref int hills, ref int waves, ref int suns)
        {
            {
                var right = Vector3.Cross(Vector3.up, n);
                var rng = new System.Random(k * 7919 + 17); var g = ctx.OnGround(x, z) - n * (float)rng.NextDouble() * 1.5f;
                var tint = PaperTints[1 + rng.Next(PaperTints.Length - 1)]; var front = PaperM(tint);
                var kind = Mathf.PerlinNoise(x * .012f + 3f, z * .012f + 7f);
                if (ctx.Ground(x, z) < waterY + 3.2f)
                {
                    // by the sea: rows of paper waves, a pale one in front of a deeper one
                    Card(mk, PaperM(PaperTints[1]), paperCream, g + n * .6f, right, n, Circle(Vector2.zero, 1.6f + (float)rng.NextDouble() * .6f, 9, 0f, 180f));
                    Card(mk, PaperM(PaperTints[6]), paperCream, g - n * .6f + right * 1.4f, right, n, Circle(Vector2.zero, 2.4f + (float)rng.NextDouble() * .8f, 10, 0f, 180f));
                    waves++;
                }
                else if (kind < .45f) { CutoutPalm(mk, g, right, n, .9f + (float)rng.NextDouble() * .5f, rng); if (rng.NextDouble() < .6) Card(mk, frondB, paperCream, g + n * 1.2f + right * ((float)rng.NextDouble() - .5f) * 3f, right, n, Circle(Vector2.zero, 1.1f + (float)rng.NextDouble() * .6f, 8, 0f, 180f)); palms++; }
                else { FoldedHill(mk, front, paperCream, g, right, n, 6f + (float)rng.NextDouble() * 3f, 4f + (float)rng.NextDouble() * 4.5f, 6, .45f); if (rng.NextDouble() < .35) FoldedHill(mk, PaperM(PaperTints[5]), paperCream, g + n * 1.6f + right * ((float)rng.NextDouble() - .5f) * 4f, right, n, 4f, 2.4f, 4, .3f); hills++; }
                // a paper sun or a cloud on a stick, standing up behind now and then
                if (rng.NextDouble() < .06)
                {
                    var sp = g - n * 2.5f; var h = 7f + (float)rng.NextDouble() * 4f; mk.Prism(stick, sp - Vector3.up * .3f, Vector3.up, .05f, h + .3f, 5);
                    if (rng.NextDouble() < .5)
                    {
                        Card(mk, PaperM(new Color(1f, .82f, .32f)), paperCream, sp + n * .1f, right, n, Circle(new Vector2(0, h), 1.3f, 14));
                        for (var r = 0; r < 12; r++) { var a = r * 30f * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var pp = new Vector2(-d.y, d.x); var c = new Vector2(0, h); Card(mk, PaperM(new Color(1f, .7f, .3f)), paperCream, sp + n * .05f, right, n, new[] { c + d * 1.4f - pp * .3f, c + d * 2.3f, c + d * 1.4f + pp * .3f }); }
                    }
                    else foreach (var (cx, cy, cr) in new[] { (-1f, 0f, .9f), (0f, .4f, 1.2f), (1.1f, 0f, .85f) }) Card(mk, paperEdge, paperCream, sp + n * .08f, right, n, Circle(new Vector2(cx, h + cy), cr, 10));
                    suns++;
                }
                // solid: nobody walks through the pages
                solid.Box(paperCream, g + Vector3.up * 1.6f - n * .4f, new Vector3(6.4f, 3.2f, 1.6f), Quaternion.LookRotation(n));
            }
        }

        /// A paper arch where a gateway meets the border: two tall folded posts and a banner with the land's name.
        void GateArch(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var to = center - at; to.y = 0; to.Normalize(); var right = Vector3.Cross(Vector3.up, to); var g = ctx.OnGround(at.x, at.z); var up = Vector3.up;
            foreach (var e in new[] { -1f, 1f }) { var p = g + right * e * 3.4f; mk.Prism(PaperM(PaperTints[e < 0 ? 3 : 1]), p - up * .3f, up, .45f, 6.6f, 4, .8f, 45f); solid.Box(paperCream, p + up * 3f, new Vector3(1f, 6f, 1f), Quaternion.LookRotation(to)); }
            Card(mk, PaperM(PaperTints[4]), PaperM(PaperTints[4]), g, right, to, new[] { new Vector2(-4.2f, 5.2f), new Vector2(4.2f, 5.2f), new Vector2(4.2f, 6.6f), new Vector2(-4.2f, 6.6f) }, .1f);
            foreach (var e in new[] { -1f, 1f }) ArtistBits.Label(ctx.Root, "THE FAR SHORES", g + up * 5.9f + to * e * .07f, to * e, .5f, Ink, TextAnchor.MiddleCenter);
        }

        /// Out in the rest of the land, beyond the pages: giant folded mountains and palm cut-outs to look at from inside,
        /// and a fleet of giant paper boats bobbing offshore.
        void PaperScenery(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var rng = new System.Random(4040); int mountains = 0, giants = 0;
            const float cell = 46f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (InHeart(px, pz, 16f) || !ctx.IsLand(px, pz, 8f)) continue;
                var g = ctx.OnGround(px, pz); var to = new Vector3(center.x - px, 0, center.z - pz).normalized; var yaw = Quaternion.Euler(0, ((float)rng.NextDouble() - .5f) * 50f, 0); var n = yaw * to; var right = Vector3.Cross(Vector3.up, n);
                if (roll < .62f) { var tint = PaperTints[1 + rng.Next(PaperTints.Length - 1)]; var w = 16f + s * 14f; FoldedHill(mk, PaperM(tint), paperCream, g, right, n, w, 12f + s * 16f, 8, 1.4f); solid.Box(paperCream, g + Vector3.up * 2f, new Vector3(w * .8f, 4f, 3f), Quaternion.LookRotation(n)); mountains++; }
                else { CutoutPalm(mk, g, right, n, 1.8f + s * .8f, rng); solid.Box(paperCream, g + Vector3.up * 2f, new Vector3(1.4f, 4f, 1f), Quaternion.LookRotation(n)); giants++; }
            }
            // the fleet
            var fleet = 0;
            for (var k = 0; k < 7; k++)
            {
                var p = beach2 - R2 * (70f + (k % 3) * 45f + (float)rng.NextDouble() * 20f) + U2 * ((k - 3) * 55f + ((float)rng.NextDouble() - .5f) * 20f);
                if (ctx.Data.IslandAt(p.x, p.y) != null || ctx.Data.HeightAt(p.x, p.y) > waterY - 1.5f) continue;
                var bm = new MeshKit(); PaperBoat(bm, Vector3.zero, 9f + (float)rng.NextDouble() * 4f, PaperM(PaperTints[rng.Next(PaperTints.Length)]));
                var go = bm.BuildSingle("Paper boat (sea)", life.transform, false); go.transform.SetPositionAndRotation(new Vector3(p.x, waterY - .4f, p.y), Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0));
                life.seaBoats.Add(go.transform); fleet++;
            }
            Debug.Log($"The Far Shores beyond the pages: {mountains} folded mountains, {giants} giant palm cut-outs, {fleet} giant paper boats offshore");
        }

        /// A folded paper boat, 'len' long along +z: two hull sides meeting in a V, and the tall middle fold.
        void PaperBoat(MeshKit mk, Vector3 o, float len, Material m)
        {
            float L = len, H = len * .32f, W = len * .18f;
            Vector3 s = o + new Vector3(0, H * .55f, -L / 2f), f = o + new Vector3(0, H * .55f, L / 2f), b0 = o + new Vector3(0, 0, -L * .3f), b1 = o + new Vector3(0, 0, L * .3f), sail = o + new Vector3(0, H * 1.45f, 0), s0 = o + new Vector3(0, H * .45f, -L * .32f), s1 = o + new Vector3(0, H * .45f, L * .32f);
            foreach (var e in new[] { -1f, 1f })
            {
                var side = o + new Vector3(e * W, H * .45f, 0); var inside = o + new Vector3(-e * W * 3f, H * .3f, 0);
                mk.Tri(m, s, b0, side, inside); mk.Tri(m, b0, b1, side, inside); mk.Tri(m, b1, f, side, inside);
                mk.Tri(paperCream, s, b0, side, side + new Vector3(e * W * 3f, 0, 0)); mk.Tri(paperCream, b0, b1, side, side + new Vector3(e * W * 3f, 0, 0)); mk.Tri(paperCream, b1, f, side, side + new Vector3(e * W * 3f, 0, 0));
            }
            mk.Tri(m, s0, s1, sail, o + new Vector3(-1f, H, 0)); mk.Tri(m, s0, s1, sail, o + new Vector3(1f, H, 0));
        }

        /// A paper palm in the heart: a folded square trunk and pleated fronds, like origami.
        void PaperPalm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng, bool straight)
        {
            var up = Vector3.up; var lean = straight ? Vector3.zero : Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) * Vector3.forward * (.1f + (float)rng.NextDouble() * .2f); var p = g - up * .3f; var d = (up + lean).normalized;
            for (var k = 0; k < 6; k++) { var len = 1.3f * s; mk.Prism(trunkP, p, d, (.22f - k * .02f) * s, len + .04f, 4, .92f, k * 22f); p += d * len; d = (d + lean * .3f).normalized; }
            for (var k = 0; k < 7; k++)
            {
                var dir = Quaternion.Euler(0, k * 51f + (float)rng.NextDouble() * 18f, 0) * Vector3.forward; var side = Vector3.Cross(up, dir); var L = (2.6f + (float)rng.NextDouble() * .6f) * s; const int steps = 6;
                Vector3 S(int i) { var t = i / (float)steps; return p + dir * L * t + up * (L * (.35f * Mathf.Sin(t * Mathf.PI * .9f) - .55f * t * t)) + up * (i % 2 == 0 ? .06f : -.06f) * s; }
                float W(int i) => .5f * s * Mathf.Sin(Mathf.PI * Mathf.Clamp01((i + .4f) / (steps + .4f)));
                var m = k % 2 == 0 ? frondA : frondB;
                for (var i = 0; i < steps; i++)
                {
                    Vector3 a = S(i), b = S(i + 1), l0 = a - side * W(i) - up * .08f * s, l1 = b - side * W(i + 1) - up * .08f * s, r0 = a + side * W(i) - up * .08f * s, r1 = b + side * W(i + 1) - up * .08f * s;
                    mk.Quad(m, a, l0, l1, b, a - up); mk.Quad(m, a, l0, l1, b, a + up * 2f); mk.Quad(m, a, b, r1, r0, a - up); mk.Quad(m, a, b, r1, r0, a + up * 2f);
                }
            }
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s);
        }

        // ---------------------------------------------------------------- origami cranes overhead
        void Cranes(LandContext ctx)
        {
            var rng = new System.Random(808);
            for (var k = 0; k < 14; k++)
            {
                var m = new MeshKit(); var col = PaperM(PaperTints[rng.Next(PaperTints.Length)]); var s = 1.3f + (float)rng.NextDouble() * .7f;
                Vector3 V(float x, float y, float z) => new Vector3(x, y, z) * s;
                foreach (var e in new[] { -1f, 1f })
                {
                    m.Tri(col, V(0, 0, .35f), V(0, 0, -.35f), V(e * 1.1f, .35f, -.05f), V(0, -1f, 0)); m.Tri(paperCream, V(0, 0, .35f), V(0, 0, -.35f), V(e * 1.1f, .35f, -.05f), V(0, 1f, 0));
                    m.Tri(col, V(0, 0, .3f), V(0, .15f, 0), V(0, .55f, .95f), V(e, 0, 0)); m.Tri(col, V(0, 0, -.3f), V(0, .15f, 0), V(0, .5f, -.85f), V(e, 0, 0));
                    m.Tri(col, V(0, .55f, .95f), V(0, .45f, .9f), V(0, .42f, 1.15f), V(e, 0, 0));
                }
                var go = m.BuildSingle("Origami crane", life.transform, false);
                life.cranes.Add(go.transform); life.craneWays.Add(new Vector4(40f + (float)rng.NextDouble() * 110f, 14f + (float)rng.NextDouble() * 14f, (.08f + (float)rng.NextDouble() * .06f) * (rng.Next(2) == 0 ? 1f : -1f), (float)rng.NextDouble() * 6.3f));
            }
        }
    }
}
