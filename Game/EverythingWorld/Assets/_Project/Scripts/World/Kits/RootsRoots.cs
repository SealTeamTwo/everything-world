using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Roots Bay's ground (part of RootsKit): there is no island. The tree of life stands in the middle and the
    /// land is its roots, coming out of a warm shallow lagoon you can wade anywhere in.
    /// - Round the trunk the roots join in a broad collar. From it they run out to the land's edges, each one a long
    ///   ridge with a flat mossy top wide enough to live on and bark sides sloping into the water, forking as they go
    ///   and dipping under at their tips.
    /// - The Great Root carries the Yard (where you arrive, at the trunk's foot) and Bay Road down to the Sound System
    ///   on the sand at its end. A root runs to each bridge to the neighbouring lands.
    /// - Every artist stands on a burl: a round swelling on a root's side.
    /// - The town is in the roots: painted houses along them, huts on stilts beside them, plank bridges slung from
    ///   root to root, lanterns everywhere.
    public partial class RootsKit
    {
        class RootLine
        {
            public readonly List<Vector2> p = new List<Vector2>(); public readonly List<float> top = new List<float>(), half = new List<float>();
            public bool great, bridge, fork; public float bearing;
            public float Len { get { var l = 0f; for (var i = 0; i + 1 < p.Count; i++) l += Vector2.Distance(p[i], p[i + 1]); return l; } }
        }
        readonly List<RootLine> lines = new List<RootLine>(); Vector2 tree2;
        public const float CollarR = 118f, CollarY = 13f, Shallow = -.45f, RingR = 84f, TrunkR = 62f; float YardOut = 122f;
        static float FlankOf(float top) => Mathf.Max(9f, (top - Shallow) * 1.35f);
        static Vector2 Rot(Vector2 v, float deg) { var a = deg * Mathf.Deg2Rad; float c = Mathf.Cos(a), s = Mathf.Sin(a); return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c); }
        static float Shape(float d, float top, float half) { if (d <= half) return top; var t = (d - half) / FlankOf(top); if (t >= 1f) return Shallow; return Mathf.Lerp(top, Shallow, t * .7f + .3f * t * t * (3f - 2f * t)); }

        /// The nearest point of a root's spine: how far, and the root's top and half-width there.
        static float Near(RootLine l, Vector2 q, out float top, out float half, out Vector2 on)
        {
            var best = float.MaxValue; top = 0; half = 0; on = q;
            for (var i = 0; i + 1 < l.p.Count; i++)
            {
                Vector2 a = l.p[i], b = l.p[i + 1], ab = b - a; var t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(.01f, ab.sqrMagnitude)); var c = a + ab * t; var d = (q - c).sqrMagnitude;
                if (d < best) { best = d; top = Mathf.Lerp(l.top[i], l.top[i + 1], t); half = Mathf.Lerp(l.half[i], l.half[i + 1], t); on = c; }
            }
            return Mathf.Sqrt(best);
        }

        float RootsAt(Vector2 q)
        {
            var h = Shape(Vector2.Distance(q, tree2), CollarY, CollarR);
            foreach (var l in lines)
            {
                var d = Near(l, q, out var top, out var half, out var on); if (d >= half + 40f) continue;
                // a root is round on top and knuckled along its length, not a causeway (the Great Root least, for the town on it)
                var k = Mathf.Clamp01(top / 3f) * (l.great ? .45f : 1f); var u = Mathf.Clamp01(d / half);
                var crown = k * (Mathf.Min(2.4f, half * .13f) * (1f - u * u) + (l.great ? 0f : .8f * Mathf.Sin(on.x * .083f + on.y * .061f)));
                h = Mathf.Max(h, Shape(d, top, half) + (d <= half ? crown : crown * Mathf.Clamp01(1f - (d - half) / 6f)));
            }
            return h;
        }

        RootLine Run(Vector2 from, Vector2 to, float top0, float top1, float half0, float half1, float wiggle, float phase, bool dip)
        {
            var l = new RootLine(); var len = Vector2.Distance(from, to); var dir = (to - from) / Mathf.Max(.01f, len); var perp = new Vector2(-dir.y, dir.x); var n = Mathf.Max(2, Mathf.CeilToInt(len / 12f));
            for (var i = 0; i <= n; i++)
            {
                var t = i / (float)n; var left = len * (1f - t);
                l.p.Add(from + dir * (len * t) + perp * (Mathf.Sin(t * Mathf.PI * 2.3f + phase) * wiggle * Mathf.Sin(t * Mathf.PI)));
                var top = Mathf.Lerp(top0, top1, Mathf.Pow(t, .85f)); if (dip && left < 24f) top = Mathf.Lerp(Shallow - .25f, top, left / 24f);
                l.top.Add(top); l.half.Add(Mathf.Lerp(half0, half1, t));
            }
            return l;
        }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel; lines.Clear();
            bool In(Vector2 p) => data.IslandAt(p.x, p.y) == isl;
            bool Room(Vector2 p, float m) { if (!In(p)) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (!In(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * m)) return false; } return true; }
            // the tree stands in the middle of the land; you arrive at its foot, on the side the land's own heart is on
            double sx = 0, sz = 0; var cells = 0;
            for (var r = 0; r < data.Nz; r++) for (var c = 0; c < data.Nx; c++) if (data.islandIds[r * data.Nx + c] == index) { sx += c * data.Step; sz += data.layout.depth - r * data.Step; cells++; }
            var mid = new Vector2((float)(sx / cells), (float)(sz / cells));
            D2 = heart2 - mid; D2 = D2.magnitude < 20f ? new Vector2(0, -1) : D2.normalized; S2 = new Vector2(-D2.y, D2.x);
            YardOut = Mathf.Clamp(Vector2.Distance(heart2, mid) + 45f, 118f, 150f); tree2 = heart2 - D2 * YardOut;
            roadL = 60f; while (roadL < 264f && In(heart2 + D2 * (roadL + 52f))) roadL += 6f;
            beach2 = heart2 + D2 * (roadL + 12f); plazaY = CollarY;

            // ---- the Great Root: the Yard, Bay Road, the sand at its end
            var great = new RootLine { great = true };
            for (var d = -44f; ; d += 12f)
            {
                var dd = Mathf.Min(d, roadL + 12f); great.p.Add(heart2 + D2 * dd);
                great.top.Add(dd <= 20f ? CollarY : Mathf.Lerp(CollarY, waterY + 1.7f, Mathf.Clamp01((dd - 20f) / (roadL - 34f)))); great.half.Add(dd <= 34f ? 30f : 24f);
                if (dd >= roadL + 12f) break;
            }
            lines.Add(great);
            var bearings = new List<float> { 0f };
            float Bearing(Vector2 v) => Vector2.SignedAngle(D2, v);
            void Fork(RootLine parent, int seed)
            {
                foreach (var (at, turn) in new[] { (.36f, 28f), (.64f, -30f) })
                {
                    var i = Mathf.RoundToInt((parent.p.Count - 1) * at); if (i < 2 || i + 2 >= parent.p.Count) continue;
                    var start = parent.p[i]; var bdir = Rot((parent.p[i + 1] - parent.p[i - 1]).normalized, turn); var max = parent.Len * .5f; var blen = 40f;
                    while (blen < max && In(start + bdir * (blen + 30f))) blen += 8f;
                    if (blen < 70f) continue;
                    var end = start + bdir * blen; var clash = false; foreach (var o in lines) if (o != parent && Near(o, end, out _, out var oh, out _) < oh + 34f) clash = true; if (clash) continue;
                    var f = Run(start, end, parent.top[i], waterY + 2.2f, parent.half[i] * .8f, 7f, 8f, seed * 1.7f + at * 9f, true); f.fork = true; lines.Add(f);
                }
            }
            // ---- a root to each bridge
            foreach (var b in data.layout.bridges)
                foreach (var e in new[] { new Vector2(b.ax, b.az), new Vector2(b.bx, b.bz) })
                {
                    var toT = tree2 - e; var far = toT.magnitude; toT /= Mathf.Max(.01f, far); if (far < 200f || far > 900f || (!In(e) && !In(e + toT * 16f))) continue;
                    var bear = Bearing(-toT); var clash = false; foreach (var o in bearings) if (Mathf.Abs(Mathf.DeltaAngle(o, bear)) < 14f) clash = true;
                    var l = Run(tree2 - toT * 100f, e, CollarY, Mathf.Max(waterY + 2.2f, data.HeightAt(e.x, e.y)), 16f, 11f, clash ? 0f : 14f, bear, false); l.bridge = true; l.bearing = bear; lines.Add(l); bearings.Add(bear);
                }
            // ---- and roots wherever there is a gap between those, out to the land's edge
            for (var guard = 0; guard < 12; guard++)
            {
                float bestA = 0f, bestGap = 0f;
                for (var a = -180f; a < 180f; a += 4f) { var gap = 360f; foreach (var o in bearings) gap = Mathf.Min(gap, Mathf.Abs(Mathf.DeltaAngle(o, a))); if (gap > bestGap) { bestGap = gap; bestA = a; } }
                if (bestGap < 33f) break;
                bearings.Add(bestA); var dir = Rot(D2, bestA); var len = 140f; while (len < 540f && In(tree2 + dir * (len + 34f))) len += 8f;
                if (len < 156f) continue;
                var l = Run(tree2 + dir * 100f, tree2 + dir * len, CollarY, waterY + 2.4f, 16f, 8.5f, Mathf.Min(20f, len * .07f), bestA * .11f, true); l.bearing = bestA; lines.Add(l);
            }
            var mains = lines.Count; for (var i = 1; i < mains; i++) Fork(lines[i], i);

            // ---- the ground itself: roots, and warm shallows between them
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var c = 0; c < data.Nx; c++)
                {
                    var i = r * data.Nx + c; if (data.islandIds[i] != index) continue;
                    var h = RootsAt(new Vector2(c * data.Step, z));
                    data.heights[i] = h > Shallow + .01f ? h : Mathf.Min(data.heights[i], Shallow);
                }
            }
            Level(data, index, heart2, 34f, 34f + FlankOf(CollarY), plazaY);
            Level(data, index, beach2, 22f, 44f, waterY + 1.7f);

            // ---- the artists, each on a burl on a root's side
            var mine = new List<IslandWorldData.Artist>(); foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId) mine.Add(a);
            var placed = new List<IslandWorldData.Artist>(); var levelOf = new Dictionary<string, float>();
            bool ClearOf(Vector2 p, float room, RootLine own)
            {
                foreach (var l in lines) { if (l == own || l.great) continue; if (Near(l, p, out _, out _, out _) < room) return false; }
                foreach (var o in placed) if (Vector2.Distance(p, new Vector2(o.x, o.z)) < room + Foot(o) + 10f) return false;
                return true;
            }
            void Put(string name, float d, float s)
            {
                foreach (var a in mine)
                {
                    if (a.name != name || placed.Contains(a)) continue;
                    foreach (var side in new[] { 1f, -1f })
                    {
                        var p = P2(d, s * side); if (!Room(p, Foot(a) + 8f) || !ClearOf(p, Foot(a) + 5f, null)) continue;
                        Near(great, p, out var top, out _, out _); if (Vector2.Distance(p, tree2) < CollarR) top = CollarY;
                        a.x = p.x; a.z = p.y; placed.Add(a); levelOf[a.name] = top; return;
                    }
                }
            }
            // the Sound System where the Great Root meets the sand; the dance hall and the grey street either side of the Yard
            Put("Bob Marley & The Wailers", roadL - 12f, -40f); Put("Toots & The Maytals", -4f, 50f); Put("The Specials", -4f, -50f);
            Put("Jimmy Cliff", 62f, 38f); Put("Desmond Dekker & The Aces", 66f, -38f);
            if (roadL > 170f) { Put("Peter Tosh", 122f, 38f); Put("Burning Spear", 126f, -38f); }
            Put("Lee \"Scratch\" Perry", -40f, 78f);
            // burls out on the other roots, the nearest to the Yard first
            const float R = 13f; var slots = new List<(Vector2 p, float top, RootLine l)>();
            for (var li = 1; li < lines.Count; li++)
            {
                var l = lines[li];
                foreach (var (f, sd) in l.fork ? new[] { (.62f, 1f) } : new[] { (.36f, -1f), (.7f, 1f) })
                {
                    var i = Mathf.Clamp(Mathf.RoundToInt((l.p.Count - 1) * f), 1, l.p.Count - 2); var t = (l.p[i + 1] - l.p[i - 1]).normalized; var nrm = new Vector2(-t.y, t.x) * sd;
                    var p = l.p[i] + nrm * (l.half[i] + R * .55f); if (!Room(p, R + 8f) || Vector2.Distance(p, tree2) < CollarR + R || Vector2.Distance(p, beach2) < 36f + R || Vector2.Distance(p, heart2) < 40f + R || !ClearOf(p, R + 1f, l)) continue;
                    var near = false; foreach (var b in data.layout.bridges) if (Vector2.Distance(p, new Vector2(b.ax, b.az)) < 44f || Vector2.Distance(p, new Vector2(b.bx, b.bz)) < 44f) near = true; if (near) continue;
                    foreach (var o in slots) if (Vector2.Distance(o.p, p) < R * 2f + 14f) near = true; if (near) continue;
                    slots.Add((p, l.top[i], l));
                }
            }
            slots.Sort((a, b) => Vector2.Distance(a.p, heart2).CompareTo(Vector2.Distance(b.p, heart2)));
            var rest = new List<IslandWorldData.Artist>(); foreach (var a in mine) if (!placed.Contains(a)) rest.Add(a);
            rest.Sort((a, b) => b.songs != a.songs ? b.songs.CompareTo(a.songs) : string.CompareOrdinal(a.name, b.name));
            var free = new List<(Vector2 p, float top, RootLine l)>(slots);
            void Take(IslandWorldData.Artist a, int k) { a.x = free[k].p.x; a.z = free[k].p.y; levelOf[a.name] = free[k].top; placed.Add(a); free.RemoveAt(k); }
            // whoever has a settled place on a burl keeps it (ArtistPlaces.txt), so nobody moves when the playlist grows
            foreach (var a in rest.ToArray()) { if (!ArtistPlaces.TryGet(a, out var at, out _)) continue; for (var k = 0; k < free.Count; k++) if (Vector2.Distance(free[k].p, at) < 14f) { Take(a, k); rest.Remove(a); break; } }
            foreach (var a in rest) { if (free.Count == 0) { Debug.LogWarning($"The Roots Bay: no burl left for {a.name}"); continue; } Take(a, 0); }
            foreach (var a in mine)
            {
                a.r = Mathf.Max(a.r, Foot(a) + 6f); if (!levelOf.TryGetValue(a.name, out var lv)) continue;
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 3f, Foot(a) + 3f + FlankOf(lv), lv);
            }
            Debug.Log($"The Roots Bay: the tree stands {Vector2.Distance(tree2, mid):0} m from the middle of the land; {mains} roots and {lines.Count - mains} forks, Bay Road {roadL:0} m, {slots.Count} burls for {mine.Count - (placed.Count - (slots.Count - free.Count))} named and {slots.Count - free.Count} other artists");
        }

        // ---------------------------------------------------------------- the roots, dressed
        Vector3 On(LandContext ctx, Vector2 p, float lift = 0f) => ctx.OnGround(p.x, p.y, lift);

        /// The worn paths along the roots' spines and round the trunk, which the lanes to the artists then join; and
        /// smaller roots arching off the flanks into the water all the way along.
        void RootPaths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var T = new Vector3(tree2.x, 0, tree2.y);
            // round the trunk
            var ring = new List<Vector3>(); for (var k = 0; k <= 60; k++) { var d = Quaternion.Euler(0, k * 6f, 0) * D; ring.Add(ctx.OnGround(T.x + d.x * RingR, T.z + d.z * RingR)); }
            Strip(mk, ring, earthM, PathW, .13f, ctx); foreach (var p in ring) Remember(p, 1);
            // from the ring out to the Yard
            var toYard = new List<Vector3>(); for (var d = RingR + 1.6f; d <= YardOut - 19.5f; d += 2f) toYard.Add(ctx.OnGround(T.x + D.x * d, T.z + D.z * d)); if (toYard.Count > 1) { Strip(mk, toYard, earthM, PathW, .13f, ctx); foreach (var p in toYard) Remember(p, 1); }
            for (var li = 1; li < lines.Count; li++)
            {
                var l = lines[li]; var pts = new List<Vector3>();
                if (!l.fork) { var dir = new Vector3(l.p[0].x - T.x, 0, l.p[0].y - T.z).normalized; for (var d = RingR + 1.6f; d < 99f; d += 2f) pts.Add(ctx.OnGround(T.x + dir.x * d, T.z + dir.z * d)); }
                for (var i = l.fork ? 0 : 0; i < l.p.Count; i++)
                {
                    var q = l.p[i]; if (l.fork && i == 0) { var t = (l.p[1] - l.p[0]).normalized; q += t * (PathW / 2f + .2f); }
                    if (ctx.Ground(q.x, q.y) < waterY + 1.5f) break; pts.Add(On(ctx, q));
                    if (i + 1 < l.p.Count) { var m = (l.p[i] + l.p[i + 1]) / 2f; if (ctx.Ground(m.x, m.y) >= waterY + 1.5f) pts.Add(On(ctx, m)); }
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, earthM, PathW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            }
            // smaller roots off the flanks, arching down into the lagoon
            var rng = new System.Random(1976); var props = 0;
            foreach (var l in lines)
                for (var i = 2; i + 1 < l.p.Count; i++)
                {
                    var t = (l.p[i + 1] - l.p[i - 1]).normalized; var sd = (i + (l.fork ? 1 : 0)) % 2 == 0 ? 1f : -1f; var nrm = new Vector2(-t.y, t.x) * sd; var top = l.top[i]; if (top < 1.2f) continue;
                    var a2 = l.p[i] + nrm * (l.half[i] - 1.5f); var reach = FlankOf(top) + 5f + (float)rng.NextDouble() * 10f; var b2 = a2 + nrm * reach + t * (((float)rng.NextDouble() - .5f) * 12f);
                    var a = On(ctx, a2, -.6f); var b = new Vector3(b2.x, Mathf.Min(ctx.Ground(b2.x, b2.y), waterY) - 1f, b2.y); if (ctx.Ground(b2.x, b2.y) > waterY + .6f || InFootprint(ctx, a2.x, a2.y, 3f) || InFootprint(ctx, b2.x, b2.y, 2f) || InNoGrass(a2.x, a2.y, 2f)) continue;
                    var rise = 1.5f + top * .25f + (float)rng.NextDouble() * 2f; var rad = .5f + top * .05f + (float)rng.NextDouble() * .4f; const int N = 7; var prev = a;
                    for (var q = 1; q <= N; q++) { var u = q / (float)N; var p = Vector3.Lerp(a, b, u) + up * (Mathf.Sin(u * Mathf.PI) * rise); mk.Prism(q % 2 == 0 ? barkGlow : barkDark, prev, p - prev, rad * Mathf.Lerp(1f, .5f, (q - 1) / (float)N), (p - prev).magnitude + rad * .3f, 6, Mathf.Lerp(1f, .5f, q / (float)N) / Mathf.Lerp(1f, .5f, (q - 1) / (float)N), q * 20f, false); prev = p; }
                    props++;
                }
            // and lesser roots lying along the edges of every root's top, half sunk in it, weaving, with gaps to step through
            var ropes = 0;
            foreach (var l in lines)
                foreach (var sd in new[] { -1f, 1f })
                {
                    Vector3? last = null; var run = 0;
                    for (var i = 1; i + 1 < l.p.Count; i++)
                    for (var h2 = 0; h2 < 2; h2++)
                    {
                        var c2 = h2 == 0 ? l.p[i] : (l.p[i] + l.p[i + 1]) / 2f; var t = (l.p[i + 1] - l.p[i - 1]).normalized; var nrm = new Vector2(-t.y, t.x) * sd; var hf = h2 == 0 ? l.half[i] : (l.half[i] + l.half[i + 1]) / 2f; var top = l.top[i];
                        var q2 = c2 + nrm * (hf - 1.2f + 1.6f * Mathf.Sin(i * 1.3f + sd)); var rad = Mathf.Clamp(.6f + top * .09f, .7f, 1.7f); run++;
                        var skip = top < 1.4f || run % 9 >= 7 || InFootprint(ctx, q2.x, q2.y, rad + 1.5f) || NearPath(q2.x, q2.y, rad + 2.2f) || InNoGrass(q2.x, q2.y, rad + .5f) || (l.great && Vector2.Distance(q2, heart2) < 34f) || Vector2.Distance(q2, beach2) < 30f;
                        if (skip) { if (last.HasValue) { mk.Blob(barkDark, last.Value, new Vector3(rad, rad * .8f, rad), rng, .2f); } last = null; continue; }
                        var p = On(ctx, q2, rad * .15f);
                        if (last.HasValue) { var a = last.Value; mk.Prism(run % 2 == 0 ? barkGlow : barkDark, a, p - a, rad, (p - a).magnitude + rad * .3f, 6, 1f, run * 25f, false); BoxCol(ctx.Root, (a + p) / 2f, new Vector3(rad * 1.6f, rad * 1.6f, (p - a).magnitude), Quaternion.LookRotation((p - a).normalized)); ropes++; }
                        last = p;
                    }
                }
            Debug.Log($"The Roots Bay: {props} smaller roots arch off the flanks into the lagoon, {ropes} lengths of lesser root lie along the edges");
        }

        /// A hut on stilts out over the lagoon beside a root, level with the root's top, with a plank walk to it.
        bool StiltHut(LandContext ctx, MeshKit mk, MeshKit solid, Vector2 from2, Vector2 nrm, float top, float gap, int i)
        {
            var up = Vector3.up; var a = On(ctx, from2); var n3 = new Vector3(nrm.x, 0, nrm.y); var hp = a + n3 * (gap + 3f); hp.y = a.y + .1f; var gy = ctx.Ground(hp.x, hp.z); if (gy > waterY + .2f || a.y < waterY + 3f) return false;
            for (var k = 0; k < 6; k++) { var q = a + n3 * (gap * (k + 1) / 6f); if (InFootprint(ctx, q.x, q.z, 3f) || NearPath(q.x, q.z, 2f)) return false; }
            var rot = Quaternion.LookRotation(-n3); var along = Quaternion.LookRotation(n3);
            foreach (var (x, zz) in new[] { (-2.2f, -2f), (2.2f, -2f), (2.2f, 2f), (-2.2f, 2f) }) { var sp = hp + rot * new Vector3(x, 0, zz); mk.Prism(trunkM, new Vector3(sp.x, waterY - 1.5f, sp.z), up, .16f, hp.y - waterY + 1.5f, 5); ArtistBits.PostCollider(ctx.Root, new Vector3(sp.x, waterY - 1f, sp.z), hp.y - waterY + 1f, .18f); }
            mk.Box(plank, hp, new Vector3(6f, .18f, 5.4f), rot); solid.Box(plank, hp - up * .3f, new Vector3(6f, .76f, 5.4f), rot);
            var wall = Paint(Paints[(i + 2) % Paints.Length]); mk.Box(wall, hp + up * 1.3f - rot * Vector3.forward * .7f, new Vector3(4.2f, 2.4f, 3.2f), rot); solid.Box(wall, hp + up * 1.3f - rot * Vector3.forward * .7f, new Vector3(4.2f, 2.4f, 3.2f), rot);
            mk.GableRoof(i % 2 == 0 ? rustM : zinc, hp + up * 2.5f - rot * Vector3.forward * .7f, new Vector3(4.4f, 0, 5f), 1.1f, Quaternion.LookRotation(rot * Vector3.right)); mk.Box(dark, hp + up * 1.1f + rot * Vector3.forward * .91f, new Vector3(.9f, 1.9f, .04f), rot);
            var lp = hp + up * 2.2f + rot * new Vector3(1.6f, 0, 1.2f); mk.Blob(GlowM(i % 2 == 0 ? Gold : Red, 2.2f), lp, new Vector3(.2f, .28f, .2f), prng, 0f); halos.Add(lp, 1.6f, Gold, .4f);
            // the plank walk, from the root's top out to the deck
            var s0 = a + n3 * -1.5f; var s1 = hp - n3 * 2.6f; var midP = (s0 + s1) / 2f; var len = Vector3.Distance(s0, s1); mk.Box(plank, midP + up * .02f, new Vector3(1.6f, .12f, len), along); solid.Box(plank, midP - up * .3f, new Vector3(1.6f, .7f, len), along);
            for (var k = 1; k < 4; k++) { var q = Vector3.Lerp(s0, s1, k / 4f); var g = ctx.Ground(q.x, q.z); mk.Prism(trunkM, new Vector3(q.x, Mathf.Min(g, waterY) - .6f, q.z), up, .1f, q.y - Mathf.Min(g, waterY) + .6f, 4); }
            Remember(midP, 9); Remember(hp, 9); Mark(hp, 4f);
            return true;
        }

        /// A plank bridge slung between two roots, with rope rails and lanterns.
        void RopeBridge(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 b, int i)
        {
            var up = Vector3.up; var flat = b - a; flat.y = 0; var len = flat.magnitude; var dir = flat / len; var side = Vector3.Cross(up, dir); var n = Mathf.Max(4, Mathf.CeilToInt(len / 3f)); var sag = Mathf.Min(2.2f, len * .03f);
            Vector3 At(float t) => Vector3.Lerp(a, b, t) - up * (Mathf.Sin(t * Mathf.PI) * sag) + up * .12f;
            for (var q = 0; q < n; q++)
            {
                var p0 = At(q / (float)n); var p1 = At((q + 1) / (float)n); var seg = p1 - p0; var rot = Quaternion.LookRotation(seg.normalized); var m = (p0 + p1) / 2f;
                mk.Box(q % 2 == 0 ? plank : woodM, m, new Vector3(2.4f, .1f, seg.magnitude + .04f), rot); solid.Box(plank, m - up * .25f, new Vector3(2.4f, .6f, seg.magnitude + .1f), rot);
                foreach (var e in new[] { -1f, 1f })
                {
                    var r0 = p0 + side * (e * 1.2f) + up * 1.05f; var r1 = p1 + side * (e * 1.2f) + up * 1.05f; mk.Prism(dark, r0, r1 - r0, .035f, (r1 - r0).magnitude, 4); if (q % 2 == 0) mk.Prism(woodM, p0 + side * (e * 1.2f), up, .05f, 1.08f, 4);
                    BoxCol(ctx.Root, (r0 + r1) / 2f - up * .45f, new Vector3(.12f, 1.2f, seg.magnitude), rot);
                }
                if (q % 6 == 3) { var lp = m + side * 1.2f + up * 1.7f; mk.Prism(woodM, m + side * 1.2f, up, .04f, 1.75f, 4); mk.Blob(GlowM(new[] { Gold, Red, new Color(.3f, .9f, .4f) }[(q / 6 + i) % 3], 2.4f), lp, new Vector3(.18f, .24f, .18f), prng, 0f); halos.Add(lp, 1.5f, Gold, .4f); }
                Remember(m, 9);
            }
            // posts where it is made fast
            foreach (var g in new[] { a, b }) foreach (var e in new[] { -1f, 1f }) { var p = g + side * (e * 1.3f); mk.Prism(trunkM, p - up * .4f, up, .14f, 2f, 6); ArtistBits.PostCollider(ctx.Root, p, 1.6f, .16f); }
        }

        /// The town out on the roots: painted houses beside the paths, huts on stilts over the lagoon, and bridges
        /// slung from each root to the next.
        void RootTown(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; int houses = 0, huts = 0, bridges = 0; var hi = 100;
            for (var li = 1; li < lines.Count; li++)
            {
                var l = lines[li];
                for (var i = 5; i + 2 < l.p.Count; i += 3)
                {
                    var t = (l.p[i + 1] - l.p[i - 1]).normalized; var sd = (i / 3 + li) % 2 == 0 ? 1f : -1f; var nrm = new Vector2(-t.y, t.x) * sd; var top = l.top[i];
                    if (top < waterY + 3f) continue;
                    if ((i / 3 + li) % 3 == 0)
                    {
                        // a hut on stilts out over the water
                        if (StiltHut(ctx, mk, solid, l.p[i] + nrm * (l.half[i] - 2f), nrm, top, FlankOf(top) + 5f, hi++)) { huts++; if (huts % 3 == 1) { var a = On(ctx, l.p[i] + nrm * (l.half[i] + FlankOf(top) + 5.4f)); a.y = top + .2f; Folk(a, new Vector3(nrm.x, 0, nrm.y), Resident.Pose.Stand, new[] { "The water's only knee deep, all the way through. Wade where you like. It's warm.", "My grandmother built on this root. Her grandmother built on that one. The tree doesn't mind, it's had worse than us.", "You fall off, you get wet, you walk back up. That's the whole danger." }, Local(Residents.Tool.Rod)); } }
                        continue;
                    }
                    var off = PathW / 2f + 6.2f; if (l.half[i] < off + 3.6f) continue;
                    var p = l.p[i] + nrm * off; if (!ctx.IsLand(p.x, p.y, 4f) || InFootprint(ctx, p.x, p.y, 5.6f) || InNoGrass(p.x, p.y, 4f)) continue;
                    var f = new PlaceFrame(On(ctx, p), new Vector3(-nrm.x, 0, -nrm.y)); var bad = false; foreach (var (x, zz) in new[] { (-3f, -2.3f), (3f, -2.3f), (3f, 2.3f), (-3f, 2.3f) }) { var q = f.P(x, 0, zz); if (LaneNear(q.x, q.z, PathW / 2f + .4f) || ctx.Ground(q.x, q.z) < top - 1.2f) bad = true; } if (bad) continue;
                    var po = Shack(ctx, mk, solid, f, 5.6f, 4.2f, hi++); Mark(f.o, 5f); houses++;
                    if (houses % 3 == 0) { Stool(mk, po.P(1.6f, 0, 0), po.Fwd, houses); Folk(po.P(-1.4f, 0, .1f), po.Fwd, Resident.Pose.Lean, new[] { "You live on a root, you learn to build level. Or you learn to sleep sloped.", "Morning! Tree dropped a leaf on the roof last night. Took four of us to carry it off.", "Bay Road's on the Great Root. Everything else is just neighbours." }); }
                }
            }
            // bridges from each root to the next one round, about halfway out and again further on
            var order = new List<RootLine>(); foreach (var l in lines) if (!l.fork) order.Add(l); order.Sort((a, b) => a.bearing.CompareTo(b.bearing));
            var all = new List<RootLine>(lines);
            for (var k = 0; k < all.Count; k++)
                foreach (var f in new[] { .5f, .82f })
                {
                    var l = all[k]; if (l.great && f > .6f) continue; var i = Mathf.Clamp(Mathf.RoundToInt((l.p.Count - 1) * f), 1, l.p.Count - 2); if (l.top[i] < waterY + 2.6f) continue;
                    // the nearest other root's spine on the anticlockwise side
                    var t = (l.p[i + 1] - l.p[i - 1]).normalized; var nrm = new Vector2(-t.y, t.x); RootLine other = null; Vector2 on = default; float best = 150f, oTop = 0, oHalf = 0;
                    foreach (var o in all) { if (o == l) continue; var d = Near(o, l.p[i], out var tp, out var hf, out var c); if (d < best && d > l.half[i] + hf + 12f && Vector2.Dot(c - l.p[i], nrm) > d * .6f && tp > waterY + 2.6f) { best = d; other = o; on = c; oTop = tp; oHalf = hf; } }
                    if (other == null) continue;
                    var dir = (on - l.p[i]).normalized; var a2 = l.p[i] + dir * (l.half[i] - 2.5f); var b2 = on - dir * (oHalf - 2.5f);
                    var clear = true; for (var u = .15f; u < .9f && clear; u += .1f) { var q = Vector2.Lerp(a2, b2, u); if (InFootprint(ctx, q.x, q.y, 4f) || InNoGrass(q.x, q.y, 2f)) clear = false; }
                    foreach (var e in new[] { a2, b2 }) if (InFootprint(ctx, e.x, e.y, 3f) || InNoGrass(e.x, e.y, 1.5f)) clear = false;
                    // over water for most of its length, or it is not a bridge
                    var wet = 0; for (var u = .2f; u < .85f; u += .1f) { var q = Vector2.Lerp(a2, b2, u); if (ctx.Ground(q.x, q.y) < waterY + .5f) wet++; } if (wet < 3 || !clear) continue;
                    var a = On(ctx, a2); var b = On(ctx, b2); if (Mathf.Abs(a.y - b.y) > Vector2.Distance(a2, b2) * .22f) continue;
                    RopeBridge(ctx, mk, solid, a, b, bridges); bridges++;
                    if (bridges == 2) Shot(ctx, "a bridge from root to root", a + up * 2.4f - new Vector3(dir.x, 0, dir.y) * 7f + new Vector3(nrm.x, 0, nrm.y) * 3f, b + up * 6f);
                }
            Debug.Log($"The Roots Bay: {houses} houses and {huts} stilt huts out on the roots, {bridges} bridges from root to root");
        }
    }
}
