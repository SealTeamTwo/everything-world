using EverythingWorld.Activities;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Punk Badlands' activity (part of PunkKit, 2026-11-17): skating. On a concrete plinth stands a little model
    /// of a skatepark, a drained swimming pool, with a board leaning against it; press E on the board and you shrink
    /// into the park (SkateGame): the same pool the size of a field, built out of sight high over the Badlands under
    /// its white-hot sky.
    public partial class PunkKit
    {
        void SkatePark(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c0 = ctx.Plaza; Vector3? found = null; var top = 0f;
            for (var r = 30f; r < 170f && found == null; r += 6f)
                for (var a = 0f; a < 360f && found == null; a += 20f)
                {
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var p = c0 + d * r; if (!Open(ctx, p.x, p.z, 6f)) continue;
                    float lo = float.MaxValue, hi = float.MinValue; foreach (var (x, z) in new[] { (0f, 0f), (-3f, -3f), (3f, -3f), (3f, 3f), (-3f, 3f) }) { var h = ctx.Ground(p.x + x, p.z + z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                    if (hi - lo > 1.4f) continue; found = p; top = hi + .18f;
                }
            if (found == null) { Debug.LogWarning("The Punk Badlands: no room for the skatepark"); return; }
            var at = new Vector3(found.Value.x, top, found.Value.z); var f = new PlaceFrame(at, FaceTo(at, c0)); const float PW = 5.4f, Scale = 1f / 40f;

            // a slab, a plinth, a sprayed sign
            mk.Box(concrete, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PW), f.r); solid.Box(concrete, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PW), f.r);
            mk.Box(concreteDark, f.P(0, .4f, 0), new Vector3(3f, .8f, 2.2f), f.r); BoxCol(ctx.Root, f.P(0, .5f, 0), new Vector3(3f, 1f, 2.2f), f.r);
            mk.Box(concreteDark, f.P(0, 1.5f, -PW / 2f + .2f), new Vector3(4.2f, 3f, .3f), f.r); BoxCol(ctx.Root, f.P(0, 1.5f, -PW / 2f + .2f), new Vector3(4.2f, 3f, .3f), f.r);
            ArtistBits.Label(ctx.Root, "SKATE", f.P(0, 2.3f, -PW / 2f + .37f), f.Fwd, .6f, Hot, TextAnchor.MiddleCenter); ArtistBits.Label(ctx.Root, "or don't", f.P(0, 1.7f, -PW / 2f + .37f), f.Fwd, .2f, Lime, TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, f.P(0, 2.4f, 1f), new Color(1f, .9f, .8f), 8f, 1.6f);

            // the park, out of sight, and the model of it on the plinth
            var big = new GameObject("Skatepark (the size you ride it)"); big.transform.SetParent(ctx.Root, false); big.transform.position = at + up * 600f;
            var bm = new MeshKit(); SkateGame.Park(bm, 1f); bm.BuildSingle("Park", big.transform, false);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, 0f) }) { var l = new GameObject("Park light").AddComponent<Light>(); l.transform.SetParent(big.transform, false); l.transform.localPosition = new Vector3(x * 28f, 12f, z * 18f); l.type = LightType.Point; l.color = new Color(1f, .95f, .85f); l.range = 40f; l.intensity = 1.6f; l.shadows = LightShadows.None; }
            var model = new GameObject("Skatepark model"); model.transform.SetParent(ctx.Root, false); model.transform.SetPositionAndRotation(f.P(0, .86f, 0), f.r); var mm = new MeshKit(); SkateGame.Park(mm, Scale); mm.BuildSingle("Park", model.transform, false);

            var game = new GameObject("Skating").AddComponent<SkateGame>(); game.transform.SetParent(ctx.Root, false); game.transform.position = at;
            game.big = big.transform; game.landScene = ctx.Island.sceneId; game.id = "punk"; game.landName = "the Badlands";
            var holder = new GameObject("Skatepark model scale").transform; holder.SetParent(model.transform, false); holder.localScale = Vector3.one * Scale; game.mini = holder;   // (park metres to model metres, for showing friends on the model)

            // the board: press E here; and a board for your best
            {
                var rm = new MeshKit(); rm.Box(wood, new Vector3(0, .5f, 0), new Vector3(.24f, .04f, .9f), Quaternion.Euler(-70f, 0, 0)); foreach (var e in new[] { -.3f, .3f }) rm.Box(white, Quaternion.Euler(-70f, 0, 0) * new Vector3(0, -.05f, e) + new Vector3(0, .5f, 0), new Vector3(.28f, .07f, .08f), Quaternion.Euler(-70f, 0, 0));
                var go = new GameObject("Skateboard"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(f.P(1.9f, 0, 1.1f), f.r); rm.BuildSingle("Skateboard", go.transform, false);
                var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.center = new Vector3(0, .6f, 0); bc.size = new Vector3(.9f, 1.3f, .9f); go.AddComponent<SkateStart>().game = game;
                var bp = f.P(-1.9f, 1.2f, 1.9f); mk.Box(dark, bp, new Vector3(1.9f, .7f, .06f), f.r); mk.Prism(metal, f.P(-1.9f, 0, 1.9f), up, .05f, 1.2f, 4); ArtistBits.PostCollider(ctx.Root, f.P(-1.9f, 0, 1.9f), 1.5f, .1f);
                game.recordText = ArtistBits.Label(ctx.Root, "", bp + f.Fwd * .04f, f.Fwd, .1f, new Color(.96f, .95f, .9f), TextAnchor.MiddleCenter);
            }
            Shot(ctx, "the skate plinth", f.P(3f, 2f, PW / 2f + 4f), at + up * .9f);
            void BigMark(string name, Vector3 cam, Vector3 look) { var m = new GameObject("Shot: " + name).transform; m.SetParent(big.transform, false); m.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized)); }
            BigMark("skate park from above", big.transform.position + new Vector3(-30f, 38f, -62f), big.transform.position); BigMark("skate play", big.transform.position, big.transform.position + Vector3.forward);
            big.AddComponent<GolfSkyLife>(); big.SetActive(false);
        }
    }
}
