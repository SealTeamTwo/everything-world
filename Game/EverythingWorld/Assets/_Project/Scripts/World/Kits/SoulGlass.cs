using System;
using System.Collections.Generic;
using EverythingWorld.Player;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Soul Lowlands as a factory built like a cathedral, all of stained glass (part of SoulKit). Much of this
    /// music came off a production line, so the river town is an assembly plant, and the plant is made of the one
    /// thing a plant is never made of:
    /// - every wall, roof and street of the town is cut into glowing panes with dark lead between them
    ///   (MeshKit.StainedGlass), each pane taking its colour from what the thing was: pale things go to cream, gold
    ///   and rose, deep things to cobalt, violet and near-black indigo. It reads by brightness, pale glass against
    ///   deep, lit from inside at dusk (the owner is colourblind);
    /// - the Line: two conveyor belts of pale glass tiles run the length of Main Street and River Road, one down to
    ///   the landing and one back, and they carry you if you stand on them (SoulWorks). You can step off whenever
    ///   you like;
    /// - over the Line a nave of pointed glass arches, a great rose window at the square end, presses that come
    ///   down in coloured light on the beat, and car bodies of glass, half built, moving along the ridge overhead;
    /// - chimneys that throw beams of coloured light where the smoke should be;
    /// - every artist has a station: a tall lancet window standing behind their place.
    public partial class SoulKit
    {
        Material[] glass; Material leadM, beltDown, beltUp, ribM, leafShared, barkShared; SoulWorks works;
        // pale panes first (cream, gold, rose, pale blue), then the deep ones (amber, ruby, cobalt, teal, violet, indigo, emerald)
        static readonly (Color c, float s)[] Panes =
        {
            (new Color(1f, .93f, .74f), .56f), (new Color(1f, .8f, .4f), .62f), (new Color(1f, .62f, .6f), .58f), (new Color(.62f, .8f, 1f), .58f),
            (new Color(1f, .58f, .18f), .7f), (new Color(.8f, .12f, .18f), .6f), (new Color(.15f, .32f, 1f), .7f), (new Color(.1f, .62f, .7f), .6f),
            (new Color(.5f, .3f, .95f), .6f), (new Color(.06f, .08f, .3f), .35f), (new Color(.15f, .6f, .3f), .55f),
        };
        static readonly int[] PaleWarm = { 0, 1, 2, 0, 1 }, PaleCool = { 3, 3, 0 }, MidWarm = { 4, 1, 4, 5 }, MidCool = { 6, 7, 8, 6 }, RubySet = { 5, 5, 4 }, GreenSet = { 10, 7, 10 }, DarkSet = { 9, 9, 9, 6 }, FloorSet = { 9, 6, 9, 8, 7, 6, 3 };

        static Texture2D tileTex;
        static Texture2D TileTexture
        {
            get
            {
                if (tileTex != null) return tileTex;
                const int S = 64; tileTex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Glass tile", wrapMode = TextureWrapMode.Repeat };
                for (var y = 0; y < S; y++) for (var x = 0; x < S; x++) { var d = Mathf.Min(Mathf.Min(x, S - 1 - x), Mathf.Min(y, S - 1 - y)) / (float)S; var g = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.02f, .07f, d)) * (.75f + .25f * Mathf.Clamp01(d / .3f)); tileTex.SetPixel(x, y, new Color(g, g, g, 1f)); }
                tileTex.Apply(true); return tileTex;
            }
        }

        void MakeGlass()
        {
            glass = new Material[Panes.Length];
            for (var i = 0; i < glass.Length; i++)
            {
                glass[i] = new Material(WorldMaterials.Glow(Panes[i].c, Panes[i].s)) { name = "Soul glass " + i };
                glass[i].SetTexture("_BaseMap", MeshKit.LeadTexture); glass[i].SetTexture("_EmissionMap", MeshKit.LeadTexture);
            }
            leadM = new Material(WorldMaterials.Solid(new Color(.05f, .05f, .07f))) { name = "Soul lead" };
            ribM = WorldMaterials.Surface(new Color(.22f, .32f, .86f), World.Surface.Painted);
            Material Belt(string name)
            {
                var m = new Material(WorldMaterials.Glow(new Color(1f, .9f, .66f), .7f)) { name = name };
                m.SetTexture("_BaseMap", TileTexture); m.SetTexture("_EmissionMap", TileTexture); m.SetTextureScale("_BaseMap", new Vector2(MeshKit.GrainScale, MeshKit.GrainScale)); return m;
            }
            beltDown = Belt("Soul belt down"); beltUp = Belt("Soul belt up");
            WorldMaterials.KindOf(WorldMaterials.Surface(new Color(.3f, .5f, .3f), World.Surface.Foliage), out leafShared, out _);
            WorldMaterials.KindOf(WorldMaterials.Surface(new Color(.3f, .25f, .2f), World.Surface.Bark), out barkShared, out _);
        }

        bool Glassable(Material m) => m != leadM && m != barkShared && !m.IsKeywordEnabled("_EMISSION");   // (leaves too: the trees are glass, on trunks of ordinary wood)

        static float Hash01(Vector3 p) { var h = Mathf.Sin(p.x * 12.9898f + p.y * 78.233f + p.z * 37.719f) * 43758.5453f; return h - Mathf.Floor(h); }

        /// Which pane a face becomes: by what colour it was (so the pink shop is rose glass and the iron rail near-black).
        int PickPane(Vector3 mid, Vector3 n, Color lin)
        {
            var L = lin.r * .2126f + lin.g * .7152f + lin.b * .0722f; int[] set;
            if (n.y > .8f) set = FloorSet;                                               // what you walk on (and every roof) is deep glass, so the pale belts stand out and nothing glares up at you
            else if (L < .035f) set = DarkSet;
            else if (L > .38f) set = lin.r >= lin.b * 1.05f ? PaleWarm : PaleCool;
            else if (lin.r > lin.g * 2.6f) set = RubySet;
            else if (lin.g > lin.r * 1.08f && lin.g > lin.b * 1.08f) set = GreenSet;
            else set = lin.r >= lin.b * 1.05f ? MidWarm : MidCool;
            return set[Mathf.Min(set.Length - 1, (int)(Hash01(mid) * set.Length))];
        }

        /// Turns everything in the kit that isn't a leaf, a trunk, lead or a light into stained glass.
        int Glassify(MeshKit k) => k.StainedGlass(glass, 1.3f, Glassable, PickPane);

        /// One pane, seen from both sides.
        void Pane(MeshKit mk, int i, Vector3 a, Vector3 b, Vector3 c)
        {
            var n = Vector3.Cross(b - a, c - a).normalized; var mid = (a + b + c) / 3f; Vector2 u0 = new Vector2(0, 0), u1 = new Vector2(1, 0), u2 = new Vector2(0, 1);
            mk.Tri(glass[i], a, b, c, mid - n, u0, u1, u2); mk.Tri(glass[i], a, b, c, mid + n, u0, u1, u2);
        }
        void Sheet(MeshKit mk, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int[] set, int seed) { Pane(mk, set[Mathf.Abs(seed) % set.Length], a, b, c); Pane(mk, set[Mathf.Abs(seed * 7 + 3) % set.Length], a, c, d); }
        void Lead(MeshKit mk, Vector3 a, Vector3 b, float r = .05f) { var d = b - a; if (d.sqrMagnitude > 1e-5f) mk.Prism(leadM, a, d, r, d.magnitude, 4); }

        /// A lancet window standing on its own: the mark of an artist's station on the Line. 'g' on the ground, seen from both sides.
        void Lancet(MeshKit mk, Transform root, Vector3 g, Vector3 face, float w, float h, int seed)
        {
            var up = Vector3.up; face.y = 0; face.Normalize(); var side = Vector3.Cross(up, face); var y0 = h * .7f; const int rows = 5;
            Vector3 P(float x, float y) => g + side * x + up * (y + .3f);
            int[][] sets = { PaleWarm, MidCool, MidWarm, PaleCool, RubySet, MidCool };
            for (var r = 0; r < rows; r++) for (var c = 0; c < 2; c++)
            {
                float x0 = -w / 2f + c * w / 2f, x1 = x0 + w / 2f, ya = r * y0 / rows, yb = (r + 1) * y0 / rows;
                Sheet(mk, P(x0, ya), P(x1, ya), P(x1, yb), P(x0, yb), sets[(r + c + seed) % sets.Length], seed + r * 3 + c);
            }
            // the pointed head
            var sh = P(-w / 2f, y0 + (h - y0) * .55f) + side * (w * .16f); var sh2 = P(w / 2f, y0 + (h - y0) * .55f) - side * (w * .16f); var apex = P(0, h);
            Pane(mk, PaleWarm[seed % PaleWarm.Length], P(-w / 2f, y0), P(0, y0), sh); Pane(mk, MidCool[seed % MidCool.Length], P(0, y0), apex, sh); Pane(mk, MidCool[(seed + 1) % MidCool.Length], P(0, y0), sh2, apex); Pane(mk, PaleWarm[(seed + 2) % PaleWarm.Length], P(0, y0), P(w / 2f, y0), sh2);
            // the lead: frame, mullion, bars
            foreach (var e in new[] { -1f, 1f }) { mk.Box(leadM, P(e * w / 2f, y0 / 2f - .15f), new Vector3(.16f, y0 + .3f, .16f), Quaternion.LookRotation(face)); Lead(mk, P(e * w / 2f, y0), e < 0 ? sh : sh2, .08f); Lead(mk, e < 0 ? sh : sh2, apex, .08f); }
            Lead(mk, P(0, 0), apex, .04f); for (var r = 0; r <= rows; r++) Lead(mk, P(-w / 2f, r * y0 / rows), P(w / 2f, r * y0 / rows), .04f);
            mk.Box(leadM, g + up * .15f, new Vector3(w + .5f, .3f, .5f), Quaternion.LookRotation(face));
            BoxCol(root, g + up * (y0 / 2f + .3f), new Vector3(w + .2f, y0 + .6f, .3f), Quaternion.LookRotation(face));
            halos.Add(P(0, y0 * .6f), w * 1.3f, Gold, .22f);
        }

        // ---------------------------------------------------------------- the works
        void Works(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(center.x, 0, center.z);
            Vector3 On(float d, float lat) => ctx.OnGround(c.x + E.x * d + N.x * lat, c.z + E.z * d + N.z * lat);
            // how far the Line runs: to where River Road ends at the water
            var endD = StreetTo; while (endD < riverDist + 30f) { var q = c + E * (endD + 2f); if (!ctx.IsLand(q.x, q.z, 0f) || ctx.Ground(q.x, q.z) < waterY + 1.7f) break; endD += 2f; }
            float from = StreetFrom + 3f, to = endD - 5f; const float Lat = 1.4f, Half = 1.25f;
            works = new GameObject("Soul works").AddComponent<SoulWorks>(); works.transform.SetParent(life.transform, false);
            works.origin = c; works.E = E; works.N = N; works.from = from; works.to = to; works.lateral = Lat; works.half = Half; works.down = beltDown; works.up = beltUp; works.ground = (x, z) => ctx.Ground(x, z); works.glass = glass;

            // ---- the Line: the belt down to the landing on the south side of the street, the belt back on the north
            foreach (var e in new[] { -1f, 1f })
            {
                var pts = new List<Vector3>(); for (var d = from; d <= to + .01f; d += 2f) pts.Add(On(d, e * Lat));
                Strip(mk, pts, e < 0 ? beltDown : beltUp, Half * 2f, .26f, ctx);
                foreach (var d in new[] { from - .2f, to + .2f }) { var r = On(d, e * Lat) + up * .08f; mk.Prism(leadM, r - N * (Half + .15f), N, .32f, Half * 2f + .3f, 10); }
                // arrows of gold set beside it, so you can see which way it goes before you step on
                for (var d = from + 8f; d < to - 4f; d += 16f) { var a = On(d, e * (Lat + Half + .45f)) + up * .2f; var fw = E * -e; mk.Prism(lampM, a - fw * .5f, fw + N * e * .0f, .05f, 1f, 4); mk.Prism(lampM, a + fw * .5f, (-fw + N * .6f).normalized, .05f, .45f, 4); mk.Prism(lampM, a + fw * .5f, (-fw - N * .6f).normalized, .05f, .45f, 4); }
            }
            Debug.Log($"The Soul Lowlands: the Line runs {to - from:0} m each way");

            // ---- the nave: pointed arches over Main Street, a ridge along their tops
            const float SpanHalf = 4.8f, Spring = 4.6f; var apexH = Spring + SpanHalf * 2f * Mathf.Sin(60f * Mathf.Deg2Rad); var arches = new List<float>();
            for (var d = StreetFrom + 6.5f; d < StreetTo - 2f; d += 22f) arches.Add(d);
            foreach (var d in arches)
            {
                var p = On(d, 0);
                foreach (var e in new[] { -1f, 1f })
                {
                    var foot = On(d, e * SpanHalf); var top = new Vector3(foot.x, p.y + Spring, foot.z);
                    mk.Box(ribM, (foot - up * .3f + top) / 2f, new Vector3(.6f, top.y - foot.y + .3f, .6f), Quaternion.LookRotation(E)); mk.Box(leadM, top, new Vector3(.8f, .16f, .8f), Quaternion.LookRotation(E)); ArtistBits.PostCollider(ctx.Root, foot, Spring, .36f);
                    var o = new Vector3(p.x, p.y + Spring, p.z) - N * (e * SpanHalf); var prev = top;
                    for (var a = 10f; a <= 60.01f; a += 10f)
                    {
                        var pt = o + N * (e * SpanHalf * 2f * Mathf.Cos(a * Mathf.Deg2Rad)) + up * (SpanHalf * 2f * Mathf.Sin(a * Mathf.Deg2Rad)); var seg = pt - prev;
                        mk.Box(ribM, (prev + pt) / 2f, new Vector3(.42f, seg.magnitude + .06f, .42f), Quaternion.LookRotation(E, seg.normalized)); prev = pt;
                    }
                }
                var apex = p + up * apexH; mk.Blob(lampM, apex + up * .5f, new Vector3(.22f, .34f, .22f), prng, 0f); halos.Add(apex + up * .5f, 2.4f, Gold, .4f);
            }
            var ridgeA = On(arches[0], 0) + up * (apexH - .1f); var ridgeB = On(arches[arches.Count - 1], 0) + up * (apexH - .1f); ridgeB.y = ridgeA.y;
            mk.Box(leadM, (ridgeA + ridgeB) / 2f, new Vector3(.24f, .24f, Vector3.Distance(ridgeA, ridgeB) + 1f), Quaternion.LookRotation(E));
            works.ridgeA = ridgeA; works.ridgeLen = Vector3.Distance(ridgeA, ridgeB);

            // ---- the rose window at the square end
            Rose(ctx, mk, On(StreetFrom - 1.5f, 0));

            // ---- the presses, between the arches, coming down over one belt and then the other
            for (var k = 0; k + 1 < arches.Count; k++)
            {
                var e = k % 2 == 0 ? -1f : 1f; var at = On((arches[k] + arches[k + 1]) / 2f, e * Lat); var pm = new MeshKit();
                pm.Box(ribM, new Vector3(0, .8f, 0), new Vector3(2.5f, 1.6f, 2.5f), Quaternion.LookRotation(E)); pm.Box(leadM, new Vector3(0, 1.7f, 0), new Vector3(2.8f, .2f, 2.8f), Quaternion.LookRotation(E)); pm.Box(leadM, new Vector3(0, 5.6f, 0), new Vector3(.4f, 7.6f, .4f), Quaternion.LookRotation(E));
                Glassify(pm); var go = pm.BuildSingle("Press " + k, works.transform, false); go.isStatic = false; var home = new Vector3(at.x, at.y + 8.2f, at.z); go.transform.position = home;
                var col = k % 2 == 0 ? Gold : new Color(.62f, .8f, 1f); var light = ArtistBits.PointLight(works.transform, at + up * 2.6f, col, 14f, 0f);
                works.presses.Add((go.transform, home, k, light));
            }

            // ---- car bodies of glass, half built, going along the ridge
            var shellM = WorldMaterials.Surface(new Color(.62f, .78f, .96f), World.Surface.Painted); var shellWarm = WorldMaterials.Surface(new Color(.98f, .86f, .6f), World.Surface.Painted);
            for (var k = 0; k < 5; k++)
            {
                var sm = new MeshKit(); var body = k % 2 == 0 ? shellM : shellWarm;
                sm.Box(body, new Vector3(0, .12f, 0), new Vector3(1.8f, .14f, 4.2f), Quaternion.identity);                                                        // the floor pan
                if (k >= 1) foreach (var e in new[] { -1f, 1f }) sm.Box(body, new Vector3(e * .86f, .55f, 0), new Vector3(.1f, .8f, 4.2f), Quaternion.identity);   // the sides
                if (k >= 2) { sm.Box(body, new Vector3(0, .55f, 2.05f), new Vector3(1.8f, .8f, .1f), Quaternion.identity); sm.Box(body, new Vector3(0, .55f, -2.05f), new Vector3(1.8f, .8f, .1f), Quaternion.identity); sm.Box(body, new Vector3(0, .98f, 1.4f), new Vector3(1.8f, .08f, 1.4f), Quaternion.identity); }
                if (k >= 3) { sm.Box(body, new Vector3(0, 1.75f, -.3f), new Vector3(1.6f, .08f, 2f), Quaternion.identity); foreach (var e in new[] { -1f, 1f }) foreach (var zz in new[] { .7f, -1.3f }) sm.Box(leadM, new Vector3(e * .78f, 1.36f, zz), new Vector3(.07f, .76f, .07f), Quaternion.identity); }
                if (k >= 4) foreach (var e in new[] { -1f, 1f }) foreach (var zz in new[] { 1.4f, -1.4f }) sm.Prism(leadM, new Vector3(e * .8f, .05f, zz), Vector3.right * e, .38f, .2f, 10);
                foreach (var zz in new[] { 1.5f, -1.5f }) sm.Box(leadM, new Vector3(0, 2.3f, zz), new Vector3(.06f, k >= 3 ? 1.1f : 4.4f, .06f), Quaternion.identity);
                if (k < 3) foreach (var zz in new[] { 1.5f, -1.5f }) sm.Box(leadM, new Vector3(0, 1.2f, zz), new Vector3(.06f, 2.2f, .06f), Quaternion.identity);
                Glassify(sm); var go = sm.BuildSingle("Glass car body " + k, works.transform, false); go.isStatic = false; foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.transform.position = ridgeA; works.shells.Add(go.transform);
            }

            // ---- the chimneys: tall stacks of banded glass, a beam of light out of each where the smoke should be
            Color[] beamCols = { Gold, new Color(.62f, .8f, 1f), new Color(1f, .62f, .6f), new Color(1f, .93f, .74f) }; var made = 0;
            foreach (var (d, lat) in new[] { (58f, 40f), (58f, -40f), (118f, 38f), (118f, -38f), (-64f, 30f), (-64f, -30f) })
            {
                if (made >= 4) break; var g = On(d, lat);
                if (!ctx.IsLand(g.x, g.z, 10f) || InFootprint(ctx, g.x, g.z, 7f) || NearPath(g.x, g.z, 5f) || InNoGrass(g.x, g.z, 3f)) continue;
                Material[] bands = { ribM, WorldMaterials.Surface(new Color(.95f, .8f, .5f), World.Surface.Painted), WorldMaterials.Surface(new Color(.4f, .25f, .7f), World.Surface.Painted) };
                for (var b = 0; b < 7; b++) mk.Prism(bands[b % 3], g + up * (b * 4.2f - .3f), up, 2.3f - b * .13f, 4.25f, 8, (2.3f - (b + 1) * .13f) / (2.3f - b * .13f));
                mk.Prism(leadM, g + up * 29.1f, up, 1.6f, .3f, 8); ArtistBits.PostCollider(ctx.Root, g, 10f, 2.3f); Mark(g, 4f);
                works.AddBeam(g + up * 29.3f, beamCols[made % beamCols.Length], made); halos.Add(g + up * 30f, 6f, beamCols[made % beamCols.Length], .4f); made++;
            }
            Shot(ctx, "the line and the nave", On(StreetFrom + 2f, 0) + up * 2.2f, On(StreetFrom + 70f, 0) + up * 8f);
            Shot(ctx, "the rose window", On(StreetFrom + 26f, -1f) + up * 2f, On(StreetFrom - 1.5f, 0) + up * 13f);
            Shot(ctx, "a press over the belt", On(arches[0] + 4f, 3.6f) + up * 2f, On((arches[0] + arches[1]) / 2f, -Lat) + up * 5f);
        }

        /// The rose window: a wheel of glass fifteen metres across, standing on two piers over the head of the Line.
        void Rose(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; const float R = 7.4f; var C = g + up * (6f + R + .2f);
            Vector3 P(float r, float deg) => C + (N * Mathf.Cos(deg * Mathf.Deg2Rad) + up * Mathf.Sin(deg * Mathf.Deg2Rad)) * r;
            float[] ring = { 0f, 1.5f, 3.7f, 5.7f, R }; int[] sectors = { 8, 8, 16, 16 }; int[][][] sets = { new[] { PaleWarm, PaleWarm }, new[] { MidCool, RubySet }, new[] { PaleWarm, MidWarm }, new[] { MidCool, DarkSet } };
            for (var k = 0; k < 4; k++)
            {
                var n = sectors[k]; var step = 360f / n;
                for (var s = 0; s < n; s++)
                {
                    float a0 = s * step, a1 = a0 + step; var set = sets[k][s % 2];
                    if (k == 0) Pane(mk, set[s % set.Length], C, P(ring[1], a0), P(ring[1], a1));
                    else if (k == 2) { var m = P(ring[k + 1] + .0f, a0 + step / 2f); Pane(mk, set[s % set.Length], P(ring[k], a0), P(ring[k], a1), m); Pane(mk, MidCool[s % MidCool.Length], P(ring[k], a0), m, P(ring[k + 1], a0)); Pane(mk, MidCool[(s + 1) % MidCool.Length], P(ring[k], a1), P(ring[k + 1], a1), m); }   // petals
                    else Sheet(mk, P(ring[k], a0), P(ring[k], a1), P(ring[k + 1], a1), P(ring[k + 1], a0), set, s + k * 5);
                    Lead(mk, P(ring[k], a0), P(ring[k + 1], a0), k == 3 ? .07f : .05f); Lead(mk, P(ring[k + 1], a0), P(ring[k + 1], a1), k == 3 ? .16f : .06f);
                }
            }
            // two piers and a beam under it; you walk beneath
            foreach (var e in new[] { -1f, 1f })
            {
                var foot = g + N * (e * 6.2f); foot.y = ctx.Ground(foot.x, foot.z); var top = new Vector3(foot.x, C.y - 1.5f, foot.z);
                mk.Box(ribM, (foot - up * .3f + top) / 2f, new Vector3(.9f, top.y - foot.y + .3f, .9f), Quaternion.LookRotation(E)); ArtistBits.PostCollider(ctx.Root, foot, 8f, .55f);
            }
            mk.Box(ribM, C - up * (R + .35f), new Vector3(.7f, .5f, 13.2f), Quaternion.LookRotation(N));
            halos.Add(C, 11f, Gold, .22f); ArtistBits.PointLight(ctx.Root, C + E * 6f - up * 3f, Gold, 30f, 2.2f); ArtistBits.PointLight(ctx.Root, C - E * 6f - up * 3f, Gold, 30f, 2.2f);
        }
    }

    /// The Soul Lowlands' works running: the belts (and you on them), the presses on the beat, the glass car bodies
    /// going along the ridge, the beams over the chimneys, the glass breathing with the music.
    public class SoulWorks : MonoBehaviour
    {
        const string Land = "The Soul Lowlands";
        public Vector3 origin, E, N, ridgeA; public float from, to, lateral = 1.4f, half = 1.25f, speed = 5f, ridgeLen;
        public Material down, up; public Material[] glass; public Func<float, float, float> ground;
        public readonly List<(Transform t, Vector3 home, int k, Light light)> presses = new List<(Transform, Vector3, int, Light)>();
        public readonly List<Transform> shells = new List<Transform>();
        readonly List<(Transform t, Vector3 foot, int k)> beams = new List<(Transform, Vector3, int)>();
        Color[] glassBase; SimplePlayerController player; CharacterController body;

        public void AddBeam(Vector3 foot, Color c, int k)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Chimney beam" };
            m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, .11f)); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = "Chimney beam"; Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            beams.Add((g.transform, foot, k));
        }

        void Update()
        {
            var t = Time.time; var now = SharedChance.Now; var energy = SurpriseDirector.Energy;
            // the belts' tiles run the way the belts go
            var run = now * speed; float ox = (float)((E.x * run) % 1.0), oz = (float)((E.z * run) % 1.0);
            if (down != null) down.SetTextureOffset("_BaseMap", new Vector2(-ox, -oz)); if (up != null) up.SetTextureOffset("_BaseMap", new Vector2(ox, oz));
            // the presses: one bar of four beats each, taking turns; down hard on the one, held, drawn back up slowly
            var bars = now * 1.75 / 4.0;
            foreach (var (tr, home, k, light) in presses)
            {
                var ph = (float)((bars + k * .25) % 1.0); var d = ph < .1f ? ph / .1f : ph < .28f ? 1f : ph < .62f ? 1f - (ph - .28f) / .34f : 0f; d = Mathf.SmoothStep(0f, 1f, d);
                if (tr != null) tr.position = home - Vector3.up * (d * 4.7f); if (light != null) light.intensity = d * d * 3.2f;
            }
            // the car bodies, one behind another along the ridge
            for (var i = 0; i < shells.Count; i++)
            {
                if (shells[i] == null || ridgeLen < 1f) continue; var s = (float)((now * 1.8 + i * ridgeLen / shells.Count) % ridgeLen);
                var edge = Mathf.Clamp01(Mathf.Min(s, ridgeLen - s) / 4f);   // they rise out of nothing at one end and sink into it at the other
                shells[i].SetPositionAndRotation(ridgeA + E * s - Vector3.up * (2.9f + (1f - edge) * 0f), Quaternion.LookRotation(E)); shells[i].localScale = Vector3.one * Mathf.Lerp(.01f, 1f, edge);
            }
            // the chimneys' beams lean slowly about
            foreach (var (tr, foot, k) in beams)
            {
                if (tr == null) continue; const float len = 150f;
                var dir = (Vector3.up + new Vector3(Mathf.Sin(t * .11f + k * 1.7f), 0, Mathf.Cos(t * .09f + k * 2.3f)) * .22f).normalized;
                tr.position = foot + dir * len / 2f; tr.rotation = Quaternion.FromToRotation(Vector3.up, dir); var w = 2.6f + .5f * Mathf.Sin(t * .7f + k) + energy * .8f; tr.localScale = new Vector3(w, len / 2f, w);
            }
            // the glass breathes a little with the music
            if (glass != null)
            {
                if (glassBase == null) { glassBase = new Color[glass.Length]; for (var i = 0; i < glass.Length; i++) glassBase[i] = glass[i].GetColor("_EmissionColor"); }
                for (var i = 0; i < glass.Length; i++) glass[i].SetColor("_EmissionColor", glassBase[i] * (.9f + energy * .3f + .06f * Mathf.Sin(t * .8f + i)));
            }
            Carry();
        }

        /// Stand on a belt and it takes you along. You can walk with it, against it, or off it.
        void Carry()
        {
            if (player == null) { player = FindAnyObjectByType<SimplePlayerController>(); if (player != null) body = player.GetComponent<CharacterController>(); }
            if (player == null || body == null || !body.enabled || player.Controlled || player.Seated || player.Flying) return;
            var p = player.transform.position; var rel = p - origin; var along = Vector3.Dot(rel, E); if (along < from || along > to) return;
            var feet = p.y - body.height / 2f; if (ground == null || Mathf.Abs(feet - ground(p.x, p.z)) > .75f) return;
            var lat = Vector3.Dot(rel, N); var dir = Mathf.Abs(lat + lateral) < half ? 1f : Mathf.Abs(lat - lateral) < half ? -1f : 0f; if (dir == 0f) return;
            body.Move(E * (dir * speed * Time.deltaTime) + Vector3.down * .02f);
        }
    }
}
