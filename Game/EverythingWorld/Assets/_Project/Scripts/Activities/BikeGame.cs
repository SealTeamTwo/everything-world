using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// The thing you press E on to ride (the parked bike).
    public class BikeStart : Interactable
    {
        public BikeGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Ride";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// Light bikes, on a grid you shrink onto (the Synthwave Coast's activity, 2026-11-17).
    /// - Press E at the parked bike: you shrink onto the grid, a square arena built out of sight high over the Coast.
    /// - Your bike never stops and leaves a wall of light behind it. A and D (or the arrows, or the two mouse buttons)
    ///   turn it left and right, always by a right angle. Touch any wall, your own included, and you're out. Last one
    ///   riding wins the round.
    /// - A round is a count-in of four, then the ride, then three seconds to see who won, then the next count-in. It
    ///   ends as soon as one bike is left (or no player is), so nobody waits. Whoever on the grid has the lowest
    ///   number calls the rounds and the rest follow, so friends are always in the same one. Step on while a round is
    ///   being ridden and it is cut short, so you ride the next together. Alone, three bikes ride themselves against you.
    /// - Win three rounds and the grid's radio can play the whole Everything Playlist (P switches).
    /// What friends share: each player's game rides their own bike and says where it is and where it has turned;
    /// each decides for itself when it has crashed.
    public class BikeGame : MonoBehaviour
    {
        public static BikeGame Active { get; private set; }
        public Transform big; public int landScene; public string id = "synth"; public string landName = "the Coast"; public TextMesh recordText;
        const string Channel = "bikes"; const double CountIn = 4, MaxRide = 45, After = 3; const int Bar = 3;
        double roundStart = -1, overAt = -1; float enteredAt; bool startDue;   // when this round's count-in began and when it was over (shared clock)

        readonly Bike mine = new Bike(); readonly List<Bike> robots = new List<Bike>(); readonly List<Bike> all = new List<Bike>(); System.Random rng = new System.Random(1);
        long round = -1; bool playing, inRound, wonThis, wasLocked; float shrinkT = 9f, outT = -1f, leaveHeld, nextSend, camYaw; Vector3 camAt; bool camSet; string banner = ""; float bannerUntil;
        SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot;
        class Remote { public Bike bike = new Bike(); public long round; public double start, over = -1; public bool inRound; public float seen; public bool playing; public Color color = Color.white; public string name = "Friend"; }
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();
        // drawing: a pool of glowing boxes for the walls of light, and one for each bike
        readonly List<Transform> walls = new List<Transform>(); readonly List<Transform> bodies = new List<Transform>(); readonly Dictionary<Color, Material> glows = new Dictionary<Color, Material>(); int wallsUsed, bodiesUsed;
        static readonly Color[] RobotColours = { new Color(1f, .05f, .55f), new Color(1f, .6f, 0f), new Color(.4f, .12f, 1f) };

        string Key(string what) => $"EW_Bikes_{id}_{what}";
        int Wins { get => PlayerPrefs.GetInt(Key("wins"), 0); set => PlayerPrefs.SetInt(Key("wins"), value); }
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        public string StartPrompt => "Ride the light bikes" + (Wins > 0 ? $"  ·  rounds won: {Wins}" : "");
        ulong Me => Friends.Instance != null && Friends.Instance.Online ? Friends.Instance.LocalId : 0;

        void OnEnable() { Friends.Received += OnNet; }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }
        void Start() { ShowRecord(); }
        void ShowRecord() { if (recordText != null) recordText.text = $"Rounds you've won: {Wins}" + "\n" + (Unlocked ? "The Everything Playlist is unlocked here" : $"Win {Bar} rounds to unlock the Everything Playlist"); }

        public void Begin(PlayerInteractor who)
        {
            if (Activity.Any || who == null) return;
            walker = who.GetComponent<SimplePlayerController>(); if (walker == null || walker.cameraPivot == null) return;
            hands = who; backPos = walker.transform.position; backRot = walker.transform.rotation; Active = this; playing = false; shrinkT = 0f; outT = -1f;
        }

        void Enter()
        {
            walker.Take(); hands.enabled = false; IslandExplorer.Frozen = true; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = false;
            big.gameObject.SetActive(true); Tune(); playing = true; inRound = false; mine.alive = false; mine.corners.Clear(); robots.Clear(); camSet = false; round = 0; roundStart = -1; overAt = -1; enteredAt = Time.time; startDue = false;
            Say("A and D turn (or the arrows, or the mouse buttons). Don't touch a wall of light.", 6f);
        }

        void Quit()
        {
            if (walker != null) walker.Release(backPos, backRot); if (hands != null) hands.enabled = true;
            IslandExplorer.Frozen = false; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = true; if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null;
            if (big != null) big.gameObject.SetActive(false); playing = false; Send(); Active = null; shrinkT = .55f; outT = -1f;
        }

        void Tune() { var d = MusicDirector.Instance; if (d == null) return; d.Held = Everything ? d.Everything : d.Catalog != null ? d.Catalog.Get(landScene) : null; }
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }

        // (if anything goes wrong in here, you are put back by the bike rather than left stranded)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }
        void LateUpdate() { try { Follow(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            shrinkT += Time.deltaTime;
            if (Active != this) return;
            if (!playing) { if (shrinkT > .55f) Enter(); else return; }
            if (outT >= 0f) { outT += Time.deltaTime; if (outT > .5f) Quit(); return; }
            var dt = Mathf.Min(Time.deltaTime, .05f); var kb = Keyboard.current; var mouse = Mouse.current; var now = SharedChance.Now;
            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;

            // whoever has the lowest number calls the rounds: the first as soon as they have listened for anyone already
            // riding, the next three seconds after the last was over
            var referee = Referee();
            if (referee)
            {
                var sinceStart = roundStart < 0 ? -1 : now - roundStart;
                var due = roundStart < 0 ? Time.time - enteredAt > 1.2f : overAt >= 0 ? now > overAt + After : sinceStart > CountIn + MaxRide + After;
                if (due) { round++; roundStart = now; startDue = true; }
            }
            if (startDue) { startDue = false; StartRound(now); nextSend = 0f; }
            var into = roundStart < 0 ? -1 : now - roundStart; var riding = roundStart >= 0 && into >= CountIn && into < CountIn + MaxRide && overAt < 0;
            all.Clear(); if (inRound) { all.Add(mine); all.AddRange(robots); } foreach (var o in others.Values) if (Time.time - o.seen < 3f && o.playing && o.round == round && o.inRound) all.Add(o.bike);

            if (canAct)
            {
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing " + landName + "'s songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f) { leaveHeld = 0f; outT = 0f; return; }
            }
            if (inRound && riding)
            {
                if (canAct && mine.alive)
                {
                    var turn = 0; if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame) turn = -1; else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame) turn = 1;
                    if (turn != 0) { mine.Turn(turn); nextSend = 0f; }
                }
                foreach (var b in robots) BikeSim.Robot(b, all, rng, dt);
                if (BikeSim.Advance(mine, all, dt)) { Say("You hit a wall", 3f); nextSend = 0f; }
                foreach (var b in robots) BikeSim.Advance(b, all, dt);
                var othersUp = 0; var othersIn = 0; foreach (var b in all) if (b != mine) { othersIn++; if (b.alive) othersUp++; }
                if (mine.alive && othersIn > 0 && othersUp == 0 && !wonThis) Won();
            }
            // the round is over as soon as one bike is left, or no player is, or someone stepped on and is waiting
            if (riding && referee)
            {
                var up = 0; foreach (var b in all) if (b.alive) up++;
                var players = inRound && mine.alive ? 1 : 0; var waiting = inRound ? 0 : 1;
                foreach (var o in others.Values) { if (Time.time - o.seen >= 3f || !o.playing) continue; if (o.round == round && o.inRound) { if (o.bike.alive) players++; } else waiting++; }
                if (players == 0 || (all.Count >= 2 && up <= 1) || (waiting > 0 && into > CountIn + 1.5)) { overAt = now; nextSend = 0f; if (waiting > 0 && players > 0 && up > 1) Say("A friend stepped on: a new round for everyone", 3f); }
            }
            else if (inRound && roundStart >= 0 && into >= CountIn + MaxRide && overAt < 0 && mine.alive && !wonThis) { Say("Time: still riding", 3f); wonThis = true; }
            if (Time.time >= nextSend) { nextSend = Time.time + .1f; Send(); }
            Draw();
        }

        bool Referee() { var me = Me; foreach (var kv in others) if (Time.time - kv.Value.seen < 3f && kv.Value.playing && kv.Key < me) return false; return true; }

        /// Lines up for the round that has just been called (or watches it, if its count-in is already over).
        void StartRound(double now)
        {
            wonThis = false; overAt = -1; camSet = false; robots.Clear(); rng = new System.Random((int)(round % 100000));
            var ids = new List<ulong> { Me }; foreach (var kv in others) if (Time.time - kv.Value.seen < 3f && kv.Value.playing) ids.Add(kv.Key); ids.Sort();
            if (now - roundStart > CountIn - .5) { inRound = false; mine.alive = false; mine.corners.Clear(); Say("A round is on: you ride in the next one", 4f); return; }
            inRound = true; BikeSim.Spawn(mine, ids.IndexOf(Me) * (ids.Count <= 4 ? 2 : 1));
            if (ids.Count == 1) for (var i = 1; i < 4; i++) { var b = new Bike(); BikeSim.Spawn(b, i * 2); robots.Add(b); }   // alone: three that ride themselves
        }

        void Won()
        {
            wonThis = true; Wins++;
            if (Wins >= Bar && !Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say("Last one riding!\nThe Everything Playlist is unlocked here. P switches it.", 9f); }
            else Say(Unlocked ? "Last one riding!" : $"Last one riding! That's {Wins} of {Bar}", 5f);
            PlayerPrefs.Save(); ShowRecord();
        }

        // ---------------------------------------------------------------- drawing
        /// A colour at full strength: bloom turns a pale or over-bright wall white, so each is pushed to its purest and kept just bright enough to glow.
        static Color Rich(Color c) { Color.RGBToHSV(c, out var h, out var sa, out var v); return Color.HSVToRGB(h, Mathf.Max(sa, .92f), 1f); }
        Material Glow(Color c) { if (!glows.TryGetValue(c, out var m)) glows[c] = m = new Material(WorldMaterials.Glow(Rich(c), 1.05f)) { name = "Light wall" }; return m; }
        Transform Box(List<Transform> pool, ref int used)
        {
            if (used == pool.Count) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.name = "Light"; g.transform.SetParent(big, false); g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; pool.Add(g.transform); }
            var t = pool[used++]; t.gameObject.SetActive(true); return t;
        }

        void Draw()
        {
            wallsUsed = bodiesUsed = 0;
            void One(Bike b, Color c)
            {
                if (b.corners.Count == 0) return; var m = Glow(c);
                for (var k = 0; k < b.corners.Count; k++)
                {
                    var p = b.corners[k]; var q = k + 1 < b.corners.Count ? b.corners[k + 1] : b.at; var len = (q - p).magnitude; if (len < .05f) continue;
                    var t = Box(walls, ref wallsUsed); var mid = (p + q) / 2f; t.localPosition = new Vector3(mid.x, .8f, mid.y); t.localRotation = Quaternion.identity;
                    t.localScale = Mathf.Abs(q.x - p.x) > Mathf.Abs(q.y - p.y) ? new Vector3(len + .2f, 1.6f, .2f) : new Vector3(.2f, 1.6f, len + .2f); t.GetComponent<MeshRenderer>().sharedMaterial = m;
                }
                if (!b.alive) return; var body = Box(bodies, ref bodiesUsed); body.localPosition = new Vector3(b.at.x, .55f, b.at.y) + new Vector3(b.Heading.x, 0, b.Heading.y) * .6f; body.localRotation = Quaternion.Euler(0, b.dir * 90f, 0); body.localScale = new Vector3(.7f, .9f, 2.2f);
                body.GetComponent<MeshRenderer>().sharedMaterial = Glow(Color.Lerp(c, Color.white, .2f));
            }
            if (inRound) { One(mine, PlayerOrb.Local != null ? PlayerOrb.Local.Color : new Color(.3f, .9f, 1f)); for (var i = 0; i < robots.Count; i++) One(robots[i], RobotColours[i % RobotColours.Length]); }
            foreach (var o in others.Values) if (Time.time - o.seen < 3f && o.playing && o.round == round) One(o.bike, o.color);
            for (var i = wallsUsed; i < walls.Count; i++) walls[i].gameObject.SetActive(false); for (var i = bodiesUsed; i < bodies.Count; i++) bodies[i].gameObject.SetActive(false);
        }

        void Follow()
        {
            if (Active != this || walker == null || !playing) return;
            Vector3 at, look;
            if (inRound && mine.alive)
            {
                // just behind and above the bike, turning with it
                camYaw = Mathf.LerpAngle(camYaw, mine.dir * 90f, 1f - Mathf.Exp(-7f * Time.deltaTime)); var back = Quaternion.Euler(0, camYaw, 0) * Vector3.back;
                var p = big.TransformPoint(new Vector3(mine.at.x, 0, mine.at.y)); at = p + back * 11f + Vector3.up * 7f; look = p - back * 9f + Vector3.up * 1f;
            }
            else { at = big.TransformPoint(new Vector3(0, 120f, -BikeSim.Half - 40f)); look = big.position; }
            if (!camSet) { camAt = at; camSet = true; camYaw = mine.dir * 90f; } camAt = Vector3.Lerp(camAt, at, 1f - Mathf.Exp(-9f * Time.deltaTime));
            var dir = (look - camAt).normalized; var yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; var pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg; var body = Quaternion.Euler(0, yaw, 0); var pivot = walker.cameraPivot;
            walker.transform.SetPositionAndRotation(camAt - body * pivot.localPosition, body); pivot.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        // ---------------------------------------------------------------- friends
        void Send()
        {
            var f = Friends.Instance; if (f == null || !f.Online) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((byte)2); w.Write(id); w.Write(Active == this && playing); w.Write(round); w.Write(roundStart); w.Write(overAt); w.Write(inRound); w.Write(mine.alive); w.Write((byte)mine.dir); w.Write(mine.at.x); w.Write(mine.at.y);
                var n = Mathf.Min(mine.corners.Count, 200); w.Write((byte)n); for (var i = mine.corners.Count - n; i < mine.corners.Count; i++) { w.Write(mine.corners[i].x); w.Write(mine.corners[i].y); }
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
                    if (r.ReadByte() != 2 || r.ReadString() != id) return; if (!others.TryGetValue(from, out var o)) others[from] = o = new Remote();
                    o.playing = r.ReadBoolean(); o.round = r.ReadInt64(); o.start = r.ReadDouble(); o.over = r.ReadDouble(); o.inRound = r.ReadBoolean(); o.bike.alive = r.ReadBoolean(); o.bike.dir = r.ReadByte() % 4; o.bike.at = new Vector2(r.ReadSingle(), r.ReadSingle());
                    var n = r.ReadByte(); o.bike.corners.Clear(); for (var i = 0; i < n; i++) o.bike.corners.Add(new Vector2(r.ReadSingle(), r.ReadSingle()));
                    o.color = PlayerOrb.Palette[Mathf.Clamp(r.ReadByte(), 0, PlayerOrb.Palette.Length - 1)].color; o.name = r.ReadString(); o.seen = o.playing ? Time.time : -99f;
                    // follow whoever is ahead (or, in the same round called twice, whoever has the lower number), and hear that a round is over
                    if (Active == this && playing && o.playing && o.start >= 0)
                    {
                        if (o.round > round || (o.round == round && from < Me && System.Math.Abs(o.start - roundStart) > .01)) { round = o.round; roundStart = o.start; overAt = o.over; startDue = true; }
                        else if (o.round == round && o.over >= 0 && overAt < 0) overAt = o.over;
                    }
                }
            }
            catch (System.Exception) { }
        }

        /// For the photo tour only: steps onto the grid and rides a few seconds of a round with the three robots.
        public System.Collections.IEnumerator Demo(PlayerInteractor who)
        {
            Begin(who); var until = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < until) yield return null;
            if (Active != this || !playing) { Debug.LogWarning("Bikes demo: the game did not start"); yield break; }
            // (not waiting for the clock: start a round now)
            inRound = true; BikeSim.Spawn(mine, 0); robots.Clear(); for (var i = 1; i < 4; i++) { var b = new Bike(); BikeSim.Spawn(b, i * 2); robots.Add(b); }
            var t = 0f; while (t < 2.5f) { all.Clear(); all.Add(mine); all.AddRange(robots); foreach (var b in robots) BikeSim.Robot(b, all, rng, Time.deltaTime); if (t > 1.2f && mine.corners.Count < 2) mine.Turn(1); foreach (var b in all) BikeSim.Advance(b, all, Mathf.Min(Time.deltaTime, .05f)); Draw(); t += Time.deltaTime; yield return null; }
            var alive = 0; foreach (var b in all) if (b.alive) alive++; Debug.Log($"Bikes demo: {alive} of {all.Count} still riding after {t:0.0} s; mine at {mine.at}, turned {mine.corners.Count - 1} times; walls drawn {wallsUsed}");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        // ---------------------------------------------------------------- what you see on the screen
        GUIStyle box, big2, small2; static Texture2D white;

        void OnGUI()
        {
            white ??= Texture2D.whiteTexture;
            var wash = outT >= 0f ? outT / .5f : shrinkT < 1.1f ? 1f - Mathf.Abs(shrinkT - .55f) / .55f : 0f;
            if (wash > 0f && Event.current.type == EventType.Repaint) { var keep = GUI.color; GUI.color = new Color(1f, .7f, .9f, Mathf.Clamp01(wash)); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white); GUI.color = keep; }
            if (Active != this || !playing || MainMenu.Showing) return;
            box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(30, TextAnchor.MiddleCenter, true, true); small2 ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            var now = SharedChance.Now; var into = roundStart < 0 ? -1 : now - roundStart; var up = 0; foreach (var b in all) if (b.alive) up++;
            GUI.Box(new Rect(12, 12, 470, 34), $"  Riding: {up} of {all.Count}  ·  rounds won: {Wins}" + (Unlocked ? "" : $" of {Bar}"), box);
            if (Everything) GUI.Box(new Rect(12, 50, 240, 30), "  Everything Playlist", box);
            if (inRound && into >= 0 && into < CountIn) GUI.Box(new Rect(Screen.width / 2f - 120, Screen.height / 2f - 130, 240, 70), Mathf.CeilToInt((float)(CountIn - into)).ToString(), big2);
            else if (overAt >= 0) GUI.Box(new Rect(Screen.width / 2f - 200, 150, 400, 44), $"Next round in {Mathf.Max(1, Mathf.CeilToInt((float)(overAt + After - now)))}", small2);
            else if (!inRound) GUI.Box(new Rect(Screen.width / 2f - 200, 150, 400, 44), roundStart < 0 ? "Getting ready..." : "Watching: you ride in the next round", small2);
            if (Time.time < bannerUntil && banner.Length > 0) Activity.Banner(banner, 96, big2);
            Activity.Hint(Screen.height - 66, "A and D (or arrows, or the mouse buttons) turn  ·  hold X to leave" + (Unlocked ? "  ·  P playlist" : ""), small2);
        }
    }
}
