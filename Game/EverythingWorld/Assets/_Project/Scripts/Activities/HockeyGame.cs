using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// The thing you press E on to play air hockey (the coin post by the table).
    public class HockeyStart : Interactable
    {
        public HockeyGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Play";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// Air hockey, on a table you shrink onto (the Game Isles' activity, 2026-11-17).
    /// - Press E at the coin post: you shrink onto the table, at one end. Move the mouse to move your mallet. First to seven.
    /// - Alone, the far mallet plays itself. The first friend to join takes it over; anyone after that watches.
    /// - Win a game and the table's radio can play the whole Everything Playlist (P switches).
    /// What friends share: the first player's game moves the puck and tells everyone where it is, thirty times a second;
    /// the second player sends where their mallet is. (The second player feels a little delay in their hits: the puck is
    /// decided on the first player's computer.)
    public class HockeyGame : MonoBehaviour
    {
        public static HockeyGame Active { get; private set; }
        public Transform big, small; public float bigScale = 30f; public int landScene; public string id = "bit"; public string landName = "the Game Isles"; public TextMesh recordText;
        const string Channel = "hockey"; const int Target = 7;
        const float RobotSpeed = 2.5f, RobotTop = 3.3f;   // the table's own mallet: slow at the start of a point, quicker as the point goes on (it follows sim.rally)

        readonly HockeySim sim = new HockeySim(); Transform[] bigBits, smallBits; bool playing, wasLocked; float shrinkT = 9f, outT = -1f, leaveHeld, nextSend, acc; double joined; int winner = -1, gameNo;
        Vector2 want; SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot; string banner = ""; float bannerUntil; Vector3 camAt, camLook; bool camSet;
        class Remote { public string name = "Friend"; public double joined; public float seen; public bool playing; public Vector2 mallet; }
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();

        string Key(string what) => $"EW_Hockey_{id}_{what}";
        int Wins { get => PlayerPrefs.GetInt(Key("wins"), 0); set => PlayerPrefs.SetInt(Key("wins"), value); }
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        public string StartPrompt => "Play air hockey" + (Wins > 0 ? $"  ·  games won: {Wins}" : "");
        ulong Me => Friends.Instance != null && Friends.Instance.Online ? Friends.Instance.LocalId : 0;

        void OnEnable() { Friends.Received += OnNet; }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }
        void Start() { sim.Reset(); if (big != null) bigBits = Bits(big, bigScale); if (small != null) smallBits = Bits(small, 1f); ShowRecord(); Place(); }

        void ShowRecord() { if (recordText != null) recordText.text = (Wins > 0 ? $"Games you've won: {Wins}" : "No game won yet") + "\n" + (Unlocked ? "The Everything Playlist is unlocked here" : "Win a game to unlock the Everything Playlist"); }

        /// The puck and the two mallets as glowing discs.
        static Transform[] Bits(Transform parent, float s)
        {
            Transform Disc(string name, float r, float h, Color c)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(g.GetComponent<Collider>()); g.name = name; g.transform.SetParent(parent, false); g.transform.localScale = new Vector3(r * 2f, h / 2f, r * 2f) * s;
                g.GetComponent<MeshRenderer>().sharedMaterial = new Material(WorldMaterials.Glow(c, 1.6f)) { name = name }; return g.transform;
            }
            return new[] { Disc("Puck", HockeySim.PuckR, .012f, new Color(1f, .85f, .2f)), Disc("Mallet (near)", HockeySim.MalletR, .05f, new Color(.3f, .9f, 1f)), Disc("Mallet (far)", HockeySim.MalletR, .05f, new Color(1f, .4f, .7f)) };
        }

        public static void Table(MeshKit mk, float s, bool legs)
        {
            // (the table is pale and barely lit: any brighter and the bloom hides its lines)
            var ice = new Material(WorldMaterials.Glow(new Color(.62f, .78f, .95f), .08f)) { name = "Hockey table" }; var line = WorldMaterials.Glow(new Color(.2f, .5f, 1f), 1.2f); var red = WorldMaterials.Glow(new Color(1f, .3f, .4f), 1.4f);
            var rail = WorldMaterials.Solid(new Color(.06f, .06f, .12f)); var gold = WorldMaterials.Glow(new Color(1f, .82f, .2f), 1.2f); var up = Vector3.up; float W = HockeySim.W * s, L = HockeySim.L * s, t = .06f * s, h = .03f * s, g = HockeySim.Goal * s;
            mk.Box(ice, -up * (.02f * s), new Vector3(W, .04f * s, L)); mk.Box(red, up * (.0006f * s), new Vector3(W, .001f * s, .012f * s)); mk.Prism(line, up * (.0004f * s), up, .16f * s, .001f * s, 20); mk.Prism(ice, up * (.0008f * s), up, .15f * s, .001f * s, 20);
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(rail, new Vector3(e * (W / 2f + t / 2f), h / 2f, 0), new Vector3(t, h + .02f * s, L + 2 * t)); mk.Box(line, new Vector3(0, .0006f * s, e * L / 4f), new Vector3(W, .001f * s, .01f * s));
                foreach (var f in new[] { -1f, 1f }) { var len = (W - g) / 2f; mk.Box(rail, new Vector3(f * (g / 2f + len / 2f), h / 2f, e * (L / 2f + t / 2f)), new Vector3(len, h + .02f * s, t)); }
                mk.Box(gold, new Vector3(0, .002f * s, e * (L / 2f + t * .5f)), new Vector3(g, .004f * s, t * .8f));   // the goal mouth, glowing
            }
            mk.Box(rail, -up * (.12f * s), new Vector3(W + 2 * t, .16f * s, L + 2 * t));
            if (legs) foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(rail, new Vector3(x * (W / 2f - .05f * s), -.5f * s, z * (L / 2f - .1f * s)), new Vector3(.1f * s, .6f * s, .1f * s));
        }

        // ---------------------------------------------------------------- in and out
        public void Begin(PlayerInteractor who)
        {
            if (Activity.Any || who == null) return;
            walker = who.GetComponent<SimplePlayerController>(); if (walker == null || walker.cameraPivot == null) return;
            hands = who; backPos = walker.transform.position; backRot = walker.transform.rotation; Active = this; playing = false; shrinkT = 0f; outT = -1f;
        }

        void Enter()
        {
            walker.Take(); hands.enabled = false; IslandExplorer.Frozen = true; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = false;
            big.gameObject.SetActive(true); Tune(); joined = SharedChance.Now; playing = true; camSet = false;
            if (Seat() == 0) { sim.Reset(); winner = -1; gameNo++; } lastMine = 0;
            want = sim.mallet[Mathf.Clamp(Seat(), 0, 1)]; Say(Seat() > 1 ? "Two are already playing: you're watching" : "First to seven. Move the mouse to move your mallet.", 5f);
        }

        void Quit()
        {
            if (walker != null) walker.Release(backPos, backRot); if (hands != null) hands.enabled = true;
            IslandExplorer.Frozen = false; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = true; Friends.ReportedEye = null; if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null;
            if (big != null) big.gameObject.SetActive(false); playing = false; Send(); Active = null; shrinkT = .55f; outT = -1f;
        }

        void Tune() { var d = MusicDirector.Instance; if (d == null) return; d.Held = Everything ? d.Everything : d.Catalog != null ? d.Catalog.Get(landScene) : null; }
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }

        List<(ulong id, double joined)> Roster()
        {
            var r = new List<(ulong, double)>(); if (Active == this && playing) r.Add((Me, joined));
            foreach (var kv in others) if (Time.time - kv.Value.seen < 3f && kv.Value.playing) r.Add((kv.Key, kv.Value.joined));
            r.Sort((a, b) => a.Item2 != b.Item2 ? a.Item2.CompareTo(b.Item2) : a.Item1.CompareTo(b.Item1)); return r;
        }
        /// 0 = the near end (and the one whose game moves the puck), 1 = the far end, 2 and up = watching.
        int Seat() { var r = Roster(); for (var i = 0; i < r.Count; i++) if (r[i].id == Me) return i; return 0; }

        // (if anything goes wrong in here, you are put back by the table rather than left stranded)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }
        void LateUpdate() { try { Follow(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            shrinkT += Time.deltaTime; Place();
            if (Active != this) return;
            if (!playing) { if (shrinkT > .55f) Enter(); else return; }
            if (outT >= 0f) { outT += Time.deltaTime; if (outT > .5f) Quit(); return; }
            var dt = Time.deltaTime; var kb = Keyboard.current; var mouse = Mouse.current; var seat = Seat(); var roster = Roster();
            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;
            if (canAct)
            {
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing " + landName + "'s songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f) { leaveHeld = 0f; outT = 0f; return; }
                if (seat < 2) { var d = mouse.delta.ReadValue() * walker.mouseSensitivity * .012f * (seat == 0 ? 1f : -1f); want += d; want.x = Mathf.Clamp(want.x, -HockeySim.W / 2, HockeySim.W / 2); want.y = seat == 0 ? Mathf.Clamp(want.y, -HockeySim.L / 2, 0) : Mathf.Clamp(want.y, 0, HockeySim.L / 2); }
            }
            if (canAct && winner >= 0 && seat < 2 && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) { sim.Reset(); winner = -1; gameNo++; Say("New game", 2f); nextSend = 0f; }
            if (seat < 2)
            {
                // Each player's own game moves the puck while it is in their half and tells the other where it is, so your
                // hits land the moment you make them at either end (the far player used to see everything late). In the
                // other half this game only follows along, and takes the other player's word.
                var other = roster.Count > 1 && others.TryGetValue(roster[1 - seat].id, out var o) ? o : null;
                sim.judge = other == null || Owns(seat);
                acc += dt; var n = Mathf.Min(60, (int)(acc / HockeySim.Step)); acc -= n * HockeySim.Step;
                for (var k = 0; k < n; k++)
                {
                    sim.Move(seat, want, 9f, HockeySim.Step);
                    if (other != null) sim.Move(1 - seat, other.mallet, 9f, HockeySim.Step); else sim.Move(1, sim.Robot(1), Mathf.Lerp(RobotSpeed, RobotTop, Mathf.Clamp01(sim.rally / 28f)), HockeySim.Step);
                    if (winner >= 0) continue; var scored = sim.Advance(HockeySim.Step);
                    if (scored >= 0) { nextSend = 0f; if (sim.score[scored] >= Target) { winner = scored; Over(); } else { Say(scored == seat ? "Goal!" : "They score", 1.5f); if (scored == seat) Activity.Cheer(); } lastMine = sim.score[seat]; }
                }
            }
            if (Time.time >= nextSend) { nextSend = Time.time + .033f; Send(); }
        }

        int lastMine;

        /// Whose half the puck is in (the near end's on the line itself).
        bool Owns(int seat) => sim.puck.y <= 0f ? seat == 0 : seat == 1;

        void Over()
        {
            var seat = Seat(); var mine = seat < 2 && winner == seat;
            if (mine) { Wins++; if (!Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say("You win!\nThe Everything Playlist is unlocked here. P switches it.", 9f); } else Say("You win!", 6f); PlayerPrefs.Save(); ShowRecord(); }
            else Say(seat < 2 ? "They win" : "Game over", 6f);
        }

        void Place()
        {
            void Put(Transform[] b, float s) { if (b == null) return; b[0].localPosition = new Vector3(sim.puck.x, .007f, sim.puck.y) * s; b[1].localPosition = new Vector3(sim.mallet[0].x, .026f, sim.mallet[0].y) * s; b[2].localPosition = new Vector3(sim.mallet[1].x, .026f, sim.mallet[1].y) * s; }
            Put(bigBits, bigScale); Put(smallBits, 1f);
        }

        void Follow()
        {
            if (Active != this || walker == null || !playing) return;
            var s = bigScale; var seat = Seat(); var end = seat == 1 ? 1f : -1f;
            var at = seat < 2 ? big.TransformPoint(new Vector3(0, 30f, end * (HockeySim.L / 2f * s + 20f))) : big.TransformPoint(new Vector3(HockeySim.W * s + 26f, 34f, 0));
            var look = seat < 2 ? big.TransformPoint(new Vector3(0, 0, end * 7f)) : big.position;
            if (!camSet) { camAt = at; camLook = look; camSet = true; } camAt = Vector3.Lerp(camAt, at, 1f - Mathf.Exp(-4f * Time.deltaTime)); camLook = Vector3.Lerp(camLook, look, 1f - Mathf.Exp(-4f * Time.deltaTime));
            var dir = (camLook - camAt).normalized; var yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; var pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg; var body = Quaternion.Euler(0, yaw, 0); var pivot = walker.cameraPivot;
            walker.transform.SetPositionAndRotation(camAt - body * pivot.localPosition, body); pivot.localEulerAngles = new Vector3(pitch, 0, 0);
            Friends.ReportedEye = small != null ? small.TransformPoint(new Vector3(0, .1f, end * (HockeySim.L / 2f + .5f))) + Vector3.up * 1.3f : (Vector3?)null;
        }

        // ---------------------------------------------------------------- friends
        void Send()
        {
            var f = Friends.Instance; if (f == null || !f.Online) return; var seat = Active == this && playing ? Seat() : 9;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((byte)2); w.Write(id); w.Write(Active == this && playing); w.Write(joined); w.Write(Friends.PlayerName); w.Write((byte)seat);
                var m = sim.mallet[Mathf.Clamp(seat, 0, 1)]; w.Write(m.x); w.Write(m.y);
                if (seat < 2) { w.Write(sim.judge && Owns(seat)); w.Write(sim.puck.x); w.Write(sim.puck.y); w.Write(sim.pv.x); w.Write(sim.pv.y); w.Write(sim.serveWait); w.Write(sim.rally); w.Write(sim.mallet[1 - seat].x); w.Write(sim.mallet[1 - seat].y); w.Write(gameNo); w.Write((byte)sim.score[0]); w.Write((byte)sim.score[1]); w.Write((sbyte)winner); }
                w.Flush(); f.Broadcast(Channel, ms.ToArray(), false);
            }
        }

        void OnNet(ulong from, string channel, byte[] data)
        {
            if (channel != Channel) return;
            try
            {
                using (var r = new BinaryReader(new MemoryStream(data)))
                {
                    if (r.ReadByte() != 2 || r.ReadString() != id) return; if (!others.TryGetValue(from, out var o)) others[from] = o = new Remote();
                    o.playing = r.ReadBoolean(); o.joined = r.ReadDouble(); o.name = r.ReadString(); var seat = r.ReadByte(); o.mallet = new Vector2(r.ReadSingle(), r.ReadSingle()); o.seen = o.playing ? Time.time : -99f;
                    if (seat >= 2) return;
                    var owns = r.ReadBoolean(); var rp = new Vector2(r.ReadSingle(), r.ReadSingle()); var rv = new Vector2(r.ReadSingle(), r.ReadSingle()); var wait = r.ReadSingle(); var rally = r.ReadSingle();
                    var otherMallet = new Vector2(r.ReadSingle(), r.ReadSingle()); var game = r.ReadInt32(); int s0 = r.ReadByte(), s1 = r.ReadByte(); var w = r.ReadSByte();
                    var mine = Active == this && playing ? Seat() : 9;
                    if (mine >= 2) { sim.mallet[seat] = o.mallet; if (owns) { sim.puck = rp; sim.pv = rv; sim.mallet[1 - seat] = otherMallet; } }      // watching, or walking past the model
                    else if (owns && (!Owns(mine) || seat < mine))
                    {
                        // the puck is theirs just now: go where they say it is (gently, unless it is far off) and as fast
                        sim.puck = (sim.puck - rp).sqrMagnitude > .04f ? rp : Vector2.Lerp(sim.puck, rp, .5f); sim.pv = rv; sim.serveWait = wait; sim.rally = rally;
                    }
                    // the score: a newer game's replaces mine; in the same game a goal either of us has counted stands
                    var before = sim.score[0] + sim.score[1];
                    if (game > gameNo) { gameNo = game; sim.score[0] = s0; sim.score[1] = s1; winner = w; }
                    else if (game == gameNo)
                    {
                        sim.score[0] = Mathf.Max(sim.score[0], s0); sim.score[1] = Mathf.Max(sim.score[1], s1);
                        if (mine < 2 && sim.score[0] + sim.score[1] > before && w < 0) { Say(sim.score[mine] > lastMine ? "Goal!" : "They score", 1.5f); if (sim.score[mine] > lastMine) Activity.Cheer(); } if (mine < 2) lastMine = sim.score[mine];
                        if (w >= 0 && winner < 0) { winner = w; if (Active == this && playing) Over(); }
                    }
                }
            }
            catch (System.Exception) { }
        }

        /// For the photo tour only: steps onto the table and lets the far mallet play for a few seconds.
        public System.Collections.IEnumerator Demo(PlayerInteractor who)
        {
            Begin(who); var until = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < until) yield return null;
            if (Active != this || !playing) { Debug.LogWarning("Hockey demo: the game did not start"); yield break; }
            sim.pv = new Vector2(.6f, 2.5f); until = Time.realtimeSinceStartup + 4f; while (Time.realtimeSinceStartup < until) yield return null;
            Debug.Log($"Hockey demo: puck at {sim.puck}, speed {sim.pv.magnitude:0.0}, score {sim.score[0]}-{sim.score[1]}, far mallet at {sim.mallet[1]}");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        // ---------------------------------------------------------------- what you see on the screen
        GUIStyle box, big2, small2; static Texture2D white;

        void OnGUI()
        { EverythingWorld.World.Ui.Begin();
            white ??= Texture2D.whiteTexture; Activity.DrawCheer();
            var wash = outT >= 0f ? outT / .5f : shrinkT < 1.1f ? 1f - Mathf.Abs(shrinkT - .55f) / .55f : 0f;
            if (wash > 0f && Event.current.type == EventType.Repaint) { var keep = GUI.color; GUI.color = new Color(.8f, .9f, 1f, Mathf.Clamp01(wash)); GUI.DrawTexture(new Rect(0, 0, EverythingWorld.World.Ui.W, EverythingWorld.World.Ui.H), white); GUI.color = keep; }
            if (Active != this || !playing || MainMenu.Showing) return;
            box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(26, TextAnchor.MiddleCenter, true, true); small2 ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            var seat = Seat(); var mine = seat == 1 ? 1 : 0; var r = Roster(); var them = r.Count > 1 ? (others.TryGetValue(r[seat == 0 ? 1 : 0].id, out var o) ? o.name : "Friend") : "The table";
            GUI.Box(new Rect(12, 12, 470, 34), seat < 2 ? $"  You {sim.score[mine]}  ·  {them} {sim.score[1 - mine]}  ·  first to {Target}" : $"  {sim.score[0]} - {sim.score[1]}  ·  first to {Target}  (watching)", box);
            if (Everything) GUI.Box(new Rect(12, 50, 240, 30), "  Everything Playlist", box);
            if (Time.time < bannerUntil && banner.Length > 0) Activity.Banner(banner, 96, big2);
            Activity.Hint(EverythingWorld.World.Ui.H - 66, winner >= 0 && seat == 0 ? "Enter  New game   ·   hold X  Leave the table" : seat < 2 ? "Move the mouse to move your mallet  ·  hold X to leave" + (Unlocked ? "  ·  P playlist" : "") : "Hold X to leave", small2);
        }
    }
}
