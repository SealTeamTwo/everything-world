using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Fusion Isthmus as a giant record (part of FusionKit, 2026-11-16): the whole middle of the land is one record,
    /// 600 m across, laid flat with the canal town on its label. A silver spindle rises out of the square. Round the
    /// label, three grooves of water channels in glossy black vinyl, each ring of vinyl between them a track (signed
    /// TRACK 1, 2, 3 where you cross onto it) with its artists along a ring walk; fine groove lines run round every
    /// track, a few of them faintly lit. Seven spokes of paving cross the grooves on little arched bridges, from the town
    /// out past the rim. Outside the rim stands the tonearm: its pivot is a lookout tower with a spiral stair, and its
    /// arm reaches far in over the record to rest its needle in the second groove, where the water glows and ripples.
    /// The label has a printed cream ring round it: THE FUSION ISTHMUS, SIDE A, 33⅓ RPM.
    public partial class FusionKit
    {
        const float LabelR = 150f, RecordR = 300f, ChW = 6f, RingW = 4.2f;
        static readonly float[] Grooves = { 150f, 200f, 250f, 292f };   // inner edge of each groove channel (ChW wide)
        static readonly float[] Spokes = { 0f, 30f, 90f, 150f, 180f, 210f, 270f, 330f };   // degrees from east, toward north: they meet the label's streets
        static readonly string[][] Tracks =
        {
            new[] { "Tom Misch", "Yussef Dayes", "Ezra Collective", "Yussef Kamaal", "Tara Clerkin Trio" },
            new[] { "CASIOPEA", "Pat Metheny Group", "Allan Holdsworth", "Victor Wooten", "Medeski, Martin & Wood", "BADBADNOTGOOD" },
        };
        float armAngle = 315f; Material vinylM, grooveWaterM, grooveLineM, grooveLitM, chromeM, labelPrintM;

        Vector3 RecDir(float deg) { var a = deg * Mathf.Deg2Rad; return A * Mathf.Cos(a) + Nrm * Mathf.Sin(a); }
        Vector3 RecAt(float deg, float r, float lift = 0f) { var p = new Vector3(heart2.x, 0, heart2.y) + RecDir(deg) * r; p.y = quayY + lift; return p; }
        float RecR(float x, float z) => Vector2.Distance(new Vector2(x, z), heart2);
        /// On the black vinyl (between the label's edge and the rim).
        bool OnVinyl(float x, float z, float pad = 0f) { var r = RecR(x, z); return r > LabelR - 12f - pad && r < RecordR + 2f + pad; }
        static float BandInner(int b) => Grooves[b] + ChW;
        static float RingPathR(int b) => Grooves[b + 1] - 4f;   // on the track's outer edge: the artist areas face out to it

        // ---------------------------------------------------------------- shaping: one flat record
        void ShapeRecord(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index];
            // the tonearm stands outside the rim wherever there's room for its tower
            foreach (var a in new[] { 315f, 300f, 330f, 285f, 345f, 225f, 255f, 195f, 165f, 15f })
            {
                var ok = true; for (var r = RecordR + 8f; r <= RecordR + 52f; r += 4f) { var p = heart2 + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * r; if (data.IslandAt(p.x, p.y) != isl) { ok = false; break; } }
                if (ok) { armAngle = a; break; }
            }
            var pivot = heart2 + new Vector2(Mathf.Cos(armAngle * Mathf.Deg2Rad), Mathf.Sin(armAngle * Mathf.Deg2Rad)) * (RecordR + 30f);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue; var x = col * data.Step;
                    var d = Vector2.Distance(new Vector2(x, z), heart2);
                    // the record is dead flat; the land round it eases back to its own lie over 40 m, and the tonearm's
                    // tower stands on a flat round of its own
                    if (d <= RecordR + 6f) data.heights[i] = quayY;
                    else if (d < RecordR + 46f) data.heights[i] = Mathf.Lerp(quayY, data.heights[i], Ease(RecordR + 6f, RecordR + 46f, d));
                    var dp = Vector2.Distance(new Vector2(x, z), pivot); if (dp < 30f) data.heights[i] = Mathf.Lerp(quayY, data.heights[i], Ease(16f, 30f, dp));
                }
            }
        }

        /// Every artist onto a track: the biggest on Track 1, the older fusion players on Track 2, everyone else on
        /// Track 3; evenly round the ring, clear of the spokes and the tonearm.
        void PlaceOnRecord(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; var bands = new List<IslandWorldData.Artist>[3]; for (var b = 0; b < 3; b++) bands[b] = new List<IslandWorldData.Artist>();
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue; var b = 2;
                for (var t = 0; t < Tracks.Length; t++) if (System.Array.IndexOf(Tracks[t], a.name) >= 0) b = t;
                bands[b].Add(a);
            }
            for (var b = 0; b < 3; b++)
            {
                var list = bands[b]; if (list.Count == 0) continue; var step = 360f / list.Count;
                for (var k = 0; k < list.Count; k++)
                {
                    var a = list[k]; var r = RingPathR(b) - RingW / 2f - 2.5f - Foot(a);
                    for (var tryA = 0f; tryA < 360f; tryA += 3f)
                    {
                        var deg = 17f + b * 31f + k * step + tryA; var clear = true;
                        foreach (var s in Spokes) if (Mathf.Abs(Mathf.DeltaAngle(deg, s)) * Mathf.Deg2Rad * r < Foot(a) + 6f) clear = false;
                        if (Mathf.Abs(Mathf.DeltaAngle(deg, armAngle)) * Mathf.Deg2Rad * r < Foot(a) + 8f) clear = false;
                        var p = heart2 + new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)) * r;
                        foreach (var o in data.layout.artists) if (o != a && o.sceneId == isl.sceneId && Vector2.Distance(new Vector2(o.x, o.z), p) < Foot(o) + Foot(a) + 6f && list.IndexOf(o) < k && list.Contains(o)) clear = false;
                        if (!clear || data.IslandAt(p.x, p.y) != isl) continue;
                        a.x = p.x; a.z = p.y; break;
                    }
                }
            }
        }

        // ---------------------------------------------------------------- building it
        void RecordMaterials()
        {
            vinylM = WorldMaterials.Surface(new Color(.05f, .05f, .07f), Surface.Glass);   // black, glossy
            grooveWaterM = new Material(WorldMaterials.Glow(new Color(.07f, .36f, .42f), .55f)) { name = "Groove water" };
            grooveLineM = WorldMaterials.Glow(new Color(.16f, .18f, .22f), .12f); grooveLitM = WorldMaterials.Glow(new Color(.3f, .64f, .7f), .45f);
            chromeM = WorldMaterials.Surface(new Color(.86f, .88f, .92f), Surface.Metal); labelPrintM = WorldMaterials.Surface(new Color(.93f, .88f, .74f), Surface.Paper);
            life.grooveWater = grooveWaterM;
        }

        List<Vector3> RingPts(float r, float lift, float step = 3f)
        {
            var n = Mathf.Max(24, Mathf.CeilToInt(2f * Mathf.PI * r / step)); var pts = new List<Vector3>(n + 1);
            for (var k = 0; k <= n; k++) pts.Add(RecAt(k * 360f / n, r) + Vector3.up * lift * 0f);
            return pts;
        }

        bool NearBridge(float deg, float r, float halfW)
        {
            foreach (var s in Spokes) if (Mathf.Abs(Mathf.DeltaAngle(deg, s)) * Mathf.Deg2Rad * r < halfW) return true;
            return Mathf.Abs(Mathf.DeltaAngle(deg, armAngle)) * Mathf.Deg2Rad * r < halfW && r > Grooves[3] - 1f;
        }

        void Record(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            RecordMaterials();
            var up = Vector3.up; var bevel = MeshKit.Bevel; var soft = SoftEdges.On; MeshKit.Bevel = 0f; SoftEdges.On = false;   // the record is machine-made: crisp
            try
            {
                // the vinyl: one flat black ring from the label's edge to the rim
                const int segs = 288; var radii = new List<float>(); for (var r = LabelR; r < RecordR; r += 10f) radii.Add(r); radii.Add(RecordR);
                for (var i = 0; i + 1 < radii.Count; i++)
                    for (var k = 0; k < segs; k++)
                    {
                        float a0 = k * 360f / segs, a1 = (k + 1) * 360f / segs;
                        mk.Quad(vinylM, RecAt(a0, radii[i], .02f), RecAt(a1, radii[i], .02f), RecAt(a1, radii[i + 1], .02f), RecAt(a0, radii[i + 1], .02f), RecAt(a0, radii[i], -2f));
                    }
                // the label's printed ring and its lettering
                for (var k = 0; k < segs; k++) { float a0 = k * 360f / segs, a1 = (k + 1) * 360f / segs; mk.Quad(labelPrintM, RecAt(a0, LabelR - 12f, .03f), RecAt(a1, LabelR - 12f, .03f), RecAt(a1, LabelR, .03f), RecAt(a0, LabelR, .03f), RecAt(a0, LabelR - 6f, -2f)); }
                foreach (var (deg, text) in new[] { (65f, "THE FUSION ISTHMUS"), (115f, "33 1/3 RPM"), (170f, "STEREO"), (225f, "SIDE A"), (270f, "NO RUSH RECORDS"), (315f, "SIDE A"), (15f, "ALL RIGHTS RESERVED") })
                {
                    var t = ArtistBits.Label(rootT, text, RecAt(deg, LabelR - 6f, .05f), Vector3.up, 2.2f, new Color(.1f, .2f, .24f), TextAnchor.MiddleCenter);
                    t.transform.rotation = Quaternion.LookRotation(Vector3.down, -RecDir(deg));
                }
                // the grooves: water in a channel, a glossy black kerb each side (solid), open where a bridge crosses
                for (var g = 0; g < Grooves.Length; g++)
                {
                    var mid = Grooves[g] + ChW / 2f; Strip(mk, RingPts(mid, 0f), grooveWaterM, ChW - .5f, .05f, ctx);
                    // strings of lights across the water between posts on each bank
                    var sn = Mathf.CeilToInt(2f * Mathf.PI * mid / 20f);
                    for (var k = 0; k < sn; k++)
                    {
                        var deg = (k + .5f) * 360f / sn; if (NearBridge(deg, mid, 5f)) continue; var r0 = Grooves[g] - .5f; var r1 = Grooves[g] + ChW + .5f;
                        foreach (var rr in new[] { r0, r1 }) { mk.Prism(iron, RecAt(deg, rr) - up * .2f, up, .05f, 4f, 5); ArtistBits.PostCollider(ctx.Root, RecAt(deg, rr), 3.8f, .07f); }
                        BulbString(mk, RecAt(deg, r0, 3.75f), RecAt(deg, r1, 3.75f), k + g);
                    }
                    // a few houseboats moored along it, and rowing boats with lanterns drifting round (FusionLife)
                    for (var k = 0; k < 3; k++)
                    {
                        var deg = 25f + g * 47f + k * 120f; if (NearBridge(deg, mid, 9f)) deg += 9f;
                        var o = RecAt(deg, mid); var tan = Vector3.Cross(Vector3.up, RecDir(deg)); var rot = Quaternion.LookRotation(tan); var hull = WorldMaterials.Surface(Paints[(g + k) % Paints.Length], Surface.Painted);
                        mk.Box(hull, o - up * .1f, new Vector3(2.6f, .9f, 9f), rot); mk.Box(brassM, o + up * .36f, new Vector3(2.7f, .06f, 9.1f), rot); mk.Box(WorldMaterials.Surface(Cream, Surface.Painted), o + up * 1.1f - tan * 1f, new Vector3(2.1f, 1.4f, 4.2f), rot); mk.Box(woodM, o + up * 1.85f - tan * 1f, new Vector3(2.3f, .1f, 4.4f), rot);
                        for (var w = 0; w < 3; w++) foreach (var e in new[] { -1f, 1f }) mk.Box(windowM, o + up * 1.2f - tan * (2.4f - w * 1.3f) + Vector3.Cross(tan, up) * e * 1.06f, new Vector3(.04f, .55f, .7f), rot);
                        ArtistBits.PointLight(ctx.Root, o + up * 2.6f, Brass, 9f, 1.2f);
                    }
                    for (var k = 0; k < 2; k++)
                    {
                        var rm = new MeshKit(); rm.Box(woodM, new Vector3(0, .12f, 0), new Vector3(1.1f, .34f, 2.8f), Quaternion.identity); rm.Box(WorldMaterials.Surface(Paints[(g * 2 + k + 2) % Paints.Length], Surface.Painted), new Vector3(0, .3f, 0), new Vector3(1.2f, .06f, 2.9f), Quaternion.identity);
                        rm.Prism(iron, new Vector3(0, .3f, 1.2f), Vector3.up, .02f, .5f, 4); rm.Blob(bulbM[k % 3], new Vector3(0, .82f, 1.2f), Vector3.one * .12f, new System.Random(g * 5 + k), 0f);
                        var go = rm.BuildSingle("Rowing boat (groove)", life.transform, false); life.grooveBoats.Add(go.transform); life.grooveBoatWays.Add(new Vector3(mid, (k % 2 == 0 ? 1f : -1f) * (2.2f + g * .3f) / mid * Mathf.Rad2Deg, g * 90f + k * 180f));
                    }
                    foreach (var rb in new[] { Grooves[g] + .2f, Grooves[g] + ChW - .2f })
                    {
                        var n = Mathf.CeilToInt(2f * Mathf.PI * rb / 4f);
                        for (var k = 0; k < n; k++)
                        {
                            var deg = (k + .5f) * 360f / n; if (NearBridge(deg, rb, 2.8f)) continue;
                            var p = RecAt(deg, rb); var tan = Vector3.Cross(Vector3.up, RecDir(deg)); var len = 2f * Mathf.PI * rb / n + .05f; var rot = Quaternion.LookRotation(tan);
                            mk.Box(vinylM, p + up * .22f, new Vector3(.4f, .44f, len), rot); solid.Box(vinylM, p + up * .65f, new Vector3(.4f, 1.3f, len), rot);
                            // a railing along it, like the canals in town
                            mk.Prism(iron, p + up * .44f - tan * len * .5f, up, .03f, .66f, 4); mk.Box(brassM, p + up * 1.1f, new Vector3(.07f, .06f, len), rot); mk.Box(iron, p + up * .78f, new Vector3(.04f, .04f, len), rot);
                        }
                    }
                }
                // the rim: a raised glossy lip right round, with a hairline of brass
                {
                    var n = Mathf.CeilToInt(2f * Mathf.PI * RecordR / 4f);
                    for (var k = 0; k < n; k++)
                    {
                        var deg = (k + .5f) * 360f / n; if (NearBridge(deg, RecordR, 3.2f)) continue;
                        var p = RecAt(deg, RecordR - .6f); var tan = Vector3.Cross(Vector3.up, RecDir(deg)); var len = 2f * Mathf.PI * RecordR / n + .05f; var rot = Quaternion.LookRotation(tan);
                        mk.Box(vinylM, p + up * .35f, new Vector3(1.2f, .7f, len), rot); mk.Box(brassM, p + up * .71f, new Vector3(1.22f, .03f, len), rot); solid.Box(vinylM, p + up * .65f, new Vector3(1.2f, 1.3f, len), rot);
                    }
                }
                // the fine groove lines on each track, a few of them faintly lit; never under an artist's area
                for (var b = 0; b < 3; b++)
                {
                    var lineN = 0;
                    for (var r = BandInner(b) + 1.4f; r < RingPathR(b) - RingW / 2f - 1.4f; r += 2.2f, lineN++)
                    {
                        var m = lineN % 4 == 1 ? grooveLitM : grooveLineM; var run = new List<Vector3>(); var n = Mathf.CeilToInt(2f * Mathf.PI * r / 3f);
                        for (var k = 0; k <= n; k++)
                        {
                            var p = RecAt(k * 360f / n, r);
                            if (InFootprint(ctx, p.x, p.z, 1f) || NearBridge(k * 360f / n, r, 2.6f)) { if (run.Count > 1) Strip(mk, run, m, .14f, .04f, ctx); run = new List<Vector3>(); continue; }
                            run.Add(p);
                        }
                        if (run.Count > 1) Strip(mk, run, m, .14f, .04f, ctx);
                    }
                    // the ring walk round the track, lamps along it
                    var ring = RingPts(RingPathR(b), 0f, 2f); Strip(mk, ring, paving, RingW, .15f, ctx); foreach (var p in ring) Remember(p, 20 + b);
                    var lampN = Mathf.CeilToInt(2f * Mathf.PI * RingPathR(b) / 24f);
                    for (var k = 0; k < lampN; k++) { var deg = (k + .5f) * 360f / lampN; if (NearBridge(deg, RingPathR(b), 6f)) continue; var lp = RecAt(deg, RingPathR(b) + RingW / 2f + .9f); if (InFootprint(ctx, lp.x, lp.z, 1f)) continue; BrassLamp(ctx, mk, lp, RecDir(deg), k % 2 == 0); }
                }
                // the spokes: paving from the town out across the grooves on arched bridges and past the rim
                foreach (var s in Spokes) Spoke(ctx, mk, solid, s, true);
                Spoke(ctx, mk, solid, armAngle, false);
                Spindle(ctx, mk);
                Tonearm(ctx, mk, solid);
            }
            finally { MeshKit.Bevel = bevel; SoftEdges.On = soft; }
            Debug.Log($"The Fusion Isthmus record: {RecordR * 2f:0} m across, label {LabelR * 2f:0} m, {Grooves.Length} grooves, {Spokes.Length} spokes, tonearm at {armAngle:0} degrees");
        }

        void Spoke(LandContext ctx, MeshKit mk, MeshKit solid, float deg, bool intoTown)
        {
            var up = Vector3.up; var dir = RecDir(deg);
            // into town: from the label's edge to the green north of the canal or to the square
            if (intoTown && Mathf.Abs(Vector3.Dot(dir, Nrm)) < .9f)   // (north and south: the avenue is already there)
            {
                var start = RecAt(deg, LabelR - .5f); var street = Mathf.Abs(Vector3.Dot(dir, Nrm)) < .2f ? 0f : Mathf.Sign(Vector3.Dot(dir, Nrm)) * 74f;
                var target = new Vector3(center.x, 0, center.z) + Nrm * street; start = target + A * Vector3.Dot(start - target, A); start.y = 0;
                var d = target - start; d.y = 0; var len = d.magnitude; var pts = new List<Vector3>(); var stop = street == 0f ? 18.5f : AvenueW / 2f + .3f;   // to the square's edge, or the avenue
                for (var t = 0f; t <= len - stop; t += 2f) { var q = start + d / len * t; pts.Add(ctx.OnGround(q.x, q.z)); }
                if (pts.Count > 1) { Strip(mk, pts, paving, RingW, .17f, ctx); foreach (var p in pts) Remember(p, 30); }
            }
            // across the vinyl, a bridge over each groove
            var from = intoTown ? LabelR - 1f : RingPathR(2);
            for (var g = 0; g < Grooves.Length; g++)
            {
                var a0 = g == 0 ? from : Grooves[g - 1] + ChW + .2f; var a1 = Grooves[g] - .2f;
                if (a1 > a0 + 1f && (intoTown || g == Grooves.Length - 1)) { var pts = new List<Vector3>(); for (var r = a0; r <= a1; r += 2f) pts.Add(RecAt(deg, r)); pts.Add(RecAt(deg, a1)); Strip(mk, pts, paving, RingW, .18f, ctx); foreach (var p in pts) Remember(p, 30); }
                if (!intoTown && g < Grooves.Length - 1) continue;
                Bridge(ctx, mk, solid, deg, Grooves[g] + ChW / 2f);
                if (intoTown) SignAt(ctx, mk, deg, g);
            }
            // out past the rim
            { var pts = new List<Vector3>(); for (var r = Grooves[3] + ChW + .2f; r <= RecordR + (intoTown ? 12f : 22f); r += 2f) pts.Add(ctx.OnGround(RecAt(deg, r).x, RecAt(deg, r).z)); Strip(mk, pts, paving, RingW, .18f, ctx); foreach (var p in pts) Remember(p, 30); }
        }

        /// A little arched bridge across a groove, brass-railed.
        void Bridge(LandContext ctx, MeshKit mk, MeshKit solid, float deg, float r)
        {
            var up = Vector3.up; var dir = RecDir(deg); var side = Vector3.Cross(up, dir); var c = RecAt(deg, r); const float half = ChW / 2f + 1.4f; var rot = Quaternion.LookRotation(dir);
            foreach (var e in new[] { -1f, 1f }) { var m = c + dir * e * half / 2f + up * .32f; var r2 = rot * Quaternion.Euler(e * 5f, 0, 0); mk.Box(stoneM, m, new Vector3(RingW, .22f, half + .1f), r2); solid.Box(stoneM, m - up * .2f, new Vector3(RingW, .5f, half + .1f), r2); }
            foreach (var e in new[] { -1f, 1f })
            {
                var rp = c + side * e * (RingW / 2f - .08f); for (var t = -half; t <= half + .01f; t += half / 3f) mk.Prism(brassM, rp + dir * t + up * (.3f + .2f * (1f - Mathf.Abs(t) / half)), up, .03f, .8f, 4);
                mk.Box(brassM, rp + up * 1.25f, new Vector3(.07f, .06f, half * 2f), rot); solid.Box(brassM, rp + up * .8f, new Vector3(.14f, 1.4f, half * 2f), rot);
            }
        }

        /// Where a spoke crosses onto a track: a brass sign, TRACK 1, 2 or 3, facing you as you come out from the town.
        void SignAt(LandContext ctx, MeshKit mk, float deg, int g)
        {
            if (g > 2) return; var dir = RecDir(deg); var side = Vector3.Cross(Vector3.up, dir); var p = RecAt(deg, BandInner(g) + 1.6f) + side * (RingW / 2f + 1.3f);
            foreach (var e in new[] { -.9f, .9f }) mk.Prism(iron, p + side * e - Vector3.up * .2f, Vector3.up, .06f, 2.6f, 5);
            BrassSign(mk, p + Vector3.up * 2.1f, -dir, 2.4f, .7f, "TRACK " + (g + 1), .3f, new Color(.08f, .09f, .12f)); ArtistBits.PostCollider(ctx.Root, p, 2.4f, .5f);
        }

        /// The spindle: a silver column rising out of the middle of the square, taller than the houses.
        void Spindle(LandContext ctx, MeshKit mk)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(brassM, c, up, 2.7f, .45f, 24); mk.Prism(chromeM, c + up * .4f, up, 1.8f, 22f, 24); mk.Prism(chromeM, c + up * 22.4f, up, 1.8f, 1.4f, 24, .45f);
            mk.Prism(new Material(WorldMaterials.Glow(new Color(.6f, .95f, 1f), 1.4f)) { name = "Spindle ring" }, c + up * 21f, up, 1.85f, .25f, 24); halos.Add(c + up * 21f, 3f, new Color(.6f, .95f, 1f), .5f);
            ArtistBits.PostCollider(ctx.Root, c, 23f, 2f); ArtistBits.PointLight(ctx.Root, c + up * 6f, new Color(.7f, .9f, 1f), 16f, 1.6f);
        }

        /// The tonearm: its pivot is a lookout tower outside the rim, climbed by a spiral stair to a railed platform;
        /// the arm reaches in over the record and rests its needle in the second groove.
        void Tonearm(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pv = RecAt(armAngle, RecordR + 30f); pv.y = ctx.Ground(pv.x, pv.z); noGrass.Add(new Vector3(pv.x, 16f, pv.z)); var darkM = WorldMaterials.Surface(new Color(.12f, .12f, .14f), Surface.Metal);
            const float H = 14.4f, Rc = 2.6f, Ri = 3f, Ro = 5.4f; const int steps = 56;
            mk.Prism(darkM, pv - up * .3f, up, 8.5f, 1f, 32); solid.Box(darkM, pv + up * .2f, new Vector3(1f, .1f, 1f), Quaternion.identity);
            mk.Prism(chromeM, pv, up, Rc, H + 1.6f, 20); ArtistBits.PostCollider(ctx.Root, pv, H + 1.6f, Rc + .1f);
            var face = (new Vector3(heart2.x, 0, heart2.y) - new Vector3(pv.x, 0, pv.z)).normalized; var a0 = Mathf.Atan2(-face.z, -face.x) * Mathf.Rad2Deg;   // the stair starts on the outer side
            for (var k = 0; k < steps; k++)
            {
                var a = (a0 + k * 16f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var tan = new Vector3(-d.z, 0, d.x); var h = (k + 1) * H / steps;
                var sp = pv + d * ((Ri + Ro) / 2f) + up * (h - .12f); var rot = Quaternion.LookRotation(tan);
                mk.Box(brassM, sp, new Vector3(Ro - Ri, .12f, .95f), rot); solid.Box(brassM, sp - up * .1f, new Vector3(Ro - Ri, .32f, .95f), rot);
                var rail = pv + d * (Ro - .05f) + up * h; mk.Prism(iron, rail - up * .05f, up, .025f, 1.05f, 4); if (k % 2 == 0) mk.Box(brassM, rail + up * 1f, new Vector3(.06f, .05f, 1.2f), rot); solid.Box(iron, rail + up * .6f, new Vector3(.1f, 1.3f, 1.05f), rot);
            }
            // the platform at the top, railed all round but where the stair arrives
            var top = pv + up * H; mk.Prism(darkM, top - up * .3f, up, Ro + 1.6f, .3f, 32); solid.Box(darkM, top - up * .15f, new Vector3(Ro * 2f + 3f, .3f, Ro * 2f + 3f), Quaternion.identity);
            for (var k = 0; k < 32; k++)
            {
                var a = k * 360f / 32f; var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)); if (Mathf.Abs(Mathf.DeltaAngle(a, a0 + steps * 16f)) < 22f) continue;
                var rp = top + d * (Ro + 1.45f); var rot = Quaternion.LookRotation(new Vector3(-d.z, 0, d.x)); mk.Prism(iron, rp, up, .03f, 1.05f, 4); mk.Box(brassM, rp + up * 1.05f, new Vector3(.06f, .05f, 1.4f), rot); solid.Box(iron, rp + up * .6f, new Vector3(.12f, 1.3f, 1.4f), rot);
            }
            // the arm: from the pivot head, in over the record, down to the headshell; a counterweight behind
            var head = top + up * 2.2f; var needleAt = RecAt(armAngle - 26f, Grooves[1] + ChW / 2f, .05f); var shell = needleAt + up * 3.2f;
            mk.Prism(chromeM, head, up, 1.4f, 1.2f, 16); var armDir = (shell - head).normalized; mk.Prism(chromeM, head + up * .6f, armDir, .55f, Vector3.Distance(head + up * .6f, shell), 12);
            mk.Prism(darkM, head + up * .6f - armDir * .5f, -armDir, 1.6f, 3.2f, 16);
            var sr = Quaternion.LookRotation(new Vector3(armDir.x, 0, armDir.z)); mk.Box(darkM, shell, new Vector3(1.6f, .5f, 3.4f), sr); mk.Box(brassM, shell - up * .45f + sr * Vector3.forward * .6f, new Vector3(.9f, .5f, 1.2f), sr);
            mk.Prism(chromeM, shell - up * .7f + sr * Vector3.forward * .6f, -up, .06f, 2.4f, 6, .2f);
            var glow = new Material(WorldMaterials.Glow(new Color(.55f, 1f, 1f), 2.4f)) { name = "Needle glow" }; mk.Prism(glow, needleAt - up * .02f, up, 1.1f, .04f, 16);
            halos.Add(needleAt + up * .4f, 3.5f, new Color(.55f, 1f, 1f), .7f); ArtistBits.PointLight(ctx.Root, needleAt + up * 1.5f, new Color(.55f, 1f, 1f), 18f, 2.6f);
            life.needle = needleAt; life.needleGlow = glow;
            Shot(ctx, "the record from the tonearm", top + up * 1.7f + face * 2f, new Vector3(heart2.x, quayY, heart2.y));
            Shot(ctx, "the tonearm from the grooves", RecAt(armAngle - 50f, RingPathR(1), 1.7f), needleAt + up * 4f);
        }
    }
}
