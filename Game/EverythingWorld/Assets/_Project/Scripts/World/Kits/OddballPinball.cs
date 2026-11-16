using EverythingWorld.Activities;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Oddball Backroads' activity (part of OddballKit, 2026-11-17): pinball where you are the ball. On a plinth
    /// by the roadside stands a little model of a pinball table with a big plunger beside it; press E on the plunger
    /// and you shrink onto the table (PinballGame): the same table at full size, a long slope of ice high over the
    /// Backroads.
    public partial class OddballKit
    {
        void PinballTable(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; Vector3? found = null; var top = 0f;
            for (var r = 30f; r < 170f && found == null; r += 6f)
                for (var a = 0f; a < 360f && found == null; a += 20f)
                {
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var p = center + d * r; if (!Open(ctx, p.x, p.z, 6f)) continue;
                    float lo = float.MaxValue, hi = float.MinValue; foreach (var (x, z) in new[] { (0f, 0f), (-3f, -3f), (3f, -3f), (3f, 3f), (-3f, 3f) }) { var h = ctx.Ground(p.x + x, p.z + z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                    if (hi - lo > 1.4f) continue; found = p; top = hi + .18f;
                }
            if (found == null) { Debug.LogWarning("The Oddball Backroads: no room for the pinball table"); return; }
            var at = new Vector3(found.Value.x, top, found.Value.z); var f = new PlaceFrame(at, FaceTo(at, center)); const float PW = 5f, Scale = 1f / 30f;

            // a footing, a plinth, the sign
            mk.Box(wood, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PW), f.r); solid.Box(wood, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PW), f.r);
            mk.Box(barnRed, f.P(0, .4f, 0), new Vector3(1.3f, .8f, 2f), f.r); BoxCol(ctx.Root, f.P(0, .5f, 0), new Vector3(1.3f, 1f, 2f), f.r);
            foreach (var e in new[] { -1.5f, 1.5f }) { mk.Prism(metal, f.P(e, 0, -PW / 2f + .3f), up, .05f, 3f, 5); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, -PW / 2f + .3f), 3f, .1f); }
            mk.Box(dark, f.P(0, 2.6f, -PW / 2f + .26f), new Vector3(3.2f, .9f, .06f), f.r);
            ArtistBits.Label(ctx.Root, "PINBALL", f.P(0, 2.75f, -PW / 2f + .3f), f.Fwd, .36f, Flamingo, TextAnchor.MiddleCenter); ArtistBits.Label(ctx.Root, "you are the ball", f.P(0, 2.36f, -PW / 2f + .3f), f.Fwd, .15f, Cream, TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, f.P(0, 2f, 0), new Color(1f, .7f, .8f), 8f, 1.8f);

            // the table itself, out of sight; and the model on the plinth: a copy of it, shrunk, without lights and lettering
            var big = GolfBuild.Build(GolfCourses.Pinball, false); big.name = "Pinball table (the size you play it)"; big.transform.SetParent(ctx.Root, false); big.transform.position = at + up * 600f;
            var model = Object.Instantiate(big, ctx.Root); model.name = "Pinball table model";
            foreach (var l in model.GetComponentsInChildren<Light>(true)) Object.Destroy(l.gameObject);
            foreach (var t in model.GetComponentsInChildren<TextMesh>(true)) Object.Destroy(t.gameObject);
            // (stood the way a pinball machine stands: its top end raised and away from you, the playfield tipped toward you)
            model.transform.localScale = Vector3.one * Scale; model.transform.SetPositionAndRotation(f.P(0, .84f, .75f), f.r * Quaternion.Euler(0, 180f, 0) * Quaternion.Euler(-24f, 0, 0));

            var game = new GameObject("Pinball").AddComponent<PinballGame>(); game.transform.SetParent(ctx.Root, false); game.transform.position = at;
            game.big = big.transform; game.mini = model.transform; game.landScene = ctx.Island.sceneId; game.id = "oddball"; game.landName = "the Backroads";

            // the plunger: press E here; and a board for your best
            {
                var rm = new MeshKit(); rm.Prism(metal, new Vector3(0, .3f, 0), Vector3.up, .06f, .9f, 8); rm.Blob(GlowM(Flamingo, 1.6f), new Vector3(0, 1.3f, 0), Vector3.one * .17f, ctx.Rng, 0f); rm.Prism(dark, Vector3.zero, Vector3.up, .16f, .34f, 10);
                for (var k = 0; k < 6; k++) rm.Prism(metal, new Vector3(0, .36f + k * .1f, 0), Vector3.up, .1f, .03f, 10);
                var go = new GameObject("Plunger"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(f.P(1.5f, 0, .8f), f.r); rm.BuildSingle("Plunger", go.transform, false);
                var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.center = new Vector3(0, .8f, 0); bc.size = new Vector3(.9f, 1.7f, .9f); go.AddComponent<PinballStart>().game = game; BoxCol(ctx.Root, f.P(1.5f, .5f, .8f), new Vector3(.3f, 1f, .3f), f.r);
                var bp = f.P(-1.7f, 1.2f, 1.9f); mk.Box(dark, bp, new Vector3(1.9f, .7f, .06f), f.r); mk.Prism(metal, f.P(-1.7f, 0, 1.9f), up, .05f, 1.2f, 4); ArtistBits.PostCollider(ctx.Root, f.P(-1.7f, 0, 1.9f), 1.5f, .1f);
                game.recordText = ArtistBits.Label(ctx.Root, "", bp + f.Fwd * .04f, f.Fwd, .1f, Cream, TextAnchor.MiddleCenter);
            }
            noGrass.Add(new Vector3(at.x, 5f, at.z));
            Shot(ctx, "the pinball table", f.P(3f, 1.8f, PW / 2f + 4f), at + up * .9f);
            void BigMark(string name, Vector3 cam, Vector3 look) { var m = new GameObject("Shot: " + name).transform; m.SetParent(big.transform, false); m.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized)); }
            BigMark("pinball table from above", big.transform.position + new Vector3(0, 46f, -18f), big.transform.position + new Vector3(0, 5f, 22f)); BigMark("pinball play", big.transform.position, big.transform.position + Vector3.forward);
            big.AddComponent<GolfSkyLife>(); big.SetActive(false);
        }
    }
}
