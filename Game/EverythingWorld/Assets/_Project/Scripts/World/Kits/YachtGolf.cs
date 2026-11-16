using EverythingWorld.Activities;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Riviera Links (part of YachtKit, 2026-11-16): the owner's mini golf, in the east meadow kept empty for it.
    /// What stands in the meadow is the way in: a long stone basin of water holding a little model of the whole
    /// eighteen-hole course, two golf bags to press E on, a board, benches, lamps and a caddy. Press E and you shrink
    /// onto the course itself (GolfGame): the same course at full size, built out of sight high over the Riviera under
    /// its own sunset, standing in a calm sea. Friends who stay by the basin see each ball as a dot of light on the model.
    public partial class YachtKit
    {
        static readonly Vector2 GolfAt = new Vector2(6772, 4034);
        const float LinksScale = 1f / 50f, LinksSky = 600f;

        void GolfLinks(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var course = GolfCourses.Riviera; var up = Vector3.up;
            var g = Site(ctx, GolfAt, 16f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            float pw = course.width * LinksScale + 1.4f, pd = course.depth * LinksScale + 1.4f; const float rim = .55f;

            // the basin: a stone rim round a shallow pool, the model standing in it
            var tile = WorldMaterials.Surface(new Color(.5f, .82f, .86f), World.Surface.Painted);
            mk.Box(stone, f.P(0, rim / 2f - .2f, -(pd / 2f + .2f)), new Vector3(pw + .8f, rim + .4f, .4f), f.r); mk.Box(stone, f.P(0, rim / 2f - .2f, pd / 2f + .2f), new Vector3(pw + .8f, rim + .4f, .4f), f.r);
            mk.Box(stone, f.P(-(pw / 2f + .2f), rim / 2f - .2f, 0), new Vector3(.4f, rim + .4f, pd), f.r); mk.Box(stone, f.P(pw / 2f + .2f, rim / 2f - .2f, 0), new Vector3(.4f, rim + .4f, pd), f.r);
            mk.Box(tile, f.P(0, rim - .2f, 0), new Vector3(pw, .1f, pd), f.r);
            var wm = new MeshKit(); wm.Box(WorldMaterials.Water("Links basin", new Color(.25f, .8f, .85f), new Color(.08f, .4f, .6f), .12f, .01f, .15f), f.P(0, rim - .09f, 0), new Vector3(pw - .02f, .04f, pd - .02f), f.r);
            var wg = wm.BuildSingle("Links basin water", ctx.Root, false); if (wg != null) wg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            solid.Box(stone, f.P(0, rim / 2f - .2f, 0), new Vector3(pw + .8f, rim + .4f, pd + .8f), f.r);
            halos.Add(f.P(0, 1.2f, 0), 5f, new Color(.4f, .9f, 1f), .3f); ArtistBits.PointLight(ctx.Root, f.P(0, 2.2f, 0), new Color(1f, .85f, .6f), 16f, 1.7f);

            // the course itself, out of sight; and the model: a copy of it, shrunk, without its sea, lights and lettering
            var big = GolfBuild.Build(course); big.transform.SetParent(ctx.Root, false); big.transform.position = new Vector3(g.x, g.y + LinksSky, g.z);
            var model = Object.Instantiate(big, ctx.Root); model.name = "Golf course model";
            foreach (var l in model.GetComponentsInChildren<Light>(true)) Object.Destroy(l.gameObject);
            foreach (var t in model.GetComponentsInChildren<TextMesh>(true)) Object.Destroy(t.gameObject);
            foreach (var n in new[] { "Sea", "Sea floor" }) { var s = model.transform.Find(n); if (s != null) Object.Destroy(s.gameObject); }
            model.transform.SetPositionAndRotation(f.P(0, rim - .07f, 0), f.r); model.transform.localScale = Vector3.one * LinksScale;
            big.AddComponent<GolfSkyLife>(); big.SetActive(false);   // (marked after the copy was made, so the model is still culled like any scenery)

            var game = new GameObject("The Riviera Links").AddComponent<GolfGame>(); game.transform.SetParent(ctx.Root, false); game.transform.position = g;
            game.course = course; game.big = big.transform; game.mini = model.transform; game.landScene = ctx.Island.sceneId;

            // two golf bags to press E on, one at each end of the near side
            foreach (var e in new[] { -1f, 1f })
            {
                // (its own little mesh, in its own metres, so the "you can use this" glow can draw round it)
                var b = G(f.P(e * (pw / 2f - .6f), 0, pd / 2f + 1.3f)); var lean = new Vector3(0, 1f, .18f).normalized; var right = Vector3.right; var bm = new MeshKit();
                bm.Prism(navy, Vector3.zero, lean, .2f, .95f, 8, 1.1f); bm.Prism(white, lean * .95f, lean, .22f, .06f, 8); bm.Box(white, new Vector3(0, .5f, .2f), new Vector3(.2f, .3f, .08f));
                for (var k = 0; k < 4; k++) { var c = lean * .95f + right * ((k - 1.5f) * .09f); var d = (lean + right * (k - 1.5f) * .05f).normalized; bm.Prism(metal, c, d, .015f, .45f + k * .05f, 4); bm.Blob(metal, c + d * (.5f + k * .05f), new Vector3(.06f, .04f, .08f), ctx.Rng, 0f); }
                foreach (var s in new[] { -1f, 1f }) bm.Prism(metal, new Vector3(0, .5f, -.1f), new Vector3(s * .5f, -1f, -.4f).normalized, .012f, .6f, 4);
                Col(b + up * .6f, new Vector3(.5f, 1.2f, .5f), f.r);
                var go = new GameObject("Golf bag"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(b, f.r); bm.BuildSingle("Golf bag", go.transform, false);
                var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.center = new Vector3(0, .7f, 0); bc.size = new Vector3(.9f, 1.5f, .9f); go.AddComponent<GolfStart>().game = game;
            }

            // the board, benches, lamps, palms and the caddy
            Placard(mk, f.P(0, 0, pd / 2f + 2.6f) + f.Right * (pw / 2f + 1.6f), f.Fwd, "THE RIVIERA LINKS\n18 holes · you'll need to be smaller", 3.6f, .15f);
            // a second board that changes: your best round, and whether the playlist is unlocked
            {
                var bp = G(f.P(0, 0, pd / 2f + 2.6f) - f.Right * (pw / 2f + 1.6f)); var br = Quaternion.LookRotation(f.Fwd);
                foreach (var e in new[] { -1f, 1f }) mk.Box(white, bp + f.Right * e * 1.9f + up * .8f, new Vector3(.1f, 1.6f, .1f), br);
                mk.Box(white, bp + up * 1.35f, new Vector3(4f, .7f, .08f), br); mk.Box(navy, bp + up * 1.72f, new Vector3(4f, .06f, .1f), br); Col(bp + up * .9f, new Vector3(4f, 1.8f, .15f), br);
                game.recordText = ArtistBits.Label(ctx.Root, "", bp + up * 1.36f + f.Fwd * .05f, f.Fwd, .14f, Navy, TextAnchor.MiddleCenter);
            }
            TeakBench(mk, f.P(-(pw / 2f + 2.2f), 0, 0), f.Right); TeakBench(mk, f.P(pw / 2f + 2.2f, 0, -1f), -f.Right);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) Lamp(ctx, mk, G(f.P(x * (pw / 2f + 1.3f), 0, z * (pd / 2f + 1.3f))));
            foreach (var e in new[] { -1f, 1f }) Palm(ctx, mk, G(f.P(e * (pw / 2f + 3.4f), 0, -(pd / 2f + 2.6f))), 8.5f);
            Folk(G(f.P(-(pw / 2f - 2.2f), 0, pd / 2f + 1.5f)), -f.Fwd, Resident.Pose.Stand, new[]
            {
                "It's a real course. You just have to be the right size for it.", "Take a bag. It does the shrinking; I never ask how.",
                "Under par, and the radio in there plays everything. Everything.", "Mind the propellers on the fourth. Lovely boat, no manners.",
            }, Smoothie(Residents.Tool.Rod));
            Lane(ctx, mk, f.P(0, 0, pd / 2f + 3.4f)); Ring(g, Mathf.Max(pw, pd) / 2f + 3.5f);
            Shot(ctx, "the links", f.P(-5f, 3.2f, pd / 2f + 6f), g + up * .5f);
        }
    }
}
