using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Roots Bay under a tree of life (part of RootsKit). The island town is kept as it is, with the feeling it
    /// has (the Yard, Bay Road, the bright morning after rain); the tree is the thing that does not belong to it.
    /// - One colossal tree stands in the bay past the Sound System, its trunk going up through the clouds, its crown
    ///   above them: it is built under the land's life script, so it is never hidden with distance and can be seen
    ///   from every island on that side.
    /// - Its roots come in over the water and through the town in great arches, touching down between the houses
    ///   and rising again, as bridges overhead and shelter underneath, hung with lanterns. They only ever touch
    ///   down on open ground, so no path is closed.
    /// - One root is broad and flat enough to walk: up from the beach, over the water, to a ledge round the trunk
    ///   and a hollow in it where the tree's keeper sits.
    public partial class RootsKit
    {
        Material barkGlow, barkDark;

        void Tree(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(1973);
            barkGlow = WorldMaterials.Glow(new Color(.46f, .32f, .2f), .14f); barkDark = WorldMaterials.Glow(new Color(.32f, .22f, .15f), .1f);
            var beach = new Vector3(center.x + D.x * (roadL + 12f), 0, center.z + D.z * (roadL + 12f));
            // where it stands: out in the bay beyond the beach, a little to one side of the road's line; in the sea, or not at all
            var B = Vector3.zero; var found = false;
            foreach (var (d, s) in new[] { (100f, 24f), (100f, -30f), (84f, 40f), (120f, 0f), (84f, -50f), (140f, 30f) })
            {
                var p = beach + D * d + S * s; var sea = true; for (var k = 0; k < 8 && sea; k++) { var a = k * Mathf.PI / 4f; if (ctx.Ground(p.x + Mathf.Cos(a) * 26f, p.z + Mathf.Sin(a) * 26f) > waterY - .3f) sea = false; }
                if (sea) { B = p; found = true; break; }
            }
            if (!found) { Debug.Log("The Roots Bay: no open water was found for the tree"); return; }
            B.y = waterY - 3f;
            // ---- the trunk, built under the land's life so it is never hidden by distance
            var tk = new MeshKit(); Vector3 Mid(float y) => B + up * y + S * (6f * Mathf.Sin(y / 90f)) + D * (4f * Mathf.Sin(y / 60f + 1f));
            float[] ys = { 0f, 7f, 18f, 45f, 90f, 140f, 190f, 235f, 262f }; float[] rs = { 26f, 19f, 15f, 12.5f, 10.5f, 9f, 7.6f, 6f, 3.4f };
            for (var i = 0; i + 1 < ys.Length; i++) { var a = Mid(ys[i]); var b = Mid(ys[i + 1]); tk.Prism(i % 2 == 0 ? barkGlow : barkDark, a, b - a, rs[i], (b - a).magnitude + .5f, 14, rs[i + 1] / rs[i], i * 13f, false); }
            // buttresses flaring out into the water
            for (var k = 0; k < 9; k++) { var dir = Quaternion.Euler(0, k * 40f + 12f, 0) * Vector3.forward; var foot = B + dir * (30f + (k % 3) * 5f) - up * 1f; var top = Mid(26f + (k % 2) * 8f) + dir * 11f; tk.Prism(barkDark, foot, top - foot, 5.2f, (top - foot).magnitude, 6, .4f); }
            // the crown, above the clouds: great boughs, and leaf by the acre
            var leafA = WorldMaterials.Glow(new Color(.16f, .52f, .26f), .2f); var leafB = WorldMaterials.Glow(new Color(.28f, .64f, .24f), .22f); var bloom = WorldMaterials.Glow(new Color(1f, .8f, .25f), .6f);
            for (var k = 0; k < 11; k++)
            {
                var dir = (Quaternion.Euler(0, k * 32.7f, 0) * Vector3.forward + up * (.25f + (k % 3) * .22f)).normalized; var from = Mid(170f + (k % 4) * 20f); var len = 80f + (k % 3) * 26f; var tip = from + dir * len;
                tk.Prism(barkGlow, from, dir, 4.6f - (k % 3) * .6f, len, 7, .28f);
                tk.Blob(k % 2 == 0 ? leafA : leafB, tip + up * 6f, new Vector3(46f, 18f, 46f), rng, .2f); tk.Blob(k % 2 == 0 ? leafB : leafA, from + dir * len * .62f + up * 12f, new Vector3(36f, 15f, 36f), rng, .2f);
                if (k % 3 == 0) tk.Blob(bloom, tip + up * 20f, new Vector3(9f, 6f, 9f), rng, .25f);
            }
            tk.Blob(leafA, Mid(262f) + up * 8f, new Vector3(60f, 24f, 60f), rng, .2f); tk.Blob(leafB, Mid(240f), new Vector3(52f, 18f, 52f), rng, .2f); tk.Blob(bloom, Mid(262f) + up * 30f, new Vector3(12f, 8f, 12f), rng, .25f);
            var tgo = tk.BuildSingle("The tree of life", life.transform, false); if (tgo != null) tgo.isStatic = false;
            BoxCol(ctx.Root, B + up * 30f, new Vector3(26f, 70f, 26f), Quaternion.identity);
            halos.Add(Mid(20f) - D * 14f, 16f, Gold, .22f);

            // ---- the roots: in over the water and through the town in arches, touching down only on open ground
            var lantern = new[] { WorldMaterials.Glow(Gold, 1.6f), WorldMaterials.Glow(Red, 1.4f), WorldMaterials.Glow(new Color(.3f, .9f, .4f), 1.4f) }; var roots = 0; var arches = 0;
            float[] lanes = { -78f, -40f, 44f, 86f, -112f };
            for (var k = 0; k < lanes.Length; k++)
            {
                // touchdowns, coming inland from the beach; each one moved sideways until it stands clear of everything
                var downs = new List<Vector3>();
                for (var d = roadL + 2f - (k % 2) * 10f; d > 16f; d -= 52f + (k % 3) * 9f)
                {
                    var okAt = Vector3.zero; var ok = false;
                    foreach (var shift in new[] { 0f, 9f, -9f, 18f, -18f, 27f }) { var p = Wp(d, lanes[k] * Mathf.Lerp(1f, .55f, 1f - d / roadL) + shift); if (Mathf.Abs(Vector3.Dot(p - center, S)) < RoadW / 2f + 5f) continue; if (Open(ctx, p.x, p.z, 4f) && ctx.Ground(p.x, p.z) > waterY + .8f) { okAt = ctx.OnGround(p.x, p.z); ok = true; break; } }
                    if (ok) downs.Add(okAt);
                }
                if (downs.Count == 0) continue; roots++;
                var dirOut = (new Vector3(downs[0].x, 0, downs[0].z) - new Vector3(B.x, 0, B.z)).normalized; var prev = Mid(13f) + dirOut * 13f; var rad = 3.4f;
                for (var i = 0; i < downs.Count; i++)
                {
                    var to = downs[i] - up * .8f; var span = Vector3.Distance(new Vector3(prev.x, 0, prev.z), new Vector3(to.x, 0, to.z)); var rise = Mathf.Clamp(span * .2f, 7f, 15f); var r1 = Mathf.Max(.7f, rad * .78f);
                    var pts = new List<Vector3>(); const int N = 14; for (var q = 0; q <= N; q++) { var t = q / (float)N; var p = Vector3.Lerp(prev, to, t); p.y += Mathf.Sin(t * Mathf.PI) * rise; pts.Add(p); }
                    for (var q = 0; q < N; q++)
                    {
                        var a = pts[q]; var b = pts[q + 1]; var r = Mathf.Lerp(rad, r1, q / (float)N); mk.Prism(q % 2 == 0 ? barkGlow : barkDark, a, b - a, r, (b - a).magnitude + r * .3f, 7, Mathf.Lerp(rad, r1, (q + 1) / (float)N) / r, q * 20f, false);
                        var mid = (a + b) / 2f; var gy = Mathf.Max(ctx.Ground(mid.x, mid.z), waterY); if (mid.y - r - gy < 2.6f && gy > waterY + .2f) BoxCol(ctx.Root, mid, new Vector3(r * 1.7f, r * 1.7f, (b - a).magnitude), Quaternion.LookRotation((b - a).normalized));
                        // lanterns hang from the undersides, one colour after another
                        if (q % 4 == 2 && mid.y - r - gy > 4.5f) { var lp = mid - up * (r + 1.4f); mk.Prism(dark, mid - up * r, -up, .02f, 1.2f, 3); mk.Blob(lantern[(q + k + i) % 3], lp, new Vector3(.28f, .36f, .28f), rng, 0f); halos.Add(lp, 1.8f, (q + k + i) % 3 == 0 ? Gold : (q + k + i) % 3 == 1 ? Red : Green, .4f); }
                    }
                    // where it touches down: a knuckle of root, and smaller roots running off from it into the ground
                    mk.Blob(barkDark, downs[i] + up * .2f, new Vector3(r1 * 1.5f, r1 * 1.1f, r1 * 1.5f), rng, .2f); BoxCol(ctx.Root, downs[i] + up * (r1 * .5f), new Vector3(r1 * 2.2f, r1 * 1.8f, r1 * 2.2f), Quaternion.identity); Mark(downs[i], r1 * 2f + 1f);
                    for (var q = 0; q < 3; q++) { var rd = Quaternion.Euler(0, q * 120f + k * 40f + i * 25f, 0) * Vector3.forward; var e = downs[i] + rd * (r1 * 3.4f); e.y = ctx.Ground(e.x, e.z) - .5f; mk.Prism(barkGlow, downs[i] + up * (r1 * .4f), e - (downs[i] + up * (r1 * .4f)), r1 * .5f, (e - downs[i]).magnitude, 5, .3f); }
                    ArtistBits.PointLight(ctx.Root, downs[i] + up * 3f, Gold, 14f, 1.1f);
                    prev = downs[i] - up * .8f; rad = r1; arches++;
                }
            }

            // ---- the root you can walk: up from the beach, over the water, to a ledge round the trunk
            var startG = ctx.OnGround(beach.x + S.x * 15f - D.x * 2f, beach.z + S.z * 15f - D.z * 2f); const float LedgeY = 15f; var ledgeMid = Mid(LedgeY); ledgeMid.y = waterY + LedgeY;
            var toTrunk = new Vector3(ledgeMid.x - startG.x, 0, ledgeMid.z - startG.z); var runLen = toTrunk.magnitude - 20.4f; toTrunk.Normalize(); var end = startG + toTrunk * runLen; end.y = ledgeMid.y; var side = Vector3.Cross(up, toTrunk);
            var deck = WorldMaterials.Glow(new Color(.56f, .42f, .28f), .12f); const int M = 26; var wp = new List<Vector3>();
            for (var q = 0; q <= M; q++) { var t = q / (float)M; var p = Vector3.Lerp(startG - up * .3f, end, t) + side * (Mathf.Sin(t * Mathf.PI * 2f) * 2.2f); wp.Add(p); }
            for (var q = 0; q < M; q++)
            {
                var a = wp[q]; var b = wp[q + 1]; var seg = b - a; var rot = Quaternion.LookRotation(seg.normalized); var mid = (a + b) / 2f;
                mk.Prism(q % 2 == 0 ? barkGlow : barkDark, a - up * 2.9f, seg, 2.9f, seg.magnitude + .6f, 8, 1f, q * 15f, false);
                mk.Box(deck, mid + up * .15f, new Vector3(4.4f, .3f, seg.magnitude + .2f), rot); solid.Box(deck, mid - up * .2f, new Vector3(4.4f, 1f, seg.magnitude + .3f), rot);
                foreach (var e in new[] { -1f, 1f }) { var ra = a + rot * Vector3.right * (e * 2.3f) + up * (.9f + .25f * Mathf.Sin(q * 1.3f + e)); var rb = b + rot * Vector3.right * (e * 2.3f) + up * (.9f + .25f * Mathf.Sin((q + 1) * 1.3f + e)); mk.Prism(barkDark, ra, rb - ra, .16f, (rb - ra).magnitude + .05f, 5); if (q % 2 == 0) mk.Prism(barkDark, a + rot * Vector3.right * (e * 2.3f), up, .12f, 1f, 4); BoxCol(ctx.Root, (ra + rb) / 2f - up * .3f, new Vector3(.3f, 1.4f, seg.magnitude), rot); }
                if (q % 5 == 2) { var lp = mid + up * 2.4f + side * 2.4f; mk.Prism(barkDark, mid + side * 2.3f + up * .9f, up, .06f, 1.6f, 4); mk.Blob(lantern[q % 3], lp, new Vector3(.24f, .32f, .24f), rng, 0f); halos.Add(lp, 1.6f, Gold, .4f); }
                Remember(mid + up * .3f, 9);
            }
            // the ledge round the trunk, a rail of rootlets, and the hollow where the keeper sits
            var ledge = new Vector3(ledgeMid.x, waterY + LedgeY, ledgeMid.z);
            for (var k = 0; k < 20; k++)
            {
                var a0 = k * 18f; var d0 = Quaternion.Euler(0, a0, 0) * Vector3.forward; var d1 = Quaternion.Euler(0, a0 + 18f, 0) * Vector3.forward; var c0 = ledge + (d0 + d1).normalized * 17.6f; var rot = Quaternion.LookRotation(Vector3.Cross(up, (d0 + d1).normalized));
                mk.Box(deck, c0 + up * .15f, new Vector3(6.4f, .3f, 6.6f), Quaternion.LookRotation((d0 + d1).normalized)); solid.Box(deck, c0 - up * .2f, new Vector3(6.4f, 1f, 6.6f), Quaternion.LookRotation((d0 + d1).normalized));
                mk.Prism(barkDark, c0 - up * .4f + (d0 + d1).normalized * 2f, (-(d0 + d1).normalized * 5f - up * 4f).normalized, .5f, 6.4f, 5, .4f);   // a knee of root holding it up
                var toRamp = Vector3.Dot((d0 + d1).normalized, -toTrunk); if (toRamp > .93f) continue;   // (the rail is open where the root comes in)
                var r0 = ledge + d0 * 20.6f + up * 1f; var r1 = ledge + d1 * 20.6f + up * 1f; mk.Prism(barkDark, r0, r1 - r0, .15f, (r1 - r0).magnitude, 5); mk.Prism(barkDark, r0 - up * 1f, up, .12f, 1f, 4); BoxCol(ctx.Root, (r0 + r1) / 2f - up * .4f, new Vector3(.3f, 1.4f, (r1 - r0).magnitude), Quaternion.LookRotation((r1 - r0).normalized));
                if (k % 4 == 1) { var lp = ledge + d0 * 20.6f + up * 2.6f; mk.Prism(barkDark, lp - up * 1.6f, up, .06f, 1.6f, 4); mk.Blob(lantern[k % 3], lp, new Vector3(.26f, .34f, .26f), rng, 0f); halos.Add(lp, 1.8f, Gold, .4f); }
            }
            var hollowDir = -toTrunk; var hc = ledge + hollowDir * 14.7f; var hrot = Quaternion.LookRotation(hollowDir);
            mk.Prism(barkDark, hc + up * 2.5f - hollowDir * .5f, hollowDir, 3.3f, .7f, 10); mk.Prism(dark, hc + up * 2.5f + hollowDir * .16f, hollowDir, 2.7f, .1f, 10);   // the hollow: a dark round in a lip of bark
            mk.Box(deck, hc + up * .5f + hollowDir * .9f, new Vector3(3.6f, .14f, 1f), hrot); mk.Box(barkDark, hc + up * .22f + hollowDir * .9f, new Vector3(3.4f, .44f, .8f), hrot); Interact.Seat(tlife.transform, hc + up * .6f + hollowDir * .9f + hrot * Vector3.right * 1.1f, hollowDir, "Sit in the hollow of the tree").seatHeight = .5f;
            var keeper = Local(); keeper.hat = Residents.Hat.None; keeper.longHair = true; keeper.hair = new Color(.86f, .86f, .82f); keeper.top = new Color(.96f, .93f, .84f);
            Folk(hc + up * .58f + hollowDir * .9f - hrot * Vector3.right * .9f, hollowDir, Resident.Pose.Sit, new[] { "It was here before the island. The island is what gathered round its roots.", "Every root you walked under on your way through town is this tree, come to see how everybody is.", "From the other islands they think it is a mountain with weather on it. Let them.", "Sit. You can hear the Sound System from here, and under it you can hear the tree. They keep the same time.", "The top? Nobody has been. The birds say there is a morning up there that never clouds over." }, keeper, null, .5f);
            ArtistBits.PointLight(ctx.Root, hc + up * 2.6f + hollowDir * 1f, Gold, 14f, 1.6f); halos.Add(hc + up * 2.4f, 3f, Gold, .4f);
            Mark(startG, 4f);
            Shot(ctx, "the tree of life from the yard", ctx.OnGround(center.x, center.z) + up * 2.2f - D * 4f, Mid(60f));
            Shot(ctx, "roots over the town", ctx.OnGround(Wp(roadL * .55f, 3f).x, Wp(roadL * .55f, 3f).z) + up * 2.2f, Mid(34f));
            Shot(ctx, "the root you can walk", startG + up * 2.2f - toTrunk * 6f, ledge + up * 4f);
            Shot(ctx, "the hollow in the tree", ledge + hollowDir * 20f + up * 2f + side * 4.5f, hc + up * 1.6f);
            Debug.Log($"The Roots Bay: the tree of life stands {Vector3.Distance(new Vector3(beach.x, 0, beach.z), new Vector3(B.x, 0, B.z)):0} m out in the bay; {roots} roots come through the town in {arches} arches");
        }
    }
}
