using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// The thing you press E on to play (a golf bag by the model of the course).
    public class GolfStart : Interactable
    {
        public GolfGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Play";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// Mini golf, played on a course you shrink onto (the first activity, 2026-11-16; the Riviera's).
    /// - Press E at the bag: you shrink onto the course. It is a real, big course built out of sight (high over its
    ///   land, under the land's own sky), and the little model by the bag is a copy of it; friends who stay outside
    ///   see everyone's ball as a dot of light on the model.
    /// - You are your ball: the camera circles it. Aim with the mouse, hold the button to swing (the bar fills and
    ///   empties), let go to putt. Everyone putts at once; balls pass through each other.
    /// - A hole ends when you're in (or after 10 strokes, or 2 minutes). When everyone on the hole is done, on to the next.
    /// - Finish all 18 under par and the course's radio can play the whole Everything Playlist instead of its land's
    ///   songs (P switches back and forth). Your best round is kept.
    /// What friends share: where each ball is, the hole and the card, ten times a second (Friends.Broadcast). Each
    /// player's own game rolls their own ball; the moving obstacles run on the shared clock, so they match.
    public class GolfGame : MonoBehaviour
    {
        public static GolfGame Active { get; private set; }
        public static bool Playing => Active != null;

        public GolfCourse course; public Transform big, mini; public int landScene;
        public TextMesh recordText;   // a board by the bags: your best round, and whether the playlist is unlocked
        public const int StrokeLimit = 10; public const float HoleSeconds = 120f;
        const string Channel = "golf";

        enum Phase { Off, ShrinkIn, Aim, Roll, HoleDone, RoundDone, ShrinkOut }
        Phase phase = Phase.Off; float phaseT, shrinkT = 9f;
        int hole; GolfSim sim; int strokes; int[] card; float holeStarted, acc, yaw, pitch = 24f, dist = 8f, power; bool charging, wasLocked, fullRound;
        Vector3 focus; int watching = -1; Transform ball, aim; Light ballLight; TrailRenderer trail; Material trailM; Material ballM; string banner = ""; float bannerUntil; float leaveHeld, nextSend, waited;
        SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot;
        readonly List<(GolfEvent e, Vector3 p, float f)> events = new List<(GolfEvent, Vector3, float)>();

        class Remote { public string name = "Friend"; public Color color = Color.white; public int hole, strokes, flags; public Vector3 pos, shown; public int[] card = new int[32]; public float seen; public Transform ball, dot; public TextMesh label; }
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();

        string Key(string what) => $"EW_Golf_{course.id}_{what}";
        public int Best => PlayerPrefs.GetInt(Key("best"), 0);
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        public string StartPrompt => $"Play mini golf  ·  {course.holes.Length} holes, par {course.Par}" + (Best > 0 ? $"  ·  your best {Best}" : "");

        void OnEnable() { Friends.Received += OnNet; }
        void Start() { ShowRecord(); }

        void ShowRecord()
        {
            if (recordText == null || course == null) return;
            var best = Best; var vs = best - course.Par;
            recordText.text = (best > 0 ? $"Your best round: {best}  ({(vs == 0 ? "level par" : vs < 0 ? $"{-vs} under par" : $"{vs} over par")})" : "No round finished yet")
                + "\n" + (Unlocked ? "The Everything Playlist is unlocked here" : $"Go round under {course.Par} to unlock the Everything Playlist");
        }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }

        // ---------------------------------------------------------------- in and out
        public void Begin(PlayerInteractor who)
        {
            if (Active != null || who == null) return;
            walker = who.GetComponent<SimplePlayerController>(); if (walker == null || walker.cameraPivot == null) return;
            hands = who; backPos = walker.transform.position; backRot = walker.transform.rotation;
            Active = this; phase = Phase.ShrinkIn; phaseT = 0f; shrinkT = 0f;
        }

        void Enter()
        {
            walker.Take(); hands.enabled = false; IslandExplorer.Frozen = true; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = false;
            big.gameObject.SetActive(true); Tune();
            if (ball == null)
            {
                ball = Sphere("Golf ball", GolfHole.BallR * 2f, big, out ballM);
                // a little light of its own and a short trail, so it is easy to follow at dusk
                var bl = new GameObject("Ball light").AddComponent<Light>(); bl.transform.SetParent(ball, false); bl.transform.localPosition = Vector3.up * 1.5f; bl.type = LightType.Point; bl.range = 3.5f; bl.intensity = 1.1f; bl.shadows = LightShadows.None; ballLight = bl;
                var tm = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Ball trail" };
                tm.SetFloat("_Surface", 1f); tm.SetFloat("_Blend", 2f); tm.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); tm.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                tm.SetFloat("_ZWrite", 0f); tm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); tm.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; trailM = tm;
                trail = ball.gameObject.AddComponent<TrailRenderer>(); trail.sharedMaterial = tm; trail.time = .4f; trail.startWidth = .24f; trail.endWidth = 0f; trail.minVertexDistance = .05f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false;
                var a = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(a.GetComponent<Collider>()); a.name = "Aim line"; a.transform.SetParent(big, false);
                var r = a.GetComponent<MeshRenderer>(); r.sharedMaterial = WorldMaterials.Glow(Color.white, 2f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; aim = a.transform;
            }
            var c = PlayerOrb.Local != null ? PlayerOrb.Local.Color : Color.white; ballM.SetColor("_BaseColor", c); ballM.SetColor("_EmissionColor", c * 1.2f);
            if (ballLight != null) ballLight.color = c; if (trailM != null) trailM.SetColor("_BaseColor", new Color(c.r, c.g, c.b, .55f));
            ball.gameObject.SetActive(true);
            card = new int[course.holes.Length];
            // join friends where they are; a round only counts for the record if you played it from the first hole
            var start = 0; foreach (var o in others.Values) if (Fresh(o) && (o.flags & 4) == 0) start = Mathf.Max(start, o.hole);
            fullRound = start == 0; StartHole(start);
        }

        void Quit()
        {
            if (walker != null) walker.Release(backPos, backRot);
            if (hands != null) hands.enabled = true;
            IslandExplorer.Frozen = false; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = true; Friends.ReportedEye = null;
            if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null;
            if (big != null) big.gameObject.SetActive(false);
            Send(8, true); Active = null; phase = Phase.Off; sim = null; charging = false; shrinkT = .55f;
        }

        /// For the photo tour only (ShotTour): steps onto the course at a hole and putts once, so a picture can be
        /// taken of the game running and the log shows whether anything went wrong.
        public System.Collections.IEnumerator Demo(PlayerInteractor who, int atHole, float speed)
        {
            Begin(who); var until = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < until) yield return null;
            if (Active != this || sim == null) { Debug.LogWarning("Golf demo: the game did not start"); yield break; }
            if (atHole != hole && atHole < course.holes.Length) StartHole(atHole);
            var dir = Quaternion.Euler(0, yaw - course.yaws[hole], 0) * Vector3.forward; sim.Hit(new Vector2(dir.x, dir.z), speed); strokes++; phase = Phase.Roll; acc = 0f;
            until = Time.realtimeSinceStartup + 1f; while (Time.realtimeSinceStartup < until) yield return null;
            Debug.Log($"Golf demo: hole {hole + 1}, ball at {sim.p}, moving {sim.moving}, phase {phase}");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        void Tune()
        {
            var d = MusicDirector.Instance; if (d == null) return;
            d.Held = Everything ? d.Everything : d.Catalog != null ? d.Catalog.Get(landScene) : null;
        }

        void StartHole(int h)
        {
            hole = h; var gh = course.holes[h]; sim = new GolfSim(gh) { onEvent = (e, p, f) => events.Add((e, p, f)) };
            strokes = 0; holeStarted = Time.time; acc = 0f; charging = false; waited = 0f; watching = -1; phase = Phase.Aim; phaseT = 0f;
            var d = gh.route.Count > 1 ? gh.route[1] - gh.route[0] : Vector2.up; yaw = course.yaws[h] + Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; pitch = 24f; dist = 8f;
            focus = BallWorld() + Vector3.up * .5f; if (ball != null) ball.position = BallWorld(); if (trail != null) trail.Clear(); Say($"Hole {h + 1}  ·  {gh.name}  ·  par {gh.par}\n{gh.blurb}", 4.5f);
        }

        Vector3 BallWorld() => big.TransformPoint(course.At(hole, sim.p + Vector3.up * GolfHole.BallR));
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }

        static Transform Sphere(string name, float size, Transform parent, out Material m)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = name; s.transform.SetParent(parent, false); s.transform.localScale = Vector3.one * size;
            m = new Material(WorldMaterials.Glow(Color.white, 1.2f)) { name = name }; s.GetComponent<MeshRenderer>().sharedMaterial = m; return s.transform;
        }

        static string Result(int strokes, int par)
        {
            if (strokes == 1) return "Hole in one!";
            switch (strokes - par) { case -3: return "Albatross!"; case -2: return "Eagle!"; case -1: return "Birdie!"; case 0: return "Par"; case 1: return "Bogey"; case 2: return "Double bogey"; }
            return strokes < par ? $"{par - strokes} under par!" : $"{strokes - par} over par";
        }

        // ---------------------------------------------------------------- playing
        // (if anything goes wrong in here, you are put back by the bags rather than left stranded on the course)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }
        void LateUpdate() { try { Follow(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            Remotes(); shrinkT += Time.deltaTime;
            if (Active != this) return;
            var dt = Time.deltaTime; phaseT += dt; var kb = Keyboard.current; var mouse = Mouse.current;
            if (phase == Phase.ShrinkIn) { if (shrinkT > .55f) Enter(); else return; }
            if (phase == Phase.ShrinkOut) { if (phaseT > .5f) Quit(); return; }

            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;
            if (canAct)
            {
                var look = mouse.delta.ReadValue() * walker.mouseSensitivity; yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, 6f, 80f);
                dist = Mathf.Clamp(dist - mouse.scroll.ReadValue().y * (Mathf.Abs(mouse.scroll.ReadValue().y) > 10f ? .01f : 1f), 3f, 26f);
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing the Riviera's songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f) { leaveHeld = 0f; phase = Phase.ShrinkOut; phaseT = 0f; return; }
            }

            // a boat or a propeller that reaches a ball lying still pushes it along (no stroke)
            if (phase == Phase.Aim && sim.Touched(SharedChance.Now)) { charging = false; sim.Wake(); phase = Phase.Roll; acc = 0f; }
            if (phase == Phase.Aim && canAct)
            {
                if (!charging && mouse.leftButton.wasPressedThisFrame) { charging = true; power = 0f; phaseT = 0f; }
                if (charging && (mouse.rightButton.wasPressedThisFrame || kb.qKey.wasPressedThisFrame)) charging = false;
                if (charging) power = Mathf.PingPong(phaseT * .85f, 1f);
                if (charging && !mouse.leftButton.isPressed)
                {
                    charging = false; var dir = Quaternion.Euler(0, yaw - course.yaws[hole], 0) * Vector3.forward;
                    sim.Hit(new Vector2(dir.x, dir.z), Mathf.Lerp(1.2f, GolfSim.MaxSpeed, Mathf.Pow(Mathf.Max(power, .03f), 1.25f))); strokes++; phase = Phase.Roll; acc = 0f;
                }
            }

            if (phase == Phase.Roll)
            {
                acc += dt; var n = Mathf.Min(120, (int)(acc / GolfSim.Step)); var now = SharedChance.Now;
                for (var k = 0; k < n && sim.moving; k++) sim.Advance(now - acc + (k + 1) * GolfSim.Step);
                acc = Mathf.Min(acc - n * GolfSim.Step, .1f);
                foreach (var (e, p, f) in events)
                {
                    if (e == GolfEvent.Splash) Say("Splash! Back where you were.", 2f);
                    else if (e == GolfEvent.LipOut) Say("Too fast: it skipped the cup.", 2f);
                }
                events.Clear();
                if (sim.holed) FinishHole(strokes);
                else if (!sim.moving) { if (strokes >= StrokeLimit) FinishHole(StrokeLimit); else phase = Phase.Aim; }
            }
            if ((phase == Phase.Aim || phase == Phase.Roll) && Time.time - holeStarted > HoleSeconds && !(phase == Phase.Roll && sim.moving)) FinishHole(Mathf.Max(strokes, StrokeLimit));

            if (phase == Phase.HoleDone)
            {
                waited += dt; var waiting = 0; foreach (var o in others.Values) if (StillOn(o)) waiting++;
                if (waiting == 0) watching = -1; else if (canAct && mouse.leftButton.wasPressedThisFrame) watching = watching + 1 >= waiting ? -1 : watching + 1;   // click: watch the next ball still out there, then your own again
                if (phaseT > 3.5f && (waiting == 0 || waited > HoleSeconds + 30f))
                {
                    if (hole + 1 < course.holes.Length) StartHole(hole + 1); else FinishRound();
                }
            }
            if (phase == Phase.RoundDone && canAct && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) { card = new int[course.holes.Length]; fullRound = true; StartHole(0); }

            if (Time.time >= nextSend) { nextSend = Time.time + .1f; Send((sim.moving ? 1 : 0) | (phase == Phase.HoleDone || phase == Phase.RoundDone ? 2 : 0) | (phase == Phase.RoundDone ? 4 : 0), false); }
        }

        void FinishHole(int score)
        {
            card[hole] = score; phase = Phase.HoleDone; phaseT = 0f; waited = 0f; charging = false;
            var par = course.holes[hole].par; Say(sim.holed ? Result(score, par) : "Out of strokes", 3.5f);
        }

        void FinishRound()
        {
            phase = Phase.RoundDone; phaseT = 0f; var total = 0; var played = 0; foreach (var s in card) if (s > 0) { total += s; played++; }
            if (fullRound && played == course.holes.Length)
            {
                if (Best == 0 || total < Best) PlayerPrefs.SetInt(Key("best"), total);
                PlayerPrefs.SetInt(Key("rounds"), PlayerPrefs.GetInt(Key("rounds"), 0) + 1);
                if (total < course.Par && !Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say($"{course.Par - total} under par!\nThe Everything Playlist is unlocked here. P switches it.", 9f); }
                else Say(total < course.Par ? $"{course.Par - total} under par!" : total == course.Par ? "Level par" : $"{total - course.Par} over par", 6f);
                PlayerPrefs.Save(); ShowRecord();
            }
            else Say("Round over", 4f);
        }

        void Follow()
        {
            if (Active != this || sim == null || walker == null) return;
            var bw = BallWorld(); var look = bw;
            ball.position = sim.holed ? bw - Vector3.up * GolfHole.BallR * 2.2f : bw;   // in the cup: dropped out of sight
            if (phase == Phase.HoleDone && watching >= 0) { var k = 0; foreach (var o in others.Values) if (StillOn(o) && o.ball != null && k++ == watching) look = o.ball.position; }
            focus = Vector3.Lerp(focus, look + Vector3.up * .5f, 1f - Mathf.Exp(-14f * Time.deltaTime));
            var rot = Quaternion.Euler(pitch, yaw, 0); var cam = focus - rot * Vector3.forward * dist; cam.y = Mathf.Max(cam.y, big.position.y + .6f);
            var body = Quaternion.Euler(0, yaw, 0); var pivot = walker.cameraPivot;
            walker.transform.SetPositionAndRotation(cam - body * pivot.localPosition, body); pivot.localEulerAngles = new Vector3(pitch, 0, 0);
            // the aim line: short while you look, growing with the swing
            var show = phase == Phase.Aim; aim.gameObject.SetActive(show);
            if (show)
            {
                var len = charging ? 1f + power * 7f : 2.2f; var fwd = body * Vector3.forward;
                aim.SetPositionAndRotation(bw + fwd * (len / 2f + .25f) - Vector3.up * (GolfHole.BallR - .04f), body); aim.localScale = new Vector3(.07f, .03f, len);
            }
            Friends.ReportedEye = mini != null ? mini.TransformPoint(course.At(hole, sim.p)) + Vector3.up * 1.3f : (Vector3?)null;
        }

        // ---------------------------------------------------------------- friends
        static bool Fresh(Remote o) => Time.time - o.seen < 5f && (o.flags & 8) == 0;
        /// A friend still playing the hole you're on.
        bool StillOn(Remote o) => Fresh(o) && (o.flags & 4) == 0 && o.hole == hole && (o.flags & 2) == 0;

        void Send(int flags, bool reliable)
        {
            var f = Friends.Instance; if (f == null || !f.Online || sim == null && flags != 8) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((byte)1); w.Write(course.id); w.Write((byte)hole); w.Write((byte)flags);
                var p = sim != null ? sim.p : Vector3.zero; w.Write(p.x); w.Write(p.y); w.Write(p.z); w.Write((byte)Mathf.Min(255, strokes));
                var n = card != null ? card.Length : 0; w.Write((byte)n); for (var i = 0; i < n; i++) w.Write((byte)Mathf.Clamp(card[i], 0, 255));
                w.Write((byte)(PlayerOrb.Local != null ? PlayerOrb.Local.Index : 0)); w.Write(Friends.PlayerName);
                w.Flush(); f.Broadcast(Channel, ms.ToArray(), reliable);
            }
        }

        void OnNet(ulong from, string channel, byte[] data)
        {
            if (channel != Channel || course == null) return;
            try
            {
                using (var r = new BinaryReader(new MemoryStream(data)))
                {
                    if (r.ReadByte() != 1 || r.ReadString() != course.id) return;
                    if (!others.TryGetValue(from, out var o)) others[from] = o = new Remote();
                    o.hole = Mathf.Clamp(r.ReadByte(), 0, course.holes.Length - 1); o.flags = r.ReadByte(); o.pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); o.strokes = r.ReadByte();
                    var n = r.ReadByte(); for (var i = 0; i < n; i++) { var s = r.ReadByte(); if (i < o.card.Length) o.card[i] = s; }
                    var colour = Mathf.Clamp(r.ReadByte(), 0, PlayerOrb.Palette.Length - 1); o.color = PlayerOrb.Palette[colour].color; o.name = r.ReadString(); o.seen = Time.time;
                }
            }
            catch (System.Exception) { }
        }

        /// Friends' balls: on the course if you're on it, and as dots of light on the model for anyone outside.
        void Remotes()
        {
            List<ulong> gone = null;
            foreach (var kv in others)
            {
                var o = kv.Value; var on = Fresh(o);
                if (!on && Time.time - o.seen > 20f) { (gone ??= new List<ulong>()).Add(kv.Key); continue; }
                if (o.ball == null && big != null)
                {
                    o.ball = Sphere("Friend's ball", GolfHole.BallR * 2f, big, out var bm); bm.SetColor("_BaseColor", o.color); bm.SetColor("_EmissionColor", o.color * 1.2f);
                    o.label = ArtistBits.Plain(ArtistBits.Label(o.ball, "", Vector3.zero, Vector3.forward, .35f, new Color(1f, .97f, .9f), TextAnchor.LowerCenter)); o.label.gameObject.AddComponent<Billboard>();
                    o.dot = Sphere("Friend's ball on the model", .06f, transform, out var dm); dm.SetColor("_BaseColor", o.color); dm.SetColor("_EmissionColor", o.color * 3f);
                    o.shown = o.pos;
                }
                if (o.ball == null) continue;
                o.shown = (o.shown - o.pos).sqrMagnitude > 400f ? o.pos : Vector3.Lerp(o.shown, o.pos, 1f - Mathf.Exp(-12f * Time.deltaTime));
                var onCourse = course.At(o.hole, o.shown);
                o.ball.gameObject.SetActive(on); o.dot.gameObject.SetActive(on && mini != null);
                if (!on) continue;
                o.ball.localPosition = onCourse + Vector3.up * GolfHole.BallR;
                if (o.label.text != o.name) o.label.text = o.name; o.label.transform.position = o.ball.position + Vector3.up * .45f;
                var far = Camera.main != null ? Vector3.Distance(Camera.main.transform.position, o.ball.position) : 8f; o.label.transform.localScale = Vector3.one * Mathf.Clamp(far / 9f, 1f, 6f) / (GolfHole.BallR * 2f);
                if (mini != null) o.dot.position = mini.TransformPoint(onCourse) + Vector3.up * .035f;
            }
            if (gone != null) foreach (var id in gone) { var o = others[id]; if (o.ball != null) Destroy(o.ball.gameObject); if (o.dot != null) Destroy(o.dot.gameObject); others.Remove(id); }
        }

        // ---------------------------------------------------------------- what you see on the screen
        GUIStyle box, big2, small, cell; static Texture2D white;

        void OnGUI()
        {
            white ??= Texture2D.whiteTexture;
            // the shrink: the screen washes out as you go in, and clears again as you arrive (and the same coming back)
            var wash = phase == Phase.ShrinkOut ? phaseT / .5f : shrinkT < 1.1f ? 1f - Mathf.Abs(shrinkT - .55f) / .55f : 0f;
            if (wash > 0f && Event.current.type == EventType.Repaint) { var keep = GUI.color; GUI.color = new Color(1f, .93f, .8f, Mathf.Clamp01(wash)); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white); GUI.color = keep; }
            if (Active != this || sim == null || MainMenu.Showing) return;
            box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(26, TextAnchor.MiddleCenter, true, true); small ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            cell ??= new GUIStyle(GameFonts.Box(14, TextAnchor.MiddleCenter)) { normal = { background = null }, padding = new RectOffset(0, 0, 0, 0) };
            var gh = course.holes[hole]; var total = 0; var parSoFar = 0; for (var i = 0; i < card.Length; i++) if (card[i] > 0) { total += card[i]; parSoFar += course.holes[i].par; }
            var vs = total - parSoFar; var left = Mathf.Max(0, Mathf.CeilToInt(HoleSeconds - (Time.time - holeStarted)));
            GUI.Box(new Rect(12, 12, 470, 34), $"  Hole {hole + 1}/{course.holes.Length}  ·  {gh.name}  ·  par {gh.par}  ·  strokes {strokes}", box);
            GUI.Box(new Rect(12, 50, 470, 30), $"  Round {(parSoFar == 0 ? "just started" : vs == 0 ? "level par" : vs < 0 ? $"{-vs} under par" : $"{vs} over par")}" + (phase == Phase.Aim || phase == Phase.Roll ? $"  ·  {left} s left" : "") + (Everything ? "  ·  Everything Playlist" : ""), box);
            if (Time.time < bannerUntil && banner.Length > 0) { var h = banner.Contains("\n") ? 92f : 54f; GUI.Box(new Rect(Screen.width / 2f - 330, 96, 660, h), banner, big2); }

            if (phase == Phase.Aim)
            {
                var w = 360f; var r = new Rect(Screen.width / 2f - w / 2f, Screen.height - 96, w, 22); GUI.Box(r, "", small);
                if (charging) { var keep = GUI.color; GUI.color = Color.Lerp(new Color(.4f, .9f, 1f), new Color(1f, .75f, .2f), power); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, (r.width - 6) * power, r.height - 6), white); GUI.color = keep; }
                GUI.Box(new Rect(Screen.width / 2f - 330, Screen.height - 66, 660, 28), charging ? "Let go to putt  ·  right click to cancel" : "Aim with the mouse  ·  hold click to swing  ·  scroll to zoom  ·  Tab scores  ·  hold X to leave" + (Unlocked ? "  ·  P playlist" : ""), small);
            }
            else if (phase == Phase.HoleDone)
            {
                var waiting = 0; foreach (var o in others.Values) if (StillOn(o)) waiting++;
                if (waiting > 0) GUI.Box(new Rect(Screen.width / 2f - 260, Screen.height - 66, 520, 28), (waiting == 1 ? "Waiting for one more ball" : $"Waiting for {waiting} more balls") + "  ·  click to watch", small);
            }
            else if (phase == Phase.RoundDone) GUI.Box(new Rect(Screen.width / 2f - 260, Screen.height - 66, 520, 28), "Enter  Play again   ·   hold X  Leave the course", small);

            var kb = Keyboard.current;
            if (phase == Phase.HoleDone || phase == Phase.RoundDone || (kb != null && kb.tabKey.isPressed)) Card();
        }

        void Card()
        {
            var rows = new List<(string name, Color c, int[] card)> { (Friends.PlayerName, PlayerOrb.Local != null ? PlayerOrb.Local.Color : Color.white, card) };
            foreach (var o in others.Values) if (Fresh(o)) rows.Add((o.name, o.color, o.card));
            var n = course.holes.Length; const float nameW = 170f, colW = 30f, rowH = 26f; var w = nameW + colW * n + 70f + 24f; var h = rowH * (rows.Count + 2) + 20f;
            var x = Screen.width / 2f - w / 2f; var y = Mathf.Max(170f, Screen.height / 2f - h / 2f - 40f);
            GUI.Box(new Rect(x, y, w, h), "", box); x += 12f; y += 10f; var keep = GUI.contentColor;
            GUI.contentColor = new Color(1f, .9f, .7f); GUI.Label(new Rect(x, y, nameW, rowH), "Hole", cell); for (var i = 0; i < n; i++) GUI.Label(new Rect(x + nameW + i * colW, y, colW, rowH), (i + 1).ToString(), cell); GUI.Label(new Rect(x + nameW + n * colW, y, 70f, rowH), "Total", cell);
            y += rowH; GUI.contentColor = new Color(.8f, .85f, .9f); GUI.Label(new Rect(x, y, nameW, rowH), "Par", cell); for (var i = 0; i < n; i++) GUI.Label(new Rect(x + nameW + i * colW, y, colW, rowH), course.holes[i].par.ToString(), cell); GUI.Label(new Rect(x + nameW + n * colW, y, 70f, rowH), course.Par.ToString(), cell);
            foreach (var (name, c, sc) in rows)
            {
                y += rowH; var total = 0; var par = 0; GUI.contentColor = Color.Lerp(c, Color.white, .35f); GUI.Label(new Rect(x, y, nameW, rowH), name, cell);
                for (var i = 0; i < n && i < sc.Length; i++)
                {
                    var s = sc[i]; if (s > 0) { total += s; par += course.holes[i].par; }
                    GUI.contentColor = s == 0 ? new Color(1, 1, 1, .35f) : s < course.holes[i].par ? new Color(.5f, .95f, 1f) : s == course.holes[i].par ? Color.white : new Color(1f, .75f, .45f);
                    GUI.Label(new Rect(x + nameW + i * colW, y, colW, rowH), s == 0 ? "·" : s.ToString(), cell);
                }
                GUI.contentColor = Color.white; GUI.Label(new Rect(x + nameW + n * colW, y, 70f, rowH), total == 0 ? "" : $"{total} ({(total - par == 0 ? "E" : (total - par).ToString("+0;-0"))})", cell);
            }
            GUI.contentColor = keep;
        }
    }
}
