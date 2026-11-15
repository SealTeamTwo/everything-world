using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Lana Del Rey's quarter on the Heartbreak Boardwalk (HeartLana.cs), alive: walk in through the cypresses and
    /// night falls (the sky goes deep blue, the moon shows, stars and fireflies come out); the lighthouse beam turns;
    /// the candles flicker; the butterflies circle; the record turns. Each of her five songs wakes its own place:
    /// - "Mariners Apartment Complex": the lighthouse beam swings round, finds you and stays on you;
    /// - "Cinnamon Girl": every candle flares up;
    /// - "Happiness is a butterfly": the butterflies rise, glow and fly wider;
    /// - "Brooklyn Baby": the string lights brighten and the record spins faster;
    /// - "Say Yes To Heaven": the stair lights up step by step and the column of light pours up, white petals fall.
    public class LanaNight : MonoBehaviour
    {
        const string Land = "The Heartbreak Boardwalk";
        public class Fly { public Transform body, left, right; public Vector3 center; public float radius, speed, phase; }

        public Vector3 center, heavenTop; public float radius; public HeartLife heart;
        public string marinersSong, cinnamonSong, butterflySong, brooklynSong, heavenSong;
        public Transform beam, record; public Renderer moon;
        public Material lighthouseLamp, candles, butterflyGlow, stringLights, flowers, moonGlow, moonHalo, heavenBeam;
        public Material[] steps;
        public readonly List<Fly> flies = new List<Fly>();

        Vector3 lastCam; float night, mar, cin, fly, bro, yes, yaw, nextStar, nextFirefly, nextPetal;
        Material beamMat; Color lampBase, candleBase, flyBase, stringBase, flowerBase, moonBase, haloBase; Color[] stepBase; ParticleSystem dots;

        public static Material Additive(string name)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        static Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;

        void Start()
        {
            lampBase = E(lighthouseLamp); candleBase = E(candles); flyBase = E(butterflyGlow); stringBase = E(stringLights); flowerBase = E(flowers); moonBase = E(moonGlow);
            haloBase = moonHalo != null ? moonHalo.GetColor("_BaseColor") : Color.white;
            if (steps != null) { stepBase = new Color[steps.Length]; for (var i = 0; i < steps.Length; i++) stepBase[i] = E(steps[i]); }
            if (beam != null)
            {
                // two long soft beams, back to back, each two crossed planes so they read from any side
                var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
                foreach (var dir in new[] { 1f, -1f })
                    foreach (var across in new[] { Vector3.right, Vector3.up })
                    {
                        var b = v.Count; var fwd = Vector3.forward * dir;
                        v.Add(-across * .5f); v.Add(across * .5f); v.Add(fwd * 150f + across * 11f); v.Add(fwd * 150f - across * 11f);
                        var col = new Color(1f, .93f, .75f, .5f); c.Add(col); c.Add(col); c.Add(new Color(1, 1, 1, 0)); c.Add(new Color(1, 1, 1, 0));
                        t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                    }
                var mesh = new Mesh { name = "Lighthouse beam" }; mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
                var go = new GameObject("Beam"); go.transform.SetParent(beam, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); beamMat = Additive("Lighthouse beam"); r.sharedMaterial = beamMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            dots = Particles("Stars, fireflies and petals", 1200);
        }

        ParticleSystem Particles(string name, int max)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .25f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) }); cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f))); }
            tex.Apply();
            var m = Additive(name); m.SetTexture("_BaseMap", tex);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var cam = Camera.main; var cp = cam != null ? cam.transform.position : center + Vector3.up * 1000f;
            var dist = Vector2.Distance(new Vector2(cp.x, cp.z), new Vector2(center.x, center.z));
            var want = here ? Mathf.InverseLerp(radius + 14f, radius - 6f, dist) : 0f;
            var jumped = (cp - lastCam).sqrMagnitude > 30f * 30f; lastCam = cp;
            night = jumped ? want : Mathf.MoveTowards(night, want, dt / 2.5f);
            if (heart != null) heart.night = night;
            SkyDirector.SetLocalNight(Land, night); if (jumped && SkyDirector.Instance != null) SkyDirector.Instance.SnapLocalNight();

            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;
            float Ease(float v, string song, float secs) => Mathf.MoveTowards(v, On(song) ? 1f : 0f, dt / secs);
            mar = Ease(mar, marinersSong, 4f); cin = Ease(cin, cinnamonSong, 3f); fly = Ease(fly, butterflySong, 4f); bro = Ease(bro, brooklynSong, 3f); yes = Ease(yes, heavenSong, 4f);
            var near = here && dist < radius + 260f;

            // the moon, only in the night
            if (moon != null) moon.enabled = night > .01f;
            if (moonGlow != null) moonGlow.SetColor("_EmissionColor", moonBase * night);
            if (moonHalo != null) moonHalo.SetColor("_BaseColor", haloBase * night);
            if (flowers != null) flowers.SetColor("_EmissionColor", flowerBase * (.6f + .4f * night + .12f * Mathf.Sin(t * .9f)));

            // the lighthouse: the beam turns slowly; for "Mariners Apartment Complex" it finds you and stays on you
            if (beam != null)
            {
                beam.gameObject.SetActive(here);
                var to = cp - beam.position; var flat = new Vector3(to.x, 0, to.z);
                if (mar > .02f && flat.sqrMagnitude > 1f) yaw = Mathf.MoveTowardsAngle(yaw, Quaternion.LookRotation(flat).eulerAngles.y, dt * (30f + 90f * mar));
                else yaw += dt * 22f;
                var pitch = Mathf.Lerp(4f, Mathf.Atan2(-to.y, Mathf.Max(flat.magnitude, 1f)) * Mathf.Rad2Deg, mar);
                beam.rotation = Quaternion.Euler(pitch, yaw, 0f);
                if (beamMat != null) beamMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f) * (.25f + .45f * night + .5f * mar));
            }
            if (lighthouseLamp != null) lighthouseLamp.SetColor("_EmissionColor", lampBase * (1f + .1f * Mathf.Sin(t * 2f) + mar * 1.6f));

            // the candles flicker; for "Cinnamon Girl" they all flare up
            if (candles != null) candles.SetColor("_EmissionColor", candleBase * (.8f + .35f * Mathf.PerlinNoise(t * 6f, 1.3f) + cin * (2f + .5f * Mathf.PerlinNoise(t * 9f, 4.1f))));

            // the butterflies circle; for "Happiness is a butterfly" they rise, glow and fly wider
            if (near)
                foreach (var f in flies)
                {
                    var a = f.phase + t * f.speed; var r = f.radius * (1f + .7f * fly);
                    var p = f.center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r + Vector3.up * (fly * 3.5f + Mathf.Sin(t * .7f + f.phase) * .6f);
                    var head = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * Mathf.Sign(f.speed);
                    f.body.SetPositionAndRotation(p, Quaternion.LookRotation(head) * Quaternion.Euler(0, 0, -Mathf.Sign(f.speed) * 12f));
                    var flap = 14f + 38f * Mathf.Sin(t * (3.2f + fly * 2.5f) + f.phase);
                    f.left.localRotation = Quaternion.Euler(0, 0, -flap); f.right.localRotation = Quaternion.Euler(0, 0, flap);
                }
            if (butterflyGlow != null) butterflyGlow.SetColor("_EmissionColor", flyBase * (1f + .15f * Mathf.Sin(t * 1.3f) + fly * 2.2f));

            // the Stoop: the record turns; for "Brooklyn Baby" the string lights brighten and it spins faster
            if (record != null && near) record.Rotate(Vector3.up, dt * (120f + bro * 100f), Space.World);
            if (stringLights != null) stringLights.SetColor("_EmissionColor", stringBase * (.9f + .1f * Mathf.Sin(t * 1.4f) + bro * (1.4f + .6f * Mathf.Sin(t * 3f))));

            // the White Stair: for "Say Yes To Heaven" it lights step by step upward and the column of light pours up
            if (steps != null && stepBase != null)
            {
                var climb = Mathf.Repeat(t * 7f, steps.Length + 10f);
                for (var k = 0; k < steps.Length; k++) if (steps[k] != null) steps[k].SetColor("_EmissionColor", stepBase[k] * (.5f + .2f * night + yes * (1.2f + 3f * Mathf.Clamp01(1f - Mathf.Abs(k - climb) / 4f))));
            }
            if (heavenBeam != null) heavenBeam.SetColor("_BaseColor", Color.white * (.06f + .14f * night + .7f * yes));

            if (!here || cam == null || dots == null) return;
            // stars overhead and fireflies low in the grass, in the night; white petals for "Say Yes To Heaven"
            if (night > .3f && t >= nextStar)
            {
                nextStar = t + .03f;
                var q = cp + new Vector3(Random.Range(-160f, 160f), Random.Range(60f, 120f), Random.Range(-160f, 160f));
                dots.Emit(new ParticleSystem.EmitParams { position = q, velocity = Vector3.zero, startColor = new Color(.8f, .86f, 1f, night), startSize = Random.Range(.5f, 1.1f), startLifetime = Random.Range(6f, 10f) }, 1);
            }
            if (night > .3f && t >= nextFirefly && dist < radius)
            {
                nextFirefly = t + .12f;
                var q = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(.4f, 2.6f), Random.Range(-14f, 14f));
                if (!IndoorSpaces.Contains(q)) dots.Emit(new ParticleSystem.EmitParams { position = q, velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(-.1f, .2f), Random.Range(-.3f, .3f)), startColor = new Color(1f, .86f, .36f), startSize = Random.Range(.09f, .14f), startLifetime = Random.Range(3f, 6f) }, 1);
            }
            if (yes > .05f && t >= nextPetal && (heavenTop - cp).sqrMagnitude < 80f * 80f)
            {
                nextPetal = t + .05f / yes;
                var q = heavenTop + new Vector3(Random.Range(-10f, 10f), Random.Range(4f, 14f), Random.Range(-10f, 10f));
                dots.Emit(new ParticleSystem.EmitParams { position = q, velocity = new Vector3(Random.Range(-.3f, .3f), -.8f, Random.Range(-.3f, .3f)), startColor = Color.white, startSize = .14f, startLifetime = 14f }, 1);
            }
        }
    }
}
