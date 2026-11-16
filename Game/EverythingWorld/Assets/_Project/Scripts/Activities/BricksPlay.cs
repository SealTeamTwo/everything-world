using UnityEngine;

namespace EverythingWorld.Activities
{
    /// The arcade cabinet's game, with nothing of Unity's in it: a bat along the bottom, a ball, a wall of bricks.
    /// Clear the wall and a new one comes, a row lower and the ball a little faster. Three lives. Our own little game
    /// in an old arcade manner. The field is 1 wide and 1.3 tall; the bat sits near y = 0.
    public class BricksPlay
    {
        public const int Cols = 10, Rows = 6; public const float BatW = .17f, BallR = .012f, BatY = .06f, Top = 1.3f;
        public readonly bool[] brick = new bool[Cols * Rows]; public float batX = .5f; public Vector2 ball, v; public bool served; public int score, lives = 3, level = 1, left;
        public float wallTop = 1.12f;   // where the top row of bricks is; it comes down a little each level
        const float BrickH = .045f;

        public BricksPlay() { NewGame(); }
        public void NewGame() { score = 0; lives = 3; level = 1; Wall(); Ready(); }
        void Wall() { for (var i = 0; i < brick.Length; i++) brick[i] = true; left = brick.Length; wallTop = Mathf.Max(.8f, 1.14f - (level - 1) * .04f); }
        void Ready() { served = false; ball = new Vector2(batX, BatY + .03f); v = Vector2.zero; }
        public float Speed => .62f + (level - 1) * .07f;
        public Rect BrickRect(int c, int r) => new Rect(c / (float)Cols + .004f, wallTop - (r + 1) * BrickH + .003f, 1f / Cols - .008f, BrickH - .006f);
        public static int Points(int row) => (Rows - row) * 10;

        public void Serve() { if (served || lives <= 0) return; served = true; v = new Vector2(.35f, 1f).normalized * Speed; }

        /// One step: the bat goes toward batTarget (0..1).
        public void Step(float dt, float batTarget)
        {
            batX = Mathf.Clamp(Mathf.MoveTowards(batX, batTarget, 2.4f * dt), BatW / 2f, 1f - BatW / 2f);
            if (lives <= 0) return; if (!served) { ball = new Vector2(batX, BatY + .03f); return; }
            var steps = Mathf.Max(1, Mathf.CeilToInt(v.magnitude * dt / (BallR * .8f))); var h = dt / steps;
            for (var k = 0; k < steps; k++)
            {
                ball += v * h;
                if (ball.x < BallR && v.x < 0) { ball.x = BallR; v.x = -v.x; } if (ball.x > 1f - BallR && v.x > 0) { ball.x = 1f - BallR; v.x = -v.x; }
                if (ball.y > Top - BallR && v.y > 0) { ball.y = Top - BallR; v.y = -v.y; }
                // the bat: where on it the ball lands sets the angle it leaves at
                if (v.y < 0 && ball.y < BatY + BallR + .012f && ball.y > BatY - .02f && Mathf.Abs(ball.x - batX) < BatW / 2f + BallR)
                { var off = Mathf.Clamp((ball.x - batX) / (BatW / 2f), -1f, 1f); var a = off * 60f * Mathf.Deg2Rad; v = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * Speed; ball.y = BatY + BallR + .013f; }
                if (ball.y < -.05f) { lives--; Ready(); return; }
                // the bricks
                if (ball.y > wallTop - Rows * BrickH - BallR && ball.y < wallTop + BallR)
                {
                    var c = Mathf.Clamp((int)(ball.x * Cols), 0, Cols - 1); var r = (int)((wallTop - ball.y) / BrickH);
                    if (r >= 0 && r < Rows && brick[r * Cols + c])
                    {
                        brick[r * Cols + c] = false; left--; score += Points(r); var rc = BrickRect(c, r);
                        // off whichever face it came in by
                        var fromSide = Mathf.Min(Mathf.Abs(ball.x - rc.xMin), Mathf.Abs(ball.x - rc.xMax)) < Mathf.Min(Mathf.Abs(ball.y - rc.yMin), Mathf.Abs(ball.y - rc.yMax)) * .6f;
                        if (fromSide) v.x = -v.x; else v.y = -v.y;
                        if (left == 0) { level++; score += 500; Wall(); Ready(); return; }
                    }
                }
            }
        }
    }
}
