using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Old Jazz Quarter (WORLD_GUIDE.md): a hill town in the rain, past midnight, in black and white. Everything
    /// here is black, grey and silver, like an old photograph of a jazz club: wet cobbles, tall dark houses with a few
    /// windows lit behind their blinds, pools of lamplight, steam from the gratings. Colour is kept for the music:
    /// the red neon of the club's marquee and the blue light inside it, the brass of the horns and the lamps, and
    /// gold at the top of the hill, where a temple stands above the rain.
    /// You arrive in Cobble Square, under the stopped clock. The Blue Mode (the club) is on one side. Lamp Street
    /// climbs from the square over the stone bridge, where somebody is always playing a saxophone to the river, and on
    /// up between the houses to the hilltop.
    /// Laid out on purpose (the build-land standard): JazzTown.cs has the places and the people.
    public partial class JazzKit : LandKit
    {
        public override string Land => "The Old Jazz Quarter";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Brass = new Color(1f, .8f, .42f), Red = new Color(1f, .14f, .16f), Blue = new Color(.3f, .52f, 1f), Gold = new Color(1f, .7f, .22f), Silver = new Color(.84f, .86f, .9f), Paper = new Color(.92f, .92f, .9f), Ink = new Color(.07f, .07f, .09f);
        static readonly Color[] Walls = { new Color(.3f, .31f, .34f), new Color(.21f, .22f, .25f), new Color(.4f, .41f, .44f), new Color(.17f, .18f, .21f), new Color(.5f, .5f, .53f), new Color(.26f, .26f, .29f) };
        const float PathW = 3.2f, StreetW = 5f, StreamHalf = 3.6f, StreamOff = 58f, HouseW = 10f, HouseD = 9f;

        Material cobble, stoneM, brassM, iron, dark, white, grey, leafM, leafDark, trunkM, waterM, puddleM, lampM, poolM, windowM, neonM, blueM, goldM, starM;
        // the marquee's bulbs: three sets, so they can chase
        readonly Material[] bulbM = new Material[3];
        GlowHalos halos; Vector3 center, U, R, streamC; JazzLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        // x, radius, z: the lamps along Lamp Street and the lanes, which a lane must not start through
        readonly List<Vector3> fixtures = new List<Vector3>();
        Vector2 heart2, U2, R2, stream2; float streamL, quayY, waterY, hillD;

        void MakeMaterials()
        {
            // the streets are faintly self-lit, so the night never swallows them
            cobble = WorldMaterials.Glow(new Color(.4f, .41f, .45f), .14f); stoneM = WorldMaterials.Glow(new Color(.62f, .63f, .67f), .12f);
            brassM = WorldMaterials.Glow(new Color(.9f, .68f, .28f), .18f); iron = WorldMaterials.Surface(new Color(.1f, .1f, .12f), Surface.Metal); dark = WorldMaterials.Solid(new Color(.05f, .05f, .06f));
            white = WorldMaterials.Glow(new Color(.93f, .93f, .92f), .1f); grey = WorldMaterials.Surface(new Color(.34f, .35f, .38f), Surface.Stone);
            leafM = WorldMaterials.Surface(new Color(.3f, .32f, .33f), Surface.Foliage); leafDark = WorldMaterials.Surface(new Color(.18f, .19f, .21f), Surface.Foliage); trunkM = WorldMaterials.Surface(new Color(.09f, .09f, .1f), Surface.Bark);
            waterM = new Material(WorldMaterials.Glow(new Color(.24f, .27f, .34f), .22f)) { name = "Jazz river" };
            puddleM = WorldMaterials.Glow(new Color(.5f, .54f, .62f), .2f);
            lampM = new Material(WorldMaterials.Glow(Brass, 1.8f)) { name = "Jazz lamps" };
            poolM = new Material(WorldMaterials.Glow(new Color(.62f, .56f, .42f), .3f)) { name = "Jazz lamp pools" };
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .95f, .8f), 1.05f)) { name = "Jazz windows" };
            neonM = new Material(WorldMaterials.Glow(Red, 2.2f)) { name = "Jazz neon" };
            blueM = new Material(WorldMaterials.Glow(Blue, 1.6f)) { name = "Jazz blue" };
            goldM = new Material(WorldMaterials.Glow(Gold, 1.3f)) { name = "Jazz gold" };
            starM = new Material(WorldMaterials.Glow(new Color(1f, .95f, .8f), 0f)) { name = "Jazz stars" };
            for (var i = 0; i < 3; i++) bulbM[i] = new Material(WorldMaterials.Glow(new Color(1f, .9f, .62f), 1.6f)) { name = "Jazz bulbs " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Alice Coltrane": return 17f;
                case "Miles Davis": return 15f;
                case "Duke Ellington": return 14f;
                case "Herbie Hancock": case "Billie Holiday": case "Chet Baker": return 11f;
                case "Louis Armstrong": return 12f;
                default: return HasStation(a) ? 12f : 8.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // rain-dark ground in greys: this land is a black and white photograph
            if (slope > 26f) color = Color.Lerp(new Color(.15f, .15f, .17f), new Color(.2f, .2f, .22f), patch);
            else color = Color.Lerp(new Color(.27f, .28f, .31f), new Color(.4f, .41f, .44f), Mathf.SmoothStep(0f, 1f, patch));
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float u, float r) => heart2 + U2 * u + R2 * r;

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // uphill: whichever way has room for the whole climb and ends highest
            U2 = new Vector2(0, 1); var bestH = float.MinValue;
            for (var k = 0; k < 8; k++)
            {
                var d = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var top = heart2 + d * 200f;
                if (!Inside(top, 70f) || !Inside(heart2 + d * 100f, 110f)) continue;
                var h = data.HeightAt(top.x, top.y); if (h > bestH) { bestH = h; U2 = d; }
            }
            R2 = new Vector2(U2.y, -U2.x); hillD = 200f; while (hillD > 120f && !Inside(heart2 + U2 * hillD, 70f)) hillD -= 10f;
            quayY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 5f);

            // the hill: lift the ground round the hilltop, as far as the land allows
            var top2 = heart2 + U2 * hillD; var hillR = 150f; while (hillR > 60f && !Inside(top2, hillR * .8f)) hillR -= 10f; var lift = hillR * .13f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - top2.y) > hillR) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - top2.x) > hillR) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), top2); if (d < hillR) data.heights[i] += lift * (1f - Ease(20f, hillR, d));
                }
            }

            // the river runs across the foot of the hill, as long as the land allows
            stream2 = heart2 + U2 * StreamOff; streamL = 110f;
            while (streamL > 40f && !(Inside(stream2 + R2 * (streamL + 16f), 26f) && Inside(stream2 - R2 * (streamL + 16f), 26f))) streamL -= 10f;

            var keep = new List<string>();
            void Put(string name, float u, float rr, float room) { var p = P2(u, rr); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // round the square: the club, the pond
            Put("Miles Davis", 0f, 42f, 30f); Put("Herbie Hancock", 4f, -44f, 28f); Put("Moondog", -40f, -30f, 24f);
            // downhill: the swing era
            Put("Duke Ellington", -66f, 8f, 30f); Put("Glenn Miller", -70f, 58f, 26f); Put("Cab Calloway", -106f, -22f, 26f); Put("Ella Fitzgerald", -114f, 26f, 26f); Put("Frank Sinatra", -74f, -70f, 26f); Put("Nat King Cole", -152f, -2f, 26f);
            Put("Louis Armstrong", -118f, 86f, 28f); Put("Preservation Hall Jazz Band", -156f, 46f, 24f);
            // east: soul jazz. west: the travellers
            Put("Cannonball Adderley", 6f, 94f, 26f); Put("Jimmy Smith", -34f, 102f, 26f); Put("Les McCann", 30f, 140f, 26f); Put("Donald Byrd", -20f, 148f, 26f);
            Put("Django Reinhardt", 10f, -96f, 26f); Put("The Dave Brubeck Quartet", -34f, -106f, 26f); Put("Stan Getz", 26f, -144f, 26f);
            // over the bridge, and up the hill
            Put("Sonny Rollins", 80f, 27f, 24f); Put("Sonny Rollins Quartet", 80f, -27f, 24f);
            Put("Billie Holiday", 118f, 98f, 30f); Put("Chet Baker", 112f, -98f, 30f); Put("Pharoah Sanders", hillD - 44f, 58f, 26f); Put("Joe Henderson", hillD - 44f, -58f, 26f);
            Put("Alice Coltrane", hillD, 0f, 34f); Put("Zoh Amba", hillD + 16f, 64f, 26f);
            var avoid = new List<Vector3>();
            for (var d = -streamL - 10f; d <= streamL + 10f; d += 22f) avoid.Add(new Vector3(stream2.x + R2.x * d, 30f, stream2.y + R2.y * d));
            for (var d = 20f; d < hillD; d += 24f) avoid.Add(new Vector3(heart2.x + U2.x * d, 26f, heart2.y + U2.y * d));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 270f);   // (all inside the rain globe: JazzGlobe)

            // level the square and the river's banks to one height, then cut the river into them
            Level(data, index, heart2, 22f, 38f, quayY);
            var half = StreamHalf + 5f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var x = col * data.Step; var rel = new Vector2(x, z) - stream2; var al = Mathf.Abs(Vector2.Dot(rel, R2)); var pe = Mathf.Abs(Vector2.Dot(rel, U2));
                    if (al > streamL + 30f || pe > half + 14f) continue;
                    var w = (1f - Ease(half, half + 14f, pe)) * (1f - Ease(streamL + 12f, streamL + 30f, al));
                    data.heights[i] = Mathf.Lerp(data.heights[i], quayY, w);
                    if (al < streamL && pe < StreamHalf + 1.2f) data.heights[i] = Mathf.Lerp(quayY - 3f, data.heights[i], Ease(StreamHalf - .2f, StreamHalf + 1.2f, pe));
                }
            }
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
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear(); fixtures.Clear();
            center = ctx.Plaza; U = new Vector3(U2.x, 0, U2.y); R = new Vector3(R2.x, 0, R2.y); streamC = new Vector3(stream2.x, quayY, stream2.y);
            life = new GameObject("Jazz life").AddComponent<JazzLife>();
            life.transform.SetParent(ctx.Root, false);
            life.globeAt = new Vector3(heart2.x, quayY, heart2.y); life.globeR = GlobeR;
            life.center = center; life.lamps = lampM; life.pools = poolM; life.windows = windowM; life.neon = neonM; life.blue = blueM; life.gold = goldM; life.stars = starM; life.bulbs = bulbM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Square(ctx, mk, solid);
            River(ctx, mk, solid);
            LampStreet(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Houses(ctx, mk, solid);
            // the rain globe (JazzGlobe): doors planned first, then the town grown out to the glass, then the glass
            PlanDoors(ctx);
            RingStreets(ctx, mk, solid);
            Globe(ctx, mk, solid);
            try { PoolHall(ctx, mk, solid); } catch (System.Exception e) { Debug.LogWarning("The Side Pocket (pool) could not be built (the rest of the Quarter is unaffected): " + e); }   // new and not yet played: it must never take the land down with it
            Wilds(ctx, mk, solid);
            GlobeStars(ctx, mk);
            mk.Build("The Old Jazz Quarter", ctx.Root, false);
            solid.BuildCollider("The Old Jazz Quarter walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Jazz Quarter glow", ctx.Root); }

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

        /// Inside the river's corridor (the water and its banks), plus 'pad'.
        bool InRiver(float x, float z, float pad)
        {
            var rel = new Vector2(x, z) - stream2; return Mathf.Abs(Vector2.Dot(rel, R2)) < streamL + 4f + pad && Mathf.Abs(Vector2.Dot(rel, U2)) < StreamHalf + 2.4f + pad;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InRiver(x, z, pad) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

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
                Vector3 l = pts[i] - side * (w / 2f * miter * SoftEdges.Edge(pts[i], w, 1f)), r = pts[i] + side * (w / 2f * miter * SoftEdges.Edge(pts[i], w, 2f));
                var bulge = Mathf.Clamp(pts[i].y - (ground.Ground(l.x, l.z) + ground.Ground(r.x, r.z)) / 2f, 0f, .3f);
                for (var k = 0; k <= lanes; k++) { var q = Vector3.Lerp(l, r, k / (float)lanes); q.y = ground.Ground(q.x, q.z) + bulge + lift; V[i, k] = q; }
            }
            for (var i = 0; i + 1 < n; i++) for (var k = 0; k < lanes; k++) mk.Quad(m, V[i, k], V[i, k + 1], V[i + 1, k + 1], V[i + 1, k], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A street lamp: a tall iron post, a hooked arm, a shade, and a pool of light on the wet ground under it.
        void StreetLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 arm, bool light = false)
        {
            var up = Vector3.up; arm.y = 0; arm = arm.sqrMagnitude > .01f ? arm.normalized : Vector3.forward;
            mk.Prism(iron, g - up * .2f, up, .07f, 4.4f, 6); mk.Prism(iron, g, up, .18f, .4f, 8, .5f); mk.Prism(iron, g + up * 4.1f, (arm + up * .25f).normalized, .035f, 1f, 5);
            var lp = g + up * 4.1f + arm * .95f; mk.Cone(iron, lp + up * .02f, .34f, .26f, 8); mk.Blob(lampM, lp - up * .08f, new Vector3(.16f, .12f, .16f), new System.Random(7), 0f); halos.Add(lp - up * .1f, 2.2f, Brass, .6f); ArtistBits.PostCollider(ctx.Root, g, 4.2f, .1f);
            var pool = ctx.OnGround(lp.x, lp.z); float lo = pool.y, hi = pool.y;
            for (var k = 0; k < 4; k++) { var gy = ctx.Ground(pool.x + (k == 0 ? 1.5f : k == 1 ? -1.5f : 0f), pool.z + (k == 2 ? 1.5f : k == 3 ? -1.5f : 0f)); lo = Mathf.Min(lo, gy); hi = Mathf.Max(hi, gy); }
            if (hi - lo < .16f) mk.Prism(poolM, pool + up * .17f, up, 1.7f, .015f, 14);
            if (light) ArtistBits.PointLight(ctx.Root, lp - up * .6f, Brass, 17f, 2.2f);
        }

        /// A string of bulbs from a to b, sagging; they take the three bulb materials in turn.
        void BulbString(MeshKit mk, Vector3 a, Vector3 b, int phase = 0)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(3, Mathf.RoundToInt(len / 1.1f)); var sag = Mathf.Min(1.2f, len * .07f); var prev = a;
            for (var i = 1; i <= n; i++)
            {
                var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag;
                mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p;
                if (i < n) { mk.Blob(bulbM[(i + phase) % 3], p - Vector3.up * .08f, Vector3.one * .075f, new System.Random(i), 0f); if (i % 4 == 0) halos.Add(p - Vector3.up * .08f, .7f, Brass, .3f); }
            }
        }

        /// A bare black tree, wet, with a little grey leaf left on it.
        void BareTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .3f, up, .22f * s, 3.2f * s, 6, .6f); ArtistBits.PostCollider(ctx.Root, g, 3f * s, .24f * s);
            for (var k = 0; k < 5; k++)
            {
                var d = (Quaternion.Euler(0, k * 72f + (float)rng.NextDouble() * 50f, 0) * Vector3.forward + up * (.7f + (float)rng.NextDouble() * .6f)).normalized; var b = g + up * (2.4f + k * .2f) * s; var len = (2f + (float)rng.NextDouble() * 1.4f) * s;
                mk.Prism(trunkM, b, d, .08f * s, len, 4, .3f); var tip = b + d * len; var d2 = (d + Quaternion.Euler(0, 90f, 0) * d * .6f).normalized; mk.Prism(trunkM, b + d * len * .55f, d2, .045f * s, len * .6f, 3, .3f);
                if (rng.Next(3) != 0) mk.Blob(k % 2 == 0 ? leafM : leafDark, tip, new Vector3(.9f, .6f, .9f) * s, rng, .3f);
            }
        }

        /// A bench to sit on: iron ends, dark slats.
        void Bench(MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(grey, g + up * .45f, new Vector3(len, .07f, .5f), rot); mk.Box(grey, g + up * .82f - face * .24f, new Vector3(len, .4f, .06f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .32f + side * e * (len / 2f - .12f) - face * .08f, new Vector3(.06f, .95f, .5f), rot);
            BoxCol(rootT, g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .4f), g + up * .52f + side * (len / 2f - .4f), face);
        }

        /// A bentwood café chair to sit on.
        void Chair(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Prism(dark, g + up * .43f, up, .22f, .05f, 10); mk.Prism(iron, g, up, .03f, .45f, 4); mk.Box(dark, g + up * .8f - face * .2f, new Vector3(.4f, .5f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face);
        }

        /// A small round cocktail table with a candle on it.
        void Table(LandContext ctx, MeshKit mk, Vector3 g, bool candle = true)
        {
            var up = Vector3.up; mk.Prism(iron, g, up, .04f, .72f, 5); mk.Prism(iron, g, up, .22f, .03f, 8); mk.Prism(white, g + up * .72f, up, .4f, .04f, 12); ArtistBits.PostCollider(ctx.Root, g, .8f, .32f);
            if (candle) { mk.Prism(white, g + up * .76f, up, .03f, .1f, 6); mk.Blob(lampM, g + up * .9f, new Vector3(.03f, .05f, .03f), new System.Random(3), 0f); halos.Add(g + up * .9f, .7f, Brass, .5f); }
        }

        /// A saxophone: a brass tube, a curve, a bell. 'o' is its foot; it leans back a little.
        void Sax(MeshKit mk, Vector3 o, Quaternion rot, Material m, float s = 1f)
        {
            var up = Vector3.up; var lean = (rot * new Vector3(0, 1f, -.22f)).normalized;
            mk.Prism(m, o + up * .25f * s, lean, .045f * s, .75f * s, 6, .6f); mk.Prism(m, o + up * .25f * s + lean * .75f * s, (lean + rot * Vector3.forward * .8f).normalized, .02f * s, .22f * s, 4);
            mk.Prism(m, o + up * .12f * s + rot * Vector3.forward * .1f * s, (rot * new Vector3(0, .5f, -1f)).normalized, .05f * s, .22f * s, 6); mk.Prism(m, o + up * .2f * s + rot * Vector3.forward * .2f * s, (up + rot * Vector3.forward * .25f).normalized, .05f * s, .3f * s, 8, 2.4f);
        }

        /// A trumpet lying level: 'o' is its middle, pointing along rot's forward.
        void Trumpet(MeshKit mk, Vector3 o, Quaternion rot, Material m)
        {
            var f = rot * Vector3.forward; mk.Prism(m, o - f * .3f, f, .018f, .5f, 5); mk.Prism(m, o + f * .2f, f, .025f, .18f, 8, 3.6f); mk.Box(m, o - f * .05f + Vector3.up * .04f, new Vector3(.05f, .1f, .16f), rot);
        }

        /// An upright bass standing on its spike.
        void Bass(MeshKit mk, Vector3 g, Quaternion rot, System.Random rng)
        {
            var up = Vector3.up; var wood = WorldMaterials.Surface(new Color(.3f, .22f, .16f), Surface.Wood); mk.Prism(iron, g, up, .012f, .25f, 4); mk.Blob(wood, g + up * .78f, new Vector3(.34f, .58f, .15f), rng, .04f); mk.Box(dark, g + up * 1.62f, new Vector3(.07f, 1f, .05f), rot); mk.Box(wood, g + up * 2.16f, new Vector3(.1f, .16f, .08f), rot);
        }

        /// A grand piano, lid up. 'g' is under its middle, the keys on its front (rot's forward).
        void Grand(Transform parent, MeshKit mk, Vector3 g, Quaternion rot, Material body, Material keys)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; var s = rot * Vector3.right;
            mk.Box(body, g + up * .86f - f * .1f, new Vector3(1.5f, .3f, 1.9f), rot); mk.Box(body, g + up * 1.36f - f * .2f - s * .5f, new Vector3(1.5f, .05f, 1.7f), rot * Quaternion.Euler(0, 0, 38f));
            mk.Box(keys ?? white, g + up * .88f + f * .92f, new Vector3(1.4f, .05f, .22f), rot); mk.Box(dark, g + up * .92f + f * .86f, new Vector3(1.4f, .04f, .1f), rot);
            foreach (var (x, zz) in new[] { (-.62f, .7f), (.62f, .7f), (0f, -.9f) }) mk.Prism(body, g + s * x + f * zz, up, .05f, .72f, 5);
            BoxCol(parent, g + up * .6f - f * .1f, new Vector3(1.5f, 1.2f, 1.9f), rot);
        }

        /// A microphone on a stand.
        void Mic(Transform parent, MeshKit mk, Vector3 g, Material head = null)
        {
            var up = Vector3.up; mk.Prism(iron, g, up, .2f, .03f, 8); mk.Prism(iron, g, up, .018f, 1.5f, 4); mk.Blob(head ?? WorldMaterials.Surface(Silver, Surface.Metal), g + up * 1.56f, new Vector3(.06f, .09f, .06f), new System.Random(2), 0f); ArtistBits.PostCollider(parent, g, 1.5f, .06f);
        }

        /// A puddle: a patch of sky lying on the ground.
        void Puddle(MeshKit mk, Vector3 g, float r, System.Random rng)
        {
            mk.Prism(puddleM, g + Vector3.up * .05f, Vector3.up, r, .012f, 9); mk.Prism(puddleM, g + Vector3.up * .05f + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * r * 1.4f, Vector3.up, r * .6f, .012f, 7);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var lamps = new List<Vector3>();
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>(); var fromSquare = new Vector2(join.x - center.x, join.z - center.z).magnitude < 21f;
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(2.5f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    // bend round any other artist's area (and the square, unless the lane starts on it) instead of
                    // running across it: an arc on whichever side the straight line passes, so the lane never flips across
                    void Round(Vector3 ov, float r)
                    {
                        var rel = q - ov; if (rel.sqrMagnitude >= r * r) return;
                        var along = Vector3.Dot(rel, dir); var sgn = Vector3.Dot(from - ov, side) >= 0f ? 1f : -1f;
                        q = ov + dir * along + side * sgn * Mathf.Sqrt(Mathf.Max(0f, r * r - along * along));
                    }
                    foreach (var o in ctx.Artists) if (o.name != artist) Round(new Vector3(o.x, 0, o.z), Foot(o) * 1.06f + 3f);
                    if (!fromSquare) Round(new Vector3(center.x, 0, center.z), 20f + PathW / 2f);
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, cobble, PathW, .14f, ctx);
                // a lamp now and then (never on another lane or the street)
                for (var i = 8; i < pts.Count; i += 13)
                {
                    var s = (i / 13) % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 17f * 17f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f) || InRiver(lp.x, lp.z, 1f) || NearPath(lp.x, lp.z, PathW / 2f + .4f)) continue;
                    lamps.Add(lp); fixtures.Add(new Vector3(lp.x, .3f, lp.z)); StreetLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * s, true);
                }
                foreach (var p in pts) Remember(p, walk);
                walk++;
            }
            // would a lane leaving p toward a run into a lamp along Lamp Street or another lane in its first stretch?
            bool Blocked(Vector3 p, IslandWorldData.Artist a)
            {
                var d = new Vector2(a.x - p.x, a.z - p.z); var len = Mathf.Min(12f, d.magnitude); if (len < .1f) return false; d /= d.magnitude;
                foreach (var f in fixtures)
                {
                    var rel = new Vector2(f.x - p.x, f.z - p.z); var t = Mathf.Clamp(Vector2.Dot(rel, d), 0f, len);
                    if ((rel - d * t).sqrMagnitude < (f.y + PathW / 2f + .2f) * (f.y + PathW / 2f + .2f)) return true;
                }
                return false;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p0, w) in walkPoints)
                    {
                        // a lane never crosses the river: it joins from its own side, and never from the bridge's deck
                        var sideA = Vector2.Dot(new Vector2(a.x, a.z) - stream2, U2) > 0f; var sideP = Vector2.Dot(new Vector2(p0.x, p0.z) - stream2, U2) > 0f; if (sideA != sideP) continue;
                        if (InRiver(p0.x, p0.z, 1f)) continue;
                        // never branch from inside another artist's area; from the square, start at the edge of its
                        // cobbles (not across the middle, past the clock, the benches, the news stand or the car)
                        if (w >= 10 && InFootprint(ctx, p0.x, p0.z, 2f)) continue;
                        var p = p0;
                        if (w == 0) { var o = new Vector3(a.x - center.x, 0, a.z - center.z); if (o.sqrMagnitude < 1f) continue; p = ctx.OnGround(center.x + o.normalized.x * 19.6f, center.z + o.normalized.z * 19.6f); }
                        var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a);
                        if (d < best && !Blocked(p, a)) { best = d; next = a; join = p; }
                    }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- out of town
        /// Bare trees, lamps with a bench under them, puddles, a phone box now and then, a parked car.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, lampsN = 0, puddles = 0, boxes = 0, cars = 0; var rng = new System.Random(1959);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz);
                // how much the ground falls across a round of radius r round 'at'; steep spots are skipped without using
                // up a different count of random numbers, so the rest of the layout stays put
                float Fall(Vector3 at, float r) { float lo = at.y, hi = at.y; for (var k = 0; k < 8; k++) { var h = ctx.Ground(at.x + Mathf.Cos(k * Mathf.PI / 4f) * r, at.z + Mathf.Sin(k * Mathf.PI / 4f) * r); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); } return hi - lo; }
                if (roll < .2f) { BareTree(ctx, mk, p, .9f + s * .7f, rng); trees++; }
                else if (roll < .3f) { if (Fall(p, 1.2f + s * 1.6f) < .2f && InGlobe(px, pz, -4f)) { Puddle(mk, p, 1.2f + s * 1.6f, rng); puddles++; } else { rng.NextDouble(); rng.NextDouble(); } }   // (dry outside the glass)
                else if (roll < .335f)
                {
                    // the bench stands on its own ground, and only where that is flat
                    var f = FaceTo(p, NearestWalk(p)); var bq = p + f * 1.3f + Vector3.Cross(Vector3.up, f) * .2f; var bp = ctx.OnGround(bq.x, bq.z);
                    if (Fall(bp, 1.2f) > .35f) continue;
                    StreetLamp(ctx, mk, p, f, lampsN % 3 == 0); Bench(mk, bp, f); lampsN++;
                }
                else if (roll < .38f) { if (boxes < 5 && NearPath(px, pz, 26f) && Fall(p, .8f) < .3f) { PhoneBox(ctx, mk, p, FaceTo(p, NearestWalk(p)), boxes); boxes++; } }
                else if (roll < .42f) { if (cars < 4 && NearPath(px, pz, 24f) && Fall(p, 2.5f) < .45f) { Sedan(ctx, mk, p, Quaternion.Euler(0, 90f, 0) * FaceTo(p, NearestWalk(p)), cars); cars++; } }
            }
            Debug.Log($"The Old Jazz Quarter: river {streamL * 2f:0} m long, hill {hillD:0} m up, {trees} bare trees, {puddles} puddles, {lampsN} lamps with benches, {boxes} phone boxes, {cars} parked cars");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// The stars that come out over the hill when the rain stops (JazzLife lights them).
        void Stars(LandContext ctx, MeshKit mk)
        {
            var rng = new System.Random(1971); var top = center + U * hillD;
            for (var k = 0; k < 170; k++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var d = 60f + (float)rng.NextDouble() * 420f; var h = 120f + (float)rng.NextDouble() * 160f;
                mk.Blob(starM, top + new Vector3(Mathf.Cos(a) * d, h, Mathf.Sin(a) * d), Vector3.one * (.5f + (float)rng.NextDouble() * 1.1f), rng, 0f);
            }
        }

        /// The verges: rain-silvered grass, in patches like an old photograph: short trimmed verge, rough dark long
        /// grass, and patches where the rain has silvered every tip, wet bare ground between. Still black and white:
        /// the patches differ only by brightness, and each blade by a touch (a small jitter, so no colour creeps in).
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .9f, new Color(.3f, .31f, .33f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1960);
            const float st = 2.6f;
            Color dark = new Color(.15f, .16f, .18f), light = new Color(.32f, .33f, .35f);
            int verge = 0, rough = 0, silvered = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || InRiver(px, pz, 1f) || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (WindMesh.Patch(px, pz, .04f, 3f) < .44f) continue;                     // wet bare ground between patches
                var kind = WindMesh.Patch(px, pz, .016f, 7f); var tone = WindMesh.Patch(px, pz, .025f, 12f);
                var r = (float)rng.NextDouble();
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(dark, light, Mathf.Clamp01((tone - .2f) * 1.7f)) * (.9f + r * .2f);
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                if (kind < .4f) { wm.Tuft(at, .16f + r * .16f, col, Color.Lerp(col, Silver, .3f), rng, 3, .05f, .06f); verge++; }
                else if (kind < .72f) { var dc = col * .8f; wm.Tuft(at, .4f + r * .35f, dc, Color.Lerp(dc, Silver, .25f), rng, 4, .05f, .06f); rough++; }
                else
                {
                    wm.Tuft(at, .26f + r * .3f, col, Color.Lerp(col, Silver, .72f), rng, 3, .05f, .06f);
                    if (r < .4f) wm.Tuft(at + new Vector3(.35f, 0, -.3f), .22f + r * .3f, col, Color.Lerp(col, Silver, .6f), rng, 3, .05f, .06f);
                    silvered++;
                }
            }
            foreach (var kv in meshes) kv.Value.Build("Jazz Quarter verge", ctx.Root, mat);
            Debug.Log($"The Old Jazz Quarter verges: {verge} trimmed tufts, {rough} rough-grass tufts, {silvered} rain-silvered spots");
        }
    }
}
