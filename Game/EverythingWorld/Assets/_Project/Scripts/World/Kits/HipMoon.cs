using System.Collections.Generic;
using EverythingWorld.Player;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Hip-Hop Boroughs on the moon (part of HipKit). It is a city first: the same blocks, stoops, shops, courts
    /// and street lights, the same crowd. It simply stands on grey ground under a black sky with the Earth hanging
    /// huge overhead (which keeps it apart from Boom Bap Harbour: that one is warm brick up a mountain in summer
    /// daylight).
    /// - Low gravity while you are here (HipMoon): long floating jumps several times your height, and everything you
    ///   throw, the basketball included, hangs in the air. Hold Ctrl or C to come down fast, so floating never takes
    ///   control away from you;
    /// - laundry drifts on its lines across Victory Avenue; a stiff flag stands on the Block; craters ring the
    ///   outskirts; the street trees have gone silver.
    public partial class HipKit
    {
        void Moon(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(1969);
            var moon = new GameObject("The moon").AddComponent<HipMoon>(); moon.transform.SetParent(life.transform, false); moon.center = center;
            // ---- the Earth, hanging over the far end of Victory Avenue
            var eg = GameObject.CreatePrimitive(PrimitiveType.Sphere); eg.name = "The Earth"; Object.Destroy(eg.GetComponent<Collider>()); eg.transform.SetParent(moon.transform, false);
            eg.transform.position = center + D * 560f + S * 90f + up * 235f; eg.transform.localScale = Vector3.one * 300f; eg.transform.rotation = Quaternion.Euler(18f, 40f, 12f);
            var em = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "The Earth" }; em.SetTexture("_BaseMap", HipMoon.EarthTexture()); em.SetColor("_BaseColor", Color.white);
            var er = eg.GetComponent<MeshRenderer>(); er.sharedMaterial = em; er.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; er.receiveShadows = false; eg.SetActive(false); moon.earth = eg.transform;
            // ---- laundry on lines across the Avenue, drifting
            Color[] cloth = { new Color(.95f, .95f, .92f), new Color(1f, .8f, .2f), new Color(.6f, .35f, .9f), new Color(.9f, .3f, .3f), new Color(.3f, .6f, 1f) }; var lines = 0;
            for (var d = 34f; d <= 150f; d += 29f)
            {
                var a = G(ctx, d, -7.4f); var b = G(ctx, d, 7.4f); if (!ctx.IsLand(a.x, a.z, 2f) || !ctx.IsLand(b.x, b.z, 2f) || InFootprint(ctx, a.x, a.z, 1.5f) || InFootprint(ctx, b.x, b.z, 1.5f)) continue;
                foreach (var p in new[] { a, b }) { mk.Prism(iron, p - up * .2f, up, .09f, 7.6f, 6); ArtistBits.PostCollider(ctx.Root, p, 7.2f, .12f); }
                var a1 = a + up * 7.2f; var b1 = b + up * 7.2f; mk.Prism(iron, a1, b1 - a1, .015f, (b1 - a1).magnitude, 3);
                for (var k = 1; k <= 5; k++)
                {
                    var at = Vector3.Lerp(a1, b1, k / 6f); var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = "Laundry"; Object.Destroy(g.GetComponent<Collider>()); g.transform.SetParent(moon.transform, false);
                    g.transform.localScale = new Vector3(.9f + (k % 2) * .3f, k % 3 == 0 ? 1.3f : .8f, .03f); var gr = g.GetComponent<MeshRenderer>(); gr.sharedMaterial = WorldMaterials.Glow(cloth[(k + lines) % cloth.Length], .12f); gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    moon.laundry.Add((g.transform, at, Quaternion.LookRotation(D), k + lines * 5));
                }
                lines++;
            }
            // ---- a flag on the Block, wired out stiff because there is no wind to hold it
            var fp = G(ctx, -9f, 15f); if (!InFootprint(ctx, fp.x, fp.z, 1f))
            {
                mk.Prism(chromeOf(), fp - up * .2f, up, .05f, 4.4f, 6); mk.Prism(chromeOf(), fp + up * 4.1f, S, .02f, 2.3f, 4); ArtistBits.PostCollider(ctx.Root, fp, 4.2f, .08f);
                Color[] stripes = { new Color(1f, .72f, .14f), new Color(.96f, .93f, .84f), new Color(.5f, .28f, .78f) }; for (var k = 0; k < 3; k++) mk.Box(WorldMaterials.Glow(stripes[k], .2f), fp + up * (3.86f - k * .42f) + S * 1.2f, new Vector3(.03f, .42f, 2.1f), Quaternion.LookRotation(D));
                noGrass.Add(new Vector3(fp.x, 1f, fp.z));
            }
            // ---- craters out past the last blocks: a ring of banked grey rubble round a darker floor
            var rock = WorldMaterials.Surface(new Color(.56f, .56f, .58f), Surface.Stone); var floor = WorldMaterials.Surface(new Color(.3f, .3f, .33f), Surface.Stone); var craters = 0;
            for (var k = 0; k < 40 && craters < 12; k++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var dist = 70f + (float)rng.NextDouble() * 230f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * dist; var r = 6f + (float)rng.NextDouble() * 9f;
                if (!ctx.IsLand(p.x, p.z, r + 8f) || InFootprint(ctx, p.x, p.z, r + 4f) || NearPath(p.x, p.z, r + 4f) || InNoGrass(p.x, p.z, r + 2f)) continue;
                var g = ctx.OnGround(p.x, p.z); var flat = true; for (var q = 0; q < 6; q++) if (Mathf.Abs(ctx.Ground(g.x + Mathf.Cos(q) * r, g.z + Mathf.Sin(q) * r) - g.y) > 1.2f) flat = false; if (!flat) continue;
                mk.Prism(floor, g - up * .1f, up, r * .86f, .16f, 14);
                var n = Mathf.RoundToInt(r * 2.2f); for (var q = 0; q < n; q++) { var qa = q * Mathf.PI * 2f / n; var rp = g + new Vector3(Mathf.Cos(qa), 0, Mathf.Sin(qa)) * r; rp.y = ctx.Ground(rp.x, rp.z); mk.Blob(rock, rp + up * .1f, new Vector3(1.5f, .5f + (float)rng.NextDouble() * .5f, 1.5f), rng, .3f); }
                noGrass.Add(new Vector3(g.x, r + 1f, g.z)); craters++;
            }
            Debug.Log($"The Hip-Hop Boroughs: on the moon, {lines} lines of laundry, {craters} craters");
        }

        Material chromeOf() => WorldMaterials.Surface(new Color(.8f, .82f, .86f), Surface.Metal);
    }

    /// The moon's rules while you are in the Boroughs: low gravity for you and for everything thrown, a fast way down,
    /// the Earth in the sky, the laundry adrift.
    public class HipMoon : MonoBehaviour
    {
        const string Land = "The Hip-Hop Boroughs";
        public const float Gravity = .24f, Jump = 3.6f;
        public Vector3 center; public Transform earth;
        public readonly List<(Transform t, Vector3 home, Quaternion rot, int k)> laundry = new List<(Transform, Vector3, Quaternion, int)>();
        bool on; float hintUntil = -99f; bool hinted; GUIStyle hint; static readonly Vector3 EarthG = new Vector3(0, -9.81f, 0);

        public static Texture2D EarthTexture()
        {
            const int W = 256, H = 128; var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "Earth", wrapMode = TextureWrapMode.Repeat };
            for (var y = 0; y < H; y++) for (var x = 0; x < W; x++)
            {
                float u = x / (float)W, v = y / (float)H; var lat = Mathf.Abs(v - .5f) * 2f;
                var land = Mathf.PerlinNoise(u * 6f + 3.1f, v * 5f + 1.7f) * .65f + Mathf.PerlinNoise(u * 14f + 9f, v * 12f + 4f) * .35f; var cloud = Mathf.PerlinNoise(u * 9f + 20f, v * 16f + 11f) * .6f + Mathf.PerlinNoise(u * 22f + 5f, v * 30f + 8f) * .4f;
                var c = land > .54f ? Color.Lerp(new Color(.3f, .55f, .3f), new Color(.7f, .62f, .4f), Mathf.InverseLerp(.54f, .75f, land)) : Color.Lerp(new Color(.08f, .2f, .6f), new Color(.2f, .46f, .9f), land / .54f);
                if (lat > .82f) c = Color.Lerp(c, Color.white, Mathf.InverseLerp(.82f, .92f, lat)); if (cloud > .56f) c = Color.Lerp(c, Color.white, Mathf.InverseLerp(.56f, .72f, cloud) * .9f);
                t.SetPixel(x, y, c * 1.15f);
            }
            t.Apply(true); return t;
        }

        void Set(bool want)
        {
            if (want == on) return; on = want;
            SimplePlayerController.GravityScale = on ? Gravity : 1f; SimplePlayerController.JumpScale = on ? Jump : 1f; SimplePlayerController.FastFall = on;
            Physics.gravity = on ? EarthG * .3f : EarthG;
            if (earth != null) earth.gameObject.SetActive(on);
            if (on && !hinted) { hinted = true; hintUntil = Time.time + 9f; }
        }

        void Update()
        {
            Set(SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land);
            if (!on) return; var t = Time.time;
            if (earth != null) earth.Rotate(0, .6f * Time.deltaTime, 0, Space.Self);
            // the laundry: each piece lifts off its line a little and turns slowly, the way cloth does with nothing to pull it down
            foreach (var (tr, home, rot, k) in laundry) if (tr != null) { tr.position = home + Vector3.up * (-.3f + .35f * Mathf.Sin(t * .31f + k * 1.7f)) + rot * Vector3.right * (.2f * Mathf.Sin(t * .23f + k)); tr.rotation = rot * Quaternion.Euler(Mathf.Sin(t * .27f + k * 2.1f) * 50f, Mathf.Sin(t * .19f + k) * 30f, Mathf.Sin(t * .22f + k * .7f) * 24f); }
        }

        void OnDisable() { Set(false); }

        void OnGUI()
        {
            if (Time.time > hintUntil || MainMenu.Showing) return;
            Ui.Begin(); hint ??= GameFonts.Box(16, TextAnchor.MiddleCenter);
            GUI.Box(new Rect(Ui.W / 2f - 250, Ui.H - 120, 500, 36), "Low gravity.  Space: jump  ·  hold Ctrl or C: come down fast", hint);
        }
    }
}
