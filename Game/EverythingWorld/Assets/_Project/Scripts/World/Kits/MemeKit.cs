using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Isle of Memes (WORLD_GUIDE.md): a vaporwave dream of the old internet. A pastel sky going from
    /// pink to teal, a sun like a pink coin, checkered marble floors running out across soft green hills, glossy aqua
    /// bubbles drifting up, marble busts on pedestals, Greek columns, palms, dolphins leaping out of glassy pools, old
    /// computer monitors left in the grass, and doors standing on their own in empty fields. You arrive on the Homepage
    /// under a giant glossy orb. The Superhighway, a road of glass, runs to the Dream Mall. Silly and strange and a bit
    /// nostalgic: everything you ever laughed at with your friends, in one place.
    /// Laid out on purpose (the build-land standard): MemeTown.cs has the places and the people.
    public partial class MemeKit : LandKit
    {
        public override string Land => "The Isle of Memes";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Pink = new Color(1f, .62f, .82f), Teal = new Color(.36f, .86f, .86f), Aqua = new Color(.4f, .8f, 1f), Lilac = new Color(.74f, .64f, 1f), Marble = new Color(.96f, .94f, .96f), Ink = new Color(.12f, .1f, .2f), Bliss = new Color(.4f, .74f, .36f), Sunny = new Color(1f, .88f, .5f);
        const float PathW = 3f, RoadW = 6f;

        Material checkA, checkB, marble, glassM, aquaM, bubbleM, white, dark, iron, leafM, trunkM, windowM, screenM, lampM, chrome;
        GlowHalos halos; Vector3 center, D, S; MemeLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            checkA = WorldMaterials.Glow(new Color(.98f, .8f, .9f), .25f); checkB = WorldMaterials.Glow(new Color(.36f, .32f, .5f), .25f); marble = WorldMaterials.Glow(Marble, .2f); glassM = WorldMaterials.Glow(new Color(.6f, .86f, .96f), .3f); white = WorldMaterials.Glow(Marble, .2f); dark = WorldMaterials.Solid(new Color(.1f, .08f, .14f)); iron = WorldMaterials.Surface(new Color(.6f, .62f, .7f), Surface.Metal); chrome = WorldMaterials.Glow(new Color(.8f, .84f, .92f), .3f);
            leafM = WorldMaterials.Glow(new Color(.3f, .7f, .5f), .2f); trunkM = WorldMaterials.Glow(new Color(.66f, .5f, .5f), .15f);
            aquaM = new Material(WorldMaterials.Glow(Aqua, .9f)) { name = "Meme aqua" }; bubbleM = new Material(WorldMaterials.Glow(new Color(.7f, .94f, 1f), .9f)) { name = "Meme bubbles" }; windowM = new Material(WorldMaterials.Glow(new Color(1f, .86f, .7f), 1f)) { name = "Meme windows" }; screenM = new Material(WorldMaterials.Glow(new Color(.5f, .7f, 1f), 1.2f)) { name = "Meme screens" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .9f, 1f), 1.4f)) { name = "Meme lamps" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .22f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Phineas and Ferb" ? 15f : a.name == "Stantough" ? 11f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // soft desktop-wallpaper green hills, a little teal; pink where it's steep
            if (slope > 24f) color = Color.Lerp(new Color(.86f, .6f, .74f), new Color(.92f, .7f, .8f), patch);
            else color = Color.Lerp(new Color(.36f, .66f, .46f), new Color(.5f, .78f, .56f), Mathf.SmoothStep(0f, 1f, patch));
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float d, float s) => heart2 + D2 * d + S2 * s;
        Vector3 Wp(float d, float s) => center + D * d + S * s;
        Vector3 G(LandContext ctx, float d, float s) { var p = Wp(d, s); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            streetL = 0f;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 130f && Inside(heart2 + dir * (len + 10f), 50f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var mall = P2(streetL + 18f, 0);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Phineas and Ferb", -6f, 54f, 30f); Put("Stantough", 0f, -50f, 24f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 12f, heart2.y + D2.y * d)); avoid.Add(new Vector3(mall.x, 26f, mall.y));
            ArtistPlacement.Spread(data, index, 50f, heart2, 44f, 38f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 22f, 40f, plazaY); Level(data, index, mall, 22f, 38f, Mathf.Max(data.HeightAt(mall.x, mall.y), waterY + 4f));
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend, float level)
        {
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Ease(0f, 1f, (d - flat) / (blend - flat)));
                }
            }
        }

        // ---------------------------------------------------------------- dressing
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Meme life").AddComponent<MemeLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.aqua = aquaM; life.screens = screenM; life.bubbles = bubbleM; life.lamps = lampM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Homepage(ctx, mk, solid);
            Superhighway(ctx, mk, solid);
            DreamMall(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Isle of Memes", ctx.Root, false);
            solid.BuildCollider("The Isle of Memes walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Meme glow", ctx.Root); }

        // ---------------------------------------------------------------- helpers
        void Remember(Vector3 p, int walk)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p); walkPoints.Add((p, walk));
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f); var n = Mathf.CeilToInt(within / 8f);
            for (var i = -n; i <= n; i++)
            for (var j = -n; j <= n; j++)
                if (pathCells.TryGetValue((cx + i, cz + j), out var l))
                    foreach (var p in l) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        bool LaneNear(float x, float z, float within) { foreach (var (p, w) in walkPoints) { if (w < 10) continue; float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; } return false; }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool InFootprint(LandContext ctx, float x, float z, float pad)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = Foot(a) + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground)
        {
            // Laid in narrow lanes across its width with every vertex on its own ground (raised only a little where the
            // middle is higher), so on a side slope it follows the ground instead of burying one edge and lifting the other.
            var n = pts.Count; var lanes = Mathf.Max(1, Mathf.CeilToInt(w / 1.2f)); var V = new Vector3[n, lanes + 1];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                Vector3 l = pts[i] - side * (w / 2f * miter), r = pts[i] + side * (w / 2f * miter);
                var bulge = Mathf.Clamp(pts[i].y - (ground.Ground(l.x, l.z) + ground.Ground(r.x, r.z)) / 2f, 0f, .3f);
                for (var k = 0; k <= lanes; k++) { var q = Vector3.Lerp(l, r, k / (float)lanes); q.y = ground.Ground(q.x, q.z) + bulge + lift; V[i, k] = q; }
            }
            for (var i = 0; i + 1 < n; i++) for (var k = 0; k < lanes; k++) mk.Quad(m, V[i, k], V[i, k + 1], V[i + 1, k + 1], V[i + 1, k], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A checkered floor of marble squares: pink and dusk-violet. With ground, each square lies on its own ground.
        void Checker(MeshKit mk, Vector3 c, Quaternion rot, int n, float tile, float lift = .02f, LandContext ground = null)
        {
            var w = n * tile; for (var x = 0; x < n; x++) for (var z = 0; z < n; z++) { var p = c + rot * new Vector3(-w / 2f + (x + .5f) * tile, lift, -w / 2f + (z + .5f) * tile); if (ground != null) p.y = ground.Ground(p.x, p.z) + lift; mk.Box((x + z) % 2 == 0 ? checkA : checkB, p, new Vector3(tile, .04f, tile), rot); }
        }

        /// The lowest ground in a square of half-size r round a point.
        static float LowUnder(LandContext ctx, Vector3 at, float r)
        {
            var lo = ctx.Ground(at.x, at.z);
            for (var i = -2; i <= 2; i++) for (var j = -2; j <= 2; j++) lo = Mathf.Min(lo, ctx.Ground(at.x + r * i / 2f, at.z + r * j / 2f));
            return lo;
        }

        /// How much the ground rises and falls across a square of half-size r round a point.
        static float Rise(LandContext ctx, float x, float z, float r)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (var i = -2; i <= 2; i++) for (var j = -2; j <= 2; j++) { var g = ctx.Ground(x + r * i / 2f, z + r * j / 2f); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            return hi - lo;
        }

        /// A marble bust on a column pedestal.
        void Bust(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, float s, System.Random rng)
        {
            var up = Vector3.up; face.y = 0; face.Normalize(); mk.Box(marble, g + up * .1f * s, new Vector3(.9f, .2f, .9f) * s, Quaternion.LookRotation(face)); mk.Prism(marble, g + up * .2f * s, up, .34f * s, 1.3f * s, 10); mk.Box(marble, g + up * 1.55f * s, new Vector3(.8f, .1f, .8f) * s, Quaternion.LookRotation(face));
            var b = g + up * 1.6f * s; mk.Blob(marble, b + up * .3f * s, new Vector3(.4f, .28f, .26f) * s, rng, 0f); mk.Prism(marble, b + up * .5f * s, up, .1f * s, .2f * s, 6); mk.Blob(marble, b + up * .9f * s, new Vector3(.24f, .3f, .26f) * s, rng, 0f); mk.Blob(Paint(Pink), b + up * 1.06f * s - face * .04f * s, new Vector3(.26f, .14f, .26f) * s, rng, .3f); mk.Blob(marble, b + up * .86f * s + face * .24f * s, new Vector3(.05f, .07f, .06f) * s, rng, 0f);
            ArtistBits.PostCollider(ctx.Root, g, 2.4f * s, .4f * s); noGrass.Add(new Vector3(g.x, .6f * s, g.z));
        }

        /// A Greek column, whole or broken off.
        void Column(LandContext ctx, MeshKit mk, Vector3 g, float h, bool broken)
        {
            var up = Vector3.up; mk.Box(marble, g + up * .15f, new Vector3(1f, .3f, 1f), Quaternion.identity); mk.Prism(marble, g + up * .3f, up, .36f, h, 12); for (var k = 0; k < 12; k++) mk.Box(Paint(new Color(.88f, .86f, .9f)), g + up * (.3f + h / 2f) + Quaternion.Euler(0, k * 30f, 0) * Vector3.forward * .36f, new Vector3(.06f, h, .02f), Quaternion.Euler(0, k * 30f, 0));
            if (!broken) mk.Box(marble, g + up * (h + .45f), new Vector3(1f, .3f, 1f), Quaternion.identity); else mk.Tri(marble, g + up * (h + .3f) + Vector3.right * .36f, g + up * (h + .7f), g + up * (h + .3f) - Vector3.right * .36f, g);
            ArtistBits.PostCollider(ctx.Root, g, h, .4f); noGrass.Add(new Vector3(g.x, .7f, g.z));
        }

        /// A glossy bubble: a sphere with a white highlight, sometimes floating.
        void Bubble(MeshKit mk, Vector3 c, float r)
        {
            mk.Blob(bubbleM, c, Vector3.one * r, new System.Random(1), 0f); mk.Blob(white, c + new Vector3(-.4f, .5f, -.3f).normalized * r * .7f, new Vector3(.3f, .16f, .3f) * r, new System.Random(2), 0f); halos.Add(c, r * 1.4f, Aqua, .14f);
        }

        /// A palm.
        void Palm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var lean = new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .3f; var prev = g; var dir = up;
            for (var k = 0; k < 6; k++) { dir = (up + lean * (k / 5f)).normalized; mk.Prism(trunkM, prev, dir, (.2f - k * .02f) * s, 1.3f * s, 6); prev += dir * 1.3f * s; }
            for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; var tip = prev + fd * 2.4f * s - up * 1f * s; var mid = prev + fd * 1.2f * s + up * .3f * s; var side = Vector3.Cross(up, fd) * .3f * s; foreach (var o in new[] { up, -up * 3f }) { mk.Tri(leafM, prev, mid + side, tip, prev + o); mk.Tri(leafM, prev, mid - side, tip, prev + o); } }
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s); noGrass.Add(new Vector3(g.x, .8f, g.z));
        }

        /// An old beige computer monitor, its screen glowing.
        void Monitor(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, float s = 1f)
        {
            var up = Vector3.up; face.y = 0; face.Normalize(); var rot = Quaternion.LookRotation(face); var beige = Paint(new Color(.86f, .82f, .72f));
            mk.Box(beige, g + up * .5f * s, new Vector3(1.2f, .9f, .9f) * s, rot); mk.Box(screenM, g + up * .55f * s + face * .46f * s, new Vector3(.9f, .66f, .02f) * s, rot); mk.Box(beige, g + up * .06f * s, new Vector3(.7f, .12f, .6f) * s, rot);
            BoxCol(ctx.Root, g + up * .5f * s, new Vector3(1.2f, 1f, .9f) * s, rot);
        }

        /// A door standing on its own in a field, slightly open, light behind it.
        void LoneDoor(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; face.y = 0; face.Normalize(); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            foreach (var e in new[] { -.55f, .55f }) mk.Box(white, g + up * 1.1f + side * e, new Vector3(.1f, 2.2f, .14f), rot); mk.Box(white, g + up * 2.25f, new Vector3(1.2f, .1f, .14f), rot); mk.Box(lampM, g + up * 1.1f - face * .02f, new Vector3(1f, 2.1f, .02f), rot);
            mk.Box(Paint(Pink), g + up * 1.1f + side * .5f + face * .4f, new Vector3(.04f, 2.1f, .9f), rot * Quaternion.Euler(0, -60f, 0)); halos.Add(g + up * 1.1f + face * .3f, 1.6f, new Color(1f, .9f, 1f), .3f); ArtistBits.PostCollider(ctx.Root, g + side * .55f, 2.2f, .08f); ArtistBits.PostCollider(ctx.Root, g - side * .55f, 2.2f, .08f);
        }

        /// A street lamp: a glossy white post and a round glowing globe.
        void GlobeLamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(chrome, g - up * .2f, up, .07f, 4f, 6, .6f); mk.Blob(lampM, g + up * 4f, Vector3.one * .3f, new System.Random(1), 0f); halos.Add(g + up * 4f, 1.6f, new Color(1f, .9f, 1f), .4f); ArtistBits.PostCollider(ctx.Root, g, 3.8f, .09f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 3.4f, new Color(1f, .86f, .96f), 14f, 1.6f);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var n = 0;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort; if (end < PathW) return;
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 16f) * Mathf.Min(1.6f, end * .03f);
                    q = RoundAreas(ctx, q, artist);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                for (var i = 0; i + 1 < pts.Count; i++) Strip(mk, new List<Vector3> { pts[i], pts[i + 1] }, i % 2 == 0 ? checkA : checkB, PathW, .13f + (walk % 4) * .012f + (i % 2) * .006f, ctx);   // each path, and each other square, a hair higher, so overlaps don't flicker
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f) || NearPath(lp.x, lp.z, 2f)) continue;
                    GlobeLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n % 3 == 0);
                }
                walk++;
            }
            // a lane bends round another artist's area instead of running across it, under their things
            Vector3 RoundAreas(LandContext c, Vector3 q, string own)
            {
                foreach (var a in c.Artists)
                {
                    if (a.name == own) continue; var v = new Vector3(q.x - a.x, 0, q.z - a.z); var r = Foot(a) + 1.5f;
                    if (v.sqrMagnitude >= r * r) continue; if (v.sqrMagnitude < .01f) v = Vector3.right;
                    var o = new Vector3(a.x, 0, a.z) + v.normalized * r; if (c.IsLand(o.x, o.z, 0f)) q = o;
                }
                return q;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                // a lane branches off before another artist's area, never from the end of a lane at its edge
                bool InArea(Vector3 p) { foreach (var o in ctx.Artists) { float dx = p.x - o.x, dz = p.z - o.z, r = Foot(o) + 1.5f; if (dx * dx + dz * dz < r * r) return true; } return false; }
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { if (InArea(p)) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- the dreamscape
        /// Marble busts, Greek columns, palms, glossy bubbles, dolphin pools, old monitors in the grass, lone doors, checker patches.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int busts = 0, columns = 0, palms = 0, bubbles = 0, pools = 0, monitors = 0, doors = 0, checks = 0; var rng = new System.Random(2000);
            const float cell = 21f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var up = Vector3.up;
                // busts, monitors and doors sit down to the lowest ground under them, so no edge hangs on a slope
                if (roll < .1f) { var bs = 1f + s * .8f; p.y = LowUnder(ctx, p, .45f * bs); Bust(ctx, mk, p, f, bs, rng); busts++; }
                else if (roll < .2f)
                {
                    // each column of a group stands on its own ground, and only where it is clear of paths and areas
                    for (var k = 0; k < 1 + rng.Next(3); k++) { var h = 3f + (float)rng.NextDouble() * 3f; var br = rng.NextDouble() < .4; var cq = p + Vector3.Cross(up, f) * (k * 3f); if (k > 0 && !Open(ctx, cq.x, cq.z, 1.5f)) continue; cq.y = LowUnder(ctx, cq, .5f); Column(ctx, mk, cq, h, br); }
                    columns++;
                }
                else if (roll < .38f) { Palm(ctx, mk, p, .9f + s * .6f, rng); palms++; }
                else if (roll < .48f) { Bubble(mk, p + up * (2f + s * 8f), .6f + s * 1.2f); bubbles++; }
                else if (roll < .54f) { if (!Open(ctx, px, pz, 6f) || Rise(ctx, px, pz, 3.4f) > .4f) continue; mk.Prism(marble, p - up * .1f, up, 3.4f, .4f, 20); mk.Prism(aquaM, p + up * .32f, up, 3.1f, .02f, 20); BoxCol(ctx.Root, p + up * .15f, new Vector3(6.8f, .3f, 6.8f), Quaternion.identity); var dk = new MeshKit(); Dolphin(dk, Vector3.zero, Paint(new Color(.6f, .7f, .86f))); var dol = dk.BuildSingle("Dolphin", ctx.Root, false); dol.transform.position = p; life.dolphins.Add((dol.transform, p, pools)); noGrass.Add(new Vector3(p.x, 3.8f, p.z)); pools++; }
                else if (roll < .6f) { p.y = LowUnder(ctx, p, .6f * (1f + s)); Monitor(ctx, mk, p, f, 1f + s); monitors++; }
                else if (roll < .65f) { p.y = LowUnder(ctx, p, .6f); LoneDoor(ctx, mk, p, f); doors++; }
                else if (roll < .72f) { if (Rise(ctx, px, pz, 4.2f) > 1.2f) continue; Checker(mk, p, Quaternion.LookRotation(f), 6, 1.4f, .03f, ctx); noGrass.Add(new Vector3(p.x, 5f, p.z)); checks++; }
            }
            Debug.Log($"The Isle of Memes: Superhighway {streetL:0} m, {busts} busts, {columns} column groups, {palms} palms, {bubbles} bubbles, {pools} dolphin pools, {monitors} monitors, {doors} lone doors, {checks} checker patches");
        }

        /// A leaping dolphin, its body arced (built at the origin and moved by MemeLife).
        void Dolphin(MeshKit mk, Vector3 c, Material m)
        {
            var up = Vector3.up; var prev = c; for (var k = 0; k < 6; k++) { var a = (k - 2.5f) * .3f; var p = c + new Vector3(0, Mathf.Cos(a) * .6f - .6f, Mathf.Sin(a) * 1.2f); if (k > 0) mk.Prism(m, prev, (p - prev).normalized, .22f - Mathf.Abs(k - 2.5f) * .05f, Vector3.Distance(prev, p) + .05f, 6); prev = p; }
            mk.Tri(m, c + new Vector3(0, .1f, 0), c + new Vector3(0, .4f, -.2f), c + new Vector3(0, .05f, -.4f), c + Vector3.right); mk.Tri(m, c + new Vector3(0, .1f, 0), c + new Vector3(0, .4f, -.2f), c + new Vector3(0, .05f, -.4f), c - Vector3.right);
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Soft green grass with pink and white flowers, in patches like a desktop wallpaper: smooth bright lawns,
        /// softer meadow grass, and drifts where the tips flower all pink or all white, bare ground between. Each
        /// patch its own shade, from a cool aqua-green to a warm wallpaper green; every blade a touch its own.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.4f, .72f, .5f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2001);
            const float st = 2.6f;
            Color cool = new Color(.34f, .62f, .52f), warm = new Color(.56f, .78f, .42f);
            int lawn = 0, meadow = 0, drifts = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (WindMesh.Patch(px, pz, .04f, 8f) < .46f) continue;                     // bare ground between patches
                var kind = WindMesh.Patch(px, pz, .016f, 5f); var tone = WindMesh.Patch(px, pz, .025f, 17f);
                var r = (float)rng.NextDouble();
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(cool, warm, Mathf.Clamp01((tone - .25f) * 2f)) * (.94f + r * .12f);
                var tip = Color.Lerp(col, new Color(.8f, 1f, .9f), .3f);
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                if (kind < .45f) { var lc = col * 1.06f; wm.Tuft(at, .16f + r * .14f, lc, Color.Lerp(lc, new Color(.8f, 1f, .9f), .25f), rng, 4, .07f, .1f); lawn++; }
                else if (kind < .75f) { wm.Tuft(at, .3f + r * .25f, col, tip, rng, 3, .08f, .1f); meadow++; }
                else
                {
                    var fl = WindMesh.Patch(px, pz, .03f, 29f) < .5f ? Pink : Marble;   // the drift's one flower
                    wm.Tuft(at, .24f + r * .3f, col, rng.NextDouble() < .6 ? fl : tip, rng, 3, .08f, .1f); drifts++;
                }
            }
            foreach (var kv in meshes) kv.Value.Build("Meme grass", ctx.Root, mat);
            Debug.Log($"The Isle of Memes grass: {lawn} lawn tufts, {meadow} meadow tufts, {drifts} flowering-drift tufts");
        }
    }
}
