using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Boom Bap Harbour as a mountain monastery built of brick housing blocks (part of BoomKit). The Block, the El,
    /// the party and the harbour stay exactly as they were: the mountain rises behind where you arrive, on ground
    /// that was empty, and the monastery is the shape the projects take as they climb it. It is still boom bap:
    /// brick, graffiti, stoops, fire escapes, the heat.
    /// - a stair street climbs from the top of the Block to the summit under gates of the El's green iron;
    /// - brick blocks are stacked up the mountainside either side of it, each on its own terrace, fire escapes
    ///   zigzagging down their fronts like temple stairs, a water tower on every roof built like a pagoda;
    /// - on the summit, the Hall: brick under three tiers of roof, a wide stoop where the old masters sit, a gong
    ///   that is a speaker cone, a yard of wooden dummies in headphones to practise on;
    /// - and in the harbour below, junks with patched sails.
    public partial class BoomKit
    {
        Vector2 summit2; bool hasMountain;
        const float MountH = 54f, MountR = 150f, SummitD = 185f;
        // the blocks: how far up the mountain from where you arrive, how far to the side of the stair, how many floors
        static readonly (float d, float s, int floors)[] Stack = { (96f, -19f, 4), (97f, 23f, 5), (121f, -21f, 5), (123f, 22f, 6), (147f, -20f, 6), (146f, 23f, 5), (128f, 50f, 4), (131f, -50f, 4) };

        /// Raises the mountain behind the top of the Block (called from ShapeGround once the Block's direction is
        /// known): a smooth rise to the summit, nothing within reach of the coast, nothing where you arrive.
        void RaiseMountain(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; summit2 = heart2 - D2 * SummitD; hasMountain = false;
            bool Land(float x, float z) => data.IslandAt(x, z) == isl;
            if (!Land(summit2.x, summit2.y)) return;
            for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (!Land(summit2.x + Mathf.Cos(a) * 70f, summit2.y + Mathf.Sin(a) * 70f)) return; }
            hasMountain = true;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - summit2.y) > MountR) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - summit2.x) > MountR) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var q = Vector2.Distance(new Vector2(x, z), summit2) / MountR; if (q >= 1f) continue;
                    var inland = 120f;
                    foreach (var rad in new[] { 20f, 45f, 70f, 95f }) { var sea = false; for (var k = 0; k < 8 && !sea; k++) { var a = k * Mathf.PI / 4f; if (!Land(x + Mathf.Cos(a) * rad, z + Mathf.Sin(a) * rad)) sea = true; } if (sea) { inland = rad; break; } }
                    var g = (1f - q * q) * (1f - q * q);
                    data.heights[i] += MountH * g * Ease(10f, 95f, inland);
                }
            }
        }

        /// The summit and the blocks' terraces, made level (called at the end of ShapeGround).
        void LevelMountain(IslandWorldData data, int index)
        {
            if (!hasMountain) return; var isl = data.layout.islands[index];
            Level(data, index, summit2, 25f, 46f, data.HeightAt(summit2.x, summit2.y) - .5f);
            var blocksLevelled = true;
            foreach (var (d, s, _) in Stack)
            {
                var p = P2(-d, s); if (data.IslandAt(p.x, p.y) != isl) continue; var free = true;
                foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && Vector2.Distance(new Vector2(a.x, a.z), p) < a.r + 15f) free = false;
                if (free) Level(data, index, p, 11.5f, 22f, data.HeightAt(p.x, p.y));
            }
            // the stair: its ground made one even ramp from end to end (the terraces beside it would otherwise leave steps in it)
            if (blocksLevelled)
            {
                var hs = new List<float>(); var ps = new List<Vector2>(); for (var d = 14f; d <= SummitD - 22f; d += 2f) { var p = P2(-d, 3f); ps.Add(p); hs.Add(data.HeightAt(p.x, p.y)); }
                for (var pass = 0; pass < 6; pass++) { var s = hs.ToArray(); for (var i = 1; i + 1 < hs.Count; i++) hs[i] = (s[i - 1] + s[i] * 2f + s[i + 1]) / 4f; }
                for (var i = 0; i < ps.Count; i++) Level(data, index, ps[i], 3.4f, 7.5f, hs[i]);
            }
        }

        void Temple(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up;
            Junks(ctx, mk);
            if (!hasMountain) { Debug.Log("Boom Bap Harbour: no room was found for the mountain"); return; }
            Vector3 On(float d, float s) => G(ctx, -d, s);   // d metres up the mountain from where you arrive
            // ---- the stair street
            const float StairS = 3f, Top = SummitD - 23f; var stone = Paint(new Color(.62f, .6f, .56f), Surface.Stone); var stoneDark = Paint(new Color(.42f, .4f, .38f), Surface.Stone);
            var pts = new List<Vector3>(); for (var d = 19f; d <= Top + .01f; d += 1.5f) pts.Add(On(d, StairS));
            Strip(mk, pts, stone, 5.2f, .14f, ctx); foreach (var p in pts) Remember(p, 8);
            for (var d = 20f; d < Top; d += 1.2f) { var a = On(d, StairS); var b = On(d + 1.2f, StairS); if (b.y - a.y > .12f) mk.Box(stoneDark, a + up * .17f, new Vector3(5f, .05f, .22f), Quaternion.LookRotation(-D)); }
            string[] gates = { "UPTOWN", "THE CLIMB", "ONE STEP AT A TIME", "KEEP YOUR HEAD UP", "ALMOST", "THE SUMMIT" }; var gn = 0;
            for (var d = 30f; d < Top - 4f; d += 25f, gn++)
            {
                var c = On(d, StairS); foreach (var e in new[] { -1f, 1f }) { var foot = On(d, StairS + e * 3.5f); mk.Box(steelEl, foot + up * 2.6f, new Vector3(.4f, 5.8f, .4f), Quaternion.LookRotation(D)); ArtistBits.PostCollider(ctx.Root, foot, 5f, .28f); mk.Box(steelEl, foot + up * 4.6f - S * e * .8f, new Vector3(.2f, .2f, 2f), Quaternion.LookRotation(S) * Quaternion.Euler(-40f * e, 0, 0)); }
                var beamAt = new Vector3(c.x, Mathf.Max(On(d, StairS - 3.5f).y, On(d, StairS + 3.5f).y) + 5.3f, c.z); mk.Box(steelEl, beamAt, new Vector3(8.2f, .5f, .5f), Quaternion.LookRotation(D)); mk.Box(steelEl, beamAt + up * .7f, new Vector3(9.4f, .22f, .9f), Quaternion.LookRotation(D));
                mk.Box(Paint(new Color(.1f, .32f, .2f)), beamAt - up * .75f, new Vector3(4.6f, .8f, .1f), Quaternion.LookRotation(D)); Sign(beamAt - up * .75f + D * .07f, D, gates[gn % gates.Length], .34f, Cream);
                if (gn % 2 == 0) StreetLight(ctx, mk, On(d + 12f, StairS - 3.4f), S);
            }
            // ---- the blocks, stacked up the mountainside
            var built = 0;
            for (var i = 0; i < Stack.Length; i++)
            {
                var (d, s, floors) = Stack[i]; var g = On(d, s); if (!ctx.IsLand(g.x, g.z, 12f) || InFootprint(ctx, g.x, g.z, 14f)) continue;
                Block(ctx, mk, solid, new PlaceFrame(g, D), floors, i); built++;
            }
            // pagoda water towers standing on their own between them
            foreach (var (d, s) in new[] { (74f, -44f), (108f, 62f), (158f, 46f), (166f, -42f) }) { var g = On(d, s); if (ctx.IsLand(g.x, g.z, 8f) && !InFootprint(ctx, g.x, g.z, 6f)) { foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var leg = g + new Vector3(x * 1.3f, 0, zz * 1.3f); leg.y = ctx.Ground(leg.x, leg.z) - .3f; mk.Box(steelEl, new Vector3(leg.x, (leg.y + g.y + 4.6f) / 2f, leg.z), new Vector3(.2f, g.y + 4.6f - leg.y, .2f), Quaternion.identity); } PagodaTank(mk, g + up * 4.5f, 1.25f); ArtistBits.PostCollider(ctx.Root, g, 5f, 1.6f); Mark(g, 3f); } }

            // ---- the summit: the Hall
            var sg = ctx.OnGround(center.x - D.x * SummitD, center.z - D.z * SummitD); var f = new PlaceFrame(sg, D); var rng = new System.Random(36);   // f.Fwd looks down the mountain to the harbour
            var brick = Paint(Browns[0], Surface.Stone); var brickDark = Paint(Browns[3], Surface.Stone); var tar = Paint(new Color(.16f, .2f, .18f)); var roofGreen = Paint(new Color(.2f, .42f, .34f), Surface.Metal);
            const float HW = 24f, HD = 14f, HH = 8.5f; var hc = f.P(0, 0, -4f);
            mk.Prism(stone, sg - up * .4f, up, 24f, .5f, 20);
            mk.Box(brick, hc + up * (HH / 2f - 1f), new Vector3(HW, HH + 2f, HD), f.r); solid.Box(brick, hc + up * (HH / 2f - 1f), new Vector3(HW, HH + 2f, HD), f.r);
            for (var k = -4; k <= 4; k++) { if (k == 0) continue; mk.Box(k % 2 == 0 ? (Material)windowM : glassM, hc + f.Right * (k * 2.5f) + up * 4.6f + f.Fwd * (HD / 2f + .02f), new Vector3(1.2f, 2.6f, .06f), f.r); mk.Box(brickDark, hc + f.Right * (k * 2.5f) + up * 6.05f + f.Fwd * (HD / 2f + .04f), new Vector3(1.5f, .22f, .1f), f.r); }
            mk.Box(dark, hc + up * 2f + f.Fwd * (HD / 2f + .03f), new Vector3(3.4f, 4f, .08f), f.r); mk.Box(goldM, hc + up * 4.2f + f.Fwd * (HD / 2f + .06f), new Vector3(4f, .3f, .14f), f.r); Sign(hc + up * 5.3f + f.Fwd * (HD / 2f + .1f), f.Fwd, "THE HALL OF THE BREAK", .5f, Gold);
            // three tiers of roof, each smaller, the corners turned up
            for (var k = 0; k < 3; k++)
            {
                float w = HW + 5f - k * 7.4f, dd = HD + 5f - k * 4.2f, y = HH + k * 3.3f; var at = hc + up * y;
                mk.Box(roofGreen, at + up * .15f, new Vector3(w, .3f, dd), f.r); mk.Box(tar, at + up * .5f, new Vector3(w - 2f, .5f, dd - 2f), f.r);
                foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var corner = at + f.Right * (x * w / 2f) + f.Fwd * (zz * dd / 2f); var tip = (f.Right * x + f.Fwd * zz).normalized; mk.Prism(roofGreen, corner + up * .2f - tip * .6f, (tip + up * .55f).normalized, .22f, 1.9f, 4, .2f); mk.Blob(goldM, corner + (tip + up * .55f).normalized * 1.4f + up * .3f, Vector3.one * .12f, rng, 0f); }
                if (k < 2) { mk.Box(brick, at + up * 2f, new Vector3(w - 9.4f, 3.4f, dd - 6f), f.r); for (var q = -1; q <= 1; q++) mk.Box(windowM, at + up * 2.1f + f.Right * (q * (w - 9.4f) * .3f) + f.Fwd * ((dd - 6f) / 2f + .02f), new Vector3(1f, 1.4f, .06f), f.r); }
            }
            var fin = hc + up * (HH + 3.3f * 2f + .8f); mk.Prism(goldM, fin, up, .14f, 2.6f, 6); mk.Blob(goldM, fin + up * 2.9f, new Vector3(.34f, .44f, .34f), rng, 0f); halos.Add(fin + up * 2.9f, 3f, Gold, .4f);   // a gold microphone for a finial
            // the wide stoop, and the masters who sit on it
            for (var k = 0; k < 4; k++) { var st = hc + f.Fwd * (HD / 2f + .6f + k * .6f) + up * (1.05f - k * .3f); mk.Box(stoneDark, st - up * .5f, new Vector3(13f, 1.3f - k * .3f + .3f, .62f), f.r); solid.Box(stoneDark, st - up * .5f, new Vector3(13f, 1.3f - k * .3f + .3f, .62f), f.r); }
            string[][] wisdom =
            {
                new[] { "You climbed all that to ask me something. Sit first. The question changes when you sit.", "Thirty-six steps to the summit, they say. I've counted. It is a great many more. It's the saying that matters.", "The break is four bars long. You could study those four bars your whole life. I have." },
                new[] { "Brick on brick, block on block. Nobody planned a monastery. It's just what the projects turned into, the higher they went.", "Down there it's a party. Up here it's the same record, played once, and listened to.", "Every fire escape on this mountain is a staircase to somebody's roof. Every roof is a place to practise." },
                new[] { "The young ones want to be fast. Fast is easy. Be on time.", "Hit the gong if you like. Everybody does. It tells the harbour somebody made it up.", "See the junks? They bring the records. Always have." },
            };
            for (var k = 0; k < 3; k++)
            {
                var seatAt = hc + f.Fwd * (HD / 2f + 1.5f) + f.Right * (-4.6f + k * 4.6f) + up * .9f; var m = Local(); m.hat = k == 1 ? Residents.Hat.Bucket : Residents.Hat.None; m.hair = new Color(.86f, .86f, .84f); m.top = k == 0 ? new Color(.86f, .5f, .1f) : k == 1 ? new Color(.62f, .16f, .12f) : new Color(.95f, .78f, .25f);
                Folk(seatAt, f.Fwd, Resident.Pose.Sit, wisdom[k], m, null, .3f);
            }
            Interact.Seat(tlife.transform, hc + f.Fwd * (HD / 2f + 1.5f) + f.Right * 7.2f + up * .95f, f.Fwd, "Sit on the Hall's stoop").seatHeight = .3f;
            // the gong: a speaker cone the size of a door, hung in a frame of green iron
            var go = f.P(-13f, 0, 9f); foreach (var e in new[] { -1f, 1f }) { mk.Box(steelEl, go + f.Right * e * 2.5f + up * 2.6f, new Vector3(.36f, 5.2f, .36f), f.r); ArtistBits.PostCollider(ctx.Root, go + f.Right * e * 2.5f, 5f, .26f); } mk.Box(steelEl, go + up * 5.3f, new Vector3(5.8f, .4f, .5f), f.r);
            var gm = new MeshKit(); gm.Prism(dark, new Vector3(0, -2.4f, -.14f), Vector3.forward, 1.9f, .28f, 16); gm.Prism(goldM, new Vector3(0, -2.4f, .14f), Vector3.forward, 1.9f, .05f, 16); gm.Prism(dark, new Vector3(0, -2.4f, .16f), Vector3.forward, 1.6f, .05f, 16); gm.Prism(goldM, new Vector3(0, -2.4f, .18f), Vector3.forward, .5f, .1f, 12); foreach (var e in new[] { -1.2f, 1.2f }) gm.Box(chain, new Vector3(e, -.5f, 0), new Vector3(.05f, 1f, .05f), Quaternion.identity);
            var ggo = gm.BuildSingle("The gong", tlife.transform, false); ggo.transform.SetPositionAndRotation(go + up * 5.1f, f.r);
            string[] strike = { "BOOM. It goes down the mountain like a kick drum and comes back off the harbour as a snare.", "The gong sounds. On the stoop, three old heads nod, once, in time.", "One hit. You feel it in your chest for eight bars." }; var sn = 0;
            var gt = Interact.Toy(ggo, Toy.Kind.Swing, "Strike the gong", new Vector3(4f, 5f, 1.6f)); gt.axis = Vector3.right; gt.amount = 10f; gt.seconds = 2.4f; gt.onUse = _ => Say(tlife.transform, go + up * 6.4f, strike[sn++ % strike.Length], 5f);
            // the practice yard: wooden dummies in headphones, crates of records, somebody at it
            var yd = f.P(13f, 0, 8f);
            for (var k = 0; k < 4; k++)
            {
                var p = yd + f.Right * ((k % 2) * 2.6f - 1.3f) + f.Fwd * ((k / 2) * 2.8f - 1.4f); mk.Prism(woodM, p - up * .2f, up, .16f, 1.9f, 8); ArtistBits.PostCollider(ctx.Root, p, 1.7f, .2f);
                for (var a = 0; a < 3; a++) mk.Prism(woodM, p + up * (.8f + a * .34f), Quaternion.Euler(0, a * 120f + k * 40f, 0) * Vector3.forward, .045f, .62f, 5);
                mk.Blob(woodM, p + up * 1.86f, Vector3.one * .2f, rng, 0f); mk.Box(dark, p + up * 2.06f, new Vector3(.46f, .05f, .08f), Quaternion.Euler(0, k * 40f, 0)); foreach (var e in new[] { -.22f, .22f }) mk.Blob(dark, p + up * 1.88f + Quaternion.Euler(0, k * 40f, 0) * Vector3.right * e, new Vector3(.07f, .1f, .1f), rng, 0f);
            }
            Folk(yd + f.Fwd * 3.6f, -f.Fwd, Resident.Pose.Work, new[] { "Jab, jab, cross. On the one. You don't hit the dummy. You hit the beat.", "Two years I swept this yard before they let me touch a turntable.", "My master says my hands are too loud. I'm working on it." });
            for (var k = 0; k < 3; k++) Crate(mk, yd - f.Right * 4f + f.Fwd * (k * .9f - .9f), f.Right, k);
            // a boombox on a plinth either side of the top of the stair, the way a temple has its lions
            foreach (var e in new[] { -1f, 1f }) { var gp = On(Top + 2f, StairS + e * 4.4f); mk.Box(stone, gp + up * .6f, new Vector3(1.9f, 1.2f, 1.2f), f.r); Col(gp + up * .6f, new Vector3(1.9f, 1.2f, 1.2f), f.r); var bx = new MeshKit(); Boombox(bx, e < 0 ? Red : Sky, bulbM); var bgo = bx.BuildSingle("Guardian boombox", ctx.Root, false); bgo.transform.SetPositionAndRotation(gp + up * 1.2f, f.r); bgo.transform.localScale = Vector3.one * 2.4f; }
            for (var k = 0; k < 4; k++) { var a = (k * 90f + 45f) * Mathf.Deg2Rad; var lp = sg + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 21f; if (!InFootprint(ctx, lp.x, lp.z, 1f)) StreetLight(ctx, mk, ctx.OnGround(lp.x, lp.z), (sg - lp).normalized); }
            Graffiti(mk, hc + f.Right * (HW / 2f + .03f) + up * 3.4f, f.Right, 9f, 3f, 5); Graffiti(mk, hc - f.Right * (HW / 2f + .03f) + up * 3.4f, -f.Right, 9f, 3f, 9);
            ArtistBits.PointLight(ctx.Root, hc + f.Fwd * (HD / 2f + 4f) + up * 4f, Gold, 26f, 1.6f); Mark(sg, 24f); for (var d = 19f; d < Top; d += 8f) Mark(On(d, StairS), 3.4f);
            Shot(ctx, "the stair to the summit", On(22f, StairS) + up * 2.2f, On(120f, StairS) + up * 8f);
            Shot(ctx, "the stacked blocks", On(38f, 30f) + up * 3f, On(126f, 0f) + up * 6f);
            Shot(ctx, "the hall of the break", f.P(8f, 2.4f, 21f), hc + up * 9f);
            Shot(ctx, "down the mountain to the harbour", f.P(0, 3f, 10f), G(ctx, 60f, 0) + up * 2f);
            Debug.Log($"Boom Bap Harbour: the mountain is {sg.y - ctx.Ground(center.x, center.z):0} m above where you arrive, {built} blocks stacked on it");
        }

        /// Brick retaining walls along the mountain's contours, on the side that faces the Block: three rings of them,
        /// tagged, with a gap after every two lengths so that nothing is fenced off. Built after the lanes, which they keep clear of.
        void MountainWalls(LandContext ctx, MeshKit mk)
        {
            if (!hasMountain) return; var up = Vector3.up;
            var wallM = Paint(Browns[1], Surface.Stone); var capM = Paint(Browns[3] * .8f, Surface.Stone); var walls = 0; var sc = new Vector3(center.x - D.x * SummitD, 0, center.z - D.z * SummitD);
            foreach (var rad in new[] { 58f, 88f, 118f })
            {
                var n = Mathf.RoundToInt(rad * 2.6f / 9f);
                for (var k = -n; k <= n; k++)
                {
                    if (((k % 3) + 3) % 3 == 2) continue; var ang = k * 9f / rad; var dir = Quaternion.Euler(0, ang * Mathf.Rad2Deg, 0) * D; var p = sc + dir * rad; var g = ctx.OnGround(p.x, p.z);
                    var lat = Vector3.Dot(g - center, S); if (Mathf.Abs(lat - 3f) < 8.8f) continue; if (!ctx.IsLand(g.x, g.z, 10f) || InFootprint(ctx, g.x, g.z, 5f) || NearPath(g.x, g.z, 6f)) continue;
                    var blocked = false; foreach (var (bd, bs, _) in Stack) if ((G(ctx, -bd, bs) - g).sqrMagnitude < 13f * 13f) blocked = true; if (blocked) continue;
                    var rot = Quaternion.LookRotation(dir); var hgt = 2.4f + (walls % 3) * .4f;
                    mk.Box(wallM, g + up * (hgt / 2f - .6f), new Vector3(8.4f, hgt + 1.2f, .7f), rot); mk.Box(capM, g + up * (hgt + .1f), new Vector3(8.7f, .22f, .95f), rot); Col(g + up * (hgt / 2f - .6f), new Vector3(8.4f, hgt + 1.2f, .7f), rot);
                    if (walls % 2 == 0) Graffiti(mk, g + up * (hgt * .5f) + dir * .37f, dir, 5.4f, hgt * .7f, walls + 3);
                    Mark(g, 4.4f); walls++;
                }
            }
            Debug.Log($"Boom Bap Harbour: {walls} lengths of retaining wall on the mountain");
        }

        /// A brick housing block on its terrace: floors of windows, a cornice, a stoop, a fire escape zigzagging down
        /// the front like a temple stair, and a pagoda for a water tower.
        void Block(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int floors, int i)
        {
            var up = Vector3.up; const float W = 16f, Dp = 10f, FH = 3.1f; var H = floors * FH + .8f; var brick = Paint(Browns[i % Browns.Length], Surface.Stone); var trim = Paint(Browns[(i + 3) % Browns.Length] * .8f, Surface.Stone);
            mk.Box(brick, f.P(0, H / 2f - 2f, 0), new Vector3(W, H + 4f, Dp), f.r); solid.Box(brick, f.P(0, H / 2f - 2f, 0), new Vector3(W, H + 4f, Dp), f.r);
            mk.Box(trim, f.P(0, H + .2f, 0), new Vector3(W + .7f, .5f, Dp + .7f), f.r); mk.Box(trim, f.P(0, .5f, Dp / 2f + .04f), new Vector3(W + .2f, 1f, .1f), f.r);
            for (var fl = 0; fl < floors; fl++) for (var k = 0; k < 5; k++)
            {
                if (fl == 0 && k == 2) continue; var x = -6f + k * 3f; var y = fl * FH + 2f; var lit = (k * 7 + fl * 5 + i * 3) % 4 == 0;
                foreach (var e in new[] { -1f, 1f }) { mk.Box(lit ? (Material)windowM : glassM, f.P(x, y, e * (Dp / 2f + .02f)), new Vector3(1.2f, 1.7f, .06f), f.r); mk.Box(trim, f.P(x, y - .95f, e * (Dp / 2f + .05f)), new Vector3(1.5f, .14f, .12f), f.r); }
            }
            // the door and its stoop
            mk.Box(dark, f.P(0, 1.2f, Dp / 2f + .03f), new Vector3(1.4f, 2.4f, .08f), f.r); for (var k = 0; k < 3; k++) { var st = f.P(0, .45f - k * .2f, Dp / 2f + .45f + k * .5f); mk.Box(trim, st - up * .4f, new Vector3(3f, 1.1f - k * .2f, .52f), f.r); solid.Box(trim, st - up * .4f, new Vector3(3f, 1.1f - k * .2f, .52f), f.r); }
            Interact.Seat(tlife.transform, f.P(1f, .5f, Dp / 2f + .7f), f.Fwd, "Sit on the stoop").seatHeight = .3f;
            // the fire escape: a landing at every floor, a stair slanting between them one way and then the other
            for (var fl = 1; fl < floors; fl++)
            {
                var y = fl * FH + .9f; var x0 = 4.6f; mk.Box(iron, f.P(x0, y, Dp / 2f + .7f), new Vector3(4.2f, .08f, 1.2f), f.r); mk.Box(iron, f.P(x0, y + .9f, Dp / 2f + 1.28f), new Vector3(4.2f, .05f, .05f), f.r); foreach (var e in new[] { -2.1f, 0f, 2.1f }) mk.Box(iron, f.P(x0 + e, y + .45f, Dp / 2f + 1.28f), new Vector3(.05f, .9f, .05f), f.r);
                var dir = fl % 2 == 0 ? 1f : -1f; mk.Box(iron, f.P(x0, y - FH / 2f, Dp / 2f + .9f), new Vector3(.5f, .06f, Mathf.Sqrt(FH * FH + 3f * 3f)), f.r * Quaternion.Euler(0, 90f, 0) * Quaternion.Euler(Mathf.Atan2(FH, 3f) * Mathf.Rad2Deg * dir, 0, 0));
            }
            // the roof: a parapet, and the water tower as a pagoda
            foreach (var e in new[] { -1f, 1f }) { mk.Box(trim, f.P(e * (W / 2f - .15f), H + .75f, 0), new Vector3(.3f, .7f, Dp), f.r); mk.Box(trim, f.P(0, H + .75f, e * (Dp / 2f - .15f)), new Vector3(W, .7f, .3f), f.r); }
            PagodaTank(mk, f.P(i % 2 == 0 ? -3.6f : 3.6f, H + .45f, -.6f), 1.2f);
            Graffiti(mk, f.P((i % 2 == 0 ? 1f : -1f) * (W / 2f + .03f), 2.6f, 0), f.Right * (i % 2 == 0 ? 1f : -1f), 6f, 2.6f, i + 2);
            Mark(f.o, 10.5f);
        }

        /// A water tower built like a pagoda: three wooden tanks one on another, each under its own wide roof.
        void PagodaTank(MeshKit mk, Vector3 at, float s)
        {
            var up = Vector3.up; var roof = Paint(new Color(.2f, .42f, .34f), Surface.Metal); var y = 0f;
            for (var k = 0; k < 3; k++)
            {
                var r = (1.5f - k * .38f) * s; var h = (1.5f - k * .2f) * s; mk.Prism(woodM, at + up * y, up, r, h, 10); mk.Prism(iron, at + up * (y + h * .3f), up, r * 1.02f, .06f, 10); mk.Prism(iron, at + up * (y + h * .7f), up, r * 1.02f, .06f, 10);
                mk.Cone(roof, at + up * (y + h), r * 1.75f, .7f * s, 8); y += h + .34f * s;
            }
            mk.Prism(goldM, at + up * (y + .2f * s), up, .05f * s, .9f * s, 5);
        }

        /// Junks in the harbour, sails patched in every colour a tag comes in.
        void Junks(LandContext ctx, MeshKit mk)
        {
            var up = Vector3.up; var beach = new Vector3(center.x + D.x * (roadL + 14f), 0, center.z + D.z * (roadL + 14f)); var hull = Paint(new Color(.3f, .2f, .16f), Surface.Wood); var deck = Paint(new Color(.5f, .38f, .26f), Surface.Wood); var n = 0;
            foreach (var (d, s, yaw) in new[] { (58f, -52f, 30f), (70f, 34f, -50f), (96f, -8f, 80f), (112f, 70f, 10f), (84f, -96f, -20f) })
            {
                var p = beach + D * d + S * s; if (ctx.Ground(p.x, p.z) > waterY - .6f) continue;   // (only out on the water)
                var rot = Quaternion.LookRotation(Quaternion.Euler(0, yaw, 0) * S); Vector3 J(float x, float y, float z) => new Vector3(p.x, waterY, p.z) + rot * new Vector3(x, y, z);
                mk.Box(hull, J(0, .5f, 0), new Vector3(3.4f, 1.6f, 10f), rot); mk.Box(hull, J(0, 1.7f, -4f), new Vector3(3.6f, 1.6f, 3f), rot * Quaternion.Euler(-8f, 0, 0)); mk.Box(hull, J(0, 1.3f, 5.2f), new Vector3(2.4f, 1f, 1.6f), rot * Quaternion.Euler(14f, 0, 0)); mk.Box(deck, J(0, 1.32f, .6f), new Vector3(3.1f, .06f, 6.4f), rot);
                foreach (var (mz, mh, sw) in new[] { (1.6f, 9f, 5.2f), (-2.6f, 6.4f, 3.6f) })
                {
                    mk.Prism(deck, J(0, 1.3f, mz), up, .14f, mh, 6);
                    for (var b = 0; b < 5; b++) { var y = 2.6f + b * (mh - 2.2f) / 5f; var patch = (b + n) % 3 == 0 ? Tags[(b + n * 2) % Tags.Length] * .85f : (b + n) % 3 == 1 ? new Color(.74f, .4f, .26f) : new Color(.86f, .78f, .6f); mk.Box(Paint(patch, Surface.Cloth), J(.12f, y + (mh - 2.2f) / 10f, mz + .2f), new Vector3(.06f, (mh - 2.2f) / 5f - .06f, sw * (.8f + b * .05f)), rot * Quaternion.Euler(0, 12f, 0)); mk.Box(deck, J(.14f, y, mz + .2f), new Vector3(.1f, .08f, sw * (.84f + b * .05f)), rot * Quaternion.Euler(0, 12f, 0)); }
                }
                mk.Blob(bulbM, J(0, 2.9f, -4.6f), new Vector3(.14f, .2f, .14f), prng, 0f); n++;
            }
            if (n > 0) Shot(ctx, "junks in the harbour", new Vector3(beach.x, waterY + 2.4f, beach.z) - D * 6f, new Vector3(beach.x, waterY + 4f, beach.z) + D * 80f);
            Debug.Log($"Boom Bap Harbour: {n} junks in the harbour");
        }
    }
}
