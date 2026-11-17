using EverythingWorld.Activities;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Synthwave Coast's activity (part of SynthKit, 2026-11-17): light bikes. A slab of glowing grid with a parked
    /// bike on it and a neon sign; press E on the bike and you shrink onto the grid (BikeGame): a square arena of
    /// dark glass ruled with light, built out of sight high over the Coast under its own sunset.
    public partial class SynthKit
    {
        void BikeGrid(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; Vector3? found = null; var top = 0f;
            for (var r = 30f; r < 160f && found == null; r += 6f)
                for (var a = 0f; a < 360f && found == null; a += 20f)
                {
                    var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var p = center + d * r; if (!Open(ctx, p.x, p.z, 6f)) continue;
                    float lo = float.MaxValue, hi = float.MinValue; foreach (var (x, z) in new[] { (0f, 0f), (-3f, -3f), (3f, -3f), (3f, 3f), (-3f, 3f) }) { var h = ctx.Ground(p.x + x, p.z + z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                    if (hi - lo > 1.4f) continue; found = p; top = hi + .18f;
                }
            if (found == null) { Debug.LogWarning("The Synthwave Coast: no room for the light bikes"); return; }
            var at = new Vector3(found.Value.x, top, found.Value.z); var f = new PlaceFrame(at, FaceTo(at, center)); const float PW = 6f;

            // the slab: dark, ruled with light; the sign
            mk.Box(dark, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PW), f.r); solid.Box(dark, f.P(0, -1.5f, 0), new Vector3(PW, 3f, PW), f.r);
            for (var k = -3; k <= 3; k++) { mk.Box(k % 2 == 0 ? cyanN : pinkN, f.P(k, .012f, 0), new Vector3(.04f, .02f, PW), f.r); mk.Box(k % 2 == 0 ? pinkN : cyanN, f.P(0, .012f, k), new Vector3(PW, .02f, .04f), f.r); }
            foreach (var e in new[] { -1.7f, 1.7f }) { mk.Prism(chrome, f.P(e, 0, -PW / 2f + .3f), up, .06f, 3.2f, 6); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, -PW / 2f + .3f), 3.2f, .1f); }
            mk.Box(dark, f.P(0, 2.7f, -PW / 2f + .26f), new Vector3(3.6f, 1f, .06f), f.r); Sign(f.P(0, 2.85f, -PW / 2f + .3f), f.Fwd, "LIGHT BIKES", .36f, Cyan); Sign(f.P(0, 2.45f, -PW / 2f + .3f), f.Fwd, "last one riding wins", .15f, Magenta);
            ArtistBits.PointLight(ctx.Root, f.P(0, 2f, 0), Magenta, 9f, 2f);

            // the arena, out of sight: a dark floor ruled with light every ten metres, a wall of light round it
            var big = new GameObject("Light bike grid (the size you ride it)"); big.transform.SetParent(ctx.Root, false); big.transform.position = at + up * 600f;
            var bm = new MeshKit(); const float H = BikeSim.Half; bm.Box(WorldMaterials.Solid(new Color(.03f, .02f, .07f), .6f), new Vector3(0, -.5f, 0), new Vector3(H * 2f + 60f, 1f, H * 2f + 60f));
            for (var k = -9; k <= 9; k++) { var m = k % 3 == 0 ? pinkN : cyanN; var w = k % 3 == 0 ? .16f : .07f; bm.Box(m, new Vector3(k * 10f, .012f, 0), new Vector3(w, .02f, H * 2f)); bm.Box(m, new Vector3(0, .012f, k * 10f), new Vector3(H * 2f, .02f, w)); }
            foreach (var e in new[] { -1f, 1f }) { bm.Box(pinkN, new Vector3(e * (H + .3f), 1.5f, 0), new Vector3(.5f, 3f, H * 2f + 1.1f)); bm.Box(pinkN, new Vector3(0, 1.5f, e * (H + .3f)), new Vector3(H * 2f + 1.1f, 3f, .5f)); }
            bm.BuildSingle("Grid", big.transform, false);

            var game = new GameObject("Light bikes").AddComponent<BikeGame>(); game.transform.SetParent(ctx.Root, false); game.transform.position = at;
            game.big = big.transform; game.landScene = ctx.Island.sceneId; game.id = "synth"; game.landName = "the Coast";

            // the parked bike: press E here; and a board for your wins
            {
                var rm = new MeshKit(); rm.Box(chrome, new Vector3(0, .5f, 0), new Vector3(.4f, .5f, 1.9f)); rm.Box(GlowM(Cyan, 2f), new Vector3(0, .5f, 0), new Vector3(.44f, .1f, 1.95f)); foreach (var e in new[] { -.75f, .75f }) rm.Prism(dark, new Vector3(-.16f, .36f, e), Vector3.right, .36f, .32f, 12);
                rm.Box(dark, new Vector3(0, .85f, -.2f), new Vector3(.3f, .16f, .7f)); rm.Box(chrome, new Vector3(0, .95f, .55f), new Vector3(.7f, .05f, .06f));
                var go = new GameObject("Light bike"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(f.P(0, .02f, .3f), f.r * Quaternion.Euler(0, 35f, 0)); rm.BuildSingle("Light bike", go.transform, false);
                var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.center = new Vector3(0, .7f, 0); bc.size = new Vector3(1.2f, 1.5f, 2.4f); go.AddComponent<BikeStart>().game = game; Col(f.P(0, .5f, .3f), new Vector3(.6f, 1f, 2f), f.r * Quaternion.Euler(0, 35f, 0));
                var bp = f.P(-2.2f, 1.2f, 2.2f); mk.Box(dark, bp, new Vector3(1.9f, .7f, .06f), f.r); mk.Prism(chrome, f.P(-2.2f, 0, 2.2f), up, .05f, 1.2f, 4); ArtistBits.PostCollider(ctx.Root, f.P(-2.2f, 0, 2.2f), 1.5f, .1f);
                game.recordText = SignFit.On(ArtistBits.Label(ctx.Root, "", bp + f.Fwd * .04f, f.Fwd, .1f, Cream, TextAnchor.MiddleCenter), 1.75f);
            }
            Mark(at, 5.5f);
            Shot(ctx, "the light bikes", f.P(3.5f, 1.8f, PW / 2f + 4.5f), at + up * .9f);
            void BigMark(string name, Vector3 cam, Vector3 look) { var m = new GameObject("Shot: " + name).transform; m.SetParent(big.transform, false); m.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized)); }
            BigMark("bikes grid", big.transform.position + new Vector3(0, 60f, -H - 50f), big.transform.position); BigMark("bikes play", big.transform.position, big.transform.position + Vector3.forward);
            big.AddComponent<GolfSkyLife>(); big.SetActive(false);
        }
    }
}
