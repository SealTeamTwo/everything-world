using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Bedroom Pop Meadows (WORLD_GUIDE.md): Sean's land. The daydream half of bedroom music: a deep lilac
    /// field just past sunset, full of fireflies. Its spine is a track, a loop you can walk round and round,
    /// lined with fairy lights that a pulse of light chases round (faster for energetic songs), circling a giant
    /// unmade bed with huge pillows, giant headphones and a glowing bedside lamp. Each artist is a small daydream
    /// scene off the loop (MeadowsArtists.cs). Fireflies, bubbles and glow-in-the-dark sticker stars: MeadowsLife.
    public partial class MeadowsKit : LandKit
    {
        public override string Land => "The Bedroom Pop Meadows";

        static readonly Color Lilac = new Color(.5f, .42f, .66f), LilacB = new Color(.58f, .48f, .71f), Dusk = new Color(.42f, .36f, .54f);
        static readonly Color FairyWarm = new Color(1f, .78f, .5f), Firefly = new Color(.75f, 1f, .45f);
        public static readonly Color SeanGreen = new Color(.13f, .55f, .2f), NoahBlue = new Color(.05f, .45f, 1f);
        static readonly Color Cream = new Color(.96f, .93f, .9f), Ink = new Color(.18f, .15f, .22f);
        static readonly Color[] Pastel =
        {
            new Color(.96f, .78f, .86f), new Color(.8f, .76f, .96f), new Color(.98f, .86f, .72f), new Color(.76f, .9f, .88f),
            new Color(.98f, .95f, .78f), new Color(.88f, .8f, .94f),
        };

        Material wood, darkWood, iron, board, fabric, rope, bark, leafA, leafB, track;
        GlowHalos halos;
        Vector3 center;
        readonly List<Vector3> loop = new List<Vector3>();
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> treeSpots = new List<Vector3>();
        MeshKit soft;       // leaves and the little fairy-light stakes you walk through (no collider; the land audit skips "Soft" meshes)

        void MakeMaterials()
        {
            wood = WorldMaterials.Solid(new Color(.6f, .47f, .38f));
            darkWood = WorldMaterials.Solid(new Color(.3f, .23f, .22f));
            iron = WorldMaterials.Solid(new Color(.22f, .2f, .26f));
            board = WorldMaterials.Solid(new Color(.18f, .15f, .22f));
            fabric = WorldMaterials.Solid(new Color(.92f, .88f, .94f));
            rope = WorldMaterials.Solid(new Color(.75f, .68f, .58f));
            bark = WorldMaterials.Solid(new Color(.33f, .27f, .3f));
            leafA = WorldMaterials.Solid(new Color(.36f, .4f, .5f));
            leafB = WorldMaterials.Solid(new Color(.44f, .42f, .58f));
            track = WorldMaterials.Solid(new Color(.8f, .76f, .88f));
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            color = slope > 6f ? Dusk : Color.Lerp(Lilac, LilacB, Mathf.SmoothStep(0, 1, patch));
            color.a = 1f;
            return true;
        }

        /// The artists stay where the layout put them, except any too close to the next land: those move inward,
        /// next to the similar artists round them (ArtistPlacement).
        public override void ShapeGround(IslandWorldData data, int index)
        {
            ArtistPlacement.KeepFromBorders(data, index, heartClear: 30f, land: Land);
            foreach (var (at, r) in PlaceRings) PlaceLevel(data, index, at, r, r + 12f);
        }

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            loop.Clear(); pathCells.Clear(); facing.Clear(); treeSpots.Clear();
            center = ctx.Plaza;
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            soft = new MeshKit();
            Loop(ctx, mk);
            Bed(ctx, mk, solid);
            MeadowPlaces(ctx, mk, solid);
            Trees(ctx, mk);
            Daydreams(ctx, mk, solid, pictures);
            SongPlaces(ctx, mk, solid);
            mk.Build("Meadows scenery", ctx.Root, false);
            soft.Build("Soft leaves and fairy-light stakes", ctx.Root, false);
            solid.BuildCollider("Meadows walls", ctx.Root);
            pictures.Build("Meadows polaroids", ctx.Root, false);
            var life = new GameObject("Meadows life").AddComponent<MeadowsLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            meadowsLife = life; life.songGlows.AddRange(pendingGlows); pendingGlows.Clear();
        }

        public override void Finish(LandContext ctx)
        {
            Meadow(ctx);
            halos.Build("Meadows glow", ctx.Root);
        }

        // ---------------------------------------------------------------- the track
        /// A loop round the middle, nudged round every artist's plot, kept on land and smoothed; then a spur from
        /// each artist to the loop, and fairy lights down both sides of the loop for the pulses to chase round.
        void Loop(LandContext ctx, MeshKit mk)
        {
            const int N = 180;
            var pts = new Vector3[N];
            for (var k = 0; k < N; k++)
            {
                var a = k * Mathf.PI * 2f / N;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var r = 112f + 12f * Mathf.Sin(3f * a + 1f) + 6f * Mathf.Sin(5f * a);
                var p = center + dir * r;
                while (!ctx.IsLand(p.x, p.z, 5f) && r > 62f) { r -= 4f; p = center + dir * r; }
                pts[k] = new Vector3(p.x, 0, p.z);
            }
            for (var pass = 0; pass < 4; pass++)
            {
                for (var k = 0; k < N; k++)
                    foreach (var art in ctx.Artists)
                    {
                        var ac = new Vector3(art.x, 0, art.z); var d = pts[k] - ac; var need = art.r + 3f;
                        if (d.magnitude < need) pts[k] = ac + (d.sqrMagnitude > .01f ? d.normalized : Vector3.right) * need;
                    }
                var smooth = new Vector3[N];
                for (var k = 0; k < N; k++) smooth[k] = (pts[(k + N - 1) % N] + pts[k] * 2f + pts[(k + 1) % N]) / 4f;
                pts = smooth;
            }
            foreach (var p in pts) { var g = ctx.OnGround(p.x, p.z); loop.Add(g); Remember(g); }
            for (var k = 0; k < N; k++) Remember((loop[k] + loop[(k + 1) % N]) / 2f);   // the loop's points are ~4 m apart: remember between them too, so things kept off it stay off its whole length
            var closed = new List<Vector3>(loop) { loop[0] };
            Ribbon(ctx, mk, track, closed, 2.6f, .07f);

            // fairy lights down both sides: each knows how far round the loop it is, for the chasing pulses
            var total = 0f;
            for (var k = 0; k < N; k++) total += Vector3.Distance(loop[k], loop[(k + 1) % N]);
            var lights = new ChaseMesh();
            float along = 0f, next = 0f;
            for (var k = 0; k < N; k++)
            {
                var a = loop[k]; var b = loop[(k + 1) % N];
                var seg = Vector3.Distance(a, b);
                var flat = b - a; flat.y = 0;
                var side = Vector3.Cross(Vector3.up, flat.normalized);
                while (next <= along + seg)
                {
                    var t = (next - along) / Mathf.Max(.01f, seg);
                    var p = Vector3.Lerp(a, b, t);
                    var u = next / total;
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var q = p + side * s * 1.75f; q.y = ctx.Ground(q.x, q.z);
                        soft.Prism(iron, q, Vector3.up, .03f, .55f, 4);
                        lights.Bulb(q + Vector3.up * .62f, .11f, ((int)(next / 4.5f)) % 3 == 0 ? Firefly : FairyWarm, u);
                    }
                    next += 4.5f;
                }
                along += seg;
            }
            lights.Build("Soft track lights", ctx.Root);
            foreach (var art in ctx.Artists) Spur(ctx, mk, art);
            // two short ways in from the bed to the loop
            foreach (var s in new[] { 0, N / 2 })
            {
                var to = loop[s]; var dir = new Vector3(to.x - center.x, 0, to.z - center.z).normalized;
                var route = new List<Vector3>();
                Leg(route, center + dir * 26f, new Vector3(to.x, 0, to.z));
                Lay(ctx, mk, route, 1.8f);
            }
            Debug.Log($"The Bedroom Pop Meadows: a loop of {total:0} m with {lights.Count} lights");
        }

        void Spur(LandContext ctx, MeshKit mk, IslandWorldData.Artist art)
        {
            var c = new Vector3(art.x, 0, art.z);
            var best = float.MaxValue; var join = Vector3.zero;
            foreach (var p in loop) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var dir = new Vector3(join.x - c.x, 0, join.z - c.z);
            dir = dir.sqrMagnitude > .01f ? dir.normalized : Vector3.forward;
            facing[art.name] = dir;
            if (best < art.r * .6f) return;
            var route = new List<Vector3>();
            Leg(route, c + dir * art.r * .5f, new Vector3(join.x, 0, join.z));
            Lay(ctx, mk, route, 1.5f, art.name);
        }

        void Lay(LandContext ctx, MeshKit mk, List<Vector3> route, float width, string own = null)
        {
            var run = new List<Vector3>();
            if (route.Count < 2) return;
            var from = route[0]; var dir = route[route.Count - 1] - from; dir.y = 0; dir = dir.sqrMagnitude > .01f ? dir.normalized : Vector3.forward;
            foreach (var p0 in route)
            {
                var p = RoundArtists(ctx, p0, from, dir, own);
                if (!ctx.IsLand(p.x, p.z, 0f)) { if (run.Count > 1) Ribbon(ctx, mk, track, run, width, .08f); run.Clear(); continue; }
                var g = ctx.OnGround(p.x, p.z); run.Add(g); Remember(g);
            }
            if (run.Count > 1) Ribbon(ctx, mk, track, run, width, .08f);
        }

        static void Leg(List<Vector3> route, Vector3 a, Vector3 b)
        {
            var d = b - a; var len = d.magnitude;
            if (len < .5f) return;
            route.Add(a);
            for (var s = 2.5f; s < len; s += 2.5f) route.Add(a + d * (s / len));
            route.Add(b);
        }

        void Remember(Vector3 p)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p);
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f);
            for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (pathCells.TryGetValue((cx + i, cz + j), out var l))
                    foreach (var p in l) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        /// Laid in narrow lanes across its width with every vertex on its own ground (raised only a little where the
        /// middle is higher), so on a side slope it follows the ground instead of burying one edge and lifting the other.
        static void Ribbon(LandContext ctx, MeshKit mk, Material m, List<Vector3> raw, float width, float lift)
        {
            var pts = new List<Vector3>();
            foreach (var p in raw) { if (pts.Count > 0) { var d = p - pts[pts.Count - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; } pts.Add(p); }
            var n = pts.Count; if (n < 2) return;
            var lanes = Mathf.Max(1, Mathf.CeilToInt(width / 1.2f)); var V = new Vector3[n, lanes + 1];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).sqrMagnitude > .01f ? (a + b).normalized : b; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                Vector3 l = pts[i] - side * (width / 2f * miter), r = pts[i] + side * (width / 2f * miter);
                var mid = ctx.Ground(pts[i].x, pts[i].z);
                var bulge = Mathf.Clamp(mid - (ctx.Ground(l.x, l.z) + ctx.Ground(r.x, r.z)) / 2f, 0f, .3f);
                for (var k = 0; k <= lanes; k++) { var q = Vector3.Lerp(l, r, k / (float)lanes); q.y = ctx.Ground(q.x, q.z) + bulge + lift; V[i, k] = q; }
            }
            for (var i = 0; i + 1 < n; i++) for (var k = 0; k < lanes; k++) mk.Quad(m, V[i, k], V[i, k + 1], V[i + 1, k + 1], V[i + 1, k], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// Bends a point of a lane round any artist's area it would cross (other than `own`), along an arc on the side
        /// the straight line passes, so the lane never flips across.
        Vector3 RoundArtists(LandContext ctx, Vector3 q, Vector3 from, Vector3 dir, string own)
        {
            var side = Vector3.Cross(Vector3.up, dir);
            foreach (var o in ctx.Artists)
            {
                if (o.name == own) continue; var r = o.r + 3f; var ov = new Vector3(o.x, 0, o.z); var rel = new Vector3(q.x - o.x, 0, q.z - o.z);
                if (rel.sqrMagnitude >= r * r) continue;
                var along = Vector3.Dot(rel, dir); var sgn = Vector3.Dot(new Vector3(from.x, 0, from.z) - ov, side) >= 0f ? 1f : -1f;
                q = ov + dir * along + side * sgn * Mathf.Sqrt(Mathf.Max(0f, r * r - along * along));
            }
            return q;
        }

        bool InArtist(LandContext ctx, float x, float z, float pad)
        {
            foreach (var o in ctx.Artists) { float dx = x - o.x, dz = z - o.z; if (dx * dx + dz * dz < (o.r + pad) * (o.r + pad)) return true; }
            return false;
        }

        /// Little glowing bulbs for the Chase Lights shader: vertex colour = colour, uv.x = how far round the loop.
        class ChaseMesh
        {
            readonly List<Vector3> v = new List<Vector3>(); readonly List<Color> c = new List<Color>(); readonly List<Vector2> uv = new List<Vector2>(); readonly List<int> t = new List<int>();
            public int Count;
            public void Bulb(Vector3 at, float r, Color col, float u)
            {
                Vector3[] p = { at + Vector3.up * r, at - Vector3.up * r, at + Vector3.right * r, at - Vector3.right * r, at + Vector3.forward * r, at - Vector3.forward * r };
                int[] f = { 0, 2, 4, 0, 4, 3, 0, 3, 5, 0, 5, 2, 1, 4, 2, 1, 3, 4, 1, 5, 3, 1, 2, 5 };
                var b = v.Count;
                foreach (var q in p) { v.Add(q); c.Add(col); uv.Add(new Vector2(u, 0)); }
                foreach (var i in f) t.Add(b + i);
                Count++;
            }
            public void Build(string name, Transform parent)
            {
                var shader = Shader.Find("Everything World/Chase Lights");
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(v); mesh.SetColors(c); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
                var go = new GameObject(name); go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = shader != null ? new Material(shader) { name = name } : WorldMaterials.Glow(FairyWarm, 2f);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.isStatic = true;
            }
        }

        // ---------------------------------------------------------------- the giant unmade bed
        void Bed(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var root = new GameObject("The giant bed").transform; root.SetParent(ctx.Root, false);
            // the bed lies across the loop's opening toward the land's longest side
            var along = Vector3.forward;
            var rot = Quaternion.LookRotation(along);
            var side = Vector3.Cross(Vector3.up, along);
            const float L = 34f, W = 24f, H = 6.5f;
            var c = ctx.OnGround(center.x, center.z);
            float lo = float.MaxValue;
            for (var i = -2; i <= 2; i++) for (var j = -2; j <= 2; j++) { var p = c + side * i * W / 4f + along * j * L / 4f; lo = Mathf.Min(lo, ctx.Ground(p.x, p.z)); }
            var baseY = lo;
            var frame = WorldMaterials.Solid(new Color(.55f, .42f, .36f));
            var sheet = WorldMaterials.Solid(new Color(.93f, .9f, .96f));
            var duvet = WorldMaterials.Solid(new Color(.78f, .66f, .9f));
            var duvetB = WorldMaterials.Solid(new Color(.86f, .74f, .94f));
            var pillow = WorldMaterials.Solid(new Color(.97f, .95f, .93f));
            Vector3 P(float x, float y, float z) => new Vector3(c.x, baseY, c.z) + side * x + Vector3.up * y + along * z;
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                mk.Box(frame, P(sx * (W / 2f - 1f), 1.2f, sz * (L / 2f - 1f)), new Vector3(1.6f, 2.4f + .5f, 1.6f), rot);
            mk.Box(frame, P(0, 2.4f, 0), new Vector3(W, 1.4f, L), rot);
            mk.Box(sheet, P(0, 4.3f, 0), new Vector3(W - .6f, 2.6f, L - .6f), rot);
            solid.Box(sheet, P(0, (H - .3f) / 2f, 0), new Vector3(W, H - .3f, L), rot);
            // the headboard, rounded on top, with fairy lights draped along it
            mk.Box(frame, P(0, 8f, L / 2f + .5f), new Vector3(W + 1f, 16f, 1.2f), rot);
            mk.Prism(frame, P(0, 16f, L / 2f - .1f), along, W / 2f + .5f, 1.2f, 20);
            solid.Box(frame, P(0, 10f, L / 2f + .5f), new Vector3(W + 1f, 20f, 1.4f), rot);
            var arcs = new List<Vector3>();
            for (var i = 0; i <= 40; i++)
            {
                var a = Mathf.PI * i / 40f;
                arcs.Add(P(Mathf.Cos(a) * (W / 2f - 1.2f), 16f + Mathf.Sin(a) * (W / 2f - 1.2f) - Mathf.Abs(Mathf.Sin(a * 6f)) * 1.2f, L / 2f - .2f));
            }
            FairyString(mk, arcs, FairyWarm);
            // pillows and a rumpled duvet sliding off one side down to the grass (walk up it onto the bed)
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Blob(pillow, P(s * 5.5f, H + 1.6f, L / 2f - 5f), new Vector3(5f, 1.8f, 3.4f), ctx.Rng, .1f);
                solid.Box(pillow, P(s * 5.5f, H + 1.2f, L / 2f - 5f), new Vector3(8.5f, 2.6f, 5.5f), rot);
            }
            for (var i = 0; i < 9; i++)
            {
                var at = P(ctx.Range(-8f, 8f), H + .6f + ctx.Range(0, 1.2f), ctx.Range(-14f, 6f));
                var size = new Vector3(ctx.Range(5f, 8f), ctx.Range(1.2f, 2.2f), ctx.Range(5f, 8f));
                mk.Blob(i % 2 == 0 ? duvet : duvetB, at, size, ctx.Rng, .15f);
                solid.Box(duvet, at, new Vector3(size.x * 1.5f, size.y * 1.4f, size.z * 1.5f), rot);
            }
            // a staircase of giant books at the foot of the bed, with a smooth slope hidden under it to walk up
            var steps = 8; const float rise = .82f, run = 1.7f;
            var spines = new[] { new Color(.62f, .5f, .82f), new Color(.92f, .62f, .74f), new Color(.6f, .76f, .86f), new Color(.95f, .82f, .58f), new Color(.72f, .84f, .7f) };
            var pages = WorldMaterials.Solid(new Color(.97f, .95f, .88f));
            var farEdge = Vector3.zero;
            for (var i = 0; i < steps; i++)
            {
                var at = P(0, (i + .5f) * rise, -L / 2f - 1.3f - (steps - 1 - i) * run);
                var spin = Quaternion.LookRotation(along) * Quaternion.Euler(0, (i * 23) % 9 - 4, 0);
                var footprint = new Vector3(6.5f, rise, run * (steps - i) + 1.2f);
                var c2 = at - along * (footprint.z / 2f - run / 2f - .6f);
                if (i == 0)
                {
                    farEdge = c2 - along * (footprint.z / 2f);
                    // where the ground under the bottom book falls below the bed's floor, a footing of more books reaches down to it
                    var stairLo = baseY;
                    for (var u = -1; u <= 1; u++) for (var w = -4; w <= 4; w++) { var q = c2 + side * u * 3.2f + along * w * footprint.z / 8.2f; stairLo = Mathf.Min(stairLo, ctx.Ground(q.x, q.z)); }
                    if (stairLo < baseY - .05f) { var fh = baseY - stairLo + .2f; mk.Box(WorldMaterials.Solid(spines[(i + 2) % spines.Length]), new Vector3(c2.x, baseY - fh / 2f + .1f, c2.z), new Vector3(footprint.x, fh, footprint.z), spin); }
                }
                mk.Box(WorldMaterials.Solid(spines[i % spines.Length]), c2, footprint, spin);
                mk.Box(pages, c2 + side * .06f + along * .08f, new Vector3(footprint.x + .1f, rise * .7f, footprint.z - .4f), spin);   // .06 sideways: at .05 the top book's pages met its cover's side
            }
            var bottom = farEdge; bottom.y = ctx.Ground(bottom.x, bottom.z) - .2f;          // the slope starts where the first book does
            var topStep = P(0, steps * rise + .05f, -L / 2f - .4f);
            var ramp = Quaternion.LookRotation(topStep - bottom);
            solid.Box(pages, (topStep + bottom) / 2f, new Vector3(6f, .4f, Vector3.Distance(topStep, bottom) + .4f), ramp);
            // giant headphones lying on the duvet
            var hp = P(-3f, H + 2.4f, -8f);
            var band = WorldMaterials.Solid(new Color(.25f, .22f, .3f), .4f);
            for (var i = 0; i < 16; i++)
            {
                float a0 = Mathf.PI * i / 16f, a1 = Mathf.PI * (i + 1) / 16f;
                Vector3 p0 = hp + side * Mathf.Cos(a0) * 3.2f + along * Mathf.Sin(a0) * 3.2f, p1 = hp + side * Mathf.Cos(a1) * 3.2f + along * Mathf.Sin(a1) * 3.2f;
                mk.Box(band, (p0 + p1) / 2f, new Vector3(.5f, .35f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0));
            }
            foreach (var s in new[] { -1f, 1f })
            {
                solid.Prism(band, hp + side * s * 3.2f - Vector3.up * .5f, Vector3.up, 1.3f, 1.1f, 8);
                mk.Prism(band, hp + side * s * 3.2f - Vector3.up * .5f, Vector3.up, 1.3f, .9f, 14);
                mk.Prism(WorldMaterials.Solid(new Color(.82f, .7f, .92f)), hp + side * s * 3.2f + Vector3.up * .4f, Vector3.up, 1f, .12f, 14);
            }
            // the bedside table and its lamp: the warm light at the heart of the field
            // the table stands on the lowest ground under its corners (its uphill side sinks into the grass)
            var table = P(-W / 2f - 5f, 0, L / 2f - 3f); table.y = ctx.Ground(table.x, table.z);
            for (var i = -1; i <= 1; i++) for (var j = -1; j <= 1; j++) { var q = table + side * i * 3f + along * j * 3f; table.y = Mathf.Min(table.y, ctx.Ground(q.x, q.z)); }
            mk.Box(frame, table + Vector3.up * 3.5f, new Vector3(6f, 7f, 6f), rot);
            solid.Box(frame, table + Vector3.up * 3.5f, new Vector3(6f, 7f, 6f), rot);
            mk.Prism(darkWood, table + Vector3.up * 7f, Vector3.up, .35f, 5f, 8);
            solid.Prism(darkWood, table + Vector3.up * 7f, Vector3.up, .5f, 5f, 8);
            solid.Prism(darkWood, table + Vector3.up * 10.9f, Vector3.up, 3.2f, 3.2f, 10, .62f);
            var shadeAt = table + Vector3.up * 12.5f;
            mk.Prism(WorldMaterials.Glow(new Color(1f, .82f, .6f), 2.2f), shadeAt - Vector3.up * 1.6f, Vector3.up, 3.2f, 3.2f, 16, .62f);
            halos.Add(shadeAt, 9f, FairyWarm, 1f);
            ArtistBits.PointLight(root, shadeAt - Vector3.up * 2f, new Color(1f, .78f, .55f), 60f, 2.2f);
            ArtistBits.PointLight(root, P(0, H + 4f, 0), new Color(.95f, .75f, .85f), 30f, 1f);
        }

        /// Fairy lights along a line of points: small warm bulbs with halos, every 0.8 m.
        void FairyString(MeshKit mk, List<Vector3> pts, Color color)
        {
            var bulb = WorldMaterials.Glow(color, 2.6f);
            float carry = 0f;
            for (var i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i]; var b = pts[i + 1]; var len = Vector3.Distance(a, b);
                mk.Box(darkWood, (a + b) / 2f, new Vector3(.03f, .03f, len), Quaternion.LookRotation((b - a).sqrMagnitude > 1e-4f ? b - a : Vector3.forward));
                for (var d = carry; d < len; d += .8f)
                {
                    var p = Vector3.Lerp(a, b, d / len) - Vector3.up * .08f;
                    mk.Blob(bulb, p, Vector3.one * .07f, fairyRng, 0f);
                    if (((int)(d * 10f) + i) % 3 == 0) halos.Add(p, .5f, color, .7f);
                }
                carry = Mathf.Repeat(carry - len, .8f);
            }
        }

        /// Fairy lights sagging from a to b.
        void FairyLights(MeshKit mk, Vector3 a, Vector3 b, float sag, Color color)
        {
            var pts = new List<Vector3>();
            for (var i = 0; i <= 16; i++) { var t = i / 16f; pts.Add(Vector3.Lerp(a, b, t) - Vector3.up * sag * 4f * t * (1f - t)); }
            FairyString(mk, pts, color);
        }

        // ---------------------------------------------------------------- daydreams in the field
        /// Bedroom things dreamed out into the meadow: glowing lava lamps, giant polaroids of the Meadows' own
        /// covers, paper planes stuck nose-first in the grass, puffy star cushions, and fairy lights on tall poles.
        void Daydreams(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit pictures)
        {
            var placed = new List<Vector3>();
            bool Spot(float pad, out Vector3 p)
            {
                for (var t = 0; t < 40; t++)
                {
                    if (!ctx.TrySpot(out p, 6f, pad, 1) || NearPath(p.x, p.z, pad)) continue;
                    var ok = true;
                    foreach (var q in placed) if ((q - p).sqrMagnitude < 18f * 18f) { ok = false; break; }
                    foreach (var q in treeSpots) if ((q - p).sqrMagnitude < 9f * 9f) { ok = false; break; }   // clear of tree trunks and crowns
                    if (!ok) continue;
                    placed.Add(p);
                    return true;
                }
                p = default; return false;
            }
            var pic = WorldMaterials.Picture(ctx.CoverAtlas);
            var counts = new int[5];
            // the lowest ground round p within r (no random numbers: things stand on it instead of floating off a slope)
            float Low(Vector3 p, float r) { var lo = ctx.Ground(p.x, p.z); for (var k = 0; k < 8; k++) lo = Mathf.Min(lo, ctx.Ground(p.x + Mathf.Cos(k * Mathf.PI / 4f) * r, p.z + Mathf.Sin(k * Mathf.PI / 4f) * r)); return lo; }
            for (var i = 0; i < 70; i++)
            {
                var kind = i % 5;
                if (!Spot(kind == 4 ? 6f : 4f, out var p)) continue;
                counts[kind]++;
                var face = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
                var color = Pastel[ctx.Rng.Next(Pastel.Length)];
                switch (kind)
                {
                    case 0: // a lava lamp, glowing
                    {
                        var h = ctx.Range(3.5f, 6.5f);
                        p.y = Low(p, 1.1f);
                        var metal = WorldMaterials.Solid(new Color(.7f, .66f, .8f), .5f);
                        mk.Prism(metal, p, Vector3.up, 1.1f, h * .22f, 12, .7f);
                        var glass = WorldMaterials.Glow(color, 1.5f);
                        mk.Prism(glass, p + Vector3.up * h * .22f, Vector3.up, .8f, h * .6f, 12, .55f);
                        mk.Cone(metal, p + Vector3.up * h * .82f, .48f, h * .18f, 12);
                        var blob = WorldMaterials.Glow(Color.Lerp(color, Color.white, .5f), 2.6f);
                        for (var k = 0; k < 3; k++) mk.Blob(blob, p + Vector3.up * (h * (.35f + k * .15f)), Vector3.one * (.32f - k * .06f), ctx.Rng, .2f);
                        halos.Add(p + Vector3.up * h * .5f, 3.2f, color, .9f);
                        ArtistBits.PostCollider(ctx.Root, p, h, 1f);
                        ArtistBits.PointLight(ctx.Root, p + Vector3.up * (h * .5f + 1.2f), color, 11f, 1.1f);
                        break;
                    }
                    case 1: // a giant polaroid standing in the grass, showing one of the Meadows' covers
                    {
                        var cover = -1;
                        for (var t = 0; t < 8 && cover < 0; t++)
                        {
                            var a = ctx.Artists[ctx.Rng.Next(ctx.Artists.Count)];
                            if (a.songList != null && a.songList.Length > 0) cover = a.songList[ctx.Rng.Next(a.songList.Length)].cover;
                        }
                        var lean = Quaternion.LookRotation(face) * Quaternion.Euler(-8f, 0, ctx.Range(-6f, 6f));
                        { var rt = Vector3.Cross(Vector3.up, face) * 1.6f; p.y = Mathf.Min(ctx.Ground((p + rt).x, (p + rt).z), ctx.Ground((p - rt).x, (p - rt).z), p.y); }
                        var c = p + Vector3.up * 2.1f;
                        mk.Box(fabric, c, new Vector3(3.2f, 3.9f, .12f), lean);
                        solid.Box(fabric, c, new Vector3(3.2f, 3.9f, .3f), lean);
                        if (cover >= 0)
                        {
                            var n = lean * Vector3.forward; var up = lean * Vector3.up; var right = Vector3.Cross(n, up).normalized;
                            pictures.Picture(pic, c + up * .3f + n * .07f, right * 1.35f, up * 1.35f, n, ctx.CoverRect(cover));
                        }
                        break;
                    }
                    case 2: // a paper plane, nose in the ground
                    {
                        var paper = WorldMaterials.Solid(new Color(.98f, .97f, .95f));
                        var dive = Quaternion.LookRotation(face) * Quaternion.Euler(28f, 0, ctx.Range(-15f, 15f));
                        { var nz = p + Vector3.up * 1.1f + dive * new Vector3(0, 0, 2.6f); p.y += ctx.Ground(nz.x, nz.z) - .25f - nz.y; }   // the nose is in the ground wherever the slope falls
                        Vector3 L(float x, float y, float z) => p + Vector3.up * 1.1f + dive * new Vector3(x, y, z);
                        var nose = L(0, 0, 2.6f); var tail = L(0, 0, -2.6f);
                        foreach (var s in new[] { -1f, 1f })
                        {
                            var wing = L(s * 2.2f, .25f, -2.6f);
                            mk.Tri(paper, nose, tail, wing, L(0, 1f, 0));
                            mk.Tri(paper, nose, tail, wing, L(0, -1f, 0));
                            mk.Tri(paper, nose, tail, L(s * .25f, -.9f, -2.4f), L(-s, 0, 0));
                        }
                        solid.Box(paper, L(0, 0, 0), new Vector3(4.6f, 1.4f, 5.4f), dive);
                        break;
                    }
                    case 3: // a puffy star cushion
                    {
                        var r = ctx.Range(1.6f, 2.6f); var th = r * .45f;
                        var standing = ctx.Chance(.5f);
                        p.y = standing ? Mathf.Min(p.y, ctx.Ground((p + Vector3.Cross(Vector3.up, face) * r * .6f).x, (p + Vector3.Cross(Vector3.up, face) * r * .6f).z), ctx.Ground((p - Vector3.Cross(Vector3.up, face) * r * .6f).x, (p - Vector3.Cross(Vector3.up, face) * r * .6f).z)) : Low(p, r * .8f);
                        var rot = standing ? Quaternion.LookRotation(face) : Quaternion.LookRotation(Vector3.up, face);
                        var c = p + Vector3.up * (standing ? r * 1.05f : th * .5f + .05f);
                        var mat = WorldMaterials.Solid(color);
                        var pts = new Vector3[10];
                        for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f + Mathf.PI / 2f; var rr = k % 2 == 0 ? r : r * .48f; pts[k] = new Vector3(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0); }
                        for (var k = 0; k < 10; k++)
                        {
                            Vector3 a0 = c + rot * pts[k], a1 = c + rot * pts[(k + 1) % 10];
                            Vector3 f0 = c + rot * Vector3.forward * th * .5f, b0 = c - rot * Vector3.forward * th * .5f;
                            mk.Tri(mat, f0, a0, a1, c - rot * Vector3.forward);
                            mk.Tri(mat, b0, a0, a1, c + rot * Vector3.forward);
                        }
                        solid.Box(mat, c, new Vector3(r * 1.95f, r * 1.9f, th + .1f), rot);
                        break;
                    }
                    default: // fairy lights strung between two tall poles
                    {
                        var other = p + face * ctx.Range(10f, 16f); other = ctx.OnGround(other.x, other.z);
                        if (!ctx.IsLand(other.x, other.z, 2f) || NearPath(other.x, other.z, 2f)) break;
                        foreach (var q in new[] { p, other }) { mk.Prism(darkWood, q, Vector3.up, .1f, 5.5f, 6); ArtistBits.PostCollider(ctx.Root, q, 5.5f, .14f); }
                        FairyLights(mk, p + Vector3.up * 5.3f, other + Vector3.up * 5.3f, 1.4f, ctx.Chance(.3f) ? Firefly : FairyWarm);
                        break;
                    }
                }
            }
            Debug.Log($"The Bedroom Pop Meadows: {counts[0]} lava lamps, {counts[1]} polaroids, {counts[2]} paper planes, {counts[3]} star cushions, {counts[4]} light strings");
        }

        // ---------------------------------------------------------------- trees and the meadow
        void Trees(LandContext ctx, MeshKit mk)
        {
            for (var i = 0; i < 64; i++)
            {
                if (!ctx.TrySpot(out var p, 8f, 6f) || NearPath(p.x, p.z, 5f)) continue;
                var h = ctx.Range(3f, 5f); var r = ctx.Range(2.6f, 4f);
                mk.Prism(bark, p, Vector3.up, .28f, h + 1f, 7, .7f);
                soft.Blob(i % 2 == 0 ? leafA : leafB, p + Vector3.up * (h + r * .55f), new Vector3(r, r * .85f, r), ctx.Rng, .14f);
                ArtistBits.PostCollider(ctx.Root, p, h, .32f);
                treeSpots.Add(p);
                if (i % 4 == 0) FairyLights(mk, p + Vector3.up * (h + .3f) + Vector3.right * r * .6f, p + Vector3.up * (h + .3f) - Vector3.right * r * .6f, 1.2f, i % 8 == 0 ? Firefly : FairyWarm);
            }
        }

        /// Deep lilac long grass in drifts, purple clover, pastel flowers and dandelion clocks. Off the paths.
        void Meadow(LandContext ctx)
        {
            var mat = WindMesh.Material(.4f, .9f, Lilac);
            var chunks = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(4949);
            const float step = 1.55f;
            // In patches like a real field: deep drifts of long lilac grass (tallest and thickest at their hearts),
            // low lawns of purple clover, small trodden hollows of bare ground, flowers in drifts of one pastel each,
            // and dandelion clocks gathered in their own corners. Each patch its own shade, from a deep dusk violet
            // to a lighter, warmer lilac (by brightness and blue/yellow); every blade a touch its own.
            int longGrass = 0, clover = 0, blooms = 0, clocks = 0;
            Color[] flowers = { new Color(.98f, .72f, .84f), new Color(.98f, .96f, .94f), new Color(.98f, .92f, .6f), new Color(.8f, .72f, .98f) };
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 3f) || !ctx.IsFree(px, pz, 1.5f) || NearPath(px, pz, 2.4f)) continue;
                if (WindMesh.Patch(px, pz, .05f, 41f) < .28f) continue;                   // little trodden hollows
                var n = Mathf.Clamp01(Mathf.PerlinNoise(px * .018f + 2f, pz * .018f + 7f));
                var tone = Mathf.Clamp01((WindMesh.Patch(px, pz, .022f, 43f) - .25f) * 2f);
                var drift = WindMesh.Patch(px, pz, .045f, 45f); var clockField = WindMesh.Patch(px, pz, .035f, 47f);
                var r = (float)rng.NextDouble();
                var g = ctx.Ground(px, pz);
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!chunks.TryGetValue(key, out var wm)) chunks[key] = wm = new WindMesh();
                var at = new Vector3(px, g - .04f, pz);
                if (n > .45f)
                {
                    var col = Color.Lerp(new Color(.44f, .37f, .64f), new Color(.64f, .52f, .76f), tone) * (.93f + r * .14f);
                    wm.Tuft(at, .6f + r * .7f * n, col, new Color(.84f, .76f, .96f), rng, 3, .1f, .12f);
                    if (n > .62f && r < .3f) wm.Tuft(at + new Vector3(.4f, 0, -.3f), .55f + r * .6f, col, new Color(.84f, .76f, .96f), rng, 3, .1f, .12f);
                    longGrass++;
                }
                else
                {
                    var col = Color.Lerp(new Color(.34f, .31f, .5f), new Color(.48f, .41f, .58f), tone) * (.93f + r * .14f);
                    wm.Tuft(at, .2f + r * .15f, col, Color.Lerp(col, Cream, .2f), rng, 4, .09f, .12f);   // low clover
                    clover++;
                }
                var f = rng.NextDouble();
                if (f < (drift > .6f ? .2 : .03))
                {
                    var fl = flowers[WindMesh.Pick(Mathf.Clamp01((WindMesh.Patch(px, pz, .02f, 49f) - .2f) * 1.67f), flowers.Length)];   // the drift's one flower
                    wm.Flower(at + new Vector3(.3f, 0, .2f), .35f + r * .3f, fl, rng); blooms++;
                }
                else if (rng.NextDouble() < (clockField > .65f ? .12 : .01)) { Dandelion(wm, at + new Vector3(-.2f, 0, .3f), .5f, rng); clocks++; }
            }
            foreach (var kv in chunks) kv.Value.Build("Lilac meadow", ctx.Root, mat);
            Debug.Log($"The Bedroom Pop Meadows: {longGrass} long-grass tufts, {clover} clover tufts, {blooms} flowers, {clocks} dandelion clocks");
        }

        /// A dandelion clock: a stem and a pale round puff of seeds.
        static void Dandelion(WindMesh wm, Vector3 at, float h, System.Random rng)
        {
            var top = at + Vector3.up * h;
            wm.Tri(at - Vector3.right * .025f, at + Vector3.right * .025f, top, new Color(.32f, .4f, .3f), new Color(.38f, .45f, .34f), 1f);
            var puff = new Color(.97f, .96f, 1f);
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .14f;
                wm.Tri(top, top + d + Vector3.up * .1f, top + Quaternion.Euler(0, 45, 0) * d + Vector3.up * .1f, puff, puff, 1f);
                wm.Tri(top, top + d - Vector3.up * .08f, top + Quaternion.Euler(0, 45, 0) * d - Vector3.up * .08f, puff * .9f, puff, 1f);
            }
        }
    }
}
