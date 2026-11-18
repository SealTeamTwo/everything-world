using System.Collections.Generic;
using EverythingWorld.Player;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Velvet Rooftops' Well (part of VelvetKit; the city round it is VelvetTowers.cs): one great round tower
    /// whose inside is a shaft.
    /// - Its wall is lined with rings of lit windows going down into the dark like the inside of a tower. Water runs
    ///   off the rim into it. There is no bottom to see.
    /// - Towers rise out of it, and on each roof a different kind of late night: a cinema showing nothing, a garden,
    ///   a line of washing with one light on, a helipad, a party that ended an hour ago, a bar that is closing, a
    ///   mattress under a water tower. Walkways of lit glass run from the rim to the roofs and from roof to roof,
    ///   more than one way, up and down.
    /// - Jump in, here or anywhere in the city. You fall past the windows into the dark, come out of the night sky
    ///   and land in the pool on the roof where you arrived (VelvetFall).
    public partial class VelvetKit
    {
        Vector2 hole2; bool hasHole; float rimY; const float HoleR = 84f;

        void Hole(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(303);
            if (!hasHole) { Debug.Log("The Velvet Rooftops: no room was found for the Hole"); return; }
            var hc = new Vector3(hole2.x, rimY, hole2.y); var c0 = ctx.OnGround(center.x, center.z); var toHole = FaceTo(c0, hc); var dist = Vector3.Distance(new Vector3(c0.x, 0, c0.z), new Vector3(hc.x, 0, hc.z));
            Mark(hc, HoleR + 14f);
            // ---- the rim: a deck all the way round, a line of light at the edge; and the shaft's wall, ringed with lit windows
            const int Seg = 56; Vector3 Ring(float r, int k, float y) { var a = k * Mathf.PI * 2f / Seg; return new Vector3(hc.x + Mathf.Cos(a) * r, y, hc.z + Mathf.Sin(a) * r); }
            var floorY = waterY + 1.2f; var wins = new[] { windowWarm, windowCool, windowRose, windowWarm };
            for (var k = 0; k < Seg; k++)
            {
                mk.Quad(deckM, Ring(HoleR - 7.6f, k, rimY + .16f), Ring(HoleR - 7.6f, k + 1, rimY + .16f), Ring(WellOut + .2f, k + 1, rimY + .16f), Ring(WellOut + .2f, k, rimY + .16f), hc - up * 30f);
                solid.Quad(deckM, Ring(HoleR - 7.6f, k, rimY + .1f), Ring(HoleR - 7.6f, k + 1, rimY + .1f), Ring(WellOut + .2f, k + 1, rimY + .1f), Ring(WellOut + .2f, k, rimY + .1f), hc - up * 30f);   // (the deck covers the dug edge: this is what you stand on there)
                var e0 = Ring(HoleR - 7.6f, k, rimY + .22f); var e1 = Ring(HoleR - 7.6f, k + 1, rimY + .22f); mk.Prism(neonM[k / 7 % 5], e0, e1 - e0, .09f, (e1 - e0).magnitude + .05f, 4);
                // the wall: facing inward, so the point it is turned away from is outside it
                var outside = Ring(HoleR + 40f, k, rimY - 20f);
                mk.Quad(dark, Ring(HoleR - 7.7f, k, rimY + .1f), Ring(HoleR - 7.7f, k + 1, rimY + .1f), Ring(HoleR - 7.7f, k + 1, floorY - 1f), Ring(HoleR - 7.7f, k, floorY - 1f), outside);
                for (var y = rimY - 3.5f; y > floorY + 2f; y -= 5f) { if ((k * 7 + (int)y * 3) % 5 == 0) continue; mk.Quad(wins[(k + (int)(y / 5f)) & 3], Ring(HoleR - 7.85f, k, y), Ring(HoleR - 7.85f, k + 1, y), Ring(HoleR - 7.85f, k + 1, y - 1.5f), Ring(HoleR - 7.85f, k, y - 1.5f), outside); }
                if (k % 8 == 0) Remember(Ring(HoleR + 3f, k, rimY + .2f), 7);
            }
            for (var k = 0; k < Seg; k += 7) { var lp = Ring(HoleR + 11f, k, rimY + .16f); Lamp(ctx, mk, lp, k % 14 == 0); }
            // the Well's outside: a great round tower like the others, down into the dark, a line of light round its top
            mk.Prism(dark, new Vector3(hc.x, floorY - 1f, hc.z), up, WellOut, rimY - floorY + .7f, Seg, 1f, 0f, false);
            for (var y = rimY - 4.5f; y > floorY + 3f; y -= 5f) if ((int)(y / 5f) % 4 != 0) mk.Prism(wins[(int)(y / 5f) & 3], new Vector3(hc.x, y, hc.z), up, WellOut + .25f, 1.5f, Seg, 1f, 0f, false);
            for (var k = 0; k < Seg; k++) { var e0 = Ring(WellOut, k, rimY + .2f); var e1 = Ring(WellOut, k + 1, rimY + .2f); mk.Prism(neonM[(k / 7 + 2) % 5], e0, e1 - e0, .1f, (e1 - e0).magnitude + .05f, 4); }
            // ---- water off the rim: six little basins, each spilling a sheet down the wall
            var fall = new Material(WorldMaterials.Glow(new Color(.5f, .9f, 1f), .7f)) { name = "Velvet falls" };
            for (var k = 4; k < Seg; k += 9)
            {
                var basin = Ring(HoleR + 7f, k, rimY + .2f); var lip = Ring(HoleR - 7.5f, k, rimY + .24f); var dir = (lip - basin).normalized; var rot = Quaternion.LookRotation(dir);
                mk.Prism(concrete, basin - up * .1f, up, 2.4f, .5f, 12); mk.Prism(poolM, basin + up * .32f, up, 2.1f, .1f, 12); ArtistBits.PostCollider(ctx.Root, basin, .5f, 2.4f); halos.Add(basin + up * .6f, 3f, Aqua, .3f);
                mk.Box(poolM, (basin + lip) / 2f + up * .06f, new Vector3(2.2f, .06f, Vector3.Distance(basin, lip)), rot); var depth = rimY - floorY;
                mk.Box(fall, lip + dir * .25f - up * (depth / 2f), new Vector3(2.2f, depth, .12f), rot); mk.Box(fall, lip + dir * .4f - up * (depth / 2f + 3f), new Vector3(1.2f, depth - 6f, .1f), rot);
            }
            // ---- there is no bottom: the dark comes up to meet you
            var fallGo = new GameObject("The Hole").AddComponent<VelvetFall>(); fallGo.transform.SetParent(life.transform, false);
            var deep = rimY - floorY; fallGo.Dark(hc, HoleR - 7.9f, rimY - Mathf.Min(17f, deep * .45f), floorY + .6f);
            var pc = c0 + R * 11f; fallGo.center = hc; fallGo.radius = HoleR - 6f; fallGo.below = floorY + 3.2f; /* just before the floor that nobody ever sees */ fallGo.landing = new Vector3(pc.x, c0.y + 150f, pc.z); fallGo.sayAt = pc + up * 2.4f;

            // ---- the towers in it, and a different late night on every roof
            var roofs = new List<(Vector3 at, float half)>(); float[] ringH = { 2f, -6f, 7f, -4f, 8f, 0f };
            roofs.Add((hc + up * 6f, 13f));
            for (var k = 0; k < 6; k++) { var a = (k * 60f + 20f) * Mathf.Deg2Rad; roofs.Add((new Vector3(hc.x + Mathf.Cos(a) * 49f, rimY + ringH[k], hc.z + Mathf.Sin(a) * 49f), 10f + (k % 3) * 1.2f)); }
            for (var i = 0; i < roofs.Count; i++)
            {
                var (at, half) = roofs[i]; var rot = Quaternion.Euler(0, i * 23f, 0); var h = at.y - floorY + 2f; var mid = new Vector3(at.x, at.y - h / 2f, at.z);
                mk.Box(dark, mid, new Vector3(half * 2f, h, half * 2f), rot); solid.Box(dark, mid, new Vector3(half * 2f, h, half * 2f), rot);
                for (var y = at.y - 3f; y > floorY + 3f; y -= 5f) if ((i + (int)y) % 4 != 0) mk.Box(wins[(i + (int)(y / 5f)) & 3], new Vector3(at.x, y, at.z), new Vector3(half * 2f + .2f, 1.4f, half * 2f + .2f), rot);
                mk.Box(deckM, at + up * .06f, new Vector3(half * 2f - .3f, .12f, half * 2f - .3f), rot); foreach (var (x, zz, sx, sz) in new[] { (0f, 1f, 1f, 0f), (0f, -1f, 1f, 0f), (1f, 0f, 0f, 1f), (-1f, 0f, 0f, 1f) }) mk.Box(neonM[(i + (int)(x + zz + 2)) % 5], at + rot * new Vector3(x * half, .14f, zz * half), new Vector3(sx * half * 2f + .16f, .16f, sz * half * 2f + .16f), rot);
                LateNight(ctx, mk, solid, at + up * .12f, rot, half, i, rng);
                ArtistBits.PointLight(ctx.Root, at + up * 4f, Color.Lerp(Neons[i % 5], Color.white, .55f), half * 2.4f, 1.8f);
            }
            // walkways: the rim to three of the ring, the ring to itself all the way round, and every one of the ring to the middle
            int ways = 0;
            void Way(Vector3 a, Vector3 b, float ha, float hb)
            {
                var flat = new Vector3(b.x - a.x, 0, b.z - a.z); var d = flat.normalized; var p = a + d * (ha - .6f) + up * .12f; var q = b - d * (hb - .6f) + up * .12f; var seg = q - p; var rot = Quaternion.LookRotation(seg.normalized, up);
                mk.Box(dark, (p + q) / 2f - rot * Vector3.up * .14f, new Vector3(3.4f, .2f, seg.magnitude), rot); mk.Box(glassWalk, (p + q) / 2f, new Vector3(2.9f, .1f, seg.magnitude), rot); solid.Box(dark, (p + q) / 2f - rot * Vector3.up * .2f, new Vector3(3.4f, .5f, seg.magnitude + .6f), rot);
                foreach (var e in new[] { -1f, 1f }) { var r0 = p + rot * Vector3.right * (e * 1.65f) + up * .9f; var r1 = q + rot * Vector3.right * (e * 1.65f) + up * .9f; mk.Prism(neonM[(ways + (e > 0 ? 1 : 3)) % 5], r0, r1 - r0, .05f, (r1 - r0).magnitude, 4); var n = Mathf.Max(2, Mathf.RoundToInt(seg.magnitude / 4f)); for (var k = 0; k <= n; k++) { var s = Vector3.Lerp(p, q, k / (float)n) + rot * Vector3.right * (e * 1.65f); mk.Prism(steel, s, up, .03f, .9f, 4); } }
                for (var k = 1; k < 4; k++) Remember(Vector3.Lerp(p, q, k / 4f) + up * .2f, 8); ways++;
            }
            for (var k = 0; k < 6; k++) { Way(roofs[1 + k].at, roofs[1 + (k + 1) % 6].at, roofs[1 + k].half, roofs[1 + (k + 1) % 6].half); Way(roofs[1 + k].at, roofs[0].at, roofs[1 + k].half, roofs[0].half); }
            foreach (var k in new[] { 0, 2, 4 }) { var at = roofs[1 + k].at; var outward = new Vector3(at.x - hc.x, 0, at.z - hc.z).normalized; var rimPt = new Vector3(hc.x, rimY + .04f, hc.z) + outward * (HoleR - 6f); Way(rimPt, at, 0f, roofs[1 + k].half); }
            var who = ctx.OnGround(hc.x - toHole.x * (HoleR + 5f), hc.z - toHole.z * (HoleR + 5f));
            Folk(who + Vector3.Cross(up, toHole) * 3f, -toHole, Resident.Pose.Lean, new[] { "Everybody jumps in the end. You fall past all those windows, and then it's the sky, and then you're in the pool where you started. Wet, mind.", "There isn't a bottom. We've dropped things. A piano, once. It's still going.", "Seven roofs out there and a different night on each. The cinema's my favourite. Nothing's on. It's very good.", "Take the glass. Or don't take the glass. Either way gets you somewhere." });
            Shot(ctx, "the hole", who + up * 2.4f - toHole * 6f, hc + up * 2f);
            Shot(ctx, "down the hole", new Vector3(hc.x, rimY + 2f, hc.z) - toHole * (HoleR - 9f), hc - up * 30f);
            Shot(ctx, "a roof in the hole", roofs[1].at + up * 2f + (roofs[1].at - hc).normalized * 4f, roofs[0].at + up * 2f);
            Debug.Log($"The Velvet Rooftops: the Hole is {dist:0} m from the Pool, {rimY - floorY:0} m deep to where the dark starts being all there is; {roofs.Count} roofs in it, {ways} walkways");
        }

        /// What is on a roof in the Hole: each one a different kind of late night.
        void LateNight(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 o, Quaternion rot, float half, int kind, System.Random rng)
        {
            var up = Vector3.up; Vector3 P(float x, float y, float z) => o + rot * new Vector3(x, y, z); var f = rot * Vector3.forward;
            switch (kind)
            {
                case 0:   // the cinema showing nothing: a blank lit screen, rows of seats, the projector's beam
                    mk.Box(dark, P(0, 3.2f, half - 1.6f), new Vector3(half * 1.5f, 5.4f, .3f), rot); mk.Box(GlowM(new Color(.95f, .96f, 1f), 1.5f), P(0, 3.4f, half - 1.78f), new Vector3(half * 1.4f, 4.4f, .06f), rot); Col(P(0, 3f, half - 1.6f), new Vector3(half * 1.5f, 6f, .4f), rot);
                    for (var r = 0; r < 3; r++) for (var k = -3; k <= 3; k++) { var s = P(k * 1.1f, 0, 2f - r * 2.2f); mk.Box(velvet, s + up * .3f, new Vector3(.8f, .6f, .8f), rot); mk.Box(velvet, s + up * .8f - f * .36f, new Vector3(.8f, .8f, .14f), rot); if ((k + r) % 2 == 0) Interact.Seat(tlife.transform, s + up * .62f, f, "Watch nothing").seatHeight = .6f; }
                    mk.Box(steel, P(0, 1.2f, -half + 2f), new Vector3(.6f, .5f, .9f), rot); mk.Prism(steel, P(0, 0, -half + 2f), up, .06f, 1f, 5); halos.Add(P(0, 3.4f, half - 3f), 7f, new Color(.9f, .92f, 1f), .3f); ArtistBits.PointLight(ctx.Root, P(0, 3f, half - 4f), new Color(.9f, .92f, 1f), 20f, 2.2f); break;
                case 1:   // the garden
                    Garden(ctx, mk, P(-3f, 0, 2f), rot, rng); Garden(ctx, mk, P(3.4f, 0, -2.6f), rot * Quaternion.Euler(0, 90f, 0), rng); for (var k = 0; k < 4; k++) { var b = P(-half + 2f + k * 2.2f, 0, -half + 1.6f); mk.Box(woodM, b + up * .35f, new Vector3(1.6f, .7f, 1f), rot); mk.Blob(leafM, b + up * 1.1f, new Vector3(.8f, .6f, .5f), rng, .25f); }
                    mk.Box(woodM, P(4f, .45f, 4f), new Vector3(2f, .1f, .5f), rot); Interact.Seat(tlife.transform, P(4f, .5f, 4f), -f, "Sit in the roof garden").seatHeight = .46f; break;
                case 2:   // the washing, and one light on
                    foreach (var e in new[] { -1f, 1f }) { mk.Prism(steel, P(e * (half - 2f), 0, 0), up, .05f, 2.6f, 5); ArtistBits.PostCollider(ctx.Root, P(e * (half - 2f), 0, 0), 2.4f, .08f); } mk.Prism(steel, P(-half + 2f, 2.5f, 0), rot * Vector3.right, .012f, half * 2f - 4f, 3);
                    for (var k = 0; k < 6; k++) mk.Box(white, P(-half + 3.4f + k * (half * 2f - 6.8f) / 5f, 1.75f - (k % 2) * .2f, 0), new Vector3(1.1f, 1.4f + (k % 2) * .4f, .03f), rot * Quaternion.Euler((k % 3 - 1) * 5f, 0, 0));
                    Lamp(ctx, mk, P(half - 3f, 0, half - 3f), true); mk.Box(woodM, P(-2f, .25f, -4f), new Vector3(.5f, .5f, .5f), rot); Interact.Seat(tlife.transform, P(-2f, .52f, -4f), f, "Sit by the washing").seatHeight = .5f; mk.Box(dark, P(-1f, .2f, -4.4f), new Vector3(.5f, .34f, .2f), rot); mk.Blob(lampM, P(-1.16f, .26f, -4.29f), Vector3.one * .03f, rng, 0f); break;
                case 3:   // the helipad
                    mk.Prism(concrete, o + up * .02f, up, half - 1.6f, .05f, 24); mk.Prism(neonM[2], o + up * .08f, up, half - 2f, .012f, 24); mk.Prism(concrete, o + up * .085f, up, half - 2.4f, .012f, 24); foreach (var e in new[] { -1.2f, 1.2f }) mk.Box(white, P(e, .1f, 0), new Vector3(.4f, .02f, 3.4f), rot); mk.Box(white, P(0, .1f, 0), new Vector3(2.4f, .02f, .4f), rot);
                    for (var k = 0; k < 8; k++) mk.Blob(beaconM, o + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * (half - 1.3f) + up * .2f, Vector3.one * .12f, rng, 0f); mk.Prism(steel, P(half - 1.2f, 0, half - 1.2f), up, .04f, 3.4f, 5); mk.Cone(GlowM(new Color(1f, .5f, .2f), 1.2f), P(half - 1.2f, 3.1f, half - 1.2f), .22f, .9f, 6); break;
                case 4:   // the party that ended an hour ago
                    for (var k = 0; k < 26; k++) mk.Box(GlowM(Neons[k % 5], 1.1f), P((float)rng.NextDouble() * (half * 2f - 3f) - half + 1.5f, .03f, (float)rng.NextDouble() * (half * 2f - 3f) - half + 1.5f), new Vector3(.12f, .01f, .05f), Quaternion.Euler(0, k * 37f, 0));
                    for (var k = 0; k < 9; k++) { var b = P((float)rng.NextDouble() * 8f - 4f, 0, (float)rng.NextDouble() * 8f - 4f); mk.Prism(GlowM(new Color(.3f, .7f, .4f), .5f), b + (k % 3 == 0 ? up * .04f : Vector3.zero), k % 3 == 0 ? rot * Vector3.right : up, .04f, .26f, 6); }
                    mk.Box(velvet, P(-3f, .4f, half - 2.4f), new Vector3(2.6f, .5f, 1f), rot); mk.Box(velvet, P(-3f, .8f, half - 1.9f), new Vector3(2.6f, .8f, .2f), rot); Interact.Seat(tlife.transform, P(-3f, .68f, half - 2.4f), -f, "Sit on the sofa").seatHeight = .64f; Col(P(-3f, .5f, half - 2.2f), new Vector3(2.6f, 1f, 1.2f), rot);
                    for (var k = 0; k < 2; k++) { mk.Box(dark, P(half - 2.2f, .6f + k * 1.2f, -half + 2f), new Vector3(1.1f, 1.16f, .8f), rot); mk.Prism(neonM[k], P(half - 2.2f, .5f + k * 1.2f, -half + 2.42f), f, .34f, .03f, 12); } Col(P(half - 2.2f, 1.2f, -half + 2f), new Vector3(1.1f, 2.4f, .8f), rot);
                    for (var k = 0; k < 4; k++) mk.Blob(GlowM(Neons[(k + 1) % 5], .7f), P(-half + 2.4f + k * .7f, .22f + (k % 2) * .1f, -half + 2.6f), new Vector3(.22f, .18f, .22f), rng, .2f); break;
                case 5:   // the bar that is closing: the stools are up on the counter
                    mk.Box(woodM, P(0, .55f, half - 2.4f), new Vector3(6f, 1.1f, .8f), rot); mk.Box(steel, P(0, 1.12f, half - 2.4f), new Vector3(6.3f, .06f, 1.1f), rot); Col(P(0, .55f, half - 2.4f), new Vector3(6f, 1.1f, .8f), rot);
                    for (var k = 0; k < 4; k++) { var s = P(-2.2f + k * 1.5f, 1.16f, half - 2.4f); mk.Prism(steel, s, up, .2f, .05f, 8); mk.Prism(steel, s, up, .03f, .7f, 4); }
                    mk.Box(dark, P(0, 1.9f, half - 1.2f), new Vector3(6f, 2.4f, .2f), rot); for (var k = 0; k < 9; k++) mk.Prism(GlowM(Neons[k % 5], .9f), P(-2.4f + k * .6f, 1.5f + (k % 2) * .5f, half - 1.34f), up, .05f, .3f, 6); ArtistBits.Label(rootT, "LAST ORDERS", P(0, 3.3f, half - 1.34f), -f, .4f, Rose, TextAnchor.MiddleCenter);
                    Folk(P(1.4f, 0, half - 1.7f), -f, Resident.Pose.Work, new[] { "We're closing. We've been closing since midnight. Sit down.", "I put the stools up and people take them down again. It's a kind of friendship.", "One for the walk home? The walk home's a glass bridge over nothing. Make it a small one." }); break;
                default:  // a mattress under a water tower, and the stars
                    WaterTower(ctx, mk, P(-half + 3.4f, 0, half - 3.4f), 1.2f, kind); mk.Box(white, P(1.6f, .14f, -1f), new Vector3(1.4f, .24f, 2f), rot * Quaternion.Euler(0, 16f, 0)); mk.Box(velvet, P(1.6f, .3f, -1.2f), new Vector3(1.3f, .1f, 1.3f), rot * Quaternion.Euler(0, 16f, 0)); mk.Blob(white, P(1.5f, .34f, -.2f), new Vector3(.34f, .1f, .2f), rng, .2f);
                    Interact.Seat(tlife.transform, P(1.6f, .32f, -1f), rot * Quaternion.Euler(0, 16f, 0) * Vector3.forward, "Lie down and look up").seatHeight = .26f; mk.Box(dark, P(2.9f, .14f, -.2f), new Vector3(.3f, .28f, .2f), rot); mk.Blob(lampM, P(2.9f, .36f, -.2f), Vector3.one * .05f, rng, 0f); break;
            }
        }
    }

    /// The Hole has no bottom: the dark that comes up it, and what happens to whoever jumps in.
    public class VelvetFall : MonoBehaviour
    {
        public Vector3 center, landing, sayAt; public float radius, below; public System.Func<Vector3, bool> where; SimplePlayerController player; float last = -99f; int falls;

        /// Layers of dark across the shaft, each hiding a little more, and under them nothing at all.
        public void Dark(Vector3 at, float r, float top, float bottom)
        {
            var veil = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "The dark in the Hole" };
            veil.SetColor("_BaseColor", new Color(.01f, .01f, .03f, .42f)); veil.SetFloat("_Surface", 1f); veil.SetFloat("_Blend", 0f);
            veil.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); veil.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            veil.SetFloat("_ZWrite", 0f); veil.SetFloat("_Cull", 0f); veil.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); veil.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            var black = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "No bottom" }; black.SetColor("_BaseColor", new Color(.004f, .004f, .012f));
            const int N = 7; for (var k = 0; k <= N; k++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = k == N ? "No bottom" : "Dark"; Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
                g.transform.position = new Vector3(at.x, Mathf.Lerp(top, Mathf.Max(bottom, top - 30f), k / (float)N), at.z); g.transform.localScale = new Vector3(r * 2f, .01f, r * 2f);
                var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = k == N ? black : veil; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            }
        }

        void Update()
        {
            if (player == null) player = FindAnyObjectByType<SimplePlayerController>();
            if (player == null || Time.time - last < 3f) return; var p = player.transform.position;
            if (p.y > below) return; float dx = p.x - center.x, dz = p.z - center.z; if (dx * dx + dz * dz > radius * radius) return; if (where != null && !where(p)) return;
            // through the dark, and out of the sky over the pool
            last = Time.time; player.Warp(landing);
            if (falls++ < 2) PlaceTools.Say(transform, sayAt, "The dark lets go of you. The whole city is underneath, lit, and the pool is coming up.", 6f);
        }
    }
}
