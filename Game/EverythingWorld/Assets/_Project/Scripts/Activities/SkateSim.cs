using UnityEngine;

namespace EverythingWorld.Activities
{
    /// A skatepark and one rider on it, with nothing of Unity's in it (so it can be ridden outside Unity to check it).
    /// The park is a drained swimming pool the size of a field: a flat floor with a curved wall all round it rising
    /// to a deck. On the floor (2026-11-17, a second pass: "more ramps and things to use"): a funbox with a rail
    /// along its top, a spine, a run of rollers, a gap jump (kicker, gap, landing ramp), a hump, a kicker at the far
    /// wall, a low ledge, and rails to grind. Its shape is one function, Height(x, z); Kind(x, z) says which piece is
    /// under a spot (so each can be painted its own colour).
    /// The rider rolls where the board points, is pulled by the slopes (so the walls can be pumped), leaves the top
    /// of a wall or a ramp into the air, and can jump anywhere. In the air: spin (a spin that comes round to facing
    /// along the board, any half turn, lands; anything else is a fall), flip the board, grab it. Come down on a rail
    /// and you grind along it. Tricks landed close together multiply.
    public class SkateSim
    {
        public const float Hx = 45f, Hz = 30f, R = 6f, Deck = 6f, Step = 1f / 120f, Gravity = 16f, MaxPush = 14f;
        static readonly float Lip = R * .951f, DeckY = R - Mathf.Sqrt(R * R - Lip * Lip);
        public const int Floor = 0, Wall = 1, DeckTop = 2, Funbox = 3, Hump = 4, Kicker = 5, Spine = 6, Rollers = 7, Lander = 8, Ledge = 9;

        /// A rail to grind: from a to b, its top at y.
        public struct Rail { public Vector2 a, b; public float y; }
        public static readonly Rail[] Rails =
        {
            new Rail { a = new Vector2(-4.5f, 0), b = new Vector2(4.5f, 0), y = 2.05f },          // along the top of the funbox
            new Rail { a = new Vector2(2f, 10.5f), b = new Vector2(15f, 10.5f), y = .55f },       // a long flat rail
            new Rail { a = new Vector2(24f, -13.7f), b = new Vector2(34f, -13.7f), y = .66f },    // the edge of the ledge
            new Rail { a = new Vector2(34f, -6f), b = new Vector2(34f, 8f), y = .55f },           // by the right-hand wall
            new Rail { a = new Vector2(-34f, 6f), b = new Vector2(-24f, 16f), y = .55f },         // a slanted one on the left
        };

        public static float Height(float x, float z) => Shape(x, z, out _);
        public static int Kind(float x, float z) { Shape(x, z, out var k); return k; }

        static float Shape(float x, float z, out int kind)
        {
            kind = Floor; var h = 0f;
            void Put(float v, int k, ref int kd) { if (v > h + .001f) { h = v; kd = k; } }
            // the pool's wall: a quarter circle from the floor, cut off at the lip, then the flat deck
            var d = Mathf.Min(Hx - Mathf.Abs(x), Hz - Mathf.Abs(z));
            if (d < R) { var c = R - d; if (c >= Lip) { h = DeckY; kind = DeckTop; } else { h = R - Mathf.Sqrt(R * R - c * c); if (h > .001f) kind = Wall; } }
            // the funbox: a flat-topped pyramid in the middle
            Put(Mathf.Clamp(Mathf.Min(1.6f * (9f - Mathf.Abs(x)) / 5f, 1.6f * (6f - Mathf.Abs(z)) / 4f), 0f, 1.6f), Funbox, ref kind);
            // a round hump
            var hd = Mathf.Sqrt((x + 27f) * (x + 27f) + (z + 9f) * (z + 9f)); if (hd < 5f) Put(.8f * (1f + Mathf.Cos(hd / 5f * Mathf.PI)), Hump, ref kind);
            // a kicker that throws you toward the right-hand wall
            if (x > 18f && x < 23f && Mathf.Abs(z) < 3f) Put((x - 18f) / 5f * 1.5f, Kicker, ref kind);
            // the spine: two curved ramps back to back, a ridge along z
            if (z > 7f && z < 21f) { var sd = Mathf.Abs(x + 14f); const float rs = 3.6f; if (sd < rs) { var c = rs - sd; var ends = Mathf.Clamp01(Mathf.Min(z - 7f, 21f - z) / 1.5f); Put(Mathf.Min(2.3f, rs - Mathf.Sqrt(rs * rs - c * c)) * ends, Spine, ref kind); } }
            // rollers: three bumps to pump along
            if (x > 6f && x < 30f && z > 14f && z < 21f) { var ends = Mathf.Clamp01(Mathf.Min(z - 14f, 21f - z) / 1.2f); Put(.75f * (.5f - .5f * Mathf.Cos((x - 6f) / 8f * Mathf.PI * 2f)) * ends, Rollers, ref kind); }
            // the gap jump: up a kicker, over the gap, down the landing ramp
            if (z > -20f && z < -14f) { if (x > -8f && x < -2f) Put((x + 8f) / 6f * 1.7f, Kicker, ref kind); if (x > 3f && x < 10f) Put((10f - x) / 7f * 1.7f, Lander, ref kind); }
            // a low ledge with a ramp up each end
            if (x > 22f && x < 36f && z > -19f && z < -14f) Put(Mathf.Min(.5f, Mathf.Min(x - 22f, 36f - x) * .5f), Ledge, ref kind);
            return h;
        }

        public Vector3 p = new Vector3(-10f, 0, -9f), v; public float yaw = 90f;   // yaw: which way the board points (degrees, 0 = +z)
        public bool grounded = true, fallen, grinding; public float spin, airTime, peak, launchY, fallT, grabT;
        public int score, combo, lastPoints; public string lastTrick = ""; public float comboT, saidT;
        public float flipAngle; public int flips, flipDir; public string grabName = "";   // the flip the board is in (degrees round its length), how many it has done this air, and which grab
        float slopeVy, flipT = -1f, grindT, grindSpeed; int rail = -1; Vector2 grindDir;

        public void Reset() { p = new Vector3(-10f, 0, -9f); v = Vector3.zero; yaw = 90f; grounded = true; fallen = false; grinding = false; score = 0; combo = 0; spin = 0; lastTrick = ""; flipT = -1f; flipAngle = 0f; }

        /// One step. push: -1..1 (forward, brake); steer: -1..1; jump: pressed this step; grab: held;
        /// flip: 1 or -1 the step a flip is asked for (a kickflip, a heelflip), else 0.
        public void Advance(float push, float steer, bool jump, bool grab, int flip = 0)
        {
            const float dt = Step; comboT -= dt; saidT -= dt; if (comboT <= 0f && !grinding) combo = 0;
            if (fallen) { fallT -= dt; v = Vector3.zero; if (fallT <= 0f) fallen = false; return; }
            var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0, Mathf.Cos(yaw * Mathf.Deg2Rad));
            if (grinding)
            {
                // along the rail, slowing a little, until its end or a jump
                var r = Rails[rail]; grindT += dt; grindSpeed = Mathf.Max(2.5f, grindSpeed - .5f * dt); var q = new Vector2(p.x, p.z) + grindDir * grindSpeed * dt; p = new Vector3(q.x, r.y, q.y);
                var len = (r.b - r.a).magnitude; var along = Vector2.Dot(q - r.a, (r.b - r.a) / len);
                if (jump || along < 0f || along > len)
                {
                    var pts = (120 + Mathf.RoundToInt(grindT * 260f)); combo++; comboT = 3.5f; pts *= combo; score += pts; lastPoints = pts; saidT = 2f; lastTrick = $"Grind {grindT:0.0} s" + (combo > 1 ? $"  x{combo}" : "");
                    grinding = false; grounded = false; v = new Vector3(grindDir.x * grindSpeed, jump ? 5.2f : 1.2f, grindDir.y * grindSpeed); Launch();
                }
                return;
            }
            if (grounded)
            {
                var hv = new Vector2(v.x, v.z); var sp = hv.magnitude;
                // the board turns; what speed there is follows it (a board rolls where it points, forwards or backwards)
                yaw += steer * Mathf.Lerp(200f, 135f, Mathf.Clamp01(sp / 12f)) * dt;   // (was 140 slow, 95 fast: it felt like steering a boat) fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0, Mathf.Cos(yaw * Mathf.Deg2Rad));
                var along = Vector2.Dot(hv, new Vector2(fwd.x, fwd.z)); hv = new Vector2(fwd.x, fwd.z) * along;
                const float e = .15f; var g0 = Height(p.x, p.z); float gx = (Height(p.x + e, p.z) - Height(p.x - e, p.z)) / (2f * e), gz = (Height(p.x, p.z + e) - Height(p.x, p.z - e)) / (2f * e);
                gx = Mathf.Clamp(gx, -1.8f, 1.8f); gz = Mathf.Clamp(gz, -1.8f, 1.8f);   // (the upright side of a ramp is a wall, not a launcher)
                var steep = 1f + gx * gx + gz * gz; var pull = new Vector2(-gx, -gz) * (Gravity / steep); hv += new Vector2(fwd.x, fwd.z) * Vector2.Dot(pull, new Vector2(fwd.x, fwd.z)) * dt;
                if (push > 0f && along < MaxPush) hv += new Vector2(fwd.x, fwd.z) * 10.5f * push * dt; else if (push < 0f) hv = Vector2.MoveTowards(hv, Vector2.zero, 12f * dt);
                sp = hv.magnitude; var dec = (.25f + .008f * sp * sp) * dt; hv = sp <= dec ? Vector2.zero : hv * ((sp - dec) / sp);
                slopeVy = gx * hv.x + gz * hv.y; var want = g0 + slopeVy * dt;
                var was = p; p.x += hv.x * dt; p.z += hv.y * dt; Walls(ref hv);
                var g1 = Height(p.x, p.z);
                if (g1 > p.y + .45f) { p = was; hv = -hv * .12f; v = new Vector3(hv.x, 0, hv.y); return; }   // rode into the upright side of something: bumped back
                slopeVy = Mathf.Clamp(slopeVy, -14f, 14f);
                if (jump) { grounded = false; v = new Vector3(hv.x, Mathf.Max(slopeVy, 0f) + 5.2f, hv.y); Launch(); return; }
                if (g1 < want - .05f && slopeVy > 1.5f) { grounded = false; v = new Vector3(hv.x, slopeVy, hv.y); Launch(); return; }   // off the lip of a wall or a ramp
                if (g1 < p.y - .3f) { grounded = false; v = new Vector3(hv.x, 0f, hv.y); Launch(); return; }                           // off a drop (the ledge, the end of the kicker)
                p.y = g1; v = new Vector3(hv.x, 0, hv.y);
            }
            else
            {
                airTime += dt; spin += steer * 330f * dt; v.y -= Gravity * dt; p += v * dt; var hv = new Vector2(v.x, v.z); Walls(ref hv); v.x = hv.x; v.z = hv.y;
                if (grab) { if (grabT <= 0f) grabName = push > .3f ? "nose grab" : push < -.3f ? "tail grab" : "indy"; grabT += dt; }
                if (flip != 0 && flipT < 0f && airTime > .04f) { flipT = 0f; flipDir = flip; }
                if (flipT >= 0f) { flipT += dt / .42f; flipAngle = Mathf.Min(1f, flipT) * 360f * flipDir; if (flipT >= 1f) { flipT = -1f; flipAngle = 0f; flips++; } }
                peak = Mathf.Max(peak, p.y - launchY);
                // a rail under you as you come down: grind it
                if (v.y <= 0f && airTime > .12f)
                    for (var i = 0; i < Rails.Length; i++)
                    {
                        var r = Rails[i]; if (p.y > r.y + .2f || p.y < r.y - .55f) continue; var ab = r.b - r.a; var len = ab.magnitude; var dir = ab / len; var q = new Vector2(p.x, p.z);
                        var t = Vector2.Dot(q - r.a, dir); if (t < 0f || t > len) continue; if ((q - (r.a + dir * t)).magnitude > .6f) continue;
                        if (!CleanAir()) { Fall(); return; }
                        ScoreAir(true); var al = Vector2.Dot(hv, dir); grindDir = al >= 0f ? dir : -dir; grindSpeed = Mathf.Max(4f, Mathf.Abs(al)); rail = i; grinding = true; grindT = 0f; p = new Vector3(r.a.x + dir.x * t, r.y, r.a.y + dir.y * t);
                        yaw = Mathf.Atan2(grindDir.x, grindDir.y) * Mathf.Rad2Deg; spin = 0f; v = Vector3.zero; return;
                    }
                var g = Height(p.x, p.z);
                if (p.y <= g && v.y <= 0f) Land(g);
            }
        }

        void Launch() { spin = 0f; airTime = 0f; peak = 0f; launchY = p.y; grabT = 0f; flips = 0; flipT = -1f; flipAngle = 0f; grabName = ""; }
        /// Came round to facing along the board, and not half way through a flip.
        bool CleanAir() { var turns = Mathf.RoundToInt(spin / 180f); var off = Mathf.Abs(spin - turns * 180f); return !(off > 68f && airTime > .4f) && !(flipT >= 0f && flipT < .55f); }
        void Fall() { grounded = true; grinding = false; p.y = Height(p.x, p.z); fallen = true; fallT = .65f; combo = 0; lastTrick = "Fell"; lastPoints = 0; saidT = 1.5f; v = Vector3.zero; flipT = -1f; flipAngle = 0f; }

        /// Points and a name for what was done in the air just ended (onto the floor, or onto a rail).
        void ScoreAir(bool ontoRail)
        {
            var turns = Mathf.RoundToInt(spin / 180f); var n = Mathf.Abs(turns); if (airTime < .3f && n == 0 && flips == 0 && !ontoRail) return;
            var pts = (n == 0 ? 0 : n == 1 ? 200 : n == 2 ? 500 : n == 3 ? 900 : 900 + (n - 3) * 500) + Mathf.RoundToInt(peak * 60f) + flips * 250; var grabbed = grabT > .3f; if (grabbed) pts = pts * 3 / 2;
            if (pts < 40) return;
            combo++; comboT = 3.5f; pts *= combo; score += pts; lastPoints = pts; saidT = 2f;
            var name = n > 0 ? (n * 180).ToString() : ""; if (flips > 0) name += (name.Length > 0 ? " " : "") + (flips > 1 ? "double " : "") + (flipDir > 0 ? "kickflip" : "heelflip"); if (grabbed) name += (name.Length > 0 ? " " : "") + grabName;
            if (name.Length == 0) name = "Air"; lastTrick = char.ToUpper(name[0]) + name.Substring(1) + (combo > 1 ? $"  x{combo}" : "");
        }

        void Land(float g)
        {
            p.y = g; grounded = true; var turns = Mathf.RoundToInt(spin / 180f);
            yaw += spin; v.y = 0f;
            if (!CleanAir()) { Fall(); return; }
            yaw -= spin - turns * 180f;   // straightened out on landing
            flipT = -1f; flipAngle = 0f; ScoreAir(false);
        }

        /// The edge of the deck: you can't roll off the world.
        void Walls(ref Vector2 hv)
        {
            float mx = Hx + Deck - .5f, mz = Hz + Deck - .5f;
            if (p.x > mx) { p.x = mx; if (hv.x > 0) hv.x = -hv.x * .3f; } if (p.x < -mx) { p.x = -mx; if (hv.x < 0) hv.x = -hv.x * .3f; }
            if (p.z > mz) { p.z = mz; if (hv.y > 0) hv.y = -hv.y * .3f; } if (p.z < -mz) { p.z = -mz; if (hv.y < 0) hv.y = -hv.y * .3f; }
        }
    }
}
