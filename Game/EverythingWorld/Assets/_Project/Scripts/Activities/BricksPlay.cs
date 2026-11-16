using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.Activities
{
    /// The arcade cabinet's game, with nothing of Unity's in it: a bat along the bottom, a ball, a wall of bricks.
    /// Our own little game in an old arcade manner. The field is 1 wide and 1.3 tall; the bat sits near y = 0.
    /// What keeps it lively (2026-11-17):
    /// - the ball gets a little faster every time it comes off the bat, until you lose it;
    /// - the top rows are armoured (two hits, more rows of them each wall), and each new wall has gaps in a new pattern;
    /// - some bricks drop a capsule to catch on the bat: M three balls at once, W a wide bat for a while, + a life;
    /// - clear the wall and a new one comes, lower, the bat a little narrower.
    public class BricksPlay
    {
        public const int Cols = 10, Rows = 6; public const float BallR = .012f, BatY = .06f, Top = 1.3f;
        public const int Multi = 0, Wide = 1, Life = 2;
        public struct Capsule { public Vector2 p; public int kind; }
        public struct Ball { public Vector2 p, v; }

        public readonly int[] hp = new int[Cols * Rows];   // hits each brick still needs (0 = gone)
        public readonly List<Ball> balls = new List<Ball>(); public readonly List<Capsule> capsules = new List<Capsule>();
        public float batX = .5f, wideFor; public bool served; public int score, lives = 3, level = 1, left, rally;
        public float wallTop = 1.12f;   // where the top row of bricks is; it comes down a little each level
        const float BrickH = .045f; System.Random rng = new System.Random(5);

        /// The first ball (or where the ball waits on the bat).
        public Vector2 ball => balls.Count > 0 ? balls[0].p : new Vector2(batX, BatY + .03f);
        public float BatW => (wideFor > 0f ? .25f : Mathf.Max(.09f, .135f - (level - 1) * .008f));
        public bool Brick(int c, int r) => hp[r * Cols + c] > 0;

        public BricksPlay() { NewGame(); }
        public void NewGame() { score = 0; lives = 3; level = 1; rng = new System.Random(5); Wall(); Ready(); }

        void Wall()
        {
            left = 0; var armoured = Mathf.Min(Rows, level);   // one more armoured row each wall
            for (var r = 0; r < Rows; r++) for (var c = 0; c < Cols; c++)
            {
                // from the second wall on, a pattern of gaps: columns, a chequer, a pyramid, in turn
                var gap = level > 1 && ((level % 3 == 2 && c % 3 == 2) || (level % 3 == 0 && (r + c) % 2 == 1 && r > 1) || (level % 3 == 1 && Mathf.Abs(c - 4.5f) > 5.5f - r));
                hp[r * Cols + c] = gap ? 0 : r < armoured ? 2 : 1; if (!gap) left++;
            }
            wallTop = Mathf.Max(.8f, 1.14f - (level - 1) * .04f); capsules.Clear();
        }

        void Ready() { served = false; balls.Clear(); rally = 0; }
        public float Speed => Mathf.Min(1.55f, .66f + (level - 1) * .07f + rally * .022f);
        public Rect BrickRect(int c, int r) => new Rect(c / (float)Cols + .004f, wallTop - (r + 1) * BrickH + .003f, 1f / Cols - .008f, BrickH - .006f);
        public static int Points(int row) => (Rows - row) * 10;

        public void Serve()
        {
            if (served || lives <= 0) return; served = true;
            balls.Add(new Ball { p = new Vector2(batX, BatY + .03f), v = new Vector2(rng.Next(2) == 0 ? .35f : -.35f, 1f).normalized * Speed });
        }

        /// One step: the bat goes toward batTarget (0..1).
        public void Step(float dt, float batTarget)
        {
            var bw = BatW; batX = Mathf.Clamp(Mathf.MoveTowards(batX, batTarget, 2.6f * dt), bw / 2f, 1f - bw / 2f);
            if (lives <= 0 || !served) return;
            if (wideFor > 0f) wideFor -= dt;

            // capsules fall; the bat catches them
            for (var i = capsules.Count - 1; i >= 0; i--)
            {
                var k = capsules[i]; k.p.y -= .34f * dt; capsules[i] = k;
                if (k.p.y < BatY + .03f && k.p.y > BatY - .03f && Mathf.Abs(k.p.x - batX) < bw / 2f + .03f)
                {
                    capsules.RemoveAt(i); score += 50;
                    if (k.kind == Wide) wideFor = 14f; else if (k.kind == Life) lives = Mathf.Min(5, lives + 1);
                    else if (balls.Count > 0 && balls.Count < 6) { var b0 = balls[0]; foreach (var a in new[] { -28f, 28f }) { var rad = a * Mathf.Deg2Rad; balls.Add(new Ball { p = b0.p, v = new Vector2(b0.v.x * Mathf.Cos(rad) - b0.v.y * Mathf.Sin(rad), b0.v.x * Mathf.Sin(rad) + b0.v.y * Mathf.Cos(rad)) }); } }
                }
                else if (k.p.y < -.05f) capsules.RemoveAt(i);
            }

            for (var bi = balls.Count - 1; bi >= 0; bi--)
            {
                var b = balls[bi]; var gone = false; var cleared = false;
                var steps = Mathf.Max(1, Mathf.CeilToInt(b.v.magnitude * dt / (BallR * .8f))); var h = dt / steps;
                for (var k = 0; k < steps && !gone && !cleared; k++)
                {
                    b.p += b.v * h;
                    if (b.p.x < BallR && b.v.x < 0) { b.p.x = BallR; b.v.x = -b.v.x; } if (b.p.x > 1f - BallR && b.v.x > 0) { b.p.x = 1f - BallR; b.v.x = -b.v.x; }
                    if (b.p.y > Top - BallR && b.v.y > 0) { b.p.y = Top - BallR; b.v.y = -b.v.y; }
                    // the bat: where on it the ball lands sets the angle it leaves at, and each touch quickens it
                    if (b.v.y < 0 && b.p.y < BatY + BallR + .012f && b.p.y > BatY - .02f && Mathf.Abs(b.p.x - batX) < bw / 2f + BallR)
                    { rally++; var off = Mathf.Clamp((b.p.x - batX) / (bw / 2f), -1f, 1f); var a = off * 62f * Mathf.Deg2Rad; b.v = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * Speed; b.p.y = BatY + BallR + .013f; }
                    if (b.p.y < -.05f) { gone = true; break; }
                    // the bricks
                    if (b.p.y > wallTop - Rows * BrickH - BallR && b.p.y < wallTop + BallR)
                    {
                        var c = Mathf.Clamp((int)(b.p.x * Cols), 0, Cols - 1); var r = (int)((wallTop - b.p.y) / BrickH);
                        if (r >= 0 && r < Rows && hp[r * Cols + c] > 0)
                        {
                            var rc = BrickRect(c, r);
                            // off whichever face it came in by
                            var fromSide = Mathf.Min(Mathf.Abs(b.p.x - rc.xMin), Mathf.Abs(b.p.x - rc.xMax)) < Mathf.Min(Mathf.Abs(b.p.y - rc.yMin), Mathf.Abs(b.p.y - rc.yMax)) * .6f;
                            if (fromSide) b.v.x = -b.v.x; else b.v.y = -b.v.y;
                            b.p += b.v * h;   // (clear of it, so one touch is one hit)
                            if (--hp[r * Cols + c] > 0) score += 5;
                            else
                            {
                                left--; score += Points(r) * (balls.Count > 1 ? 2 : 1);   // double while more than one ball is up
                                if (rng.Next(100) < 11 && capsules.Count < 3) { var roll = rng.Next(10); capsules.Add(new Capsule { p = new Vector2(rc.x + rc.width / 2f, rc.y + rc.height / 2f), kind = roll < 5 ? Multi : roll < 9 ? Wide : Life }); }
                                if (left == 0) cleared = true;
                            }
                        }
                    }
                }
                if (cleared) { level++; score += 500; Wall(); Ready(); return; }
                if (gone) balls.RemoveAt(bi); else balls[bi] = b;
            }
            if (balls.Count == 0) { lives--; capsules.Clear(); wideFor = 0f; Ready(); }
        }
    }
}
