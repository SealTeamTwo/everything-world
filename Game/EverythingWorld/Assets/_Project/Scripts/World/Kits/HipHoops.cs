using EverythingWorld.Activities;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Hip-Hop Boroughs' activity (part of HipKit, 2026-11-17): basketball. By the Block's half court stand a ball
    /// rack to press E on and a little model of a full street court. Press E and you shrink onto that court
    /// (HoopsGame): the real thing, built out of sight high over the Boroughs in the same golden light, with six
    /// baskets that each keep count.
    public partial class HipKit
    {
        void HoopsCourt(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var ct = new PlaceFrame(c - D * 10f - S * 7.5f + up * .2f, (D + S * .6f).normalized);

            // the court itself, out of sight: floor, fence, baskets, lights
            var big = new GameObject("Basketball court (the size you play it)"); big.transform.SetParent(ctx.Root, false); big.transform.position = c + up * 600f;
            var bm = new MeshKit(); HoopsBuild.Court(bm, 1f); bm.Box(brickM, new Vector3(0, -31f, 0), new Vector3(HoopsBuild.W + 5.8f, 60f, HoopsBuild.L + 5.8f));   // it is the roof of a tall brick block
            bm.BuildSingle("Court", big.transform, false);
            void Solid(Vector3 at, Vector3 size) { var g = new GameObject("Court wall"); g.transform.SetParent(big.transform, false); g.transform.localPosition = at; g.AddComponent<BoxCollider>().size = size; }
            float W = HoopsBuild.W, L = HoopsBuild.L;
            Solid(new Vector3(0, -.5f, 0), new Vector3(W + 8f, 1f, L + 8f));
            foreach (var e in new[] { -1f, 1f }) { Solid(new Vector3(e * (W / 2f + 2.9f), 8f, 0), new Vector3(1f, 16f, L + 8f)); Solid(new Vector3(0, 8f, e * (L / 2f + 2.9f)), new Vector3(W + 8f, 16f, 1f)); }
            var hoops = HoopsBuild.Hoops();
            foreach (var h in hoops)
            {
                var hm = new MeshKit(); HoopsBuild.Basket(hm, h, 1f); var go = hm.BuildSingle("Basket: " + h.name, big.transform, false); go.transform.localPosition = h.ring; h.t = go.transform;
                for (var k = 0; k < 12; k++) { var sc = go.AddComponent<SphereCollider>(); sc.radius = .04f; sc.center = Quaternion.Euler(0, k * 30f, 0) * Vector3.forward * (h.r + .02f); }
                if (h.board) { var kk = HoopsBuild.K(h); var bc = go.AddComponent<BoxCollider>(); bc.center = -h.face * (h.r + .16f) + up * (.45f * kk); bc.size = Mathf.Abs(h.face.x) > .5f ? new Vector3(.08f, 1.1f * kk, 1.8f * kk) : new Vector3(1.8f * kk, 1.1f * kk, .08f); }
                if (h.seconds > 0f) go.AddComponent<Rigidbody>().isKinematic = true;
                var at = h.board ? h.ring - h.face * (h.r + .1f) + up * (1.15f * HoopsBuild.K(h) + .4f) : h.ring + up * 1.7f; var facing = h.board ? h.face : Vector3.back;
                h.counter = ArtistBits.Label(go.transform, "0", big.transform.TransformPoint(at), facing, .8f, Gold, TextAnchor.MiddleCenter);
                var nm = ArtistBits.Label(go.transform, h.name, big.transform.TransformPoint(at + up * .7f), facing, .3f, Cream, TextAnchor.MiddleCenter);
                if (!h.board) { h.counter.gameObject.AddComponent<Billboard>(); nm.gameObject.AddComponent<Billboard>(); }
            }
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var l = new GameObject("Court light").AddComponent<Light>(); l.transform.SetParent(big.transform, false); l.transform.localPosition = new Vector3(x * W * .3f, 10f, z * L * .3f); l.type = LightType.Point; l.color = new Color(1f, .85f, .6f); l.range = 34f; l.intensity = 2.6f; l.shadows = LightShadows.None; }

            var game = new GameObject("The Court").AddComponent<HoopsGame>(); game.transform.SetParent(ctx.Root, false); game.transform.position = ct.o;
            game.big = big.transform; game.landScene = ctx.Island.sceneId; game.id = "hiphop"; game.landName = "the Boroughs"; game.Setup(hoops);

            // by the half court: the model on a plinth, the ball rack to press E on, a board
            const float ms = .05f; var pl = new PlaceFrame(ct.P(-2.2f, 0, 6.3f), ct.Fwd);
            mk.Box(concrete, pl.P(0, .45f, 0), new Vector3(W * ms + .5f, .9f, L * ms + .5f), pl.r); Col(pl.P(0, .5f, 0), new Vector3(W * ms + .5f, 1f, L * ms + .5f), pl.r);
            var model = new GameObject("Basketball court model"); model.transform.SetParent(ctx.Root, false); model.transform.SetPositionAndRotation(pl.P(0, .96f, 0), pl.r);
            var mm = new MeshKit(); HoopsBuild.Court(mm, ms); mm.BuildSingle("Court", model.transform, false);
            foreach (var h in hoops) { var hm = new MeshKit(); HoopsBuild.Basket(hm, h, ms); var go = hm.BuildSingle("Basket", model.transform, false); if (go != null) go.transform.localPosition = h.ring * ms; }
            {
                var b = ct.P(1.6f, 0, 6.1f); var rm = new MeshKit(); var orange = Paint(new Color(.92f, .45f, .14f));
                foreach (var e in new[] { -.55f, .55f }) { rm.Prism(iron, new Vector3(e, 0, 0), Vector3.up, .03f, 1.1f, 5); } foreach (var y in new[] { .35f, .8f }) rm.Box(iron, new Vector3(0, y, 0), new Vector3(1.2f, .03f, .3f));
                for (var k = 0; k < 3; k++) { rm.Blob(orange, new Vector3(-.36f + k * .36f, .5f, 0), Vector3.one * .13f, ctx.Rng, 0f); if (k < 2) rm.Blob(orange, new Vector3(-.18f + k * .36f, .95f, 0), Vector3.one * .13f, ctx.Rng, 0f); }
                var go = new GameObject("Ball rack"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(b, ct.r); rm.BuildSingle("Ball rack", go.transform, false);
                var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.center = new Vector3(0, .7f, 0); bc.size = new Vector3(1.4f, 1.5f, .8f); go.AddComponent<HoopsStart>().game = game; Col(b + up * .55f, new Vector3(1.2f, 1.1f, .35f), ct.r);
                mk.Box(dark, b + ct.Right * 1.5f + up * 1.2f, new Vector3(1.9f, .7f, .06f), ct.r); mk.Prism(iron, b + ct.Right * 1.5f, up, .04f, 1.2f, 5); ArtistBits.PostCollider(ctx.Root, b + ct.Right * 1.5f, 1.5f, .1f);
                game.recordText = ArtistBits.Label(ctx.Root, "", b + ct.Right * 1.5f + up * 1.2f + ct.Fwd * .04f, ct.Fwd, .1f, Cream, TextAnchor.MiddleCenter);
            }
            Shot(ctx, "the ball rack", ct.P(-1f, 2f, 11f), ct.P(0, 1f, 5.5f));
            void BigMark(string name, Vector3 cam, Vector3 look) { var m = new GameObject("Shot: " + name).transform; m.SetParent(big.transform, false); m.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized)); }
            BigMark("hoops court", big.transform.position + new Vector3(-18f, 16f, -31f), big.transform.position + up * 2f); BigMark("hoops play", big.transform.position, big.transform.position + Vector3.forward);
            big.AddComponent<GolfSkyLife>(); big.SetActive(false);
        }
    }
}
