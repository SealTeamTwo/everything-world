using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// Whether the player is inside any activity (so the world's own keys and friend markers stand down).
    public static class Activity { public static bool Any => GolfGame.Playing || PoolGame.Active != null || HoopsGame.Active != null; }

    /// The thing you press E on to play pool (the cue rack by the table).
    public class PoolStart : Interactable
    {
        public PoolGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Play";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// Draws a pool table of any size: 'bed' is the middle of the cloth's top; everything is multiplied by 's'.
    public static class PoolBuild
    {
        public static readonly Color[] BallColours =
        {
            new Color(.97f, .96f, .92f), new Color(1f, .82f, .1f), new Color(.15f, .35f, .95f), new Color(.92f, .15f, .12f), new Color(.5f, .2f, .75f), new Color(1f, .5f, .1f), new Color(.1f, .6f, .3f), new Color(.5f, .12f, .14f),
            new Color(.04f, .04f, .05f),
        };

        public static void Table(MeshKit mk, float s, bool legs)
        {
            var cloth = WorldMaterials.Surface(new Color(.1f, .32f, .62f), Surface.Cloth); var cushion = WorldMaterials.Surface(new Color(.07f, .24f, .5f), Surface.Cloth);
            var wood = WorldMaterials.Surface(new Color(.3f, .17f, .1f), Surface.Wood); var hole = WorldMaterials.Solid(new Color(.01f, .01f, .015f)); var brass = WorldMaterials.Surface(new Color(.9f, .72f, .3f), Surface.Metal);
            var dot = WorldMaterials.Solid(new Color(.85f, .9f, 1f)); var up = Vector3.up;
            float W = PoolSim.W * s, L = PoolSim.L * s, c = .05f * s, rail = .13f * s, h = .042f * s;
            mk.Box(cloth, -up * (.02f * s), new Vector3(W + 2 * c, .04f * s, L + 2 * c));
            // cushions between the pockets, and the wooden rail outside them
            var gapC = PoolSim.CornerPocket * s * 1.15f; var gapS = PoolSim.SidePocket * s * 1.15f;
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(cushion, new Vector3(0, h / 2f, e * (L / 2f + c / 2f)), new Vector3(W - 2 * gapC, h, c));
                foreach (var f in new[] { -1f, 1f }) { var len = L / 2f - gapC - gapS; mk.Box(cushion, new Vector3(e * (W / 2f + c / 2f), h / 2f, f * (gapS + len / 2f)), new Vector3(c, h, len)); }
                mk.Box(wood, new Vector3(0, h / 2f - .01f * s, e * (L / 2f + c + rail / 2f)), new Vector3(W + 2 * c + 2 * rail, h + .02f * s, rail));
                mk.Box(wood, new Vector3(e * (W / 2f + c + rail / 2f), h / 2f - .01f * s, 0), new Vector3(rail, h + .02f * s, L + 2 * c));
                for (var k = 0; k < 7; k++) if (k != 3) mk.Prism(dot, new Vector3(e * (W / 2f + c + rail / 2f), h + .0105f * s, (k - 3) * L / 8f), up, .009f * s, .002f * s, 6);
            }
            for (var k = 0; k < PoolSim.Pockets.Length; k++)
            {
                var p = PoolSim.Pockets[k] * s; var r = (k == 2 || k == 3 ? PoolSim.SidePocket : PoolSim.CornerPocket) * s;
                mk.Prism(hole, new Vector3(p.x, .0015f * s, p.y), up, r, .002f * s, 14); mk.Prism(brass, new Vector3(p.x, .0005f * s, p.y), up, r * 1.12f, .002f * s, 14);
            }
            mk.Prism(dot, new Vector3(0, .0012f * s, PoolSim.FootSpot.y * s), up, .008f * s, .001f * s, 8); mk.Prism(dot, new Vector3(0, .0012f * s, PoolSim.HeadSpot.y * s), up, .008f * s, .001f * s, 8);
            // the body under the cloth, and legs
            mk.Box(wood, -up * (.13f * s), new Vector3(W + 2 * c + 2 * rail, .2f * s, L + 2 * c + 2 * rail));
            if (legs) foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(wood, new Vector3(x * (W / 2f - .05f * s), -.5f * s, z * (L / 2f - .1f * s)), new Vector3(.13f * s, .6f * s, .13f * s));
        }

        /// The sixteen balls as spheres under 'parent' (the cue ball first): solids are a plain colour, stripes white with a band of colour.
        public static Transform[] MakeBalls(Transform parent, float s)
        {
            var balls = new Transform[PoolSim.Balls];
            for (var i = 0; i < PoolSim.Balls; i++)
            {
                var stripe = i >= 9; var col = BallColours[stripe ? i - 8 : i];
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(b.GetComponent<Collider>()); b.name = "Pool ball " + i; b.transform.SetParent(parent, false); b.transform.localScale = Vector3.one * PoolSim.R * 2f * s;
                var body = stripe || i == 0 ? BallColours[0] : col; var m = new Material(WorldMaterials.Glow(body, i == 8 ? 0f : .28f)) { name = "Pool ball " + i }; m.SetFloat("_Smoothness", .85f); b.GetComponent<MeshRenderer>().sharedMaterial = m;
                if (stripe)
                {
                    var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(band.GetComponent<Collider>()); band.name = "Stripe"; band.transform.SetParent(b.transform, false);
                    band.transform.localScale = new Vector3(1.03f, .24f, 1.03f); band.transform.localRotation = Quaternion.Euler(64f + i * 9f, i * 47f, 0);
                    var bm = new Material(WorldMaterials.Glow(col, .3f)) { name = "Pool stripe " + i }; band.GetComponent<MeshRenderer>().sharedMaterial = bm;
                }
                balls[i] = b.transform;
            }
            return balls;
        }
    }

    /// Pool, played on a table you shrink onto (the Old Jazz Quarter's activity, 2026-11-17).
    /// - Press E at the cue rack: you shrink onto the table. It is the same table built very large out of sight, under
    ///   a great hanging lamp; the real table shows the same balls for anyone standing beside it.
    /// - The camera circles the cue ball. Aim with the mouse (a line shows what the cue ball will meet and where that
    ///   ball will go), hold the button to swing, let go to play.
    /// - Alone: clear the table, the black last, in as few shots as you can. With friends: eight-ball, two sides
    ///   (players take sides in the order they came to the table and take turns within a side).
    /// - Win a game against a friend, or clear the table alone in 30 shots or fewer, and the table's radio can play
    ///   the whole Everything Playlist (P switches).
    /// What friends share: after every shot the player who took it sends where every ball lies and whose turn it is,
    /// and while the balls roll, where they are. Only the shooter's game rolls the balls.
    public class PoolGame : MonoBehaviour
    {
        public static PoolGame Active { get; private set; }
        public Transform big, small; public float bigScale = 30f; public int landScene; public string id = "jazz"; public string landName = "the Jazz Quarter";
        public TextMesh recordText;
        const string Channel = "pool"; const int SoloBar = 30;

        enum Phase { Off, ShrinkIn, Aim, Roll, Watch, Over, ShrinkOut }
        Phase phase = Phase.Off; float phaseT, shrinkT = 9f, yaw, pitch = 32f, dist = 26f, power, leaveHeld, rackHeld, nextSend, nextState, nextBeat; bool charging, wasLocked;
        readonly PoolSim sim = new PoolSim(); readonly PoolRules rules = new PoolRules();
        // the shared state of the table
        int seq, rackSeed = 1; bool versus; ulong shooter; int[] turns = new int[2]; bool haveState;
        Vector2[] shown = new Vector2[PoolSim.Balls]; Transform[] bigBalls, smallBalls; Transform aim, aim2, ghost; Vector3 focus;
        SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot; double joined; string banner = ""; float bannerUntil;

        class Remote { public string name = "Friend"; public Color color = Color.white; public double joined; public float seen; public bool playing; }
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();

        string Key(string what) => $"EW_Pool_{id}_{what}";
        public int Best => PlayerPrefs.GetInt(Key("best"), 0);
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        public string StartPrompt => "Play pool" + (Best > 0 ? $"  ·  your best clearance {Best} shots" : "");
        ulong Me => Friends.Instance != null && Friends.Instance.Online ? Friends.Instance.LocalId : 0;

        void OnEnable() { Friends.Received += OnNet; }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }

        void Start()
        {
            if (big != null) bigBalls = PoolBuild.MakeBalls(big, bigScale);
            if (small != null) smallBalls = PoolBuild.MakeBalls(small, 1f);
            for (var i = 0; i < PoolSim.Balls; i++) shown[i] = sim.p[i];
            ShowRecord(); Place();
        }

        void ShowRecord()
        {
            if (recordText == null) return;
            recordText.text = (Best > 0 ? $"Your best clearance: {Best} shots" : "No table cleared yet") + "\n" + (Unlocked ? "The Everything Playlist is unlocked here" : $"Beat a friend, or clear it alone in {SoloBar}, to unlock the Everything Playlist");
        }

        // ---------------------------------------------------------------- in and out
        public void Begin(PlayerInteractor who)
        {
            if (Activity.Any || who == null) return;
            walker = who.GetComponent<SimplePlayerController>(); if (walker == null || walker.cameraPivot == null) return;
            hands = who; backPos = walker.transform.position; backRot = walker.transform.rotation; Active = this; phase = Phase.ShrinkIn; phaseT = 0f; shrinkT = 0f;
        }

        void Enter()
        {
            walker.Take(); hands.enabled = false; IslandExplorer.Frozen = true; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = false;
            big.gameObject.SetActive(true); Tune(); joined = SharedChance.Now;
            if (aim == null) { aim = Bar("Aim line", Color.white); aim2 = Bar("Aim line 2", new Color(1f, .85f, .4f)); ghost = Bar("Ghost ball", Color.white); }
            var playing = false; foreach (var o in others.Values) if (Fresh(o) && o.playing) playing = true;
            if (!playing || !haveState) NewGame(); else phase = Phase.Watch;
            Send("hello", null); yaw = 0f; focus = big.position; Say(versus ? "Eight-ball" : "Clear the table, the black last", 4f);
        }

        Transform Bar(string name, Color c)
        {
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(a.GetComponent<Collider>()); a.name = name; a.transform.SetParent(big, false);
            var r = a.GetComponent<MeshRenderer>(); r.sharedMaterial = WorldMaterials.Glow(c, 2f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return a.transform;
        }

        void Quit()
        {
            if (walker != null) walker.Release(backPos, backRot);
            if (hands != null) hands.enabled = true;
            IslandExplorer.Frozen = false; if (PlayerOrb.Local != null) PlayerOrb.Local.enabled = true; Friends.ReportedEye = null;
            if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null;
            if (big != null) big.gameObject.SetActive(false);
            Send("bye", null); Active = null; phase = Phase.Off; charging = false; shrinkT = .55f;
        }

        void Tune() { var d = MusicDirector.Instance; if (d == null) return; d.Held = Everything ? d.Everything : d.Catalog != null ? d.Catalog.Get(landScene) : null; }
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }

        // ---------------------------------------------------------------- the game
        List<(ulong id, double joined)> Roster()
        {
            var r = new List<(ulong, double)>(); if (Active == this) r.Add((Me, joined));
            foreach (var kv in others) if (Fresh(kv.Value) && kv.Value.playing) r.Add((kv.Key, kv.Value.joined));
            r.Sort((a, b) => a.Item2 != b.Item2 ? a.Item2.CompareTo(b.Item2) : a.Item1.CompareTo(b.Item1)); return r;
        }
        int SideOf(ulong who) { var r = Roster(); for (var i = 0; i < r.Count; i++) if (r[i].id == who) return i % 2; return 0; }

        /// Whose shot it is on a side: its players take turns.
        ulong NextShooter(int side)
        {
            var r = Roster(); var mine = new List<ulong>(); for (var i = 0; i < r.Count; i++) if (i % 2 == side || !versus) mine.Add(r[i].id);
            if (mine.Count == 0) return Me; return mine[turns[side] % mine.Count];
        }

        void NewGame()
        {
            rackSeed = (int)(SharedChance.Now % 100000) + 1; sim.Rack(rackSeed); rules.Reset(); versus = Roster().Count > 1; turns[0] = turns[1] = 0; seq++; haveState = true;
            shooter = NextShooter(0); for (var i = 0; i < PoolSim.Balls; i++) shown[i] = sim.p[i];
            phase = shooter == Me ? Phase.Aim : Phase.Watch; SendState(true);
        }

        void AfterShot()
        {
            for (var i = 0; i < PoolSim.Balls; i++) shown[i] = sim.p[i];
            if (versus)
            {
                var again = rules.Settle(sim);
                if (rules.winner >= 0) { phase = Phase.Over; Won(rules.winner == SideOf(Me)); }
                else { if (!again) turns[1 - rules.turn]++; shooter = NextShooter(rules.turn); }   // (the side that just finished moves on to its next player for next time)
            }
            else
            {
                rules.shots++; rules.lastCall = sim.potted.Count > 0 ? "" : ""; if (sim.potted.Contains(0)) { sim.PlaceCue(PoolSim.HeadSpot); rules.lastCall = "In-off: the cue ball goes back"; }
                var left = 0; for (var b = 1; b < PoolSim.Balls; b++) if (!sim.down[b]) left++;
                if (sim.down[8] && left > 0) { rules.winner = 1; rules.lastCall = "The black went down too soon"; phase = Phase.Over; }
                else if (left == 0) { rules.winner = 0; phase = Phase.Over; Cleared(rules.shots); }
                shooter = Me;
            }
            for (var i = 0; i < PoolSim.Balls; i++) shown[i] = sim.p[i];   // (the cue ball may have been put back)
            seq++; if (phase != Phase.Over) phase = shooter == Me ? Phase.Aim : Phase.Watch;
            if (rules.lastCall.Length > 0) Say(rules.lastCall, 3f);
            SendState(true);
        }

        void Cleared(int shots)
        {
            if (Best == 0 || shots < Best) PlayerPrefs.SetInt(Key("best"), shots);
            if (shots <= SoloBar) Unlock($"Cleared in {shots}!"); else Say($"Cleared in {shots} shots. {SoloBar} or fewer unlocks the Everything Playlist.", 7f);
            PlayerPrefs.Save(); ShowRecord();
        }
        void Won(bool mine) { if (mine) Unlock("Your side wins!"); else Say("The other side wins", 6f); }
        void Unlock(string head)
        {
            if (!Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say(head + "\nThe Everything Playlist is unlocked here. P switches it.", 9f); } else Say(head, 6f);
            PlayerPrefs.Save(); ShowRecord();
        }

        // (if anything goes wrong in here, you are put back by the table rather than left stranded)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }
        void LateUpdate() { try { Follow(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            shrinkT += Time.deltaTime; Place();
            if (Active != this) return;
            var dt = Time.deltaTime; phaseT += dt; var kb = Keyboard.current; var mouse = Mouse.current;
            if (phase == Phase.ShrinkIn) { if (shrinkT > .55f) Enter(); else return; }
            if (phase == Phase.ShrinkOut) { if (phaseT > .5f) Quit(); return; }
            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;
            if (canAct)
            {
                var look = mouse.delta.ReadValue() * walker.mouseSensitivity * (kb.leftShiftKey.isPressed ? .15f : 1f); yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, 8f, 88f);
                var sc = mouse.scroll.ReadValue().y; dist = Mathf.Clamp(dist - sc * (Mathf.Abs(sc) > 10f ? .03f : 3f), 6f, 90f);
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing " + landName + "'s songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f) { leaveHeld = 0f; phase = Phase.ShrinkOut; phaseT = 0f; return; }
                rackHeld = kb.rKey.isPressed ? rackHeld + dt : 0f; if (rackHeld > 1f && phase != Phase.Roll) { rackHeld = 0f; NewGame(); Say("New game", 2.5f); }
                if (phase == Phase.Over && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) { NewGame(); Say("New game", 2.5f); }
            }
            // the player whose turn it was has gone: whoever is first at the table takes the shot over
            if (phase == Phase.Watch && shooter != Me && (!others.TryGetValue(shooter, out var sh) || !Fresh(sh) || !sh.playing))
            {
                var r = Roster(); if (r.Count > 0 && r[0].id == Me) { shooter = Me; seq++; phase = rules.winner >= 0 ? Phase.Over : Phase.Aim; SendState(true); }
            }
            if (phase == Phase.Aim && canAct)
            {
                if (!charging && mouse.leftButton.wasPressedThisFrame) { charging = true; power = 0f; phaseT = 0f; }
                if (charging && (mouse.rightButton.wasPressedThisFrame || kb.qKey.wasPressedThisFrame)) charging = false;
                if (charging) power = Mathf.PingPong(phaseT * .8f, 1f);
                if (charging && !mouse.leftButton.isPressed)
                {
                    charging = false; var d = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                    sim.Hit(new Vector2(d.x, d.z), Mathf.Lerp(.25f, PoolSim.MaxSpeed, Mathf.Pow(Mathf.Max(power, .03f), 1.4f))); phase = Phase.Roll;
                }
            }
            if (phase == Phase.Roll)
            {
                var n = Mathf.Min(400, Mathf.CeilToInt(dt / PoolSim.Step)); for (var k = 0; k < n && sim.moving; k++) sim.Advance();
                for (var i = 0; i < PoolSim.Balls; i++) shown[i] = sim.p[i];
                if (Time.time >= nextSend) { nextSend = Time.time + .066f; SendBalls(); }
                if (!sim.moving) AfterShot();
            }
            if (haveState && shooter == Me && Time.time >= nextState) SendState(false);
            else if (Time.time >= nextBeat) { nextBeat = Time.time + 2f; Send("here", null, false); }   // so the others know you're still at the table   // (said again now and then, in case someone missed it or has just arrived)
        }

        /// Puts every ball where it should be drawn, on the big table and the real one.
        void Place()
        {
            if (bigBalls == null && smallBalls == null) return;
            for (var i = 0; i < PoolSim.Balls; i++)
            {
                var on = !sim.down[i]; var q = shown[i];
                if (bigBalls != null) { bigBalls[i].gameObject.SetActive(on); if (on) bigBalls[i].localPosition = new Vector3(q.x * bigScale, PoolSim.R * bigScale, q.y * bigScale); }
                if (smallBalls != null) { smallBalls[i].gameObject.SetActive(on); if (on) smallBalls[i].localPosition = new Vector3(q.x, PoolSim.R, q.y); }
            }
        }

        void Follow()
        {
            if (Active != this || walker == null || phase == Phase.ShrinkIn) return;
            var s = bigScale; var cue = big.TransformPoint(new Vector3(shown[0].x * s, PoolSim.R * s, shown[0].y * s));
            var want = phase == Phase.Aim ? cue : big.position; focus = Vector3.Lerp(focus, want, 1f - Mathf.Exp(-6f * Time.deltaTime));
            var rot = Quaternion.Euler(pitch, yaw, 0); var cam = focus - rot * Vector3.forward * (phase == Phase.Aim ? dist : Mathf.Max(dist, 50f)); cam.y = Mathf.Max(cam.y, big.position.y + 1.5f);
            var body = Quaternion.Euler(0, yaw, 0); var pivot = walker.cameraPivot;
            walker.transform.SetPositionAndRotation(cam - body * pivot.localPosition, body); pivot.localEulerAngles = new Vector3(pitch, 0, 0);
            // the aim: a line to what the cue ball meets, a ring where it will be then, and the way the struck ball will go
            var show = phase == Phase.Aim; aim.gameObject.SetActive(show); ghost.gameObject.SetActive(show); aim2.gameObject.SetActive(false);
            if (show)
            {
                var d = body * Vector3.forward; var hit = sim.Sight(new Vector2(d.x, d.z), out var at); var a = big.TransformPoint(new Vector3(at.x * s, PoolSim.R * s, at.y * s));
                var len = Vector3.Distance(cue, a); aim.SetPositionAndRotation((cue + a) / 2f, body); aim.localScale = new Vector3(.006f * s, .004f * s, Mathf.Max(.01f, len - PoolSim.R * s * 2f));
                ghost.position = a; ghost.rotation = body; ghost.localScale = new Vector3(PoolSim.R * 2f * s, .004f * s, PoolSim.R * 2f * s) * (charging ? .6f + power * .5f : 1f);
                if (hit >= 0)
                {
                    var go = (sim.p[hit] - at).normalized; var from = big.TransformPoint(new Vector3(sim.p[hit].x * s, PoolSim.R * s, sim.p[hit].y * s)); var dir3 = big.TransformDirection(new Vector3(go.x, 0, go.y));
                    aim2.gameObject.SetActive(true); aim2.SetPositionAndRotation(from + dir3 * (.18f * s), Quaternion.LookRotation(dir3)); aim2.localScale = new Vector3(.006f * s, .004f * s, .28f * s);
                }
            }
            Friends.ReportedEye = small != null ? small.TransformPoint(new Vector3(shown[0].x, .1f, shown[0].y)) + Vector3.up * 1.2f : (Vector3?)null;
        }

        // ---------------------------------------------------------------- friends
        static bool Fresh(Remote o) => Time.time - o.seen < 6f;

        void Send(string kind, System.Action<BinaryWriter> body, bool reliable = true)
        {
            var f = Friends.Instance; if (f == null || !f.Online) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((byte)1); w.Write(id); w.Write(kind); w.Write(Active == this); w.Write(joined); w.Write((byte)(PlayerOrb.Local != null ? PlayerOrb.Local.Index : 0)); w.Write(Friends.PlayerName);
                body?.Invoke(w); w.Flush(); f.Broadcast(Channel, ms.ToArray(), reliable);
            }
        }

        void WriteBalls(BinaryWriter w) { var mask = 0; for (var i = 0; i < PoolSim.Balls; i++) if (sim.down[i]) mask |= 1 << i; w.Write(seq); w.Write((ushort)mask); for (var i = 0; i < PoolSim.Balls; i++) { w.Write(shown[i].x); w.Write(shown[i].y); } }
        void SendBalls() => Send("balls", WriteBalls, false);
        void SendState(bool reliable)
        {
            nextState = Time.time + 1.5f;
            Send("state", w => { WriteBalls(w); w.Write(rackSeed); w.Write(versus); w.Write(shooter); w.Write((sbyte)rules.turn); w.Write((sbyte)rules.solidsSide); w.Write((sbyte)rules.winner); w.Write(rules.shots); w.Write(turns[0]); w.Write(turns[1]); w.Write(rules.lastCall ?? ""); }, reliable);
        }

        void ReadBalls(BinaryReader r, out int theirSeq, bool take)
        {
            theirSeq = r.ReadInt32(); int mask = r.ReadUInt16(); var pos = new Vector2[PoolSim.Balls]; for (var i = 0; i < PoolSim.Balls; i++) pos[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
            if (!take) return; for (var i = 0; i < PoolSim.Balls; i++) { sim.down[i] = (mask & (1 << i)) != 0; sim.p[i] = pos[i]; sim.v[i] = Vector2.zero; shown[i] = pos[i]; } sim.moving = false;
        }

        void OnNet(ulong from, string channel, byte[] data)
        {
            if (channel != Channel) return;
            try
            {
                using (var r = new BinaryReader(new MemoryStream(data)))
                {
                    if (r.ReadByte() != 1 || r.ReadString() != id) return; var kind = r.ReadString();
                    if (!others.TryGetValue(from, out var o)) others[from] = o = new Remote();
                    o.playing = r.ReadBoolean(); o.joined = r.ReadDouble(); o.color = PlayerOrb.Palette[Mathf.Clamp(r.ReadByte(), 0, PlayerOrb.Palette.Length - 1)].color; o.name = r.ReadString(); o.seen = Time.time;
                    if (kind == "bye") { o.playing = false; return; }
                    if (kind == "hello") { if (haveState && Active == this) SendState(true); return; }
                    if (kind == "balls") { if (phase == Phase.Roll) return; var from0 = r.BaseStream.Position; ReadBalls(r, out var bs, false); if (bs < seq) return; r.BaseStream.Position = from0; ReadBalls(r, out bs, true); return; }
                    if (kind == "state")
                    {
                        if (phase == Phase.Roll) return;   // mid-shot: mine is the newer truth
                        var mark = r.BaseStream.Position; ReadBalls(r, out var ts, false); if (ts < seq || (ts == seq && haveState)) return;
                        r.BaseStream.Position = mark; ReadBalls(r, out ts, true); seq = ts; haveState = true;
                        rackSeed = r.ReadInt32(); versus = r.ReadBoolean(); shooter = r.ReadUInt64(); rules.turn = r.ReadSByte(); rules.solidsSide = r.ReadSByte(); rules.winner = r.ReadSByte(); rules.shots = r.ReadInt32(); turns[0] = r.ReadInt32(); turns[1] = r.ReadInt32(); rules.lastCall = r.ReadString();
                        if (Active == this && phase != Phase.ShrinkIn && phase != Phase.ShrinkOut)
                        {
                            var was = phase; phase = rules.winner >= 0 ? Phase.Over : shooter == Me ? Phase.Aim : Phase.Watch;
                            if (phase == Phase.Over && was != Phase.Over && versus) Won(rules.winner == SideOf(Me));
                            else if (rules.lastCall.Length > 0) Say(rules.lastCall, 3f);
                            if (phase == Phase.Aim && was != Phase.Aim) Say("Your shot", 2.5f);
                        }
                    }
                }
            }
            catch (System.Exception) { }
        }

        /// For the photo tour only: steps onto the table and plays the break.
        public System.Collections.IEnumerator Demo(PlayerInteractor who)
        {
            Begin(who); var until = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < until) yield return null;
            if (Active != this) { Debug.LogWarning("Pool demo: the game did not start"); yield break; }
            sim.Hit(new Vector2(0, 1), 6.5f); phase = Phase.Roll; pitch = 40f; dist = 60f;
            until = Time.realtimeSinceStartup + 2.5f; while (Time.realtimeSinceStartup < until) yield return null;
            Debug.Log($"Pool demo: cue ball at {sim.p[0]}, balls down {sim.potted.Count}, moving {sim.moving}, phase {phase}");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        // ---------------------------------------------------------------- what you see on the screen
        GUIStyle box, big2, small2; static Texture2D white;

        void OnGUI()
        {
            white ??= Texture2D.whiteTexture;
            var wash = phase == Phase.ShrinkOut ? phaseT / .5f : shrinkT < 1.1f ? 1f - Mathf.Abs(shrinkT - .55f) / .55f : 0f;
            if (wash > 0f && Event.current.type == EventType.Repaint) { var keep = GUI.color; GUI.color = new Color(.85f, .9f, 1f, Mathf.Clamp01(wash)); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white); GUI.color = keep; }
            if (Active != this || phase == Phase.ShrinkIn || MainMenu.Showing) return;
            box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(26, TextAnchor.MiddleCenter, true, true); small2 ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            string top, second;
            if (versus)
            {
                var mySide = SideOf(Me); var who = shooter == Me ? "Your shot" : others.TryGetValue(shooter, out var o) ? o.name + "'s shot" : "Waiting";
                top = $"  Eight-ball  ·  {who}"; second = $"  You: {rules.GroupOf(mySide)}" + (rules.solidsSide >= 0 ? $", {rules.Left(sim, mySide)} left  ·  them: {rules.Left(sim, 1 - mySide)} left" : "");
            }
            else { var left = 0; for (var b = 1; b < PoolSim.Balls; b++) if (!sim.down[b]) left++; top = $"  Clear the table  ·  shots {rules.shots}"; second = $"  {left} balls left  ·  the black goes last" + (Best > 0 ? $"  ·  your best {Best}" : ""); }
            GUI.Box(new Rect(12, 12, 470, 34), top, box); GUI.Box(new Rect(12, 50, 470, 30), second + (Everything ? "  ·  Everything Playlist" : ""), box);
            if (Time.time < bannerUntil && banner.Length > 0) GUI.Box(new Rect(Screen.width / 2f - 330, 96, 660, banner.Contains("\n") ? 92f : 54f), banner, big2);
            if (phase == Phase.Aim)
            {
                var w = 360f; var r = new Rect(Screen.width / 2f - w / 2f, Screen.height - 96, w, 22); GUI.Box(r, "", small2);
                if (charging) { var keep = GUI.color; GUI.color = Color.Lerp(new Color(.4f, .9f, 1f), new Color(1f, .75f, .2f), power); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, (r.width - 6) * power, r.height - 6), white); GUI.color = keep; }
                GUI.Box(new Rect(Screen.width / 2f - 380, Screen.height - 66, 760, 28), charging ? "Let go to play  ·  right click to cancel" : "Aim with the mouse (Shift for fine)  ·  hold click to swing  ·  scroll to zoom  ·  hold R new game  ·  hold X to leave" + (Unlocked ? "  ·  P playlist" : ""), small2);
            }
            else if (phase == Phase.Over) GUI.Box(new Rect(Screen.width / 2f - 260, Screen.height - 66, 520, 28), "Enter  New game   ·   hold X  Leave the table", small2);
            else if (phase == Phase.Watch) GUI.Box(new Rect(Screen.width / 2f - 260, Screen.height - 66, 520, 28), "Look round with the mouse  ·  hold X to leave", small2);
        }
    }
}
