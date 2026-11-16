using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// The thing you press E on to skate (the board leaning by the model park).
    public class SkateStart : Interactable
    {
        public SkateGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Skate";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// Skating, in a park you shrink onto (the Punk Badlands' activity, 2026-11-17).
    /// - Press E at the board: you shrink into the park, a drained swimming pool the size of a field, built out of
    ///   sight high over the Badlands. You're an orb on a skateboard.
    /// - W pushes, S brakes, A and D steer. Ride up a wall and you leave the top into the air; Space jumps anywhere.
    ///   In the air A and D spin you and Shift grabs the board. Come round to facing along the board (any half turn)
    ///   and you land it; anything else is a fall. Tricks landed close together multiply.
    /// - It is free skating. Enter starts a 90-second run; 30,000 in a run and the park's radio can play the whole
    ///   Everything Playlist (P switches).
    /// Friends in the park each ride their own board; you see each other and each other's scores.
    public class SkateGame : MonoBehaviour
    {
        public static SkateGame Active { get; private set; }
        public Transform big, mini; public int landScene; public string id = "punk"; public string landName = "the Badlands"; public TextMesh recordText;
        const string Channel = "skate"; const int Bar = 30000; const float RunSeconds = 90f;
        int flipQueued;

        readonly SkateSim sim = new SkateSim(); Transform rider; Material orbM; bool playing, wasLocked, jumpQueued; float shrinkT = 9f, outT = -1f, leaveHeld, nextSend, acc, runLeft = -1f, camYaw; int runStart; Vector3 camAt; bool camSet; string banner = ""; float bannerUntil;
        SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot;
        class Remote { public string name = "Friend"; public Color color = Color.white; public Vector3 pos, shown; public float yaw; public int score; public float seen; public Transform body, dot; public Material m; }
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();

        string Key(string what) => $"EW_Skate_{id}_{what}";
        public int Best => PlayerPrefs.GetInt(Key("best"), 0);
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        public string StartPrompt => "Skate" + (Best > 0 ? $"  ·  your best run {Best:N0}" : "");

        void OnEnable() { Friends.Received += OnNet; }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }
        void Start() { ShowRecord(); }
        void ShowRecord() { if (recordText != null) recordText.text = (Best > 0 ? $"Your best run: {Best:N0}" : "No run finished yet") + "\n" + (Unlocked ? "The Everything Playlist is unlocked here" : $"Score {Bar:N0} in a 90-second run to unlock the Everything Playlist"); }

        static Material floorB;
        /// The park's shape as a mesh, from SkateSim.Height: 's' scales it (1 for the park, small for the model).
        public static void Park(MeshKit mk, float s)
        {
            var conc = WorldMaterials.Surface(new Color(.72f, .72f, .7f), Surface.Stone); var deck = WorldMaterials.Surface(new Color(.42f, .4f, .4f), Surface.Stone); var coping = WorldMaterials.Surface(new Color(.96f, .96f, .92f), Surface.Painted);
            float mx = SkateSim.Hx + SkateSim.Deck, mz = SkateSim.Hz + SkateSim.Deck; var top = SkateSim.Height(SkateSim.Hx, 0);
            Vector3 P(float x, float z) => new Vector3(x, SkateSim.Height(x, z), z) * s;
            // every piece of the park has its own paint, and its sloping faces a different shade from its flat ones, so
            // the ramps read from across the park (told apart by how bright they are as much as by colour)
            Color[] paint =
            {
                new Color(.5f, .5f, .5f), new Color(.86f, .86f, .82f), new Color(.3f, .29f, .3f), new Color(1f, .8f, .15f), new Color(.2f, .75f, .95f),
                new Color(.98f, .98f, .95f), new Color(.2f, .32f, .85f), new Color(.55f, .95f, .85f), new Color(1f, .5f, .15f), new Color(.16f, .16f, .2f),
            };
            var flat = new Material[paint.Length]; var slope = new Material[paint.Length];
            for (var i = 0; i < paint.Length; i++) { flat[i] = WorldMaterials.Surface(paint[i], i <= 2 ? Surface.Stone : Surface.Painted); slope[i] = WorldMaterials.Surface(i <= 2 ? paint[i] * 1.12f : Color.Lerp(paint[i], Color.black, .28f), i <= 2 ? Surface.Stone : Surface.Painted); }
            for (var x = -mx; x < mx; x += 1f) for (var z = -mz; z < mz; z += 1f)
            {
                var k = SkateSim.Kind(x + .5f, z + .5f); float h0 = SkateSim.Height(x, z), h1 = SkateSim.Height(x + 1, z), h2 = SkateSim.Height(x + 1, z + 1), h3 = SkateSim.Height(x, z + 1);
                var tilt = Mathf.Max(Mathf.Max(h0, h1), Mathf.Max(h2, h3)) - Mathf.Min(Mathf.Min(h0, h1), Mathf.Min(h2, h3));
                var m = (tilt > .12f ? slope : flat)[k]; if (k == SkateSim.Floor && ((int)Mathf.Floor(x / 6f) + (int)Mathf.Floor(z / 6f)) % 2 == 0) m = floorB ??= WorldMaterials.Surface(new Color(.43f, .43f, .45f), Surface.Stone);   // big pale and dark squares on the floor
                var mid = P(x + .5f, z + .5f) + Vector3.down * 5f * s;
                mk.Tri(m, P(x, z), P(x + 1, z), P(x + 1, z + 1), mid); mk.Tri(m, P(x, z), P(x + 1, z + 1), P(x, z + 1), mid);
            }
            // the rails: bright bars on little legs
            var railM = WorldMaterials.Glow(new Color(1f, .9f, .3f), 1.3f); var legM = WorldMaterials.Solid(new Color(.1f, .1f, .12f));
            foreach (var r in SkateSim.Rails)
            {
                var a = new Vector3(r.a.x, r.y, r.a.y); var b = new Vector3(r.b.x, r.y, r.b.y); var d = b - a; var len = d.magnitude; var rot = Quaternion.LookRotation(d / len);
                mk.Box(railM, (a + b) / 2f * s - Vector3.up * (.05f * s), new Vector3(.1f, .1f, len) * s, rot);
                for (var t = .5f; t < len; t += 2.5f) { var q = a + d / len * t; var g = SkateSim.Height(q.x, q.z); if (r.y - g > .12f) mk.Box(legM, new Vector3(q.x, (g + r.y - .1f) / 2f, q.z) * s, new Vector3(.07f, r.y - .1f - g, .07f) * s, rot); }
            }
            // things standing about on the deck: lamp posts with a light on top, barrels, a stack of cones
            var lampM = WorldMaterials.Glow(new Color(1f, .85f, .5f), 2.2f); var barrel = WorldMaterials.Surface(new Color(.85f, .2f, .2f), Surface.Painted); var cone = WorldMaterials.Surface(new Color(1f, .55f, .1f), Surface.Painted);
            foreach (var ex in new[] { -1f, -.33f, .33f, 1f }) foreach (var ez in new[] { -1f, 1f })
            {
                var at = new Vector3(ex * (mx - 1.5f), top, ez * (mz - 1.5f)) * s; mk.Prism(legM, at, Vector3.up, .1f * s, 5f * s, 6); mk.Box(lampM, at + Vector3.up * (5.1f * s), new Vector3(.7f, .2f, .7f) * s);
            }
            foreach (var (bx, bz) in new[] { (-30f, mz - 2.5f), (-28.6f, mz - 2.2f), (20f, -(mz - 2.4f)), (0f, mz - 2f) }) mk.Prism(barrel, new Vector3(bx, top, bz) * s, Vector3.up, .45f * s, 1.1f * s, 10);
            foreach (var (cx, cz) in new[] { (12f, -8f), (13.2f, -8.6f), (-20f, 2f), (30f, 10f), (-6f, 22f) }) mk.Prism(cone, new Vector3(cx, 0, cz) * s, Vector3.up, .28f * s, .7f * s, 8, .15f);
            // the coping round the lip, a body under it all, a low rail round the deck
            float lx = SkateSim.Hx - .3f, lz = SkateSim.Hz - .3f;
            foreach (var e in new[] { -1f, 1f }) { mk.Box(coping, new Vector3(0, top + .03f, e * lz) * s, new Vector3(lx * 2f, .12f, .14f) * s); mk.Box(coping, new Vector3(e * lx, top + .03f, 0) * s, new Vector3(.14f, .12f, lz * 2f) * s); }
            mk.Box(deck, new Vector3(0, -1.1f, 0) * s, new Vector3(mx * 2f, 2f, mz * 2f) * s);   // (its top a hand below the floor, so it never shows through it)
            foreach (var e in new[] { -1f, 1f }) { mk.Box(coping, new Vector3(0, top + .6f, e * mz) * s, new Vector3(mx * 2f, .08f, .08f) * s); mk.Box(coping, new Vector3(e * mx, top + .6f, 0) * s, new Vector3(.08f, .08f, mz * 2f) * s); }
            // paint on the floor
            var cols = new[] { new Color(1f, .3f, .55f), new Color(.55f, 1f, .3f), new Color(.3f, .75f, 1f), new Color(1f, .82f, .25f) }; var rng = new System.Random(77);
            for (var k = 0; k < 14; k++) { var x = (float)rng.NextDouble() * 60f - 30f; var z = (float)rng.NextDouble() * 36f - 18f; if (SkateSim.Height(x, z) > .05f) continue; mk.Box(WorldMaterials.Surface(cols[k % 4], Surface.Painted), new Vector3(x, .012f, z) * s, new Vector3(2f + (float)rng.NextDouble() * 4f, .02f, .5f + (float)rng.NextDouble() * 1.5f) * s, Quaternion.Euler(0, rng.Next(180), 0)); }
        }

        /// An orb on a skateboard.
        static Transform Rider(Transform parent, Color c, out Material orb)
        {
            var root = new GameObject("Skater").transform; root.SetParent(parent, false);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(board.GetComponent<Collider>()); board.transform.SetParent(root, false); board.transform.localPosition = new Vector3(0, .09f, 0); board.transform.localScale = new Vector3(.26f, .04f, .95f);
            board.GetComponent<MeshRenderer>().sharedMaterial = WorldMaterials.Surface(new Color(.2f, .16f, .14f), Surface.Wood);
            foreach (var e in new[] { -.32f, .32f }) { var w = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(w.GetComponent<Collider>()); w.transform.SetParent(root, false); w.transform.localPosition = new Vector3(0, .035f, e); w.transform.localScale = new Vector3(.3f, .07f, .08f); w.GetComponent<MeshRenderer>().sharedMaterial = WorldMaterials.Solid(new Color(.9f, .9f, .85f)); }
            var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(o.GetComponent<Collider>()); o.transform.SetParent(root, false); o.transform.localPosition = new Vector3(0, .62f, 0); o.transform.localScale = Vector3.one * .5f;
            orb = new Material(WorldMaterials.Glow(c, 1.3f)) { name = "Skater orb" }; o.GetComponent<MeshRenderer>().sharedMaterial = orb; return root;
        }

        public void Begin(PlayerInteractor who)
        {
            if (Activity.Any || who == null) return;
            walker = who.GetComponent<SimplePlayerController>(); if (walker == null || walker.cameraPivot == null) return;
            hands = who; backPos = walker.transform.position; backRot = walker.transform.rotation; Active = this; playing = false; shrinkT = 0f; outT = -1f;
        }

        void Enter()
        {
            walker.Take(); hands.enabled = false; IslandExplorer.Frozen = true; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = false;
            big.gameObject.SetActive(true); Tune(); playing = true; camSet = false; sim.Reset(); runLeft = -1f; acc = 0f;
            var c = PlayerOrb.Local != null ? PlayerOrb.Local.Color : Color.white; if (rider == null) rider = Rider(big, c, out orbM); orbM.SetColor("_BaseColor", c); orbM.SetColor("_EmissionColor", c * 1.3f);
            Say("W pushes, A and D steer, Space jumps.\nIn the air: A and D spin, Q or E flip the board (or the mouse buttons), hold Shift to grab it (with W or S for the nose or the tail).\nCome down on a yellow rail to grind. Land facing along the board. Enter starts a run.", 14f);
        }

        void Quit()
        {
            if (walker != null) walker.Release(backPos, backRot); if (hands != null) hands.enabled = true;
            IslandExplorer.Frozen = false; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = true; Friends.ReportedEye = null; if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null;
            if (big != null) big.gameObject.SetActive(false); playing = false; Send(); Active = null; shrinkT = .55f; outT = -1f;
        }

        void Tune() { var d = MusicDirector.Instance; if (d == null) return; d.Held = Everything ? d.Everything : d.Catalog != null ? d.Catalog.Get(landScene) : null; }
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }

        // (if anything goes wrong in here, you are put back by the board rather than left stranded)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }
        void LateUpdate() { try { Follow(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            shrinkT += Time.deltaTime; Remotes();
            if (Active != this) return;
            if (!playing) { if (shrinkT > .55f) Enter(); else return; }
            if (outT >= 0f) { outT += Time.deltaTime; if (outT > .5f) Quit(); return; }
            var dt = Mathf.Min(Time.deltaTime, .1f); var kb = Keyboard.current; var mouse = Mouse.current; float push = 0f, steer = 0f; var grab = false;
            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;
            if (canAct)
            {
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing " + landName + "'s songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f) { leaveHeld = 0f; outT = 0f; return; }
                push = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f); steer = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f); grab = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed; if (kb.spaceKey.wasPressedThisFrame) jumpQueued = true;
                if (kb.qKey.wasPressedThisFrame || (mouse != null && mouse.leftButton.wasPressedThisFrame)) flipQueued = 1; if (kb.eKey.wasPressedThisFrame || (mouse != null && mouse.rightButton.wasPressedThisFrame)) flipQueued = -1;
                if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) && runLeft < 0f) { runLeft = RunSeconds; runStart = sim.score; Say("Go!", 1.5f); }
            }
            acc += dt; var n = Mathf.Min(24, (int)(acc / SkateSim.Step)); acc -= n * SkateSim.Step;
            for (var k = 0; k < n; k++) { sim.Advance(push, steer, jumpQueued, grab, flipQueued); jumpQueued = false; if (!sim.grounded) flipQueued = 0; }
            if (sim.grounded) flipQueued = 0;
            if (runLeft >= 0f)
            {
                runLeft -= dt;
                if (runLeft < 0f)
                {
                    var got = sim.score - runStart; if (got > Best) PlayerPrefs.SetInt(Key("best"), got);
                    if (got >= Bar && !Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say($"{got:N0}!\nThe Everything Playlist is unlocked here. P switches it.", 9f); }
                    else Say($"Time: {got:N0}", 6f);
                    PlayerPrefs.Save(); ShowRecord();
                }
            }
            if (Time.time >= nextSend) { nextSend = Time.time + .08f; Send(); }
        }

        static Quaternion Lean(Vector3 p, float yaw, bool grounded, float spin)
        {
            // the board lies on the slope under it; in the air it turns with the spin
            const float e = .2f; var n = new Vector3(-(SkateSim.Height(p.x + e, p.z) - SkateSim.Height(p.x - e, p.z)) / (2f * e), 1f, -(SkateSim.Height(p.x, p.z + e) - SkateSim.Height(p.x, p.z - e)) / (2f * e)).normalized;
            var face = Quaternion.Euler(0, yaw + (grounded ? 0f : spin), 0) * Vector3.forward; if (!grounded) n = Vector3.up;
            var fwd = Vector3.ProjectOnPlane(face, n); return fwd.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(fwd.normalized, n) : Quaternion.identity;
        }

        void Follow()
        {
            if (Active != this || walker == null || !playing) return;
            rider.localPosition = sim.p; rider.localRotation = sim.fallen ? Quaternion.Euler(0, sim.yaw, 80f) : Lean(sim.p, sim.yaw, sim.grounded, sim.spin);
            // the board rolls over in a flip; the rider crouches down to it in a grab
            if (rider.childCount > 3)
            {
                var flipRot = Quaternion.Euler(0, 0, sim.flipAngle); rider.GetChild(0).localRotation = flipRot; rider.GetChild(0).localPosition = new Vector3(0, .09f, 0);
                for (var w = 1; w <= 2; w++) { var e = w == 1 ? -.32f : .32f; rider.GetChild(w).localRotation = flipRot; rider.GetChild(w).localPosition = new Vector3(0, .09f, e) + flipRot * new Vector3(0, -.055f, 0); }
                var orbAt = rider.GetChild(3).localPosition; orbAt.y = Mathf.Lerp(orbAt.y, !sim.grounded && sim.grabT > 0f ? .36f : sim.grinding ? .5f : .62f, 1f - Mathf.Exp(-18f * Time.deltaTime)); rider.GetChild(3).localPosition = orbAt;
            }
            // the camera trails the way you're travelling (not the way you're spinning)
            var hv = new Vector3(sim.v.x, 0, sim.v.z); var want = hv.sqrMagnitude > 1f ? Mathf.Atan2(hv.x, hv.z) * Mathf.Rad2Deg : sim.yaw; camYaw = Mathf.LerpAngle(camYaw, want, 1f - Mathf.Exp(-3f * Time.deltaTime));
            var back = Quaternion.Euler(0, camYaw, 0) * Vector3.back; var p = big.TransformPoint(sim.p); var at = p + back * 8.5f + Vector3.up * 4.2f; var look = p + Vector3.up * 1f;
            if (!camSet) { camAt = at; camSet = true; camYaw = sim.yaw; } camAt = Vector3.Lerp(camAt, at, 1f - Mathf.Exp(-7f * Time.deltaTime));
            var dir = (look - camAt).normalized; var yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; var pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg; var body = Quaternion.Euler(0, yaw, 0); var pivot = walker.cameraPivot;
            walker.transform.SetPositionAndRotation(camAt - body * pivot.localPosition, body); pivot.localEulerAngles = new Vector3(pitch, 0, 0);
            Friends.ReportedEye = mini != null ? mini.TransformPoint(sim.p) + Vector3.up * 1.3f : (Vector3?)null;
        }

        // ---------------------------------------------------------------- friends
        void Send()
        {
            var f = Friends.Instance; if (f == null || !f.Online) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((byte)1); w.Write(id); w.Write(Active == this && playing); w.Write(sim.p.x); w.Write(sim.p.y); w.Write(sim.p.z); w.Write(sim.yaw + (sim.grounded ? 0f : sim.spin)); w.Write(sim.score);
                w.Write((byte)(PlayerOrb.Local != null ? PlayerOrb.Local.Index : 0)); w.Write(Friends.PlayerName); w.Flush(); f.Broadcast(Channel, ms.ToArray(), false);
            }
        }

        void OnNet(ulong from, string channel, byte[] data)
        {
            if (channel != Channel) return;
            try
            {
                using (var r = new BinaryReader(new MemoryStream(data)))
                {
                    if (r.ReadByte() != 1 || r.ReadString() != id) return; if (!others.TryGetValue(from, out var o)) others[from] = o = new Remote();
                    var on = r.ReadBoolean(); o.pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); o.yaw = r.ReadSingle(); o.score = r.ReadInt32();
                    o.color = PlayerOrb.Palette[Mathf.Clamp(r.ReadByte(), 0, PlayerOrb.Palette.Length - 1)].color; o.name = r.ReadString(); o.seen = on ? Time.time : -99f;
                }
            }
            catch (System.Exception) { }
        }

        void Remotes()
        {
            foreach (var o in others.Values)
            {
                var on = Time.time - o.seen < 3f;
                if (o.body == null && on && big != null) { o.body = Rider(big, o.color, out o.m); o.m.SetColor("_EmissionColor", o.color * 1.3f); o.shown = o.pos; var d = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(d.GetComponent<Collider>()); d.name = "Friend on the model"; d.transform.SetParent(transform, false); d.transform.localScale = Vector3.one * .05f; d.GetComponent<MeshRenderer>().sharedMaterial = o.m; o.dot = d.transform; }
                if (o.body == null) continue; o.body.gameObject.SetActive(on); o.dot.gameObject.SetActive(on && mini != null); if (!on) continue;
                o.shown = (o.shown - o.pos).sqrMagnitude > 400f ? o.pos : Vector3.Lerp(o.shown, o.pos, 1f - Mathf.Exp(-14f * Time.deltaTime));
                o.body.localPosition = o.shown; o.body.localRotation = Quaternion.Euler(0, o.yaw, 0); if (mini != null) o.dot.position = mini.TransformPoint(o.shown) + Vector3.up * .03f;
            }
        }

        /// For the photo tour only: drops into the park and rides at a wall for a few seconds.
        public System.Collections.IEnumerator Demo(PlayerInteractor who)
        {
            Begin(who); var until = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < until) yield return null;
            if (Active != this || !playing) { Debug.LogWarning("Skate demo: the game did not start"); yield break; }
            var t = 0f; var peak = 0f; while (t < 6.5f) { var n = Mathf.Min(24, Mathf.CeilToInt(Time.deltaTime / SkateSim.Step)); for (var k = 0; k < n; k++) sim.Advance(1f, !sim.grounded && Mathf.Abs(sim.spin) < 176f ? 1f : 0f, false, false); peak = Mathf.Max(peak, sim.p.y); t += Time.deltaTime; yield return null; }
            Debug.Log($"Skate demo: rider at {sim.p}, highest {peak:0.0} m, score {sim.score}, last trick '{sim.lastTrick}'");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        // ---------------------------------------------------------------- what you see on the screen
        GUIStyle box, big2, small2; static Texture2D white;

        void OnGUI()
        {
            white ??= Texture2D.whiteTexture;
            var wash = outT >= 0f ? outT / .5f : shrinkT < 1.1f ? 1f - Mathf.Abs(shrinkT - .55f) / .55f : 0f;
            if (wash > 0f && Event.current.type == EventType.Repaint) { var keep = GUI.color; GUI.color = new Color(1f, .95f, .85f, Mathf.Clamp01(wash)); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white); GUI.color = keep; }
            if (Active != this || !playing || MainMenu.Showing) return;
            box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(24, TextAnchor.MiddleCenter, true, true); small2 ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            GUI.Box(new Rect(12, 12, 470, 34), runLeft >= 0f ? $"  Run: {sim.score - runStart:N0}  ·  {Mathf.CeilToInt(runLeft)} s left" : $"  Free skating  ·  {sim.score:N0}" + (Best > 0 ? $"  ·  your best run {Best:N0}" : ""), box);
            var y = 50f; if (Everything) { GUI.Box(new Rect(12, y, 240, 30), "  Everything Playlist", box); y += 34f; }
            foreach (var o in others.Values) if (Time.time - o.seen < 3f) { GUI.Box(new Rect(12, y, 330, 28), $"  {o.name}: {o.score:N0}", box); y += 30f; }
            if (sim.saidT > 0f && sim.lastTrick.Length > 0 && Time.time > bannerUntil) GUI.Box(new Rect(Screen.width / 2f - 200, 96, 400, 44), sim.lastPoints > 0 ? $"{sim.lastTrick}   +{sim.lastPoints:N0}" : sim.lastTrick, small2);
            if (Time.time < bannerUntil && banner.Length > 0) Activity.Banner(banner, 96, big2);
            Activity.Hint(Screen.height - 66, "W push  ·  S brake  ·  A D steer and spin  ·  Space jump  ·  Q E flip  ·  Shift grab  ·  land on a rail to grind  ·  " + (runLeft < 0f ? "Enter 90-second run  ·  " : "") + "hold X to leave" + (Unlocked ? "  ·  P playlist" : ""), small2);
        }
    }
}
