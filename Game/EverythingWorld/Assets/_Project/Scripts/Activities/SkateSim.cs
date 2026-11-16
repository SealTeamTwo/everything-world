using UnityEngine;

namespace EverythingWorld.Activities
{
    /// A skatepark and one rider on it, with nothing of Unity's in it (so it can be ridden outside Unity to check it).
    /// The park is a drained swimming pool the size of a field: a flat floor with a curved wall all round it rising
    /// to a deck, a funbox in the middle, a hump and a kicker. Its shape is one function, Height(x, z).
    /// The rider rolls where the board points, is pulled by the slopes (so the walls can be pumped), leaves the top
    /// of a wall or a ramp into the air, and can jump anywhere. In the air they spin; a spin that comes round to
    /// facing along the board (any half turn) lands, anything else is a fall. Tricks landed close together multiply.
    public class SkateSim
    {
        public const float Hx = 45f, Hz = 30f, R = 6f, Deck = 6f, Step = 1f / 120f, Gravity = 16f, MaxPush = 12f;
        static readonly float Lip = R * .951f, DeckY = R - Mathf.Sqrt(R * R - Lip * Lip);

        public static float Height(float x, float z)
        {
            // the pool's wall: a quarter circle from the floor, cut off at the lip, then the flat deck
            var d = Mathf.Min(Hx - Mathf.Abs(x), Hz - Mathf.Abs(z)); var h = 0f;
            if (d < R) { var c = R - d; h = c >= Lip ? DeckY : R - Mathf.Sqrt(R * R - c * c); }
            // the funbox: a flat-topped pyramid in the middle
            h = Mathf.Max(h, Mathf.Clamp(Mathf.Min(1.6f * (9f - Mathf.Abs(x)) / 5f, 1.6f * (6f - Mathf.Abs(z)) / 4f), 0f, 1.6f));
            // a round hump, and a kicker that throws you toward the right-hand wall
            var hd = Mathf.Sqrt((x + 25f) * (x + 25f) + z * z); if (hd < 5f) h = Mathf.Max(h, .7f * (1f + Mathf.Cos(hd / 5f * Mathf.PI)));
            if (x > 18f && x < 23f && Mathf.Abs(z) < 3f) h = Mathf.Max(h, (x - 18f) / 5f * 1.5f);
            return h;
        }

        public Vector3 p = new Vector3(-10f, 0, -15f), v; public float yaw = 90f;   // yaw: which way the board points (degrees, 0 = +z)
        public bool grounded = true, fallen; public float spin, airTime, peak, launchY, fallT, grabT;
        public int score, combo, lastPoints; public string lastTrick = ""; public float comboT, saidT;
        float slopeVy;

        public void Reset() { p = new Vector3(-10f, 0, -15f); v = Vector3.zero; yaw = 90f; grounded = true; fallen = false; score = 0; combo = 0; spin = 0; lastTrick = ""; }

        /// One step. push: -1..1 (forward, brake); steer: -1..1; jump: pressed this step; grab: held.
        public void Advance(float push, float steer, bool jump, bool grab)
        {
            const float dt = Step; comboT -= dt; saidT -= dt; if (comboT <= 0f) combo = 0;
            if (fallen) { fallT -= dt; v = Vector3.zero; if (fallT <= 0f) fallen = false; return; }
            var fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0, Mathf.Cos(yaw * Mathf.Deg2Rad));
            if (grounded)
            {
                var hv = new Vector2(v.x, v.z); var sp = hv.magnitude;
                // the board turns; what speed there is follows it (a board rolls where it points, forwards or backwards)
                yaw += steer * (sp > 6f ? 95f : 140f) * dt; fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0, Mathf.Cos(yaw * Mathf.Deg2Rad));
                var along = Vector2.Dot(hv, new Vector2(fwd.x, fwd.z)); hv = new Vector2(fwd.x, fwd.z) * along;
                const float e = .15f; var g0 = Height(p.x, p.z); float gx = (Height(p.x + e, p.z) - Height(p.x - e, p.z)) / (2f * e), gz = (Height(p.x, p.z + e) - Height(p.x, p.z - e)) / (2f * e);
                var steep = 1f + gx * gx + gz * gz; var pull = new Vector2(-gx, -gz) * (Gravity / steep); hv += new Vector2(fwd.x, fwd.z) * Vector2.Dot(pull, new Vector2(fwd.x, fwd.z)) * dt;
                if (push > 0f && along < MaxPush) hv += new Vector2(fwd.x, fwd.z) * 7f * push * dt; else if (push < 0f) hv = Vector2.MoveTowards(hv, Vector2.zero, 12f * dt);
                sp = hv.magnitude; var dec = (.35f + .012f * sp * sp) * dt; hv = sp <= dec ? Vector2.zero : hv * ((sp - dec) / sp);
                slopeVy = gx * hv.x + gz * hv.y; var want = g0 + slopeVy * dt;
                p.x += hv.x * dt; p.z += hv.y * dt; Walls(ref hv);
                var g1 = Height(p.x, p.z);
                if (jump) { grounded = false; v = new Vector3(hv.x, Mathf.Max(slopeVy, 0f) + 5.2f, hv.y); Launch(); return; }
                if (g1 < want - .05f && slopeVy > 1.5f) { grounded = false; v = new Vector3(hv.x, slopeVy, hv.y); Launch(); return; }   // off the lip of a wall or a ramp
                p.y = g1; v = new Vector3(hv.x, 0, hv.y);
            }
            else
            {
                airTime += dt; spin += steer * 330f * dt; if (grab) grabT += dt; v.y -= Gravity * dt; p += v * dt; var hv = new Vector2(v.x, v.z); Walls(ref hv); v.x = hv.x; v.z = hv.y;
                peak = Mathf.Max(peak, p.y - launchY); var g = Height(p.x, p.z);
                if (p.y <= g && v.y <= 0f) Land(g);
            }
        }

        void Launch() { spin = 0f; airTime = 0f; peak = 0f; launchY = p.y; grabT = 0f; }

        void Land(float g)
        {
            p.y = g; grounded = true; var turns = Mathf.RoundToInt(spin / 180f); var off = Mathf.Abs(spin - turns * 180f);
            yaw += spin; var hv = new Vector2(v.x, v.z); v.y = 0f;
            if (off > 42f && airTime > .25f) { fallen = true; fallT = 1.1f; combo = 0; lastTrick = "Fell"; lastPoints = 0; saidT = 1.5f; v = Vector3.zero; return; }
            yaw -= spin - turns * 180f;   // straightened out on landing
            var n = Mathf.Abs(turns); if (airTime < .3f && n == 0) return;
            var pts = (n == 0 ? 0 : n == 1 ? 200 : n == 2 ? 500 : n == 3 ? 900 : 900 + (n - 3) * 500) + Mathf.RoundToInt(peak * 60f); if (grabT > .3f) pts = pts * 3 / 2; if (pts < 40) return;
            combo++; comboT = 3.5f; pts *= combo; score += pts; lastPoints = pts; saidT = 2f;
            lastTrick = (n == 0 ? "Air" : (n * 180) + (grabT > .3f ? " grab" : "")) + (combo > 1 ? $"  x{combo}" : "");
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
