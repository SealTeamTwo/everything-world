using EverythingWorld.Activities;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Side Pocket (part of JazzKit, 2026-11-17): the Old Jazz Quarter's activity, pool. An open-sided hall on a
    /// stone footing near the square: iron posts, a flat dark roof, white neon, a pool table under a three-shade lamp,
    /// a cue rack to press E on. Press E and you shrink onto the table (PoolGame): the same table built thirty times
    /// the size, out of sight high over the Quarter, in a great dark room under its lamp. The real table shows the
    /// same balls, so anyone standing beside it can watch the game.
    public partial class JazzKit
    {
        const float PoolSky = 600f, PoolBig = 30f;

        void PoolHall(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var gc = life.globeAt; Vector3? found = null; var top = 0f;
            for (var r = 34f; r < 170f && found == null; r += 6f)
                for (var a = 0f; a < 360f && found == null; a += 20f)
                {
                    var d = Quaternion.Euler(0, a, 0) * U; var p = center + d * r;
                    if (!Open(ctx, p.x, p.z, 6f) || new Vector2(p.x - gc.x, p.z - gc.z).magnitude > GlobeR - 26f) continue;
                    float lo = float.MaxValue, hi = float.MinValue; foreach (var (x, z) in new[] { (0f, 0f), (-5f, -4f), (5f, -4f), (5f, 4f), (-5f, 4f) }) { var h = ctx.Ground(p.x + x, p.z + z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                    if (hi - lo > 1.4f) continue;
                    found = p; top = hi + .18f;
                }
            if (found == null) { Debug.LogWarning("The Old Jazz Quarter: no room for The Side Pocket (pool)"); return; }
            var at = new Vector3(found.Value.x, top, found.Value.z); var f = new PlaceFrame(at, FaceTo(at, center)); const float PW = 9.6f, PD = 7.6f, H = 3.2f;

            // the footing, the posts, the roof, the neon
            mk.Box(stoneM, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PD), f.r); solid.Box(stoneM, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PD), f.r);
            mk.Box(cobble, f.P(0, .01f, 0), new Vector3(PW - .5f, .02f, PD - .5f), f.r);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var q = f.P(x * (PW / 2f - .3f), 0, z * (PD / 2f - .3f)); mk.Prism(iron, q, up, .09f, H, 8); ArtistBits.PostCollider(ctx.Root, q, H, .12f); }
            mk.Box(dark, f.P(0, H + .08f, 0), new Vector3(PW + .8f, .16f, PD + .8f), f.r);
            Sign(f.P(0, H - .35f, PD / 2f + .42f), f.Fwd, "THE SIDE POCKET", .3f, Paper); mk.Box(dark, f.P(0, H - .35f, PD / 2f + .36f), new Vector3(4.4f, .6f, .06f), f.r); halos.Add(f.P(0, H - .35f, PD / 2f + .9f), 2f, Paper, .25f);
            // the lamp over the table: a brass bar, three dark shades, light under each
            mk.Box(lampM, f.P(0, 2.25f, 0), new Vector3(.05f, .05f, 2.2f), f.r); foreach (var e in new[] { -.9f, 0f, .9f }) { mk.Prism(iron, f.P(0, 2.25f, e), up, .012f, H - 2.25f, 4); mk.Cone(dark, f.P(0, 1.95f, e), .34f, .3f, 10); mk.Blob(lampM, f.P(0, 1.97f, e), new Vector3(.12f, .05f, .12f), ctx.Rng, 0f); }
            ArtistBits.PointLight(ctx.Root, f.P(0, 1.8f, 0), new Color(1f, .9f, .7f), 9f, 2.4f); halos.Add(f.P(0, 1.6f, 0), 2.6f, Brass, .3f);

            // the table (its own piece: the balls ride on it), along the hall
            var tm = new MeshKit(); PoolBuild.Table(tm, 1f, true); var table = tm.BuildSingle("Pool table", ctx.Root, false); table.transform.SetPositionAndRotation(f.P(0, .82f, 0), f.r);
            Col(f.P(0, .42f, 0), new Vector3(PoolSim.W + .5f, .84f, PoolSim.L + .5f), f.r);

            // the table again, thirty times the size, in a great dark room: its lamp far overhead, a floor far below
            var big = new GameObject("Pool table (the size you play it)"); big.transform.SetParent(ctx.Root, false); big.transform.position = at + up * PoolSky;
            var bm = new MeshKit(); PoolBuild.Table(bm, PoolBig, true); var s = PoolBig;
            // (the lamp hangs well above where the camera can climb, so its shades never come between you and the table)
            bm.Box(lampM, new Vector3(0, 128f, 0), new Vector3(1.2f, 1.2f, 2.2f * s)); foreach (var e in new[] { -.9f, 0f, .9f }) { bm.Prism(iron, new Vector3(0, 128f, e * s), up, .3f, 60f, 6); bm.Cone(dark, new Vector3(0, 118f, e * s), .4f * s, .34f * s, 14); bm.Blob(lampM, new Vector3(0, 118.6f, e * s), new Vector3(4.2f, 1.4f, 4.2f), ctx.Rng, 0f); }
            bm.Prism(WorldMaterials.Surface(new Color(.1f, .08f, .07f), World.Surface.Wood), new Vector3(0, -.82f * s - .5f, 0), up, 260f, .5f, 40);
            bm.Box(WorldMaterials.Surface(new Color(.3f, .5f, .9f), World.Surface.Painted), new Vector3(PoolSim.W / 2f * s + 4.2f, .05f * s + .55f, -18f), new Vector3(1.1f, 1.1f, 1.1f), Quaternion.Euler(0, 24f, 0));   // the chalk, left on the rail
            bm.BuildSingle("Table", big.transform, false);
            foreach (var e in new[] { -.9f, 0f, .9f }) { var l = new GameObject("Table lamp").AddComponent<Light>(); l.transform.SetParent(big.transform, false); l.transform.localPosition = new Vector3(0, 16f, e * s); l.type = LightType.Point; l.color = new Color(1f, .92f, .75f); l.range = 75f; l.intensity = 5f; l.shadows = LightShadows.None; }

            var game = new GameObject("The Side Pocket").AddComponent<PoolGame>(); game.transform.SetParent(ctx.Root, false); game.transform.position = at;
            game.big = big.transform; game.small = table.transform; game.bigScale = PoolBig; game.landScene = ctx.Island.sceneId; game.id = "jazz"; game.landName = "the Jazz Quarter";

            // the cue rack: press E here
            {
                var b = f.P(PW / 2f - 1.1f, 0, -PD / 2f + 1.2f); var rm = new MeshKit();
                rm.Box(dark, new Vector3(0, .9f, 0), new Vector3(1.1f, .08f, .18f)); rm.Box(dark, new Vector3(0, .12f, 0), new Vector3(1.1f, .1f, .26f)); foreach (var e in new[] { -.5f, .5f }) rm.Box(dark, new Vector3(e, .5f, 0), new Vector3(.07f, 1f, .1f));
                for (var k = 0; k < 4; k++) { rm.Prism(lampM, new Vector3(-.36f + k * .24f, .17f, .02f), new Vector3(0, 1f, -.03f), .017f, 1.4f, 6, .45f); rm.Prism(WorldMaterials.Solid(new Color(.9f, .9f, .92f)), new Vector3(-.36f + k * .24f, 1.57f, -.022f), up, .009f, .03f, 5); }
                var go = new GameObject("Cue rack"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(b, Quaternion.LookRotation(-f.Right)); rm.BuildSingle("Cue rack", go.transform, false);
                var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.center = new Vector3(0, .85f, 0); bc.size = new Vector3(1.3f, 1.8f, .7f); go.AddComponent<PoolStart>().game = game;
                Col(b + up * .5f, new Vector3(.3f, 1f, 1.1f), f.r);
            }
            // a board for your best, two stools, somebody watching
            {
                var bp = f.P(-PW / 2f + .35f, 1.7f, 0); mk.Box(dark, bp, new Vector3(.06f, .9f, 2.6f), f.r);
                game.recordText = ArtistBits.Label(ctx.Root, "", bp + f.Right * .05f, f.Right, .1f, Paper, TextAnchor.MiddleCenter);
            }
            foreach (var e in new[] { -1.6f, 1.6f }) { var sp = f.P(-PW / 2f + 1.3f, 0, e); mk.Prism(iron, sp, up, .04f, .72f, 5); mk.Prism(dark, sp + up * .72f, up, .19f, .06f, 10); ArtistBits.PostCollider(ctx.Root, sp, .75f, .17f); Interact.Seat(tlife.transform, sp + up * .8f, f.Right, "Sit and watch"); }
            Folk(f.P(PW / 2f - 1.2f, 0, 1.6f), -f.Right, Resident.Pose.Stand, new[] { "Take a cue. It's a bigger table than it looks, once you're on it.", "Beat somebody, or clear it in thirty, and the radio in there plays everything.", "The black goes last. It always goes last. That's the whole of the law." });
            Mark(at, 7.5f);
            Shot(ctx, "the side pocket", f.P(-3.5f, 1.6f, PD / 2f + 5.5f), at + up * 1f);
            void BigMark(string name, Vector3 cam, Vector3 look) { var m = new GameObject("Shot: " + name).transform; m.SetParent(big.transform, false); m.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized)); }
            BigMark("pool table from the end", big.transform.position + new Vector3(0, 22f, -62f), big.transform.position); BigMark("pool play", big.transform.position, big.transform.position + Vector3.forward);
            big.AddComponent<GolfSkyLife>(); big.SetActive(false);
        }
    }
}
