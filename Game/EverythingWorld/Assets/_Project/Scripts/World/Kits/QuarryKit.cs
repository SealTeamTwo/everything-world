using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Nu Metal Quarry (WORLD_GUIDE.md): 2000s radio nostalgia, big earnest choruses on the car radio. Beside where
    /// you arrive a huge open-pit quarry steps down in terraces, a haul road ramping down its side to the floor, where a
    /// stage is built on a mining rig (Nickelback). Rusted machinery on the benches: giant haul trucks, excavators,
    /// conveyor belts, a crusher, floodlight towers; round the rim the other bands among chain-link, burning oil drums,
    /// wrecked cars, portable cabins and rock piles. Overcast and gritty, dust in the air. Reads by flat grey light and
    /// rust against warm fire (the Punk Badlands next door is bright noon, the Rainwood dark green forest).
    /// Files: QuarryKit (ground, the pit, arrival, paths, wilds), QuarryArtists, QuarrySongIds, QuarryLife.
    public partial class QuarryKit : LandKit
    {
        public override string Land => "The Nu Metal Quarry";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Fire = new Color(1f, .55f, .2f), Sodium = new Color(1f, .78f, .45f), Hazard = new Color(1f, .8f, .1f), Ink = new Color(.95f, .93f, .88f), Green = new Color(.4f, 1f, .45f);
        const float PathW = 3.2f, PitR = 105f, BenchH = 6.5f; const int Benches = 6;

        Material rock, rockDark, gravel, rust, rustDark, steel, yellow, dark, concrete, wood, fireM, lampM, chain;
        GlowHalos halos; Vector3 center; QuarryLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<(Vector2 c, float r)> blocks = new List<(Vector2, float)>();   // places a lane goes round: the gate cabin, the crusher, the conveyor's legs
        Vector2 heart2, pit2, rampDir; bool hasPit; float rimY, floorY;
        float BenchW => PitR / (Benches + 2.2f);
        float FloorR => PitR - Benches * BenchW;

        void MakeMaterials()
        {
            rock = WorldMaterials.Solid(new Color(.52f, .5f, .47f)); rockDark = WorldMaterials.Solid(new Color(.34f, .33f, .31f)); gravel = WorldMaterials.Solid(new Color(.6f, .57f, .52f));
            rust = WorldMaterials.Solid(new Color(.55f, .3f, .18f)); rustDark = WorldMaterials.Solid(new Color(.35f, .2f, .13f)); steel = WorldMaterials.Solid(new Color(.45f, .47f, .5f), .6f);
            yellow = WorldMaterials.Solid(new Color(.85f, .68f, .15f)); dark = WorldMaterials.Solid(new Color(.08f, .08f, .09f)); concrete = WorldMaterials.Solid(new Color(.62f, .61f, .58f));
            wood = WorldMaterials.Solid(new Color(.45f, .34f, .24f)); chain = WorldMaterials.Solid(new Color(.55f, .56f, .58f), .4f);
            fireM = new Material(WorldMaterials.Glow(Fire, 2.2f)) { name = "Drum fires" };
            lampM = new Material(WorldMaterials.Glow(Sodium, 2f)) { name = "Floodlights" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Nickelback": return 17f;
                case "Creed": case "3 Doors Down": return 13f;
                case "Breaking Benjamin": case "Disturbed": return 12f;
                default: return HasStation(a) ? 12f : 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 30f) color = Color.Lerp(new Color(.42f, .4f, .38f), new Color(.5f, .47f, .44f), patch);             // the cut rock faces
            else if (slope > 14f) color = Color.Lerp(new Color(.5f, .48f, .44f), new Color(.58f, .55f, .5f), patch);
            else color = Color.Lerp(new Color(.48f, .48f, .38f), new Color(.6f, .57f, .46f), Mathf.SmoothStep(0f, 1f, patch));   // dusty scrub
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        /// The quarry's shape: the height at a distance from its centre, terraces stepping down to the floor, and the
        /// haul road cutting a straight ramp down from the rim on the arrival side.
        float PitHeight(Vector2 p, float ground)
        {
            var rel = p - pit2; var d = rel.magnitude; if (d >= PitR + 4f) return ground;
            var k = Mathf.Clamp((PitR - d) / BenchW, 0f, Benches);
            var step = Mathf.Floor(k); var fr = k - step;
            var terraced = rimY - (step + Ease(.74f, 1f, fr)) * BenchH;   // flat benches, steep cut faces between
            if (d < FloorR) terraced = floorY;
            var h = Mathf.Lerp(ground, terraced, Ease(PitR + 4f, PitR - 2f, d));
            // the haul road: a straight ramp down the arrival side, 8 m wide
            var along = Vector2.Dot(rel, rampDir); var across = Mathf.Abs(Vector2.Dot(rel, new Vector2(-rampDir.y, rampDir.x)));
            if (along > FloorR - 4f && along < PitR + 6f && across < 7f)
            {
                var t = Mathf.InverseLerp(FloorR, PitR + 2f, along); var ramp = Mathf.Lerp(floorY, rimY, t);
                var w = 1f - Ease(4.5f, 7f, across);
                h = Mathf.Lerp(h, ramp, w);
            }
            return h;
        }

        // ---------------------------------------------------------------- shaping the ground
        void ShapeQuarry(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 24; k++) { var a = k * Mathf.PI / 12f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            hasPit = false;
            foreach (var dist in new[] { 125f, 140f, 115f, 155f })
            {
                for (var k = 0; k < 24 && !hasPit; k++)
                {
                    var a = k * Mathf.PI / 12f; var c = heart2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist;
                    if (Inside(c, PitR + 46f)) { pit2 = c; hasPit = true; }
                }
                if (hasPit) break;
            }
            if (hasPit)
            {
                rampDir = (heart2 - pit2).normalized;
                Level(data, index, pit2, PitR + 8f, PitR + 40f);
                rimY = data.HeightAt(pit2.x, pit2.y); floorY = rimY - Benches * BenchH - 2f;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - pit2.y) > PitR + 10f) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - pit2.x) > PitR + 10f) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        data.heights[i] = PitHeight(new Vector2(x, z), data.heights[i]);
                    }
                }
            }
            // Nickelback on the quarry floor, the rest round the rim
            var keep = new List<string>();
            foreach (var a in data.layout.artists)
                if (a.sceneId == isl.sceneId && a.name == "Nickelback" && hasPit) { var p = pit2 - rampDir * 4f; a.x = p.x; a.z = p.y; keep.Add(a.name); }
            var avoid = new List<Vector3>();
            if (hasPit) avoid.Add(new Vector3(pit2.x, PitR + 18f, pit2.y));
            ArtistPlacement.Spread(data, index, 50f, heart2, 44f, 36f, avoid, keep, Land, 400f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                if (keep.Contains(a.name)) continue;
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
            Level(data, index, heart2, 16f, 28f);
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
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear(); blocks.Clear();
            center = ctx.Plaza;
            life = new GameObject("Quarry life").AddComponent<QuarryLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.fires = fireM; life.lamps = lampM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            if (hasPit) Pit(ctx, mk, solid);
            Paths(ctx, mk, solid);
            QuarryPlaces(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Nu Metal Quarry", ctx.Root, false);
            solid.BuildCollider("The Nu Metal Quarry walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Scrub(ctx); halos.Build("Quarry glow", ctx.Root); }

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

        bool InPit(float x, float z, float pad) => hasPit && Vector2.Distance(new Vector2(x, z), pit2) < PitR + pad;

        bool OnRamp(float x, float z, float pad)
        {
            if (!hasPit) return false; var rel = new Vector2(x, z) - pit2; var along = Vector2.Dot(rel, rampDir); var across = Mathf.Abs(Vector2.Dot(rel, new Vector2(-rampDir.y, rampDir.x)));
            return along > FloorR - 6f - pad && along < PitR + 10f + pad && across < 6f + pad;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !OnRamp(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 20f + pad;

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

        /// The lowest ground in a square of half-size r round a point.
        static float LowUnder(LandContext ctx, Vector3 at, float r)
        {
            var lo = ctx.Ground(at.x, at.z);
            for (var i = -2; i <= 2; i++) for (var j = -2; j <= 2; j++) lo = Mathf.Min(lo, ctx.Ground(at.x + r * i / 2f, at.z + r * j / 2f));
            return lo;
        }

        /// A portable cabin on a concrete footing that reaches down to the lowest ground under it.
        void Cabin(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Quaternion rot, Color paint, float h)
        {
            var up = Vector3.up; var lo = LowUnder(ctx, p, 3.8f);
            if (p.y - lo > .1f) { mk.Box(concrete, new Vector3(p.x, (lo + p.y + .2f) / 2f - .15f, p.z), new Vector3(3.1f, p.y - lo + .5f, 7.1f), rot); solid.Box(concrete, new Vector3(p.x, (lo + p.y + .2f) / 2f - .15f, p.z), new Vector3(3.1f, p.y - lo + .5f, 7.1f), rot); }
            mk.Box(WorldMaterials.Solid(paint), p + up * h, new Vector3(3f, 2.6f, 7f), rot); solid.Box(concrete, p + up * h, new Vector3(3f, 2.6f, 7f), rot);
        }

        /// A burning oil drum, rusted, fire in its mouth (QuarryLife makes it flicker and spark).
        void Drum(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up;
            mk.Prism(rust, g - up * .05f, up, .42f, 1f, 10); mk.Prism(rustDark, g + up * .3f, up, .44f, .08f, 10); mk.Prism(rustDark, g + up * .75f, up, .44f, .08f, 10);
            mk.Blob(fireM, g + up * 1.1f, new Vector3(.32f, .35f, .32f), ctx.Rng, .3f);
            halos.Add(g + up * 1.3f, 2.2f, Fire, .8f); life.drumAt.Add(g + up * 1.05f);
            ArtistBits.PostCollider(ctx.Root, g, 1f, .44f);
        }

        /// A floodlight tower: a lattice mast, a bank of lamps at the top tipped down, cables.
        void Floodlight(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, bool light)
        {
            var up = Vector3.up; const float H = 12f;
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) mk.Box(steel, g + new Vector3(e * .5f, H / 2f, f * .5f), new Vector3(.12f, H, .12f), Quaternion.identity);
            for (var y = 1.5f; y < H; y += 1.5f) mk.Box(steel, g + up * y, new Vector3(1.1f, .06f, 1.1f), Quaternion.identity);
            var head = g + up * (H + .6f); var rot = Quaternion.LookRotation(face) * Quaternion.Euler(30f, 0, 0);
            mk.Box(dark, head, new Vector3(3f, 1.4f, .4f), rot);
            for (var k = 0; k < 6; k++) mk.Box(lampM, head + rot * new Vector3((k % 3 - 1) * .9f, (k / 3 - .5f) * .6f, .21f), new Vector3(.7f, .45f, .02f), rot);
            halos.Add(head + face * 1.5f, 4f, Sodium, .5f);
            if (light) ArtistBits.PointLight(ctx.Root, head + face * 10f - up * 6f, Sodium, 34f, 1.6f);
            ArtistBits.PostCollider(ctx.Root, g, H, .8f);
        }

        /// A chain-link fence run between posts.
        void Fence(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 b)
        {
            var up = Vector3.up; var n = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / 3f));
            for (var k = 0; k < n; k++)
            {
                var p0 = Vector3.Lerp(a, b, k / (float)n); var p1 = Vector3.Lerp(a, b, (k + 1f) / n); p0 = ctx.OnGround(p0.x, p0.z); p1 = ctx.OnGround(p1.x, p1.z);
                mk.Prism(steel, p0 - up * .2f, up, .05f, 2.4f, 6);
                mk.Box(WorldMaterials.Solid(new Color(.55f, .56f, .58f), .3f), (p0 + p1) / 2f + up * 1.15f, new Vector3(.02f, 2f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
                mk.Box(steel, (p0 + p1) / 2f + up * 2.15f, new Vector3(.05f, .05f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
                solid.Box(steel, (p0 + p1) / 2f + up * 1.1f, new Vector3(.12f, 2.2f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
            }
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a gravel yard at the quarry gate, a site sign with the land's name, a portable cabin with a
        /// lit window, oil drums burning, and the edge of the pit just ahead with the haul road going down.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var toPit = hasPit ? new Vector3(pit2.x - c.x, 0, pit2.y - c.z).normalized : Vector3.forward; var sd = Vector3.Cross(up, toPit);
            mk.Prism(gravel, c - up * .25f, up, 14f, .32f, 20);
            var s = c - toPit * 12f; s.y = ctx.Ground(s.x, s.z); var rot = Quaternion.LookRotation(toPit);
            foreach (var e in new[] { -2.6f, 2.6f }) { mk.Box(steel, s + sd * e + up * 1.6f, new Vector3(.15f, 3.2f, .15f), rot); ArtistBits.PostCollider(ctx.Root, s + sd * e, 3f, .12f); }
            mk.Box(WorldMaterials.Solid(new Color(.9f, .9f, .85f)), s + up * 2.6f, new Vector3(5.8f, 1.6f, .1f), rot);
            mk.Box(yellow, s + up * 3.45f + toPit * .02f, new Vector3(5.8f, .18f, .12f), rot);
            ArtistBits.Label(ctx.Root, "THE NU METAL QUARRY", s + up * 2.8f + toPit * .06f, toPit, .32f, new Color(.15f, .15f, .15f), TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, "HARD HATS MUST BE WORN", s + up * 2.25f + toPit * .06f, toPit, .16f, new Color(.6f, .15f, .1f), TextAnchor.MiddleCenter);
            var cab = c + sd * 13f - toPit * 3f; cab.y = ctx.Ground(cab.x, cab.z); var crot = Quaternion.LookRotation(-sd);
            Cabin(ctx, mk, solid, cab, crot, new Color(.75f, .72f, .6f), 1.5f); blocks.Add((new Vector2(cab.x, cab.z), 6f));
            mk.Box(GlowM(Sodium, 1.4f), cab + up * 1.8f - sd * 1.52f, new Vector3(.03f, .8f, 1.6f), crot);
            foreach (var e in new[] { -1f, 1f }) Drum(ctx, mk, ctx.OnGround(c.x + sd.x * e * 8f + toPit.x * 6f, c.z + sd.z * e * 8f + toPit.z * 6f));
            Floodlight(ctx, mk, ctx.OnGround(c.x - sd.x * 12f, c.z - sd.z * 12f), toPit, true);
            noGrass.Add(new Vector3(c.x, 15f, c.z));
            var mark = new GameObject("Shot: the arrival and the quarry").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toPit * 8f + up * 1.7f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((toPit * 80f - up * 18f).normalized));
        }

        // ---------------------------------------------------------------- the pit
        /// The quarry: cut rock faces between the benches; on the benches, giant rusted haul trucks, an excavator,
        /// a conveyor climbing out of the pit to a crusher on the rim, rock piles, drums burning, floodlight towers
        /// round the rim; the haul road edged with a low berm and marker posts.
        void Pit(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(pit2.x, floorY, pit2.y); var ramp = new Vector3(rampDir.x, 0, rampDir.y); var rs = Vector3.Cross(up, ramp);
            // marker posts and a berm along the haul road
            for (var t = FloorR; t < PitR; t += 6f)
                foreach (var e in new[] { -1f, 1f })
                {
                    var p = c + ramp * t + rs * e * 5.4f; p = ctx.OnGround(p.x, p.z);
                    mk.Prism(yellow, p - up * .1f, up, .08f, 1.1f, 6); mk.Box(GlowM(Hazard, 1.8f), p + up * 1f, new Vector3(.12f, .15f, .12f), Quaternion.identity);
                    mk.Blob(rockDark, p + rs * e * .8f + up * .2f, new Vector3(.8f, .4f, 1.6f), ctx.Rng, .3f); BoxCol(ctx.Root, p + rs * e * .8f + up * .3f, new Vector3(1.6f, .8f, 3.2f), Quaternion.LookRotation(ramp));
                    ArtistBits.PostCollider(ctx.Root, p, 1f, .1f);
                }
            // on each bench: machinery, rock piles, drums
            for (var b = 0; b < Benches; b++)
            {
                var r = PitR - (b + .37f) * BenchW;   // the middle of the bench's flat, clear of the drop at its inner edge
                var count = Mathf.Max(3, Mathf.RoundToInt(2f * Mathf.PI * r / 40f));
                for (var k = 0; k < count; k++)
                {
                    var a = k * Mathf.PI * 2f / count + b * .7f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var p = c + d * r;
                    if (OnRamp(p.x, p.z, 3f)) continue;
                    // a truck or digger beside the haul road slides along its bench until its whole length is off the road
                    var acr = Vector2.Dot(new Vector2(p.x, p.z) - pit2, new Vector2(-rampDir.y, rampDir.x)); var away = acr >= 0f ? 1f : -1f;
                    for (var n = 0; n < 16 && OnRamp(p.x, p.z, 6f); n++) { a += away * .8f / r * (Vector2.Dot(new Vector2(-Mathf.Sin(a), Mathf.Cos(a)), new Vector2(-rampDir.y, rampDir.x)) >= 0f ? 1f : -1f); d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); p = c + d * r; }
                    p = ctx.OnGround(p.x, p.z); var tang = Vector3.Cross(up, d);
                    var pick = (k + b * 3) % 6;
                    if (pick == 0) HaulTruck(ctx, mk, p, tang, b + k);
                    else if (pick == 1) Excavator(ctx, mk, p, -d);
                    else if (pick == 2 || pick == 4) { for (var j = 0; j < 4; j++) { var q = p + tang * (j - 1.5f) * 1.6f; q = ctx.OnGround(q.x, q.z); mk.Blob(j % 2 == 0 ? rock : rockDark, q + up * .5f, new Vector3(1.6f, 1.2f, 1.4f), ctx.Rng, .35f); BoxCol(ctx.Root, q + up * .6f, new Vector3(3f, 2.2f, 2.6f), Quaternion.LookRotation(tang)); } }
                    else Drum(ctx, mk, p);
                }
            }
            // the conveyor: a long inclined belt from the floor up over the rim to a crusher
            var a0 = Mathf.Atan2(rampDir.y, rampDir.x) + Mathf.PI * .6f; var cd = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0));
            var bottom = c + cd * (FloorR - 4f); var top = c + cd * (PitR + 12f); top.y = rimY + 8f; bottom.y = floorY + 2f;
            var belt = top - bottom; var brot = Quaternion.LookRotation(belt);
            mk.Box(steel, (bottom + top) / 2f, new Vector3(2f, .3f, belt.magnitude), brot); mk.Box(dark, (bottom + top) / 2f + up * .2f, new Vector3(1.6f, .1f, belt.magnitude), brot);
            for (var t = 0f; t < 1f; t += .08f) { var p = Vector3.Lerp(bottom, top, t); var g = ctx.Ground(p.x, p.z); if (p.y - g > 1f) { mk.Box(steel, new Vector3(p.x, (p.y + g) / 2f, p.z), new Vector3(.25f, p.y - g, .25f), brot); BoxCol(ctx.Root, new Vector3(p.x, (p.y + g) / 2f, p.z), new Vector3(.4f, p.y - g, .4f), Quaternion.identity); } }
            var rocks = new MeshKit(); for (var k = 0; k < 14; k++) rocks.Blob(k % 2 == 0 ? rock : rockDark, Vector3.forward * (k * 1.6f) + Vector3.up * .45f, new Vector3(.45f, .3f, .4f), ctx.Rng, .3f);
            var rg = rocks.BuildSingle("Conveyor rocks", life.transform, false); if (rg != null) { life.conveyorRocks = rg.transform; life.beltA = bottom; life.beltB = top; }
            var crusher = top + cd * 4f; crusher.y = ctx.Ground(crusher.x, crusher.z);
            blocks.Add((new Vector2(crusher.x, crusher.z), 6.5f)); var legs = c + cd * (PitR + 7f); blocks.Add((new Vector2(legs.x, legs.z), 6.5f));
            mk.Box(rust, crusher + up * 4f, new Vector3(6f, 8f, 6f), Quaternion.LookRotation(cd)); solid.Box(rust, crusher + up * 4f, new Vector3(6f, 8f, 6f), Quaternion.LookRotation(cd));
            mk.Prism(rustDark, crusher + up * 8f, up, 3.2f, 3f, 8, 1.4f);
            for (var y = 1f; y < 8f; y += 2f) mk.Box(yellow, crusher + up * y + cd * 3.02f, new Vector3(6f, .3f, .04f), Quaternion.LookRotation(cd));
            // floodlight towers round the rim, facing in
            for (var k = 0; k < 6; k++)
            {
                var a = k * Mathf.PI / 3f + .3f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var p = c + d * (PitR + 6f);
                if (OnRamp(p.x, p.z, 2f) || !ctx.IsLand(p.x, p.z, 3f)) continue;
                Floodlight(ctx, mk, ctx.OnGround(p.x, p.z), -d, true);
            }
            // a chain-link fence round the rim, open at the haul road
            for (var k = 0; k < 48; k++)
            {
                float b0 = k * Mathf.PI / 24f, b1 = (k + 1) * Mathf.PI / 24f; var p0 = c + new Vector3(Mathf.Cos(b0), 0, Mathf.Sin(b0)) * (PitR + 2.5f); var p1 = c + new Vector3(Mathf.Cos(b1), 0, Mathf.Sin(b1)) * (PitR + 2.5f);
                if (OnRamp(p0.x, p0.z, 3f) || OnRamp(p1.x, p1.z, 3f) || !ctx.IsLand(p0.x, p0.z, 2f)) continue;
                Fence(ctx, mk, solid, p0, p1);
            }
            life.pit = c; life.rampTop = c + ramp * PitR; life.rampTop.y = rimY; life.rampBottom = c + ramp * FloorR; life.rampBottom.y = floorY;
            noGrass.Add(new Vector3(c.x, PitR + 3f, c.z));
            var mark = new GameObject("Shot: down into the quarry").transform; mark.SetParent(ctx.Root, false);
            var cam = c + rs * (PitR + 2f); cam.y = rimY + 6f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((c - cam).normalized));
        }

        /// A giant haul truck: a huge yellow-rust body, a tipping bed, wheels taller than you.
        void HaulTruck(LandContext ctx, MeshKit mk, Vector3 g, Vector3 dir, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(dir); var sd = Vector3.Cross(up, dir); var paint = i % 2 == 0 ? yellow : rust;
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -2.8f, 2.6f }) mk.Prism(dark, g + sd * e * 2.4f + dir * f + up * 1.6f - sd * e * .5f, sd * e, 1.6f, 1f, 12);
            mk.Box(paint, g + up * 3f + dir * 2.6f, new Vector3(5f, 2.2f, 2.6f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .35f, .4f), .8f), g + up * 3.6f + dir * 3.9f, new Vector3(2f, .9f, .05f), rot);
            mk.Box(paint, g + up * 4f - dir * 1.2f, new Vector3(5.4f, 2.2f, 6f), rot * Quaternion.Euler(-8f, 0, 0));
            mk.Box(rustDark, g + up * 3f - dir * 1.2f, new Vector3(5f, .4f, 5.6f), rot);
            BoxCol(ctx.Root, g + up * 2.6f, new Vector3(6.8f, 5.2f, 9f), rot);
        }

        /// An excavator: tracks, a cab, a long arm reaching out with a bucket.
        void Excavator(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            foreach (var e in new[] { -1f, 1f }) mk.Box(dark, g + sd * e * 1.4f + up * .5f, new Vector3(.9f, 1f, 4.4f), rot);
            mk.Box(yellow, g + up * 1.8f, new Vector3(3f, 1.6f, 3.2f), rot); mk.Box(WorldMaterials.Solid(new Color(.3f, .35f, .4f), .8f), g + up * 2.9f + sd * .7f + face * .6f, new Vector3(1.2f, 1.1f, 1.4f), rot);
            var shoulder = g + up * 2.6f + face * 1.4f - sd * .6f; var elbow = shoulder + face * 3.6f + up * 2.6f; var bucket = elbow + face * 2.4f - up * 3.2f;
            mk.Box(yellow, (shoulder + elbow) / 2f, new Vector3(.5f, .6f, Vector3.Distance(shoulder, elbow)), Quaternion.LookRotation(elbow - shoulder));
            mk.Box(yellow, (elbow + bucket) / 2f, new Vector3(.4f, .5f, Vector3.Distance(elbow, bucket)), Quaternion.LookRotation(bucket - elbow));
            mk.Box(rustDark, bucket, new Vector3(1.4f, 1f, 1f), rot);
            BoxCol(ctx.Root, g + up * 1.6f, new Vector3(3.6f, 3.2f, 4.4f), rot); BoxCol(ctx.Root, bucket + up * .2f, new Vector3(1.4f, 1.4f, 1.2f), rot);
        }

        // ---------------------------------------------------------------- paths
        /// Gravel tracks branching from the gate to every band, the haul road down into the pit to the rig stage; lamps on
        /// poles along them.
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var walk = 1; var lamps = new List<Vector3>();
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var straight = artist == "Nickelback" || artist == null;
                // the circles a lane walks round instead of across: the pit (with its fence), other artists' areas, the gate
                // cabin, the crusher and the conveyor's legs. Each is passed along an arc on the side its straight line passes.
                var rounds = new List<(Vector2 c, float r, float s)>();
                void AddRound(Vector2 c2, float r2) { var off = Vector2.Dot(c2 - new Vector2(from.x, from.z), new Vector2(side.x, side.z)); rounds.Add((c2, r2, off > 0f ? -1f : 1f)); }
                if (!straight)
                {
                    if (hasPit) AddRound(pit2, PitR + 7f);
                    foreach (var o in ctx.Artists) if (o.name != artist) AddRound(new Vector2(o.x, o.z), Foot(o) + 2f);
                    foreach (var (bc, br) in blocks) AddRound(bc, br);
                }
                Vector3 Round(Vector3 q)
                {
                    for (var pass = 0; pass < 2; pass++)
                        foreach (var (c2, r2, s2) in rounds)
                        {
                            var v = new Vector2(q.x, q.z) - c2; if (v.sqrMagnitude >= r2 * r2) continue;
                            var al = Mathf.Clamp(Vector2.Dot(v, new Vector2(dir.x, dir.z)), -r2, r2); var o2 = c2 + new Vector2(dir.x, dir.z) * al + new Vector2(side.x, side.z) * s2 * Mathf.Sqrt(r2 * r2 - al * al + .01f);
                            if (ctx.IsLand(o2.x, o2.y, 0f)) q = new Vector3(o2.x, 0, o2.y);
                        }
                    return q;
                }
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 14f : PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + (straight ? Vector3.zero : side * Mathf.Sin(dd / 14f) * Mathf.Min(2.5f, end * .04f));
                    q = Round(q);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    var gq = ctx.OnGround(q.x, q.z);
                    if (pts.Count > 0)
                    {   // where an arc stretches the spacing, fill in so every stretch of the lane follows the ground
                        var last = pts[pts.Count - 1]; var gap = new Vector2(gq.x - last.x, gq.z - last.z).magnitude;
                        for (var f = 1; f < Mathf.CeilToInt(gap / 1.5f); f++) { var m2 = Vector3.Lerp(last, gq, f / Mathf.Ceil(gap / 1.5f)); pts.Add(ctx.OnGround(m2.x, m2.z)); }
                    }
                    pts.Add(gq); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, gravel, PathW, .14f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 14)
                {
                    var lp = pts[i] + side * ((i / 14) % 2 == 0 ? 1f : -1f) * (PathW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 18f * 18f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f) || OnRamp(lp.x, lp.z, -1f) || NearPath(lp.x, lp.z, 2f) || InPit(lp.x, lp.z, 4f)) continue;
                    lamps.Add(lp); var g = ctx.OnGround(lp.x, lp.z); var inward = -side * ((i / 14) % 2 == 0 ? 1f : -1f);
                    mk.Prism(wood, g - up * .2f, up, .12f, 5.4f, 6); mk.Box(wood, g + up * 5.1f + inward * .5f, new Vector3(.08f, .08f, 1f), Quaternion.LookRotation(inward));
                    mk.Box(lampM, g + up * 4.95f + inward * .9f, new Vector3(.4f, .15f, .3f), Quaternion.LookRotation(inward)); halos.Add(g + up * 4.7f + inward * .9f, 1.8f, Sodium, .55f);
                    ArtistBits.PostCollider(ctx.Root, g, 5.2f, .14f);
                }
                walk++;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            // a lane never starts in the pit, inside another artist's area or under the gate cabin, crusher or conveyor
            bool NoJoin(Vector3 p)
            {
                if (InPit(p.x, p.z, 7f)) return true;
                foreach (var o in ctx.Artists) { float dx = p.x - o.x, dz = p.z - o.z, r = Foot(o) + 1.5f; if (dx * dx + dz * dz < r * r) return true; }
                foreach (var (bc, br) in blocks) if ((new Vector2(p.x, p.z) - bc).sqrMagnitude < br * br) return true;
                return false;
            }
            // the haul road first, from the gate straight down to Nickelback on the floor
            foreach (var a in ctx.Artists) if (a.name == "Nickelback") { Lay(ctx.OnGround(center.x, center.z), new Vector3(a.x, 0, a.z), Foot(a) * .8f, a.name); todo.Remove(a); }
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { if (NoJoin(p)) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- the wilds
        /// Round the rim: wrecked cars, oil drums burning, chain-link runs, rock piles, portable cabins, old tyres, spoil
        /// heaps, a few dead trees, power poles.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int cars = 0, drums = 0, fences = 0, piles = 0, cabins = 0, tyres = 0, trees = 0;
            const float cell = 21f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (InPit(px, pz, 8f) || !Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f); var face = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
                if (roll < .08f) { WreckedCar(ctx, mk, p, face, cars); cars++; }
                else if (roll < .14f) { Drum(ctx, mk, p); drums++; }
                else if (roll < .2f)
                {   // a fence run stops short of any path, artist's area, the pit and the haul road
                    var len = ctx.Range(9f, 18f); var ok = 0f;
                    for (var t = 0f; t <= len; t += 1f) { var q = p + face * t; if (!ctx.IsLand(q.x, q.z, 2f) || InFootprint(ctx, q.x, q.z, 2f) || NearPath(q.x, q.z, 3f) || InPit(q.x, q.z, 6f) || OnRamp(q.x, q.z, 2f) || InNoGrass(q.x, q.z, 1f) && t > 0f) break; ok = t; }
                    if (ok >= 4f) { Fence(ctx, mk, solid, p, p + face * ok); fences++; }
                }
                else if (roll < .3f) { var s = ctx.Range(2f, 4.5f); var lo = p.y; for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; lo = Mathf.Min(lo, ctx.Ground(px + Mathf.Cos(a) * s, pz + Mathf.Sin(a) * s)); } mk.Cone(gravel, new Vector3(p.x, lo, p.z), s, s * .7f, 9); BoxCol(ctx.Root, new Vector3(p.x, lo + s * .25f, p.z), new Vector3(s * 1.2f, s * .5f, s * 1.2f), Quaternion.identity); piles++; }
                else if (roll < .33f && cabins < 8) { var crot = Quaternion.LookRotation(face); Cabin(ctx, mk, solid, p, crot, new Color(.72f, .7f, .58f), 1.4f); mk.Box(GlowM(Sodium, 1.2f), p + Vector3.up * 1.7f + Vector3.Cross(Vector3.up, face) * 1.52f, new Vector3(.03f, .8f, 1.6f), crot); cabins++; }
                else if (roll < .38f) { for (var k = 0; k < 4; k++) mk.Prism(dark, p + Vector3.up * (.18f + k * .36f), Vector3.up, .6f, .35f, 12); ArtistBits.PostCollider(ctx.Root, p, 1.4f, .6f); tyres++; }
                else if (roll < .42f) { var h = ctx.Range(5f, 8f); mk.Prism(rustDark, p - Vector3.up * .3f, Vector3.up, .22f, h, 6, .5f); for (var k = 0; k < 4; k++) mk.Prism(rustDark, p + Vector3.up * h * (.45f + k * .1f), (Quaternion.Euler(0, k * 90f + 20f, 0) * Quaternion.Euler(50f, 0, 0) * Vector3.up).normalized, .08f, h * .3f, 5, .3f); ArtistBits.PostCollider(ctx.Root, p, h * .5f, .25f); trees++; }
            }
            Debug.Log($"The Nu Metal Quarry: pit {(hasPit ? "yes" : "no")}, {cars} wrecked cars, {drums} drums, {fences} fences, {piles} spoil heaps, {cabins} cabins, {tyres} tyre stacks, {trees} dead trees");
        }

        void WreckedCar(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            // it sits along the slope it rests on: tipped to the ground under its front, back and sides, a little sunk
            var sd0 = Vector3.Cross(Vector3.up, face); float hf = ctx.Ground(g.x + face.x * 2f, g.z + face.z * 2f), hb = ctx.Ground(g.x - face.x * 2f, g.z - face.z * 2f), hl = ctx.Ground(g.x - sd0.x * .9f, g.z - sd0.z * .9f), hr = ctx.Ground(g.x + sd0.x * .9f, g.z + sd0.z * .9f);
            var fw = (face * 4f + Vector3.up * (hf - hb)).normalized; var nrm = Vector3.Cross(fw, (sd0 * 1.8f + Vector3.up * (hr - hl)).normalized).normalized; if (nrm.y < 0f) nrm = -nrm;
            g.y = Mathf.Min((hf + hb + hl + hr) / 4f, g.y) - .05f;
            var rot = Quaternion.LookRotation(fw, nrm) * Quaternion.Euler(0, 0, i % 3 == 0 ? 8f : 0f); var up = rot * Vector3.up; face = rot * Vector3.forward; var paint = WorldMaterials.Solid(Color.Lerp(new[] { new Color(.6f, .15f, .12f), new Color(.2f, .35f, .55f), new Color(.75f, .75f, .7f) }[i % 3], new Color(.4f, .25f, .18f), .5f));
            mk.Box(paint, g + up * .55f, new Vector3(1.9f, .7f, 4.4f), rot); mk.Box(paint, g + up * 1.1f - face * .3f, new Vector3(1.7f, .5f, 2.2f), rot);
            mk.Box(dark, g + up * 1.12f - face * .3f, new Vector3(1.74f, .38f, 2.1f), rot); mk.Box(rustDark, g + up * .92f + face * 1.2f, new Vector3(1.6f, .04f, 1.4f), rot * Quaternion.Euler(-15f, 0, 0));
            BoxCol(ctx.Root, g + up * .75f, new Vector3(1.9f, 1.5f, 4.4f), rot);
        }

        void Scrub(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.5f, .5f, .35f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2001);
            const float st = 2.8f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (InPit(px, pz, 4f) || !ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 2f, pz * .04f + 5f) < .5f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.45f, .45f, .3f), new Color(.62f, .58f, .4f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .25f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, new Color(.85f, .8f, .6f), .3f), rng, 3, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Quarry scrub", ctx.Root, mat);
        }
    }
}
