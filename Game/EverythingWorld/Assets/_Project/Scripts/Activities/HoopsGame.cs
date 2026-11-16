using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Activities
{
    /// The thing you press E on to play basketball (the ball rack).
    public class HoopsStart : Interactable
    {
        public HoopsGame game;
        public override string Prompt => game != null ? game.StartPrompt : "Play";
        public override void Use(PlayerInteractor who) { if (game != null) game.Begin(who); }
    }

    /// One basket: where its ring is (in the court's own metres), which way it faces, and how it moves if it does.
    public class Hoop { public string name; public Vector3 ring; public Vector3 face; public float r = .42f; public bool board = true; public Vector3 slide; public float seconds; public Transform t; public TextMesh counter; }

    /// Draws a street court with its baskets, at any size (the one you play on, and the little model of it).
    public static class HoopsBuild
    {
        // (2026-11-17: everything half as big again, after the owner's first game: a big ball, big hoops, room to run)
        public const float W = 26f, L = 44f, F = 1.45f, BallR = .22f;
        /// How much bigger than a regulation basket this one's board and net are (they follow its ring).
        public static float K(Hoop h) => h.r / .24f;
        public static List<Hoop> Hoops() => new List<Hoop>
        {
            new Hoop { name = "The Regular", ring = new Vector3(0, 3.7f, -19.4f), face = Vector3.forward },
            new Hoop { name = "The Other End", ring = new Vector3(0, 3.7f, 19.4f), face = Vector3.back },
            new Hoop { name = "The High One", ring = new Vector3(-11.2f, 6.6f, 0), face = Vector3.right },
            new Hoop { name = "The Little One", ring = new Vector3(11.2f, 2.6f, -10f), face = Vector3.left, r = .36f },
            new Hoop { name = "The Shuttle", ring = new Vector3(11.2f, 4.1f, 9f), face = Vector3.left, slide = new Vector3(0, 0, 6f), seconds = 6f },
            new Hoop { name = "The Bin", ring = new Vector3(-9f, 1.3f, 13f), face = Vector3.zero, r = .62f, board = false },
        };

        public static void Court(MeshKit mk, float s)
        {
            var floor = WorldMaterials.Surface(new Color(.62f, .3f, .2f), Surface.Painted); var key = WorldMaterials.Surface(new Color(.3f, .5f, .85f), Surface.Painted); var white = WorldMaterials.Surface(new Color(.96f, .95f, .9f), Surface.Painted);
            var conc = WorldMaterials.Surface(new Color(.5f, .5f, .52f), Surface.Stone); var iron = WorldMaterials.Solid(new Color(.1f, .1f, .12f)); var up = Vector3.up;
            mk.Box(conc, -up * (.5f * s), new Vector3(W + 6f, 1f, L + 6f) * s); mk.Box(floor, up * (.01f * s), new Vector3(W, .02f, L) * s);
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(key, new Vector3(0, .025f, e * (L / 2f - 2.9f * F)) * s, new Vector3(4.9f * F, .02f, 5.8f * F) * s); mk.Box(white, new Vector3(e * (W / 2f - .05f), .03f, 0) * s, new Vector3(.1f, .02f, L) * s); mk.Box(white, new Vector3(0, .03f, e * (L / 2f - .05f)) * s, new Vector3(W, .02f, .1f) * s);
                for (var k = 0; k <= 12; k++) { var a = Quaternion.Euler(0, (e > 0 ? 180f : 0f) - 90f + k * 15f, 0); mk.Box(white, (new Vector3(0, .03f, e * (L / 2f - 1.6f * F)) + a * Vector3.forward * 6.6f * F * -e * (e > 0 ? -1f : 1f)) * s, new Vector3(1.75f * F, .02f, .1f) * s, a); }
                // a low wall and fence posts down each side and across each end
                mk.Box(conc, new Vector3(e * (W / 2f + 2.6f), .5f, 0) * s, new Vector3(.4f, 1f, L + 5.6f) * s); mk.Box(conc, new Vector3(0, .5f, e * (L / 2f + 2.6f)) * s, new Vector3(W + 5.6f, 1f, .4f) * s);
                for (var k = -3; k <= 3; k++) { mk.Prism(iron, new Vector3(e * (W / 2f + 2.6f), 1f, k * (L + 5f) / 6f) * s, up, .07f * s, 8f * s, 5); mk.Prism(iron, new Vector3(k * (W + 5f) / 6f, 1f, e * (L / 2f + 2.6f)) * s, up, .07f * s, 8f * s, 5); }
                foreach (var y in new[] { 5f, 9f }) { mk.Box(iron, new Vector3(e * (W / 2f + 2.6f), y, 0) * s, new Vector3(.08f, .08f, L + 5.6f) * s); mk.Box(iron, new Vector3(0, y, e * (L / 2f + 2.6f)) * s, new Vector3(W + 5.6f, .08f, .08f) * s); }
            }
            mk.Box(white, new Vector3(0, .03f, 0) * s, new Vector3(W, .02f, .1f) * s); mk.Prism(white, new Vector3(0, .028f, 0) * s, up, 1.8f * F * s, .004f * s, 20); mk.Prism(floor, new Vector3(0, .03f, 0) * s, up, (1.8f * F - .1f) * s, .004f * s, 20);
        }

        /// One basket's own pieces, built round its ring at the origin (so a sliding one can be moved as a whole).
        public static void Basket(MeshKit mk, Hoop h, float s)
        {
            var white = WorldMaterials.Surface(new Color(.96f, .95f, .9f), Surface.Painted); var red = WorldMaterials.Surface(new Color(.9f, .2f, .15f), Surface.Painted); var gold = WorldMaterials.Glow(new Color(1f, .75f, .2f), 1.6f); var iron = WorldMaterials.Solid(new Color(.1f, .1f, .12f)); var up = Vector3.up;
            for (var k = 0; k < 14; k++) { var a = Quaternion.Euler(0, k * 360f / 14f, 0); mk.Box(gold, a * Vector3.forward * h.r * s, new Vector3(h.r * .5f, .05f, .05f) * s, a); }
            var kk = K(h);
            if (!h.board)
            {
                // a bin: a tapering can under the ring
                mk.Prism(iron, -up * (h.ring.y * s), up, h.r * .8f * s, h.ring.y * s, 12, 1.25f, 0f, false); return;
            }
            var back = -h.face; var rot = Quaternion.LookRotation(h.face); var bb = back * (h.r + .16f) * s;
            mk.Box(white, bb + up * (.45f * kk * s), new Vector3(1.8f * kk, 1.1f * kk, .05f) * s, rot); mk.Box(red, bb + up * (.28f * kk * s) + h.face * (.03f * s), new Vector3(.6f * kk, .45f * kk, .02f) * s, rot); mk.Box(white, bb + up * (.28f * kk * s) + h.face * (.035f * s), new Vector3(.5f * kk, .35f * kk, .02f) * s, rot);
            var post = back * (h.r + 1.1f) * s; mk.Prism(iron, post - up * (h.ring.y * s), up, .13f * s, (h.ring.y + .5f * kk) * s, 6); mk.Prism(iron, post + up * (.45f * kk * s), h.face, .08f * s, .95f * s, 4);
            for (var k = 0; k < 9; k++) { var a = Quaternion.Euler(0, k * 360f / 9f, 0); mk.Prism(white, a * Vector3.forward * h.r * .95f * s, (Vector3.down + a * Vector3.back * .25f).normalized, .012f * s, .42f * kk * s, 3); }
        }
    }

    /// Basketball, freestyle, on a street court you shrink onto (the Hip-Hop Boroughs' activity, 2026-11-17).
    /// - Press E at the ball rack: you shrink onto the court. It is a real court built out of sight high over the
    ///   Boroughs; the little model by the rack is the same court. Here you walk about as you always do.
    /// - Walk up to a ball and click to pick it up: a big, bouncy ball that you dribble as you go. Hold the button to
    ///   wind up a shot: a line of dots shows the arc it will fly, turning gold when it is going in (a shot that is
    ///   nearly right is helped the rest of the way). Let go to shoot. Six baskets, each keeping count: a regular one
    ///   at each end, a high one, a little one, one that slides to and fro, and a bin on the ground.
    /// - A friend dribbling next to you can have the ball taken off them: click it.
    /// - Score in all six and the court's radio can play the whole Everything Playlist (P switches).
    /// Friends on the court are simply there (their orbs, as anywhere); each sends where their ball is and their
    /// counts, and each basket's counter shows everyone's baskets added up.
    public class HoopsGame : MonoBehaviour
    {
        public static HoopsGame Active { get; private set; }
        public Transform big; public int landScene; public string id = "hiphop"; public string landName = "the Boroughs"; public TextMesh recordText;
        const string Channel = "hoops"; const float BallR = HoopsBuild.BallR;
        Transform[] dots; Material dotM, dotGold; bool arcGood; Vector3 arcFrom, arcV; float stealCool; ulong holdingFrom;

        List<Hoop> hoops; int[] mine; Rigidbody[] balls; Rigidbody held; int active = -1; bool playing, charging, wasLocked; float power, chargeT, shrinkT = 9f, leaveHeld, nextSend, outT = -1f; Vector3[] lastPos;
        SimplePlayerController walker; PlayerInteractor hands; Vector3 backPos; Quaternion backRot; string banner = ""; float bannerUntil; Transform eye;
        class Remote { public int[] counts = new int[8]; public Vector3 ball; public bool hasBall, holding; public float seen; public Transform ghost; public Color color = Color.white; }
        readonly Dictionary<ulong, Remote> others = new Dictionary<ulong, Remote>();

        string Key(string what) => $"EW_Hoops_{id}_{what}";
        int Made { get => PlayerPrefs.GetInt(Key("made"), 0); set => PlayerPrefs.SetInt(Key("made"), value); }   // a bit for each basket you have ever scored in
        public bool Unlocked => PlayerPrefs.GetInt(Key("unlocked"), 0) == 1;
        bool Everything { get => Unlocked && PlayerPrefs.GetInt(Key("everything"), 0) == 1; set => PlayerPrefs.SetInt(Key("everything"), value ? 1 : 0); }
        int MadeCount { get { var n = 0; for (var i = 0; i < 6; i++) if ((Made & (1 << i)) != 0) n++; return n; } }
        public string StartPrompt => $"Play basketball  ·  baskets scored in: {MadeCount} of 6";

        void OnEnable() { Friends.Received += OnNet; }
        void OnDisable() { Friends.Received -= OnNet; if (Active == this) Quit(); }
        void Start() { ShowRecord(); }

        void ShowRecord()
        {
            if (recordText == null) return;
            recordText.text = $"Baskets you've scored in: {MadeCount} of 6" + "\n" + (Unlocked ? "The Everything Playlist is unlocked here" : "Score in all six to unlock the Everything Playlist");
        }

        /// Called by the kit once the court is built: its baskets (with their moving parts) and the balls.
        public void Setup(List<Hoop> hs)
        {
            hoops = hs; mine = new int[hs.Count];
            var bouncy = new PhysicsMaterial("Basketball") { bounciness = .88f, bounceCombine = PhysicsMaterialCombine.Maximum, dynamicFriction = .45f, staticFriction = .45f };
            balls = new Rigidbody[5]; lastPos = new Vector3[5];
            for (var i = 0; i < balls.Length; i++)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name = "Basketball"; b.transform.SetParent(big, false); b.transform.localScale = Vector3.one * BallR * 2f; b.transform.localPosition = Home(i);
                b.GetComponent<MeshRenderer>().sharedMaterial = WorldMaterials.Surface(new Color(.92f, .45f, .14f), Surface.Painted); b.GetComponent<SphereCollider>().sharedMaterial = bouncy;
                var rb = b.AddComponent<Rigidbody>(); rb.mass = .6f; rb.linearDamping = 0f; rb.angularDamping = .4f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; rb.interpolation = RigidbodyInterpolation.Interpolate; balls[i] = rb;
            }
        }
        static Vector3 Home(int i) => new Vector3(-3f + i * 1.5f, BallR + .05f, -2f);

        // ---------------------------------------------------------------- the shot
        /// Where a shot would start and how it would leave your hands at this much wind-up: it always goes up in a good
        /// arc (never flatter than 40 degrees), higher if you look up.
        void Shot(float pw, out Vector3 from, out Vector3 v)
        {
            from = eye.position + eye.forward * .7f + Vector3.up * .3f; var flat = Vector3.ProjectOnPlane(eye.forward, Vector3.up); if (flat.sqrMagnitude < 1e-4f) flat = walker.transform.forward; flat.Normalize();
            var look = Mathf.Asin(Mathf.Clamp(eye.forward.y, -1f, 1f)) * Mathf.Rad2Deg; var up = Mathf.Clamp(look + 34f, 40f, 74f) * Mathf.Deg2Rad;
            v = (flat * Mathf.Cos(up) + Vector3.up * Mathf.Sin(up)) * Mathf.Lerp(6f, 23f, pw);
        }

        /// A shot that is nearly right for a basket is made exactly right. True if it was.
        bool Helped(Vector3 from, ref Vector3 v)
        {
            var g = -Physics.gravity.y; var speed = v.magnitude; var flat = new Vector3(v.x, 0, v.z); var hs = flat.magnitude; if (hs < .01f) return false; var up = Mathf.Atan2(v.y, hs);
            foreach (var h in hoops)
            {
                var ring = h.t != null ? h.t.position : big.TransformPoint(h.ring); var to = ring - from; var d = new Vector3(to.x, 0, to.z).magnitude; if (d < 1.2f) continue;
                var drop = d * Mathf.Tan(up) - to.y; if (drop <= .05f) continue;   // (that arc cannot come down into it from here)
                var need = Mathf.Sqrt(g * d * d / (2f * Mathf.Cos(up) * Mathf.Cos(up) * drop));
                var off = Vector3.Angle(flat, new Vector3(to.x, 0, to.z));
                if (off < 9f && Mathf.Abs(speed - need) / need < .17f) { var dir = new Vector3(to.x, 0, to.z).normalized; v = (dir * Mathf.Cos(up) + Vector3.up * Mathf.Sin(up)) * need; return true; }
            }
            return false;
        }

        /// The line of dots along the arc the ball will fly.
        void Arc(bool show)
        {
            if (dots == null)
            {
                dotM = WorldMaterials.Glow(new Color(.75f, .93f, 1f), 1.6f); dotGold = WorldMaterials.Glow(new Color(1f, .78f, .2f), 2.4f); dots = new Transform[30];
                for (var i = 0; i < dots.Length; i++) { var d = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(d.GetComponent<Collider>()); d.name = "Arc"; d.transform.SetParent(big, false); d.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; dots[i] = d.transform; }
            }
            var floor = big.position.y;
            for (var i = 0; i < dots.Length; i++)
            {
                var t = (i + 1) * .075f; var p = arcFrom + arcV * t + Physics.gravity * (.5f * t * t); var on = show && p.y > floor;
                if (dots[i].gameObject.activeSelf != on) dots[i].gameObject.SetActive(on); if (!on) continue;
                dots[i].position = p; dots[i].localScale = Vector3.one * (arcGood ? .16f : .11f); dots[i].GetComponent<MeshRenderer>().sharedMaterial = arcGood ? dotGold : dotM;
            }
        }

        public void Begin(PlayerInteractor who)
        {
            if (Activity.Any || who == null || hoops == null) return;
            walker = who.GetComponent<SimplePlayerController>(); if (walker == null || walker.cameraPivot == null) return;
            hands = who; eye = walker.cameraPivot; backPos = walker.transform.position; backRot = walker.transform.rotation; Active = this; playing = false; shrinkT = 0f; outT = -1f;
        }

        void Enter()
        {
            big.gameObject.SetActive(true); IslandExplorer.Frozen = true; hands.enabled = false; Tune();
            for (var i = 0; i < balls.Length; i++) { balls[i].isKinematic = false; balls[i].position = big.TransformPoint(Home(i)); balls[i].linearVelocity = Vector3.zero; lastPos[i] = balls[i].position; }
            walker.Take(); walker.Release(big.TransformPoint(new Vector3(0, 1.2f, 3f)), Quaternion.LookRotation(big.TransformDirection(Vector3.back)));
            playing = true; held = null; Say("Click a ball to pick it up. Hold click to wind up: the dots show the arc, gold means it's going in. Let go to shoot.\nSix baskets. Score in every one.", 9f);
        }

        void Quit()
        {
            Drop(); if (walker != null) { walker.Take(); walker.Release(backPos, backRot); }
            if (hands != null) hands.enabled = true; IslandExplorer.Frozen = false; if (MusicDirector.Instance != null) MusicDirector.Instance.Held = null;
            if (dots != null) Arc(false); if (big != null) big.gameObject.SetActive(false); Send(true); Active = null; playing = false; shrinkT = .55f; outT = -1f;
        }

        void Tune() { var d = MusicDirector.Instance; if (d == null) return; d.Held = Everything ? d.Everything : d.Catalog != null ? d.Catalog.Get(landScene) : null; }
        void Say(string text, float seconds) { banner = text; bannerUntil = Time.time + seconds; }
        void Drop() { if (held == null) return; held.isKinematic = false; held = null; charging = false; }

        // (if anything goes wrong in here, you are put back by the rack rather than left stranded)
        void Update() { try { Tick(); } catch (System.Exception e) { Debug.LogException(e); if (Active == this) Quit(); } }

        void Tick()
        {
            shrinkT += Time.deltaTime; Baskets(); Ghosts();
            if (Active != this) return;
            if (!playing) { if (shrinkT > .55f) Enter(); else return; }
            if (outT >= 0f) { outT += Time.deltaTime; if (outT > .5f) Quit(); return; }
            var dt = Time.deltaTime; var kb = Keyboard.current; var mouse = Mouse.current;
            var locked = Cursor.lockState == CursorLockMode.Locked && !MainMenu.Showing; var canAct = locked && wasLocked && kb != null && mouse != null && !PlayNotes.Active; wasLocked = locked;
            if (canAct)
            {
                if (kb.pKey.wasPressedThisFrame && Unlocked) { Everything = !Everything; Tune(); Say(Everything ? "Playing the Everything Playlist" : "Playing " + landName + "'s songs", 3f); }
                leaveHeld = kb.xKey.isPressed ? leaveHeld + dt : 0f; if (leaveHeld > .8f) { leaveHeld = 0f; outT = 0f; return; }
                stealCool -= dt;
                if (held == null && mouse.leftButton.wasPressedThisFrame && stealCool <= 0f)
                {
                    // the nearest ball in reach, roughly where you're looking
                    var best = 3.6f; for (var i = 0; i < balls.Length; i++) { var to = balls[i].position - eye.position; var d = to.magnitude; if (d < best && Vector3.Dot(to.normalized, eye.forward) > .35f) { best = d; held = balls[i]; active = i; } }
                    // or the ball a friend is dribbling beside you: take it off them
                    var thief = false; ulong victim = 0; Vector3 at = Vector3.zero;
                    foreach (var kv in others) { var o = kv.Value; if (!o.holding || o.ghost == null || Time.time - o.seen > 2f) continue; var to = o.ghost.position - eye.position; var d = to.magnitude; if (d < Mathf.Min(best, 3f) && Vector3.Dot(to.normalized, eye.forward) > .3f) { best = d; thief = true; victim = kv.Key; at = o.ghost.position; } }
                    if (thief)
                    {
                        active = 0; held = balls[0]; held.position = at; lastPos[0] = at; SendSteal(victim); stealCool = .8f;
                        Say(Friends.Instance != null && Friends.Instance.TryGetFriend(victim, out var vn, out _) ? $"Stolen from {vn}!" : "Stolen!", 2f);
                    }
                    if (held != null) { held.isKinematic = true; charging = false; }
                }
                else if (held != null)
                {
                    if (!charging && mouse.leftButton.wasPressedThisFrame) { charging = true; chargeT = 0f; }
                    // the wind-up fills once and holds at full (the dots show what it will do the whole time)
                    if (charging) { chargeT += dt * (arcGood ? .3f : 1f); power = Mathf.Clamp01(chargeT / 1.5f);   // (the wind-up dawdles while the dots are gold, so there is time to let go)
                    if (mouse.rightButton.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) charging = false; }
                    if (charging) { Shot(power, out arcFrom, out arcV); arcGood = Helped(arcFrom, ref arcV); }
                    if (charging && !mouse.leftButton.isPressed)
                    {
                        var b = held; Drop(); b.position = arcFrom; b.linearVelocity = arcV; b.angularVelocity = -eye.right * 7f;
                        lastPos[active] = b.position;
                    }
                    else if (kb.eKey.wasPressedThisFrame) Drop();
                }
            }
            if (held != null)
            {
                // carried at your side; it bounces to the ground and back as you walk
                var moving = kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed) && !charging;
                var hand = eye.position + eye.forward * .75f + eye.right * .42f - Vector3.up * (charging ? .05f : .5f);
                // a real dribble: down to the floor and back up to your hand, quicker when you run; held still while you wind up
                if (!charging) { var floor = big.position.y + BallR; var u = Mathf.Abs(Mathf.Cos(Time.time * (moving ? 8.5f : 5.5f))); hand.y = Mathf.Lerp(floor, hand.y, 1f - (1f - u) * (1f - u)); }
                held.position = Vector3.Lerp(held.position, hand, 1f - Mathf.Exp(-40f * dt)); held.rotation = Quaternion.Euler(Time.time * 300f, 0, 0) * Quaternion.identity; lastPos[active] = held.position;
            }
            Arc(charging && held != null);
            if (Time.time >= nextSend) { nextSend = Time.time + .08f; Send(false); }
        }

        /// Sliding baskets follow the shared clock (for everyone, whether or not they're on the court).
        void Baskets()
        {
            if (hoops == null) return;
            foreach (var h in hoops) if (h.t != null && h.seconds > 0f) h.t.localPosition = h.ring + h.slide * Mathf.Sin((float)(SharedChance.Now % h.seconds) / h.seconds * Mathf.PI * 2f);
            for (var i = 0; i < hoops.Count; i++) { if (hoops[i].counter == null) continue; var n = mine[i]; foreach (var o in others.Values) if (Time.time - o.seen < 6f) n += o.counts[i]; var text = n.ToString(); if (hoops[i].counter.text != text) hoops[i].counter.text = text; }
        }

        void FixedUpdate()
        {
            if (Active != this || !playing || balls == null) return;
            for (var i = 0; i < balls.Length; i++)
            {
                var p = balls[i].position; var was = lastPos[i]; lastPos[i] = p; if (balls[i] == held) continue;
                // out of the court (over the fence, through the floor): back to the middle
                var local = big.InverseTransformPoint(p); if (local.y < -3f || Mathf.Abs(local.x) > HoopsBuild.W / 2f + 12f || Mathf.Abs(local.z) > HoopsBuild.L / 2f + 12f) { balls[i].position = big.TransformPoint(Home(i)); balls[i].linearVelocity = Vector3.zero; lastPos[i] = balls[i].position; continue; }
                for (var k = 0; k < hoops.Count; k++)
                {
                    var h = hoops[k]; var ring = h.t != null ? h.t.position : big.TransformPoint(h.ring);
                    if (was.y > ring.y && p.y <= ring.y)
                    {
                        var t = (was.y - ring.y) / Mathf.Max(.0001f, was.y - p.y); var at = Vector3.Lerp(was, p, t); at.y = ring.y;
                        if ((at - ring).magnitude < h.r) Scored(k);
                    }
                }
            }
        }

        void Scored(int k)
        {
            mine[k]++; var first = (Made & (1 << k)) == 0; if (first) Made |= 1 << k;
            if (first && MadeCount == hoops.Count && !Unlocked) { PlayerPrefs.SetInt(Key("unlocked"), 1); Everything = true; Tune(); Say("All six!\nThe Everything Playlist is unlocked here. P switches it.", 9f); }
            else Say(first ? $"{hoops[k].name}: that's {MadeCount} of {hoops.Count}" : hoops[k].name + "!", 2.5f);
            PlayerPrefs.Save(); ShowRecord();
        }

        // ---------------------------------------------------------------- friends
        void Send(bool leaving)
        {
            var f = Friends.Instance; if (f == null || !f.Online || mine == null) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write((byte)2); w.Write(id); w.Write(!leaving); w.Write((byte)mine.Length); foreach (var n in mine) w.Write((ushort)Mathf.Min(65535, n));
                var has = !leaving && active >= 0; w.Write(has); w.Write(has && held != null); var p = has ? big.InverseTransformPoint(balls[active].position) : Vector3.zero; w.Write(p.x); w.Write(p.y); w.Write(p.z);
                w.Write((byte)(PlayerOrb.Local != null ? PlayerOrb.Local.Index : 0)); w.Flush(); f.Broadcast(Channel, ms.ToArray(), leaving);
            }
        }

        void OnNet(ulong from, string channel, byte[] data)
        {
            if (channel != Channel) return;
            try
            {
                using (var r = new BinaryReader(new MemoryStream(data)))
                {
                    var kind = r.ReadByte(); if ((kind != 2 && kind != 3) || r.ReadString() != id) return;
                    if (kind == 3)
                    {
                        // someone has taken the ball off whoever this names: if that is you, it is gone from your hands
                        var whom = r.ReadUInt64(); var me = Friends.Instance != null ? Friends.Instance.LocalId : 0;
                        if (whom == me && Active == this && held != null)
                        {
                            var b = held; var i = active; Drop(); b.position = big.TransformPoint(Home(i)); b.linearVelocity = Vector3.zero; lastPos[i] = b.position; stealCool = 1.5f;
                            Say(Friends.Instance != null && Friends.Instance.TryGetFriend(from, out var tn, out _) ? $"{tn} stole your ball!" : "Your ball was stolen!", 2.5f);
                        }
                        return;
                    }
                    if (!others.TryGetValue(from, out var o)) others[from] = o = new Remote();
                    var here = r.ReadBoolean(); var n = r.ReadByte(); for (var i = 0; i < n; i++) { var c = r.ReadUInt16(); if (i < o.counts.Length) o.counts[i] = c; }
                    o.hasBall = r.ReadBoolean() && here; o.holding = r.ReadBoolean() && here; o.ball = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); o.color = PlayerOrb.Palette[Mathf.Clamp(r.ReadByte(), 0, PlayerOrb.Palette.Length - 1)].color; o.seen = here ? Time.time : -99f;
                }
            }
            catch (System.Exception) { }
        }

        void SendSteal(ulong victim)
        {
            var f = Friends.Instance; if (f == null || !f.Online) return;
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms)) { w.Write((byte)3); w.Write(id); w.Write(victim); w.Flush(); f.Broadcast(Channel, ms.ToArray(), true); }
        }

        /// A friend's ball: a plain ball where they say theirs is.
        void Ghosts()
        {
            foreach (var o in others.Values)
            {
                var on = o.hasBall && Time.time - o.seen < 3f && big != null && big.gameObject.activeSelf;
                if (o.ghost == null && on)
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.name = "Friend's basketball"; g.transform.SetParent(big, false); g.transform.localScale = Vector3.one * BallR * 2f;
                    g.GetComponent<MeshRenderer>().sharedMaterial = WorldMaterials.Surface(new Color(.92f, .45f, .14f), Surface.Painted); g.transform.localPosition = o.ball; o.ghost = g.transform;
                }
                if (o.ghost == null) continue; o.ghost.gameObject.SetActive(on); if (on) o.ghost.localPosition = Vector3.Lerp(o.ghost.localPosition, o.ball, 1f - Mathf.Exp(-14f * Time.deltaTime));
            }
        }

        /// For the photo tour only: steps onto the court and drops a ball through the first basket.
        public System.Collections.IEnumerator Demo(PlayerInteractor who)
        {
            Begin(who); var until = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < until) yield return null;
            if (Active != this || !playing) { Debug.LogWarning("Hoops demo: the game did not start"); yield break; }
            var before = mine[0]; balls[0].position = big.TransformPoint(hoops[0].ring + Vector3.up * 1.2f); balls[0].linearVelocity = Vector3.zero; lastPos[0] = balls[0].position;
            until = Time.realtimeSinceStartup + 2.5f; while (Time.realtimeSinceStartup < until) yield return null;
            Debug.Log($"Hoops demo: a ball dropped over the first basket; it counted {mine[0] - before}; player at {big.InverseTransformPoint(walker.transform.position)}");
        }
        public void DemoEnd() { if (Active == this) Quit(); }

        // ---------------------------------------------------------------- what you see on the screen
        GUIStyle box, big2, small2; static Texture2D white;

        void OnGUI()
        {
            white ??= Texture2D.whiteTexture;
            var wash = outT >= 0f ? outT / .5f : shrinkT < 1.1f ? 1f - Mathf.Abs(shrinkT - .55f) / .55f : 0f;
            if (wash > 0f && Event.current.type == EventType.Repaint) { var keep = GUI.color; GUI.color = new Color(1f, .9f, .75f, Mathf.Clamp01(wash)); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white); GUI.color = keep; }
            if (Active != this || !playing || MainMenu.Showing) return;
            box ??= GameFonts.Box(17, TextAnchor.MiddleLeft); big2 ??= GameFonts.Box(24, TextAnchor.MiddleCenter, true, true); small2 ??= GameFonts.Box(15, TextAnchor.MiddleCenter);
            var list = ""; for (var i = 0; i < hoops.Count; i++) list += (i > 0 ? "   " : "") + ((Made & (1 << i)) != 0 ? "● " : "○ ") + hoops[i].name;
            GUI.Box(new Rect(12, 12, 860, 34), "  " + list, box);
            if (Everything) GUI.Box(new Rect(12, 50, 240, 30), "  Everything Playlist", box);
            if (Time.time < bannerUntil && banner.Length > 0) Activity.Banner(banner, 96, big2);
            if (charging && held != null) { var w = 360f; var r = new Rect(Screen.width / 2f - w / 2f, Screen.height - 96, w, 22); GUI.Box(r, "", small2); var keep = GUI.color; GUI.color = Color.Lerp(new Color(.4f, .9f, 1f), new Color(1f, .75f, .2f), power); GUI.DrawTexture(new Rect(r.x + 3, r.y + 3, (r.width - 6) * power, r.height - 6), white); GUI.color = keep; }
            Activity.Hint(Screen.height - 66, held == null ? "Click a ball to pick it up (or one a friend is dribbling, to steal it)  ·  hold X to leave" + (Unlocked ? "  ·  P playlist" : "") : charging ? (arcGood ? "Gold: it's going in. Let go!" : "Let go to shoot  ·  look up for a higher arc  ·  right click to cancel") : "Hold click to wind up a shot  ·  E put it down  ·  hold X to leave", small2);
        }
    }
}
