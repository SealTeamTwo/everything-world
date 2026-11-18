using System;
using System.Collections.Generic;
using EverythingWorld.Player;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Funk Tropics' twist (part of FunkKit): the Mothership stays landed, and her tender, a small glass saucer,
    /// flies a slow round over the whole jungle on the shared clock (friends see her in the same place), with a
    /// beam of gold light under her. Whatever the beam passes over goes up: leaves, sparks, now and then one of the
    /// locals, and you. You rise through the canopy onto her glass floor, ride round looking down at the lights, and
    /// are set down somewhere you did not choose. Step into the beam on purpose and it is how you get about; get
    /// caught by it and it is the sidetrack. Space asks to be put down at once (nothing here takes control from you
    /// for longer than you want).
    public partial class FunkKit
    {
        void Tender(LandContext ctx)
        {
            var up = Vector3.up; const int N = 72; var c2 = new Vector3(center.x, 0, center.z); var rad = new float[N];
            Vector3 Dir(int k) => Quaternion.Euler(0, k * 360f / N, 0) * Vector3.forward;
            for (var k = 0; k < N; k++) { var d = Dir(k); var r = 40f; while (r < 460f && ctx.IsLand(c2.x + d.x * (r + 10f), c2.z + d.z * (r + 10f), 26f)) r += 10f; rad[k] = Mathf.Max(48f, r * .72f); }
            for (var pass = 0; pass < 5; pass++) { var s = (float[])rad.Clone(); for (var k = 0; k < N; k++) rad[k] = (s[(k + N - 1) % N] + s[k] * 2f + s[(k + 1) % N]) / 4f; }
            var pts = new Vector3[N]; var h = new float[N];
            for (var k = 0; k < N; k++) { var p = c2 + Dir(k) * rad[k]; pts[k] = p; h[k] = Mathf.Max(ctx.Ground(p.x, p.z), waterY); }
            for (var pass = 0; pass < 8; pass++) { var s = (float[])h.Clone(); for (var k = 0; k < N; k++) h[k] = Mathf.Max(s[k], (s[(k + N - 1) % N] + s[k] * 2f + s[(k + 1) % N]) / 4f); }
            for (var k = 0; k < N; k++) pts[k].y = h[k] + FunkTender.Height;

            var go = new GameObject("The tender"); go.transform.SetParent(life.transform, false); var tn = go.AddComponent<FunkTender>();
            tn.path = pts; tn.waterY = waterY; tn.ground = (x, z) => ctx.Ground(x, z);
            tn.canLand = p => { if (!ctx.IsLand(p.x, p.z, 4f)) return false; foreach (var (c, r) in blocks) if ((new Vector2(p.x, p.z) - c).sqrMagnitude < r * r) return false; return true; };
            // the saucer: a low rim of lights, a glass floor, a glass dome (you stand inside and see out all round and straight down)
            const float R = FunkTender.Radius; var sm = new MeshKit();
            // (a ring, open in the middle: you stand in it and look straight down through nothing)
            for (var k = 0; k < 20; k++) { var q = Quaternion.Euler(0, k * 18f, 0); sm.Box(dark, q * new Vector3(0, -.25f, R * .9f), new Vector3(R * .34f, .5f, R * .2f), q); sm.Box(chrome, q * new Vector3(0, .04f, R * .9f), new Vector3(R * .35f, .08f, R * .24f), q); sm.Box(chrome, q * new Vector3(0, -.62f, R * .84f), new Vector3(R * .3f, .26f, R * .14f), q * Quaternion.Euler(-28f, 0, 0)); }
            for (var k = 0; k < 20; k++) { var d = Quaternion.Euler(0, k * 18f, 0) * Vector3.forward; sm.Blob(tileM[k % 4], d * (R + .04f) - up * .22f, Vector3.one * .2f, prng, 0f); }
            var rimM = new Material(WorldMaterials.Glow(Gold, 1.8f)) { name = "Tender belly" }; for (var k = 0; k < 20; k++) { var a = Quaternion.Euler(0, k * 18f, 0) * Vector3.forward * (R * .7f) - up * 1.02f; var b = Quaternion.Euler(0, k * 18f + 18f, 0) * Vector3.forward * (R * .7f) - up * 1.02f; sm.Prism(rimM, a, b - a, .07f, (b - a).magnitude, 4); }
            var sgo = sm.BuildSingle("Saucer", go.transform, false); if (sgo != null) { sgo.isStatic = false; foreach (var r in sgo.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            tn.Furnish(Cyan, Gold, new[] { Lime, Orange, Pink, Purple });
            // one of the locals, who gets picked up now and then and is quite used to it
            var look = Fly(); var rider = Residents.Make(go.transform, Vector3.zero, Vector3.forward, look, Resident.Pose.Wave, prng, new[] { "This is fine. This happens.", "Third time this week! Tell them I'll be late.", "You get used to it. The view's the best in the Tropics.", "Don't fight it. Nobody ever landed anywhere boring." });
            rider.gameObject.SetActive(false); tn.rider = rider.transform;
            // two marks for the photo tour that go where she goes: her and her beam from the ground, and the view from her floor
            tn.seen = new GameObject("Shot: the tender and her beam").transform; tn.seen.SetParent(ctx.Root, false); tn.from = new GameObject("Shot: from the tender's glass floor").transform; tn.from.SetParent(ctx.Root, false);
            Debug.Log($"The Funk Tropics: the tender's round is {tn.Length:0} m");
        }
    }

    /// The tender on her round, her beam, and whoever is in it.
    public class FunkTender : MonoBehaviour
    {
        const string Land = "The Funk Tropics";
        public const float Height = 30f, Radius = 4.6f, Speed = 7f, BeamR = 4.2f;
        public Vector3[] path; public float waterY; public Func<float, float, float> ground; public Func<Vector3, bool> canLand; public Transform rider, seen, from;
        public float Length { get { Measure(); return total; } }

        float[] along; float total; Transform beam, disc; Light spot; ParticleSystem lifted; Material beamM, discM, glassM; Color[] leafCols;
        SimplePlayerController player; CharacterController body;
        enum Phase { None, Rise, Ride, Lower }
        Phase phase; float rideT, cooldown, hintUntil; bool wantDown; GUIStyle hint;

        void Measure()
        {
            if (along != null || path == null) return;
            along = new float[path.Length + 1]; for (var i = 0; i < path.Length; i++) along[i + 1] = along[i] + Vector3.Distance(path[i], path[(i + 1) % path.Length]); total = along[path.Length];
        }

        Vector3 At(float s)
        {
            s = Mathf.Repeat(s, total); int lo = 0, hi = path.Length;
            while (hi - lo > 1) { var mid = (lo + hi) / 2; if (along[mid] <= s) lo = mid; else hi = mid; }
            return Vector3.Lerp(path[lo], path[(lo + 1) % path.Length], Mathf.InverseLerp(along[lo], along[lo + 1], s));
        }

        static Material Soft(string name, Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", c); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        Transform Shape(PrimitiveType kind, string name, Material m, Transform parent)
        {
            var g = GameObject.CreatePrimitive(kind); g.name = name; Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false);
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return g.transform;
        }

        /// The glass, the beam, its light, and what rises in it.
        public void Furnish(Color glass, Color gold, Color[] leaves)
        {
            leafCols = leaves;
            glassM = Soft("Tender glass", new Color(glass.r, glass.g, glass.b, .028f)); var dome = Shape(PrimitiveType.Sphere, "Dome", glassM, transform); dome.localScale = new Vector3(Radius * 1.9f, 5.2f, Radius * 1.9f); dome.localPosition = new Vector3(0, .1f, 0);
            // (the floor itself is not drawn: any glass at all, seen edge on from inside, hid the land below)
            beamM = Soft("Tender beam", new Color(gold.r, gold.g, gold.b, .09f)); beam = Shape(PrimitiveType.Cylinder, "Beam", beamM, transform.parent);
            discM = Soft("Tender beam on the ground", new Color(gold.r, gold.g, gold.b, .3f)); disc = Shape(PrimitiveType.Cylinder, "Beam on the ground", discM, transform.parent);
            var lg = new GameObject("Beam light"); lg.transform.SetParent(transform, false); lg.transform.localPosition = new Vector3(0, -1.2f, 0); lg.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            spot = lg.AddComponent<Light>(); spot.type = LightType.Spot; spot.color = gold; spot.range = Height + 16f; spot.spotAngle = 22f; spot.innerSpotAngle = 14f; spot.intensity = 5f; spot.shadows = LightShadows.None;
            var pg = new GameObject("Lifted things"); pg.transform.SetParent(transform.parent, false); lifted = pg.AddComponent<ParticleSystem>(); lifted.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = lifted.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 400; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(.08f, .3f); main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.5f);
            var em = lifted.emission; em.rateOverTime = 0f; var rot = lifted.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var pr = pg.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = Soft("Lifted things", Color.white); pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            Measure(); if (along == null || total < 1f) return;
            var t = Time.time; var dt = Time.deltaTime; var s = ShotTour.Running ? total * .08f : (float)((SharedChance.Now * Speed) % total);   // (she holds still to be photographed)
            var pos = At(s) + Vector3.up * Mathf.Sin(t * .9f) * .4f; var ahead = At(s + 4f) - pos; ahead.y = 0;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, t * 30f, 0));
            var gy = Mathf.Max(ground(pos.x, pos.z), waterY); var len = pos.y - 1f - gy;
            if (beam != null) { beam.position = new Vector3(pos.x, gy + len / 2f, pos.z); beam.localScale = new Vector3(BeamR * 2f, len / 2f, BeamR * 2f); beamM.SetColor("_BaseColor", new Color(1f, .8f, .34f, .075f + .03f * Mathf.Sin(t * 5f))); }
            if (disc != null) { disc.position = new Vector3(pos.x, gy + .12f, pos.z); disc.localScale = new Vector3(BeamR * 2f, .01f, BeamR * 2f); }
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land; var cam = Camera.main;
            // what the beam picks up as it passes
            if (here && lifted != null && cam != null && (cam.transform.position - pos).sqrMagnitude < 260f * 260f)
                for (var k = 0; k < 2; k++) { var a = UnityEngine.Random.Range(0f, Mathf.PI * 2f); var r = Mathf.Sqrt(UnityEngine.Random.value) * BeamR * .9f; lifted.Emit(new ParticleSystem.EmitParams { position = new Vector3(pos.x + Mathf.Cos(a) * r, gy + UnityEngine.Random.Range(0f, 2f), pos.z + Mathf.Sin(a) * r), velocity = new Vector3(ahead.normalized.x * Speed, UnityEngine.Random.Range(4f, 7f), ahead.normalized.z * Speed), startColor = leafCols[UnityEngine.Random.Range(0, leafCols.Length)] }, 1); }
            // one of the locals, lifted for a stretch of the round now and then (the same for everyone)
            if (rider != null)
            {
                var window = SharedChance.Now / 75.0; var w = (long)window; var u = (float)(window - w); var on = SharedChance.Roll(SharedChance.Key("funk tender rider"), w, .45) && phase == Phase.None;
                var k = on ? Mathf.SmoothStep(0f, 1f, Mathf.Min(u / .07f, (1f - u) / .07f)) : 0f;
                if (rider.gameObject.activeSelf != (k > 0f)) rider.gameObject.SetActive(k > 0f);
                if (k > 0f) rider.SetPositionAndRotation(new Vector3(pos.x + 1.2f, Mathf.Lerp(gy, pos.y - 9f, k), pos.z), Quaternion.Euler(0, t * 50f, Mathf.Sin(t * 1.3f) * 10f));
            }
            if (seen != null) { var side = Vector3.Cross(Vector3.up, ahead.sqrMagnitude > .01f ? ahead.normalized : Vector3.forward); var at = new Vector3(pos.x, 0, pos.z) + side * 46f - ahead.normalized * 14f; at.y = Mathf.Max(ground(at.x, at.z), waterY) + 2.2f; seen.SetPositionAndRotation(at, Quaternion.LookRotation((Vector3.Lerp(pos, new Vector3(pos.x, gy, pos.z), .3f) - at).normalized)); }
            if (from != null) from.SetPositionAndRotation(pos + Vector3.up * 1.1f, Quaternion.LookRotation((ahead.normalized * 1f + Vector3.down * .75f).normalized));
            Carry(pos, gy, dt);
        }

        void Carry(Vector3 pos, float gy, float dt)
        {
            if (player == null) { player = FindAnyObjectByType<SimplePlayerController>(); if (player != null) body = player.GetComponent<CharacterController>(); }
            if (player == null || body == null) return;
            var half = body.height / 2f; var p = player.transform.position; var floorY = pos.y - .45f + half; var kb = UnityEngine.InputSystem.Keyboard.current;
            switch (phase)
            {
                case Phase.None:
                    if (Time.time < cooldown || player.Flying || player.Seated || player.Controlled || IslandExplorer.Frozen) return;
                    float dx = p.x - pos.x, dz = p.z - pos.z; if (dx * dx + dz * dz > BeamR * BeamR || p.y > pos.y - 3f || p.y - half < gy - 2f) return;
                    player.Take(); player.LookWhileControlled = true; phase = Phase.Rise; rideT = 0f; wantDown = false; hintUntil = Time.time + 9f;
                    break;
                case Phase.Rise:
                    p.x = Mathf.MoveTowards(p.x, pos.x, 12f * dt); p.z = Mathf.MoveTowards(p.z, pos.z, 12f * dt); p.y = Mathf.MoveTowards(p.y, floorY, 9f * dt); player.transform.position = p;
                    if (p.y >= floorY - .05f) phase = Phase.Ride;
                    break;
                case Phase.Ride:
                    player.transform.position = new Vector3(pos.x, floorY, pos.z); rideT += dt;
                    if (kb != null && kb.spaceKey.wasPressedThisFrame) wantDown = true;
                    if ((rideT > 38f || wantDown) && rideT > 2.5f && canLand(pos)) phase = Phase.Lower;
                    break;
                case Phase.Lower:
                    p.x = pos.x; p.z = pos.z; p.y -= 9f * dt; var land = gy + half + .1f;
                    if (Physics.Raycast(new Vector3(p.x, p.y, p.z), Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore)) land = Mathf.Max(land, hit.point.y + half + .1f);
                    // carried out over water or a roof she can't leave you on: up again, and on a little further
                    if (!canLand(pos) && p.y - land < 8f) { phase = Phase.Rise; wantDown = true; player.transform.position = p; break; }
                    if (p.y <= land) { p.y = land; player.transform.position = p; player.LetGo(); phase = Phase.None; cooldown = Time.time + 12f; break; }
                    player.transform.position = p;
                    break;
            }
        }

        void OnDisable() { if (phase != Phase.None && player != null) { phase = Phase.None; player.LetGo(); } }

        void OnGUI()
        {
            if (phase == Phase.None || Time.time > hintUntil || MainMenu.Showing) return;
            Ui.Begin(); hint ??= GameFonts.Box(16, TextAnchor.MiddleCenter);
            GUI.Box(new Rect(Ui.W / 2f - 230, Ui.H - 120, 460, 36), "The tender has you.  Space: ask to be put down", hint);
        }
    }
}
