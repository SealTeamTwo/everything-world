using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Kawaii Cascades (WORLD_GUIDE.md): a mountain of cake under a bright pastel sky, and the one land where you
    /// move at hyper speed. The ground is cut into terraces like the layers of a cake (blueberry-dark on the flat, bands
    /// of lemon, pink, cream and soda blue on the risers), stepping down from the summit where you arrive to a sugar
    /// beach. On the land you run more than twice as fast, jump high, jump again in the air and kick off walls
    /// (SimplePlayerController.Hyper), and the land is built for it: four Runs go out from the summit, each a road of
    /// boost strips with a candy-ribbon rail overhead from the top of the Parfait (a sundae glass as tall as a tower,
    /// climbed by a spiral of macarons or one soda geyser) down to the shore, and a return geyser at the far end.
    /// Jelly pads, geysers, donut arches, floating macarons, cupcakes and lollipop trees fill the rest.
    /// KawaiiCourse.cs has the Parfait, the Runs and the wilds; KawaiiArtists.cs the artists; KawaiiLife.cs what moves.
    public partial class KawaiiKit : LandKit
    {
        public override string Land => "The Kawaii Cascades";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Yellow = new Color(1f, .78f, .12f), Pink = new Color(1f, .38f, .68f), Soda = new Color(.25f, .74f, 1f), Berry = new Color(.1f, .14f, .4f),
            Mint = new Color(.3f, .92f, .66f), Cream = new Color(1f, .93f, .8f), Cherry = new Color(.95f, .12f, .28f), Ink = new Color(.07f, .08f, .22f), Lilac = new Color(.62f, .48f, 1f);
        public static readonly Color[] Sweet = { Yellow, Pink, Soda, Mint, Cream, Lilac };
        const float PathW = 3.2f, RoadW = 6f, Tier = 7f;

        Material berry, berryD, cream, pinkM, yellowM, sodaM, mintM, lilacM, cherryM, white, stick, glass, lampM, jellyM, railM, milkM, starM;
        GlowHalos halos; Vector3 center, D, S; KawaiiLife life; HyperCourse course; MeshKit plates;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();   // x, radius, z
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0), parfait2; float plazaY, waterY, parfaitY;
        // the four Runs: which way each goes from the summit, how far, and its name and colour
        readonly Vector2[] runDir = new Vector2[4]; readonly float[] runLen = new float[4];
        static readonly string[] RunName = { "STRAWBERRY RUN", "LEMON RUN", "SODA RUN", "MINT RUN" };
        static readonly Color[] RunColor = { Pink, Yellow, Soda, Mint };

        void MakeMaterials()
        {
            berry = WorldMaterials.Surface(Berry, Surface.Painted); berryD = WorldMaterials.Surface(Berry * .7f, Surface.Painted); cream = WorldMaterials.Glow(Cream, .08f); white = WorldMaterials.Glow(new Color(.96f, .96f, 1f), .1f);
            pinkM = WorldMaterials.Glow(Pink, .14f); yellowM = WorldMaterials.Glow(Yellow, .14f); sodaM = WorldMaterials.Glow(Soda, .14f); mintM = WorldMaterials.Glow(Mint, .12f); lilacM = WorldMaterials.Glow(Lilac, .14f); cherryM = WorldMaterials.Glow(Cherry, .2f);
            stick = WorldMaterials.Surface(new Color(.95f, .93f, .9f), Surface.Painted); glass = WorldMaterials.Glow(new Color(.75f, .95f, 1f), .5f);
            lampM = new Material(WorldMaterials.Glow(Yellow, 1.5f)) { name = "Kawaii lamps" }; jellyM = new Material(WorldMaterials.Glow(new Color(1f, .4f, .7f), 1.1f)) { name = "Kawaii jelly" };
            railM = new Material(WorldMaterials.Glow(Soda, 1.3f)) { name = "Kawaii rails" }; milkM = new Material(WorldMaterials.Glow(new Color(1f, .5f, .7f), .1f)) { name = "Kawaii milk" }; starM = new Material(WorldMaterials.Glow(Yellow, 1.8f)) { name = "Kawaii stars" };
        }

        Material GlowM(Color c, float s = 1.4f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .1f);
        Material SweetM(int k) { switch (((k % 6) + 6) % 6) { case 0: return yellowM; case 1: return pinkM; case 2: return sodaM; case 3: return mintM; case 4: return cream; default: return lilacM; } }
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Flay!" ? 14f : a.songs >= 2 ? 10f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // the cake: blueberry-dark flats, bright bands on the risers (one colour per tier), a cream-sugar beach
            if (height < 2.4f) color = Color.Lerp(new Color(1f, .93f, .82f), new Color(1f, .82f, .86f), patch);
            else if (slope > 1.7f)
            {
                // (slope is how many metres a patch of ground rises: the risers between tiers are far steeper than the flats)
                var band = Mathf.FloorToInt((height + Tier * .3f) / Tier) % 4;
                color = band == 0 ? new Color(1f, .8f, .2f) : band == 1 ? new Color(1f, .45f, .72f) : band == 2 ? new Color(1f, .95f, .85f) : new Color(.35f, .8f, 1f);
                color = Color.Lerp(color, color * .88f, patch);
            }
            else color = Color.Lerp(new Color(.13f, .18f, .5f), new Color(.2f, .25f, .62f), patch);
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float d, float s) => heart2 + D2 * d + S2 * s;
        Vector3 Wp(float d, float s) => center + D * d + S * s;
        Vector3 G(float x, float z) => ctx0.OnGround(x, z);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        Vector3 V3(Vector2 p) => new Vector3(p.x, 0, p.y);

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            // the three rings: the Cake in the middle, the Milk round it, the Crust along the shore (KawaiiMilk.cs)
            ShapeZones(data, index);
            // the Cake's tiers: every seven metres of height becomes a flat with a short steep riser up to the next
            for (var i = 0; i < data.heights.Length; i++)
            {
                if (zone[i] != 3) continue; var h = Mathf.Max(data.heights[i], CakeMin + .2f);
                var b = Mathf.Floor(h / Tier) * Tier; var f = (h - b) / Tier;
                data.heights[i] = Mathf.Max(CakeMin, b + .4f * f + Ease(.72f, 1f, f) * (Tier - .4f));
            }
            plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            // the Parfait stands a little way off the plaza, toward the widest stretch of milk
            var far = 0f;
            for (var k = 0; k < 12; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 6f), Mathf.Cos(k * Mathf.PI / 6f)); var len = March(dir, 1, 200f); if (len > far) { far = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); parfait2 = P2(62f, 0);
            // the Runs, four ways out from the summit to the Cake's edge; their rails go on over the milk to the Crust.
            // The courses come back between them.
            for (var r = 0; r < 4; r++)
            {
                var ang = (r * 90f + 135f) * Mathf.Deg2Rad; var dir = (D2 * Mathf.Cos(ang) + S2 * Mathf.Sin(ang)).normalized;
                runDir[r] = dir; runLen[r] = Mathf.Max(60f, March(dir, 3, 60f, true) - 8f);
                var crustIn = March(dir, 1, runLen[r]); runCrust[r] = heart2 + dir * ((crustIn < 0f ? runLen[r] + 120f : crustIn) + 14f);
            }
            for (var w = 0; w < 4; w++) courseDir[w] = (runDir[w] + runDir[(w + 1) % 4]).normalized;
            // a causeway from each bridge: from the Crust's inner edge straight to the Cake
            causeways.Clear();
            foreach (var b in data.layout.bridges)
            {
                if (b.a != index && b.b != index) continue; var end = b.a == index ? new Vector2(b.ax, b.az) : new Vector2(b.bx, b.bz);
                var dir = (end - heart2).normalized; var cake = March(dir, 3, 60f, true); var crustIn = March(dir, 1, cake + 8f); if (cake < 0f || crustIn < 0f) continue;
                causeways.Add((heart2 + dir * (crustIn + 5f), heart2 + dir * (cake - 5f)));
            }
            var avoid = new List<Vector3> { new Vector3(parfait2.x, 34f, parfait2.y) };
            for (var d = 0f; d <= 62f; d += 16f) avoid.Add(new Vector3(heart2.x + D2.x * d, 12f, heart2.y + D2.y * d));
            for (var r = 0; r < 4; r++) for (var d = 20f; d <= runLen[r] + 16f; d += 14f) { var p = heart2 + runDir[r] * d; avoid.Add(new Vector3(p.x, 13f, p.y)); }
            for (var w = 0; w < 4; w++) { var e = March(courseDir[w], 3, 60f, true); var p = heart2 + courseDir[w] * (e - 8f); avoid.Add(new Vector3(p.x, 14f, p.y)); }
            foreach (var (_, cake2) in causeways) avoid.Add(new Vector3(cake2.x, 16f, cake2.y));
            // (the milk is not ground you can stand on, so the artists settle on the Cake)
            foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId) a.r = Mathf.Max(a.r, Foot(a) + 6f);
            ArtistPlacement.Spread(data, index, 52f, heart2, 44f, 40f, avoid, null, Land, 185f);
            Level(data, index, heart2, 24f, 36f, plazaY);
            parfaitY = Mathf.Max(data.HeightAt(parfait2.x, parfait2.y), CakeMin);
            Level(data, index, parfait2, 22f, 32f, parfaitY);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                // each artist stands on a tier: the flat of the tier their spot is nearest, with room to stand back from it
                var tier = Mathf.Max(Mathf.Round(data.HeightAt(a.x, a.z) / Tier) * Tier, CakeMin);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 5f, Foot(a) + 20f, tier);
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
                    var i = r * data.Nx + col; if (data.islandIds[i] != index || zone[i] != 3) continue;   // only the Cake is levelled: its sides stay sheer
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
            plates = new MeshKit();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Kawaii life").AddComponent<KawaiiLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.jelly = jellyM; life.rails = railM; life.milk = milkM; life.stars = starM;
            course = life.gameObject.AddComponent<HyperCourse>(); life.course = course;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Plaza(ctx, mk, solid);
            Parfait(ctx, mk, solid);
            Runs(ctx, mk, solid);
            Causeways(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Moat(ctx, mk);
            Courses(ctx, mk, solid);
            Edging(ctx, mk, solid);
            People(ctx);
            mk.Build("The Kawaii Cascades", ctx.Root, false);
            solid.BuildCollider("The Kawaii Cascades walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); plates.Build("Kawaii pad plates", ctx.Root, false); halos.Build("Kawaii glow", ctx.Root); }

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

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

        /// How steep the ground is round a point: the most it rises or falls within 'r' metres.
        float Drop(Vector3 at, float r)
        {
            float lo = at.y, hi = at.y;
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var h = ctx0.Ground(at.x + Mathf.Cos(a) * r, at.z + Mathf.Sin(a) * r); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo;
        }

        float Lowest(Vector3 at, float r)
        {
            var lo = ctx0.Ground(at.x, at.z);
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; lo = Mathf.Min(lo, ctx0.Ground(at.x + Mathf.Cos(a) * r, at.z + Mathf.Sin(a) * r)); }
            return lo;
        }

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground)
        {
            // laid in narrow lanes across its width, every vertex on its own ground, so it follows the tiers
            var n = pts.Count; if (n < 2) return; var lanes = Mathf.Max(1, Mathf.CeilToInt(w / 1.2f)); var V = new Vector3[n, lanes + 1];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                Vector3 l = pts[i] - side * (w / 2f * miter), r = pts[i] + side * (w / 2f * miter);
                for (var k = 0; k <= lanes; k++) { var q = Vector3.Lerp(l, r, k / (float)lanes); q.y = ground.Ground(q.x, q.z) + lift; V[i, k] = q; }
            }
            for (var i = 0; i + 1 < n; i++) for (var k = 0; k < lanes; k++) mk.Quad(m, V[i, k], V[i, k + 1], V[i + 1, k + 1], V[i + 1, k], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A lollipop lamp: a white stick, a glowing round sweet on top.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false, int k = 0)
        {
            var up = Vector3.up; var lo = Mathf.Min(g.y, Lowest(g, .2f)) - .1f; mk.Prism(stick, new Vector3(g.x, lo, g.z), up, .07f, g.y + 2.3f - lo, 6);
            mk.Prism(lampM, g + up * 2.7f + Vector3.forward * .09f, -Vector3.forward, .5f, .18f, 12); mk.Prism(SweetM(k), g + up * 2.7f + Vector3.forward * .1f, Vector3.forward, .26f, .02f, 10); mk.Prism(SweetM(k), g + up * 2.7f - Vector3.forward * .1f, -Vector3.forward, .26f, .02f, 10);
            halos.Add(g + up * 2.7f, 1.5f, Yellow, .45f); ArtistBits.PostCollider(ctx.Root, g, 2.4f, .18f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.6f, new Color(1f, .9f, .6f), 13f, 1.4f);
        }

        /// A macaron: two round shells with cream between. Solid, so you can stand on it.
        void Macaron(MeshKit mk, MeshKit solid, Vector3 c, float r, int k, float thick = .9f)
        {
            var up = Vector3.up; var shell = SweetM(k);
            mk.Prism(shell, c - up * thick, up, r, thick * .42f, 14, 1.06f); mk.Prism(cream, c - up * thick * .58f, up, r * .94f, thick * .2f, 14); mk.Prism(shell, c - up * thick * .38f, up, r * 1.06f, thick * .38f, 14, .92f);
            solid.Prism(shell, c - up * thick, up, r * 1.02f, thick, 10);
        }

        /// A jelly pad that throws you: a wobbling dome on a plate. 'velocity' is where it sends you.
        void Jelly(Vector3 g, float r, Vector3 velocity, Color c, HyperCourse.Kind kind = HyperCourse.Kind.Bounce)
        {
            var up = Vector3.up; var m = new MeshKit();
            m.Prism(jellyM, Vector3.zero, up, r, r * .32f, 14, .78f); m.Prism(GlowM(c, 1.6f), up * r * .32f, up, r * .78f, r * .12f, 14, .5f);
            var go = m.BuildSingle(kind == HyperCourse.Kind.Geyser ? "Geyser" : "Jelly pad", life.transform, false); go.transform.position = g + up * .06f;
            plates.Prism(white, g - up * .3f, up, r * 1.15f, .36f, 14);
            course.AddPad(kind, g + up * .3f, r * 1.05f, velocity, go.transform);
            halos.Add(g + up * .5f, r * 1.6f, c, .3f); noGrass.Add(new Vector3(g.x, r + 1.2f, g.z));
        }

        /// A boost strip set into the ground: glowing arrows, and it throws you the way you face.
        void Boost(MeshKit mk, Vector3 g, Vector3 dir, Color c, float speed = 22f)
        {
            var rot = Quaternion.LookRotation(dir); var up = Vector3.up; var side = Vector3.Cross(up, dir);
            mk.Box(white, g + up * .07f, new Vector3(2.6f, .06f, 4f), rot);
            for (var k = -1; k <= 1; k++) foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(c, 2f), g + up * .11f + dir * (k * 1.2f) + side * e * .5f, new Vector3(1.1f, .03f, .3f), rot * Quaternion.Euler(0, e * 40f, 0));
            course.AddPad(HyperCourse.Kind.Boost, g, 2.1f, dir * speed + up * 3.5f);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(1.6f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, n % 2 == 0 ? pinkM : cream, PathW, .15f + (n % 2) * .02f, ctx);   // (lanes lie a little above the roads they cross, so nothing flickers)
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 11, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n % 3 == 0, n);
                }
                // a boost strip half way along a long lane
                if (pts.Count > 30) { var mid = pts[pts.Count / 2]; if (Drop(mid, 3f) < 1.2f) Boost(mk, mid, dir, Sweet[n % 4]); }
                walk++; n++;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Candy-floss grass: soft tufts in pink, mint and lilac on the dark flats, sparse so the cake shows through.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.5f, 1.1f, new Color(.9f, .6f, .8f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(6601);
            const float st = 2.8f; var tufts = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.8f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (WindMesh.Patch(px, pz, .035f, 1f) < .52f) continue;     // bare cake between the patches
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var kind = WindMesh.Pick(WindMesh.Patch(px, pz, .014f, 4f), 3);
                var col = kind == 0 ? new Color(1f, .62f, .82f) : kind == 1 ? new Color(.62f, .96f, .82f) : new Color(.8f, .72f, 1f);
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .22f + (float)rng.NextDouble() * .3f, col * .8f, Color.Lerp(col, Color.white, .5f), rng, 4, .09f, .13f); tufts++;
            }
            foreach (var kv in meshes) kv.Value.Build("Kawaii candy floss", ctx.Root, mat);
            Debug.Log($"The Kawaii Cascades: {tufts} tufts of candy floss");
        }
    }
}
