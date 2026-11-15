using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Lana Del Rey's quarter on the Heartbreak Boardwalk (part of HeartKit). Her music is beautiful, very sad and
    /// dark, so inside a ring of black cypresses it is always night: the sky goes deep blue, a big pale moon hangs
    /// low, fireflies and stars come out. Her bungalow is in the middle (HeartArtists.cs); pale stone walks lead out
    /// from it to five places, one for each of the five songs that matter most:
    /// - "Brooklyn Baby": the Stoop. A slice of a city street: a brownstone with its windows lit, a fire escape,
    ///   string lights, a stoop to sit on and a record player turning on the top step.
    /// - "Cinnamon Girl": the Candle Room. A roofless room with tall empty windows, full of candles, warm amber in
    ///   the blue night, an armchair in the middle and a chandelier hanging from nothing.
    /// - "Mariners Apartment Complex": the Lighthouse. A white lighthouse at the back of the garden, its beam turning
    ///   slowly over everything; a ship's bell to ring, an anchor, benches at its foot.
    /// - "Happiness is a butterfly": the Butterfly Garden. A ring of pale night flowers, a swing under a flowering
    ///   bar, and big glowing butterflies circling overhead.
    /// - "Say Yes To Heaven": the White Stair. Floating white steps rising to a little platform in the sky, with an
    ///   arch of white flowers, a bench and a column of soft light going up.
    /// When one of the five plays (her station covers the whole quarter), its place answers: the lighthouse finds
    /// you and holds its beam on you, the candles flare, the butterflies rise and widen, the string lights brighten
    /// and the record glows, the stair lights step by step up into a bright column.
    public partial class HeartKit
    {
        const float PlaceR = 42f;

        void Quarter(Plot z, string cg, string ma, string hb, string br, string sy)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.c; var fr = z.front; var sd = z.side; var rng = new System.Random(2012);
            var night = new GameObject("Lana's night").AddComponent<LanaNight>(); night.transform.SetParent(z.root, false);
            night.center = c; night.radius = QuarterR; night.heart = life; night.marinersSong = ma; night.cinnamonSong = cg; night.butterflySong = hb; night.brooklynSong = br; night.heavenSong = sy;
            var stone = WorldMaterials.Glow(new Color(.8f, .8f, .84f), .24f); var cyp = WorldMaterials.Glow(new Color(.07f, .13f, .13f), .06f);
            var flowerW = new Material(GlowM(new Color(.84f, .9f, 1f), 1.1f)) { name = "Lana moonflowers" }; var flowerP = WorldMaterials.Glow(new Color(.9f, .74f, .84f), .5f);
            var warm = new Color(1f, .74f, .42f);
            night.flowers = flowerW;
            Vector3 Dir(float deg) { var a = deg * Mathf.Deg2Rad; return (fr * Mathf.Cos(a) + sd * Mathf.Sin(a)).normalized; }
            Vector3 Ring(float deg, float r) { var p = c + Dir(deg) * r; return ctx.OnGround(p.x, p.z); }
            PlaceFrame Place(float deg) => new PlaceFrame(Ring(deg, PlaceR), -Dir(deg));
            var clear = new List<Vector3>();   // x, radius, z: grass and benches keep off
            void Clear(Vector3 p, float r) => clear.Add(new Vector3(p.x, r, p.z));
            bool Clean(float x, float zz, float pad) { foreach (var q in clear) { float dx = x - q.x, dz = zz - q.z, r = q.y + pad; if (dx * dx + dz * dz < r * r) return false; } return true; }

            // ------------------------------------------------ the gate, where the walk from the Promenade comes in
            var gp = Ring(0, QuarterR); var gb = 99f;
            foreach (var (p, _) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (Mathf.Abs(d - QuarterR) < gb) { gb = Mathf.Abs(d - QuarterR); gp = p; } }
            gp = ctx.OnGround(gp.x, gp.z); var gd = FaceTo(c, gp); var gs = Vector3.Cross(up, gd); var gr = Quaternion.LookRotation(gd);
            foreach (var e in new[] { -1f, 1f })
            {
                var pp = gp + gs * e * 3.9f; z.mk.Box(white, pp + up * 2.1f, new Vector3(1f, 4.6f, 1f), gr); z.solid.Box(white, pp + up * 2.1f, new Vector3(1f, 4.6f, 1f), gr);
                z.mk.Box(white, pp + up * 4.5f, new Vector3(1.3f, .25f, 1.3f), gr); z.mk.Blob(lampM, pp + up * 4.9f, Vector3.one * .3f, rng, 0f); halos.Add(pp + up * 4.9f, 2f, warm, .5f);
            }
            for (var k = 0; k < 18; k++)
            {
                float a0 = Mathf.PI * k / 18f, a1 = Mathf.PI * (k + 1) / 18f; var p0 = gp + up * 4.4f + gs * Mathf.Cos(a0) * 3.9f + up * Mathf.Sin(a0) * 2.2f; var p1 = gp + up * 4.4f + gs * Mathf.Cos(a1) * 3.9f + up * Mathf.Sin(a1) * 2.2f;
                z.mk.Box(iron, (p0 + p1) / 2f, new Vector3(.08f, .08f, Vector3.Distance(p0, p1) + .04f), Quaternion.LookRotation(p1 - p0)); if (k % 2 == 1) z.mk.Blob(lampM, p1 - up * .14f, Vector3.one * .09f, rng, 0f);
            }
            Shot(ctx, "lana's gate", gp + gd * 9f + up * 2.2f, gp - gd * 20f + up * 3f);

            // ------------------------------------------------ the cypress ring, black against the night sky
            var nCyp = Mathf.RoundToInt(2f * Mathf.PI * QuarterR / 4.4f); var cyps = 0;
            for (var k = 0; k < nCyp; k++)
            {
                var g = Ring(k * 360f / nCyp + (float)rng.NextDouble() * 1.5f, QuarterR + ((float)rng.NextDouble() - .5f) * 1.6f);
                if ((g - gp).magnitude < 8f || !ctx.IsLand(g.x, g.z, 1f) || NearPath(g.x, g.z, 3.5f)) continue;
                var s = .85f + (float)rng.NextDouble() * .5f; var lo = Lowest(g, .4f);
                z.mk.Prism(trunkM, new Vector3(g.x, lo - .2f, g.z), up, .22f, g.y - lo + .9f, 6);
                z.mk.Prism(cyp, new Vector3(g.x, lo - .1f, g.z), up, 1.05f * s, g.y - lo + .7f + 1.6f * s, 7, .95f); z.mk.Prism(cyp, g + up * (.6f + 1.6f * s), up, 1f * s, 5.6f * s, 7, .06f);
                ArtistBits.PostCollider(z.root, g, 3f, .9f * s); cyps++;
            }

            // ------------------------------------------------ the walks: a pale ring round the bungalow, a spoke to each place
            var ringPts = new List<Vector3>(); for (var k = 0; k <= 60; k++) ringPts.Add(Ring(k * 6f, 19f));
            Strip(z.mk, ringPts, stone, 2.4f, .16f, ctx); foreach (var p in ringPts) Remember(p, 0);
            var spokes = 0;
            void Spoke(float deg, Vector3 to)
            {
                var a = Ring(deg, 19f + 1f); var pts = new List<Vector3>(); var len = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(to.x, 0, to.z));
                for (var d = 0f; ; d += 1.5f) { var dd = Mathf.Min(d, len); var q = Vector3.Lerp(a, to, dd / len); pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= len) break; }
                if (pts.Count < 2) return;
                Strip(z.mk, pts, stone, 2.2f, .145f + (spokes % 3) * .006f, ctx); foreach (var p in pts) Remember(p, 0);
                var side = Vector3.Cross(up, FaceTo(a, to));
                for (var i = 5; i < pts.Count - 3; i += 7) { var e = (i / 7) % 2 == 0 ? 1f : -1f; GlobeLamp(ctx, z.mk, ctx.OnGround(pts[i].x + side.x * e * 1.9f, pts[i].z + side.z * e * 1.9f), i % 14 == 5); Clear(pts[i] + side * e * 1.9f, .8f); }
                spokes++;
            }

            // ------------------------------------------------ "Brooklyn Baby": the Stoop
            {
                var f = Place(60f); var brick = Paint(new Color(.44f, .26f, .22f)); var trim = Paint(new Color(.86f, .82f, .76f)); const float W = 8f, H = 9.2f, Dp = 6f;
                z.mk.Box(Paint(new Color(.42f, .42f, .46f)), f.P(0, .03f, 2.3f), new Vector3(12f, .12f, 4.6f), f.r);
                z.mk.Box(brick, f.P(0, H / 2f - .3f, -Dp / 2f), new Vector3(W, H + .6f, Dp), f.r); z.solid.Box(brick, f.P(0, H / 2f - .3f, -Dp / 2f), new Vector3(W, H + .6f, Dp), f.r);
                z.mk.Box(Paint(new Color(.28f, .2f, .2f)), f.P(0, H + .15f, -Dp / 2f + .05f), new Vector3(W + .5f, .5f, Dp + .5f), f.r);
                var lit = new[] { true, false, true, true, true, false, false, true, true };
                for (var row = 0; row < 3; row++) for (var col = 0; col < 3; col++)
                {
                    if (row == 0 && col == 1) continue;
                    var wp = f.P(-2.6f + col * 2.6f, 2.7f + row * 2.65f, .03f); var on = lit[row * 3 + col];
                    z.mk.Box(on ? windowM : dark, wp, new Vector3(1.1f, 1.5f, .06f), f.r); z.mk.Box(trim, wp + up * .86f, new Vector3(1.4f, .16f, .14f), f.r); z.mk.Box(trim, wp - up * .8f + f.Fwd * .06f, new Vector3(1.3f, .08f, .2f), f.r);
                    if (on) halos.Add(wp + f.Fwd * .6f, 1.2f, warm, .3f);
                }
                z.mk.Box(woodM, f.P(0, 2.75f, .03f), new Vector3(1.3f, 2.3f, .06f), f.r); z.mk.Box(windowM, f.P(0, 4.15f, .03f), new Vector3(1.3f, .36f, .06f), f.r); z.mk.Box(trim, f.P(0, 4.45f, .08f), new Vector3(1.8f, .18f, .2f), f.r);
                // the stoop: a landing and four steps down to the pavement, iron rails both sides
                z.mk.Box(trim, f.P(0, .8f, .7f), new Vector3(2.8f, 1.6f, 1.4f), f.r); z.solid.Box(trim, f.P(0, .8f, .7f), new Vector3(2.8f, 1.6f, 1.4f), f.r);
                for (var i = 0; i < 4; i++) { var h = 1.6f - (i + 1) * .32f; z.mk.Box(trim, f.P(0, h / 2f - .15f, 1.6f + i * .4f), new Vector3(2.8f, h + .3f, .4f), f.r); z.solid.Box(trim, f.P(0, h / 2f, 1.6f + i * .4f), new Vector3(2.8f, h, .4f), f.r); }
                foreach (var e in new[] { -1f, 1f })
                {
                    Vector3 r0 = f.P(e * 1.45f, 2.5f, .2f), r1 = f.P(e * 1.45f, 2.5f, 1.4f), r2 = f.P(e * 1.45f, 1.2f, 3.1f);
                    Bar(z.mk, iron, r0, r1, .05f); Bar(z.mk, iron, r1, r2, .05f); foreach (var q in new[] { r1, r2 }) Bar(z.mk, iron, q, new Vector3(q.x, f.o.y, q.z), .04f);
                    z.solid.Box(iron, (r1 + r2) / 2f - up * .5f, new Vector3(.1f, 1f, Vector3.Distance(r1, r2)), Quaternion.LookRotation(r2 - r1));
                }
                foreach (var e in new[] { -.6f, .6f }) Interact.Seat(tlife.transform, f.P(e, .64f, 2.35f), f.Fwd, "Sit on the stoop").seatHeight = .42f;
                // the record player on the top step, turning
                z.mk.Box(woodM, f.P(1f, 1.68f, .9f), new Vector3(.5f, .16f, .42f), f.r);
                var rp = new GameObject("Brooklyn record").transform; rp.SetParent(night.transform, false); rp.position = f.P(1f, 1.77f, .9f);
                var rk = new MeshKit(); rk.Prism(dark, Vector3.zero, up, .19f, .015f, 16); rk.Prism(SongGlow(br, warm, 1.4f), up * .001f, up, .06f, .016f, 10); rk.Box(lampM, new Vector3(.12f, .017f, 0), new Vector3(.05f, .004f, .03f));
                rk.BuildSingle("Record", rp, false); night.record = rp;
                // the fire escape in front of the right-hand windows
                foreach (var y in new[] { 4.5f, 7.15f })
                {
                    z.mk.Box(iron, f.P(2.6f, y, .55f), new Vector3(2.2f, .06f, 1.05f), f.r); z.mk.Box(iron, f.P(2.6f, y + 1f, 1.05f), new Vector3(2.2f, .04f, .04f), f.r);
                    foreach (var e in new[] { -1.08f, 1.08f }) { z.mk.Box(iron, f.P(2.6f + e, y + .5f, 1.05f), new Vector3(.04f, 1f, .04f), f.r); z.mk.Box(iron, f.P(2.6f + e, y + 1f, .55f), new Vector3(.04f, .04f, 1.05f), f.r); }
                }
                foreach (var e in new[] { 3.3f, 3.6f }) z.mk.Box(iron, f.P(e, 4.25f, .9f), new Vector3(.04f, 3.3f, .04f), f.r * Quaternion.Euler(0, 0, 0)); for (var k = 0; k < 9; k++) z.mk.Box(iron, f.P(3.45f, 2.9f + k * .32f, .9f), new Vector3(.3f, .03f, .03f), f.r);
                // string lights from the roof out to two poles
                var sl = new Material(GlowM(new Color(1f, .82f, .5f), 1.8f)) { name = "Lana string lights" }; night.stringLights = sl;
                foreach (var e in new[] { -1f, 1f })
                {
                    var pole = f.P(e * 5.6f, 0, 4.4f); z.mk.Prism(iron, pole - up * .2f, up, .07f, 4.6f, 6); ArtistBits.PostCollider(z.root, pole, 4.4f, .1f);
                    Vector3 a0 = f.P(e * 3.6f, H - .1f, .15f), a1 = pole + up * 4.3f;
                    for (var k = 0; k <= 12; k++) { var t = k / 12f; var q = Vector3.Lerp(a0, a1, t) - up * Mathf.Sin(t * Mathf.PI) * .9f; z.mk.Blob(sl, q, Vector3.one * .085f, rng, 0f); }
                }
                for (var k = 0; k <= 16; k++) { var t = k / 16f; z.mk.Blob(sl, f.P(-3.6f + t * 7.2f, H - .1f - Mathf.Sin(t * Mathf.PI) * .5f, .2f), Vector3.one * .085f, rng, 0f); }
                GlobeLamp(ctx, z.mk, f.P(-2.7f, 0, 3.5f), true);
                ArtistBits.PointLight(z.root, f.P(0, 3.5f, 4.5f), new Color(1f, .8f, .55f), 16f, 2.2f);
                Folk(f.P(-3.3f, 0, 4.2f), f.Fwd, Resident.Pose.Lean, new[] { "Somebody upstairs plays the same record every night. I've never asked them to stop.", "It's a city street in a garden. Don't think about it too hard.", "Sit on the stoop. That's what it's for." });
                Clear(f.P(0, 0, -1.5f), 8.5f); Clear(f.P(0, 0, 3f), 6f);
                Spoke(60f, f.P(0, 0, 4.4f));
                Shot(ctx, "the stoop", f.P(4f, 2f, 13f), f.P(0, 3.5f, -1f));
            }

            // ------------------------------------------------ "Cinnamon Girl": the Candle Room
            {
                var f = Place(130f); var plaster = Paint(new Color(.84f, .78f, .72f)); const float H = 4.2f, T = .4f, Hw = 4.5f, Rd = 9.2f;
                var candle = new Material(GlowM(new Color(1f, .62f, .26f), 2.2f)) { name = "Lana candles" }; night.candles = candle; var wax = Paint(new Color(.96f, .9f, .78f));
                HoleWall(z.mk, z.solid, plaster, f, new Vector3(-Hw, 0, -T / 2f), new Vector3(Hw, 0, -T / 2f), H, T, new[] { (0f, 1.8f, 0f, 2.8f) });
                HoleWall(z.mk, z.solid, plaster, f, new Vector3(-Hw, 0, -Rd + T / 2f), new Vector3(Hw, 0, -Rd + T / 2f), H, T, new[] { (0f, 1.6f, 1.2f, 3.4f) });
                foreach (var e in new[] { -1f, 1f }) HoleWall(z.mk, z.solid, plaster, f, new Vector3(e * (Hw - T / 2f), 0, -T), new Vector3(e * (Hw - T / 2f), 0, -Rd + T), H, T, new[] { (-2.2f, 1.1f, 1.1f, 3.3f), (2.2f, 1.1f, 1.1f, 3.3f) });
                var mid = f.P(0, 0, -Rd / 2f); z.mk.Box(boardsDark, mid + up * .05f, new Vector3(2f * Hw - 2f * T, .1f, Rd - 2f * T), f.r);
                // the armchair, deep red velvet, facing the door
                var vel = Paint(new Color(.5f, .1f, .16f)); var ch = f.P(0, 0, -5.4f);
                z.mk.Box(vel, ch + up * .38f, new Vector3(1f, .46f, .9f), f.r); z.mk.Box(vel, ch + up * .85f - f.Fwd * .42f, new Vector3(1.1f, 1.2f, .26f), f.r); foreach (var e in new[] { -1f, 1f }) z.mk.Box(vel, ch + up * .48f + f.Right * e * .58f, new Vector3(.24f, .7f, .9f), f.r);
                BoxCol(z.root, ch + up * .5f, new Vector3(1.4f, 1f, 1f), f.r); Interact.Seat(tlife.transform, ch + up * .62f + f.Fwd * .05f, f.Fwd, "Sit among the candles");
                // the candles: along the walls, in a ring round the chair (open toward the door), on the sills, on the chandelier
                void Candle(Vector3 g, float h, float r) { z.mk.Prism(wax, g, up, r, h, 6); z.mk.Blob(candle, g + up * (h + .05f), new Vector3(.025f, .055f, .025f), rng, 0f); }
                var n = 0;
                for (var x = -Hw + T + .3f; x < Hw - T - .2f; x += .32f) for (var row = 0; row < 2; row++)
                {
                    float jx = ((float)rng.NextDouble() - .5f) * .15f, jz = .3f + row * .35f + (float)rng.NextDouble() * .15f;
                    Candle(f.P(x + jx, .1f, -Rd + T + jz), .12f + (float)rng.NextDouble() * .3f, .04f + (float)rng.NextDouble() * .03f); n++;
                    if (Mathf.Abs(x) > 1.3f) { Candle(f.P(x + jx, .1f, -T - jz), .1f + (float)rng.NextDouble() * .26f, .04f + (float)rng.NextDouble() * .03f); n++; }
                }
                foreach (var e in new[] { -1f, 1f }) for (var zz = -T - .4f; zz > -Rd + T + .3f; zz -= .34f) for (var row = 0; row < 2; row++) { Candle(f.P(e * (Hw - T - .3f - row * .35f - (float)rng.NextDouble() * .12f), .1f, zz + ((float)rng.NextDouble() - .5f) * .15f), .12f + (float)rng.NextDouble() * .34f, .04f + (float)rng.NextDouble() * .03f); n++; }
                for (var k = 0; k < 26; k++) { var a = k * Mathf.PI * 2f / 26f; if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 32f) continue; var rr = 1.6f + (k % 2) * .4f; Candle(ch + up * .1f + f.Right * Mathf.Sin(a) * rr + f.Fwd * Mathf.Cos(a) * rr, .15f + (float)rng.NextDouble() * .4f, .05f); n++; }
                foreach (var e in new[] { -1f, 1f }) foreach (var w in new[] { -2.2f, 2.2f }) for (var k = 0; k < 4; k++) { Candle(f.P(e * (Hw - T / 2f), 1.1f, -Rd / 2f + w - .4f + k * .26f), .1f + k % 2 * .08f, .035f); n++; }
                var chand = f.P(0, 3.5f, -Rd / 2f); z.mk.Prism(iron, chand + up * .02f, up, .95f, .05f, 14); z.mk.Prism(iron, chand, up, .06f, .9f, 5);
                for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; Candle(chand + up * .07f + f.Right * Mathf.Cos(a) * .9f + f.Fwd * Mathf.Sin(a) * .9f, .14f, .035f); n++; }
                // cinnamon sticks tied in bundles on the sills
                foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) z.mk.Prism(Paint(new Color(.5f, .26f, .14f)), f.P(e * (Hw - T / 2f) + ((k % 3) - 1) * .03f, 1.12f, -Rd / 2f + .6f + k * .03f), f.Right, .02f, .3f, 5);
                halos.Add(f.P(0, 1.2f, -Rd / 2f), 4.6f, new Color(1f, .6f, .3f), .32f); halos.Add(chand, 1.8f, new Color(1f, .7f, .4f), .5f); halos.Add(f.P(0, 1.4f, .8f), 2.2f, new Color(1f, .6f, .3f), .3f);
                ArtistBits.PointLight(z.root, f.P(0, 2.4f, -Rd / 2f), new Color(1f, .6f, .28f), 16f, 3f);
                Clear(mid, 7.5f);
                Spoke(130f, f.P(0, 0, .5f));
                Shot(ctx, "the candle room", f.P(2f, 2f, 8f), f.P(0, 1f, -5f));
                Debug.Log($"Lana's candle room: {n} candles");
            }

            // ------------------------------------------------ "Mariners Apartment Complex": the Lighthouse
            {
                var f = Place(180f); var T = f.P(0, 0, -4.2f); const float R0 = 2.8f, R1 = 2f, Ht = 17f; var navy = Paint(new Color(.2f, .26f, .46f));
                z.mk.Prism(white, T - up * .3f, up, R0, Ht + .3f, 14, R1 / R0); z.solid.Prism(white, T - up * .3f, up, R0, Ht + .3f, 14, R1 / R0);
                float Rad(float y) => Mathf.Lerp(R0, R1, (y + .3f) / (Ht + .3f));
                foreach (var y in new[] { 5.2f, 10.8f }) z.mk.Prism(navy, T + up * y, up, Rad(y) + .04f, 1.3f, 14, (Rad(y + 1.3f) + .04f) / (Rad(y) + .04f));
                z.mk.Prism(iron, T + up * Ht, up, 3.1f, .25f, 14);
                for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; var q = T + up * (Ht + .25f) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.95f; z.mk.Prism(iron, q, up, .03f, .9f, 4); }
                z.mk.Prism(iron, T + up * (Ht + 1.1f), up, 3f, .06f, 16);
                var glass = WorldMaterials.Glow(new Color(1f, .92f, .7f), .9f); z.mk.Prism(glass, T + up * (Ht + .25f), up, 1.35f, 2.2f, 10); z.mk.Cone(navy, T + up * (Ht + 2.45f), 1.7f, 1.5f, 10);
                var lamp = new Material(GlowM(new Color(1f, .9f, .6f), 2.4f)) { name = "Lana lighthouse lamp" }; night.lighthouseLamp = lamp; var lp = T + up * (Ht + 1.35f);
                z.mk.Blob(lamp, lp, Vector3.one * .6f, rng, 0f); halos.Add(lp, 4f, new Color(1f, .9f, .6f), .7f);
                var pivot = new GameObject("Lighthouse beam").transform; pivot.SetParent(night.transform, false); pivot.position = lp; night.beam = pivot;
                // the door and the little windows up the tower, toward the garden
                z.mk.Box(woodM, T + f.Fwd * (R0 - .02f) + up * 1.1f, new Vector3(1.2f, 2.2f, .12f), f.r); z.mk.Box(lampM, T + f.Fwd * (R0 + .03f) + up * 2.45f, new Vector3(.3f, .3f, .06f), f.r);
                foreach (var y in new[] { 4f, 8.4f, 13.4f }) { var wp = T + f.Fwd * (Rad(y) + .01f) + up * y; z.mk.Box(windowM, wp, new Vector3(.5f, .8f, .08f), f.r); halos.Add(wp + f.Fwd * .4f, 1f, warm, .3f); }
                // rocks round its foot, an anchor, a ship's bell to ring, two benches facing it
                for (var k = 0; k < 7; k++) { var a = (k * 51f + 20f) * Mathf.Deg2Rad; var q = T + (f.Right * Mathf.Cos(a) - f.Fwd * Mathf.Sin(a) * .9f) * 3.5f; if (Vector3.Dot(q - T, f.Fwd) > 1.5f) continue; q = ctx.OnGround(q.x, q.z); var s = .5f + (float)rng.NextDouble() * .5f; z.mk.Blob(Paint(new Color(.3f, .3f, .34f)), q + up * s * .3f, new Vector3(s, s * .6f, s), rng, .25f); ArtistBits.PostCollider(z.root, q, .6f, s * .8f); }
                var an = f.P(-3.6f, 0, -1.4f); z.mk.Box(iron, an + up * 1.1f, new Vector3(.2f, 2.2f, .2f), f.r * Quaternion.Euler(0, 0, 8f)); z.mk.Box(iron, an + up * 2f, new Vector3(1f, .14f, .14f), f.r); z.mk.Prism(iron, an + up * 2.3f, f.Fwd, .15f, .06f, 10);
                foreach (var e in new[] { -1f, 1f }) z.mk.Box(iron, an + up * .35f + f.Right * e * .4f, new Vector3(.9f, .16f, .16f), f.r * Quaternion.Euler(0, 0, e * 35f)); ArtistBits.PostCollider(z.root, an, 2.2f, .5f);
                var bp = f.P(3.4f, 0, -1.2f); z.mk.Prism(woodM, bp - up * .2f, up, .1f, 2.4f, 6); z.mk.Box(woodM, bp + up * 2.1f + f.Right * .3f, new Vector3(.8f, .1f, .1f), f.r); ArtistBits.PostCollider(z.root, bp, 2.2f, .14f);
                var bell = new MeshKit(); bell.Prism(WorldMaterials.Glow(new Color(.86f, .66f, .3f), .5f), -up * .42f, up, .22f, .38f, 10, .55f); bell.Prism(iron, -up * .06f, up, .03f, .1f, 4);
                var bgo = bell.BuildSingle("Ship's bell", tlife.transform, false); bgo.transform.position = bp + up * 2.05f + f.Right * .55f; Interact.Toy(bgo, Toy.Kind.Swing, "Ring the bell", new Vector3(.5f, .5f, .5f));
                foreach (var e in new[] { -1f, 1f }) { var bq = f.P(e * 4.4f, 0, 2.2f); Bench(z.root, z.mk, bq, FaceTo(bq, T)); }
                ArtistBits.PointLight(z.root, T + f.Fwd * 4.5f + up * 3f, new Color(1f, .86f, .6f), 18f, 2f);
                Clear(T, 7.5f); Clear(f.P(0, 0, 2f), 5.5f);
                Spoke(180f, f.P(0, 0, -.9f));
                Shot(ctx, "the lighthouse", f.P(6f, 2f, 14f), T + up * 9f);
            }

            // ------------------------------------------------ "Happiness is a butterfly": the Butterfly Garden
            {
                var f = Place(230f); var G = f.P(0, 0, -8.4f); const float Rg = 8f;
                for (var k = 0; k < 40; k++)
                {
                    float a0 = k * Mathf.PI * 2f / 40f, a1 = (k + 1) * Mathf.PI * 2f / 40f; var mida = (a0 + a1) / 2f; if (Mathf.Abs(Mathf.DeltaAngle(mida * Mathf.Rad2Deg, 90f)) < 14f) continue;
                    Vector3 p0 = G + (f.Right * Mathf.Cos(a0) + f.Fwd * Mathf.Sin(a0)) * Rg, p1 = G + (f.Right * Mathf.Cos(a1) + f.Fwd * Mathf.Sin(a1)) * Rg; var pm = ctx.OnGround((p0.x + p1.x) / 2f, (p0.z + p1.z) / 2f);
                    z.mk.Box(stone, pm + up * .12f, new Vector3(.32f, .4f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0));
                }
                var walk = new List<Vector3>(); for (var d = 0f; d <= 6.2f; d += 1f) { var q = f.P(0, 0, -d); walk.Add(ctx.OnGround(q.x, q.z)); }
                Strip(z.mk, walk, stone, 1.4f, .15f, ctx);
                for (var k = 0; k < 280; k++)
                {
                    var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = Mathf.Sqrt((float)rng.NextDouble()) * (Rg - .5f); var q = G + (f.Right * Mathf.Cos(a) + f.Fwd * Mathf.Sin(a)) * r;
                    if (r < 2.2f) continue; var lx = Vector3.Dot(q - G, f.Right); if (Mathf.Abs(lx) < 1.1f && Vector3.Dot(q - G, f.Fwd) > 0f) continue;
                    q = ctx.OnGround(q.x, q.z); var h = .22f + (float)rng.NextDouble() * .4f; z.mk.Prism(leafDark, q, up, .015f, h, 3);
                    z.mk.Blob(k % 3 == 0 ? flowerP : flowerW, q + up * h, Vector3.one * (.06f + (float)rng.NextDouble() * .05f), rng, .2f);
                }
                // the swing under a bar wound with flowers
                foreach (var e in new[] { -1f, 1f }) foreach (var s2 in new[] { -1f, 1f }) { var foot = G + f.Right * e * 1.6f + f.Fwd * s2 * .7f; Bar(z.mk, white, ctx.OnGround(foot.x, foot.z), G + f.Right * e * 1.5f + up * 3.2f, .06f); ArtistBits.PostCollider(z.root, ctx.OnGround(foot.x, foot.z), 1.6f, .1f); }
                z.mk.Box(white, G + up * 3.2f, new Vector3(3.2f, .1f, .1f), f.r); for (var k = 0; k < 18; k++) z.mk.Blob(k % 2 == 0 ? flowerW : flowerP, G + up * 3.25f + f.Right * (-1.5f + k * 3f / 17f) + Random3(rng) * .08f, Vector3.one * .1f, rng, .2f);
                foreach (var e in new[] { -.55f, .55f }) z.mk.Prism(dark, G + up * .6f + f.Right * e, up, .012f, 2.6f, 3);
                z.mk.Box(woodM, G + up * .58f, new Vector3(1.3f, .06f, .42f), f.r); z.solid.Box(woodM, G + up * .55f, new Vector3(1.3f, .12f, .42f), f.r); Interact.Seat(tlife.transform, G + up * .62f, f.Fwd, "Sit on the swing").seatHeight = .55f;
                // little lanterns round the ring
                for (var k = 0; k < 6; k++) { var a = (k * 60f + 30f) * Mathf.Deg2Rad; var q = G + (f.Right * Mathf.Cos(a) + f.Fwd * Mathf.Sin(a)) * (Rg + .8f); q = ctx.OnGround(q.x, q.z); z.mk.Prism(white, q - up * .1f, up, .06f, 1.1f, 5); z.mk.Blob(flowerW, q + up * 1.15f, Vector3.one * .13f, rng, 0f); ArtistBits.PostCollider(z.root, q, 1.1f, .08f); }
                // the butterflies, wings as wide as a door, circling slowly overhead
                var flyM = new Material(GlowM(new Color(.66f, .82f, 1f), 1.5f)) { name = "Lana butterflies" }; night.butterflyGlow = flyM;
                for (var k = 0; k < 9; k++)
                {
                    var body = new GameObject("Butterfly " + k).transform; body.SetParent(night.transform, false);
                    var bk = new MeshKit(); bk.Prism(dark, -Vector3.forward * .45f, Vector3.forward, .05f, .9f, 5); bk.BuildSingle("Body", body, false);
                    Transform Wing(float side)
                    {
                        var w = new GameObject(side > 0 ? "Right wing" : "Left wing").transform; w.SetParent(body, false);
                        var wk = new MeshKit(); var s = 1.3f;
                        Vector3 V(float x, float zz) => new Vector3(x * side * s, 0, zz * s);
                        foreach (var y in new[] { 1f, -1f }) { wk.Tri(flyM, V(0, .2f), V(1f, .65f), V(1.05f, -.05f), Vector3.up * y); wk.Tri(flyM, V(0, -.05f), V(.95f, -.15f), V(.5f, -.75f), Vector3.up * y); }
                        wk.BuildSingle("Wing", w, false); return w;
                    }
                    night.flies.Add(new LanaNight.Fly { body = body, left = Wing(-1f), right = Wing(1f), center = G + up * (3.4f + (k % 3) * 1.2f), radius = 2.4f + (k % 4) * 1.4f, speed = (k % 2 == 0 ? 1f : -1f) * (.18f + k * .025f), phase = k * 1.7f });
                }
                halos.Add(G + up * 1.2f, 5.5f, new Color(.7f, .82f, 1f), .22f);
                ArtistBits.PointLight(z.root, G + up * 3.5f, new Color(.74f, .84f, 1f), 15f, 1.8f);
                Clear(G, Rg + 1.4f);
                Spoke(230f, f.P(0, 0, .4f));
                Shot(ctx, "the butterfly garden", f.P(3f, 2.4f, 7f), G + up * 2.5f);
            }

            // ------------------------------------------------ "Say Yes To Heaven": the White Stair
            {
                var f = Place(300f); const int N = 34; const float Rise = .36f, Run = .78f, X0 = -13f, Zs = -2f;
                night.steps = new Material[N];
                for (var k = 0; k < N; k++)
                {
                    var top = (k + 1) * Rise; var sc = f.P(X0 + k * Run, top - .14f, Zs);
                    z.mk.Box(white, sc, new Vector3(Run + .08f, .28f, 2.4f), f.r); z.solid.Box(white, sc, new Vector3(Run + .08f, .28f, 2.4f), f.r);
                    var sm = new Material(GlowM(new Color(.92f, .94f, 1f), 1f)) { name = "White stair " + k }; night.steps[k] = sm;
                    z.mk.Box(sm, f.P(X0 + k * Run - Run / 2f, top - .27f, Zs), new Vector3(.07f, .07f, 2.42f), f.r);
                    if (k % 4 == 0) Clear(sc, 2.4f);
                }
                var topY = N * Rise; var pc = f.P(X0 + (N - 1) * Run + Run / 2f + 3f, 0, Zs); var pt = pc + up * topY;
                z.mk.Prism(white, pt - up * .4f, up, 3.2f, .4f, 18); z.solid.Prism(white, pt - up * .4f, up, 3.2f, .4f, 18);
                z.mk.Prism(night.steps[N - 1], pt - up * .45f, up, 3.1f, .04f, 18);
                for (var k = 0; k < 22; k++)
                {
                    float a0 = k * Mathf.PI * 2f / 22f, a1 = (k + 1) * Mathf.PI * 2f / 22f; var mida = (a0 + a1) / 2f;
                    var toStair = -f.Right; var ang = Vector3.SignedAngle(toStair, f.Right * Mathf.Cos(mida) + f.Fwd * Mathf.Sin(mida), up); if (Mathf.Abs(ang) < 22f) continue;
                    Vector3 p0 = pt + (f.Right * Mathf.Cos(a0) + f.Fwd * Mathf.Sin(a0)) * 3f, p1 = pt + (f.Right * Mathf.Cos(a1) + f.Fwd * Mathf.Sin(a1)) * 3f;
                    z.mk.Box(white, (p0 + p1) / 2f + up * .45f, new Vector3(.16f, .9f, Vector3.Distance(p0, p1) + .04f), Quaternion.LookRotation(p1 - p0)); z.solid.Box(white, (p0 + p1) / 2f + up * .45f, new Vector3(.16f, .9f, Vector3.Distance(p0, p1) + .04f), Quaternion.LookRotation(p1 - p0));
                }
                // an arch of white flowers at the far side, a bench looking back over the garden
                var far = pt + f.Right * 2.3f;
                foreach (var e in new[] { -1f, 1f }) z.mk.Prism(white, far + f.Fwd * e * 1.3f, up, .07f, 2.4f, 5);
                for (var k = 0; k <= 16; k++) { var a = Mathf.PI * k / 16f; z.mk.Blob(k % 3 == 0 ? flowerP : flowerW, far + up * (2.4f + Mathf.Sin(a) * 1.1f) + f.Fwd * Mathf.Cos(a) * 1.3f, Vector3.one * .17f, rng, .2f); }
                var bq = pt - f.Fwd * 1.6f + f.Right * .2f; Bench(z.root, z.mk, bq, FaceTo(bq, c));
                // the column of light going up from the platform
                night.heavenBeam = Column(night.transform, pt, 3f, 90f);
                halos.Add(pt + up * 1f, 4f, new Color(.94f, .95f, 1f), .35f);
                ArtistBits.PointLight(z.root, pt + up * 2.4f, new Color(1f, .97f, .92f), 16f, 2f); ArtistBits.PointLight(z.root, f.P(X0, 2f, Zs + 2f), new Color(1f, .97f, .92f), 12f, 1.6f);
                night.heavenTop = pt;
                Spoke(300f, f.P(X0 - 1.6f, 0, Zs));
                Shot(ctx, "the white stair", f.P(X0 - 6f, 2f, 10f), f.P(0, 6f, Zs));
            }

            // ------------------------------------------------ benches with lamps between the places
            foreach (var deg in new[] { 28f, 96f, 156f, 206f, 266f, 332f })
            {
                var bq = Ring(deg, 28f); if (!Clean(bq.x, bq.z, 2f) || NearPath(bq.x, bq.z, 2.4f)) continue;
                Bench(z.root, z.mk, bq, Dir(deg)); var lq = bq + Vector3.Cross(up, Dir(deg)) * 1.7f - Dir(deg) * .6f; GlobeLamp(ctx, z.mk, ctx.OnGround(lq.x, lq.z), true); Clear(bq, 2.5f);
            }

            // ------------------------------------------------ the moon, low and huge over the lighthouse
            {
                var mc = c + Dir(180f) * 340f + up * 75f; var face = (c + up * 10f - mc).normalized; var mr = Quaternion.LookRotation(face);
                var moon = new GameObject("Lana's moon").transform; moon.SetParent(night.transform, false); moon.SetPositionAndRotation(mc, mr);
                var mm = new Material(GlowM(new Color(.86f, .9f, 1f), 1.6f)) { name = "Lana's moon" }; var mk = new MeshKit();
                for (var k = 0; k < 40; k++) { float a0 = k * Mathf.PI / 20f, a1 = (k + 1) * Mathf.PI / 20f; mk.Tri(mm, Vector3.zero, new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 26f, new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 26f, -Vector3.forward); }
                var mgo = mk.BuildSingle("Moon", moon, false); night.moon = mgo.GetComponent<Renderer>(); night.moonGlow = mm;
                night.moonHalo = Disc(moon, 70f, new Color(.6f, .7f, 1f, .22f));
            }

            // ------------------------------------------------ night grass and moonflowers over the whole quarter
            {
                var mat = WindMesh.Material(.3f, .7f, new Color(.16f, .24f, .28f)); var meshes = new Dictionary<(int, int), WindMesh>(); var tufts = 0; var blooms = 0;
                for (var x = c.x - QuarterR; x < c.x + QuarterR; x += 1.5f)
                for (var zz = c.z - QuarterR; zz < c.z + QuarterR; zz += 1.5f)
                {
                    float px = x + ((float)rng.NextDouble() - .5f) * 1.5f, pz = zz + ((float)rng.NextDouble() - .5f) * 1.5f; var r = Vector2.Distance(new Vector2(px, pz), new Vector2(c.x, c.z));
                    if (r < 16.5f || r > QuarterR - 2.5f || !ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.4f) || !Clean(px, pz, 0f)) continue;
                    var key = (Mathf.FloorToInt(px / 40f), Mathf.FloorToInt(pz / 40f)); if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                    var col = Color.Lerp(new Color(.1f, .17f, .2f), new Color(.16f, .25f, .27f), WindMesh.Patch(px, pz, .05f, 5f)); var g = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                    wm.Tuft(g, .25f + (float)rng.NextDouble() * .35f, col, new Color(.3f, .42f, .52f), rng, 3, .07f, .1f); tufts++;
                    if (rng.NextDouble() < .07) { var h = .3f + (float)rng.NextDouble() * .3f; z.mk.Prism(leafDark, g, up, .012f, h, 3); z.mk.Blob(rng.NextDouble() < .8 ? flowerW : flowerP, g + up * h, Vector3.one * .07f, rng, .2f); blooms++; }
                }
                foreach (var kv in meshes) kv.Value.Build("Lana night grass", z.root, mat);
                Debug.Log($"Lana's quarter: {cyps} cypresses, {spokes} walks, {tufts} tufts of night grass, {blooms} moonflowers");
            }
            noGrass.Add(new Vector3(c.x, QuarterR + 2f, c.z));
            Shot(ctx, "lana's quarter from above", c + fr * 30f + up * 34f, c - fr * 20f);
        }

        static Vector3 Random3(System.Random rng) => new Vector3((float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f) * 2f;

        /// A thin square bar from a to b.
        static void Bar(MeshKit mk, Material m, Vector3 a, Vector3 b, float t)
        {
            var d = b - a; if (d.sqrMagnitude < .0001f) return; var rot = Mathf.Abs(Vector3.Dot(d.normalized, Vector3.up)) > .99f ? Quaternion.LookRotation(d, Vector3.forward) : Quaternion.LookRotation(d);
            mk.Box(m, (a + b) / 2f, new Vector3(t, t, d.magnitude), rot);
        }

        /// A wall from a to b (local to the frame, on the ground) with holes in it: (centre along the wall from its
        /// middle, width, bottom, top). The pieces between and round the holes each get a collider.
        void HoleWall(MeshKit mk, MeshKit solid, Material m, PlaceFrame f, Vector3 a, Vector3 b, float h, float t, (float at, float w, float lo, float hi)[] holes)
        {
            var len = Vector3.Distance(a, b); var u = (b - a) / len; var rot = f.r * Quaternion.LookRotation(u);
            void Piece(float s0, float s1, float y0, float y1)
            {
                if (s1 - s0 < .01f || y1 - y0 < .01f) return;
                var mid = f.o + f.r * (a + u * ((s0 + s1) / 2f)); var c = new Vector3(mid.x, f.o.y + (y0 + y1) / 2f, mid.z); var size = new Vector3(t, y1 - y0, s1 - s0);
                if (y0 < .01f) { c.y -= .3f; size.y += .3f; }
                mk.Box(m, c, size, rot); solid.Box(m, c, size, rot);
            }
            var list = new List<(float at, float w, float lo, float hi)>(holes); list.Sort((p, q) => p.at.CompareTo(q.at));
            var s = 0f;
            foreach (var hl in list)
            {
                float h0 = len / 2f + hl.at - hl.w / 2f, h1 = len / 2f + hl.at + hl.w / 2f;
                Piece(s, h0, 0f, h); Piece(h0, h1, 0f, hl.lo); Piece(h0, h1, hl.hi, h); s = h1;
            }
            Piece(s, len, 0f, h);
        }

        /// A soft column of light standing up from a point (two crossed planes, bright at the foot, fading up).
        static Material Column(Transform parent, Vector3 at, float r, float height)
        {
            var v = new List<Vector3>(); var col = new List<Color>(); var t = new List<int>();
            foreach (var d in new[] { Vector3.right, Vector3.forward, (Vector3.right + Vector3.forward).normalized, (Vector3.right - Vector3.forward).normalized })
            {
                var b = v.Count; v.Add(-d * r); v.Add(d * r); v.Add(d * r * 1.3f + Vector3.up * height); v.Add(-d * r * 1.3f + Vector3.up * height);
                col.Add(new Color(1, 1, 1, .7f)); col.Add(new Color(1, 1, 1, .7f)); col.Add(new Color(1, 1, 1, 0f)); col.Add(new Color(1, 1, 1, 0f));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = "Column of light" }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Column of light"); go.transform.SetParent(parent, false); go.transform.position = at;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); var m = LanaNight.Additive("Column of light"); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return m;
        }

        /// A soft round glow facing along the parent's forward (bright in the middle, fading to nothing at the edge).
        static Material Disc(Transform parent, float r, Color c)
        {
            var v = new List<Vector3> { Vector3.zero }; var col = new List<Color> { c }; var t = new List<int>();
            for (var k = 0; k < 40; k++) { var a = k * Mathf.PI / 20f; v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r); col.Add(new Color(c.r, c.g, c.b, 0f)); t.AddRange(new[] { 0, 1 + k, 1 + (k + 1) % 40 }); }
            var mesh = new Mesh { name = "Glow" }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Glow"); go.transform.SetParent(parent, false); go.transform.localPosition = -Vector3.forward * 2f;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); var m = LanaNight.Additive("Moon glow"); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return m;
        }
    }
}
