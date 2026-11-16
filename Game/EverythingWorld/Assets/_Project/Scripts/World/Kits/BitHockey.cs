using EverythingWorld.Activities;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Game Isles' activity (part of BitKit, 2026-11-17): air hockey. A table on a checkered pad near the Arcade,
    /// a coin post to press E on, a lamp at each corner. Press E and you shrink onto the table (HockeyGame): the same
    /// table thirty times the size, glowing, out of sight high over the Isles. The real table shows the same puck
    /// and mallets, so anyone beside it can watch.
    public partial class BitKit
    {
        void HockeyTable(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; Vector3? found = null; var top = 0f;
            for (var r = 28f; r < 150f && found == null; r += 6f)
                for (var a = 10f; a < 370f && found == null; a += 20f)
                {
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var p = center + d * r; if (!Open(ctx, p.x, p.z, 6f)) continue;
                    float lo = float.MaxValue, hi = float.MinValue; foreach (var (x, z) in new[] { (0f, 0f), (-4f, -4f), (4f, -4f), (4f, 4f), (-4f, 4f) }) { var h = ctx.Ground(p.x + x, p.z + z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                    if (hi - lo > 1.4f) continue; found = p; top = hi + .18f;
                }
            if (found == null) { Debug.LogWarning("The Game Isles: no room for the air hockey table"); return; }
            var at = new Vector3(found.Value.x, top, found.Value.z); var f = new PlaceFrame(at, FaceTo(at, center)); const float PW = 7f, PD = 7f;

            // a checkered pad, a lamp at each corner, a sign
            mk.Box(stoneM, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PD), f.r); solid.Box(stoneM, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PD), f.r);
            for (var i = 0; i < 6; i++) for (var j = 0; j < 6; j++) mk.Box((i + j) % 2 == 0 ? tileA : tileB, f.P(-2.5f + i, .01f, -2.5f + j), new Vector3(1f, .02f, 1f), f.r);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) Lamp(ctx, mk, f.P(x * (PW / 2f - .4f), 0, z * (PD / 2f - .4f)), x * z > 0);
            Sign(f.P(0, 2.7f, -PD / 2f + .3f), f.Fwd, "AIR HOCKEY", .34f, Gold); Sign(f.P(0, 2.3f, -PD / 2f + .3f), f.Fwd, "first to seven", .16f, Cyan);
            foreach (var e in new[] { -1.6f, 1.6f }) { mk.Prism(iron, f.P(e, 0, -PD / 2f + .3f), up, .06f, 3f, 4); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, -PD / 2f + .3f), 3f, .1f); }
            mk.Box(dark, f.P(0, 2.55f, -PD / 2f + .26f), new Vector3(3.4f, .9f, .06f), f.r);
            ArtistBits.PointLight(ctx.Root, f.P(0, 2.2f, 0), new Color(.6f, .9f, 1f), 9f, 2f);

            // the table (its own piece: the puck and mallets ride on it), across the pad
            var tm = new MeshKit(); HockeyGame.Table(tm, 1f, true); var table = tm.BuildSingle("Air hockey table", ctx.Root, false); table.transform.SetPositionAndRotation(f.P(0, .82f, 0), f.r * Quaternion.Euler(0, 90f, 0));
            Col(f.P(0, .42f, 0), new Vector3(HockeySim.L + .3f, .84f, HockeySim.W + .3f), f.r);

            // the table again, thirty times the size, glowing over a dark floor
            const float s = 30f; var big = new GameObject("Air hockey table (the size you play it)"); big.transform.SetParent(ctx.Root, false); big.transform.position = at + up * 600f;
            var bm = new MeshKit(); HockeyGame.Table(bm, s, true); bm.Prism(WorldMaterials.Solid(new Color(.05f, .05f, .1f)), new Vector3(0, -.82f * s - .5f, 0), up, 240f, .5f, 40);
            for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; bm.Box(GlowM(k % 2 == 0 ? Cyan : Pink, 1.6f), new Vector3(Mathf.Cos(a) * 120f, -.82f * s + .1f, Mathf.Sin(a) * 120f), new Vector3(8f, .2f, 8f)); }   // a ring of lit tiles on the floor far below
            bm.BuildSingle("Table", big.transform, false);
            foreach (var e in new[] { -1f, 0f, 1f }) { var l = new GameObject("Table light").AddComponent<Light>(); l.transform.SetParent(big.transform, false); l.transform.localPosition = new Vector3(0, 22f, e * 24f); l.type = LightType.Point; l.color = new Color(.75f, .9f, 1f); l.range = 70f; l.intensity = 4f; l.shadows = LightShadows.None; }

            var game = new GameObject("Air hockey").AddComponent<HockeyGame>(); game.transform.SetParent(ctx.Root, false); game.transform.position = at;
            game.big = big.transform; game.small = table.transform; game.bigScale = s; game.landScene = ctx.Island.sceneId; game.id = "bit"; game.landName = "the Game Isles";

            // the coin post: press E here; and a board for your wins
            {
                var b = f.P(2.4f, 0, 2.2f); var rm = new MeshKit(); rm.Box(dark, new Vector3(0, .55f, 0), new Vector3(.34f, 1.1f, .3f)); rm.Box(GlowM(Gold, 1.6f), new Vector3(0, .9f, .16f), new Vector3(.2f, .12f, .02f)); rm.Box(GlowM(Pink, 1.8f), new Vector3(0, 1.16f, 0), new Vector3(.38f, .1f, .34f));
                var go = new GameObject("Coin post"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(b, f.r); rm.BuildSingle("Coin post", go.transform, false);
                var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.center = new Vector3(0, .7f, 0); bc.size = new Vector3(.9f, 1.5f, .9f); go.AddComponent<HockeyStart>().game = game; Col(b + up * .55f, new Vector3(.34f, 1.1f, .3f), f.r);
                var bp = f.P(-2.6f, 1.2f, 2.4f); mk.Box(dark, bp, new Vector3(1.9f, .7f, .06f), f.r); mk.Prism(iron, f.P(-2.6f, 0, 2.4f), up, .05f, 1.2f, 4); ArtistBits.PostCollider(ctx.Root, f.P(-2.6f, 0, 2.4f), 1.5f, .1f);
                game.recordText = ArtistBits.Label(ctx.Root, "", bp + f.Fwd * .04f, f.Fwd, .1f, Gold, TextAnchor.MiddleCenter);
            }
            Folk(f.P(-2.2f, 0, -1.6f), f.Right, Resident.Pose.Stand, new[] { "Put a coin in. Well, press the post. The coin's implied.", "The far mallet plays itself till a friend takes it. It's not bad. It's not good either.", "Win one and the table's radio plays everything. Everything!" });
            Mark(at, 6f);
            Shot(ctx, "the air hockey table", f.P(3.5f, 1.8f, PD / 2f + 4.5f), at + up * .9f);
            void BigMark(string name, Vector3 cam, Vector3 look) { var m = new GameObject("Shot: " + name).transform; m.SetParent(big.transform, false); m.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized)); }
            BigMark("hockey play", big.transform.position, big.transform.position + Vector3.forward);
            big.AddComponent<GolfSkyLife>(); big.SetActive(false);
        }
    }
}
