using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The great market and the old stone (part of AfroKit, 2026-11-16). The market spreads right across the land:
    /// besides Market Road, Cloth Street (a second bolt of print) runs east from the roundabout with cross streets off it,
    /// every street lined with stalls and roofed here and there with printed cloth on poles. Through all of it, and out
    /// in the open ground, stands the old stone, a thousand years older than the market and walling nothing off: broken
    /// lengths of curving dry-stone wall with a zigzag band, a conical tower, half-ruined rings, standing stones, carved
    /// birds on pillars. The cloth weaves in and out of it: printed cloths hung down the old walls, banners tied between
    /// standing stones, streamers from the birds' pillars, strings of printed flags run from stone to pole.
    public partial class AfroKit
    {
        Material oldStone, oldStoneDark, oldStoneLight;
        readonly List<(Vector3 a, Vector3 b)> streets = new List<(Vector3, Vector3)>();   // the market streets' centre lines
        float clothE;   // how far Cloth Street runs east

        bool InHeart(float x, float z, float pad = 0f) => true;   // (nothing is walled off here)

        void StoneMaterials()
        {
            oldStone = WorldMaterials.Surface(new Color(.56f, .54f, .5f), Surface.Stone); oldStoneDark = WorldMaterials.Surface(new Color(.3f, .29f, .28f), Surface.Stone); oldStoneLight = WorldMaterials.Surface(new Color(.7f, .68f, .63f), Surface.Stone);
        }

        bool NearStreet(Vector3 p, float within) { foreach (var (a, b) in streets) if (ShardCaveMap.Seg(new Vector2(p.x, p.z), new Vector2(a.x, a.z), new Vector2(b.x, b.z)) < within) return true; return false; }

        // ---------------------------------------------------------------- the market grows
        /// Cloth Street east from the roundabout, cross streets off it: laid before the lanes, so lanes can join them.
        void MarketStreets(LandContext ctx, MeshKit mk)
        {
            StoneMaterials(); streets.Clear(); var up = Vector3.up;
            clothE = 30f; while (clothE < 320f) { var q = Wp(0, clothE + 24f); if (!ctx.IsLand(q.x, q.z, 18f) || ctx.Ground(q.x, q.z) < waterY + 2f) break; clothE += 6f; }
            if (clothE < 60f) return;
            var pts = new List<Vector3>(); for (var d = 24f; d <= clothE; d += 2f) pts.Add(G(ctx, 0, d));
            Strip(mk, pts, PrintWorld(9, 7f), RoadW, .14f, ctx); foreach (var p in pts) Remember(p, 3);
            foreach (var e in new[] { -1f, 1f }) { var edge = new List<Vector3>(); foreach (var p in pts) edge.Add(ctx.OnGround(p.x + U.x * e * (RoadW / 2f + .3f), p.z + U.z * e * (RoadW / 2f + .3f))); Strip(mk, edge, earth, .9f, .13f, ctx); }
            streets.Add((Wp(0, 24f), Wp(0, clothE)));
            // cross streets, north and south, as far as the land and the artists allow
            for (var d = 80f; d < clothE - 20f; d += 70f)
                foreach (var e in new[] { -1f, 1f })
                {
                    var cp = new List<Vector3>();
                    for (var s = RoadW / 2f + .5f; s <= 110f; s += 2f) { var q = Wp(e * s, d); if (!ctx.IsLand(q.x, q.z, 10f) || InFootprint(ctx, q.x, q.z, 3f) || ctx.Ground(q.x, q.z) < waterY + 2f) break; cp.Add(ctx.OnGround(q.x, q.z)); }
                    if (cp.Count < 10) continue;
                    EarthRoad(mk, cp, 5f, .14f, ctx); foreach (var p in cp) Remember(p, 3); streets.Add((cp[0], cp[cp.Count - 1]));
                }
            // printed cloth roofs over Cloth Street on poles, and strings of bulbs between
            var n = 0;
            for (var d = 34f; d <= clothE - 8f; d += 15f, n++)
            {
                var a = G(ctx, RoadW / 2f + .8f, d); var b = G(ctx, -(RoadW / 2f + .8f), d); var a2 = G(ctx, RoadW / 2f + .8f, d + 7f); var b2 = G(ctx, -(RoadW / 2f + .8f), d + 7f);
                foreach (var g in new[] { a, b, a2, b2 }) { mk.Prism(woodM, g - up * .3f, up, .08f, 5.3f, 5, .8f); ArtistBits.PostCollider(ctx.Root, g, 5f, .1f); }
                if (n % 2 == 0) Canopy(mk, a + up * 4.9f, b + up * 4.9f, a2 + up * 4.9f, b2 + up * 4.9f, n * 7 + 11);
                else { BulbString(mk, a + up * 4.8f, b + up * 4.8f, n); BulbString(mk, a2 + up * 4.8f, b2 + up * 4.8f, n + 1); }
                if (n % 2 == 0) ArtistBits.PointLight(ctx.Root, (a + b2) / 2f + up * 3.4f, new Color(1f, .68f, .4f), 16f, 2f);
            }
            Shot(ctx, "down cloth street", G(ctx, 0, 30f) + up * 2.4f, G(ctx, 0, Mathf.Min(clothE, 140f)) + up * 2.2f);
        }

        /// A printed cloth roof between four poles, sagging in the middle.
        void Canopy(MeshKit mk, Vector3 a, Vector3 b, Vector3 a2, Vector3 b2, int k)
        {
            var m = PrintM(k); const int c = 6, r = 3;
            Vector3 P(int i, int j) { var u = i / (float)c; var v = j / (float)r; var p = Vector3.Lerp(Vector3.Lerp(a, b, u), Vector3.Lerp(a2, b2, u), v); return p - Vector3.up * (Mathf.Sin(u * Mathf.PI) * .9f + Mathf.Sin(v * Mathf.PI) * .3f); }
            var w = Vector3.Distance(a, b); var l = Vector3.Distance(a, a2); Vector2 UV(int i, int j) => new Vector2(w * i / c / 3f, l * j / r / 3f);
            for (var i = 0; i < c; i++) for (var j = 0; j < r; j++) { Vector3 p0 = P(i, j), p1 = P(i + 1, j), p2 = P(i + 1, j + 1), p3 = P(i, j + 1); var below = (p0 + p2) / 2f - Vector3.up * 3f; mk.Tri(m, p0, p1, p2, below, UV(i, j), UV(i + 1, j), UV(i + 1, j + 1)); mk.Tri(m, p0, p2, p3, below, UV(i, j), UV(i + 1, j + 1), UV(i, j + 1)); }
        }

        /// Stalls down both sides of every new market street, after the lanes are laid (so none stands in one).
        void MarketStalls(LandContext ctx, MeshKit mk)
        {
            var i = 200; var built = 0;
            foreach (var (a, b) in streets)
            {
                var dir = b - a; dir.y = 0; var len = dir.magnitude; if (len < 10f) continue; dir /= len; var side = Vector3.Cross(Vector3.up, dir); var half = (a - b).sqrMagnitude > 0 && streets.IndexOf((a, b)) == 0 ? RoadW / 2f : 2.5f;
                foreach (var e in new[] { 1f, -1f })
                    for (var d = 6f; d <= len - 4f; d += 6.6f, i++)
                    {
                        var p = a + dir * d + side * e * (half + 2.5f); p.y = 0;
                        if (!ctx.IsLand(p.x, p.z, 4f) || InFootprint(ctx, p.x, p.z, 2.6f) || LaneNear(p.x, p.z, 4.4f) || Fall(ctx, p, 1.7f) > .9f) continue;
                        var crossing = false; foreach (var (a2, b2) in streets) if ((a2 != a || b2 != b) && ShardCaveMap.Seg(new Vector2(p.x, p.z), new Vector2(a2.x, a2.z), new Vector2(b2.x, b2.z)) < 6f) crossing = true; if (crossing) continue;
                        Stall(ctx, mk, new PlaceFrame(ctx.OnGround(p.x, p.z), -side * e), i); built++;
                    }
            }
            Debug.Log($"The Afrobeat Coast, the great market: Cloth Street {clothE:0} m, {streets.Count - 1} cross streets, {built} more stalls");
        }

        // ---------------------------------------------------------------- the old stone, everywhere, walling nothing off
        void OldStone(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var rng = new System.Random(1450); int frags = 0, towers = 0, rings = 0, stones = 0, birds = 0, cloths = 0; const float cell = 34f; var up = Vector3.up;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble(); var cl = (float)rng.NextDouble(); var yaw = (float)rng.NextDouble() * 360f;
                var g = ctx.OnGround(px, pz);
                if (!ctx.IsLand(px, pz, 8f) || ctx.Ground(px, pz) < waterY + 1.6f || NearPath(px, pz, 8f) || NearStreet(g, 9f) || InFootprint(ctx, px, pz, 5f) || InNoGrass(px, pz, 3f)) continue;
                var face = Quaternion.Euler(0, yaw, 0) * Vector3.forward; var right = Vector3.Cross(up, face);
                if (roll < .4f)
                {   // a broken length of the old curving wall: two to four stretches, each a different height, the zigzag band where it still stands tall
                    var cnt = 2 + rng.Next(3); var bend = ((float)rng.NextDouble() - .5f) * 30f; var p = g - right * cnt * 2.3f; var d = right; var tallest = 0f; Vector3 tallAt = g;
                    for (var c = 0; c < cnt; c++)
                    {
                        var q = p + d * 2.3f; q.y = ctx.Ground(q.x, q.z); var h = 2f + (float)rng.NextDouble() * 4.5f; var rot = Quaternion.LookRotation(d);   // its length runs along the wall
                        mk.Box(oldStone, q + up * (h / 2f - .7f), new Vector3(2f, h + 1.4f, 4.6f), rot); mk.Box(oldStoneLight, q + up * (h + .05f), new Vector3(2.05f, .18f, 4.6f), rot); solid.Box(oldStone, q + up * (h / 2f), new Vector3(2f, h + .4f, 4.6f), rot);
                        if (h > 3.5f) foreach (var e in new[] { 1f, -1f }) for (var z2 = -1; z2 <= 1; z2++) foreach (var sg in new[] { -1f, 1f }) mk.Box(oldStoneDark, q + up * (h - .8f) + d * (z2 * 1.3f + sg * .32f) + Vector3.Cross(d, up) * e * 1.02f, new Vector3(.06f, .18f, .75f), rot * Quaternion.Euler(sg * 35f, 0, 0));
                        if (h > tallest) { tallest = h; tallAt = q; }
                        p = q + d * 2.3f; d = Quaternion.Euler(0, bend / cnt, 0) * d;
                    }
                    // the cloth: a great printed cloth hung down its face, or a string of printed flags run off to a pole
                    if (cl < .55f && tallest > 3f) { var n2 = Vector3.Cross(d, up).normalized; if (rng.Next(2) == 0) n2 = -n2; var top = tallAt + up * (tallest + .3f) + n2 * 1.2f; mk.Prism(woodM, top - d * 2f, d, .07f, 4f, 5); HangingCloth(top, n2, 3.6f, Mathf.Min(tallest - .3f, 5f), (int)(px * 3 + pz)); cloths++; }
                    else if (cl < .85f) { var pole = tallAt + Quaternion.Euler(0, 90f, 0) * d * 9f; pole.y = ctx.Ground(pole.x, pole.z); if (!NearPath(pole.x, pole.z, 3f)) { mk.Prism(woodM, pole - up * .3f, up, .08f, 5.6f, 5); ArtistBits.PostCollider(ctx.Root, pole, 5f, .1f); Bunting(mk, tallAt + up * tallest, pole + up * 5.2f, (int)(px + pz * 7)); cloths++; } }
                    frags++;
                }
                else if (roll < .62f)
                {   // standing stones; a printed banner tied between two of them now and then
                    var cnt = 2 + rng.Next(3); var at = new List<(Vector3 q, float h)>();
                    for (var c = 0; c < cnt; c++) { var q = g + right * (c - cnt / 2f) * 3.2f + face * ((float)rng.NextDouble() - .5f) * 3f; q.y = ctx.Ground(q.x, q.z); var h = 2.6f + (float)rng.NextDouble() * 3.6f; mk.Box(oldStone, q + up * (h / 2f - .5f), new Vector3(1f + (float)rng.NextDouble() * .5f, h + 1f, .7f), Quaternion.LookRotation(face) * Quaternion.Euler(((float)rng.NextDouble() - .5f) * 7f, 0, ((float)rng.NextDouble() - .5f) * 7f)); ArtistBits.PostCollider(ctx.Root, q, h, .6f); at.Add((q, h)); }
                    if (cl < .5f && at.Count >= 2) { var (q0, h0) = at[0]; var (q1, h1) = at[1]; var top = (q0 + q1) / 2f + up * (Mathf.Min(h0, h1) - .2f); HangingCloth(top, face, Vector3.Distance(q0, q1) - 1f, Mathf.Min(h0, h1) - .6f, (int)(pz * 5 + px)); cloths++; }
                    stones++;
                }
                else if (roll < .76f)
                {   // a carved stone bird on a tall pillar, looking out to sea, a long printed streamer hanging from it
                    var hh = 5f + s * 2f; mk.Prism(oldStone, g - up * .4f, up, .45f, hh + .4f, 8, .9f); var b = g + up * hh; var fq = Quaternion.LookRotation(-R);
                    mk.Box(oldStoneLight, b + up * .6f, new Vector3(.5f, 1.2f, .7f), fq * Quaternion.Euler(-12f, 0, 0)); mk.Box(oldStoneLight, b + up * 1.35f + fq * Vector3.forward * .3f, new Vector3(.36f, .36f, .5f), fq); mk.Cone(oldStoneDark, b + up * 1.35f + fq * Vector3.forward * .6f, .12f, .3f, 4);
                    foreach (var e in new[] { -1f, 1f }) mk.Box(oldStone, b + up * .7f + fq * Vector3.right * e * .32f, new Vector3(.12f, 1f, .6f), fq * Quaternion.Euler(0, 0, e * 8f));
                    ArtistBits.PostCollider(ctx.Root, g, hh + 1.6f, .5f);
                    if (cl < .7f) { HangingCloth(g + up * (hh - .3f) + face * .5f, face, 1f, hh - 1.2f, (int)(px * 11)); cloths++; }
                    birds++;
                }
                else if (roll < .9f || towers >= 2)
                {   // a half-ruined ring of wall with a way in
                    var Rr = 6f + s * 6f; var n = Mathf.CeilToInt(2f * Mathf.PI * Rr / 4.4f); var gap = rng.Next(n);
                    for (var c = 0; c < n; c++)
                    {
                        var keep = (float)rng.NextDouble(); if (c == gap || keep < .3f) continue;
                        var a = c * Mathf.PI * 2f / n; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var q = g + d * Rr; q.y = ctx.Ground(q.x, q.z); var hh = 1.4f + keep * 2.6f; var rot = Quaternion.LookRotation(Vector3.Cross(up, d));
                        mk.Box(oldStone, q + up * (hh / 2f - .6f), new Vector3(1.5f, hh + 1.2f, 4.6f), rot); solid.Box(oldStone, q + up * (hh / 2f), new Vector3(1.5f, hh + .4f, 4.6f), rot);
                        if (cl < .45f && c % 4 == 1 && hh > 2.6f) { HangingCloth(q + up * (hh + .2f) - d * 1f, -d, 3.4f, hh - .2f, c + (int)px); cloths++; }
                    }
                    rings++;
                }
                else
                {   // the conical tower, banded in stone and wrapped in a printed sash
                    var h = 11f + s * 4f; mk.Prism(oldStone, g - up * .6f, up, 3.4f, h + .6f, 20, .55f); mk.Prism(oldStoneLight, g + up * h, up, 1.9f, .3f, 20);
                    for (var c = 0; c < 16; c++) { var a = c * 22.5f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var rr = Mathf.Lerp(3.4f, 1.9f, (h - 2f) / h) + .05f; mk.Box(oldStoneDark, g + up * (h - 2f) + d * rr, new Vector3(.08f, .2f, .9f), Quaternion.LookRotation(Vector3.Cross(up, d)) * Quaternion.Euler(0, 0, c % 2 == 0 ? 35f : -35f)); }
                    mk.Prism(PrintM((int)px), g + up * (h * .45f), up, Mathf.Lerp(3.4f, 1.9f, .45f) + .08f, 1.4f, 20, .96f);
                    ArtistBits.PostCollider(ctx.Root, g, h, 3.4f); towers++;
                }
            }
            Debug.Log($"The Afrobeat Coast, the old stone: {frags} broken walls, {stones} groups of standing stones, {birds} stone birds, {rings} ruined rings, {towers} conical towers; {cloths} cloths and flag strings woven through");
        }

        /// A string of printed flags from a to b, sagging.
        void Bunting(MeshKit mk, Vector3 a, Vector3 b, int k)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(4, Mathf.RoundToInt(len / .8f)); var sag = len * .08f; var prev = a; var side = Vector3.Cross(Vector3.up, (b - a).normalized);
            for (var i = 1; i <= n; i++)
            {
                var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag; mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3);
                if (i < n) { var m = PrintM(k + i); var c = (prev + p) / 2f; mk.Tri(m, prev, p, c - Vector3.up * .55f, c + side); mk.Tri(m, prev, p, c - Vector3.up * .55f, c - side); }
                prev = p;
            }
        }
    }
}
