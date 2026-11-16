using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// The thing you press E on to play the cabinet.
    public class ArcadeStart : Interactable
    {
        public ArcadeGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Play";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// An arcade cabinet you can really play (the Game Isles, 2026-11-17): BRICKS, our own bat-ball-and-wall game.
    /// - Press E at the cabinet: its screen fills yours. Move the mouse to move the bat, click to serve. Three lives;
    ///   each wall you clear brings the next one lower and the ball faster.
    /// - Friends at their own cabinets anywhere in the Isles see each other's scores.
    /// - Score 5,000 and the cabinet's radio can play the whole Everything Playlist (P switches).
    public class ArcadeGame : MonoBehaviour
    {
        public static ArcadeGame Active { get; private set; }
        public int landScene; public string id = "bricks"; public string landName = "the Game Isles"; public TextMesh recordText;
        const string Channel = "arcade"; const int Bar = 5000;

        readonly BricksPlay play = new BricksPlay(); float target = .5f, leaveHeld, nextSend; bool wasLocked, over; string banner = ""; float bannerUntil;
        SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot;
        class Remote { public string name = "Friend"; public int score; public float seen; }
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();
        static readonly Color[] RowColours = { new Color(1f, .3f, .4f), new Color(1f, .6f, .2f), new Color(1f, .86f, .3f), new Color(.4f, .95f, .5f), new Color(.3f, .8f, 1f), new Color(.7f, .5f, 1f) };

        string Key(string what) => $"EW_Arcade_{id}_{what}";
        public int Best => PlayerPrefs.GetInt(Key("best"), 0);
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        public string StartPrompt => "Play BRICKS" + (Best > 0 ? $"  ·  your best {Best:N0}" : "");

        void OnEnable() { Friends.Received += OnNet; }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }
        void Start() { ShowRecord(); }
        void ShowRecord() { if (recordText != null) recordText.text = (Best > 0 ? $"High score: {Best:N0}" : "No score yet") + "\n" + (Unlocked ? "Everything Playlist unlocked" : $"{Bar:N0} unlocks the Everything Playlist"); }

        public void Begin(PlayerInteractor who)
        {
            if (Activity.Any || who == null) return;
            walker = who.GetComponent<SimplePlayerController>(); if (walker == null) return;
            hands = who; backPos = walker.transform.position; backRot = walker.transform.rotation; Active = this; walker.Take(); hands.enabled = false; play.NewGame(); over = false; target = .5f; Tune();
            Say("Move the mouse to move the bat. Click to serve.", 4f);
        }

        void Quit()
        {
            if (walker != null) walker.Release(backPos, backRot); if (hands != null) hands.enabled = true; if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null; Send(false); Active = null;
        }

        void Tune() { var d = MusicDirector.Instance; if (d == null) return; d.Held = Everything ? d.Everything : null; }
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }

        // (if anything goes wrong in here, you are let go of the cabinet rather than left stuck at it)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            if (Active != this) return;
            var dt = Mathf.Min(Time.deltaTime, .05f); var kb = Keyboard.current; var mouse = Mouse.current;
            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;
            if (canAct)
            {
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing " + landName + "'s songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f || kb.eKey.wasPressedThisFrame) { leaveHeld = 0f; Quit(); return; }
                target = Mathf.Clamp01(target + mouse.delta.ReadValue().x * walker.mouseSensitivity * .012f);
                if (mouse.leftButton.wasPressedThisFrame) { if (play.lives <= 0) { play.NewGame(); over = false; } else play.Serve(); }
            }
            if (!MainMenu.Showing) play.Step(dt, target);
            if (play.lives <= 0 && !over)
            {
                over = true; if (play.score > Best) PlayerPrefs.SetInt(Key("best"), play.score);
                if (play.score >= Bar && !Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say($"{play.score:N0}!\nThe Everything Playlist is unlocked here. P switches it.", 9f); } else Say($"Game over: {play.score:N0}", 5f);
                PlayerPrefs.Save(); ShowRecord();
            }
            if (Time.time >= nextSend) { nextSend = Time.time + .5f; Send(true); }
        }

        void Send(bool on)
        {
            var f = Friends.Instance; if (f == null || !f.Online) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms)) { w.Write((byte)1); w.Write(id); w.Write(on); w.Write(play.score); w.Write(Friends.PlayerName); w.Flush(); f.Broadcast(Channel, ms.ToArray(), false); }
        }

        void OnNet(ulong from, string channel, byte[] data)
        {
            if (channel != Channel) return;
            try { using (var r = new BinaryReader(new MemoryStream(data))) { if (r.ReadByte() != 1 || r.ReadString() != id) return; if (!others.TryGetValue(from, out var o)) others[from] = o = new Remote(); var on = r.ReadBoolean(); o.score = r.ReadInt32(); o.name = r.ReadString(); o.seen = on ? Time.time : -99f; } }
            catch (System.Exception) { }
        }

        /// For the photo tour only: sits at the cabinet and plays a few seconds with the bat following the ball.
        public System.Collections.IEnumerator Demo(PlayerInteractor who)
        {
            Begin(who); if (Active != this) { Debug.LogWarning("Arcade demo: the game did not start"); yield break; }
            play.Serve(); var t = 0f; while (t < 5f) { target = play.ball.x; t += Time.deltaTime; yield return null; }
            Debug.Log($"Arcade demo: score {play.score}, bricks left {play.left}, lives {play.lives}, ball at {play.ball}");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        // ---------------------------------------------------------------- the screen
        GUIStyle box, big2, small2; static Texture2D white;

        void OnGUI()
        {
            if (Active != this || MainMenu.Showing) return;
            white ??= Texture2D.whiteTexture; box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(26, TextAnchor.MiddleCenter, true, true); small2 ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            var keep = GUI.color; var h = Screen.height * .78f; var w = h / BricksPlay.Top; var x0 = Screen.width / 2f - w / 2f; var y0 = Screen.height / 2f - h / 2f + 10f;
            void Fill(float x, float y, float fw, float fh, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(x0 + x * w, y0 + (BricksPlay.Top - y - fh) * w, fw * w, fh * w), white); }   // (field units, y up)
            if (Event.current.type == EventType.Repaint)
            {
                GUI.color = new Color(0, 0, 0, .55f); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
                GUI.color = new Color(.25f, .9f, 1f); GUI.DrawTexture(new Rect(x0 - 6, y0 - 6, w + 12, h + 12), white); GUI.color = new Color(.03f, .03f, .09f); GUI.DrawTexture(new Rect(x0, y0, w, h), white);
                for (var r = 0; r < BricksPlay.Rows; r++) for (var c = 0; c < BricksPlay.Cols; c++) if (play.brick[r * BricksPlay.Cols + c]) { var b = play.BrickRect(c, r); Fill(b.x, b.y, b.width, b.height, RowColours[r % RowColours.Length]); }
                Fill(play.batX - BricksPlay.BatW / 2f, BricksPlay.BatY - .012f, BricksPlay.BatW, .024f, Color.white);
                Fill(play.ball.x - BricksPlay.BallR, play.ball.y - BricksPlay.BallR, BricksPlay.BallR * 2f, BricksPlay.BallR * 2f, new Color(1f, .95f, .5f));
                GUI.color = keep;
            }
            GUI.Box(new Rect(12, 12, 470, 34), $"  BRICKS  ·  score {play.score:N0}  ·  lives {Mathf.Max(0, play.lives)}  ·  wall {play.level}" + (Best > 0 ? $"  ·  best {Best:N0}" : ""), box);
            var y = 50f; if (Everything) { GUI.Box(new Rect(12, y, 240, 30), "  Everything Playlist", box); y += 34f; }
            foreach (var o in others.Values) if (Time.time - o.seen < 4f) { GUI.Box(new Rect(12, y, 330, 28), $"  {o.name}: {o.score:N0}", box); y += 30f; }
            if (Time.time < bannerUntil && banner.Length > 0) GUI.Box(new Rect(Screen.width / 2f - 360, y0 + h * .45f, 720, banner.Contains("\n") ? 92f : 54f), banner, big2);
            GUI.Box(new Rect(Screen.width / 2f - 330, Screen.height - 40, 660, 28), play.lives <= 0 ? "Click  New game   ·   E  Step away" : !play.served ? "Click to serve  ·  E to step away" : "Move the mouse  ·  E to step away" + (Unlocked ? "  ·  P playlist" : ""), small2);
        }
    }
}
