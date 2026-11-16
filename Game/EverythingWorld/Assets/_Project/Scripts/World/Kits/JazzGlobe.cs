using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Old Jazz Quarter in a rain globe (part of JazzKit, 2026-11-16): the whole town under a giant glass dome,
    /// 680 m across and 190 m to the top, standing on a carved wooden base. It rains only inside; outside the glass
    /// it is a dry, clear, starry night (JazzLife). The town grows out to fill it: a ring street close inside the glass
    /// with tall dark houses along it, a second ring of streets nearer the middle, and a promenade along the glass with
    /// benches facing out at the stars. Brass revolving doors in the glass face each neighbouring land, with a lane out
    /// to it; a brass plate on the base gives the town's name.
    public partial class JazzKit
    {
        const float GlobeR = 340f, GlobeTop = 190f, RingOuterR = 300f, RingInnerR = 205f;
        readonly List<float> doors = new List<float>();   // degrees (from east toward north) of the revolving doors
        readonly Dictionary<float, Vector3> doorTo = new Dictionary<float, Vector3>();   // where a door's lane leads, if not straight out (a causeway's end)
        Material glassDomeM, woodBaseM;

        float GlobeDist(float x, float z) => Vector2.Distance(new Vector2(x, z), heart2);
        bool InGlobe(float x, float z, float pad = 0f) => GlobeDist(x, z) < GlobeR + pad;
        Vector3 GDir(float deg) { var a = deg * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); }
        Vector3 GAt(LandContext ctx, float deg, float r) { var p = new Vector3(heart2.x, 0, heart2.y) + GDir(deg) * r; return ctx.OnGround(p.x, p.z); }
        bool NearDoor(float deg, float r, float halfW) { foreach (var d in doors) if (Mathf.Abs(Mathf.DeltaAngle(deg, d)) * Mathf.Deg2Rad * r < halfW) return true; return false; }

        /// Outside the glass the ground is a dark night meadow with colour in it; inside, the town's greys.
        public override bool GroundAt(float x, float z, float height, float slope, float patch, out Color color)
        {
            Ground(height, slope, patch, out color);
            var out_ = Mathf.Clamp01((GlobeDist(x, z) - GlobeR - 2f) / 10f); if (out_ <= 0f) return true;
            var night = Color.Lerp(new Color(.1f, .2f, .22f), new Color(.17f, .27f, .24f), Mathf.SmoothStep(0f, 1f, patch));
            color = Color.Lerp(color, night, out_); color.a = 1f; return true;
        }

        void GlobeMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            glassDomeM = new Material(lit) { name = "Rain globe glass" };
            glassDomeM.SetFloat("_Surface", 1f); glassDomeM.SetFloat("_Blend", 0f); glassDomeM.SetFloat("_Cull", 0f);
            glassDomeM.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); glassDomeM.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); glassDomeM.SetFloat("_ZWrite", 0f);
            glassDomeM.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glassDomeM.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            glassDomeM.SetColor("_BaseColor", new Color(.78f, .86f, .96f, .1f)); glassDomeM.SetFloat("_Smoothness", .96f); glassDomeM.SetFloat("_Metallic", .15f);
            glassDomeM.EnableKeyword("_EMISSION"); glassDomeM.SetColor("_EmissionColor", new Color(.05f, .06f, .08f));
            // rain running down the inside of the glass: streaks of brighter water (JazzLife scrolls them downward)
            const int w = 64, h = 256; var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Rain on glass", wrapMode = TextureWrapMode.Repeat };
            var rng = new System.Random(77); var px = new Color[w * h];
            for (var i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, .7f);
            for (var k = 0; k < 26; k++) { var x = rng.Next(w); var y0 = rng.Next(h); var len = 20 + rng.Next(90); for (var y = 0; y < len; y++) { var yy = (y0 + y) % h; var a = 1f - y / (float)len; px[yy * w + x] = new Color(1f, 1f, 1f, .7f + 1.8f * a); if (x + 1 < w) px[yy * w + x + 1] = new Color(1f, 1f, 1f, .7f + .8f * a); } }
            tex.SetPixels(px); tex.Apply(true); glassDomeM.SetTexture("_BaseMap", tex);
            woodBaseM = WorldMaterials.Surface(new Color(.3f, .17f, .09f), Surface.Wood);
            life.globeGlass = glassDomeM;
        }

        /// Where the revolving doors go: one facing each land this one borders over ground (toward the nearest point of
        /// the shared border), at least two.
        void PlanDoors(LandContext ctx)
        {
            doors.Clear(); doorTo.Clear(); var best = new Dictionary<string, (float deg, float d)>(); var isles = new Dictionary<string, IslandWorldData.Island>();
            // a door facing every bridge or causeway that lands on this land, its lane running to the bridge's end
            foreach (var b in ctx.Data.layout.bridges)
            {
                if (b.a != ctx.Island.sceneId && b.b != ctx.Island.sceneId) continue;
                var path = IslandWorldBuilder.BridgePath(ctx.Data, b); if (path.Count < 2) continue; var end = b.a == ctx.Island.sceneId ? path[0] : path[path.Count - 1];
                var deg = Mathf.Atan2(end.z - heart2.y, end.x - heart2.x) * Mathf.Rad2Deg; doors.Add(deg); doorTo[deg] = end;
            }
            for (var x = ctx.MinX; x <= ctx.MaxX; x += 6f)
            for (var z = ctx.MinZ; z <= ctx.MaxZ; z += 6f)
            {
                if (ctx.Data.IslandAt(x, z) != ctx.Island || ctx.Ground(x, z) < waterY + .5f) continue;
                foreach (var (dx, dz) in new[] { (6f, 0f), (-6f, 0f), (0f, 6f), (0f, -6f) })
                {
                    var o = ctx.Data.IslandAt(x + dx, z + dz); if (o == null || o == ctx.Island || ctx.Ground(x + dx, z + dz) < waterY + .5f) continue;
                    var d = GlobeDist(x, z); var deg = Mathf.Atan2(z - heart2.y, x - heart2.x) * Mathf.Rad2Deg;
                    if (!best.TryGetValue(o.name, out var b) || d < b.d) best[o.name] = (deg, d); isles[o.name] = o;
                }
            }
            // toward each neighbour: at the crossing point it uses too (so this door and its gate line up); never two doors
            // close together (a causeway's door comes first)
            foreach (var kv in best)
            {
                var cp = LandCrossings.Point(ctx.Data, ctx.Island, isles[kv.Key]); var deg = cp.HasValue ? Mathf.Atan2(cp.Value.z - heart2.y, cp.Value.x - heart2.x) * Mathf.Rad2Deg : kv.Value.deg;
                var ok = true; foreach (var d0 in doors) if (Mathf.Abs(Mathf.DeltaAngle(d0, deg)) < 40f) ok = false; if (!ok) continue;
                doors.Add(deg); if (cp.HasValue) doorTo[deg] = cp.Value;
            }
            var down = Mathf.Atan2(-U.z, -U.x) * Mathf.Rad2Deg; if (doors.Count < 2) { var ok = true; foreach (var d0 in doors) if (Mathf.Abs(Mathf.DeltaAngle(d0, down)) < 40f) ok = false; if (ok) doors.Add(down); }
            if (doors.Count < 2) doors.Add(down + 180f);
            // and more wherever the glass runs a long way without one: never more than about a quarter turn between doors
            for (var guard = 0; guard < 8; guard++)
            {
                doors.Sort(); var gap = 0f; var at = 0f;
                for (var k = 0; k < doors.Count; k++) { var a = doors[k]; var b = k + 1 < doors.Count ? doors[k + 1] : doors[0] + 360f; if (b - a > gap) { gap = b - a; at = a + (b - a) / 2f; } }
                if (gap <= 95f) break; doors.Add(Mathf.Repeat(at, 360f));
            }            Debug.Log($"The Old Jazz Quarter: {doors.Count} revolving doors in the glass ({string.Join(", ", best.Keys)})");
        }

        // ---------------------------------------------------------------- the globe
        void Globe(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            GlobeMaterials();
            var up = Vector3.up; var c0 = new Vector3(heart2.x, 0, heart2.y); var topY = quayY + GlobeTop; const int segs = 144, rings = 30;
            float BaseY(float deg) { var lo = float.MaxValue; for (var s = -1; s <= 1; s++) { var p = c0 + GDir(deg + s * 1.2f) * GlobeR; lo = Mathf.Min(lo, ctx.Ground(p.x, p.z)); } return lo + 2.6f; }
            var baseY = new float[segs + 1]; for (var k = 0; k <= segs; k++) baseY[k] = BaseY(k * 360f / segs);
            // the glass: an ellipsoid cap from the base's top to the crown; its texture runs down the glass
            var dome = new MeshKit();
            Vector3 D(int k, int j) { var f = j / (float)rings; var ang = f * Mathf.PI / 2f; var r = GlobeR * Mathf.Cos(ang); var by = baseY[k]; var y = by + (topY - by) * Mathf.Sin(ang); return c0 + GDir(k * 360f / segs) * r + up * y; }
            Vector2 UV(int k, int j) => new Vector2(k * 360f / segs / 3f, (rings - j) * 1.6f);
            for (var k = 0; k < segs; k++)
                for (var j = 0; j < rings; j++)
                {
                    Vector3 a = D(k, j), b = D(k + 1, j), cc = D(k + 1, j + 1), d = D(k, j + 1); var inside = c0 + up * quayY;
                    dome.Tri(glassDomeM, a, b, cc, inside, UV(k, j), UV(k + 1, j), UV(k + 1, j + 1)); dome.Tri(glassDomeM, a, cc, d, inside, UV(k, j), UV(k + 1, j + 1), UV(k, j + 1));
                }
            var go = dome.BuildSingle("The rain globe", ctx.Root, false); go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // the wooden base all round, a brass band along its top; the glass can't be walked through but at the doors
            for (var k = 0; k < segs; k++)
            {
                var deg = (k + .5f) * 360f / segs; var p = c0 + GDir(deg) * (GlobeR + 2f); var g = ctx.Ground(p.x, p.z); var top = (baseY[k] + baseY[k + 1]) / 2f; var low = Mathf.Min(g, top - 2.6f) - .6f;
                var len = 2f * Mathf.PI * (GlobeR + 2f) / segs + .1f; var rot = Quaternion.LookRotation(Vector3.Cross(up, GDir(deg)));
                if (NearDoor(deg, GlobeR, 5f)) continue;
                mk.Box(woodBaseM, new Vector3(p.x, (top + low) / 2f, p.z), new Vector3(5f, top - low, len), rot); mk.Box(brassM, new Vector3(p.x, top + .06f, p.z) - GDir(deg) * 2.4f, new Vector3(.3f, .14f, len), rot);
                mk.Box(brassM, new Vector3(p.x, top - .9f, p.z) + GDir(deg) * 2.52f, new Vector3(.06f, .12f, len), rot);
                solid.Box(woodBaseM, new Vector3(p.x, top + 4f, p.z) - GDir(deg) * 2f, new Vector3(1f, 14f, len), rot);
            }
            // a brass nameplate on the base, facing downhill
            var downDeg = Mathf.Atan2(-U.z, -U.x) * Mathf.Rad2Deg; if (NearDoor(downDeg, GlobeR, 14f)) downDeg += 12f;
            { var p = c0 + GDir(downDeg) * (GlobeR + 4.6f); var top = BaseY(downDeg); var face = GDir(downDeg); var pp = new Vector3(p.x, top - 1.3f, p.z); mk.Box(brassM, pp, new Vector3(12f, 1.6f, .12f), Quaternion.LookRotation(face)); ArtistBits.Label(rootT, "THE OLD JAZZ QUARTER", pp + face * .08f, face, .9f, Ink, TextAnchor.MiddleCenter); halos.Add(pp + face, 4f, Brass, .3f); }
            foreach (var d in doors) Door(ctx, mk, solid, d, BaseY(d));
            Debug.Log($"The Old Jazz Quarter: the rain globe, {GlobeR * 2f:0} m across, {GlobeTop:0} m high");
        }

        /// A brass revolving door in the glass: two pillars and an arch, four glass leaves turning round a post, a lane
        /// in to the ring street and out to the neighbouring land.
        void Door(LandContext ctx, MeshKit mk, MeshKit solid, float deg, float top)
        {
            var up = Vector3.up; var dir = GDir(deg); var side = Vector3.Cross(up, dir); var p = GAt(ctx, deg, GlobeR + 1f); var rot = Quaternion.LookRotation(dir);
            foreach (var e in new[] { -1f, 1f }) { var q = p + side * e * 3.4f; mk.Prism(brassM, q - up * .5f, up, .45f, 7.5f, 10); mk.Prism(brassM, q + up * 7f, up, .6f, .5f, 10); solid.Box(brassM, q + up * 3f, new Vector3(1f, 8f, 1f), rot); }
            mk.Box(brassM, p + up * 7.4f, new Vector3(7.8f, .5f, .8f), Quaternion.LookRotation(side)); halos.Add(p + up * 7.6f, 3f, Brass, .4f); ArtistBits.PointLight(ctx.Root, p + up * 6f, Brass, 14f, 1.8f);
            mk.Prism(brassM, p - up * .1f, up, 2.9f, .18f, 24); mk.Prism(brassM, p + up * 3.4f, up, 2.9f, .12f, 24);
            var rm = new MeshKit(); for (var k = 0; k < 4; k++) { var a = Quaternion.Euler(0, k * 90f, 0); rm.Box(glassDomeM, a * new Vector3(0, 1.7f, 1.35f), new Vector3(.06f, 3.2f, 2.6f), a); rm.Box(brassM, a * new Vector3(0, 3.3f, 1.35f), new Vector3(.1f, .1f, 2.6f), a); }
            rm.Prism(brassM, Vector3.zero, up, .12f, 3.4f, 8); var rgo = rm.BuildSingle("Revolving door", life.transform, false); rgo.transform.position = p; life.doors.Add(rgo.transform);
            ArtistBits.PostCollider(ctx.Root, p, 3.4f, .15f);
            // the lanes: in to the ring street, out toward the neighbour
            var pts = new List<Vector3>(); for (var r = RingOuterR + 2f; r <= GlobeR + 30f; r += 2f) pts.Add(GAt(ctx, deg, r)); Strip(mk, pts, cobble, PathW, .15f, ctx); foreach (var q in pts) Remember(q, 40);
            var outPts = new List<Vector3>();
            if (doorTo.TryGetValue(deg, out var to))
            {   // to the causeway's end
                var from = GAt(ctx, deg, GlobeR + 30f); var d = to - from; d.y = 0; var len = d.magnitude;
                for (var t = 0f; t <= len; t += 2f) { var q = from + d / len * t; if (!ctx.IsLand(q.x, q.z, 0f)) break; outPts.Add(ctx.OnGround(q.x, q.z)); }
            }
            else for (var r = GlobeR + 30f; r <= GlobeR + 160f; r += 2f) { var q = GAt(ctx, deg, r); if (!ctx.IsLand(q.x, q.z, 0f) || ctx.Data.IslandAt(q.x, q.z) != ctx.Island) break; outPts.Add(q); }
            if (outPts.Count > 1) { Strip(mk, outPts, cobble, PathW, .15f, ctx); foreach (var q in outPts) Remember(q, 41); for (var i = 6; i < outPts.Count; i += 14) StreetLamp(ctx, mk, outPts[i] + side * (PathW / 2f + .9f) * (i % 28 == 6 ? 1f : -1f), -side, true); }
            Mark(p, 7f);
        }

        // ---------------------------------------------------------------- the city grown out to the glass
        /// Two ring streets and the houses along them, and a promenade inside the glass with benches facing out.
        void RingStreets(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var houses = 0;
            foreach (var rr in new[] { RingInnerR, RingOuterR })
            {
                // the street: cobbles right round, lamps along it; it passes round artist areas and the river
                var run = new List<Vector3>(); var n = Mathf.CeilToInt(2f * Mathf.PI * rr / 2f);
                void Flush() { if (run.Count > 1) { Strip(mk, run, cobble, StreetW, .15f, ctx); foreach (var q in run) Remember(q, rr < 250f ? 20 : 21); } run = new List<Vector3>(); }
                for (var k = 0; k <= n; k++) { var deg = k * 360f / n; var q = GAt(ctx, deg, rr); if (!ctx.IsLand(q.x, q.z, 3f) || InFootprint(ctx, q.x, q.z, 1.5f) || InRiver(q.x, q.z, 3f)) { Flush(); continue; } run.Add(q); }
                Flush();
                var ln = Mathf.CeilToInt(2f * Mathf.PI * rr / 26f);
                for (var k = 0; k < ln; k++) { var deg = (k + .5f) * 360f / ln; var lp = GAt(ctx, deg, rr + (k % 2 == 0 ? 1f : -1f) * (StreetW / 2f + .9f)); if (InFootprint(ctx, lp.x, lp.z, 1f) || InRiver(lp.x, lp.z, 2f) || NearDoor(deg, rr, 4f)) continue; fixtures.Add(new Vector3(lp.x, .3f, lp.z)); StreetLamp(ctx, mk, lp, (k % 2 == 0 ? -1f : 1f) * GDir(deg), k % 3 == 0); }
            }
            // the houses: along the inside of the outer ring, and both sides of the inner one, with gaps for alleys
            var i = 400; var off = StreetW / 2f + 2.6f + HouseD / 2f;
            bool Free(Vector3 p) => ctx.IsLand(p.x, p.z, 8f) && !InFootprint(ctx, p.x, p.z, 6.6f) && !LaneNear(p.x, p.z, 7.6f) && !InRiver(p.x, p.z, 6f) && !NearHouse(p, 10.4f);
            foreach (var (rr, sides) in new[] { (RingOuterR, new[] { -1f }), (RingInnerR, new[] { -1f, 1f }) })
                foreach (var e in sides)
                {
                    var r = rr + e * off; var n = Mathf.FloorToInt(2f * Mathf.PI * r / (HouseW + .6f));
                    for (var k = 0; k < n; k++, i++)
                    {
                        if ((k * 7 + (int)rr) % 9 < 2) continue;                           // an alley now and then
                        var deg = k * 360f / n; if (NearDoor(deg, r, 9f)) continue; var p = GAt(ctx, deg, r); if (!Free(p)) continue;
                        House(ctx, mk, solid, new PlaceFrame(p, -GDir(deg) * e), i); houseAt.Add(p); houses++;
                    }
                }
            // the promenade along the glass: lamps, benches facing out at the dry night
            var pn = Mathf.CeilToInt(2f * Mathf.PI * (GlobeR - 12f) / 34f);
            for (var k = 0; k < pn; k++)
            {
                var deg = (k + .5f) * 360f / pn; if (NearDoor(deg, GlobeR - 12f, 8f)) continue; var bq = GAt(ctx, deg, GlobeR - 10f); if (!ctx.IsLand(bq.x, bq.z, 4f) || InFootprint(ctx, bq.x, bq.z, 2f)) continue;
                Bench(mk, bq, GDir(deg)); var lp = GAt(ctx, deg + 1.6f, GlobeR - 11f); StreetLamp(ctx, mk, lp, GDir(deg), k % 2 == 0); Mark(bq, 2.5f);
            }
            Debug.Log($"The Old Jazz Quarter: the town grown out to the glass, {houses} more houses on two ring streets");
        }

        readonly List<Vector3> houseAt = new List<Vector3>();
        bool NearHouse(Vector3 p, float d) { foreach (var q in houseAt) if ((new Vector2(q.x - p.x, q.z - p.z)).sqrMagnitude < d * d) return true; return false; }

        /// The stars: high over the glass, out of the rain. Seen from outside always; from inside, only when Alice
        /// Coltrane or Astral Traveling clears the rain (JazzLife).
        void GlobeStars(LandContext ctx, MeshKit mk)
        {
            var rng = new System.Random(1971); var c0 = new Vector3(heart2.x, quayY, heart2.y);
            for (var k = 0; k < 320; k++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var d = 40f + (float)rng.NextDouble() * 900f; var h = GlobeTop + 60f + (float)rng.NextDouble() * 220f;
                mk.Blob(starM, c0 + new Vector3(Mathf.Cos(a) * d, h, Mathf.Sin(a) * d), Vector3.one * (.8f + (float)rng.NextDouble() * 1.6f), rng, 0f);
            }
        }
    }
}
