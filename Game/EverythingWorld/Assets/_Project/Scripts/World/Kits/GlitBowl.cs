using EverythingWorld.Activities;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Glitterlands' activity (part of GlitKit, 2026-11-17): bowling. A little model lane on a mirrored plinth, a
    /// ball return to press E on, a sign in lights. Press E and you shrink onto the lane (BowlGame): the same lane ten
    /// times the size, glitter down both sides, built out of sight high over the Glitterlands.
    public partial class GlitKit
    {
        void BowlingLane(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; Vector3? found = null; var top = 0f;
            for (var r = 30f; r < 170f && found == null; r += 6f)
                for (var a = 0f; a < 360f && found == null; a += 20f)
                {
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var p = center + d * r; if (!Open(ctx, p.x, p.z, 6f)) continue;
                    float lo = float.MaxValue, hi = float.MinValue; foreach (var (x, z) in new[] { (0f, 0f), (-3f, -3f), (3f, -3f), (3f, 3f), (-3f, 3f) }) { var h = ctx.Ground(p.x + x, p.z + z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                    if (hi - lo > 1.4f) continue; found = p; top = hi + .18f;
                }
            if (found == null) { Debug.LogWarning("The Glitterlands: no room for the bowling lane"); return; }
            var at = new Vector3(found.Value.x, top, found.Value.z); var f = new PlaceFrame(at, FaceTo(at, center)); const float PW = 5.4f, ms = .14f;

            mk.Box(floorM, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PW), f.r); solid.Box(floorM, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PW), f.r);
            mk.Box(mirror, f.P(0, .4f, 0), new Vector3(1.1f, .8f, 3.6f), f.r); Col(f.P(0, .5f, 0), new Vector3(1.1f, 1f, 3.6f), f.r);
            foreach (var e in new[] { -1.6f, 1.6f }) { mk.Prism(chrome, f.P(e, 0, -PW / 2f + .3f), up, .05f, 3.1f, 6); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, -PW / 2f + .3f), 3.1f, .1f); }
            mk.Box(dark, f.P(0, 2.7f, -PW / 2f + .26f), new Vector3(3.4f, .9f, .06f), f.r); Sign(f.P(0, 2.85f, -PW / 2f + .3f), f.Fwd, "BOWLING", .36f, Pink); Sign(f.P(0, 2.46f, -PW / 2f + .3f), f.Fwd, "ten frames of glitter", .15f, Silver);
            for (var k = 0; k < 9; k++) mk.Blob(GlowM(Disco[k % Disco.Length], 2.2f), f.P(-1.6f + k * .4f, 3.2f, -PW / 2f + .3f), Vector3.one * .08f, ctx.Rng, 0f);
            ArtistBits.PointLight(ctx.Root, f.P(0, 2.2f, .5f), Pink, 8f, 1.8f);

            // the model lane on the plinth, and the lane itself out of sight
            var model = new GameObject("Bowling lane model"); model.transform.SetParent(ctx.Root, false); model.transform.SetPositionAndRotation(f.P(0, .84f, 1.5f), f.r * Quaternion.Euler(0, 180f, 0)); var mm = new MeshKit(); BowlGame.Lane(mm, ms); mm.BuildSingle("Lane", model.transform, false);
            const float s = 10f; var big = new GameObject("Bowling lane (the size you bowl it)"); big.transform.SetParent(ctx.Root, false); big.transform.position = at + up * 600f;
            var bm = new MeshKit(); BowlGame.Lane(bm, s); bm.Blob(GlowM(Silver, 1.4f), new Vector3(0, 9f * s * .1f + 22f, BowlSim.L * s), Vector3.one * 5f, ctx.Rng, .05f);   // a mirror ball over the pins
            bm.BuildSingle("Lane", big.transform, false);
            for (var k = 0; k < 5; k++) { var l = new GameObject("Lane light").AddComponent<Light>(); l.transform.SetParent(big.transform, false); l.transform.localPosition = new Vector3(0, 14f, (k + .5f) * BowlSim.L * s / 5f + (k == 4 ? 10f : 0f)); l.type = LightType.Point; l.color = k % 2 == 0 ? new Color(1f, .8f, .95f) : new Color(.8f, .85f, 1f); l.range = 46f; l.intensity = k == 4 ? 3.2f : 2f; l.shadows = LightShadows.None; }

            var game = new GameObject("Bowling").AddComponent<BowlGame>(); game.transform.SetParent(ctx.Root, false); game.transform.position = at;
            game.big = big.transform; game.bigScale = s; game.landScene = ctx.Island.sceneId; game.id = "glit"; game.landName = "the Glitterlands";
            {
                var rm = new MeshKit(); rm.Box(chrome, new Vector3(0, .35f, 0), new Vector3(.5f, .7f, 1.1f)); for (var k = 0; k < 3; k++) rm.Blob(GlowM(Disco[k * 2 % Disco.Length], 1.2f), new Vector3(0, .82f, -.34f + k * .34f), Vector3.one * .15f, ctx.Rng, 0f);
                var go = new GameObject("Ball return"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(f.P(1.7f, 0, 1f), f.r); rm.BuildSingle("Ball return", go.transform, false);
                var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.center = new Vector3(0, .6f, 0); bc.size = new Vector3(1f, 1.4f, 1.5f); go.AddComponent<BowlStart>().game = game; Col(f.P(1.7f, .35f, 1f), new Vector3(.5f, .7f, 1.1f), f.r);
                var bp = f.P(-1.9f, 1.2f, 1.9f); mk.Box(dark, bp, new Vector3(1.9f, .7f, .06f), f.r); mk.Prism(chrome, f.P(-1.9f, 0, 1.9f), up, .05f, 1.2f, 4); ArtistBits.PostCollider(ctx.Root, f.P(-1.9f, 0, 1.9f), 1.5f, .1f);
                game.recordText = ArtistBits.Label(ctx.Root, "", bp + f.Fwd * .04f, f.Fwd, .1f, Cream, TextAnchor.MiddleCenter);
            }
            Mark(at, 5f);
            Shot(ctx, "the bowling plinth", f.P(3f, 2f, PW / 2f + 4f), at + up * .9f);
            void BigMark(string name, Vector3 cam, Vector3 look) { var m = new GameObject("Shot: " + name).transform; m.SetParent(big.transform, false); m.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized)); }
            BigMark("bowl lane", big.transform.position + new Vector3(0, 9f, -22f), big.transform.position + new Vector3(0, 0, 40f)); BigMark("bowl play", big.transform.position, big.transform.position + Vector3.forward);
            big.AddComponent<GolfSkyLife>(); big.SetActive(false);
        }
    }
}
