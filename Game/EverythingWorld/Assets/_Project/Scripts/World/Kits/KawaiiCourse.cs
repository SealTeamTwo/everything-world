using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Kawaii Cascades' places (part of KawaiiKit), all built to be crossed at speed:
    /// - the Sugar Bowl, where you arrive: a round floor of sprinkles ringed with giant lollipops, a big jelly in the
    ///   middle to bounce on, and signs that say how to move here;
    /// - the Parfait: a sundae glass 46 m tall in bands of colour, a spiral of macarons round it to climb, a soda geyser
    ///   at its foot that fires you to the top in one go, whipped cream and a cherry on top (the cherry is the biggest
    ///   bounce in the land), and the four rails starting from its rim;
    /// - the four Runs: a road of boost strips from the summit down the tiers to the shore, the rail from the Parfait
    ///   overhead on candy-cane posts, a gate with the Run's name at the far end, and a return geyser that fires you
    ///   back up toward the summit;
    /// - everywhere else: jelly pads, geysers under floating donuts, hops of floating macarons with a star at the top,
    ///   donut arches, cupcakes, lollipop trees, gummy bears, falls of strawberry milk down the risers, benches.
    public partial class KawaiiKit
    {
        KawaiiTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;
        const float ParfaitH = 46f;

        static readonly Color[] Tops = { Pink, Yellow, Soda, Mint, Lilac, Cream };
        static readonly Color[] Bottoms = { new Color(.2f, .24f, .6f), new Color(.9f, .5f, .7f), new Color(.95f, .95f, 1f) };
        static readonly string[] TownTalk =
        {
            "Can't stop! Won't stop! ... Okay, stopped. Hi!", "Space twice. Trust me. Space TWICE.", "Jump at a wall and jump again. You kick right off it!",
            "The rails start at the top of the Parfait. Touch one and hold on.", "The cherry on the Parfait is the biggest bounce there is.", "Everything here is edible. Probably. Don't check.",
            "I ran all four Runs before breakfast. Then I had breakfast four times.", "You slow down when you leave. Everyone does. It's sad.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(6602);
            tlife = new GameObject("Kawaii town life").AddComponent<KawaiiTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Cap, Residents.Hat.Bucket }, new[] { Pink, Yellow, Soda }); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Local(), pose, prng, lines ?? TownTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Sign(Vector3 at, Vector3 face, string text, float size, Color c) => ArtistBits.Label(rootT, text, at, face, size, c, TextAnchor.MiddleCenter);

        /// A signboard on two sticks.
        void Board(MeshKit mk, Vector3 g, Vector3 face, string text, Color back, float w = 4.2f, float h = 1.3f, float size = .2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var lo = Lowest(g, w / 2f) - .1f;
            foreach (var e in new[] { -1f, 1f }) { var lp = g + side * e * (w / 2f - .2f); mk.Prism(stick, new Vector3(lp.x, lo, lp.z), up, .08f, g.y + 2.9f - lo, 6); }
            mk.Box(Paint(back), g + up * 2.3f, new Vector3(w, h, .12f), rot); mk.Box(white, g + up * 2.3f - face * .02f, new Vector3(w + .25f, h + .25f, .1f), rot);
            Col(g + up * 1.5f, new Vector3(w, 3f, .2f), rot); Sign(g + up * 2.3f + face * .08f, face, text, size, Ink);
        }

        void Bench(MeshKit mk, Vector3 g, Vector3 face, int k)
        {
            // a wafer bench: two biscuit slabs on short legs
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); const float len = 2.2f;
            mk.Box(Paint(new Color(.95f, .78f, .5f)), g + up * .44f, new Vector3(len, .14f, .55f), rot); mk.Box(SweetM(k), g + up * .52f, new Vector3(len * .96f, .04f, .5f), rot);
            foreach (var e in new[] { -1f, 1f }) { var lp = g + side * e * (len / 2f - .25f); var lo = Mathf.Min(g.y, Lowest(lp, .3f)) - .1f; mk.Box(Paint(new Color(.85f, .62f, .38f)), new Vector3(lp.x, (lo + g.y + .4f) / 2f, lp.z), new Vector3(.3f, g.y + .4f - lo, .5f), rot); }
            Col(g + up * .27f, new Vector3(len, .54f, .55f), rot); Interact.Bench(tlife.transform, g + up * .54f - side * (len / 2f - .5f), g + up * .54f + side * (len / 2f - .5f), face);
        }

        // ---------------------------------------------------------------- the Sugar Bowl (where you arrive)
        void Plaza(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(11);
            mk.Prism(berryD, c - up * .4f, up, 20f, .5f, 40); mk.Prism(white, c + up * .1f, up, 19.4f, .02f, 40); mk.Prism(berry, c + up * .11f, up, 18.8f, .02f, 40);
            // sprinkles set into the floor
            for (var k = 0; k < 150; k++) { var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = 5f + (float)rng.NextDouble() * 13.4f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; mk.Box(SweetM(k), p + up * .135f, new Vector3(.55f, .02f, .16f), Quaternion.Euler(0, (float)rng.NextDouble() * 180f, 0)); }
            for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 17f, 0);
            // a ring of giant lollipops
            for (var k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * D; var g = c + d * 17.6f; var h = 6f + (k % 2) * 1.6f;
                mk.Prism(stick, g, up, .16f, h, 8); var top = g + up * (h + 1.3f); var rot = Quaternion.LookRotation(d);
                mk.Prism(SweetM(k), top - d * .25f, d, 1.5f, .5f, 18); for (var j = 0; j < 3; j++) mk.Prism(j % 2 == 0 ? white : SweetM(k + 1), top + d * (.26f + j * .01f), d, 1.2f - j * .4f, .02f, 16);
                mk.Prism(lampM, top - d * .26f, -d, 1.6f, .04f, 18); halos.Add(top, 3.2f, Sweet[k % 6], .35f); ArtistBits.PostCollider(ctx.Root, g, h, .3f);
                if (k % 2 == 0) ArtistBits.PointLight(ctx.Root, top + up * .5f - d * 2f, new Color(1f, .9f, .7f), 26f, 1.5f);
            }
            // the big jelly in the middle
            Jelly(G(c + D * 8f) + up * .1f, 3.2f, up * 23f, Yellow);   // (beside where you arrive, not under your feet)
            // how to move here
            Board(mk, G(c + D * -12f + S * 6f), D, "YOU ARE HYPER HERE", Yellow, 6.4f, 1.4f, .42f);
            Board(mk, G(c + D * -12f - S * 6f), D, "SHIFT: SPRINT   SPACE x2: DOUBLE JUMP", Pink, 9f, 1.3f, .3f);
            Board(mk, G(c + D * -15f), D, "JUMP AT A WALL TO KICK OFF IT", Soda, 7.4f, 1.3f, .3f);
            Board(mk, G(c + D * 14f + S * 8f), -D, "THE PARFAIT: CLIMB IT, THEN RIDE A RAIL", Mint, 9.4f, 1.3f, .3f);
            foreach (var e in new[] { -1f, 1f }) Bench(mk, G(c + S * e * 13f + D * -4f), -S * e, e > 0 ? 1 : 2);
            Mark(c, 21f);
            Shot(ctx, "The Sugar Bowl", c - D * 22f + up * 6f, D - up * .1f);
        }

        // ---------------------------------------------------------------- the Parfait
        void Parfait(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(parfait2.x, parfait2.y); var rng = new System.Random(12);
            life.parfaitTop = c + up * (ParfaitH + 1f);
            float Rad(float y) => 5.5f + 5.5f * Mathf.Pow(Mathf.Clamp01(y / ParfaitH), .7f);    // the glass widens as it rises
            // the foot and the stem, then the bands of the sundae
            mk.Prism(white, c - up * .3f, up, 9f, .8f, 28, .8f); solid.Prism(white, c - up * .3f, up, 9f, .8f, 14, .8f);
            const int bands = 12; var bh = ParfaitH / bands;
            for (var k = 0; k < bands; k++)
            {
                float y0 = k * bh, r0 = Rad(y0), r1 = Rad(y0 + bh); var m = k % 4 == 0 ? pinkM : k % 4 == 1 ? cream : k % 4 == 2 ? yellowM : sodaM;
                mk.Prism(m, c + up * (y0 + .5f), up, r0, bh, 28, r1 / r0); solid.Prism(m, c + up * (y0 + .5f), up, r0, bh, 14, r1 / r0);
                if (k % 2 == 1) mk.Prism(white, c + up * (y0 + .5f + bh - .12f), up, r1 + .12f, .14f, 28);
            }
            // the rim, the whipped cream and the cherry
            var top = c + up * (ParfaitH + .5f); var R = Rad(ParfaitH);
            mk.Prism(glass, top - up * .3f, up, R + 1.6f, .5f, 32); solid.Prism(glass, top - up * .3f, up, R + 1.6f, .5f, 16);
            mk.Prism(cream, top + up * .2f, up, R + .4f, .5f, 28, .9f);
            for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51.4f, 0) * D; mk.Blob(white, top + d * (R * .55f) + up * 1.1f, new Vector3(2.6f, 1.5f, 2.6f), rng, .2f); ArtistBits.PostCollider(ctx.Root, top + d * (R * .55f) + up * .6f, 2.2f, 1.6f); }
            mk.Blob(white, top + up * 1.6f, new Vector3(3.6f, 2f, 3.6f), rng, .18f); solid.Prism(white, top + up * .6f, up, 3.4f, 2.4f, 10, .7f);
            var ch = top + up * 3.1f; var cm = new MeshKit(); cm.Blob(cherryM, up * 1.2f, new Vector3(1.5f, 1.4f, 1.5f), rng, .08f); cm.Prism(Paint(new Color(.3f, .5f, .2f)), up * 2.4f, (up + D * .4f).normalized, .06f, 1.6f, 5);
            var cg = cm.BuildSingle("The cherry", life.transform, false); cg.transform.position = ch;
            course.AddPad(HyperCourse.Kind.Bounce, ch + up * 1.4f, 2.2f, up * 34f, cg.transform); halos.Add(ch + up * 1.2f, 4f, Cherry, .4f);
            ArtistBits.PointLight(ctx.Root, top + up * 6f, new Color(1f, .8f, .85f), 40f, 1.6f);
            // the spiral of macarons round the glass
            var steps = Mathf.RoundToInt(ParfaitH / 2.3f); Vector3 last = c;
            for (var k = 0; k <= steps; k++)
            {
                var y = 2.3f + k * (ParfaitH - 2f) / steps; var d = Quaternion.Euler(0, k * 27f, 0) * -D; var p = c + d * (Rad(y) + 3.2f) + up * y;
                Macaron(mk, solid, p, 2.1f, k, .8f); last = p;
                if (k % 5 == 2) { mk.Prism(lampM, p + d * 1.9f + up * .2f, up, .16f, .9f, 6, .6f); halos.Add(p + d * 1.9f + up * .8f, 1.6f, Yellow, .4f); }
            }
            // a geyser at the foot fires you straight to the rim
            var gy = c - D * 14f; gy = ctx.OnGround(gy.x, gy.z);
            Jelly(gy, 2.2f, up * Mathf.Sqrt(2f * 38f * (ParfaitH + 8f)) + D * 5.5f, Soda, HyperCourse.Kind.Geyser);
            Board(mk, G(gy - D * 5f + S * 4f), -D, "GEYSER: STRAIGHT TO THE TOP", Soda, 7f, 1.3f, .3f);
            Board(mk, G(c - D * 12f - S * 8f), -D, "OR CLIMB THE MACARONS", Pink, 5.8f, 1.3f, .3f);
            Sign(top + up * 1f - D * (R + 1.7f), -D, "THE PARFAIT", .5f, Ink);
            for (var k = 0; k < 16; k++) Remember(c + Quaternion.Euler(0, k * 22.5f, 0) * D * 19f, 1);
            // the road from the Sugar Bowl
            var pts = new List<Vector3>(); for (var d = 18f; d <= 44f; d += 2f) pts.Add(G(center + D * d)); Strip(mk, pts, cream, RoadW, .1f, ctx); foreach (var p in pts) Remember(p, 1);
            Boost(mk, G(center + D * 30f), D, Yellow);
            Mark(c, 21f);
            Shot(ctx, "The Parfait", c - D * 46f + S * 20f + up * 16f, D * 46f - S * 20f + up * 8f);
            Shot(ctx, "From the top of the Parfait", top + up * 7f - D * 4f, -D - up * .35f);
        }

        // ---------------------------------------------------------------- the four Runs
        void Runs(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var topC = ctx.OnGround(parfait2.x, parfait2.y) + up * (ParfaitH + .9f); var rimR = 5.5f + 5.5f + 1.2f;
            for (var r = 0; r < 4; r++)
            {
                var dir = new Vector3(runDir[r].x, 0, runDir[r].y); var side = Vector3.Cross(up, dir); var len = runLen[r]; var col = RunColor[r]; var mat = r == 0 ? pinkM : r == 1 ? yellowM : r == 2 ? sodaM : mintM;
                // the road: a ribbon of the Run's colour with a boost strip every so often
                var pts = new List<Vector3>(); for (var d = 19f; d <= len; d += 2f) pts.Add(G(center + dir * d));
                Strip(mk, pts, mat, RoadW, .08f + r * .008f, ctx); foreach (var p in pts) Remember(p, 2 + r);
                for (var d = 34f; d < len - 10f; d += 36f) { var g = G(center + dir * d); if (Drop(g, 3f) < 2.2f) Boost(mk, g, dir, col); }
                for (var d = 28f; d < len; d += 24f) { var e = (Mathf.RoundToInt(d / 24f) % 2 == 0) ? 1f : -1f; var lp = G(center + dir * d + side * e * (RoadW / 2f + 1f)); if (!InFootprint(ctx, lp.x, lp.z, 1f)) Lamp(ctx, mk, lp, Mathf.RoundToInt(d / 24f) % 2 == 0, r); }
                // the gate at the far end, and the return geyser beyond it
                var end = G(center + dir * len); var gate = G(center + dir * (len - 6f));
                foreach (var e in new[] { -1f, 1f }) { var gp = G(gate + side * e * 4.6f); var lo = Lowest(gp, .5f) - .2f; for (var k = 0; k < 7; k++) mk.Prism(k % 2 == 0 ? white : mat, new Vector3(gp.x, lo + k * 1f, gp.z), up, .34f, 1f, 8); ArtistBits.PostCollider(ctx.Root, gp, 7f, .4f); }
                mk.Box(Paint(col), gate + up * 7.2f, new Vector3(10.4f, 1.5f, .3f), Quaternion.LookRotation(dir)); mk.Box(white, gate + up * 7.2f, new Vector3(10.8f, 1.9f, .2f), Quaternion.LookRotation(dir));
                Sign(gate + up * 7.2f - dir * .2f, -dir, RunName[r], .5f, Ink); Sign(gate + up * 7.2f + dir * .2f, dir, RunName[r], .5f, Ink);
                var back = (new Vector3(center.x - end.x, 0, center.z - end.z)).normalized;
                Jelly(end + side * 5f, 2.2f, back * 30f + up * 34f, col, HyperCourse.Kind.Geyser);
                Board(mk, G(end + side * 9.5f - dir * 2f), -dir, "RETURN GEYSER", col, 4.6f, 1.3f, .34f);
                Bench(mk, G(end - side * 9f - dir * 12f), side, r);
                // the rail: from the Parfait's rim over the road to just before the gate, on candy-cane posts
                var start = topC + (dir * .9f + new Vector3(parfait2.x - center.x, 0, parfait2.y - center.z).normalized * -.1f).normalized * rimR; start.y = topC.y + .3f;
                var stop = G(center + dir * (len - 16f)) + up * 2.2f; var rail = new List<Vector3>(); const int n = 34;
                for (var i = 0; i <= n; i++)
                {
                    var t = i / (float)n; var p = Vector3.Lerp(start, stop, t); p.y = Mathf.Lerp(start.y, stop.y, Mathf.Pow(t, .75f)) + Mathf.Sin(t * Mathf.PI) * 6f;
                    p.y = Mathf.Max(p.y, ctx.Ground(p.x, p.z) + 3.4f); if (i == n) p.y = stop.y; rail.Add(p);
                }
                // smooth it once so the kinks where it meets the ground's clearance are soft
                for (var pass = 0; pass < 2; pass++) for (var i = 1; i < n; i++) { var p = rail[i]; p.y = Mathf.Max(p.y, (rail[i - 1].y + rail[i + 1].y) / 2f); rail[i] = p; }
                course.AddRail(rail, 27f);
                for (var i = 0; i < n; i++)
                {
                    Vector3 a = rail[i], b = rail[i + 1]; var seg = b - a; var rot = Quaternion.LookRotation(seg.normalized);
                    mk.Box(railM, (a + b) / 2f, new Vector3(.5f, .14f, seg.magnitude + .05f), rot); mk.Box(i % 2 == 0 ? white : mat, (a + b) / 2f - up * .12f, new Vector3(.22f, .16f, seg.magnitude + .05f), rot);
                    if (i % 4 == 2 && i > 3)
                    {
                        var g = ctx.OnGround(a.x, a.z); if (!ctx.IsLand(g.x, g.z, 1f) || a.y - g.y < 2f) continue; var lo = Lowest(g, .4f) - .2f; var hgt = a.y - .2f - lo; var bandsN = Mathf.Max(2, Mathf.RoundToInt(hgt / 1.2f));
                        for (var k = 0; k < bandsN; k++) mk.Prism(k % 2 == 0 ? white : cherryM, new Vector3(g.x, lo + k * hgt / bandsN, g.z), up, .2f, hgt / bandsN, 7);
                        ArtistBits.PostCollider(ctx.Root, g, Mathf.Min(hgt, 4f), .26f); Mark(g, .8f);
                    }
                }
                halos.Add(start, 2.4f, col, .35f);
                Shot(ctx, RunName[r], G(center + dir * (len * .45f) + side * 16f) + up * 9f, -side - up * .2f + dir * .3f);
            }
            Debug.Log($"The Kawaii Cascades: Runs of {runLen[0]:0}, {runLen[1]:0}, {runLen[2]:0} and {runLen[3]:0} m, {course.rails.Count} rails");
        }

        // ---------------------------------------------------------------- the rest of the cake
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pads = 0, geysers = 0, hops = 0, arches = 0, cakes = 0, trees = 0, bears = 0, falls = 0, benches = 0; var rng = new System.Random(6603); var up = Vector3.up;
            const float cell = 21f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble(); var k = rng.Next(600);
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(up, f); var drop = Drop(p, 5f);
                // strawberry milk pouring down a riser: where the ground falls away steeply
                if (drop > 4.5f)
                {
                    if (roll > .5f) continue;
                    var lowDir = Vector3.forward; var lowest = float.MaxValue; for (var a = 0; a < 12; a++) { var d = Quaternion.Euler(0, a * 30f, 0) * Vector3.forward; var h = ctx.Ground(p.x + d.x * 6f, p.z + d.z * 6f); if (h < lowest) { lowest = h; lowDir = d; } }
                    var foot = ctx.OnGround(p.x + lowDir.x * 6.5f, p.z + lowDir.z * 6.5f); var head = ctx.OnGround(p.x - lowDir.x * 3f, p.z - lowDir.z * 3f); if (head.y - foot.y < 4f || !ctx.IsLand(foot.x, foot.z, 2f)) continue;
                    var w = 3f + s * 3f; var sr = Quaternion.LookRotation(lowDir); var mid = (head + foot) / 2f; var span = head - foot;
                    mk.Box(milkM, mid + up * .25f, new Vector3(w, .12f, span.magnitude + 1f), Quaternion.LookRotation(span.normalized)); mk.Prism(Paint(new Color(1f, .88f, .92f)), foot - up * .3f, up, w * .9f, .42f, 14); mk.Prism(milkM, foot + up * .13f, up, w * .78f, .02f, 14);
                    life.milkFeet.Add(foot + up * .3f); halos.Add(foot + up * .5f, w, new Color(1f, .7f, .85f), .2f); Mark(foot, w); falls++; continue;
                }
                if (drop > 2.2f) continue;      // everything else wants level ground
                if (roll < .13f) { Jelly(p, 1.6f + s * .5f, up * (17f + s * 6f), Sweet[k % 4]); pads++; }
                else if (roll < .19f)
                {
                    // a geyser under a floating donut: it fires you up through the hole
                    var h = 16f + s * 10f; Jelly(p, 1.9f, up * Mathf.Sqrt(2f * 38f * (h + 6f)), Soda, HyperCourse.Kind.Geyser);
                    var dm = new MeshKit(); for (var a = 0; a < 14; a++) { var d = Quaternion.Euler(0, a * 25.7f, 0) * Vector3.forward; dm.Blob(a % 2 == 0 ? SweetM(k) : SweetM(k + 1), d * 4.2f, new Vector3(1.5f, 1.1f, 1.5f), rng, .12f); dm.Box(white, d * 4.2f + up * .9f, new Vector3(.5f, .06f, .14f), Quaternion.Euler(0, a * 60f, 0)); }
                    var dg = dm.BuildSingle("Floating donut", ctx.Root, false); dg.transform.position = p + up * h; life.floaters.Add((dg.transform, p + up * h, s * 6f)); halos.Add(p + up * h, 5f, Sweet[k % 6], .15f); geysers++;
                }
                else if (roll < .29f)
                {
                    // a hop of floating macarons, each higher than the last, a star over the top one
                    var n = 3 + rng.Next(3); var at = p; var turn = (s - .5f) * 50f; var d = f;
                    for (var i = 0; i < n; i++) { at += d * (4.6f + i * .3f); d = Quaternion.Euler(0, turn, 0) * d; var gy = ctx.Ground(at.x, at.z); if (!ctx.IsLand(at.x, at.z, 1f)) break; var c = new Vector3(at.x, Mathf.Max(gy + 1.6f, p.y + 2.2f + i * 2.1f), at.z); Macaron(mk, solid, c, 1.7f, k + i); if (i == n - 1) Star(c + up * 2.2f); }
                    Mark(p, 2f); hops++;
                }
                else if (roll < .37f)
                {
                    // a donut standing on edge, big enough to run through
                    var R = 3.4f + s * 1.2f; var c = p + up * (R + .2f);
                    for (var a = 0; a < 16; a++) { var ang = a * 22.5f * Mathf.Deg2Rad; var q = c + side * Mathf.Cos(ang) * R + up * Mathf.Sin(ang) * R; mk.Blob(a % 4 == 3 ? white : SweetM(k), q, new Vector3(.95f, .95f, .8f), rng, .1f); }
                    // solid where the ring comes down to the ground on each side; open through the middle
                    foreach (var e in new[] { -1f, 1f }) for (var a = 0; a < 3; a++) { var off = R * (1f - a * .16f); ArtistBits.PostCollider(ctx.Root, p + side * e * off, R * (.55f + a * .5f), .75f); }
                    Mark(p, R + 1f); arches++;
                }
                else if (roll < .47f)
                {
                    // a cupcake as big as a shed: a pleated case, frosting, a sweet on top (you can stand on it)
                    var R = 2f + s * 1.4f; var h = R * 1.1f; var lo = Lowest(p, R) - .2f;
                    mk.Prism(SweetM(k), new Vector3(p.x, lo, p.z), up, R * .8f, p.y + h - lo, 12, 1.25f); solid.Prism(SweetM(k), new Vector3(p.x, lo, p.z), up, R * .9f, p.y + h - lo, 10, 1.1f);
                    mk.Blob(k % 2 == 0 ? white : pinkM, p + up * (h + R * .35f), new Vector3(R * 1.15f, R * .5f, R * 1.15f), rng, .16f); mk.Blob(white, p + up * (h + R * .8f), new Vector3(R * .7f, R * .4f, R * .7f), rng, .16f); solid.Prism(white, p + up * h, up, R * 1.05f, R * .9f, 10, .5f);
                    mk.Blob(cherryM, p + up * (h + R * 1.25f), Vector3.one * R * .28f, rng, .1f); Mark(p, R * 1.3f); cakes++;
                }
                else if (roll < .66f)
                {
                    // a lollipop tree: a tall stick, a swirled disc, or three smaller ones
                    var h = 4f + s * 4f; var lo = Lowest(p, .3f) - .2f; mk.Prism(stick, new Vector3(p.x, lo, p.z), up, .14f, p.y + h - lo, 7);
                    var n = k % 3 == 0 ? 3 : 1; for (var i = 0; i < n; i++) { var c = p + up * (h + (n == 1 ? 1.2f : .4f + i * .9f)) + (n == 1 ? Vector3.zero : Quaternion.Euler(0, i * 120f, 0) * f * .9f); var R = n == 1 ? 1.5f + s * .5f : .8f; mk.Prism(SweetM(k + i), c - f * .2f, f, R, .4f, 16); mk.Prism(white, c + f * .21f, f, R * .7f, .02f, 14); mk.Prism(SweetM(k + i + 2), c + f * .22f, f, R * .4f, .02f, 12); mk.Prism(white, c - f * .21f, -f, R * .7f, .02f, 14); }
                    ArtistBits.PostCollider(ctx.Root, p, Mathf.Min(h, 4f), .24f); Mark(p, 1f); trees++;
                }
                else if (roll < .72f)
                {
                    // a gummy bear, sitting
                    var m = GlowM(Sweet[k % 4], .7f); var sc = 1.2f + s * 1.2f;
                    mk.Blob(m, p + up * sc * .8f, new Vector3(.9f, .9f, .75f) * sc, rng, .08f); mk.Blob(m, p + up * sc * 1.9f, new Vector3(.62f, .58f, .56f) * sc, rng, .08f);
                    foreach (var e in new[] { -1f, 1f }) { mk.Blob(m, p + up * sc * 2.45f + side * e * sc * .45f, Vector3.one * sc * .22f, rng, .05f); mk.Blob(m, p + up * sc * 1.1f + side * e * sc * .85f + f * sc * .2f, new Vector3(.3f, .3f, .4f) * sc, rng, .05f); mk.Blob(m, p + up * sc * .3f + side * e * sc * .55f + f * sc * .7f, new Vector3(.34f, .3f, .5f) * sc, rng, .05f); mk.Box(Paint(Ink), p + up * sc * 2f + side * e * sc * .22f + f * sc * .5f, Vector3.one * sc * .1f, rot); }
                    ArtistBits.PostCollider(ctx.Root, p, sc * 2.4f, sc * .8f); Mark(p, sc * 1.2f); bears++;
                }
                else if (roll < .78f) { Bench(mk, p, f, k); Lamp(ctx, mk, G(p - f * .9f + side * 1.8f), benches % 2 == 0, k); Mark(p, 2.4f); benches++; }
            }
            Debug.Log($"The Kawaii Cascades: {pads} jelly pads, {geysers} geysers under donuts, {hops} macaron hops, {arches} donut arches, {cakes} cupcakes, {trees} lollipop trees, {bears} gummy bears, {falls} milk falls, {benches} benches");
        }

        /// A star to jump for at the top of a hop: it spins, and collecting it sets off a burst (KawaiiLife).
        void Star(Vector3 at)
        {
            var m = new MeshKit(); var up = Vector3.up;
            for (var k = 0; k < 5; k++) { var a = Quaternion.Euler(0, 0, k * 72f) * up; var b = Quaternion.Euler(0, 0, k * 72f + 36f) * up * .42f; var c = Quaternion.Euler(0, 0, k * 72f - 36f) * up * .42f; foreach (var e in new[] { -1f, 1f }) m.Tri(starM, a * .9f, b, c + Vector3.forward * .001f * e, Vector3.forward * -e); }
            m.Prism(starM, -Vector3.forward * .08f, Vector3.forward, .42f, .16f, 10);
            var go = m.BuildSingle("Star", life.transform, false); go.transform.position = at; life.starsToGet.Add((go.transform, at)); halos.Add(at, 1.4f, Yellow, .5f);
        }

        // ---------------------------------------------------------------- the people
        void People(LandContext ctx)
        {
            var c = ctx.OnGround(center.x, center.z);
            Folk(G(c + D * -9f + S * 3f), D, Resident.Pose.Wave, new[] { "Welcome to the Cascades! You're fast now. Did you notice? You're SO fast.", "Shift to sprint. Space, then Space again in the air.", "Bounce on the big jelly. Go on." });
            Folk(G(c + S * 12f + D * 4f), -S, Resident.Pose.Stand, new[] { "Four Runs, four rails. Strawberry, Lemon, Soda, Mint.", "The geyser at the end of each Run fires you home.", "If you fall off a rail, just jump back on. It doesn't mind." });
            var pc = ctx.OnGround(parfait2.x, parfait2.y);
            Folk(G(pc - D * 17f - S * 5f), -D, Resident.Pose.Stand, new[] { "I've been meaning to climb it. For three years.", "Geyser up, cherry bounce, rail down. That's the loop.", "Stand on the cherry. You'll see the whole cake." });
            // runners doing laps of the Sugar Bowl and up and down the Runs
            var ring = new Vector3[10]; for (var k = 0; k < 10; k++) ring[k] = G(c + Quaternion.Euler(0, k * 36f, 0) * D * 22.5f);
            Folk(ring[0], S, Resident.Pose.Walk, new[] { "Lap four hundred and six!", "Can't talk, running!" }, null, ring);
            for (var r = 0; r < 4; r += 2) { var dir = new Vector3(runDir[r].x, 0, runDir[r].y); var side = Vector3.Cross(Vector3.up, dir); var route = new List<Vector3>(); for (var d = 26f; d < runLen[r] - 12f; d += 16f) route.Add(G(c + dir * d + side * 4.4f)); if (route.Count > 2) Folk(route[0], dir, Resident.Pose.Walk, new[] { "Down by rail, back by geyser. All day.", "This Run is the best one. I say that on every Run." }, null, route.ToArray()); }
        }
    }

    /// Holds the Kawaii Cascades' residents and the things you can use.
    public class KawaiiTownLife : MonoBehaviour { }
}
