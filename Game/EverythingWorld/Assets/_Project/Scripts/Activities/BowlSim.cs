using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.Activities
{
    /// A bowling lane seen from above, in real metres, with nothing of Unity's in it: a heavy ball, ten pins that
    /// fall when they're struck hard enough and then slide about knocking others over, gutters down both sides.
    /// x is across the lane (-W/2..W/2), z down it from the foul line (0) to the head pin (L).
    public class BowlSim
    {
        public const float W = 1.05f, L = 18.29f, BallR = .108f, PinR = .06f, Step = 1f / 240f, BallMass = 6f, PinMass = 1.5f;
        public Vector2 ball, bv; public bool rolling, gutter;
        public readonly Vector2[] pin = new Vector2[10], pv = new Vector2[10]; public readonly int[] state = new int[10];   // 0 standing, 1 knocked and sliding, 2 gone
        public readonly float[] fellAt = new float[10]; float t;

        public BowlSim() { Rack(); }

        /// Stands all ten pins up: the head pin nearest, four rows.
        public void Rack()
        {
            var n = 0; const float gap = .3048f;
            for (var row = 0; row < 4; row++) for (var k = 0; k <= row; k++, n++) { pin[n] = new Vector2((k - row / 2f) * gap, L + row * gap * .866f); pv[n] = Vector2.zero; state[n] = 0; }
            rolling = false; gutter = false; t = 0f;
        }
        /// Clears away what has fallen (between the two balls of a frame).
        public void Sweep() { for (var i = 0; i < 10; i++) if (state[i] == 1) state[i] = 2; rolling = false; gutter = false; }
        public int Standing() { var n = 0; for (var i = 0; i < 10; i++) if (state[i] == 0) n++; return n; }

        /// Sends the ball down the lane from 'x' at the foul line, 'angle' degrees off straight (positive = to the right), at 'speed'.
        public void Bowl(float x, float angle, float speed)
        {
            ball = new Vector2(Mathf.Clamp(x, -W / 2 + BallR, W / 2 - BallR), 0); var a = angle * Mathf.Deg2Rad; bv = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * speed; rolling = true; gutter = false; t = 0f;
        }

        /// One step; 'hook' (-1..1) curls the ball sideways while it is on the lane.
        public void Advance(float hook)
        {
            if (!rolling) return; const float dt = Step; t += dt;
            if (!gutter) bv.x += hook * .5f * dt;   // (was 1.1: you could steer every ball into the pocket after letting go)
            bv *= 1f - .03f * dt; ball += bv * dt;
            if (!gutter && Mathf.Abs(ball.x) > W / 2 && ball.y < L - .4f) { gutter = true; ball.x = Mathf.Sign(ball.x) * (W / 2 + BallR); bv = new Vector2(0, bv.magnitude * .85f); }
            if (gutter) ball.x = Mathf.Sign(ball.x) * (W / 2 + BallR);
            var any = false;
            for (var i = 0; i < 10; i++)
            {
                if (state[i] == 2) continue;
                if (state[i] == 1)
                {
                    var sp = pv[i].magnitude; var dec = 2.2f * dt; pv[i] = sp <= dec ? Vector2.zero : pv[i] * ((sp - dec) / sp); pin[i] += pv[i] * dt; if (sp > .05f) any = true;
                    if (Mathf.Abs(pin[i].x) > W / 2 + .12f || pin[i].y > L + 1.25f || t - fellAt[i] > 2.2f) state[i] = 2;
                }
                // the ball against this pin
                if (!gutter && state[i] != 2) Hit(ref ball, ref bv, BallMass, BallR, i);
            }
            // pins against pins
            for (var i = 0; i < 10; i++)
            {
                if (state[i] != 1) continue;
                for (var j = 0; j < 10; j++)
                {
                    if (j == i || state[j] == 2) continue; var d = pin[j] - pin[i]; var dist = d.magnitude; if (dist >= 2f * PinR * 1.25f || dist < 1e-5f) continue;
                    var n = d / dist; var rel = Vector2.Dot(pv[i] - pv[j], n); if (rel <= 0f) continue;
                    if (state[j] == 0) { if (rel < .5f) { pv[i] -= n * rel * 1.3f; continue; } state[j] = 1; fellAt[j] = t; }
                    var imp = rel * .9f; pv[i] -= n * imp; pv[j] += n * imp; pin[j] += n * (2f * PinR * 1.25f - dist);
                }
            }
            if (ball.y > L + 1.6f || bv.magnitude < .25f || t > 12f) { if (!any || t > 12f) rolling = false; bv = Vector2.zero; ball.y = Mathf.Max(ball.y, L + 1.7f); }
        }

        void Hit(ref Vector2 b, ref Vector2 v, float mass, float r, int i)
        {
            var d = pin[i] - b; var dist = d.magnitude; var reach = r + PinR * 1.25f; if (dist >= reach || dist < 1e-5f) return;   // (a pin is wider at the belly than at the foot)
            var n = d / dist; var rel = Vector2.Dot(v - pv[i], n); if (rel <= 0f) return;
            if (state[i] == 0) { if (rel < .45f) { v -= n * rel * 1.2f; return; } state[i] = 1; fellAt[i] = t; }
            var imp = (1f + .75f) * rel / (1f / mass + 1f / PinMass); v -= n * (imp / mass); pv[i] += n * (imp / PinMass); pin[i] += n * (reach - dist);
        }
    }

    /// A ten-frame scorecard: strikes and spares scored the proper way.
    public class BowlCard
    {
        public readonly List<int> rolls = new List<int>();
        public bool Done { get { int f = 0, i = 0; while (f < 10 && i < rolls.Count) { if (f == 9) { var need = i + 1 < rolls.Count && (rolls[i] == 10 || rolls[i] + rolls[i + 1] == 10) ? 3 : 2; return rolls.Count - i >= need; } if (rolls[i] == 10) i++; else i += 2; f++; } return false; } }
        /// Which frame is being bowled (0-9) and which ball of it (0, 1 or 2).
        public (int frame, int ball) At { get { int f = 0, i = 0; while (f < 9) { if (i >= rolls.Count) return (f, 0); if (rolls[i] == 10) i++; else { if (i + 1 >= rolls.Count) return (f, 1); i += 2; } f++; } return (9, rolls.Count - i); } }
        public int Total
        {
            get
            {
                int s = 0, i = 0;
                for (var f = 0; f < 10 && i < rolls.Count; f++)
                {
                    int R(int k) => k < rolls.Count ? rolls[k] : 0;
                    if (rolls[i] == 10) { s += 10 + R(i + 1) + R(i + 2); i++; } else if (R(i) + R(i + 1) == 10) { s += 10 + R(i + 2); i += 2; } else { s += R(i) + R(i + 1); i += 2; }
                }
                return s;
            }
        }
        /// True if the pins should be stood up again before the next ball.
        public bool FreshRack { get { var (f, b) = At; if (b == 0) return true; if (f < 9) return false; var i = rolls.Count - b; return rolls[i] == 10 && (b == 1 || rolls[i + 1] == 10) || (b == 2 && rolls[i] + rolls[i + 1] == 10); } }
    }
}
