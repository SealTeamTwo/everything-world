using UnityEngine;

namespace EverythingWorld.Activities
{
    /// One game of pinball on the Oddball's table (GolfCourses.Pinball), with nothing of Unity's in it so it can be
    /// played outside Unity to check it: three balls, two flippers worked by hand, a launch up the right-hand lane,
    /// points for bumpers (100), the booster lane (50), the spring (250), the pipe (500) and every knock (5).
    /// A ball that finds the drain is gone. A ball that comes to a dead stop somewhere is given a shove.
    public class PinballPlay
    {
        public readonly GolfHole table; public readonly GolfSim sim;
        public int score, ballsLeft = 3; public bool inPlay; public string lastHit = ""; public float lastHitAt;
        const float LRest = -25f, LUp = 32f, RRest = 205f, RUp = 148f, Swing = 520f;
        bool fellOff; double clock, launched; float still, time; readonly System.Random rng = new System.Random(11);
        public System.Action<string, int> onScore;

        public PinballPlay()
        {
            table = GolfCourses.Pinball.holes[0]; sim = new GolfSim(table) { endless = true };
            sim.onEvent = (e, p, f) =>
            {
                switch (e)
                {
                    case GolfEvent.Kick: Add("Bumper", 100); break; case GolfEvent.Boost: Add("The fast lane", 50); break; case GolfEvent.Spring: Add("Boing", 250); break;
                    case GolfEvent.Pipe: Add("Down the pipe", 250); break; case GolfEvent.Bounce: score += 5; break;
                    case GolfEvent.Splash: fellOff = p.y < .6f; break;   // (the drain is up on the table; anything lower fell off it)
                }
            };
            NewGame();
        }

        void Add(string what, int n) { score += n; lastHit = what; lastHitAt = time; onScore?.Invoke(what, n); }
        public void NewGame() { score = 0; ballsLeft = 3; Ready(); }
        void Ready() { var s = GolfCourses.PinStart; sim.Place(new Vector3(s.x, 0, s.z)); inPlay = false; table.movers[0].manualAngle = table.movers[0].manualPrev = LRest; table.movers[1].manualAngle = table.movers[1].manualPrev = RRest; }

        /// Sends the waiting ball up the launch lane: 0 = a soft plunge, 1 = full.
        public void Launch(float power)
        {
            if (inPlay || ballsLeft <= 0) return; Ready(); sim.v = new Vector3(0, 0, Mathf.Lerp(17f, 22f, Mathf.Clamp01(power))); sim.moving = true; sim.grounded = true; inPlay = true; still = 0f; launched = clock;
        }

        /// The table has a glass lid and sides: a fast ball can no longer hop a wall and sail off the top into the sky
        /// (it did on every launch, 2026-11-16). It comes back off the glass a little slower.
        void KeepOnTable()
        {
            var p = sim.p; var v = sim.v; var floor = 1f + Mathf.Clamp(p.z, 0f, 44f) * GolfCourses.PinSlope; var hit = false;
            if (p.y > floor + 1.1f) { p.y = floor + 1.1f; if (v.y > 0f) v.y = -v.y * .2f; hit = true; }
            if (p.z > 43.4f) { p.z = 43.4f; if (v.z > 0f) v.z = -v.z * .55f; hit = true; }
            if (p.x > 10.7f) { p.x = 10.7f; if (v.x > 0f) v.x = -v.x * .6f; hit = true; }
            if (p.x < -10.7f) { p.x = -10.7f; if (v.x < 0f) v.x = -v.x * .6f; hit = true; }
            if (hit) { sim.p = p; sim.v = v; }
        }

        public void Step(float dt, bool left, bool right)
        {
            time += dt; var n = Mathf.Min(120, Mathf.CeilToInt(dt / GolfSim.Step)); var lm = table.movers[0]; var rm = table.movers[1];
            for (var k = 0; k < n; k++)
            {
                lm.manualPrev = lm.manualAngle; lm.manualAngle = Mathf.MoveTowards(lm.manualAngle, left ? LUp : LRest, Swing * GolfSim.Step);
                rm.manualPrev = rm.manualAngle; rm.manualAngle = Mathf.MoveTowards(rm.manualAngle, right ? RUp : RRest, Swing * GolfSim.Step);
                if (!inPlay) continue;
                sim.Advance(clock); clock += GolfSim.Step;
                KeepOnTable();
                if (!sim.moving && fellOff) { fellOff = false; Ready(); lastHit = "Off the table: have it back"; lastHitAt = time; break; }   // not your fault: the same ball again
                if (!sim.moving) { ballsLeft--; inPlay = false; lastHit = ballsLeft > 0 ? "Drained" : "Game over"; lastHitAt = time; break; }   // (the drain puts the ball back and stops it)
                // a ball that falls back down the launch lane is simply launched again
                if (sim.p.x > 9.4f && sim.p.z < 4.6f && sim.v.magnitude < .6f && clock - launched > 2.0) { Ready(); break; }
                still = sim.v.magnitude < .12f ? still + GolfSim.Step : 0f;
                if (still > 3f) { sim.v += new Vector3((float)(rng.NextDouble() - .5) * 4f, 0, -2.5f); sim.grounded = true; still = 0f; }   // stuck: a shove
            }
        }
    }
}
