using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// The thing you press E on to bowl (the ball return by the model lane).
    public class BowlStart : Interactable
    {
        public BowlGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Bowl";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// Bowling, on a lane you shrink onto (the Glitterlands' activity, 2026-11-17).
    /// - Press E at the ball return: you shrink onto the lane, built out of sight high over the Glitterlands, ten
    ///   times the size, glitter down both sides.
    /// - A and D move where you stand, the mouse aims, hold the button to swing and let go to bowl. While the ball
    ///   rolls, A and D curl it. Ten frames, scored the proper way.
    /// - Everyone bowls at once on their own copy of the lane. The board at the top right is the alley's overhead
    ///   screen: every bowler's card, frame by frame, as it fills in. A strike, a spare, a gutter ball or three strikes
    ///   running plays a little cartoon on the screen, yours and your friends' alike (with their name on it).
    /// - Bowl 180 or better and the lane's radio can play the whole Everything Playlist (P switches).
    public class BowlGame : MonoBehaviour
    {
        public static BowlGame Active { get; private set; }
        public Transform big; public float bigScale = 10f; public int landScene; public string id = "glit"; public string landName = "the Glitterlands"; public TextMesh recordText;
        const string Channel = "bowl"; const int Bar = 180;
        const float SwingTravel = 30f, MinPower = .03f;   // the swing, as in golf: hold click and push the mouse forward for power; let go below MinPower and nothing is bowled
        /// Your arm is never quite still: the line you bowl along sways a little either side of where you aim, quicker
        /// in the later frames, so a strike has to be timed and the same ball can't just be bowled again.
        float Sway => (1.5f + card.At.Item1 * .07f) * Mathf.Sin((Time.time - swayFrom) * (2.1f + card.At.Item1 * .09f));
        float swayFrom;

        readonly BowlSim sim = new BowlSim(); BowlCard card = new BowlCard(); Transform ball, aimBar; Transform[] pins; float[] tip = new float[10];
        bool playing, charging, wasLocked; float power, chargeT, standX, aimDeg, shrinkT = 9f, outT = -1f, leaveHeld, nextSend, acc, settle = -1f; int before; Vector3 camAt; bool camSet; string banner = ""; float bannerUntil;
        SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot;
        class Remote { public string name = "Friend"; public int total, frame; public float seen; public List<int> rolls = new List<int>(); }
        string toon; string toonWho = ""; float toonAt = -99f;   // the cartoon on the alley's screen: what happened, to whom, when
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();

        string Key(string what) => $"EW_Bowl_{id}_{what}";
        public int Best => PlayerPrefs.GetInt(Key("best"), 0);
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        public string StartPrompt => "Go bowling" + (Best > 0 ? $"  ·  your best {Best}" : "");

        void OnEnable() { Friends.Received += OnNet; }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }
        void Start() { ShowRecord(); }
        void ShowRecord() { if (recordText != null) recordText.text = (Best > 0 ? $"Your best game: {Best}" : "No game bowled yet") + "\n" + (Unlocked ? "The Everything Playlist is unlocked here" : $"Bowl {Bar} to unlock the Everything Playlist"); }

        /// The lane, its gutters and its glittering sides, at any size.
        public static void Lane(MeshKit mk, float s)
        {
            var wood = WorldMaterials.Surface(new Color(.86f, .7f, .46f), Surface.Wood); var gutter = WorldMaterials.Solid(new Color(.08f, .06f, .14f), .6f); var deck = WorldMaterials.Surface(new Color(.95f, .93f, .9f), Surface.Painted);
            float W = BowlSim.W * s, L = (BowlSim.L + 1.4f) * s, g = .24f * s, back = 4.6f * s; var up = Vector3.up;
            mk.Box(wood, new Vector3(0, -.05f * s, L / 2f - back / 2f + back * 0f), new Vector3(W, .1f * s, L)); mk.Box(wood, new Vector3(0, -.05f * s, -back / 2f), new Vector3(W + 2 * g + .6f * s, .1f * s, back));   // the lane, and the approach behind the foul line
            mk.Box(WorldMaterials.Glow(new Color(1f, .3f, .4f), 1.2f), new Vector3(0, .002f * s, 0), new Vector3(W, .004f * s, .03f * s));   // the foul line
            for (var k = -2; k <= 2; k++) mk.Tri(WorldMaterials.Solid(new Color(.2f, .14f, .1f)), new Vector3(k * .19f * s, .003f * s, 4.3f * s), new Vector3((k * .19f - .03f) * s, .003f * s, 4.1f * s), new Vector3((k * .19f + .03f) * s, .003f * s, 4.1f * s), Vector3.down * s);   // the arrows
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(gutter, new Vector3(e * (W / 2f + g / 2f), -.09f * s, L / 2f), new Vector3(g, .1f * s, L));
                for (var k = 0; k < 24; k++) { var c = new[] { new Color(1f, .4f, .74f), new Color(.6f, .5f, 1f), new Color(.4f, .86f, 1f), new Color(1f, .84f, .4f) }[k % 4]; mk.Box(WorldMaterials.Glow(c, 1.5f), new Vector3(e * (W / 2f + g + .12f * s), .12f * s, (k + .5f) * L / 24f), new Vector3(.2f * s, .34f * s, L / 24f - .1f * s)); }
            }
            mk.Box(deck, new Vector3(0, .6f * s, L + .15f * s), new Vector3(W + 2 * g + .5f * s, 1.4f * s, .2f * s));   // the wall behind the pins
            mk.Box(gutter, new Vector3(0, -.3f * s, L / 2f - back / 2f), new Vector3(W + 2 * g + .7f * s, .4f * s, L + back));
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
            big.gameObject.SetActive(true); Tune(); playing = true; camSet = false; card = new BowlCard(); sim.Rack(); standX = .1f; aimDeg = 0f; settle = -1f;
            if (ball == null)
            {
                var s = bigScale; var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(b.GetComponent<Collider>()); b.name = "Bowling ball"; b.transform.SetParent(big, false); b.transform.localScale = Vector3.one * BowlSim.BallR * 2f * s; ball = b.transform;
                b.GetComponent<MeshRenderer>().sharedMaterial = new Material(WorldMaterials.Glow(PlayerOrb.Local != null ? PlayerOrb.Local.Color : new Color(.6f, .5f, 1f), .9f)) { name = "Bowling ball" };
                pins = new Transform[10]; var white = WorldMaterials.Surface(new Color(.97f, .96f, .93f), Surface.Painted); var red = WorldMaterials.Glow(new Color(1f, .25f, .35f), 1f);
                for (var i = 0; i < 10; i++)
                {
                    var root = new GameObject("Pin").transform; root.SetParent(big, false);
                    var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(body.GetComponent<Collider>()); body.transform.SetParent(root, false); body.transform.localPosition = new Vector3(0, .19f * s, 0); body.transform.localScale = new Vector3(BowlSim.PinR * 2.2f * s, .19f * s, BowlSim.PinR * 2.2f * s); body.GetComponent<MeshRenderer>().sharedMaterial = white;
                    var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(band.GetComponent<Collider>()); band.transform.SetParent(root, false); band.transform.localPosition = new Vector3(0, .27f * s, 0); band.transform.localScale = new Vector3(BowlSim.PinR * 1.75f * s, .012f * s, BowlSim.PinR * 1.75f * s); band.GetComponent<MeshRenderer>().sharedMaterial = red;
                    pins[i] = root;
                }
                var a = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(a.GetComponent<Collider>()); a.name = "Aim"; a.transform.SetParent(big, false); a.GetComponent<MeshRenderer>().sharedMaterial = WorldMaterials.Glow(Color.white, 2f); aimBar = a.transform;
            }
            Say("A and D move you, the mouse aims. Your line sways: hold click, push the mouse forward for power, and let go as it crosses your mark.\nWhile it rolls, A and D curl it.", 10f);
        }

        void Quit()
        {
            if (walker != null) walker.Release(backPos, backRot); if (hands != null) hands.enabled = true;
            IslandExplorer.Frozen = false; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = true; if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null;
            if (big != null) big.gameObject.SetActive(false); playing = false; Send(); Active = null; shrinkT = .55f; outT = -1f;
        }

        void Tune() { var d = MusicDirector.Instance; if (d == null) return; d.Held = Everything ? d.Everything : d.Catalog != null ? d.Catalog.Get(landScene) : null; }
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }

        // (if anything goes wrong in here, you are put back by the ball return rather than left stranded)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }
        void LateUpdate() { try { Follow(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            shrinkT += Time.deltaTime;
            if (Active != this) return;
            if (!playing) { if (shrinkT > .55f) Enter(); else return; }
            if (outT >= 0f) { outT += Time.deltaTime; if (outT > .5f) Quit(); return; }
            var dt = Mathf.Min(Time.deltaTime, .1f); var kb = Keyboard.current; var mouse = Mouse.current; var hook = 0f;
            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;
            if (canAct)
            {
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing " + landName + "'s songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f) { leaveHeld = 0f; outT = 0f; return; }
                var side = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                if (sim.rolling) hook = side;
                else if (settle < 0f && !card.Done)
                {
                    standX = Mathf.Clamp(standX + side * .35f * dt, -BowlSim.W / 2 + BowlSim.BallR, BowlSim.W / 2 - BowlSim.BallR); aimDeg = Mathf.Clamp(aimDeg + mouse.delta.ReadValue().x * walker.mouseSensitivity * .06f, -5f, 5f);
                    if (!charging && mouse.leftButton.wasPressedThisFrame) { charging = true; power = 0f; }
                    if (charging) { power = Mathf.Clamp01(power + mouse.delta.ReadValue().y * walker.mouseSensitivity / SwingTravel); if (mouse.rightButton.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) charging = false; }
                    if (charging && !mouse.leftButton.isPressed && power < MinPower) charging = false;   // let go with nothing wound up: nothing bowled
                    if (charging && !mouse.leftButton.isPressed)
                    {
                        // a harder ball is a touch wilder
                        charging = false; before = sim.Standing(); sim.Bowl(standX, aimDeg + Sway + Random.Range(-.3f, .3f) * (1f + power), Mathf.Lerp(4.5f, 9.5f, power)); acc = 0f;
                    }
                }
                if (card.Done && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) { card = new BowlCard(); sim.Rack(); settle = -1f; }
            }
            if (sim.rolling)
            {
                acc += dt; var n = Mathf.Min(60, (int)(acc / BowlSim.Step)); acc -= n * BowlSim.Step; for (var k = 0; k < n && sim.rolling; k++) sim.Advance(hook);
                if (!sim.rolling) settle = 0f;
            }
            else if (settle >= 0f)
            {
                settle += dt;
                if (settle > 1.4f)
                {
                    settle = -1f; var got = before - sim.Standing(); var (f, b) = card.At; card.rolls.Add(got);
                    var what = What(card.rolls, got == 10 && b == 0, sim.Standing() == 0, sim.gutter && got == 0);
                    if (what != null) Toon(what, ""); else Say(got.ToString(), 2f);
                    nextSend = 0f;
                    if (card.Done) Over(); else if (card.FreshRack) sim.Rack(); else sim.Sweep();
                    standX = 0f; aimDeg = 0f; swayFrom = Time.time - Random.Range(0f, 3f);   // back to the middle of the approach for every ball
                }
            }
            if (Time.time >= nextSend) { nextSend = Time.time + .5f; Send(); }
        }

        void Over()
        {
            var total = card.Total; if (total > Best) PlayerPrefs.SetInt(Key("best"), total);
            if (total >= Bar && !Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say($"{total}!\nThe Everything Playlist is unlocked here. P switches it.", 9f); } else Say($"Game: {total}", 6f);
            PlayerPrefs.Save(); ShowRecord();
        }

        void Follow()
        {
            if (Active != this || walker == null || !playing) return;
            var s = bigScale; var bp = sim.rolling || settle >= 0f ? new Vector3(sim.ball.x, BowlSim.BallR, sim.ball.y) : new Vector3(standX, BowlSim.BallR, -.15f); ball.localPosition = bp * s; ball.gameObject.SetActive(!(settle >= 0f && sim.ball.y > BowlSim.L + 1.5f));
            for (var i = 0; i < 10; i++)
            {
                var on = sim.state[i] != 2; pins[i].gameObject.SetActive(on); if (!on) { tip[i] = 0f; continue; }
                pins[i].localPosition = new Vector3(sim.pin[i].x, 0, sim.pin[i].y) * s; tip[i] = Mathf.MoveTowards(tip[i], sim.state[i] == 1 ? 88f : 0f, 420f * Time.deltaTime);
                var d = sim.pv[i].sqrMagnitude > .01f ? Mathf.Atan2(sim.pv[i].x, sim.pv[i].y) * Mathf.Rad2Deg : 0f; pins[i].localRotation = Quaternion.Euler(0, d, 0) * Quaternion.Euler(tip[i], 0, 0);
            }
            var aiming = !sim.rolling && settle < 0f && !card.Done; aimBar.gameObject.SetActive(aiming);
            if (aiming) { var rot = Quaternion.Euler(0, aimDeg + Sway, 0); var len = (charging ? 2f + power * 5f : 4f) * s; aimBar.localRotation = rot; aimBar.localPosition = new Vector3(standX, .01f, 0) * s + rot * Vector3.forward * (len / 2f + .2f * s); aimBar.localScale = new Vector3(.012f * s, .006f * s, len); }
            // behind the ball as you aim; following it down, then held looking at the pins
            var z = sim.rolling || settle >= 0f ? Mathf.Min(sim.ball.y, BowlSim.L - 3.2f) : 0f; var x = aiming ? standX : sim.ball.x * .4f;
            var at = big.TransformPoint(new Vector3(x, .75f, z - 1.9f) * s); var look = big.TransformPoint(new Vector3(x * .5f, .1f, z + 3.2f) * s);
            if (!camSet) { camAt = at; camSet = true; } camAt = Vector3.Lerp(camAt, at, 1f - Mathf.Exp(-6f * Time.deltaTime));
            var dir = (look - camAt).normalized; var yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; var pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg; var body = Quaternion.Euler(0, yaw, 0); var pivot = walker.cameraPivot;
            walker.transform.SetPositionAndRotation(camAt - body * pivot.localPosition, body); pivot.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        // ---------------------------------------------------------------- the alley's screen
        /// What a roll was, for the cartoon: three strikes running is a turkey.
        static string What(List<int> rolls, bool strike, bool spare, bool gutter)
        {
            var n = rolls.Count; if (strike) return n >= 3 && rolls[n - 2] == 10 && rolls[n - 3] == 10 ? "turkey" : "strike";
            return spare ? "spare" : gutter ? "gutter" : null;
        }
        void Toon(string what, string who) { toon = what; toonWho = who; toonAt = Time.time; if (who.Length == 0 && what != "gutter") Activity.Cheer(); }

        /// A card written the way an alley's screen shows it: X for a strike, / for a spare, - for a miss.
        static string Marks(List<int> rolls)
        {
            var s = new System.Text.StringBuilder(); int f = 0, i = 0;
            while (i < rolls.Count && f < 10)
            {
                if (f == 9) { for (var k = i; k < rolls.Count; k++) { var r = rolls[k]; var sp = k > i && rolls[k - 1] != 10 && rolls[k - 1] + r == 10 && (k - i == 1 || rolls[i] == 10); s.Append(r == 10 ? "X" : sp ? "/" : r == 0 ? "-" : r.ToString()); } break; }
                if (rolls[i] == 10) { s.Append("X "); i++; }
                else { s.Append(rolls[i] == 0 ? "-" : rolls[i].ToString()); if (i + 1 < rolls.Count) s.Append(rolls[i] + rolls[i + 1] == 10 ? "/" : rolls[i + 1] == 0 ? "-" : rolls[i + 1].ToString()); s.Append(' '); i += 2; }
                f++;
            }
            return s.ToString();
        }

        void Fill(Rect r, Color c) { var keep = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = keep; }

        /// The cartoon: two and a half seconds on a dark panel under the banner, drawn from plain blocks.
        void DrawToon()
        {
            var t = Time.time - toonAt; if (toon == null || t > 2.6f || Event.current.type != EventType.Repaint) return;
            const float w = 460f, h = 170f; var r = new Rect(Screen.width / 2f - w / 2f, 150f, w, h); var c = new Vector2(r.center.x, r.y + h * .62f);
            Fill(r, new Color(.04f, .03f, .1f, .92f)); Fill(new Rect(r.x, r.y, w, 4), new Color(1f, .4f, .8f)); Fill(new Rect(r.x, r.yMax - 4, w, 4), new Color(.3f, .9f, 1f));
            var flash = Color.HSVToRGB(Mathf.Repeat(t * 1.6f, 1f), .7f, 1f); var word = ""; var pin = new Color(.97f, .96f, .92f);
            switch (toon)
            {
                case "strike": case "turkey":
                    // the ten pins leap apart
                    for (var i = 0; i < 10; i++)
                    {
                        var row = i < 1 ? 0 : i < 3 ? 1 : i < 6 ? 2 : 3; var col = i - (row == 0 ? 0 : row == 1 ? 1 : row == 2 ? 3 : 6); var home = new Vector2((col - row / 2f) * 26f, -row * 14f);
                        var k = Mathf.Clamp01((t - .15f) / 1.2f); var fly = home.normalized * (i == 0 ? 0f : 1f) + new Vector2(Mathf.Sin(i * 2.4f), -1f).normalized * .9f; var p = c + home + fly * (k * 150f) + Vector2.up * (k * k * 120f);
                        Fill(new Rect(p.x - 5, p.y - 16, 10, 22), pin); Fill(new Rect(p.x - 5, p.y - 10, 10, 3), new Color(.9f, .2f, .25f));
                    }
                    if (t < .3f) Fill(new Rect(c.x - 12 - (1f - t / .3f) * 200f, c.y + 2, 24, 24), new Color(.3f, .6f, 1f));
                    word = toon == "turkey" ? "TURKEY!" : "STRIKE!";
                    if (toon == "turkey") for (var i = 0; i < 3; i++) Fill(new Rect(r.x + 24 + i * 30, r.y + 40 + Mathf.Sin(t * 9f + i) * 5f, 22, 22), flash);
                    break;
                case "spare":
                    // the sweeper comes down and clears what is left
                    for (var i = 0; i < 3; i++) { var x = c.x - 40 + i * 40; var gone = t > .5f + i * .15f; if (!gone) Fill(new Rect(x - 5, c.y - 16, 10, 22), pin); }
                    var sx = Mathf.Lerp(r.x + 20, r.xMax - 20, Mathf.Clamp01((t - .3f) / 1f)); Fill(new Rect(sx - 4, c.y - 30, 8, 44), new Color(.6f, .6f, .7f)); Fill(new Rect(sx - 22, c.y + 8, 44, 6), new Color(.6f, .6f, .7f));
                    word = "SPARE!"; break;
                case "gutter":
                    // the ball trundles sadly down the gutter
                    Fill(new Rect(r.x + 20, c.y + 18, w - 40, 5), new Color(.25f, .25f, .35f)); var bx = Mathf.Lerp(r.x + 30, r.xMax - 40, Mathf.Clamp01(t / 2f)); Fill(new Rect(bx, c.y - 2 + Mathf.Abs(Mathf.Sin(t * 5f)) * -3f, 20, 20), new Color(.3f, .6f, 1f));
                    for (var i = 0; i < 10; i++) Fill(new Rect(r.xMax - 150 + (i % 4) * 22, c.y - 34 - (i / 4) * 0, 8, 18), pin);
                    word = "GUTTER..."; flash = new Color(.6f, .7f, .9f); break;
            }
            var keepC = GUI.color; GUI.color = flash; var bounce = toon == "gutter" ? 0f : Mathf.Abs(Mathf.Sin(t * 7f)) * 8f;
            GUI.Label(new Rect(r.x, r.y + 14 - bounce, w, 44), word, big2); GUI.color = keepC;
            if (toonWho.Length > 0) GUI.Label(new Rect(r.x, r.yMax - 30, w, 24), toonWho, small2);
        }

        /// The overhead screen: every bowler's card, top right.
        void DrawBoard()
        {
            var rows = new List<(string name, string marks, int total)> { (Friends.PlayerName, Marks(card.rolls), card.Total) };
            foreach (var o in others.Values) if (Time.time - o.seen < 4f) rows.Add((o.name, Marks(o.rolls), o.total));
            if (rows.Count < 2) return;   // alone, the card on the left says it all
            const float w = 470f; var x = Screen.width - w - 12f; var y = 12f;
            GUI.Box(new Rect(x, y, w, 26), "  TONIGHT'S BOWLERS", box); y += 28f;
            rows.Sort((a, b) => b.total.CompareTo(a.total));
            foreach (var (name, marks, total) in rows) { GUI.Box(new Rect(x, y, w, 28), $"  {(name.Length > 10 ? name.Substring(0, 10) : name),-10}  {marks}", box); GUI.Box(new Rect(x + w - 62, y, 62, 28), total.ToString(), small2); y += 30f; }
        }

        // ---------------------------------------------------------------- friends
        void Send()
        {
            var f = Friends.Instance; if (f == null || !f.Online) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms)) { w.Write((byte)2); w.Write(id); w.Write(Active == this && playing); w.Write(card.Total); w.Write((byte)card.At.frame); w.Write(Friends.PlayerName); w.Write((byte)card.rolls.Count); foreach (var roll in card.rolls) w.Write((byte)roll); w.Flush(); f.Broadcast(Channel, ms.ToArray(), false); }
        }

        void OnNet(ulong from, string channel, byte[] data)
        {
            if (channel != Channel) return;
            try
            {
                using (var r = new BinaryReader(new MemoryStream(data)))
                {
                    if (r.ReadByte() != 2 || r.ReadString() != id) return; if (!others.TryGetValue(from, out var o)) others[from] = o = new Remote();
                    var on = r.ReadBoolean(); o.total = r.ReadInt32(); o.frame = r.ReadByte(); o.name = r.ReadString(); o.seen = on ? Time.time : -99f;
                    var n = r.ReadByte(); var had = o.rolls.Count; var fresh = new List<int>(); for (var i = 0; i < n; i++) fresh.Add(r.ReadByte());
                    // a friend's new roll: if it was a strike, a spare or a gutter ball, their cartoon plays on my screen too
                    if (Active == this && playing && on && n == had + 1 && Time.time - toonAt > 2.6f)
                    {
                        var roll = fresh[n - 1]; var prev = n >= 2 ? fresh[n - 2] : -1; var probe = new BowlCard(); probe.rolls.AddRange(fresh.GetRange(0, n - 1)); var ball = probe.At.ball;
                        var what = What(fresh, roll == 10 && ball == 0, ball > 0 && prev >= 0 && prev != 10 && prev + roll == 10, false);
                        if (what != null) Toon(what, o.name);
                    }
                    o.rolls = fresh;
                }
            }
            catch (System.Exception) { }
        }

        /// For the photo tour only: steps onto the lane and bowls one ball at the pocket.
        public System.Collections.IEnumerator Demo(PlayerInteractor who)
        {
            Begin(who); var until = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < until) yield return null;
            if (Active != this || !playing) { Debug.LogWarning("Bowl demo: the game did not start"); yield break; }
            before = sim.Standing(); sim.Bowl(.1f, 0f, 7.5f); acc = 0f; var t = 0f; while (t < 5f) { t += Time.deltaTime; yield return null; }
            Debug.Log($"Bowl demo: {10 - sim.Standing()} pins down, ball at {sim.ball}, rolling {sim.rolling}, card {card.Total}");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        // ---------------------------------------------------------------- what you see on the screen
        GUIStyle box, big2, small2; static Texture2D white;

        void OnGUI()
        {
            white ??= Texture2D.whiteTexture; Activity.DrawCheer();
            var wash = outT >= 0f ? outT / .5f : shrinkT < 1.1f ? 1f - Mathf.Abs(shrinkT - .55f) / .55f : 0f;
            if (wash > 0f && Event.current.type == EventType.Repaint) { var keep = GUI.color; GUI.color = new Color(1f, .8f, .95f, Mathf.Clamp01(wash)); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white); GUI.color = keep; }
            if (Active != this || !playing || MainMenu.Showing) return;
            box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(26, TextAnchor.MiddleCenter, true, true); small2 ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            var (f, b) = card.At; var rolls = ""; foreach (var r in card.rolls) rolls += (r == 10 ? "X" : r.ToString()) + " ";
            GUI.Box(new Rect(12, 12, 470, 34), card.Done ? $"  Game over  ·  {card.Total}" : $"  Frame {f + 1} of 10, ball {b + 1}  ·  score {card.Total}" + (Best > 0 ? $"  ·  your best {Best}" : ""), box);
            GUI.Box(new Rect(12, 50, 470, 30), "  " + rolls, box); var y = 84f; if (Everything) { GUI.Box(new Rect(12, y, 240, 30), "  Everything Playlist", box); y += 34f; }
            foreach (var o in others.Values) if (Time.time - o.seen < 4f) { GUI.Box(new Rect(12, y, 330, 28), $"  {o.name}: {o.total}  (frame {o.frame + 1})", box); y += 30f; }
            if (Time.time < bannerUntil && banner.Length > 0) Activity.Banner(banner, 96, big2);
            DrawToon(); DrawBoard();
            if (charging) { var w = 360f; var r = new Rect(Screen.width / 2f - w / 2f, Screen.height - 96, w, 22); GUI.Box(r, "", small2); var keep = GUI.color; GUI.color = Color.Lerp(new Color(.4f, .9f, 1f), new Color(1f, .75f, .2f), power); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, (r.width - 6) * power, r.height - 6), white); GUI.color = keep; }
            Activity.Hint(Screen.height - 66, card.Done ? "Enter  New game   ·   hold X  Leave the lane" : sim.rolling ? "A and D curl the ball" : (charging ? (power < MinPower ? "Push the mouse forward for power  ·  let go now to cancel" : "Let go as the line crosses your mark  ·  pull back to ease off") : "A and D move  ·  mouse aims  ·  hold click and push forward to swing  ·  hold X to leave") + (Unlocked ? "  ·  P playlist" : ""), small2);
        }
    }
}
