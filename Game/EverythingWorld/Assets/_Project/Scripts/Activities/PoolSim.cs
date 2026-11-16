using System;
using UnityEngine;

namespace EverythingWorld.Activities
{
    /// A pool table and its sixteen balls, in real metres (the picture of it is scaled up afterwards): balls roll and
    /// slow, knock each other on (equal weights, nearly all the speed kept), come off the cushions and drop into six
    /// pockets. No spin: plain pool. Small fixed steps and no Unity physics, so it can be played outside Unity to
    /// check it, and one player's game can roll a shot while the others just watch the balls it reports.
    /// The table lies along z: x across (-W/2..W/2), z along (-L/2..L/2). Ball 0 is the cue ball, 8 the black,
    /// 1-7 solids, 9-15 stripes.
    public class PoolSim
    {
        public const float L = 2.54f, W = 1.27f, R = .0286f, Step = 1f / 600f;
        public const float CornerPocket = .062f, SidePocket = .056f, Friction = .22f, Slide = .12f, MaxSpeed = 7.5f;
        public const int Balls = 16;

        public readonly Vector2[] p = new Vector2[Balls], v = new Vector2[Balls];
        public readonly bool[] down = new bool[Balls];        // potted
        public bool moving;
        // what happened in the shot being played
        public int firstHit = -1; public bool cushionAfterHit; public readonly System.Collections.Generic.List<int> potted = new System.Collections.Generic.List<int>();
        public Action<int, float> onKnock;   // (0 ball-ball, 1 cushion, 2 pocket), how hard

        public static readonly Vector2[] Pockets =
        {
            new Vector2(-W / 2, -L / 2), new Vector2(W / 2, -L / 2), new Vector2(-W / 2 - .012f, 0), new Vector2(W / 2 + .012f, 0), new Vector2(-W / 2, L / 2), new Vector2(W / 2, L / 2),
        };
        public static Vector2 HeadSpot => new Vector2(0, -L / 4f);
        public static Vector2 FootSpot => new Vector2(0, L / 4f);

        public PoolSim() { Rack(1); }

        /// The opening: cue ball on the head spot, the fifteen in a triangle on the foot spot, the black in the middle,
        /// a solid and a stripe at the back corners, the rest shuffled by 'seed' (the same seed gives the same rack).
        public void Rack(int seed)
        {
            for (var i = 0; i < Balls; i++) { v[i] = Vector2.zero; down[i] = false; }
            p[0] = HeadSpot; var rng = new System.Random(seed);
            var others = new System.Collections.Generic.List<int> { 2, 3, 4, 5, 6, 7, 10, 11, 12, 13, 14, 15 };
            for (var i = others.Count - 1; i > 0; i--) { var j = rng.Next(i + 1); (others[i], others[j]) = (others[j], others[i]); }
            var k = 0; var n = 0; const float gap = R * 2f + .0006f;
            for (var row = 0; row < 5; row++)
                for (var col = 0; col <= row; col++, n++)
                {
                    var at = FootSpot + new Vector2((col - row / 2f) * gap, row * gap * .8660254f);
                    var ball = n == 4 ? 8 : n == 10 ? 1 : n == 14 ? 9 : others[k++];
                    p[ball] = at;
                }
            moving = false; firstHit = -1; cushionAfterHit = false; potted.Clear();
        }

        /// Strikes the cue ball.
        public void Hit(Vector2 dir, float speed)
        {
            if (moving || down[0]) return; v[0] = dir.normalized * Mathf.Min(speed, MaxSpeed); moving = true; firstHit = -1; cushionAfterHit = false; potted.Clear();
        }

        /// Puts the cue ball back on the table (after a scratch): at 'want' if it's free there, else the nearest free spot along the head line.
        public void PlaceCue(Vector2 want)
        {
            down[0] = false; v[0] = Vector2.zero;
            for (var t = 0; t < 60; t++)
            {
                var q = want + new Vector2(((t + 1) / 2) * (t % 2 == 0 ? 1f : -1f) * R * 1.1f, 0); q.x = Mathf.Clamp(q.x, -W / 2 + R, W / 2 - R); q.y = Mathf.Clamp(q.y, -L / 2 + R, L / 2 - R);
                var free = true; for (var i = 1; i < Balls; i++) if (!down[i] && (p[i] - q).sqrMagnitude < 4.2f * R * R) free = false;
                if (free) { p[0] = q; return; }
            }
            p[0] = want;
        }

        public void Advance()
        {
            if (!moving) return; const float dt = Step; var any = false;
            for (var i = 0; i < Balls; i++)
            {
                if (down[i]) continue; var sp = v[i].magnitude; if (sp <= 0f) continue;
                var dec = (Friction + Slide * sp) * dt; v[i] = sp <= dec ? Vector2.zero : v[i] * ((sp - dec) / sp);
                p[i] += v[i] * dt; if (v[i].sqrMagnitude > 0f) any = true;
            }
            // ball against ball
            for (var i = 0; i < Balls; i++)
            {
                if (down[i]) continue;
                for (var j = i + 1; j < Balls; j++)
                {
                    if (down[j]) continue; var d = p[j] - p[i]; var dist2 = d.sqrMagnitude; if (dist2 >= 4f * R * R || dist2 < 1e-12f) continue;
                    var dist = Mathf.Sqrt(dist2); var n = d / dist; var push = (2f * R - dist) / 2f; p[i] -= n * push; p[j] += n * push;
                    var rel = Vector2.Dot(v[j] - v[i], n); if (rel >= 0f) continue;
                    var imp = -(1f + .95f) * rel / 2f; v[i] -= n * imp; v[j] += n * imp;
                    if (i == 0 && firstHit < 0) firstHit = j;
                    onKnock?.Invoke(0, -rel);
                }
            }
            // cushions and pockets
            for (var i = 0; i < Balls; i++)
            {
                if (down[i]) continue; var q = p[i];
                var inPocket = false;
                for (var k = 0; k < Pockets.Length; k++)
                {
                    var pr = k == 2 || k == 3 ? SidePocket : CornerPocket;
                    if ((q - Pockets[k]).sqrMagnitude < pr * pr) { inPocket = true; break; }
                }
                if (inPocket) { down[i] = true; v[i] = Vector2.zero; potted.Add(i); onKnock?.Invoke(2, 1f); continue; }
                // the cushions are open at the pockets: no bounce within a pocket's mouth
                bool Mouth(Vector2 at) { for (var k = 0; k < Pockets.Length; k++) { var pr = (k == 2 || k == 3 ? SidePocket : CornerPocket) * 1.25f; if ((at - Pockets[k]).sqrMagnitude < pr * pr) return true; } return false; }
                var hit = false;
                if (q.x < -W / 2 + R && v[i].x < 0f && !Mouth(new Vector2(-W / 2, q.y))) { q.x = -W / 2 + R; v[i].x = -v[i].x * .78f; v[i].y *= .97f; hit = true; }
                if (q.x > W / 2 - R && v[i].x > 0f && !Mouth(new Vector2(W / 2, q.y))) { q.x = W / 2 - R; v[i].x = -v[i].x * .78f; v[i].y *= .97f; hit = true; }
                if (q.y < -L / 2 + R && v[i].y < 0f && !Mouth(new Vector2(q.x, -L / 2))) { q.y = -L / 2 + R; v[i].y = -v[i].y * .78f; v[i].x *= .97f; hit = true; }
                if (q.y > L / 2 - R && v[i].y > 0f && !Mouth(new Vector2(q.x, L / 2))) { q.y = L / 2 - R; v[i].y = -v[i].y * .78f; v[i].x *= .97f; hit = true; }
                // a ball that slips past the cushion line anywhere else has gone down the nearest pocket
                if (Mathf.Abs(q.x) > W / 2 + R * 1.5f || Mathf.Abs(q.y) > L / 2 + R * 1.5f) { down[i] = true; v[i] = Vector2.zero; potted.Add(i); onKnock?.Invoke(2, 1f); continue; }
                p[i] = q; if (hit) { if (firstHit >= 0) cushionAfterHit = true; onKnock?.Invoke(1, v[i].magnitude); }
            }
            if (!any) moving = false;
        }

        /// Where the cue ball, sent this way, would first touch something: the ball it meets (or -1) and where the cue
        /// ball's middle would be then, for the aiming line.
        public int Sight(Vector2 dir, out Vector2 at)
        {
            dir = dir.normalized; var best = float.MaxValue; var ball = -1; var o = p[0];
            for (var i = 1; i < Balls; i++)
            {
                if (down[i]) continue; var d = p[i] - o; var along = Vector2.Dot(d, dir); if (along <= 0f) continue;
                var off2 = d.sqrMagnitude - along * along; if (off2 >= 4f * R * R) continue; var t = along - Mathf.Sqrt(4f * R * R - off2); if (t < best) { best = t; ball = i; }
            }
            // or the cushion
            var tc = float.MaxValue;
            if (dir.x > 1e-5f) tc = Mathf.Min(tc, (W / 2 - R - o.x) / dir.x); if (dir.x < -1e-5f) tc = Mathf.Min(tc, (-W / 2 + R - o.x) / dir.x);
            if (dir.y > 1e-5f) tc = Mathf.Min(tc, (L / 2 - R - o.y) / dir.y); if (dir.y < -1e-5f) tc = Mathf.Min(tc, (-L / 2 + R - o.y) / dir.y);
            if (tc < best) { best = tc; ball = -1; }
            at = o + dir * Mathf.Max(0f, best); return ball;
        }
    }

    /// Eight-ball, kept simple: break, the table is open until someone pots a ball (which gives them solids or
    /// stripes), pot all of yours and then the black to win; pot the black early, or go in-off on it, and you lose.
    /// A foul (cue ball down, nothing hit, or the wrong ball hit first) ends the turn and the cue ball goes back to the
    /// head spot. Sides: 0 and 1; a solo player plays both turns and the table simply counts the shots.
    public class PoolRules
    {
        public int turn;                 // side to play: 0 or 1
        public int solidsSide = -1;      // which side has solids (1-7); -1 while the table is open
        public int winner = -1;          // side, once the game is over
        public int shots; public string lastCall = "";

        public void Reset() { turn = 0; solidsSide = -1; winner = -1; shots = 0; lastCall = "Break"; }

        static bool Solid(int b) => b >= 1 && b <= 7;
        static bool Stripe(int b) => b >= 9;
        public int Left(PoolSim s, int side) { if (solidsSide < 0) return 7; var n = 0; for (var b = 1; b < PoolSim.Balls; b++) if (b != 8 && !s.down[b] && (Solid(b) == (solidsSide == side))) n++; return n; }
        public string GroupOf(int side) => solidsSide < 0 ? "open table" : solidsSide == side ? "solids" : "stripes";

        /// Call when the balls have stopped after a shot. Returns true if the same side shoots again.
        public bool Settle(PoolSim s)
        {
            shots++; var mine = turn; var other = 1 - turn; var scratch = s.potted.Contains(0); var black = s.potted.Contains(8);
            var wasOpen = solidsSide < 0; var needBlack = !wasOpen && LeftBefore(s, mine) == 0;
            var foul = scratch || s.firstHit < 0;
            if (!foul && !wasOpen)
            {
                var hitSolid = Solid(s.firstHit); var wantSolid = solidsSide == mine;
                if (needBlack ? s.firstHit != 8 : (s.firstHit == 8 || hitSolid != wantSolid)) foul = true;
            }
            else if (!foul && wasOpen && s.firstHit == 8 && shots > 1) foul = true;
            if (black)
            {
                winner = needBlack && !foul ? mine : other; lastCall = winner == mine ? "The black is down: game!" : "The black went down too soon"; return false;
            }
            var pottedOwn = false;
            foreach (var b in s.potted)
            {
                if (b == 0 || b == 8) continue;
                if (wasOpen && solidsSide < 0 && !foul) solidsSide = Solid(b) ? mine : other;
                if (solidsSide >= 0 && Solid(b) == (solidsSide == mine)) pottedOwn = true;
            }
            if (scratch) s.PlaceCue(PoolSim.HeadSpot);
            if (foul) { turn = other; lastCall = scratch ? "In-off: the cue ball goes back" : s.firstHit < 0 ? "Nothing hit" : "Wrong ball first"; return false; }
            if (pottedOwn) { lastCall = "Potted: go again"; return true; }
            turn = other; lastCall = ""; return false;
        }

        // how many of this side's balls were up before this shot's pots (so potting your last ball and the black in one shot is not a win)
        int LeftBefore(PoolSim s, int side) { var n = Left(s, side); foreach (var b in s.potted) if (b != 0 && b != 8 && solidsSide >= 0 && Solid(b) == (solidsSide == side)) n++; return n; }
    }
}
