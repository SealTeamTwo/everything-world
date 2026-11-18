using System;
using System.Collections.Generic;
using EverythingWorld.Player;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Neo Soul Quarter's twist (part of NeoKit): the quarter is kept as it is, and it turns out to be built on
    /// the shell of something vast and asleep. The plants are so big because they grow out of it.
    /// - Where the land meets the sea the paving gives way to great plates of shell, which rise and fall with its
    ///   breathing, and past them its head lies on the shore with one great eyelid closed. A gardener sits there who
    ///   will tell you about it.
    /// - The quarter rewards staying still, and says so ("STAY A WHILE" in lights on the Courtyard; one line on the
    ///   screen the first time you stop). Stand or sit still a few seconds and a ring of soft light spreads out from
    ///   you, fireflies gather, flowers come up out of the ground round you, and the breathing slows and deepens.
    ///   Stay long enough (and every friend in the quarter with you) and the eye opens a little, looks, and closes.
    public partial class NeoKit
    {
        Vector2 head2, headDir2; bool hasHead; NeoGiant giant;

        /// Where the sleeper's head lies: on a shore of open sea, reached from the Courtyard without crossing anyone's
        /// place, for choice out past the end of Honey Street. (Called at the end of ShapeGround.)
        void SiteHead(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; hasHead = false; var best = float.MaxValue;
            for (var ang = 0f; ang < 360f; ang += 6f)
            {
                var d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)); var reach = 0f;
                for (var s = 30f; s < 520f; s += 4f) { var p = heart2 + d * s; if (data.IslandAt(p.x, p.y) != isl || data.HeightAt(p.x, p.y) < waterY + .6f) break; reach = s; }
                if (reach < 90f) continue; var beyond = heart2 + d * (reach + 40f); if (data.HeightAt(beyond.x, beyond.y) > waterY + .5f) continue;   // the end must be the sea, not another land
                var ok = true; foreach (var a in data.layout.artists) { if (a.sceneId != isl.sceneId) continue; var ap = new Vector2(a.x, a.z); var t = Mathf.Clamp(Vector2.Dot(ap - heart2, d), 24f, reach); if ((heart2 + d * t - ap).magnitude < a.r + 9f) { ok = false; break; } }
                if (!ok) continue;
                var score = reach - 90f * Vector2.Dot(d, D2);
                if (score < best) { best = score; headDir2 = d; head2 = heart2 + d * (reach - 6f); hasHead = true; }
            }
            if (hasHead) Level(data, index, head2 - headDir2 * 22f, 20f, 40f, waterY + 2.4f);
        }

        void Giant(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            giant = new GameObject("The sleeper").AddComponent<NeoGiant>(); giant.transform.SetParent(life.transform, false); giant.center = center; giant.ground = (x, z) => ctx.Ground(x, z);
            // the rule, in lights, where you arrive
            var sg = G(ctx, -15f, -9f); var sface = FaceTo(sg, c); foreach (var e in new[] { -1.9f, 1.9f }) { var p = sg + Vector3.Cross(up, sface) * e; mk.Prism(woodM, p - up * .2f, up, .09f, 3.4f, 6); ArtistBits.PostCollider(ctx.Root, p, 3.2f, .12f); fixtures.Add(new Vector3(p.x, .4f, p.z)); }
            mk.Box(dark, sg + up * 3f, new Vector3(4.4f, 1.1f, .12f), Quaternion.LookRotation(sface)); ArtistBits.Label(rootT, "STAY A WHILE", sg + up * 3f + sface * .08f, sface, .52f, Honey, TextAnchor.MiddleCenter); halos.Add(sg + up * 3f + sface * .4f, 3f, Honey, .35f); noGrass.Add(new Vector3(sg.x, 2.4f, sg.z));
            if (!hasHead) { Debug.Log("The Neo Soul Quarter: no shore was found for the sleeper's head"); return; }

            var hd = new Vector3(headDir2.x, 0, headDir2.y); var hs = Vector3.Cross(up, hd); var headG = ctx.OnGround(head2.x, head2.y); var reach = Vector3.Distance(new Vector3(c.x, 0, c.z), new Vector3(headG.x, 0, headG.z));
            Vector3 On(float d, float lat) => ctx.OnGround(c.x + hd.x * d + hs.x * lat, c.z + hd.z * d + hs.z * lat);
            // the Shell Walk: a lane out from the Courtyard that turns, two thirds of the way, into plates of shell
            var along = Vector3.Dot(hd, D) > .92f; var from = along ? streetL : 23f; var plateFrom = Mathf.Max(from + 10f, reach - 58f);
            var pts = new List<Vector3>(); for (var d = from; d <= plateFrom + 2f; d += 2f) pts.Add(On(d, 0));
            if (pts.Count > 1) { Strip(mk, pts, tileDark, PathW + .6f, .13f, ctx); foreach (var p in pts) Remember(p, 7); }
            var shell = WorldMaterials.Glow(new Color(.5f, .46f, .3f), .14f); var shellPale = WorldMaterials.Glow(new Color(.66f, .6f, .4f), .16f); var seam = WorldMaterials.Solid(new Color(.14f, .12f, .09f)); var n = 0;
            const float PR = 3.3f; float stepD = PR * 1.5f, stepL = PR * Mathf.Sqrt(3f);
            for (var row = 0; ; row++)
            {
                var d = plateFrom + row * stepD; if (d > reach - 14f) break; var wide = Mathf.Min(3, 1 + row / 2);
                for (var k = -wide; k <= wide; k++)
                {
                    var lat = (k + (row % 2 == 0 ? 0f : .5f)) * stepL; var g = On(d, lat); if (!ctx.IsLand(g.x, g.z, 0f) || InFootprint(ctx, g.x, g.z, 3f)) continue;
                    var pm = new MeshKit(); pm.Prism((row + k) % 3 == 0 ? shellPale : shell, new Vector3(0, -.5f, 0), up, PR * .96f, .72f, 6, .9f); pm.Prism(seam, new Vector3(0, -.6f, 0), up, PR * 1.02f, .5f, 6);
                    for (var q = 0; q < 2; q++) for (var e = 0; e < 6; e++) { var r = PR * (.32f + q * .26f); Vector3 a = new Vector3(Mathf.Cos(e * Mathf.PI / 3f) * r, .225f, Mathf.Sin(e * Mathf.PI / 3f) * r), b = new Vector3(Mathf.Cos((e + 1) * Mathf.PI / 3f) * r, .225f, Mathf.Sin((e + 1) * Mathf.PI / 3f) * r); pm.Prism(seam, a, b - a, .035f, (b - a).magnitude, 3); }   // the growth rings, as lines
                    var go = pm.BuildSingle("Shell plate", giant.transform, true); go.isStatic = false; var home = g + up * .12f; go.transform.position = home; giant.plates.Add((go.transform, home, d)); n++;
                    Remember(home, 7); noGrass.Add(new Vector3(g.x, PR, g.z));
                }
            }
            // ---- the head, chin on the shore, one eye turned to the way you come
            var skin = WorldMaterials.Glow(new Color(.3f, .44f, .26f), .18f); var skinDark = WorldMaterials.Glow(new Color(.18f, .3f, .2f), .14f); var hm = new MeshKit(); var hr = new System.Random(77);
            hm.Blob(skin, new Vector3(0, 2.6f, 0), new Vector3(7f, 3.6f, 10f), hr, .05f); hm.Blob(skin, new Vector3(0, 1.9f, 8.5f), new Vector3(5f, 2.5f, 4.6f), hr, .05f); hm.Blob(skinDark, new Vector3(0, 1.2f, -9f), new Vector3(5.4f, 2.6f, 5f), hr, .06f);
            foreach (var e in new[] { -1f, 1f }) { hm.Blob(skinDark, new Vector3(e * 1.6f, 2.5f, 12.4f), new Vector3(.36f, .3f, .3f), hr, 0f); hm.Blob(skinDark, new Vector3(e * 4.6f, 3.4f, 3.4f), new Vector3(2.5f, 1.9f, 2.9f), hr, .04f); }
            for (var k = 0; k < 5; k++) hm.Blob(skinDark, new Vector3(0, 5.4f - k * .12f, -5f + k * 2.6f), new Vector3(2.2f - k * .2f, .5f, 1.1f), hr, .1f);   // a ridge of plates down the brow
            // what makes it a face: the line of the closed eye, pale, and the long line of the mouth
            var lash = WorldMaterials.Glow(new Color(.95f, .9f, .7f), .8f); var lp0 = new Vector3(-6.5f, 2.6f, 1.5f);
            foreach (var z in new[] { 2.3f, 3.1f, 3.9f, 4.7f, 5.7f }) { var q = new Vector3(-6.55f + Mathf.Abs(z - 3.6f) * .1f, 2.6f - .5f * Mathf.Sin((z - 1.5f) / 4.2f * Mathf.PI), z); hm.Prism(lash, lp0, q - lp0, .09f, (q - lp0).magnitude + .04f, 4); lp0 = q; }
            var mp0 = new Vector3(-5.9f, 1.5f, 5.2f); foreach (var q in new[] { new Vector3(-5f, 1.35f, 8f), new Vector3(-3.6f, 1.35f, 11f), new Vector3(-1.4f, 1.5f, 12.9f) }) { hm.Prism(dark, mp0, q - mp0, .13f, (q - mp0).magnitude + .05f, 4); mp0 = q; }
            var head = hm.BuildSingle("The sleeper's head", giant.transform, false); head.isStatic = false; head.transform.localScale = Vector3.one * 1.3f;
            var headRot = Quaternion.LookRotation(Quaternion.AngleAxis(18f, up) * -hs);   // (lying along the shore, so the whole side of its face, and one eye, is turned to the walk)
            var headAt = headG + hd * 5f; headAt.y = Mathf.Max(headG.y, waterY) - .5f; head.transform.SetPositionAndRotation(headAt, headRot); giant.head = head.transform; giant.headHome = headAt;
            BoxCol(ctx.Root, headAt + up * 3f, new Vector3(15f, 7.6f, 25f), headRot);
            // the eye: a dark socket, an amber eye with a slit in it, and the lid over them
            var eyeM = new Material(WorldMaterials.Glow(new Color(1f, .7f, .2f), .2f)) { name = "The sleeper's eye" }; var em = new MeshKit(); em.Blob(dark, Vector3.zero, new Vector3(.4f, 1.5f, 2f), hr, 0f); em.Blob(eyeM, new Vector3(-.16f, 0, 0), new Vector3(.34f, 1.25f, 1.7f), hr, 0f); em.Box(dark, new Vector3(-.5f, 0, 0), new Vector3(.08f, 2.2f, .34f), Quaternion.identity);
            var eye = em.BuildSingle("Eye", head.transform, false); eye.isStatic = false; eye.transform.localPosition = new Vector3(-6.3f, 3.5f, 3.6f); eye.transform.localRotation = Quaternion.identity;
            var lm = new MeshKit(); lm.Blob(skinDark, new Vector3(0, 0, 0), new Vector3(.5f, 1.75f, 2.3f), hr, .03f); var lid = lm.BuildSingle("Eyelid", head.transform, false); lid.isStatic = false; lid.transform.localPosition = new Vector3(-6.62f, 3.5f, 3.6f);
            giant.lid = lid.transform; giant.lidHome = lid.transform.localPosition; giant.eyeM = eyeM; giant.eyeAt = head.transform.TransformPoint(new Vector3(-7f, 3.5f, 3.6f)); giant.nose = head.transform.TransformPoint(new Vector3(0, 2.5f, 12.8f)); giant.noseDir = head.transform.forward;
            giant.eyeLight = ArtistBits.PointLight(ctx.Root, giant.eyeAt + (giant.eyeAt - headAt).normalized * 2f, new Color(1f, .7f, .25f), 26f, 0f);
            // the gardener who sits by it, a lantern, a bench of driftwood
            var seat = On(reach - 30f, -9f); var sf = FaceTo(seat, giant.eyeAt); mk.Box(woodM, seat + up * .24f, new Vector3(2f, .2f, .6f), Quaternion.LookRotation(sf)); foreach (var e in new[] { -.8f, .8f }) mk.Box(woodM, seat + up * .1f + Vector3.Cross(up, sf) * e, new Vector3(.2f, .3f, .5f), Quaternion.LookRotation(sf)); BoxCol(ctx.Root, seat + up * .2f, new Vector3(2f, .4f, .6f), Quaternion.LookRotation(sf));
            Interact.Seat(tlife.transform, seat + up * .38f + Vector3.Cross(up, sf) * .6f, sf, "Sit by the sleeper").seatHeight = .36f;
            var gl = Local(); gl.hat = Residents.Hat.Straw; gl.hair = new Color(.86f, .86f, .84f);
            Folk(seat + up * .36f - Vector3.Cross(up, sf) * .5f, sf, Resident.Pose.Sit, new[] { "Shh. Not because it would wake. Because it is nice to be quiet near something this old.", "The whole quarter is on its back. Why do you think the plants got so big? They are growing out of it.", "Feel that? Up, and down. A hand's width. Once every few breaths of yours.", "Stand still anywhere in the quarter and it knows. The ground goes soft with light. Flowers come up. Stay longer and it may even look at you.", "I have seen the eye open twice. Both times I was doing nothing at all. That is the whole trick." }, gl, null, .36f);
            var lp = seat - Vector3.Cross(up, sf) * 1.6f; mk.Prism(woodM, lp - up * .2f, up, .05f, 1.5f, 5); mk.Blob(lampM, lp + up * 1.42f, new Vector3(.14f, .2f, .14f), prng, 0f); halos.Add(lp + up * 1.42f, 1.6f, Honey, .5f); ArtistBits.PointLight(ctx.Root, lp + up * 1.6f, Honey, 12f, 1.4f); ArtistBits.PostCollider(ctx.Root, lp, 1.4f, .08f);
            noGrass.Add(new Vector3(headG.x, 16f, headG.z)); noGrass.Add(new Vector3(seat.x, 3f, seat.z));
            Shot(ctx, "the sleeper's head", On(reach - 32f, -3f) + up * 2.2f, giant.eyeAt - up * 1f);
            Shot(ctx, "the shell walk", On(plateFrom - 10f, 1f) + up * 2.2f, On(reach - 20f, 0) + up * 2f);
            Debug.Log($"The Neo Soul Quarter: the sleeper's head lies {reach:0} m from the Courtyard, {n} plates of shell");
        }
    }

    /// The sleeper under the Neo Soul Quarter: its breathing, and what happens when you stay still.
    public class NeoGiant : MonoBehaviour
    {
        const string Land = "The Neo Soul Quarter";
        /// The breath, -1 (out) to 1 (in), for anything in the quarter that wants to move with it.
        public static float Breath;
        public Vector3 center, headHome, eyeAt, nose, noseDir; public Func<float, float, float> ground;
        public readonly List<(Transform t, Vector3 home, float d)> plates = new List<(Transform, Vector3, float)>();
        public Transform head, lid; public Vector3 lidHome; public Material eyeM; public Light eyeLight;

        SimplePlayerController player; Vector3 anchor, ringAt; float still, calm, phase, ringR, ringA, eyeOpen, eyeUntil = -99f, nextEye, hintUntil = -99f, nextBloom, nextFly, lastPuff; bool hinted, told; Color eyeBase;
        ParticleSystem ringDots, flies, puff; GUIStyle hint;
        readonly List<(Transform t, float born, Vector3 at)> garden = new List<(Transform, float, Vector3)>(); readonly Queue<Transform> pool = new Queue<Transform>();
        readonly Dictionary<string, (Vector3 at, float since)> others = new Dictionary<string, (Vector3, float)>();

        static Material Soft(string name, Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", c); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; return m;
        }

        void Start()
        {
            if (eyeM != null) eyeBase = eyeM.GetColor("_EmissionColor");
            // the ring of light: a circle of motes lying on the ground, spreading out from where you stopped
            ringDots = Particles("Stillness ring", 900, new Color(1f, .84f, .48f, .8f), .1f, .22f, .9f, 1.5f);
            flies = Particles("Gathering fireflies", 160, new Color(1f, .9f, .5f, .9f), .05f, .1f, 4f, 7f); puff = Particles("The sleeper's breath", 60, new Color(.8f, .86f, .9f, .12f), 1.4f, 2.6f, 3f, 5f);
            var flower = WorldMaterials.Glow(new Color(1f, .62f, .7f), 1.3f); var flower2 = WorldMaterials.Glow(new Color(1f, .86f, .5f), 1.3f); var stem = WorldMaterials.Glow(new Color(.3f, .5f, .28f), .2f);
            for (var i = 0; i < 18; i++)
            {
                var f = new GameObject("Stillness flower").transform; f.SetParent(transform, false);
                Bit(PrimitiveType.Cylinder, stem, f, new Vector3(0, .22f, 0), new Vector3(.03f, .22f, .03f)); Bit(PrimitiveType.Sphere, i % 2 == 0 ? flower : flower2, f, new Vector3(0, .48f, 0), new Vector3(.2f, .1f, .2f)); Bit(PrimitiveType.Sphere, i % 2 == 0 ? flower2 : flower, f, new Vector3(0, .52f, 0), new Vector3(.08f, .08f, .08f));
                f.gameObject.SetActive(false); pool.Enqueue(f);
            }
        }

        static void Bit(PrimitiveType kind, Material m, Transform parent, Vector3 at, Vector3 scale)
        {
            var g = GameObject.CreatePrimitive(kind); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false); g.transform.localPosition = at; g.transform.localScale = scale;
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, float l0, float l1)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col; main.startLifetime = new ParticleSystem.MinMaxCurve(l0, l1);
            var e = ps.emission; e.rateOverTime = 0f; var cl = ps.colorOverLifetime; cl.enabled = true; var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .25f), new GradientAlphaKey(0f, 1f) }); cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); } tex.Apply();
            var m = Soft(name, Color.white); m.SetTexture("_BaseMap", tex); var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return ps;
        }

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime; var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            // the breath: slow, and slower and deeper the calmer you are
            calm = Mathf.MoveTowards(calm, here ? Mathf.Clamp01((still - 3f) / 20f) : 0f, dt / 6f);
            phase += dt / Mathf.Lerp(7f, 11f, calm) * Mathf.PI * 2f; Breath = Mathf.Sin(phase); var rise = Breath * Mathf.Lerp(.07f, .13f, calm);
            foreach (var (tr, home, d) in plates) if (tr != null) tr.position = home + Vector3.up * (rise * (.6f + .4f * Mathf.Sin(d * .11f)));
            if (head != null) head.position = headHome + Vector3.up * rise * 1.6f;
            if (here && puff != null && Breath < -.2f && t - lastPuff > .5f && nose != Vector3.zero) { lastPuff = t; puff.Emit(new ParticleSystem.EmitParams { position = nose, velocity = noseDir * 1.4f + Vector3.up * .3f }, 1); }

            if (player == null) player = FindAnyObjectByType<SimplePlayerController>();
            if (player == null || !here) { still = 0f; Fade(dt, false); Tend(t, dt, Vector3.zero, false); Eye(t, dt, Vector3.zero); return; }
            // staying still: standing where you are, or sitting down
            var p = player.transform.position; var feet = new Vector3(p.x, ground != null ? ground(p.x, p.z) : p.y - player.Height / 2f, p.z);
            if ((p - anchor).sqrMagnitude > .3f * .3f && !player.Seated) { anchor = p; still = 0f; } else still += dt;
            if (still > 1.2f && !hinted) { hinted = true; hintUntil = t + 7f; }
            var on = still > 3.5f;
            if (on) { ringAt = feet; ringR = Mathf.MoveTowards(ringR, 11f, dt * 1.5f); ringA = Mathf.MoveTowards(ringA, .5f, dt / 2f); } else Fade(dt, true);
            if (ringDots != null && ringA > .02f && ringR > .3f)
            {
                var count = Mathf.CeilToInt(ringR * 34f * dt * ringA * 2f);
                for (var k = 0; k < count; k++) { var a = UnityEngine.Random.Range(0f, Mathf.PI * 2f); var q = ringAt + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (ringR + UnityEngine.Random.Range(-.25f, .25f)); q.y = (ground != null ? ground(q.x, q.z) : ringAt.y) + .12f; ringDots.Emit(new ParticleSystem.EmitParams { position = q, velocity = Vector3.up * .05f }, 1); }
            }
            if (on && t >= nextFly) { nextFly = t + .12f; var a = UnityEngine.Random.Range(0f, Mathf.PI * 2f); var r = UnityEngine.Random.Range(1f, Mathf.Max(2f, ringR)); flies.Emit(new ParticleSystem.EmitParams { position = feet + new Vector3(Mathf.Cos(a) * r, UnityEngine.Random.Range(.3f, 2.4f), Mathf.Sin(a) * r), velocity = new Vector3(-Mathf.Cos(a), .1f, -Mathf.Sin(a)) * .25f }, 1); }
            Tend(t, dt, feet, on);
            // is everybody in the quarter keeping still?
            var all = still > 34f;
            if (all && Friends.Instance != null)
                foreach (var (name, _, eye) in Friends.Instance.Others())
                {
                    if ((eye - center).sqrMagnitude > 420f * 420f) continue;
                    if (!others.TryGetValue(name, out var o) || (eye - o.at).sqrMagnitude > .5f * .5f) { others[name] = (eye, t); all = false; } else if (t - o.since < 20f) all = false;
                }
            if (all && t > nextEye) { nextEye = t + 240f; eyeUntil = t + 9f; if (!told || (p - eyeAt).sqrMagnitude > 70f * 70f) { told = true; Say(transform, p + Vector3.up * 1.1f + player.transform.forward * 2f, "Under the quarter, something very large has noticed how still you are. It looks. It goes back to sleep.", 8f); } }
            Eye(t, dt, p);
        }

        void Fade(float dt, bool keepPlace) { ringA = Mathf.MoveTowards(ringA, 0f, dt / 1.2f); if (ringA <= 0f) ringR = 0f; }

        /// The flowers that come up round someone who stays: one every little while, in a ring round them; they go
        /// back into the ground a minute and a half after.
        void Tend(float t, float dt, Vector3 feet, bool on)
        {
            if (on && t >= nextBloom && pool.Count > 0)
            {
                nextBloom = t + .7f; var a = UnityEngine.Random.Range(0f, Mathf.PI * 2f); var r = UnityEngine.Random.Range(1.4f, 5.5f); var at = feet + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); if (ground != null) at.y = ground(at.x, at.z);
                var f = pool.Dequeue(); f.position = at; f.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0); f.localScale = Vector3.zero; f.gameObject.SetActive(true); garden.Add((f, t, at));
            }
            for (var i = garden.Count - 1; i >= 0; i--)
            {
                var (f, born, at) = garden[i]; var age = t - born; var s = age < 2.5f ? Mathf.SmoothStep(0f, 1f, age / 2.5f) : age > 90f ? 1f - Mathf.Clamp01((age - 90f) / 3f) : 1f;
                f.localScale = Vector3.one * s * (1f + .06f * Breath); if (age > 93f) { f.gameObject.SetActive(false); pool.Enqueue(f); garden.RemoveAt(i); }
            }
        }

        void Eye(float t, float dt, Vector3 lookAt)
        {
            eyeOpen = Mathf.MoveTowards(eyeOpen, t < eyeUntil ? 1f : 0f, dt / 2.2f);
            if (lid != null) lid.localPosition = lidHome + new Vector3(0, Mathf.SmoothStep(0f, 1f, eyeOpen) * 1.5f, 0);
            if (eyeM != null) eyeM.SetColor("_EmissionColor", eyeBase * (1f + eyeOpen * 9f)); if (eyeLight != null) eyeLight.intensity = eyeOpen * 2.6f;
        }

        void OnGUI()
        {
            if (Time.time > hintUntil || MainMenu.Showing) return;
            Ui.Begin(); hint ??= GameFonts.Box(20, TextAnchor.MiddleCenter, false, true);
            GUI.Box(new Rect(Ui.W / 2f - 150, Ui.H * .7f, 300, 44), "Stay a while.", hint);
        }
    }
}
