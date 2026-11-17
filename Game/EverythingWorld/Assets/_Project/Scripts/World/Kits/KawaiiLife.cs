using System.Collections.Generic;
using EverythingWorld.Music;
using EverythingWorld.Player;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Kawaii Cascades, alive:
    /// - while you are on the land you are hyper (SimplePlayerController.Hyper) and its pads and rails are live; the view
    ///   widens the faster you go; a note says how to move when you arrive;
    /// - everything glows on the beat (about 160 a minute, with the songs): the lollipop lamps, the jelly, the rails;
    /// - floating donuts bob, stars spin over the macaron hops and burst into sprinkles when you reach them (they come
    ///   back after a minute), pads puff sprinkles when they throw you, spray rises where the milk lands, clouds race;
    /// - some songs, a shower of sprinkles falls over the whole land (the same songs for everyone);
    /// - "Signs of Rain": a soft rain and a blue light. "dreamseeker", "Moonheart" and "Lost": night falls and the stars
    ///   come out. "ENERGIZE": the whole land flashes yellow on every beat. "Monochrome Anomaly": the colour drains away.
    ///   "Summer Trees": gold light. "Glass Petals": lilac. Every song's own piece brightens while it plays.
    public class KawaiiLife : MonoBehaviour
    {
        const string Land = "The Kawaii Cascades";
        const float Bpm = 160f;
        public Vector3 center, parfaitTop;
        public HyperCourse course;
        public string rainSong, energizeSong, monoSong, summerSong, glassSong; public List<string> nightSongs = new List<string>();
        public Material lamps, jelly, rails, milk, stars;
        public List<(Transform t, Vector3 home, float phase)> floaters = new List<(Transform, Vector3, float)>();
        public List<(Transform t, Vector3 axis, float speed)> spinners = new List<(Transform, Vector3, float)>();
        public List<(Transform t, Vector3 home)> starsToGet = new List<(Transform, Vector3)>();
        public List<Vector3> milkFeet = new List<Vector3>();
        /// The milk round the Cake: where it is, and how high. Fall in and you are put back on the last firm thing you stood on.
        public System.Func<float, float, bool> inMilk; public float milkY;
        readonly List<(float t, Vector3 at)> firm = new List<(float, Vector3)>(); float nextFirm, splashed = -99f; int splashes;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        readonly Dictionary<Transform, float> starBack = new Dictionary<Transform, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, jellyBase, railBase, milkBase, starBase;
        float rain, night, energize, mono, summer, glassy, shower, arrived = -99f, baseFov = -1f, nextSpray, starNote = -99f;
        bool wasHere; int starCount; SimplePlayerController player; GUIStyle hud, big;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 46; clouds.span = 2600f; clouds.key = "kawaii";
            clouds.minHeight = 110f; clouds.maxHeight = 200f; clouds.size = 3f; clouds.speed = 22f; clouds.cover = .4f;
            clouds.color = new Color(1f, .86f, .94f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); jellyBase = E(jelly); railBase = E(rails); milkBase = E(milk); starBase = E(stars);
            dots = Particles("Sprinkles and spray", 2400, Color.white, .07f, .2f);
            if (course != null) { course.onPad = (at, kind) => Burst(at + Vector3.up * .5f, kind == HyperCourse.Kind.Geyser ? 60 : 26, kind == HyperCourse.Kind.Geyser ? 9f : 5f); course.onRail = at => Burst(at, 18, 4f); }
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max; main.gravityModifier = .5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 8; y++) for (var x = 0; x < 8; x++) tex.SetPixel(x, y, Color.white);
            tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetTexture("_BaseMap", tex);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Burst(Vector3 at, int n, float speed)
        {
            if (dots == null) return;
            for (var i = 0; i < n; i++)
            {
                var c = KawaiiKit.Sweet[Random.Range(0, KawaiiKit.Sweet.Length)];
                dots.Emit(new ParticleSystem.EmitParams { position = at, velocity = Random.insideUnitSphere * speed + Vector3.up * speed, startColor = c, startLifetime = Random.Range(.7f, 1.6f), startSize = Random.Range(.08f, .22f) }, 1);
            }
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;
            float Ease(float v, bool on, float secs) => Mathf.MoveTowards(v, on ? 1f : 0f, dt / secs);

            // hyper while you are here
            if (player == null) player = FindAnyObjectByType<SimplePlayerController>();
            if (here != wasHere) { wasHere = here; SimplePlayerController.Hyper = here; if (here) arrived = t; }
            if (course != null) course.live = here;
            var cam = Camera.main;
            if (cam != null && player != null)
            {
                if (baseFov < 0f) baseFov = cam.fieldOfView;
                var speed = course != null && course.Riding ? course.RideSpeed : here && !player.Controlled && !player.Flying ? player.GroundSpeed : 0f;
                var want = baseFov + (here ? Mathf.Clamp01((speed - 9f) / 26f) * 16f : 0f);
                if (here || Mathf.Abs(cam.fieldOfView - baseFov) > .05f) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, want, 1f - Mathf.Exp(-5f * dt));
            }

            rain = Ease(rain, On(rainSong), 4f); energize = Ease(energize, On(energizeSong), 2f); mono = Ease(mono, On(monoSong), 3f); summer = Ease(summer, On(summerSong), 4f); glassy = Ease(glassy, On(glassSong), 4f);
            night = Ease(night, here && id != null && nightSongs.Contains(id), 5f);
            // a shower of sprinkles on about one song in three (the same songs for everyone)
            var showerOn = here && SharedChance.CurrentSong(out var songNo, out _, out _) && SharedChance.Roll(SharedChance.Key("kawaii sprinkles"), songNo, .34);
            shower = Ease(shower, showerOn, 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 5f) * .7f)));

            // the beat: a quick flash that dies away before the next one
            var beat = (float)(now * Bpm / 60.0 % 1.0); var pulse = Mathf.Exp(-beat * 5f); var half = Mathf.Exp(-(float)(now * Bpm / 30.0 % 1.0) * 5f);
            var p = pulse * (.35f + energy * .5f) + energize * half * .9f;
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (.9f + p * .7f));
            if (jelly != null) jelly.SetColor("_EmissionColor", jellyBase * (.8f + p * .9f));
            if (rails != null) rails.SetColor("_EmissionColor", railBase * (.8f + p * .8f + (course != null && course.Riding ? .8f : 0f)));
            if (milk != null) milk.SetColor("_EmissionColor", milkBase * (.9f + .15f * Mathf.Sin(t * 3f)));
            if (stars != null) stars.SetColor("_EmissionColor", starBase * (.8f + p * .6f));

            foreach (var (tr, home, ph) in floaters) if (tr != null) { tr.position = home + Vector3.up * (Mathf.Sin(t * .9f + ph) * 1.1f + pulse * .25f); tr.Rotate(0f, dt * 14f, 0f, Space.World); }
            foreach (var (tr, axis, sp) in spinners) if (tr != null) tr.Rotate(axis, dt * sp * (1f + energy), Space.Self);

            // the milk: remember where you last stood firm, and put you back there if you fall in
            if (here && player != null && inMilk != null && !player.Flying && !player.Controlled && !player.Seated)
            {
                var pos = player.transform.position; var feetY = pos.y - player.Height / 2f; var overMilk = inMilk(pos.x, pos.z);
                if (overMilk && feetY < milkY + .15f)
                {
                    Burst(new Vector3(pos.x, milkY + .3f, pos.z), 80, 6f);
                    var back = firm.Count > 0 ? firm[0].at : center + Vector3.up * 2f; for (var i = firm.Count - 1; i >= 0; i--) if (t - firm[i].t > .9f) { back = firm[i].at; break; }
                    player.Warp(back); splashed = t; splashes++; firm.Clear(); firm.Add((t - 2f, back));
                }
                else if (player.Grounded && feetY > milkY + 1.2f && t >= nextFirm) { nextFirm = t + .3f; firm.Add((t, pos)); if (firm.Count > 12) firm.RemoveAt(0); }
            }

            // stars: spin, and burst when you reach one
            var pp = player != null ? player.transform.position : Vector3.one * 99999f;
            foreach (var (tr, home) in starsToGet)
            {
                if (tr == null) continue;
                if (starBack.TryGetValue(tr, out var back)) { if (t < back) continue; starBack.Remove(tr); tr.gameObject.SetActive(true); }
                tr.position = home + Vector3.up * Mathf.Sin(t * 2f + home.x) * .25f; tr.rotation = Quaternion.Euler(0, t * 140f + home.z * 10f, 0);
                if (here && (pp - home).sqrMagnitude < 2.2f * 2.2f) { tr.gameObject.SetActive(false); starBack[tr] = t + 60f; Burst(home, 70, 7f); starCount++; starNote = t; }
            }

            var tint = new Color(1f, .9f, .96f); var amount = .06f;
            if (summer > 0f) { tint = Color.Lerp(tint, new Color(1f, .82f, .4f), summer); amount = Mathf.Lerp(amount, .32f, summer); }
            if (glassy > 0f) { tint = Color.Lerp(tint, new Color(.78f, .72f, 1f), glassy); amount = Mathf.Lerp(amount, .34f, glassy); }
            if (rain > 0f) { tint = Color.Lerp(tint, new Color(.55f, .7f, 1f), rain); amount = Mathf.Lerp(amount, .36f, rain); }
            if (mono > 0f) { tint = Color.Lerp(tint, new Color(.5f, .5f, .5f), mono); amount = Mathf.Lerp(amount, .62f, mono); }
            if (energize > 0f) { tint = Color.Lerp(tint, new Color(1f, .9f, .3f), energize); amount = Mathf.Lerp(amount, .14f + half * .3f, energize); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, rain * .55f);
            SkyDirector.SetLocalNight(Land, night * .85f);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .6f + .4f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            if (!here || cam == null || dots == null) return; var cp = cam.transform.position;
            if (t >= nextSpray)
            {
                nextSpray = t + .12f;
                foreach (var f in milkFeet) if ((f - cp).sqrMagnitude < 70f * 70f) dots.Emit(new ParticleSystem.EmitParams { position = f + Random.insideUnitSphere * 1.2f, velocity = Vector3.up * Random.Range(1.5f, 3.5f) + Random.insideUnitSphere, startColor = new Color(1f, .86f, .92f, .8f), startLifetime = Random.Range(.6f, 1.2f), startSize = Random.Range(.1f, .3f) }, 2);
                var n = Mathf.RoundToInt(shower * 26f);
                for (var i = 0; i < n; i++)
                {
                    var o = Random.insideUnitCircle * 34f;
                    dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(o.x, Random.Range(8f, 16f), o.y), velocity = new Vector3(wind.x, -3f, wind.y) * 1.5f, startColor = KawaiiKit.Sweet[Random.Range(0, KawaiiKit.Sweet.Length)], startLifetime = Random.Range(3f, 5f), startSize = Random.Range(.07f, .16f) }, 1);
                }
            }
        }

        void OnGUI()
        { EverythingWorld.World.Ui.Begin();
            var t = Time.time;
            if (wasHere && t - arrived < 9f && player != null && !player.Controlled)
            {
                big ??= GameFonts.Box(22, TextAnchor.MiddleCenter, false, true); hud ??= GameFonts.Box(16, TextAnchor.MiddleCenter);
                GUI.Box(new Rect(EverythingWorld.World.Ui.W / 2f - 150, 54, 300, 38), "HYPER!", big);
                GUI.Box(new Rect(EverythingWorld.World.Ui.W / 2f - 360, 96, 720, 30), "Shift sprints  ·  Space, then Space again in the air  ·  jump at a wall to kick off it  ·  touch a rail to ride it", hud);
            }
            if (t - splashed < 2.2f) { hud ??= GameFonts.Box(16, TextAnchor.MiddleCenter); GUI.Box(new Rect(EverythingWorld.World.Ui.W / 2f - 150, 168, 300, 30), "Splash! Back where you last stood.", hud); }
            if (t - starNote < 2.5f) { hud ??= GameFonts.Box(16, TextAnchor.MiddleCenter); GUI.Box(new Rect(EverythingWorld.World.Ui.W / 2f - 90, 132, 180, 30), "★ " + starCount + (starCount == 1 ? " star" : " stars"), hud); }
        }

        void OnDisable()
        {
            if (wasHere) { SimplePlayerController.Hyper = false; wasHere = false; }
            if (baseFov > 0f && Camera.main != null) Camera.main.fieldOfView = baseFov;
        }
    }
}
