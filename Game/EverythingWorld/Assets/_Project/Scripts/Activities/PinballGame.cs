using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// The thing you press E on to play pinball (the plunger by the model table).
    public class PinballStart : Interactable
    {
        public PinballGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Play";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// Pinball where you are the ball (the Oddball Backroads' activity, 2026-11-17).
    /// - Press E at the plunger: you shrink onto the table, a long slope of ice built out of sight high over the
    ///   Backroads, and the camera rides with the ball.
    /// - Hold Space and let go to launch. A (or the left mouse button) works the left flipper, D (or the right) the
    ///   right. Bumpers, the fast lane up the left, the spring and the pipe all score. Three balls.
    /// - Everyone on the table plays their own ball at once, on their own flippers; you see each other's balls and scores.
    /// - Score 10,000 in a game and the table's radio can play the whole Everything Playlist (P switches).
    public class PinballGame : MonoBehaviour
    {
        public static PinballGame Active { get; private set; }
        public Transform big, mini; public int landScene; public string id = "oddball"; public string landName = "the Backroads"; public TextMesh recordText;
        const string Channel = "pinball"; const int Bar = 10000;

        PinballPlay play; Transform ball; Material ballM; Transform[] flippers; bool playing, charging, wasLocked; float power, chargeT, shrinkT = 9f, outT = -1f, leaveHeld, nextSend; Vector3 camAt; bool camSet; string banner = ""; float bannerUntil;
        SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot;
        class Remote { public string name = "Friend"; public Color color = Color.white; public Vector3 pos, shown; public int score, balls; public float seen; public bool on; public Transform ball, dot; }
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();

        string Key(string what) => $"EW_Pinball_{id}_{what}";
        public int Best => PlayerPrefs.GetInt(Key("best"), 0);
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        public string StartPrompt => "Play pinball (you're the ball)" + (Best > 0 ? $"  ·  your best {Best:N0}" : "");

        void OnEnable() { Friends.Received += OnNet; }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }
        void Start() { ShowRecord(); }
        void ShowRecord() { if (recordText != null) recordText.text = (Best > 0 ? $"Your best: {Best:N0}" : "No game played yet") + "\n" + (Unlocked ? "The Everything Playlist is unlocked here" : $"Score {Bar:N0} to unlock the Everything Playlist"); }

        public void Begin(PlayerInteractor who)
        {
            if (Activity.Any || who == null) return;
            walker = who.GetComponent<SimplePlayerController>(); if (walker == null || walker.cameraPivot == null) return;
            hands = who; backPos = walker.transform.position; backRot = walker.transform.rotation; Active = this; playing = false; shrinkT = 0f; outT = -1f;
        }

        static Transform Sphere(string name, float size, Transform parent, out Material m)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = name; s.transform.SetParent(parent, false); s.transform.localScale = Vector3.one * size;
            m = new Material(WorldMaterials.Glow(Color.white, 1.3f)) { name = name }; s.GetComponent<MeshRenderer>().sharedMaterial = m; return s.transform;
        }

        void Enter()
        {
            walker.Take(); hands.enabled = false; IslandExplorer.Frozen = true; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = false;
            big.gameObject.SetActive(true); Tune(); playing = true; camSet = false; play ??= new PinballPlay(); play.onBig = () => Activity.Cheer(new Color(.4f, .9f, 1f), new Color(1f, .4f, .8f)); play.NewGame();
            if (ball == null)
            {
                ball = Sphere("Pinball", GolfHole.BallR * 4.2f, big, out ballM);   // (drawn a little big, with a light of its own, so it is easy to find on the dark table)
                { var bl = new GameObject("Ball light").AddComponent<Light>(); bl.transform.SetParent(ball, false); bl.transform.localPosition = Vector3.up * 2f; bl.type = LightType.Point; bl.range = 9f; bl.intensity = 3.2f; bl.color = new Color(1f, .95f, .8f); bl.shadows = LightShadows.None; }
                var view = big.GetComponent<GolfCourseView>(); flippers = new[] { view.movers[0], view.movers[1] };
            }
            var c = PlayerOrb.Local != null ? PlayerOrb.Local.Color : Color.white; ballM.SetColor("_BaseColor", c); ballM.SetColor("_EmissionColor", c * 1.3f);
            Say("Hold Space and let go to launch. A and D (or the mouse buttons) work the flippers.", 6f);
        }

        void Quit()
        {
            if (walker != null) walker.Release(backPos, backRot); if (hands != null) hands.enabled = true;
            IslandExplorer.Frozen = false; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = true; Friends.ReportedEye = null; if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null;
            if (big != null) big.gameObject.SetActive(false); playing = false; Send(); Active = null; shrinkT = .55f; outT = -1f;
        }

        void Tune() { var d = MusicDirector.Instance; if (d == null) return; d.Held = Everything ? d.Everything : d.Catalog != null ? d.Catalog.Get(landScene) : null; }
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }

        // (if anything goes wrong in here, you are put back by the table rather than left stranded)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }
        void LateUpdate() { try { Follow(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            shrinkT += Time.deltaTime; Remotes();
            if (Active != this) return;
            if (!playing) { if (shrinkT > .55f) Enter(); else return; }
            if (outT >= 0f) { outT += Time.deltaTime; if (outT > .5f) Quit(); return; }
            var dt = Mathf.Min(Time.deltaTime, .1f); var kb = Keyboard.current; var mouse = Mouse.current; bool left = false, right = false;
            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;
            if (canAct)
            {
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing " + landName + "'s songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f) { leaveHeld = 0f; outT = 0f; return; }
                left = kb.aKey.isPressed || kb.leftArrowKey.isPressed || mouse.leftButton.isPressed; right = kb.dKey.isPressed || kb.rightArrowKey.isPressed || mouse.rightButton.isPressed;
                if (!play.inPlay && play.ballsLeft > 0)
                {
                    if (kb.spaceKey.isPressed) { if (!charging) { charging = true; chargeT = 0f; } chargeT += dt; power = Mathf.PingPong(chargeT * .9f, 1f); }
                    else if (charging) { charging = false; play.Launch(power); }
                }
                if (play.ballsLeft <= 0 && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) play.NewGame();
            }
            var ballsBefore = play.ballsLeft; play.Step(dt, left, right);
            if (play.ballsLeft < ballsBefore)
            {
                if (play.ballsLeft > 0) Say($"Drained. {play.ballsLeft} {(play.ballsLeft == 1 ? "ball" : "balls")} left", 3f);
                else
                {
                    if (play.score > Best) PlayerPrefs.SetInt(Key("best"), play.score);
                    if (play.score >= Bar && !Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say($"{play.score:N0}!\nThe Everything Playlist is unlocked here. P switches it.", 9f); }
                    else Say($"Game over: {play.score:N0}", 6f);
                    PlayerPrefs.Save(); ShowRecord();
                }
            }
            if (Time.time >= nextSend) { nextSend = Time.time + .1f; Send(); }
        }

        void Follow()
        {
            if (Active != this || walker == null || !playing || play == null) return;
            var table = play.table; var p = play.sim.p + Vector3.up * GolfHole.BallR; ball.localPosition = p; ball.gameObject.SetActive(play.ballsLeft > 0);
            for (var i = 0; i < 2; i++) { var m = table.movers[i]; flippers[i].localPosition = new Vector3(m.pivot.x, m.y, m.pivot.y); flippers[i].localRotation = Quaternion.Euler(0, -m.manualAngle, 0); }
            // the camera rides behind and above the ball, looking up the table; it keeps the flippers in sight when the ball is low
            // (waiting in the launch lane, it looks straight at the ball, close, so you can see where you are)
            var waiting = !play.inPlay;
            // (it was too far off and aimed past the ball: you could not find yourself. Now it sits close behind the
            // ball and looks straight at it; in play it pulls back only enough to show the flippers and what is ahead.)
            var follow = waiting ? p : new Vector3(Mathf.Clamp(p.x * .85f, -8f, 8f), p.y, Mathf.Max(p.z, 5f));
            var at = big.TransformPoint(follow + (waiting ? new Vector3(-1.2f, 2.6f, -4.4f) : new Vector3(0, 7.5f, -8f))); var look = big.TransformPoint(follow + new Vector3(0, .2f, waiting ? .6f : 2.2f));
            if (!camSet) { camAt = at; camSet = true; } camAt = Vector3.Lerp(camAt, at, 1f - Mathf.Exp(-6f * Time.deltaTime));
            var dir = (look - at).normalized; var yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; var pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg; var body = Quaternion.Euler(0, yaw, 0); var pivot = walker.cameraPivot;
            walker.transform.SetPositionAndRotation(camAt - body * pivot.localPosition, body); pivot.localEulerAngles = new Vector3(pitch, 0, 0);
            Friends.ReportedEye = mini != null ? mini.TransformPoint(play.sim.p) + Vector3.up * 1.3f : (Vector3?)null;
        }

        // ---------------------------------------------------------------- friends
        void Send()
        {
            var f = Friends.Instance; if (f == null || !f.Online) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                var p = play != null ? play.sim.p : Vector3.zero; w.Write((byte)1); w.Write(id); w.Write(Active == this && playing); w.Write(p.x); w.Write(p.y); w.Write(p.z); w.Write(play != null ? play.score : 0); w.Write((byte)(play != null ? play.ballsLeft : 0));
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
                    o.on = r.ReadBoolean(); o.pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); o.score = r.ReadInt32(); o.balls = r.ReadByte();
                    o.color = PlayerOrb.Palette[Mathf.Clamp(r.ReadByte(), 0, PlayerOrb.Palette.Length - 1)].color; o.name = r.ReadString(); o.seen = o.on ? Time.time : -99f;
                }
            }
            catch (System.Exception) { }
        }

        void Remotes()
        {
            foreach (var o in others.Values)
            {
                var on = Time.time - o.seen < 3f && o.balls > 0;
                if (o.ball == null && on && big != null) { o.ball = Sphere("Friend's pinball", GolfHole.BallR * 2f, big, out var bm); bm.SetColor("_BaseColor", o.color); bm.SetColor("_EmissionColor", o.color * 1.3f); o.dot = Sphere("Friend's pinball on the model", .05f, transform, out var dm); dm.SetColor("_EmissionColor", o.color * 3f); o.shown = o.pos; }
                if (o.ball == null) continue; o.ball.gameObject.SetActive(on); o.dot.gameObject.SetActive(on && mini != null); if (!on) continue;
                o.shown = (o.shown - o.pos).sqrMagnitude > 100f ? o.pos : Vector3.Lerp(o.shown, o.pos, 1f - Mathf.Exp(-14f * Time.deltaTime));
                o.ball.localPosition = o.shown + Vector3.up * GolfHole.BallR; if (mini != null) o.dot.position = mini.TransformPoint(o.shown) + Vector3.up * .03f;
            }
        }

        /// For the photo tour only: steps onto the table, launches a ball and flips for a few seconds.
        public System.Collections.IEnumerator Demo(PlayerInteractor who)
        {
            Begin(who); var until = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < until) yield return null;
            if (Active != this || !playing) { Debug.LogWarning("Pinball demo: the game did not start"); yield break; }
            play.Launch(.7f); var t = 0f; while (t < 5f) { play.Step(Mathf.Min(Time.deltaTime, .1f), (int)(t * 2) % 2 == 0, (int)(t * 2) % 2 == 1); t += Time.deltaTime; yield return null; }
            Debug.Log($"Pinball demo: ball at {play.sim.p}, score {play.score}, balls left {play.ballsLeft}, in play {play.inPlay}; camera at {big.InverseTransformPoint(Camera.main.transform.position)} looking {Camera.main.transform.forward}");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        // ---------------------------------------------------------------- what you see on the screen
        GUIStyle box, big2, small2; static Texture2D white;

        void OnGUI()
        { EverythingWorld.World.Ui.Begin();
            white ??= Texture2D.whiteTexture; Activity.DrawCheer();
            var wash = outT >= 0f ? outT / .5f : shrinkT < 1.1f ? 1f - Mathf.Abs(shrinkT - .55f) / .55f : 0f;
            if (wash > 0f && Event.current.type == EventType.Repaint) { var keep = GUI.color; GUI.color = new Color(1f, .8f, .9f, Mathf.Clamp01(wash)); GUI.DrawTexture(new Rect(0, 0, EverythingWorld.World.Ui.W, EverythingWorld.World.Ui.H), white); GUI.color = keep; }
            if (Active != this || !playing || play == null || MainMenu.Showing) return;
            box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(26, TextAnchor.MiddleCenter, true, true); small2 ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            GUI.Box(new Rect(12, 12, 470, 34), $"  Score {play.score:N0}  ·  balls {play.ballsLeft}" + (Best > 0 ? $"  ·  your best {Best:N0}" : ""), box);
            var y = 50f; if (Everything) { GUI.Box(new Rect(12, y, 240, 30), "  Everything Playlist", box); y += 34f; }
            foreach (var o in others.Values) if (Time.time - o.seen < 3f) { GUI.Box(new Rect(12, y, 330, 28), $"  {o.name}: {o.score:N0}", box); y += 30f; }
            if (Time.time - play.lastHitAt < 1.2f && play.lastHit.Length > 0 && Time.time > bannerUntil) GUI.Box(new Rect(EverythingWorld.World.Ui.W / 2f - 150, 96, 300, 40), play.lastHit, small2);
            if (Time.time < bannerUntil && banner.Length > 0) Activity.Banner(banner, 96, big2);
            if (charging) { var w = 360f; var r = new Rect(EverythingWorld.World.Ui.W / 2f - w / 2f, EverythingWorld.World.Ui.H - 96, w, 22); GUI.Box(r, "", small2); var keep = GUI.color; GUI.color = Color.Lerp(new Color(.4f, .9f, 1f), new Color(1f, .75f, .2f), power); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, (r.width - 6) * power, r.height - 6), white); GUI.color = keep; }
            Activity.Hint(EverythingWorld.World.Ui.H - 66, play.ballsLeft <= 0 ? "Enter  New game   ·   hold X  Leave the table" : !play.inPlay ? "Hold Space and let go to launch  ·  hold X to leave" : "A and D (or the mouse buttons) work the flippers  ·  hold X to leave" + (Unlocked ? "  ·  P playlist" : ""), small2);
        }
    }
}
