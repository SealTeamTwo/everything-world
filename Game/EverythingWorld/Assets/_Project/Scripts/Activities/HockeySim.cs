using UnityEngine;

namespace EverythingWorld.Activities
{
    /// An air hockey table, in real metres: a puck that slides almost for ever, two mallets (each kept to its own
    /// half), walls it rings off, a goal mouth at each end. Side 0 defends z = -L/2, side 1 defends z = +L/2.
    public class HockeySim
    {
        public const float W = 1.2f, L = 2.4f, PuckR = .04f, MalletR = .055f, Goal = .4f, Step = 1f / 240f, MaxSpeed = 6.5f;
        public Vector2 puck, pv; public readonly Vector2[] mallet = { new Vector2(0, -.9f), new Vector2(0, .9f) }, mv = new Vector2[2]; public readonly int[] score = new int[2]; public float serveWait;
        /// Each point starts slow and gets faster the longer the puck stays in play: this is how long this one has run, and the top speed that allows.
        /// False on a game that is only following along while the puck is in the other player's half: it never calls a goal.
        public bool judge = true;
        public float rally; public float Cap => Mathf.Lerp(2.3f, MaxSpeed, Mathf.Clamp01(rally / 28f));

        public void Reset() { score[0] = score[1] = 0; Serve(0); mallet[0] = new Vector2(0, -.9f); mallet[1] = new Vector2(0, .9f); }
        readonly System.Random rng = new System.Random(7);
        /// The puck is put down in the half of whoever was scored on, a little to one side or the other.
        public void Serve(int toSide) { puck = new Vector2((float)(rng.NextDouble() - .5) * .5f, toSide == 0 ? -.35f : .35f); pv = Vector2.zero; serveWait = .8f; rally = 0f; }

        /// Moves a mallet toward where its player wants it, no faster than 'speed', kept inside its half.
        public void Move(int side, Vector2 want, float speed, float dt)
        {
            want.x = Mathf.Clamp(want.x, -W / 2 + MalletR, W / 2 - MalletR);
            want.y = side == 0 ? Mathf.Clamp(want.y, -L / 2 + MalletR, -MalletR * .5f) : Mathf.Clamp(want.y, MalletR * .5f, L / 2 - MalletR);
            var next = Vector2.MoveTowards(mallet[side], want, speed * dt); mv[side] = (next - mallet[side]) / Mathf.Max(dt, 1e-5f); mallet[side] = next;
        }

        /// One small step. Returns the side that just scored, or -1.
        public int Advance(float dt)
        {
            if (serveWait > 0f) { serveWait -= dt; return -1; }
            rally += dt; puck += pv * dt; var sp = pv.magnitude; if (sp > 0f) pv *= Mathf.Max(0f, 1f - .12f * dt);
            for (var i = 0; i < 2; i++)
            {
                var d = puck - mallet[i]; var dist = d.magnitude; if (dist >= PuckR + MalletR) continue; var n = dist > 1e-5f ? d / dist : new Vector2(0, i == 0 ? 1 : -1);
                puck = mallet[i] + n * (PuckR + MalletR); var rel = pv - mv[i]; var vn = Vector2.Dot(rel, n); if (vn < 0f) { rel -= n * (vn * 1.85f); pv = rel + mv[i]; }
            }
            if (puck.x < -W / 2 + PuckR && pv.x < 0f) { puck.x = -W / 2 + PuckR; pv.x = -pv.x * .92f; }
            if (puck.x > W / 2 - PuckR && pv.x > 0f) { puck.x = W / 2 - PuckR; pv.x = -pv.x * .92f; }
            var inMouth = Mathf.Abs(puck.x) < Goal / 2f - PuckR * .5f;
            if (Mathf.Abs(puck.y) > L / 2 - PuckR)
            {
                if (inMouth) { if (Mathf.Abs(puck.y) > L / 2 + PuckR) { if (!judge) { puck.y = Mathf.Sign(puck.y) * (L / 2 + PuckR); pv = Vector2.zero; return -1; } var scorer = puck.y > 0f ? 0 : 1; score[scorer]++; Serve(1 - scorer); return scorer; } }
                else if (puck.y * pv.y > 0f) { puck.y = Mathf.Sign(puck.y) * (L / 2 - PuckR); pv.y = -pv.y * .92f; }
            }
            sp = pv.magnitude; var cap = Cap; if (sp > cap) pv *= cap / sp;
            return -1;
        }

        /// Where a simple opponent on 'side' wants its mallet: when the puck is in its half it gets behind it, on the
        /// line to the far corner of the other goal, and drives through; otherwise it waits in front of its own goal,
        /// following the puck across.
        public Vector2 Robot(int side)
        {
            var s = side == 0 ? -1f : 1f; var mine = puck.y * s > 0f; var m = mallet[side];
            if (!mine) return new Vector2(Mathf.Clamp(puck.x * .7f, -Goal * .6f, Goal * .6f), s * (L / 2 - .25f));
            var aim = new Vector2(puck.x > 0f ? -Goal * .3f : Goal * .3f, -s * L / 2); var dir = (aim - puck).normalized; var toPuck = puck - m; var d = toPuck.magnitude;
            var lined = d < .26f && d > 1e-4f && Vector2.Dot(toPuck / d, dir) > .86f;
            return lined ? puck + dir * .22f : puck - dir * .17f;
        }
    }
}
