using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Delta Crossroads at a midnight that never ends:
    /// - motes drift across the roads; heat lightning flickers silently in the west now and then (the same for
    ///   everyone); a few dark clouds cross a big moon; the blue bottles in the bottle trees glint; smoke idles from
    ///   the locomotive; a small black cloud rains on Albert King's sign and nowhere else;
    /// - the card that is turned with each song (DeltaCards) and what the night does about it: the Moon comes down
    ///   close and silvers everything; the Tower brings the storm overhead; the Star sets stars falling; the Sun
    ///   raises a false dawn in the east; Death stills the wind and lays a mist; the Hermit puts out every lamp but
    ///   the crooked one; the Wheel sets the wind going; the Devil (always, for Robert Johnson) gutters the bulbs,
    ///   opens eyes in the cotton, and stands something tall up at the head of the shadow on the west road;
    /// - "Smokestack Lightnin'": lightning, close and often, and the engine pours smoke. "Didn't It Rain": the
    ///   storm breaks. "The Thrill Is Gone": the colour goes out of the night. "Boom Boom" and Muddy Waters' songs:
    ///   the juke joint's lights stomp. Every song's own light brightens while it plays.
    public class DeltaLife : MonoBehaviour
    {
        const string Land = "The Delta Crossroads";
        public Vector3 center, smokeAt, cloudAt;
        public string wolfSong, rainSong, thrillSong, boomSong;
        public HashSet<string> johnson = new HashSet<string>(), muddy = new HashSet<string>();
        public Material bulbs, neon, bottles, eyes, storm, lamp, moonM; public Transform figure, moon; public GameObject offer; public float moonSize = 110f; public Vector3 dawnAt; Transform dawn; Material dawnM; public DeltaCards cards;
        public GameObject stormBank;   // only shown while you're in the Delta (it stands over the neighbouring lands)
        public float played = -99f, stomped = -99f;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dust, smoke, drizzle, falling, mist;
        Color bulbBase, neonBase, bottleBase, stormBase, lampBase, moonBase;
        float cMoon, cTower, cStar, cSun, cDeath, cHermit, cWheel, nextStar, nextMist;
        float devil, wolf, rain, thrill, boom, mud, nextDust, nextSmoke, nextDrizzle, flashUntil; long lastFlash = -1;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2600f; clouds.key = "delta";
            clouds.minHeight = 170f; clouds.maxHeight = 260f; clouds.size = 3.6f; clouds.speed = 4f; clouds.cover = .3f;
            clouds.color = new Color(.17f, .18f, .32f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            bulbBase = E(bulbs); neonBase = E(neon); bottleBase = E(bottles); stormBase = E(storm); lampBase = E(lamp); moonBase = E(moonM);
            dust = Particles("Dust", 500, new Color(.5f, .54f, .85f, .05f), .8f, 2.2f, false);
            // falling stars (the Star): quick bright streaks high up; the mist Death lays over the fields
            falling = Particles("Falling stars", 60, new Color(1f, .93f, .7f, 1f), 1.6f, 2.6f, true);
            var fm = falling.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.7f, 1.2f); var fr = falling.GetComponent<ParticleSystemRenderer>(); fr.renderMode = ParticleSystemRenderMode.Stretch; fr.velocityScale = .16f; fr.lengthScale = 1f;
            // the false dawn: one wide soft glow standing on the eastern horizon, faded in by the Sun
            if (dawnAt != Vector3.zero)
            {
                dawnM = Particles("Dawn glow", 1, Color.white, 1f, 1f, true).GetComponent<ParticleSystemRenderer>().sharedMaterial;
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Delta false dawn"; Destroy(q.GetComponent<Collider>()); q.transform.SetParent(transform, false);
                var away = dawnAt - center; away.y = 0; q.transform.SetPositionAndRotation(dawnAt, Quaternion.LookRotation(away.normalized)); q.transform.localScale = new Vector3(1700f, 560f, 1f);
                var qr = q.GetComponent<MeshRenderer>(); qr.sharedMaterial = dawnM; qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; qr.receiveShadows = false; dawn = q.transform; q.SetActive(false);
            }
            mist = Particles("Mist", 300, new Color(.72f, .76f, .9f, .06f), 7f, 13f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
            var dm = dust.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            smoke = Particles("Engine smoke", 300, new Color(.2f, .2f, .22f, .32f), .8f, 1.6f, false);
            var sm = smoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f); var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .5f, 1f, 3f));
            drizzle = Particles("One small rain", 200, new Color(.7f, .8f, 1f, .7f), .03f, .05f, false);
            var zm = drizzle.main; zm.startLifetime = 1f; zm.gravityModifier = 1.2f;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool additive)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetTexture("_BaseMap", tex);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = (float)(SharedChance.Now % 100000.0);
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            if (stormBank != null && stormBank.activeSelf != here) stormBank.SetActive(here);
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;

            // the card that is turned
            var card = here && cards != null ? cards.Card : -1;
            cMoon = Mathf.MoveTowards(cMoon, card == 18 ? 1f : 0f, dt / 5f); cTower = Mathf.MoveTowards(cTower, card == 16 ? 1f : 0f, dt / 4f); cStar = Mathf.MoveTowards(cStar, card == 17 ? 1f : 0f, dt / 3f);
            cSun = Mathf.MoveTowards(cSun, card == 19 ? 1f : 0f, dt / 7f); cDeath = Mathf.MoveTowards(cDeath, card == 13 ? 1f : 0f, dt / 6f); cHermit = Mathf.MoveTowards(cHermit, card == 9 ? 1f : 0f, dt / 3f); cWheel = Mathf.MoveTowards(cWheel, card == 10 ? 1f : 0f, dt / 3f);
            devil = Mathf.MoveTowards(devil, card == 15 || (here && id != null && johnson.Contains(id)) ? 1f : 0f, dt / 6f);
            mud = Mathf.MoveTowards(mud, here && id != null && muddy.Contains(id) ? 1f : 0f, dt / 2f);
            wolf = Mathf.MoveTowards(wolf, On(wolfSong) ? 1f : 0f, dt / 3f);
            rain = Mathf.MoveTowards(rain, On(rainSong) ? 1f : 0f, dt / 6f);
            thrill = Mathf.MoveTowards(thrill, On(thrillSong) ? 1f : 0f, dt / 5f);
            boom = Mathf.MoveTowards(boom, On(boomSong) ? 1f : 0f, dt / 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // silent lightning in the storm bank: now and then for everyone; close and often for Smokestack Lightnin'
            var window = (long)(SharedChance.Now / 3.0);
            if (window != lastFlash) { lastFlash = window; if (SharedChance.Value(SharedChance.Key("delta lightning"), window) < .16 + wolf * .6 + rain * .3 + devil * .2 + cTower * .7) flashUntil = t + .45f; }
            var flash = t < flashUntil ? (Mathf.Sin((flashUntil - t) * 44f) > -.2f ? 1f : .15f) : 0f;
            if (storm != null) storm.SetColor("_EmissionColor", stormBase * (1f - devil * .6f) + new Color(.7f, .78f, 1f) * flash * .7f);

            // the bulbs: steady by day; they stomp in fours for Boom Boom and the juke joint, and gutter for Robert Johnson
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var beat = Mathf.Repeat(now * 2f, 1f); var st = t - stomped < 2.2f ? Mathf.Repeat((t - stomped) * 2f, 1f) : 1f;
            var stomp = Mathf.Max(boom, mud) * (beat < .25f ? 1.6f : 0f) + (st < .25f ? 1.6f : 0f); var gutter = devil * (Mathf.PerlinNoise(t * 9f, 3f) < .45f ? -.75f : .2f);
            if (bulbs != null) bulbs.SetColor("_EmissionColor", bulbBase * Mathf.Max(.12f, 1f + energy * .2f + lift * .4f + stomp + gutter) * (1f - cHermit * .92f));
            // the crooked lamp never gutters and never goes out; when it is the only light left it burns up
            if (lamp != null) lamp.SetColor("_EmissionColor", lampBase * (1f + cHermit * 1.4f + .04f * Mathf.Sin(t * 2f)));
            // the moon is only in this land's sky; the Moon brings it down close. The false dawn comes up for the Sun and stops
            if (moon != null) { if (moon.gameObject.activeSelf != here) moon.gameObject.SetActive(here); moon.localScale = Vector3.one * moonSize * (1f + Mathf.SmoothStep(0f, 1f, cMoon) * 1.3f); if (moonM != null) moonM.SetColor("_EmissionColor", moonBase * (1f + cMoon * .5f) * (1f - devil * .5f)); }
            if (dawn != null) { var on = here && cSun > .01f; if (dawn.gameObject.activeSelf != on) dawn.gameObject.SetActive(on); if (on) { var k = Mathf.SmoothStep(0f, 1f, cSun); dawn.position = dawnAt + Vector3.up * (-90f + k * 70f); dawnM.SetColor("_BaseColor", new Color(1f, .62f, .24f, k * .85f)); } }
            if (neon != null) neon.SetColor("_EmissionColor", neonBase * (1f + mud * 1.4f + boom * .8f + stomp * .4f + .06f * Mathf.Sin(t * 13f)));
            if (bottles != null) bottles.SetColor("_EmissionColor", bottleBase * (1f + .35f * Mathf.Sin(t * 1.4f) + devil * 2.4f));
            if (eyes != null) eyes.SetColor("_EmissionColor", new Color(1f, .15f, .08f) * (devil * (2.6f + .6f * Mathf.Sin(t * 3f))));
            if (offer != null) { var stood = devil > .9f; if (offer.activeSelf != stood) offer.SetActive(stood); }
            if (figure != null) { var up = devil > .35f; if (figure.gameObject.activeSelf != up) figure.gameObject.SetActive(up); if (up) { var s = figure.localScale; s.y = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.35f, 1f, devil)); figure.localScale = s; } }

            // the sky: midnight; then whatever the card says, and the storm for Didn't It Rain, grey for The Thrill Is Gone
            SkyDirector.SetSongRain(Land, Mathf.Max(rain * .85f, cTower * .55f));
            var tint = Color.white; var amount = 0f;
            if (cMoon > 0f) { tint = Color.Lerp(tint, new Color(.74f, .82f, 1f), cMoon); amount = Mathf.Lerp(amount, .34f, cMoon); }
            if (cSun > 0f) { tint = Color.Lerp(tint, new Color(1f, .7f, .36f), cSun); amount = Mathf.Lerp(amount, .4f, cSun); }
            if (cDeath > 0f) { tint = Color.Lerp(tint, new Color(.6f, .63f, .72f), cDeath); amount = Mathf.Lerp(amount, .38f, cDeath); }
            if (cTower > 0f) { tint = Color.Lerp(tint, new Color(.2f, .24f, .42f), cTower); amount = Mathf.Lerp(amount, .4f, cTower); }
            if (thrill > 0f) { tint = Color.Lerp(tint, new Color(.5f, .54f, .64f), thrill); amount = Mathf.Lerp(amount, .5f, thrill); }
            if (wolf > 0f) { tint = Color.Lerp(tint, new Color(.3f, .34f, .5f), wolf); amount = Mathf.Lerp(amount, .45f, wolf); }
            if (rain > 0f) { tint = Color.Lerp(tint, new Color(.26f, .3f, .42f), rain); amount = Mathf.Lerp(amount, .6f, rain); }
            if (devil > 0f) { tint = Color.Lerp(tint, new Color(.16f, .02f, .03f), devil); amount = Mathf.Lerp(amount, .62f, devil); }
            if (flash > 0f && (wolf > 0f || rain > 0f || cTower > 0f)) { tint = Color.Lerp(tint, new Color(.8f, .85f, 1f), flash * .6f); amount *= 1f - flash * .5f; }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", (.4f + .2f * energy + .4f * Mathf.Max(wolf, rain, cTower) + .2f * devil + .9f * cWheel) * (1f - cDeath * .95f)); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (cStar > .3f && t >= nextStar)
            {
                nextStar = t + Random.Range(.25f, .8f);
                var a = Random.Range(0f, Mathf.PI * 2f); var b = a + Random.Range(1.2f, 1.9f); var from = cp + new Vector3(Mathf.Cos(a) * 260f, Random.Range(150f, 260f), Mathf.Sin(a) * 260f);
                falling.Emit(new ParticleSystem.EmitParams { position = from, velocity = new Vector3(Mathf.Cos(b) * 150f, -55f, Mathf.Sin(b) * 150f) }, 1);
            }
            if (cDeath > .2f && t >= nextMist)
            {
                nextMist = t + .12f;
                var p = cp + new Vector3(Random.Range(-45f, 45f), 0f, Random.Range(-45f, 45f)); p.y = cp.y - 1.4f + Random.Range(0f, 1.6f);
                if (!IndoorSpaces.Contains(p)) mist.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.2f, .2f), 0f, Random.Range(-.2f, .2f)) }, 1);
            }
            if (t >= nextDust && rain < .5f && cDeath < .5f)
            {
                nextDust = t + .1f;
                var p = cp + new Vector3(Random.Range(-26f, 26f), Random.Range(-1.2f, 1.5f), Random.Range(-26f, 26f));
                if (!IndoorSpaces.Contains(p)) dust.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * 1.6f, .1f, wind.y * 1.6f) }, 1);
            }
            if (smokeAt != Vector3.zero && t >= nextSmoke && (smokeAt - cp).sqrMagnitude < 160f * 160f)
            {
                nextSmoke = t + Mathf.Lerp(.5f, .05f, wolf);
                smoke.Emit(new ParticleSystem.EmitParams { position = smokeAt, velocity = new Vector3(wind.x * .8f, 1.4f + wolf * 1.6f, wind.y * .8f) }, 1);
            }
            if (cloudAt != Vector3.zero && t >= nextDrizzle && (cloudAt - cp).sqrMagnitude < 50f * 50f)
            {
                nextDrizzle = t + .03f;
                drizzle.Emit(new ParticleSystem.EmitParams { position = cloudAt + new Vector3(Random.Range(-1.2f, 1.2f), 0, Random.Range(-.8f, .8f)), velocity = Vector3.down * 3f }, 1);
            }
        }
    }
}
