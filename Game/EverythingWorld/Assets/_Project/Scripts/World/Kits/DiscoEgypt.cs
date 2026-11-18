using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Discotopia crossed with ancient Egypt (part of DiscoKit; the owner's idea). Everything disco the land had is
    /// kept, and it is built as Egypt:
    /// - the Floor is the temple court: lotus columns round it, golden sarcophagi standing either side of the booth;
    /// - a processional way runs from the Floor to the Pyramid, faced in mirror tiles, its edges strung with the
    ///   chasing lights, a mirror ball held over its point between two horns like a sun disc;
    /// - along the way: obelisks that are columns of light, walls of figures caught in dance poses, a sphinx in
    ///   sunglasses, and a river of glitter for the Nile;
    /// - inside the Pyramid, the tomb: a dance floor, and a beam of light to be sent from mirror to mirror onto the
    ///   ball in the middle (the tomb puzzle and the mirror ball are the same trick). Send it home and the whole
    ///   pyramid lights and throws a beam into the sky (DiscoTomb).
    public partial class DiscoKit
    {
        Vector2 pyr2; bool hasPyramid; DiscoTomb tomb;
        const float PyrHalf = 20f, PyrH = 27f;

        /// Where the Pyramid can stand: near, clear of every artist and of the town, with a straight way to it from the
        /// Floor that crosses nobody's place. (Called at the end of ShapeGround, once the artists have their places.)
        void SitePyramid(IslandWorldData data, int index, System.Func<Vector2, float, bool> inside)
        {
            var isl = data.layout.islands[index]; hasPyramid = false; var best = float.MaxValue;
            for (var ang = 0f; ang < 360f; ang += 8f) for (var dist = 105f; dist <= 236f; dist += 12f)
            {
                var d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)); var p = heart2 + d * dist; if (!inside(p, PyrHalf + 22f)) continue;
                if (Vector2.Distance(p, light2) < 70f || Vector2.Distance(p, belv2) < 66f) continue;
                var al = Vector2.Dot(p - heart2, dir2); var off = Mathf.Abs(Vector2.Dot(p - heart2, side2)); if (al > -10f && al < lightDist + 20f && off < 46f) continue;   // not across the Esplanade
                var tb = Mathf.Clamp(Vector2.Dot(belv2 - heart2, d), 30f, dist); if ((heart2 + d * tb - belv2).magnitude < 36f) continue;                                   // nor the way through the Belvedere
                var ok = true;
                foreach (var a in data.layout.artists)
                {
                    if (a.sceneId != isl.sceneId) continue; var ap = new Vector2(a.x, a.z);
                    if (Vector2.Distance(ap, p) < a.r + PyrHalf + 16f) { ok = false; break; }
                    var t = Mathf.Clamp(Vector2.Dot(ap - heart2, d), 30f, dist); if ((heart2 + d * t - ap).magnitude < a.r + 17f) { ok = false; break; }   // (room for the way and the river beside it)
                }
                if (!ok) continue;
                var score = dist + (1f + Vector2.Dot(d, dir2)) * 40f;   // nearer is better; inland, away from the lighthouse, is better
                if (score < best) { best = score; pyr2 = p; hasPyramid = true; }
            }
            if (hasPyramid) Level(data, index, pyr2, PyrHalf + 10f, PyrHalf + 26f);
        }

        void Egypt(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(1352);
            // ---- the Floor as the temple court: lotus columns between the lamps, a sarcophagus either side of the booth
            for (var k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * toLight; var g = c + d * 19f + up * .13f;   // (inside the ring of benches, outside the arches' feet)
                LotusColumn(ctx, mk, g, 7.5f, k);
            }
            var bo = new PlaceFrame(c - sideways * (FloorHalf + 3.2f) + up * .13f, sideways); foreach (var e in new[] { -1f, 1f }) Sarcophagus(ctx, mk, bo.P(e * 8.6f, 0, -1.2f), bo.Fwd);
            if (!hasPyramid) { Debug.Log("Discotopia: no room was found for the Pyramid"); return; }

            var pg = ctx.OnGround(pyr2.x, pyr2.y); var toPyr = FaceTo(c, pg); var side = Vector3.Cross(up, toPyr); var dist = Vector3.Distance(new Vector3(c.x, 0, c.z), new Vector3(pg.x, 0, pg.z)); var wayEnd = dist - PyrHalf - 4f;
            Vector3 On(float d, float lat) => ctx.OnGround(c.x + toPyr.x * d + side.x * lat, c.z + toPyr.z * d + side.z * lat);
            // ---- the processional way
            var pts = new List<Vector3>(); for (var d = 29f; d <= wayEnd + .01f; d += 2f) pts.Add(On(d, 0));
            Strip(mk, pts, stone, 7f, .14f, ctx); foreach (var p in pts) Remember(p, 6);
            for (var d = 31f; d < wayEnd; d += 3f) foreach (var e in new[] { -1f, 1f }) mk.Blob(tileM[(int)(d / 3f) % 4], On(d, e * 3.3f) + up * .2f, new Vector3(.16f, .07f, .16f), prng, 0f);
            for (var d = 29f; d <= wayEnd; d += 10f) Mark(On(d, 0), 6f);
            // obelisks at its two ends; walls of dancers and torches between
            foreach (var e in new[] { -1f, 1f }) { Obelisk(ctx, mk, On(34f, e * 6.2f), 13f); Obelisk(ctx, mk, On(wayEnd - 5f, e * 7.5f), 16f); }
            var walls = 0;
            for (var d = 42f; d < wayEnd - 15f; d += 11f, walls++)
            {
                var e = walls % 2 == 0 ? 1f : -1f; DanceWall(ctx, mk, On(d, e * 6.4f), -side * e, walls);
                Torch(ctx, mk, On(d + 5.5f, -e * 5.2f));
            }
            // the sphinx, near the Pyramid, looking back down the way at whoever is coming
            Sphinx(ctx, mk, On(wayEnd - 20f, 13f), -toPyr);
            // ---- the Nile: a river of glitter down the other side of the way, from a pool by the Floor to the Pyramid
            var nile = WorldMaterials.Water("Disco Nile", new Color(1f, .92f, .62f), new Color(.5f, .66f, 1f), 1.2f, 0f, 1f); var np = new List<Vector3>();
            for (var d = 44f; d <= wayEnd - 2f; d += 2f) np.Add(On(d, -12.5f + 3f * Mathf.Sin(d / 10f)));
            if (np.Count > 2)
            {
                Strip(mk, np, nile, 4.6f, .1f, ctx); mk.Prism(nile, np[0] - up * .1f, up, 5.5f, .2f, 16); Mark(np[0], 6f);
                for (var i = 0; i < np.Count; i += 3) { Mark(np[i], 3.2f); if (i % 12 == 6) { var bank = np[i] + side * ((i / 12) % 2 == 0 ? -3.4f : 3.4f); bank = ctx.OnGround(bank.x, bank.z); Reeds(mk, bank, rng); } }
                halos.Add(np[0] + up * .6f, 6f, Gold, .25f);
            }

            // ---- the Pyramid
            var f = -toPyr;   // the face with the door looks back toward the Floor
            Vector3 L(float x, float y, float z) => pg + side * x + up * y + f * z;
            mk.Prism(stone, pg - up * .3f, up, PyrHalf * 1.62f, .42f, 4, 1f, Mathf.Atan2(side.x, side.z) * Mathf.Rad2Deg + 45f);   // a square plinth under it
            var capM = new Material(WorldMaterials.Glow(Gold, 1.5f)) { name = "The Pyramid's cap" }; const int Rows = 8; const float Hw = 1.9f;
            var apex = pg + up * PyrH; var faces = new[] { (f, side), (side, -f), (-f, -side), (-side, f) };
            for (var fi = 0; fi < 4; fi++)
            {
                var (n, t) = faces[fi];
                Vector3 P(float x, float a) => pg + n * (PyrHalf * (1f - a)) + t * x + up * (PyrH * a);
                for (var r = 0; r < Rows; r++)
                {
                    float a0 = r / (float)Rows, a1 = (r + 1) / (float)Rows; float w0 = PyrHalf * (1f - a0), w1 = PyrHalf * (1f - a1); var nc = Mathf.Max(1, Rows - r);
                    var cap = r >= Rows - 1;
                    for (var k = 0; k < nc; k++)
                    {
                        float u0 = k / (float)nc, u1 = (k + 1) / (float)nc; float xa0 = Mathf.Lerp(-w0, w0, u0), xb0 = Mathf.Lerp(-w0, w0, u1), xa1 = Mathf.Lerp(-w1, w1, u0), xb1 = Mathf.Lerp(-w1, w1, u1);
                        // the doorway: the middle of the bottom row of the front face is left open
                        if (fi == 0 && r == 0 && xb0 > -Hw && xa0 < Hw) { if (xa0 < -Hw) mk.Quad(mirror, P(xa0, a0), P(-Hw, a0), P(-Hw, a1), P(xa1, a1), pg + up * 5f); if (xb0 > Hw) mk.Quad(chrome, P(Hw, a0), P(xb0, a0), P(xb1, a1), P(Hw, a1), pg + up * 5f); continue; }
                        mk.Quad(cap ? capM : (r + k) % 2 == 0 ? mirror : chrome, P(xa0, a0), P(xb0, a0), P(xb1, a1), P(xa1, a1), pg + up * 5f);
                    }
                }
                // its edge, strung with the chasing lights
                var corner = pg + n * PyrHalf + t * PyrHalf; for (var k = 1; k < 26; k++) mk.Blob(tileM[k % 4], Vector3.Lerp(corner, apex, k / 26f) + (n + t).normalized * .12f, Vector3.one * .2f, prng, 0f);
                // what you can't walk through
                if (fi == 0) { solid.Tri(stone, P(-PyrHalf, 0), P(-Hw, 0), apex, pg + up * 5f); solid.Tri(stone, P(Hw, 0), P(PyrHalf, 0), apex, pg + up * 5f); solid.Tri(stone, P(-Hw, 1f / Rows), P(Hw, 1f / Rows), apex, pg + up * 5f); }
                else solid.Tri(stone, P(-PyrHalf, 0), P(PyrHalf, 0), apex, pg + up * 5f);
            }
            halos.Add(apex - up * 2f, 8f, Gold, .4f); Mark(pg, PyrHalf * 1.5f);
            // the sun disc: a mirror ball held over the point between two horns
            foreach (var e in new[] { -1f, 1f }) { var prev = apex + side * e * .8f - up * .6f; for (var k = 1; k <= 7; k++) { var a = k / 7f; var q = apex + side * e * (.8f + 3.1f * Mathf.Sin(a * Mathf.PI * .62f)) + up * (-.6f + a * 7.4f); mk.Prism(goldM, prev, q - prev, .3f - a * .16f, (q - prev).magnitude + .05f, 6); prev = q; } }
            var sun = new MeshKit(); var sunM = new Material(WorldMaterials.Glow(new Color(1f, .93f, .7f), 1.8f)) { name = "The sun disc" }; sun.Blob(sunM, Vector3.zero, Vector3.one * 2.5f, prng, .1f);
            var sgo = sun.BuildSingle("The sun disc", tlife.transform, false); sgo.transform.position = apex + up * 4.1f; halos.Add(apex + up * 4.1f, 11f, Gold, .5f);

            // ---- the way in: a gate of two leaning towers, a passage, and the tomb
            const float CH = 7f, CHt = 6.4f, PW = 3.8f, PH = 3.5f, T = .5f; var rot = Quaternion.LookRotation(f);
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(stoneWarm, L(e * 4.6f, -.2f, PyrHalf + 2.6f), up, 2.2f, 8.2f, 4, .7f, Mathf.Atan2(side.x, side.z) * Mathf.Rad2Deg + 45f); mk.Box(goldM, L(e * 4.6f, 8.1f, PyrHalf + 2.6f), new Vector3(2.5f, .3f, 2.5f), rot); Col(L(e * 4.6f, 4f, PyrHalf + 2.6f), new Vector3(3f, 8f, 3f), rot); }
            mk.Box(stoneWarm, L(0, 5.9f, PyrHalf + 2.6f), new Vector3(6.6f, 1.3f, 1.6f), rot); mk.Box(goldM, L(0, 6.62f, PyrHalf + 2.6f), new Vector3(7f, .16f, 1.9f), rot); Sign(L(0, 5.9f, PyrHalf + 3.42f), f, "THE HOUSE OF THE EVERLASTING GROOVE", .34f, Ink);
            void Slab(Vector3 at, Vector3 size, Material m) { mk.Box(m, at, size, rot); solid.Box(m, at, size, rot); }
            float pz0 = CH, pz1 = PyrHalf + 1.2f, pl = pz1 - pz0;
            Slab(L(0, -.09f, (pz0 + pz1) / 2f), new Vector3(PW + T * 2f, .5f, pl), navy);
            foreach (var e in new[] { -1f, 1f }) Slab(L(e * (PW / 2f + T / 2f), PH / 2f, (pz0 + pz1) / 2f), new Vector3(T, PH, pl), navy);
            Slab(L(0, PH + T / 2f, (pz0 + pz1) / 2f), new Vector3(PW + T * 2f, T, pl), navy);
            for (var z = pz0 + 1.5f; z < pz1; z += 3f) foreach (var e in new[] { -1f, 1f }) { mk.Blob(tileM[(int)z % 4], L(e * (PW / 2f - .08f), 2.6f, z), Vector3.one * .12f, prng, 0f); }
            // the tomb: four walls, a ceiling, a floor that lights up
            Slab(L(0, -.09f, 0), new Vector3(CH * 2f + T * 2f, .5f, CH * 2f + T * 2f), dark);   // (its top just above the plinth the Pyramid stands on)
            Slab(L(0, CHt / 2f, -CH - T / 2f), new Vector3(CH * 2f + T * 2f, CHt, T), navy); foreach (var e in new[] { -1f, 1f }) Slab(L(e * (CH + T / 2f), CHt / 2f, 0), new Vector3(T, CHt, CH * 2f), navy);
            foreach (var e in new[] { -1f, 1f }) Slab(L(e * (CH + PW / 2f + T) / 2f, CHt / 2f, CH + T / 2f), new Vector3(CH - PW / 2f - T, CHt, T), navy);
            Slab(L(0, (CHt + PH + T) / 2f, CH + T / 2f), new Vector3(PW + T * 2f, CHt - PH - T, T), navy); Slab(L(0, CHt + T / 2f, 0), new Vector3(CH * 2f + T * 2f, T, CH * 2f + T * 2f), navy);
            IndoorSpaces.Add(L(0, CHt / 2f, 0), rot, new Vector3(CH * 2f - .2f, CHt, CH * 2f - .2f)); IndoorSpaces.Add(L(0, PH / 2f, (pz0 + pz1) / 2f), rot, new Vector3(PW - .2f, PH, pl));
            for (var x = 0; x < 5; x++) for (var z = 0; z < 5; z++) mk.Box(tileM[(x * 3 + z * 5 + (x * z) % 3) % 4], L(-4f + x * 2f, .19f, -4f + z * 2f), new Vector3(1.86f, .04f, 1.86f), rot);
            // dancers on the tomb's walls, torches in its corners
            for (var k = 0; k < 3; k++) { Dancer(mk, tileM[k % 4], L(-4.4f + k * 4.4f, 3.3f, -CH + .04f), side, up, 1.5f, k, .05f); Dancer(mk, tileM[(k + 2) % 4], L(CH - .04f, 3.3f, -4.4f + k * 4.4f), f, up, 1.5f, k + 1, .05f); Dancer(mk, tileM[(k + 1) % 4], L(-CH + .04f, 3.3f, 4.4f - k * 4.4f), -f, up, 1.5f, k + 2, .05f); }
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f) }) { var tp = L(x * (CH - .9f), 0, z * (CH - .9f)); mk.Prism(goldM, tp, up, .16f, 1.5f, 6, 1.8f); mk.Blob(windowM, tp + up * 1.75f, new Vector3(.2f, .3f, .2f), prng, .2f); ArtistBits.PointLight(ctx.Root, tp + up * 2.2f, Gold, 11f, 1.3f, true); }

            // ---- the puzzle: the Eye sends a beam; four mirrors, each turned the wrong way; the ball in the middle waits for it
            tomb = new GameObject("The tomb").AddComponent<DiscoTomb>(); tomb.transform.SetParent(life.transform, false);
            const float BY = 1.55f; var eye = L(-5f, BY, 5f); mk.Prism(goldM, L(-5.9f, 0, 5f), up, .34f, 1.2f, 6); mk.Box(goldM, L(-5.9f, BY, 5f), new Vector3(.5f, .9f, .9f), rot); mk.Blob(sunM, L(-5.62f, BY, 5f), new Vector3(.12f, .26f, .26f), prng, 0f); halos.Add(eye, 1.4f, Gold, .5f);
            var ballAt = L(0, BY, 0); mk.Prism(goldM, L(0, 0, 0), up, .5f, BY - .95f, 8, .5f); var bm = new MeshKit(); var ballM = new Material(WorldMaterials.Glow(Silver, .5f)) { name = "The tomb's mirror ball" }; bm.Blob(ballM, Vector3.zero, Vector3.one * .9f, prng, .12f);
            var bgo = bm.BuildSingle("The tomb's mirror ball", tomb.transform, false); bgo.transform.position = ballAt; ArtistBits.PostCollider(ctx.Root, L(0, 0, 0), 2.4f, .8f);
            var stops = new[] { (5f, 5f), (5f, -5f), (-5f, -5f), (-5f, 0f) }; var prevAt = eye;
            for (var i = 0; i < 4; i++)
            {
                var at = L(stops[i].Item1, BY, stops[i].Item2); var next = i < 3 ? L(stops[i + 1].Item1, BY, stops[i + 1].Item2) : ballAt; var inD = (at - prevAt).normalized; var outD = (next - at).normalized;
                var foot = at - up * BY; mk.Prism(goldM, foot, up, .3f, .5f, 8); mk.Prism(chrome, foot, up, .06f, BY - .2f, 6); ArtistBits.PostCollider(ctx.Root, foot, BY, .34f);
                var pm = new MeshKit(); pm.Box(goldM, Vector3.zero, new Vector3(1.3f, 1.3f, .08f), Quaternion.identity); pm.Box(mirror, new Vector3(0, 0, .05f), new Vector3(1.1f, 1.1f, .03f), Quaternion.identity);
                var pgo = pm.BuildSingle("Tomb mirror " + (i + 1), tomb.transform, false); pgo.transform.position = at; var n = i;
                // a turned mirror throws the beam the other way, into the wall
                var miss = at - outD * 1.95f;
                tomb.mirrors.Add(new DiscoTomb.Mirror { plate = pgo.transform, at = at, inD = inD, outD = outD, miss = miss, good = false });
                Interact.Toy(pgo, Toy.Kind.Custom, "Turn the mirror", new Vector3(1.5f, 2.2f, 1.5f)).onUse = _ => tomb.Turn(n);
                prevAt = at;
            }
            tomb.eye = eye; tomb.ballAt = ballAt; tomb.ball = bgo.transform; tomb.ballM = ballM; tomb.capM = capM; tomb.sunM = sunM; tomb.sun = sgo.transform; tomb.apex = apex + up * 4.1f;
            tomb.ballLight = ArtistBits.PointLight(ctx.Root, ballAt + up * 2.4f, new Color(1f, .97f, .9f), 16f, .6f, true); tomb.sayAt = ballAt + up * 2.2f;
            Folk(L(2.2f, 0, PyrHalf + 5f), f, Resident.Pose.Wave, new[] { "Four thousand years they've been dancing in there. The floor still lights up.", "There's a beam and four mirrors and every one of them is turned wrong. Nobody's had the patience. They keep getting up to dance.", "Send the light to the ball and the old place wakes up. You'll see it from the lighthouse." });
            Shot(ctx, "the pyramid", On(wayEnd - 40f, -3f) + up * 2.4f, apex - up * 12f);
            Shot(ctx, "inside the tomb", L(4.5f, 2.2f, 5.8f), L(-2f, 1.6f, -3f));
            Shot(ctx, "the processional way", On(31f, 1.5f) + up * 2.2f, On(wayEnd, 0) + up * 9f);
            Shot(ctx, "the sphinx", On(wayEnd - 34f, 4f) + up * 2.2f, On(wayEnd - 20f, 13f) + up * 4f);
            Debug.Log($"Discotopia: the Pyramid stands {dist:0} m from the Floor, {walls} walls of dancers on the way");
        }

        /// A lotus column: a white shaft, a flared gold head, a band of the chasing lights.
        void LotusColumn(LandContext ctx, MeshKit mk, Vector3 g, float h, int k)
        {
            var up = Vector3.up; mk.Prism(marble, g - up * .2f, up, .8f, .5f, 10); mk.Prism(marble, g + up * .3f, up, .5f, h - .3f, 10, .82f); mk.Prism(goldM, g + up * h, up, .42f, .9f, 10, 2.1f); mk.Prism(marble, g + up * (h + .9f), up, .95f, .2f, 10);
            for (var q = 0; q < 6; q++) mk.Blob(tileM[(q + k) % 4], g + up * (h - .25f) + Quaternion.Euler(0, q * 60f, 0) * Vector3.forward * .46f, Vector3.one * .1f, prng, 0f);
            ArtistBits.PostCollider(ctx.Root, g, h, .6f); Fixture(g, 1f); Mark(g, 1.2f);
        }

        /// A golden sarcophagus standing on end: shoulders, a striped headdress, a face that is a small mirror ball.
        void Sarcophagus(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(marble, g + up * .15f, new Vector3(1.4f, .3f, 1.1f), rot);
            mk.Prism(goldM, g + up * .3f, up, .34f, 1.5f, 8, 1.75f); mk.Prism(goldM, g + up * 1.8f, up, .6f, .5f, 8, .8f); mk.Blob(chrome, g + up * 2.62f + face * .08f, new Vector3(.3f, .34f, .3f), prng, .08f);
            mk.Box(goldM, g + up * 2.7f - face * .1f, new Vector3(.86f, .8f, .4f), rot); for (var k = 0; k < 4; k++) mk.Box(navy, g + up * (2.42f + k * .18f) - face * .1f, new Vector3(.9f, .06f, .44f), rot);
            foreach (var e in new[] { -1f, 1f }) mk.Box(navy, g + up * 2f + face * .5f + rot * Vector3.right * e * .18f, new Vector3(.5f, .08f, .08f), rot * Quaternion.Euler(0, 0, e * 34f));
            mk.Blob(tileM[0], g + up * 1.6f + face * .6f, Vector3.one * .1f, prng, 0f); Col(g + up * 1.5f, new Vector3(1.3f, 3f, 1.1f), rot); Fixture(g, 1.1f);
        }

        /// An obelisk that is a column of light: a tall white needle, a gold point, a beam going up from it.
        void Obelisk(LandContext ctx, MeshKit mk, Vector3 g, float h)
        {
            var up = Vector3.up; var m = new Material(WorldMaterials.Glow(new Color(1f, .97f, .88f), .55f)) { name = "Obelisk" };
            mk.Prism(marble, g - up * .2f, up, 1.5f, .8f, 4, 1f, 45f); mk.Prism(m, g + up * .6f, up, .95f, h, 4, .62f, 45f); mk.Prism(windowM, g + up * (h + .6f), up, .59f, 1.3f, 4, .04f, 45f);
            for (var k = 0; k < 5; k++) mk.Box(goldM, g + up * (2f + k * (h - 3f) / 4f), new Vector3(1.36f - k * .1f, .08f, 1.36f - k * .1f), Quaternion.identity);
            ArtistBits.PostCollider(ctx.Root, g, h, 1f); Fixture(g, 1.6f); Mark(g, 2f); halos.Add(g + up * (h + 1.4f), 4.5f, Gold, .45f); ArtistBits.PointLight(ctx.Root, g + up * 3f + Vector3.forward * 1.6f, Gold, 12f, 1.2f);
            if (life != null) life.columns.Add(g + up * (h + 1.9f));
        }

        void Torch(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; mk.Prism(goldM, g - up * .2f, up, .14f, 2.2f, 6); mk.Prism(goldM, g + up * 2f, up, .2f, .4f, 8, 2.2f); mk.Blob(windowM, g + up * 2.6f, new Vector3(.26f, .4f, .26f), prng, .25f);
            ArtistBits.PostCollider(ctx.Root, g, 2.2f, .16f); Fixture(g, .5f); halos.Add(g + up * 2.6f, 1.8f, Amber, .5f);
        }

        void Reeds(MeshKit mk, Vector3 g, System.Random rng)
        {
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + (float)rng.NextDouble() * 40f, 0) * Vector3.forward * (.2f + (float)rng.NextDouble() * .5f); var h = 1.2f + (float)rng.NextDouble() * 1.3f; var lean = (Vector3.up + d * .25f).normalized; mk.Prism(canopy, g + d, lean, .03f, h, 3); mk.Blob(tileM[k % 4], g + d + lean * h, new Vector3(.1f, .16f, .1f), rng, 0f); }
        }

        /// A wall of pale stone with a figure on it caught in a dance pose, drawn in light.
        void DanceWall(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int k)
        {
            var up = Vector3.up; face.y = 0; face.Normalize(); var rot = Quaternion.LookRotation(face); var rt = rot * Vector3.right;
            mk.Box(stoneWarm, g + up * 1.9f, new Vector3(6f, 3.8f, .6f), rot); mk.Box(goldM, g + up * 3.86f, new Vector3(6.3f, .14f, .8f), rot); mk.Box(marble, g + up * .12f, new Vector3(6.4f, .3f, .9f), rot); Col(g + up * 1.9f, new Vector3(6f, 3.8f, .6f), rot);
            for (var q = 0; q < 2; q++) Dancer(mk, tileM[(k + q) % 4], g + up * 2f + face * .32f + rt * (q == 0 ? -1.5f : 1.5f), rt, up, 1.35f, k * 2 + q, .045f);
            Fixture(g, 3.2f); Mark(g, 3.4f);
        }

        /// A figure in a dance pose, in line, side on in the old way: 'c' the middle of its body, 's' half its height.
        static void Dancer(MeshKit mk, Material m, Vector3 c, Vector3 right, Vector3 upv, float s, int pose, float th)
        {
            Vector3 P(float x, float y) => c + right * (x * s) + upv * (y * s);
            void L(params float[] v) { for (var i = 0; i + 3 < v.Length; i += 2) { Vector3 a = P(v[i], v[i + 1]), b = P(v[i + 2], v[i + 3]); var d = b - a; if (d.sqrMagnitude > 1e-6f) mk.Prism(m, a - d.normalized * th * .5f, d, th, d.magnitude + th, 4); } }
            void Ring(float cx, float cy, float r) { const int n = 10; var v = new float[(n + 1) * 2]; for (var i = 0; i <= n; i++) { var a = i * Mathf.PI * 2f / n; v[i * 2] = cx + Mathf.Cos(a) * r; v[i * 2 + 1] = cy + Mathf.Sin(a) * r; } L(v); }
            Ring(0, .78f, .12f); L(-.12f, .86f, -.3f, .7f, -.24f, .56f);                 // the head, and the cloth hanging behind it
            L(0, .64f, 0, .08f); L(-.16f, .08f, .16f, .08f, .24f, -.2f, -.24f, -.2f, -.16f, .08f);   // the body, the kilt
            switch (((pose % 4) + 4) % 4)
            {
                case 0: L(0, .56f, .28f, .74f, .46f, 1f); L(0, .56f, -.28f, .36f, -.42f, .1f); L(-.08f, -.2f, -.22f, -.6f, -.3f, -1f); L(.08f, -.2f, .3f, -.56f, .5f, -1f); break;                         // the point: one hand at the sky
                case 1: L(0, .56f, .34f, .56f, .34f, .84f, .52f, .84f); L(0, .56f, -.34f, .56f, -.34f, .28f, -.52f, .28f); L(-.08f, -.2f, -.3f, -.6f, -.5f, -1f); L(.08f, -.2f, .26f, -.62f, .2f, -1f); break;   // the walk: one hand flat forward, one flat back
                case 2: L(0, .56f, .32f, .8f, .5f, 1f); L(0, .56f, -.32f, .8f, -.5f, 1f); L(-.08f, -.2f, -.28f, -.62f, -.2f, -1f); L(.08f, -.2f, .28f, -.62f, .2f, -1f); break;                             // both hands up
                default: L(0, .56f, -.32f, .4f, -.1f, .2f); L(0, .56f, .34f, .5f, .56f, .66f); L(-.08f, -.2f, -.1f, -.62f, -.34f, -1f); L(.08f, -.2f, .36f, -.5f, .3f, -1f); break;                         // a hand on the hip, a knee out
            }
        }

        /// The sphinx: lying along the way on a plinth, a striped headdress, and round dark glasses.
        void Sphinx(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; face.y = 0; face.Normalize(); var rot = Quaternion.LookRotation(face); Vector3 S(float x, float y, float z) => g + rot * new Vector3(x, y, z);
            mk.Box(marble, S(0, .5f, 0), new Vector3(5.2f, 1f, 12.4f), rot); mk.Box(goldM, S(0, 1.02f, 0), new Vector3(5.4f, .06f, 12.6f), rot);
            mk.Box(stoneWarm, S(0, 2.1f, -1.2f), new Vector3(3f, 2.2f, 7f), rot); foreach (var e in new[] { -1f, 1f }) { mk.Blob(stoneWarm, S(e * 1.3f, 2.1f, -3.6f), new Vector3(1.1f, 1.3f, 1.9f), prng, .08f); mk.Box(stoneWarm, S(e * 1.1f, 1.5f, 3.6f), new Vector3(1f, 1f, 4.2f), rot); mk.Box(stoneWarm, S(e * 1.1f, 1.32f, 5.7f), new Vector3(1.15f, .64f, .9f), rot); }
            mk.Box(stoneWarm, S(0, 2.9f, 2.2f), new Vector3(2.6f, 2.8f, 1.8f), rot); mk.Blob(stoneWarm, S(0, 5.15f, 2.6f), new Vector3(1.1f, 1.25f, 1.1f), prng, .05f);
            mk.Box(goldM, S(0, 5.2f, 2f), new Vector3(3f, 2.5f, .6f), rot * Quaternion.Euler(-8f, 0, 0)); foreach (var e in new[] { -1f, 1f }) { mk.Box(goldM, S(e * 1.2f, 4.1f, 2.6f), new Vector3(.7f, 2.3f, .7f), rot); for (var k = 0; k < 5; k++) mk.Box(navy, S(e * 1.2f, 3.2f + k * .44f, 2.6f), new Vector3(.74f, .12f, .74f), rot); }
            for (var k = 0; k < 5; k++) mk.Box(navy, S(0, 4.3f + k * .44f, 1.98f), new Vector3(3.04f, .12f, .64f), rot * Quaternion.Euler(-8f, 0, 0));
            // the glasses
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(dark, S(e * .46f, 5.3f, 3.62f), face, .4f, .08f, 14); mk.Prism(tileM[0], S(e * .46f, 5.3f, 3.6f), face, .46f, .05f, 14); } mk.Box(tileM[0], S(0, 5.36f, 3.64f), new Vector3(.3f, .07f, .05f), rot);
            mk.Blob(lampM, S(.62f, 5.46f, 3.72f), Vector3.one * .06f, prng, 0f);
            Col(S(0, 1.6f, 0), new Vector3(5.2f, 3.2f, 12.4f), rot); Col(S(0, 4.6f, 2.4f), new Vector3(3f, 3.2f, 2.2f), rot); Mark(g, 7f); Fixture(g, 6.6f);
            ArtistBits.PointLight(ctx.Root, S(0, 3f, 8.4f), Gold, 16f, 1.8f); halos.Add(S(0, 5.3f, 3.9f), 2.4f, Gold, .3f);
            string[] riddle = { "\"What goes on four legs at the afterparty, two at the disco, and three on the way home?\" It doesn't wait for an answer.", "The sphinx has seen every dance there is. It rates yours without moving.", "You could swear it tilted its glasses down to look at you." }; var rn = 0;
            var ask = new GameObject("The sphinx"); ask.transform.SetParent(tlife.transform, false); ask.transform.position = S(0, 2f, 6.6f); Interact.Toy(ask, Toy.Kind.Custom, "Ask the sphinx", new Vector3(3f, 2.6f, 1.6f)).onUse = _ => Say(tlife.transform, S(0, 7.2f, 3.4f), riddle[rn++ % riddle.Length], 6f);
        }
    }

    /// The tomb in the Pyramid: the beam from the Eye, the four mirrors you turn, the ball it has to reach, and what
    /// happens when it does (the pyramid's cap blazes, the sun disc spins, a beam goes up into the sky).
    public class DiscoTomb : MonoBehaviour
    {
        public class Mirror { public Transform plate; public Vector3 at, inD, outD, miss; public bool good; }
        public readonly List<Mirror> mirrors = new List<Mirror>();
        public Vector3 eye, ballAt, apex, sayAt; public Transform ball, sun; public Material ballM, capM, sunM; public Light ballLight;
        readonly List<Transform> beams = new List<Transform>(); Transform sky; Material beamM; Color ballBase, capBase, sunBase; float lit; bool solved, told; float started;

        void Start()
        {
            beamM = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Tomb beam" };
            beamM.SetColor("_BaseColor", new Color(1f, .9f, .55f, .55f)); beamM.SetFloat("_Surface", 1f); beamM.SetFloat("_Blend", 2f);
            beamM.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); beamM.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            beamM.SetFloat("_ZWrite", 0f); beamM.SetFloat("_Cull", 0f); beamM.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); beamM.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            for (var i = 0; i < 6; i++) beams.Add(Cyl("Tomb beam"));
            sky = Cyl("The Pyramid's beam"); sky.gameObject.SetActive(false);
            ballBase = ballM != null ? ballM.GetColor("_EmissionColor") : Color.black; capBase = capM != null ? capM.GetColor("_EmissionColor") : Color.black; sunBase = sunM != null ? sunM.GetColor("_EmissionColor") : Color.black;
            started = Time.time; Aim(); Trace();
        }

        Transform Cyl(string name)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = beamM; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return g.transform;
        }

        public void Turn(int i) { if (i < 0 || i >= mirrors.Count) return; mirrors[i].good = !mirrors[i].good; Aim(); Trace(); }

        /// Each mirror faces half way between where the light comes from and where it sends it.
        void Aim() { foreach (var m in mirrors) { var o = m.good ? m.outD : -m.outD; var n = (o - m.inD).normalized; if (m.plate != null) m.plate.rotation = Quaternion.LookRotation(n, Vector3.up); } }

        void Trace()
        {
            var pts = new List<Vector3> { eye }; var home = true;
            foreach (var m in mirrors) { pts.Add(m.at); if (!m.good) { pts.Add(m.miss); home = false; break; } }
            if (home) pts.Add(ballAt);
            for (var i = 0; i < beams.Count; i++)
            {
                var on = i + 1 < pts.Count; beams[i].gameObject.SetActive(on); if (!on) continue;
                var d = pts[i + 1] - pts[i]; beams[i].position = (pts[i] + pts[i + 1]) / 2f; beams[i].rotation = Quaternion.FromToRotation(Vector3.up, d.normalized); beams[i].localScale = new Vector3(.09f, d.magnitude / 2f, .09f);
            }
            if (home && !solved && Time.time - started > 1f && !told) { told = true; PlaceTools.Say(transform, sayAt, "The light finds the ball. It turns, the floor comes up under your feet in colour, and over your head the whole Pyramid wakes.", 8f); }
            solved = home;
        }

        void Update()
        {
            var t = Time.time; lit = Mathf.MoveTowards(lit, solved ? 1f : 0f, Time.deltaTime / 1.5f);
            if (ball != null) ball.Rotate(0, (10f + lit * 70f) * Time.deltaTime, 0, Space.World); if (sun != null) sun.Rotate(0, (14f + lit * 60f) * Time.deltaTime, 0, Space.World);
            if (ballM != null) ballM.SetColor("_EmissionColor", ballBase * (1f + lit * 4f)); if (capM != null) capM.SetColor("_EmissionColor", capBase * (1f + lit * (2.2f + .5f * Mathf.Sin(t * 3f))));
            if (sunM != null) sunM.SetColor("_EmissionColor", sunBase * (1f + lit * 1.2f)); if (ballLight != null) { ballLight.intensity = .6f + lit * 3.4f; if (lit > 0f) ballLight.color = Color.HSVToRGB(Mathf.Repeat(t * .15f, 1f), .35f, 1f); }
            if (sky != null) { var on = lit > .02f; if (sky.gameObject.activeSelf != on) sky.gameObject.SetActive(on); if (on) { const float len = 260f; sky.position = apex + Vector3.up * len / 2f; sky.rotation = Quaternion.identity; var w = (1.6f + .3f * Mathf.Sin(t * 2f)) * lit; sky.localScale = new Vector3(w, len / 2f, w); } }
        }
    }
}
