using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The tree of life (part of RootsKit; the ground it stands in is RootsRoots.cs). It stands in the middle of the
    /// land and the land is its roots.
    /// - The trunk is as wide as a town square and goes up through the clouds; the crown is above them, as wide as
    ///   the land. It is built under the land's life script, so it is never hidden with distance and can be seen from
    ///   every land on that side of the world.
    /// - Great buttress roots come down off the trunk onto each root of the ground, high enough to walk under, and
    ///   further out the roots throw arches over the lagoon from one to the next, hung with lanterns. Aerial roots
    ///   hang all the way down from the boughs into the water.
    /// - At the trunk's foot, facing the Yard, is a hollow where the tree's keeper sits.
    public partial class RootsKit
    {
        Material barkGlow, barkDark;

        void Tree(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(1973);
            var T = new Vector3(tree2.x, 0, tree2.y); var B = new Vector3(tree2.x, CollarY - 5f, tree2.y);
            var lantern = new[] { WorldMaterials.Glow(Gold, 1.6f), WorldMaterials.Glow(Red, 1.4f), WorldMaterials.Glow(new Color(.3f, .9f, .4f), 1.4f) };
            // ---- the trunk, built under the land's life so it is never hidden by distance
            var tk = new MeshKit(); Vector3 Mid(float y) => B + up * y + S * (16f * Mathf.Sin(y / 230f)) + D * (10f * Mathf.Sin(y / 160f));
            float[] ys = { 0f, 16f, 44f, 95f, 165f, 255f, 350f, 440f, 530f, 600f, 640f }; float[] rs = { 68f, 59f, 50f, 43f, 37.5f, 32f, 27f, 21.5f, 15f, 9f, 4f };
            for (var i = 0; i + 1 < ys.Length; i++) { var a = Mid(ys[i]); var b = Mid(ys[i + 1]); tk.Prism(i % 2 == 0 ? barkGlow : barkDark, a, b - a, rs[i], (b - a).magnitude + 1f, 18, rs[i + 1] / rs[i], i * 13f, false); }
            // ribs of bark up the lower trunk
            for (var k = 0; k < 15; k++) { var dir = Quaternion.Euler(0, k * 24f + 7f, 0) * Vector3.forward; var foot = Mid(0f) + dir * 64f; var top = Mid(150f + (k % 3) * 30f) + dir * 34f; tk.Prism(k % 2 == 0 ? barkDark : barkGlow, foot, top - foot, 7.5f, (top - foot).magnitude, 6, .45f, k * 11f, false); }
            solid.Prism(barkDark, B, up, 63f, 80f, 16);
            // ---- the buttress roots: down off the trunk onto each root of the ground, high enough to walk under
            var mains = new List<RootLine>(); foreach (var l in lines) if (!l.fork) mains.Add(l);
            var butts = 0;
            foreach (var l in mains)
                foreach (var e in l.great ? new[] { -1f, 1f } : new[] { butts % 2 == 0 ? 1f : -1f })
                {
                    Vector2 foot2; Vector3 dir;
                    if (l.great) { foot2 = tree2 + D2 * 104f + S2 * (e * 36f); dir = new Vector3(foot2.x - T.x, 0, foot2.y - T.z).normalized; }
                    else { var t = (l.p[3] - l.p[1]).normalized; foot2 = l.p[2] + new Vector2(-t.y, t.x) * (e * 12f); dir = new Vector3(foot2.x - T.x, 0, foot2.y - T.z).normalized; }
                    var gy = ctx.Ground(foot2.x, foot2.y); if (InFootprint(ctx, foot2.x, foot2.y, 5f)) continue;
                    Vector3 p0 = Mid(74f) + dir * 38f, p2 = new Vector3(foot2.x, gy - 1.6f, foot2.y), p1 = T + dir * 62f + up * (gy + 15f); const int N = 9; var prev = p0;
                    for (var q = 1; q <= N; q++)
                    {
                        var u = q / (float)N; var p = (1 - u) * (1 - u) * p0 + 2f * u * (1 - u) * p1 + u * u * p2; var r = Mathf.Lerp(10.5f, 4.6f, (q - 1) / (float)N); var rn = Mathf.Lerp(10.5f, 4.6f, u);
                        tk.Prism(q % 2 == 0 ? barkGlow : barkDark, prev, p - prev, r, (p - prev).magnitude + r * .4f, 8, rn / r, q * 17f, false);
                        var mid = (prev + p) / 2f; if (mid.y - r < ctx.Ground(mid.x, mid.z) + 3f) BoxCol(ctx.Root, mid, new Vector3(r * 1.6f, r * 1.6f, (p - prev).magnitude), Quaternion.LookRotation((p - prev).normalized));
                        prev = p;
                    }
                    Mark(new Vector3(foot2.x, 0, foot2.y), 7f); butts++;
                }
            // ---- the crown, above the clouds: great boughs, and leaf by the acre
            var leafA = WorldMaterials.Glow(new Color(.16f, .52f, .26f), .2f); var leafB = WorldMaterials.Glow(new Color(.28f, .64f, .24f), .22f); var bloom = WorldMaterials.Glow(new Color(1f, .8f, .25f), .6f);
            var boughTips = new List<Vector3>();
            for (var k = 0; k < 15; k++)
            {
                var dir = (Quaternion.Euler(0, k * 24.7f, 0) * Vector3.forward + up * (.16f + (k % 3) * .2f)).normalized; var from = Mid(300f + (k % 5) * 52f); var len = 170f + (k % 4) * 44f; var tip = from + dir * len;
                tk.Prism(barkGlow, from, dir, 13f - (k % 3) * 2f, len, 8, .22f);
                tk.Blob(k % 2 == 0 ? leafA : leafB, tip + up * 12f, new Vector3(104f, 36f, 104f), rng, .2f); tk.Blob(k % 2 == 0 ? leafB : leafA, from + dir * len * .58f + up * 26f, new Vector3(84f, 32f, 84f), rng, .2f);
                if (k % 3 == 0) tk.Blob(bloom, tip + up * 46f, new Vector3(18f, 12f, 18f), rng, .25f);
                if (k % 5 < 2) boughTips.Add(from + dir * len * .8f);
            }
            tk.Blob(leafA, Mid(640f) + up * 14f, new Vector3(150f, 50f, 150f), rng, .2f); tk.Blob(leafB, Mid(560f), new Vector3(128f, 40f, 128f), rng, .2f); tk.Blob(leafA, Mid(470f) + S * 30f, new Vector3(110f, 34f, 110f), rng, .2f); tk.Blob(bloom, Mid(640f) + up * 64f, new Vector3(24f, 16f, 24f), rng, .25f);
            // aerial roots, hanging all the way down from the boughs into the lagoon
            var aerial = 0;
            foreach (var tip in boughTips)
            {
                var ok = false; var foot = Vector3.zero;
                for (var k = 0; k < 10 && !ok; k++) { var q = tip + Quaternion.Euler(0, k * 36f, 0) * Vector3.forward * (k * 9f); if (ctx.Data.IslandAt(q.x, q.z) == ctx.Island && ctx.Ground(q.x, q.z) < waterY && !NearPath(q.x, q.z, 6f)) { ok = true; foot = new Vector3(q.x, waterY - 2f, q.z); } }
                if (!ok) continue;
                var top = new Vector3(tip.x, tip.y, tip.z); var prev = foot; const int N = 6;
                for (var q = 1; q <= N; q++) { var u = q / (float)N; var p = Vector3.Lerp(foot, top, u) + (S * Mathf.Sin(u * 5f + aerial) + D * Mathf.Cos(u * 4f + aerial)) * (3f * Mathf.Sin(u * Mathf.PI)); tk.Prism(q % 2 == 0 ? barkGlow : barkDark, prev, p - prev, Mathf.Lerp(1.5f, 1f, u), (p - prev).magnitude + .5f, 6, 1f, q * 30f, false); prev = p; }
                tk.Blob(barkDark, foot + up * 2.4f, new Vector3(3.4f, 2f, 3.4f), rng, .25f); ArtistBits.PostCollider(ctx.Root, foot, 9f, 1.7f); aerial++;
            }
            var tgo = tk.BuildSingle("The tree of life", life.transform, false); if (tgo != null) tgo.isStatic = false;

            // ---- arches of root over the lagoon, from each root to the next one round, hung with lanterns
            mains.Sort((a, b) => a.bearing.CompareTo(b.bearing)); var arches = 0;
            for (var k = 0; k < mains.Count; k++)
            {
                var l0 = mains[k]; var l1 = mains[(k + 1) % mains.Count]; if (mains.Count < 2) break;
                Vector2 Spot(RootLine l, int steps, Vector2 toward) { var want = 172f + steps * 26f; var best = l.p[0]; foreach (var p in l.p) if (Mathf.Abs(Vector2.Distance(p, tree2) - want) < Mathf.Abs(Vector2.Distance(best, tree2) - want)) best = p; Near(l, best, out _, out var half, out _); return best + (toward - best).normalized * (half - 3.5f); }
                for (var steps = 0; steps < 4; steps++)
                {
                    var a2 = Spot(l0, steps, l1.p[Mathf.Min(l1.p.Count - 1, 8)]); var b2 = Spot(l1, steps, a2); a2 = Spot(l0, steps, b2);
                    var span = Vector2.Distance(a2, b2); if (span < 40f || span > 260f) continue;
                    if (!Open(ctx, a2.x, a2.y, 4f) || !Open(ctx, b2.x, b2.y, 4f)) continue;
                    var a = On(ctx, a2, -1f); var b = On(ctx, b2, -1f); var rise = Mathf.Clamp(span * .2f, 12f, 34f); const int N = 16; var prev = a; var rad = Mathf.Clamp(span * .02f, 1.8f, 3.6f);
                    for (var q = 1; q <= N; q++)
                    {
                        var u = q / (float)N; var p = Vector3.Lerp(a, b, u) + up * (Mathf.Sin(u * Mathf.PI) * rise); var r = rad * (1f - .35f * Mathf.Sin(u * Mathf.PI));
                        mk.Prism(q % 2 == 0 ? barkGlow : barkDark, prev, p - prev, r, (p - prev).magnitude + r * .3f, 7, 1f, q * 20f, false);
                        var mid = (prev + p) / 2f; var gy = Mathf.Max(ctx.Ground(mid.x, mid.z), waterY);
                        if (mid.y - r - gy < 2.6f) BoxCol(ctx.Root, mid, new Vector3(r * 1.7f, r * 1.7f, (p - prev).magnitude), Quaternion.LookRotation((p - prev).normalized));
                        if (q % 2 == 0 && mid.y - r - gy > 4.5f) { var drop = 1.2f + (q % 3) * .8f; var lp = mid - up * (r + drop); mk.Prism(dark, mid - up * r, -up, .02f, drop, 3); mk.Blob(lantern[(q + k) % 3], lp, new Vector3(.3f, .4f, .3f), rng, 0f); halos.Add(lp, 2f, (q + k) % 3 == 1 ? Red : Gold, .4f); }
                        prev = p;
                    }
                    foreach (var g in new[] { a, b }) { mk.Blob(barkDark, g + up * 1.2f, new Vector3(rad * 1.5f, rad * 1.1f, rad * 1.5f), rng, .2f); BoxCol(ctx.Root, g + up * 1.4f, new Vector3(rad * 2f, rad * 2f, rad * 2f), Quaternion.identity); Mark(g, rad * 2f + 1f); }
                    if (arches == 0) Shot(ctx, "an arch of root over the lagoon", Vector3.Lerp(a, b, .5f) + up * 1.4f + new Vector3(a2.x - tree2.x, 0, a2.y - tree2.y).normalized * 40f, Vector3.Lerp(a, b, .5f) + up * rise);
                    arches++; break;
                }
            }

            // ---- the hollow at the trunk's foot, facing the Yard, where the keeper sits
            var deck = WorldMaterials.Glow(new Color(.56f, .42f, .28f), .12f);
            var hc = ctx.OnGround(T.x + D.x * 66.5f, T.z + D.z * 66.5f); var hrot = Quaternion.LookRotation(D);
            mk.Prism(barkDark, hc + up * 5.4f - D * 2.2f, D, 7.4f, 1.6f, 12); mk.Prism(dark, hc + up * 5.4f - D * .7f, D, 6f, .24f, 12);   // the hollow: a dark round in a lip of bark
            mk.Box(deck, hc + up * .5f + D * .9f, new Vector3(5.6f, .14f, 1.1f), hrot); mk.Box(barkDark, hc + up * .22f + D * .9f, new Vector3(5.4f, .44f, .9f), hrot); Col(hc + up * .3f + D * .9f, new Vector3(5.6f, .6f, 1.1f), hrot);
            Interact.Seat(tlife.transform, hc + up * .6f + D * .9f + hrot * Vector3.right * 1.6f, D, "Sit in the hollow of the tree").seatHeight = .5f;
            var keeper = Local(); keeper.hat = Residents.Hat.None; keeper.longHair = true; keeper.hair = new Color(.86f, .86f, .82f); keeper.top = new Color(.96f, .93f, .84f);
            Folk(hc + up * .58f + D * .9f - hrot * Vector3.right * 1.3f, D, Resident.Pose.Sit, new[] { "There never was an island. There is the tree, and what the tree holds up out of the water, and us.", "Every root you walk on is this tree, gone out to see how everybody is.", "From the other lands they think it is a mountain with weather on it. Let them.", "Sit. You can hear the Sound System from here, right down the Great Root. It comes up through the wood." }, keeper, null, .5f);
            foreach (var e in new[] { -1f, 1f }) { var lp = hc + hrot * Vector3.right * (e * 7.6f) + up * 3.2f + D * 1f; mk.Prism(barkDark, lp - up * 3.3f, up, .12f, 3f, 5); ArtistBits.PostCollider(ctx.Root, lp - up * 3.2f, 3f, .14f); mk.Blob(lantern[e > 0 ? 0 : 1], lp, new Vector3(.34f, .46f, .34f), rng, 0f); halos.Add(lp, 2.4f, Gold, .4f); }
            ArtistBits.PointLight(ctx.Root, hc + up * 3.4f + D * 2f, Gold, 20f, 1.8f); halos.Add(hc + up * 5.4f + D * .4f, 5f, Gold, .3f);
            var spur = new List<Vector3>(); for (var d = 69f; d <= RingR - 1.4f; d += 2f) spur.Add(ctx.OnGround(T.x + D.x * d, T.z + D.z * d)); if (spur.Count > 1) Strip(mk, spur, earthM, PathW, .13f, ctx);
            // lanterns on posts round the trunk
            for (var k = 0; k < 20; k++)
            {
                var d = Quaternion.Euler(0, k * 18f + 9f, 0) * D; var g = ctx.OnGround(T.x + d.x * (RingR + 3.4f), T.z + d.z * (RingR + 3.4f)); if (InFootprint(ctx, g.x, g.z, 1f) || NearPath(g.x, g.z, PathW / 2f + .5f) || InNoGrass(g.x, g.z, .5f)) continue;
                mk.Prism(barkDark, g - up * .3f, up, .09f, 3.1f, 5); ArtistBits.PostCollider(ctx.Root, g, 2.8f, .11f); var lp = g + up * 3f; mk.Blob(lantern[k % 3], lp, new Vector3(.26f, .34f, .26f), rng, 0f); halos.Add(lp, 1.8f, k % 3 == 1 ? Red : Gold, .4f);
                if (k % 5 == 2) ArtistBits.PointLight(ctx.Root, lp, Gold, 16f, 1.2f);
            }

            var yard = ctx.OnGround(center.x, center.z);
            Shot(ctx, "the tree of life from the yard", yard + up * 2.2f + D * 15f, Mid(170f));
            Shot(ctx, "the hollow in the tree", hc + D * 15f + up * 2.2f + S * 5f, hc + up * 3.6f);
            { var d = Quaternion.Euler(0, 100f, 0) * D; var g = ctx.OnGround(T.x + d.x * RingR, T.z + d.z * RingR); var along = Vector3.Cross(up, d); Shot(ctx, "round the trunk under the buttress roots", g + up * 2f, g + along * 40f + up * 10f - d * 10f); }
            // out on a root, in the lagoon between two, from above, and from across the water
            RootLine pick = null; foreach (var l in mains) if (!l.great && !l.bridge && (pick == null || l.p.Count > pick.p.Count)) pick = l; pick = pick ?? mains[mains.Count - 1];
            { var i = Mathf.Min(pick.p.Count - 3, 9); var g = On(ctx, pick.p[i]); var o = On(ctx, pick.p[Mathf.Min(pick.p.Count - 1, i + 8)]); Shot(ctx, "out along a root", g + up * 2.2f, o + up * 2f); Shot(ctx, "back along a root to the tree", o + up * 2.4f, Mid(110f)); }
            for (var a = 20f; a < 360f; a += 12f) { var d2 = Rot(D2, a); var q = tree2 + d2 * 215f; if (ctx.Data.IslandAt(q.x, q.y) != ctx.Island || ctx.Ground(q.x, q.y) > waterY - .2f || RootsAt(q + d2 * 24f) > 0f || RootsAt(q - d2 * 24f) > 0f) continue; Shot(ctx, "wading in the lagoon between the roots", new Vector3(q.x, waterY + 1.3f, q.y), Mid(60f)); break; }
            { var d2 = Rot(D2, 150f); Shot(ctx, "the town in the roots, from above", new Vector3(tree2.x + d2.x * 380f, 120f, tree2.y + d2.y * 380f), T + up * 20f); var d3 = Rot(D2, -25f); Shot(ctx, "the roots from above, the other side", new Vector3(tree2.x + d3.x * 420f, 150f, tree2.y + d3.y * 420f), T + up * 20f); }
            Shot(ctx, "the tree of life from out in the bay", new Vector3(center.x + D.x * (roadL + 420f), 14f, center.z + D.z * (roadL + 420f)), Mid(300f));
            Debug.Log($"The Roots Bay: the tree of life stands {ys[ys.Length - 1]:0} m tall, {butts} buttress roots, {arches} arches over the lagoon, {aerial} aerial roots");
        }
    }
}
