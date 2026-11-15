using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Virtual Riot's Moon Hall, the great cavern in the middle of the Dubstep Shards (part of ShardKit). All of his music
    /// fits here, heavy, bright, sci-fi and euphoric; "Lunar" is the heart of it. A still black lake fills the middle of
    /// the floor, and over it hangs a huge glowing moon, turning slowly, shards orbiting it, its light lying on the water.
    /// Stepping stones lead out to an islet under it. Round the hall, between the tunnels, his other songs each have a
    /// place, its title floating over it:
    /// - "Stargarden": a bed of crystal flowers with a sky of tiny stars hanging under the roof above it, a seat to look;
    /// - "Neon Angel": a crystal angel on a plinth, wings spread, lit pink from below;
    /// - "CHROMA": a prism on a pedestal; a white beam from the wall goes in and six colours fan out across the floor;
    /// - "Rewrite": a black wall of glowing symbols that keep rewriting themselves;
    /// - "Edge": a knife-edge ridge rising to a lookout high over the hall, a giant crystal blade beside it;
    /// - "Blackout": a small black room you walk into, dark but for white flashes;
    /// - "Different World": a portal ring showing a different sky, a sunset over hills, turning slowly.
    /// When "Lunar" plays the moon brightens and turns faster, the crystals all over the Shards go silver and ripples run
    /// across the lake. Each other song wakes its own place.
    public partial class ShardKit
    {
        const float ChamberR = 40f;
        readonly List<Vector2> hallSpots = new List<Vector2>();

        /// Where the seven places go round the hall: as far as they can be from the openings and from each other.
        void PlanHall()
        {
            hallSpots.Clear(); if (caves.Count < 2) return; var cv = caves[1];
            var blocked = new List<(float ang, float half)>();
            foreach (var h in cv.holes) blocked.Add((Mathf.Atan2(h.dir.y, h.dir.x), Mathf.Asin(Mathf.Min(1f, (h.hw + 6f) / ChamberR))));
            var front = caves[0].c - cv.c; blocked.Add((Mathf.Atan2(front.y, front.x), 30f * Mathf.Deg2Rad));
            var chosen = new List<float>();
            for (var k = 0; k < 7; k++)
            {
                var ba = 0f; var bc = float.MinValue;
                for (var d = 0; d < 360; d += 2)
                {
                    var a = d * Mathf.Deg2Rad; var m = 9f;
                    foreach (var (b, half) in blocked) m = Mathf.Min(m, Mathf.Abs(Mathf.DeltaAngle(d, b * Mathf.Rad2Deg)) * Mathf.Deg2Rad - half);
                    foreach (var c in chosen) m = Mathf.Min(m, Mathf.Abs(Mathf.DeltaAngle(d, c * Mathf.Rad2Deg)) * Mathf.Deg2Rad * .5f);
                    if (m > bc) { bc = m; ba = a; }
                }
                chosen.Add(ba);
            }
            chosen.Sort();
            foreach (var a in chosen) { var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); hallSpots.Add(dir); var p = cv.c + dir * ChamberR; caveClear.Add(new Vector3(p.x, 9f, p.y)); }
        }

        void RiotHall(Plot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rng = new System.Random(2017); var c = z.c; var fr = z.front; var sd = z.side;
            var star = Song(z, "Stargarden"); var angel = Song(z, "Neon Angel"); var lunar = Song(z, "Lunar"); var edge = Song(z, "Edge"); var chroma = Song(z, "CHROMA"); var black = Song(z, "Blackout"); var diff = Song(z, "Different World"); var rew = Song(z, "Rewrite");
            life.chromaSong = chroma; life.blackoutSong = black; life.lunarSong = lunar; life.starSong = star;
            var riot = new GameObject("The Moon Hall life").AddComponent<RiotLife>(); riot.transform.SetParent(z.root, false);
            riot.chromaSong = chroma; riot.rewriteSong = rew; riot.blackoutSong = black; riot.lunarSong = lunar; riot.diffSong = diff;

            // ------------------------------------------------ "Lunar": the lake and the moon
            const float LakeR = 14f;
            var lakeM = new Material(WorldMaterials.Glow(new Color(.05f, .06f, .14f), .5f)) { name = "The moon lake" }; life.pool = lakeM;
            z.mk.Prism(lakeM, c + up * .07f, up, LakeR, .03f, 48);
            for (var k = 0; k < 40; k++) { var a = k * Mathf.PI * 2f / 40f; var d = Mathf.Cos(a) * fr + Mathf.Sin(a) * sd; if (Vector3.Angle(d, fr) < 10f) continue; z.mk.Blob(rockD, c + d * (LakeR + .4f) + up * .12f, new Vector3(.9f, .3f, .9f), rng, .3f); }
            var refl = new GameObject("Moonlight on the water").transform; refl.SetParent(riot.transform, false); refl.SetPositionAndRotation(c + up * .16f, Quaternion.Euler(90f, 0, 0)); SoftDisc(refl, 9f, new Color(.8f, .86f, 1f, .3f), false);
            for (var k = 0; k < 3; k++) { var rp = new GameObject("Ripple").transform; rp.SetParent(riot.transform, false); rp.SetPositionAndRotation(c + up * .18f, Quaternion.Euler(90f, 0, 0)); riot.ripples.Add((rp, SoftDisc(rp, 1f, new Color(.8f, .9f, 1f, .5f), true))); }
            // stepping stones out to an islet under the moon
            for (var k = 0; k < 5; k++) { var sp = c + fr * (LakeR - 1f - k * 2.1f); z.mk.Prism(rock, sp - up * .2f, up, .75f, .55f, 8); z.solid.Prism(rock, sp - up * .2f, up, .75f, .55f, 8); }
            z.mk.Prism(rock, c - up * .2f, up, 2.6f, .6f, 14); z.solid.Prism(rock, c - up * .2f, up, 2.6f, .6f, 14); z.mk.Prism(shardM, c + up * .41f, up, 2.62f, .02f, 14);
            Interact.Seat(tlife.transform, c + up * .4f + fr * .6f, -fr, "Sit under the moon").seatHeight = .35f;
            // the moon, turning, with shards orbiting it
            var moonM = new Material(WorldMaterials.Glow(new Color(.86f, .9f, 1f), 1.3f)) { name = "The moon" }; life.coreM = moonM; var crater = Paint(new Color(.55f, .58f, .68f));
            var mm = new MeshKit(); const int Lat = 12, Lon = 22; const float MoonR = 7f;
            Vector3 S(int i, int j) { var th = Mathf.PI * i / Lat; var ph = Mathf.PI * 2f * j / Lon; return new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * MoonR; }
            for (var i = 0; i < Lat; i++) for (var j = 0; j < Lon; j++) { Vector3 a = S(i, j), b = S(i, j + 1), cc = S(i + 1, j + 1), d = S(i + 1, j); if (i > 0) mm.Tri(moonM, a, b, d, Vector3.zero); if (i < Lat - 1) mm.Tri(moonM, b, cc, d, Vector3.zero); }
            for (var k = 0; k < 9; k++) { var dir = Random3n(rng); mm.Blob(crater, dir * (MoonR - .25f), Vector3.one * (.7f + (float)rng.NextDouble() * 1f), rng, .2f); }
            var moonAt = c + up * 22f; var mg = mm.BuildSingle("The moon", z.root, false); mg.transform.position = moonAt; life.core = mg.transform; life.coreHome = moonAt;
            for (var k = 0; k < 8; k++) { var fm = new MeshKit(); fm.Prism(k % 2 == 0 ? shardM : shardM2, Vector3.zero, Vector3.up, .5f, 2f, 5, .02f); fm.Prism(k % 2 == 0 ? shardM : shardM2, Vector3.zero, Vector3.down, .5f, 1f, 5, .02f); var fg = fm.BuildSingle("Orbiting shard", z.root, false); life.orbiters.Add((fg.transform, k * Mathf.PI / 4f)); }
            halos.Add(moonAt, 18f, new Color(.8f, .86f, 1f), .3f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 15f, new Color(.8f, .86f, 1f), 62f, 1.5f);
            for (var k = 0; k < 6; k++) { var a = (k * 60f + 30f) * Mathf.Deg2Rad; var d = Mathf.Cos(a) * fr + Mathf.Sin(a) * sd; var bq = ctx.OnGround(c.x + d.x * 18f, c.z + d.z * 18f); Bench(z.root, z.mk, bq, -d); }
            Title(z, c + up * 31f, "LUNAR", 2.4f, White, fr);
            Folk(c + fr * 16.5f + sd * 3f, -fr, Resident.Pose.Stand, new[] { "It isn't a real moon. Don't tell it.", "Walk out on the stones and look up. That's the whole thing. That's enough.", "When Lunar plays the water moves. Nobody's touching it." }, Local());
            Shot(ctx, "the moon hall", c + fr * 30f + up * 3f, moonAt - up * 4f);

            // ------------------------------------------------ the seven places round the hall
            var titles = new[] { ("STARGARDEN", 0), ("NEON ANGEL", 1), ("CHROMA", 2), ("REWRITE", 3), ("EDGE", 4), ("BLACKOUT", 5), ("DIFFERENT WORLD", 6) };
            for (var k = 0; k < hallSpots.Count && k < 7; k++)
            {
                var dir = hallSpots[k]; var g = ctx.OnGround(c.x + dir.x * ChamberR, c.z + dir.y * ChamberR); var f = new PlaceFrame(g, -V3(dir));
                switch (k)
                {
                    case 0: // "Stargarden"
                        for (var i = 0; i < 44; i++)
                        {
                            var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = Mathf.Sqrt((float)rng.NextDouble()) * 4.4f; var fp = f.o + (f.Right * Mathf.Cos(a) + f.Fwd * Mathf.Sin(a)) * r; fp = ctx.OnGround(fp.x, fp.z); var h = .5f + (float)rng.NextDouble() * .9f;
                            z.mk.Prism(Paint(new Color(.3f, .4f, .4f)), fp, up, .035f, h, 4); var m = i % 7 == 0 ? SongGlow(star, Cyan, 1.4f) : (i % 2 == 0 ? shardM : shardM2);
                            for (var j = 0; j < 4; j++) z.mk.Prism(m, fp + up * h, (up + Quaternion.Euler(0, j * 90f + i * 23f, 0) * Vector3.forward * .8f).normalized, .06f, .45f, 4, .05f);
                        }
                        for (var i = 0; i < 16; i++) { var a = i * Mathf.PI / 8f; z.mk.Blob(rockD, f.o + (f.Right * Mathf.Cos(a) + f.Fwd * Mathf.Sin(a)) * 5f + up * .15f, new Vector3(.5f, .3f, .5f), rng, .3f); }
                        var starM = new Material(GlowM(White, 2.2f)) { name = "Stargarden stars" }; riot.stars = starM;
                        for (var i = 0; i < 80; i++) { var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = Mathf.Sqrt((float)rng.NextDouble()) * 9f; z.mk.Blob(starM, f.o + (f.Right * Mathf.Cos(a) + f.Fwd * Mathf.Sin(a)) * r + up * (13f + (float)rng.NextDouble() * 9f), Vector3.one * (.06f + (float)rng.NextDouble() * .1f), rng, 0f); }
                        ArtistBits.PostCollider(z.root, f.o, 1.4f, 4.4f); Bench(z.root, z.mk, f.P(0, 0, 6.6f), -f.Fwd);
                        ArtistBits.PointLight(z.root, f.P(0, 3f, 2f), Cyan, 13f, 1.6f);
                        Shot(ctx, "stargarden", f.P(3f, 2.2f, 9f), f.o + up * 6f);
                        break;
                    case 1: // "Neon Angel"
                        z.mk.Box(rock, f.P(0, .7f, 0), new Vector3(2.4f, 1.4f, 2.4f), f.r); BoxCol(z.root, f.P(0, 3f, 0), new Vector3(2.4f, 6f, 2.4f), f.r);
                        z.mk.Prism(SongGlow(angel, Magenta, 1.4f), f.P(0, 1.4f, 0), up, .45f, 3.1f, 6, .45f); z.mk.Blob(shardM2, f.P(0, 4.85f, 0), Vector3.one * .38f, rng, .1f);
                        for (var i = 0; i < 16; i++) { var a = i * Mathf.PI / 8f; z.mk.Box(shardM, f.P(Mathf.Cos(a) * .6f, 5.55f, Mathf.Sin(a) * .6f), new Vector3(.24f, .05f, .08f), f.r * Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
                        foreach (var e in new[] { -1f, 1f }) for (var i = 0; i < 6; i++) { var root = f.P(e * .3f, 3.7f, -.2f); var d = (f.Right * e * 1f + up * (1.3f - i * .38f) - f.Fwd * .25f).normalized; z.mk.Prism(i % 2 == 0 ? shardM2 : shardM, root, d, .14f, 3.8f - i * .32f, 4, .05f); }
                        ArtistBits.PointLight(z.root, f.P(0, .6f, 2f), Magenta, 11f, 2.2f); halos.Add(f.P(0, 3.4f, 0), 4f, Magenta, .25f);
                        Shot(ctx, "neon angel", f.P(2f, 2f, 8f), f.P(0, 3.5f, 0));
                        break;
                    case 2: // "CHROMA"
                        z.mk.Box(dark, f.P(0, .5f, 0), new Vector3(1.6f, 1f, 1.6f), f.r); ArtistBits.PostCollider(z.root, f.o, 3f, 1.1f);
                        z.mk.Prism(SongGlow(chroma, White, .8f), f.P(0, 1f, 0), up, 1.1f, 2.2f, 3, 1f, 30f);
                        var src = f.P(0, 2.1f, -12.5f); var hit = f.P(0, 2.1f, 0); z.mk.Box(GlowM(White, 1.8f), (src + hit) / 2f, new Vector3(.1f, .1f, Vector3.Distance(src, hit)), Quaternion.LookRotation(hit - src));
                        var cols = new[] { new Color(1f, .2f, .2f), new Color(1f, .6f, .15f), new Color(1f, 1f, .3f), new Color(.3f, 1f, .4f), new Color(.3f, .5f, 1f), new Color(.7f, .3f, 1f) };
                        riot.beams = new Material[6]; riot.beamBase = new Color[6];
                        for (var i = 0; i < 6; i++)
                        {
                            var bm = new Material(GlowM(cols[i], 1.7f)) { name = "Chroma beam " + i }; riot.beams[i] = bm; riot.beamBase[i] = cols[i];
                            var end = f.P(-6.5f + i * 2.6f, .2f, 7f); z.mk.Box(bm, (hit + end) / 2f, new Vector3(.09f, .09f, Vector3.Distance(hit, end)), Quaternion.LookRotation(end - hit)); z.mk.Prism(bm, ctx.OnGround(end.x, end.z) + up * .2f, up, .7f, .02f, 12);
                        }
                        ArtistBits.PointLight(z.root, f.P(0, 3f, 3f), White, 11f, 1.4f);
                        Shot(ctx, "chroma", f.P(-4f, 2.2f, 10f), f.P(0, 1.5f, 1f));
                        break;
                    case 3: // "Rewrite"
                        z.mk.Box(dark, f.P(0, 2.3f, -1f), new Vector3(7f, 4.6f, .5f), f.r); z.solid.Box(dark, f.P(0, 2.3f, -1f), new Vector3(7f, 4.6f, .5f), f.r);
                        foreach (var e in new[] { -1f, 1f }) z.mk.Box(shardM, f.P(e * 3.55f, 2.3f, -.74f), new Vector3(.1f, 4.7f, .1f), f.r); z.mk.Box(shardM, f.P(0, 4.65f, -.74f), new Vector3(7.2f, .1f, .1f), f.r);
                        var glyph = GlowM(Lime, 1.3f);
                        for (var row = 0; row < 6; row++) for (var col = 0; col < 8; col++)
                        {
                            var gk = new MeshKit(); var w = .3f + (float)rng.NextDouble() * .45f; gk.Box(row == 5 && col == 0 ? SongGlow(rew, Lime, 1.4f) : glyph, Vector3.zero, new Vector3(w, .14f, .02f));
                            var gg = gk.BuildSingle("Symbol", riot.transform, false); gg.transform.SetPositionAndRotation(f.P(-3f + col * .82f + (float)rng.NextDouble() * .1f, .8f + row * .65f, -.74f), f.r); riot.glyphs.Add(gg.GetComponent<Renderer>());
                        }
                        ArtistBits.PointLight(z.root, f.P(0, 2.5f, 2.5f), Lime, 10f, 1.4f);
                        Shot(ctx, "rewrite", f.P(1f, 1.8f, 6f), f.P(0, 2.3f, -1f));
                        break;
                    case 4: // "Edge"
                        Vector3 r0 = f.P(0, 0, 4f), r1 = f.P(0, 5f, -8f); var along = (r1 - r0).normalized; var nrm = Vector3.Cross(along, f.Right).normalized; if (nrm.y < 0) nrm = -nrm;
                        z.mk.Box(rock, (r0 + r1) / 2f - nrm * .25f, new Vector3(1.6f, .5f, Vector3.Distance(r0, r1) + .3f), Quaternion.LookRotation(along, nrm)); z.solid.Box(rock, (r0 + r1) / 2f - nrm * .25f, new Vector3(1.6f, .5f, Vector3.Distance(r0, r1) + .3f), Quaternion.LookRotation(along, nrm));
                        z.mk.Box(shardM, (r0 + r1) / 2f + nrm * .01f + f.Right * .78f, new Vector3(.05f, .03f, Vector3.Distance(r0, r1)), Quaternion.LookRotation(along, nrm)); z.mk.Box(shardM2, (r0 + r1) / 2f + nrm * .01f - f.Right * .78f, new Vector3(.05f, .03f, Vector3.Distance(r0, r1)), Quaternion.LookRotation(along, nrm));
                        for (var i = 1; i < 12; i++) { var zz = 4f - i; var h = 5f * i / 12f - .4f; if (h < .1f) continue; z.mk.Box(rockD, f.P(0, h / 2f - .2f, zz), new Vector3(1.1f, h + .4f, 1.02f), f.r); z.solid.Box(rockD, f.P(0, h / 2f - .2f, zz), new Vector3(1.1f, h + .4f, 1.02f), f.r); }
                        z.mk.Box(rock, f.P(0, 4.75f, -9.5f), new Vector3(3f, .5f, 3f), f.r); z.solid.Box(rock, f.P(0, 4.75f, -9.5f), new Vector3(3f, .5f, 3f), f.r); z.mk.Box(rockD, f.P(0, 2.2f, -9.5f), new Vector3(2.4f, 4.6f, 2.4f), f.r); z.solid.Box(rockD, f.P(0, 2.2f, -9.5f), new Vector3(2.4f, 4.6f, 2.4f), f.r);
                        z.mk.Box(SongGlow(edge, White, 1.1f), f.P(2.5f, 5.6f, -9.5f), new Vector3(.16f, 11f, 2.6f), f.r * Quaternion.Euler(0, 8f, 4f)); z.mk.Blob(rockD, f.P(2.5f, .3f, -9.5f), new Vector3(1.4f, .8f, 1.8f), rng, .3f); BoxCol(z.root, f.P(2.5f, 5.5f, -9.5f), new Vector3(.5f, 11f, 2.6f), f.r);
                        Interact.Seat(tlife.transform, f.P(0, 5f, -8.6f), f.Fwd, "Sit on the edge").seatHeight = .45f;
                        halos.Add(f.P(2.5f, 6f, -9.5f), 4.5f, White, .25f); ArtistBits.PointLight(z.root, f.P(0, 6f, -6f), White, 12f, 1.4f);
                        caveClear.Add(new Vector3(f.P(0, 0, -9f).x, 4f, f.P(0, 0, -9f).z));
                        Shot(ctx, "edge", f.P(-6f, 2f, 7f), f.P(0, 4f, -7f));
                        break;
                    case 5: // "Blackout": a black room
                        var room = f.o; var bholes = new List<(Vector2 dir, float hw, float wallH, float roofH)> { (new Vector2(f.Fwd.x, f.Fwd.z).normalized, 1.3f, 2.6f, 3f) };
                        Shell(z.mk, z.solid, room, 6.5f, 3.2f, 6.8f, bholes, dark, rockD, 77, .5f); Collar(z.mk, z.solid, room + f.Fwd * 5.9f, f.Fwd, 1.3f, 2.6f, 3f, rockD);
                        z.mk.Prism(dark, room + up * .03f, up, 6.3f, .05f, 24); z.mk.Prism(SongGlow(black, Violet, .4f), room + up * .09f, up, 5.6f, .01f, 32); z.mk.Prism(dark, room + up * .095f, up, 5.45f, .01f, 32);
                        riot.strobe = ArtistBits.PointLight(z.root, room + up * 3.6f, White, 10f, 0f); IndoorSpaces.Add(room + up * 2f, f.r, new Vector3(8.6f, 4f, 8.6f));
                        Shot(ctx, "blackout", f.P(0, 1.8f, 11f), room + up * 2f);
                        break;
                    default: // "Different World": a portal showing another sky
                        var pc = f.P(0, 4f, 0); var pm = new MeshKit();
                        for (var i = 0; i < 28; i++) { var a = i * Mathf.PI * 2f / 28f; pm.Box(i % 2 == 0 ? shardM : shardM2, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 3.7f, new Vector3(.8f, .28f, .3f), Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); }
                        var pg = pm.BuildSingle("Portal ring", riot.transform, false); pg.transform.SetPositionAndRotation(pc, f.r); riot.portal = pg.transform;
                        var skyCols = new[] { new Color(.2f, .35f, .6f), new Color(.3f, .45f, .7f), new Color(.5f, .55f, .75f), new Color(.85f, .6f, .55f), new Color(1f, .62f, .35f), new Color(1f, .78f, .4f), new Color(.2f, .25f, .3f), new Color(.14f, .2f, .22f) };
                        const float PR = 3.4f; var bands = 12;
                        for (var i = 0; i < bands; i++)
                        {
                            float y0 = -PR + 2f * PR * i / bands, y1 = -PR + 2f * PR * (i + 1) / bands; float w0 = Mathf.Sqrt(Mathf.Max(0f, PR * PR - y0 * y0)), w1 = Mathf.Sqrt(Mathf.Max(0f, PR * PR - y1 * y1));
                            var ci = Mathf.Clamp(Mathf.FloorToInt((1f - (i + .5f) / bands) * skyCols.Length), 0, skyCols.Length - 1); var bm = GlowM(skyCols[ci], 1.1f);
                            Vector3 a = pc + f.Right * -w0 + up * y0, b = pc + f.Right * w0 + up * y0, cc = pc + f.Right * w1 + up * y1, d = pc + f.Right * -w1 + up * y1;
                            z.mk.Quad(bm, a, b, cc, d, pc - f.Fwd); z.mk.Quad(bm, a, b, cc, d, pc + f.Fwd);
                        }
                        foreach (var e in new[] { 1f, -1f }) z.mk.Prism(SongGlow(diff, new Color(1f, .9f, .6f), 1.6f), pc + f.Right * 1.1f + up * -.4f + f.Fwd * e * .02f, f.Fwd * e, .7f, .02f, 16);
                        foreach (var e in new[] { -2.6f, 2.6f }) { z.mk.Blob(rockD, f.P(e, .4f, 0), new Vector3(1.1f, 1f, 1f), rng, .3f); }
                        BoxCol(z.root, f.P(0, 3.9f, 0), new Vector3(7.6f, 7.8f, .5f), f.r);
                        ArtistBits.PointLight(z.root, f.P(0, 3f, 2.5f), new Color(1f, .7f, .45f), 11f, 1.6f);
                        Shot(ctx, "different world", f.P(-2f, 2f, 8f), pc);
                        break;
                }
                Title(z, f.P(0, 9f, 0), titles[k].Item1, 1.1f, Cyan, f.Fwd);
            }
            Shot(ctx, "virtual riot's moon hall", c + fr * 34f + up * 9f, moonAt);
        }

        void Title(Plot z, Vector3 at, string text, float size, Color col, Vector3 face) { ArtistBits.Label(z.labels, text, at, face, size, col, TextAnchor.MiddleCenter); halos.Add(at, size * 2.4f, col, .2f); }

        static Vector3 Random3n(System.Random rng) { var v = new Vector3((float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f); return v.sqrMagnitude < .0001f ? Vector3.up : v.normalized; }

        /// A soft round glow (or a thin ring) in the parent's XY plane, additive; returns its material.
        static Material SoftDisc(Transform parent, float r, Color c, bool ring)
        {
            var v = new List<Vector3>(); var col = new List<Color>(); var t = new List<int>();
            if (!ring) { v.Add(Vector3.zero); col.Add(c); for (var k = 0; k < 40; k++) { var a = k * Mathf.PI / 20f; v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * r); col.Add(new Color(c.r, c.g, c.b, 0f)); t.AddRange(new[] { 0, 1 + k, 1 + (k + 1) % 40 }); } }
            else for (var k = 0; k < 48; k++) { var a = k * Mathf.PI / 24f; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); v.Add(d * r * .9f); v.Add(d * r); v.Add(d * r * 1.06f); col.Add(new Color(c.r, c.g, c.b, 0f)); col.Add(c); col.Add(new Color(c.r, c.g, c.b, 0f)); var b = k * 3; var n = ((k + 1) % 48) * 3; t.AddRange(new[] { b, b + 1, n + 1, b, n + 1, n, b + 1, b + 2, n + 2, b + 1, n + 2, n + 1 }); }
            var mesh = new Mesh { name = "Soft glow" }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Soft glow"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); var m = LanaNight.Additive("Soft glow"); mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return m;
        }
    }

    /// The Moon Hall's own movements: the CHROMA beams cycle colour, the Rewrite symbols rewrite, the Blackout room
    /// flashes, the Different World portal turns, ripples cross the lake for "Lunar", the Stargarden stars twinkle.
    public class RiotLife : MonoBehaviour
    {
        const string Land = "The Dubstep Shards";
        public string chromaSong, rewriteSong, blackoutSong, lunarSong, diffSong;
        public Material[] beams; public Color[] beamBase; public Material stars; public Light strobe; public Transform portal;
        public readonly List<Renderer> glyphs = new List<Renderer>(); public readonly List<(Transform t, Material m)> ripples = new List<(Transform, Material)>();
        float chroma, rew, black, lunar, diff, nextGlyph, nextFlash, flashUntil; Color starBase;

        void Start() { if (stars != null) starBase = stars.GetColor("_EmissionColor"); nextFlash = Time.time + 3f; }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            float Ease(float v, string song, float secs) => Mathf.MoveTowards(v, here && song != null && id == song ? 1f : 0f, dt / secs);
            chroma = Ease(chroma, chromaSong, 2f); rew = Ease(rew, rewriteSong, 2f); black = Ease(black, blackoutSong, 1f); lunar = Ease(lunar, lunarSong, 4f); diff = Ease(diff, diffSong, 3f);
            if (!here) return;
            if (beams != null) for (var i = 0; i < beams.Length; i++) if (beams[i] != null) { var hue = Color.HSVToRGB(Mathf.Repeat(t * .25f + i / 6f, 1f), .8f, 1f); beams[i].SetColor("_EmissionColor", Color.Lerp(beamBase[i], hue, chroma) * (1.1f + chroma * 1.4f)); }
            if (glyphs.Count > 0 && t >= nextGlyph) { nextGlyph = t + .14f / (1f + rew * 4f); for (var k = 0; k < 3 + (int)(rew * 6f); k++) { var g = glyphs[Random.Range(0, glyphs.Count)]; if (g != null) g.enabled = !g.enabled || Random.value < .3f; } }
            if (strobe != null)
            {
                if (black > .1f) strobe.intensity = Mathf.Repeat(t * 7f, 1f) < .22f ? 7f * black : 0f;
                else { if (t >= nextFlash) { flashUntil = t + .09f; nextFlash = t + Random.Range(3f, 7f); } strobe.intensity = t < flashUntil ? 5f : 0f; }
            }
            if (portal != null) portal.Rotate(Vector3.forward, dt * (8f + diff * 50f), Space.Self);
            for (var i = 0; i < ripples.Count; i++)
            {
                var (tr, m) = ripples[i]; if (tr == null) continue; var s = Mathf.Repeat(t * .09f + i / 3f, 1f);
                tr.localScale = Vector3.one * Mathf.Lerp(1f, 13.5f, s); m.SetColor("_BaseColor", new Color(1f, 1f, 1f, (.12f + lunar * .7f) * (1f - s)));
            }
            if (stars != null) stars.SetColor("_EmissionColor", starBase * (.7f + .3f * Mathf.Sin(t * 2.6f)));
        }
    }
}
