using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Discotopia (WORLD_GUIDE.md): a white resort town by the sea on a clear starry night, picked out in silver, gold
    /// and white light. You arrive on the Floor: a great light-up dance floor that is the town square, a mirror ball
    /// hung over it from four white arches, a DJ in a booth, a white limousine at the kerb. The Esplanade, a white
    /// promenade with silver globe lamps and a balustrade, runs out along the headland to the Lighthouse, whose lamp
    /// is a giant mirror ball: it turns, and specks of light sweep over the town, the lawns and the water. Inland
    /// the Belvedere, a raised white terrace up a grand stair, is where the glamour sits: champagne, little tables,
    /// the view. Out on the lawns: white villas with gold windows, glitter trees strung with lights, mirror balls
    /// on plinths, parasol tables. The silly Euro-disco corner is out toward the lighthouse.
    /// Laid out on purpose (the build-land standard): DiscoTown.cs has the places and the people.
    public partial class DiscoKit : LandKit
    {
        public override string Land => "Discotopia";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Gold = new Color(1f, .8f, .32f), Silver = new Color(.86f, .9f, 1f), Amber = new Color(1f, .58f, .2f), Aqua = new Color(.55f, .9f, 1f), Ink = new Color(.12f, .12f, .2f), Pale = new Color(.97f, .97f, 1f);
        static readonly Color[] TileC = { new Color(1f, .8f, .32f), new Color(.92f, .95f, 1f), new Color(1f, .58f, .2f), new Color(.55f, .9f, 1f) };
        const float PathW = 3.4f, FloorHalf = 9f, TowerH = 24f;

        Material stone, stoneWarm, marble, goldM, chrome, dark, navy, bark, canopy, mirror;
        // the four colours of every light-up tile and string of lights; the life script chases them
        readonly Material[] tileM = new Material[4];
        Material lampM, windowM;
        GlowHalos halos; Vector3 center, toLight, sideways; DiscoLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        // x, radius, z: the small fixtures along the Esplanade and round the lighthouse and the Belvedere that a lane must not start through
        readonly List<Vector3> fixtures = new List<Vector3>();
        Vector2 heart2, light2, belv2, dir2, side2; float waterY, lightDist;

        void MakeMaterials()
        {
            // white stone that is faintly self-lit, so the night never swallows the town
            stone = WorldMaterials.Glow(new Color(.86f, .88f, .96f), .2f);
            stoneWarm = WorldMaterials.Glow(new Color(.96f, .9f, .8f), .2f);
            marble = WorldMaterials.Glow(new Color(.95f, .95f, 1f), .3f);
            goldM = WorldMaterials.Surface(new Color(.92f, .72f, .26f), Surface.Metal);
            chrome = WorldMaterials.Surface(new Color(.8f, .83f, .9f), Surface.Metal);
            mirror = WorldMaterials.Surface(new Color(.9f, .92f, .98f), Surface.Glass);
            dark = WorldMaterials.Solid(new Color(.1f, .1f, .16f));
            navy = WorldMaterials.Glow(new Color(.2f, .24f, .5f), .14f);
            bark = WorldMaterials.Glow(new Color(.9f, .9f, .95f), .16f);
            canopy = WorldMaterials.Glow(new Color(.5f, .72f, .7f), .14f);
            for (var i = 0; i < 4; i++) tileM[i] = new Material(WorldMaterials.Glow(TileC[i], 1.3f)) { name = "Disco tile " + i };
            lampM = new Material(WorldMaterials.Glow(Silver, 1.6f)) { name = "Disco lamps" };
            windowM = new Material(WorldMaterials.Glow(Gold, 1.2f)) { name = "Disco windows" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "ABBA": return 17f;
                case "Earth, Wind & Fire": return 13f;
                default: return HasStation(a) ? 12f : 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // pale moonlit sand by the water, silver rock on the steep parts, cool blue-green lawns
            if (height < waterY + 2.2f) color = Color.Lerp(new Color(.74f, .72f, .62f), new Color(.84f, .82f, .7f), patch);
            else if (slope > 20f) color = Color.Lerp(new Color(.5f, .52f, .62f), new Color(.6f, .62f, .72f), patch);
            else color = Color.Lerp(new Color(.2f, .42f, .42f), new Color(.3f, .52f, .46f), Mathf.SmoothStep(0f, 1f, patch));
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Firm(Vector2 p) => data.IslandAt(p.x, p.y) == isl && data.HeightAt(p.x, p.y) > waterY + 1.4f;
            bool Inside(Vector2 p, float m) { if (!Firm(p)) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (!Firm(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * m)) return false; } return true; }
            // the lighthouse stands at the end of the longest reach of land out into the sea
            var best = 0f; var tip = heart2 + new Vector2(0, 120f);
            for (var ang = 0f; ang < 360f; ang += 6f)
            {
                var d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)); var reach = 0f;
                for (var s = 20f; s < 480f; s += 6f) { if (!Firm(heart2 + d * s)) break; reach = s; }
                // only count it if the end really is the sea, not another land
                var beyond = heart2 + d * (reach + 30f); var sea = data.HeightAt(beyond.x, beyond.y) < waterY + 1f;
                if (sea && reach > best) { best = reach; tip = heart2 + d * reach; }
            }
            dir2 = (tip - heart2).normalized; side2 = new Vector2(dir2.y, -dir2.x);
            lightDist = Mathf.Max(90f, Vector2.Distance(heart2, tip) - 26f); light2 = heart2 + dir2 * lightDist;
            for (var k = 0; k < 20 && !Inside(light2, 13f); k++) { lightDist -= 6f; light2 = heart2 + dir2 * lightDist; }
            belv2 = heart2 - dir2 * 66f;
            if (!Inside(belv2, 30f)) belv2 = heart2 + side2 * 70f;
            if (!Inside(belv2, 30f)) belv2 = heart2 - side2 * 70f;

            var keep = new List<string>();
            void Put(string name, Vector2 p, float room) { if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("ABBA", heart2 + side2 * 84f - dir2 * 6f, 34f); Put("Earth, Wind & Fire", heart2 - side2 * 78f - dir2 * 4f, 30f);
            // the Euro-disco corner, out along the Esplanade toward the lighthouse
            Put("Dschinghis Khan", light2 - dir2 * 50f + side2 * 27f, 14f); Put("Boney M.", light2 - dir2 * 50f - side2 * 27f, 14f);
            Put("Sandy Marton", light2 - dir2 * 94f + side2 * 27f, 14f); Put("International Music System", light2 - dir2 * 94f - side2 * 27f, 14f); Put("Walter Murphy", light2 - dir2 * 138f + side2 * 27f, 14f);
            var avoid = new List<Vector3> { new Vector3(light2.x, 22f, light2.y), new Vector3(belv2.x, 30f, belv2.y) };
            for (var d = 24f; d < lightDist - 14f; d += 14f) avoid.Add(new Vector3(heart2.x + dir2.x * d, 9f, heart2.y + dir2.y * d));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 44f, avoid, keep, Land, 330f);

            Level(data, index, heart2, 30f, 46f);
            Level(data, index, light2, 16f, 28f);
            Level(data, index, belv2, 24f, 38f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = Mathf.Max(data.HeightAt(c.x, c.y), data.layout.waterLevel + 2.6f);
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
            center = ctx.Plaza; toLight = new Vector3(dir2.x, 0, dir2.y); sideways = new Vector3(side2.x, 0, side2.y);
            life = new GameObject("Disco life").AddComponent<DiscoLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.tiles = tileM; life.lamps = lampM; life.windows = windowM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            TheFloor(ctx, mk, solid);
            Esplanade(ctx, mk, solid);
            Lighthouse(ctx, mk, solid);
            Belvedere(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("Discotopia", ctx.Root, false);
            solid.BuildCollider("Discotopia walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Lawn(ctx); halos.Build("Discotopia glow", ctx.Root); }

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
            ctx.IsLand(x, z, 5f) && ctx.Ground(x, z) > waterY + 2.2f && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

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

        /// A white post with a silver globe: the town's lamp. (All the globes are one material.)
        void GlobeLamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(marble, g - up * .2f, up, .08f, 3.6f, 6); mk.Prism(marble, g, up, .22f, .3f, 8, .5f);
            mk.Blob(lampM, g + up * 3.7f, Vector3.one * .34f, new System.Random(7), 0f); halos.Add(g + up * 3.7f, 2.2f, Silver, .55f); ArtistBits.PostCollider(ctx.Root, g, 3.6f, .12f);
            if (light) ArtistBits.PointLight(ctx.Root, g + up * 3.3f, new Color(.95f, .97f, 1f), 17f, 2.2f);
        }

        /// A string of lights from a to b; the bulbs take the four tile colours in turn, so they chase with the floor.
        void LightString(MeshKit mk, Vector3 a, Vector3 b, int phase = 0)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(3, Mathf.RoundToInt(len / 1.1f)); var sag = Mathf.Min(1.4f, len * .08f); var prev = a;
            for (var i = 1; i <= n; i++)
            {
                var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag;
                mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p;
                if (i < n) { mk.Blob(tileM[(i + phase) % 4], p - Vector3.up * .08f, Vector3.one * .08f, new System.Random(i), 0f); if (i % 4 == 0) halos.Add(p - Vector3.up * .08f, .7f, TileC[(i + phase) % 4], .3f); }
            }
        }

        /// A glitter tree: a white trunk, a round pale crown dotted with lights.
        void GlitterTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(bark, g - up * .3f, up, .18f * s, 3f * s, 6, .7f); ArtistBits.PostCollider(ctx.Root, g, 2.8f * s, .2f * s);
            var c = g + up * 4.1f * s; mk.Blob(canopy, c, new Vector3(2f, 1.8f, 2f) * s, rng, .16f);
            for (var k = 0; k < 12; k++) { var d = Quaternion.Euler((float)rng.NextDouble() * 140f - 70f, k * 30f, 0) * Vector3.forward; mk.Blob(tileM[k % 4], c + Vector3.Scale(d, new Vector3(2f, 1.8f, 2f) * s), Vector3.one * .09f, rng, 0f); }
            halos.Add(c, 2.6f * s, Gold, .22f);
        }

        /// A mirror ball on a white plinth.
        void BallPlinth(LandContext ctx, MeshKit mk, Vector3 g, System.Random rng)
        {
            var up = Vector3.up; mk.Box(marble, g + up * .6f, new Vector3(.9f, 1.2f, .9f), Quaternion.identity); mk.Box(goldM, g + up * 1.22f, new Vector3(1f, .06f, 1f), Quaternion.identity); BoxCol(ctx.Root, g + up * .6f, new Vector3(.9f, 1.2f, .9f), Quaternion.identity);
            mk.Blob(lampM, g + up * 1.85f, Vector3.one * .55f, rng, .14f); halos.Add(g + up * 1.85f, 2f, Silver, .45f);
        }

        /// A white bench you can sit on.
        void WhiteBench(MeshKit mk, Vector3 g, Vector3 face, float len = 2.2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(marble, g + up * .45f, new Vector3(len, .1f, .55f), rot); mk.Box(marble, g + up * .8f - face * .25f, new Vector3(len, .5f, .08f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(goldM, g + up * .22f + side * e * (len / 2f - .15f), new Vector3(.1f, .44f, .5f), rot);
            BoxCol(rootT, g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .4f), g + up * .52f + side * (len / 2f - .4f), face);
        }

        /// A little round table under a parasol with two chairs to sit on.
        void ParasolTable(LandContext ctx, MeshKit mk, Vector3 g, System.Random rng, int i)
        {
            var up = Vector3.up; mk.Prism(marble, g, up, .05f, .74f, 6); mk.Prism(marble, g + up * .74f, up, .5f, .05f, 12); mk.Prism(chrome, g + up * .79f, up, .03f, 1.7f, 5); mk.Prism(i % 2 == 0 ? goldM : marble, g + up * 2.3f, up, 1.3f, .4f, 10, .06f);
            mk.Blob(tileM[i % 4], g + up * .88f, Vector3.one * .07f, rng, 0f); ArtistBits.PostCollider(ctx.Root, g, 2.2f, .14f);
            foreach (var e in new[] { -1f, 1f }) { var ch = g + Vector3.right * e * .95f; mk.Prism(marble, ch, up, .04f, .45f, 5); mk.Prism(marble, ch + up * .45f, up, .24f, .06f, 10); Interact.Seat(tlife.transform, ch + up * .52f, -Vector3.right * e); }
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
                var pts = new List<Vector3>();
                // the big round places a lane goes round rather than across (the Floor, the lighthouse's plinth, the
                // Belvedere), unless it starts inside one
                var rounds = new List<Vector3> { new Vector3(center.x, 28.5f, center.z), new Vector3(light2.x, 13.6f, light2.y), new Vector3(belv2.x, 14.5f, belv2.y) };
                rounds.RemoveAll(o => (new Vector2(join.x - o.x, join.z - o.z)).sqrMagnitude < (o.y + PathW / 2f + .5f) * (o.y + PathW / 2f + .5f));
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(2.5f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    // bend round any other artist's area (and the big places) instead of running across it: an arc on
                    // whichever side the straight line passes, so the lane never flips across
                    void Round(Vector3 ov, float r)
                    {
                        var rel = q - ov; if (rel.sqrMagnitude >= r * r) return;
                        var along = Vector3.Dot(rel, dir); var sgn = Vector3.Dot(from - ov, side) >= 0f ? 1f : -1f;
                        q = ov + dir * along + side * sgn * Mathf.Sqrt(Mathf.Max(0f, r * r - along * along));
                    }
                    foreach (var o in ctx.Artists) if (o.name != artist) Round(new Vector3(o.x, 0, o.z), Foot(o) * 1.06f + 3f);
                    foreach (var o in rounds) Round(new Vector3(o.x, 0, o.z), o.y + PathW / 2f);
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, stone, PathW, .14f, ctx);
                // gold studs down the middle, a globe lamp now and then (never on another lane)
                for (var i = 1; i < pts.Count; i += 3) mk.Blob(tileM[(i / 3 + walk) % 4], pts[i] + Vector3.up * .17f, new Vector3(.12f, .04f, .12f), new System.Random(i), 0f);
                for (var i = 8; i < pts.Count; i += 13)
                {
                    var s = (i / 13) % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 17f * 17f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f) || NearPath(lp.x, lp.z, PathW / 2f + .4f)) continue;
                    lamps.Add(lp); GlobeLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), lamps.Count % 2 == 0);
                }
                foreach (var p in pts) Remember(p, walk);
                walk++;
            }
            // would a lane leaving p toward a run into one of the Esplanade's or the lighthouse's fixtures (bollards,
            // lamps, benches, the photo booth, the cart) in its first stretch?
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
                        // never branch from inside another artist's area; from the Floor, start at the edge of its paving
                        // (not across the dance floor, past the DJ booth or through the limousine)
                        if (w >= 10 && InFootprint(ctx, p0.x, p0.z, 2f)) continue;
                        var p = p0;
                        if (w == 0) { var o = new Vector3(a.x - center.x, 0, a.z - center.z); if (o.sqrMagnitude < 1f) continue; p = ctx.OnGround(center.x + o.normalized.x * 27.5f, center.z + o.normalized.z * 27.5f); }
                        var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a);
                        if (d < best && !Blocked(p, a)) { best = d; next = a; join = p; }
                    }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- the lawns
        /// Glitter trees, white villas with gold windows and lights along the roof, mirror balls on plinths, parasol
        /// tables, benches. On a jittered grid so the whole land is filled.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, villas = 0, balls = 0, tables = 0, benches = 0; var rng = new System.Random(1978);
            const float cell = 21f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz);
                // how much the ground falls across a round of radius r round p; steep spots are skipped after the random
                // numbers are drawn, so the rest of the layout stays put
                float Fall(float r) { float lo = p.y, hi = p.y; for (var k = 0; k < 8; k++) { var h = ctx.Ground(px + Mathf.Cos(k * Mathf.PI / 4f) * r, pz + Mathf.Sin(k * Mathf.PI / 4f) * r); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); } return hi - lo; }
                if (roll < .34f) { GlitterTree(ctx, mk, p, .9f + s * .5f, rng); trees++; }
                else if (roll < .46f) { if (villas < 20 && Open(ctx, px, pz, 9f) && Fall(7f) < 3f) { Villa(ctx, mk, solid, p, FaceTo(p, NearestWalk(p)), villas, rng); villas++; } }
                else if (roll < .52f) { if (balls < 14 && Fall(.8f) < .3f) { BallPlinth(ctx, mk, p, rng); balls++; } }
                else if (roll < .57f) { if (tables < 16 && Fall(1.3f) < .3f) { ParasolTable(ctx, mk, p, rng, tables); tables++; noGrass.Add(new Vector3(p.x, 2f, p.z)); } }
                else if (roll < .61f) { if (Fall(1.4f) > .35f) continue; WhiteBench(mk, p, FaceTo(p, NearestWalk(p))); benches++; }
            }
            Debug.Log($"Discotopia: lighthouse {lightDist:0} m out, {trees} glitter trees, {villas} villas, {balls} mirror balls, {tables} parasol tables, {benches} benches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// A white villa: two stacked blocks with flat roofs, gold windows, an arched door, lights along the roof edge.
        void Villa(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, int i, System.Random rng)
        {
            var up = Vector3.up; float W = 7f + (i % 3), D = 6f, H = 3.2f;
            // the villa and its terrace stand level at the highest ground under them, on a white footing down to the
            // lowest (on a slope the low side would otherwise hang in the air)
            var rot0 = Quaternion.LookRotation(face); float lo = g.y, hi = g.y;
            foreach (var (cx, cz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, 1f), (0f, -1f), (-1f, 0f), (1f, 0f) })
            {
                var q = g + rot0 * new Vector3(cx * (W / 2f + .2f), 0, cz < 0 ? -D / 2f - .2f : cz * (D / 2f + 3.2f)); var h = ctx.Ground(q.x, q.z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
            }
            g.y = hi; var f = new PlaceFrame(g + up * .2f, face);
            if (hi - lo > .15f)
            {
                var depth = hi - lo + .6f;
                mk.Box(stone, f.P(0, -.2f - depth / 2f, 0), new Vector3(W, depth, D), f.r); solid.Box(stone, f.P(0, -.2f - depth / 2f, 0), new Vector3(W, depth, D), f.r);
                mk.Box(marble, f.P(1f, -.15f - depth / 2f, D / 2f + 1.6f), new Vector3(W - 2f, depth, 3f), f.r); solid.Box(marble, f.P(1f, -.15f - depth / 2f, D / 2f + 1.6f), new Vector3(W - 2f, depth, 3f), f.r);
            }
            mk.Box(stone, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, D), f.r); solid.Box(stone, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, D), f.r); mk.Box(marble, f.P(0, H + .12f, 0), new Vector3(W + .3f, .24f, D + .3f), f.r);
            var up2 = i % 2 == 0; if (up2) { mk.Box(stone, f.P(-W / 4f, H + 1.5f, -D / 6f), new Vector3(W / 2f, 2.6f, D * .66f), f.r); mk.Box(marble, f.P(-W / 4f, H + 2.9f, -D / 6f), new Vector3(W / 2f + .3f, .2f, D * .66f + .3f), f.r); mk.Box(windowM, f.P(-W / 4f, H + 1.5f, D / 6f + .01f), new Vector3(1.4f, 1.3f, .05f), f.r); }
            mk.Box(dark, f.P(-W / 2f + 1.3f, 1.05f, D / 2f + .02f), new Vector3(1f, 2.1f, .05f), f.r); mk.Prism(dark, f.P(-W / 2f + 1.3f, 2.1f, D / 2f - .02f), f.Fwd, .5f, .06f, 8);
            for (var k = 0; k < 2; k++) { mk.Box(windowM, f.P(.2f + k * 2.2f, 1.7f, D / 2f + .02f), new Vector3(1.3f, 1.3f, .05f), f.r); halos.Add(f.P(.2f + k * 2.2f, 1.7f, D / 2f + .7f), 1.4f, Gold, .32f); }
            for (var k = 0; k < Mathf.RoundToInt(W / .7f); k++) mk.Blob(tileM[k % 4], f.P(-W / 2f + .3f + k * .7f, H + .3f, D / 2f + .1f), Vector3.one * .07f, rng, 0f);
            // a terrace in front with a bench facing out
            mk.Box(marble, f.P(1f, -.05f, D / 2f + 1.6f), new Vector3(W - 2f, .3f, 3f), f.r); WhiteBench(mk, f.P(1.6f, .1f, D / 2f + 1.5f), f.Fwd, 2f);
            noGrass.Add(new Vector3(g.x, 6.5f, g.z));
            if (i % 3 == 0) Folk(f.P(-W / 2f + 1.3f, .1f, D / 2f + 2f), f.Fwd, Resident.Pose.Wave, VillaTalk);
        }

        Vector3 G0(Vector3 p) => ctx0.OnGround(p.x, p.z);

        /// The lawns, in patches the way a resort's grounds grow: kept lawns of short cool grass with pale tips,
        /// flower lawns where every tuft in a drift is tipped the same (all white, all gold or all pale blue), and
        /// longer soft grass left to grow in the far corners and down by the shore. Each patch its own shade, from a
        /// deep sea blue-green to a fresher green; the kept lawns vary only a little, to stay neat.
        void Lawn(LandContext ctx)
        {
            var mat = WindMesh.Material(.22f, .8f, new Color(.3f, .5f, .46f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1976);
            Color[] flowers = { new Color(1f, .96f, .85f), new Color(1f, .82f, .4f), new Color(.8f, .9f, 1f) };
            Color sea = new Color(.2f, .42f, .44f), fresh = new Color(.36f, .56f, .44f), pale = new Color(.8f, .9f, .95f);
            const float st = 2.6f;
            int kept = 0, flowered = 0, longGrass = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || ctx.Ground(px, pz) < waterY + 2.2f || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                var gy = ctx.Ground(px, pz);
                var kind = WindMesh.Patch(px, pz, .015f, 1f);                     // kept lawn or long grass
                var isLong = kind > .62f || gy < waterY + 5f;                     // the far corners and the shore
                if (WindMesh.Patch(px, pz, .04f, 2f) < (isLong ? .45f : .38f)) continue;
                var tone = WindMesh.Patch(px, pz, .025f, 3f); var drift = WindMesh.Patch(px, pz, .06f, 4f);
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(sea, fresh, tone) * (.94f + (float)rng.NextDouble() * .12f);
                var at = new Vector3(px, gy - .04f, pz);
                if (isLong) { wm.Tuft(at, .4f + (float)rng.NextDouble() * .35f, col, Color.Lerp(col, pale, .4f), rng, 4, .05f, .12f); longGrass++; }
                else if (drift > .6f && rng.NextDouble() < .55)
                {
                    var fl = flowers[WindMesh.Pick(WindMesh.Patch(px, pz, .018f, 5f), flowers.Length)];
                    wm.Tuft(at, .22f + (float)rng.NextDouble() * .2f, col, fl, rng, 3, .05f, .08f); flowered++;
                }
                else { wm.Tuft(at, .2f + (float)rng.NextDouble() * .2f, col, rng.NextDouble() < .06 ? flowers[rng.Next(flowers.Length)] : Color.Lerp(col, pale, .4f), rng, 3, .05f, .08f); kept++; }
            }
            foreach (var kv in meshes) kv.Value.Build("Discotopia lawn", ctx.Root, mat);
            Debug.Log($"Discotopia lawns: {kept} kept-lawn tufts, {flowered} in flower drifts, {longGrass} long-grass tufts");
        }
    }
}
