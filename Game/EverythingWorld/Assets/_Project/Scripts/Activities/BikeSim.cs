using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.Activities
{
    /// One light bike: it never stops, turns only in right angles, and leaves a wall of light behind it. Its trail is
    /// the list of corners it has turned at, ending at where it is now.
    public class Bike
    {
        public Vector2 at; public int dir;                 // 0 = +z, 1 = +x, 2 = -z, 3 = -x
        public bool alive = true; public readonly List<Vector2> corners = new List<Vector2>();
        public static readonly Vector2[] Dirs = { new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, -1), new Vector2(-1, 0) };
        public Vector2 Heading => Dirs[dir];
        public void Start(Vector2 p, int d) { at = p; dir = d; alive = true; corners.Clear(); corners.Add(p); }
        public void Turn(int by) { if (!alive) return; corners.Add(at); dir = (dir + by + 4) % 4; }
    }

    /// The arena and its rules: a square floor with a wall round it; touch that wall, or any wall of light (your own
    /// included), and you're out.
    public static class BikeSim
    {
        public const float Half = 90f, Speed = 31f, Reach = .45f;

        /// The start places, round the arena, each heading for the middle.
        public static void Spawn(Bike b, int slot)
        {
            var k = slot % 8; var ang = k * 45f * Mathf.Deg2Rad; var p = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * (Half - 22f);
            var d = Mathf.Abs(p.x) > Mathf.Abs(p.y) + .01f ? (p.x > 0 ? 3 : 1) : (p.y > 0 ? 2 : 0); b.Start(p, d);
        }

        /// How far a bike could go straight from 'from' along direction 'dir' before it met a wall, up to 'max'.
        public static float Free(Vector2 from, int dir, IList<Bike> all, Bike self, float max)
        {
            var d = Bike.Dirs[dir]; var best = max;
            // the arena's own wall
            var edge = d.x > 0 ? Half - from.x : d.x < 0 ? from.x + Half : d.y > 0 ? Half - from.y : from.y + Half; best = Mathf.Min(best, edge);
            for (var i = 0; i < all.Count; i++)
            {
                var b = all[i]; var n = b.corners.Count;
                for (var k = 0; k < n; k++)
                {
                    var p = b.corners[k]; var q = k + 1 < n ? b.corners[k + 1] : b.at;
                    if (b == self && k >= n - 2) continue;   // the stretch I'm laying down now, and the one I have just turned off (I'm standing on its end)
                    // an axis-aligned stretch p..q against a ray from 'from' along d
                    float lo, hi, t;
                    if (d.x != 0f)
                    {
                        if (Mathf.Abs(p.x - q.x) < 1e-4f) { lo = Mathf.Min(p.y, q.y) - Reach; hi = Mathf.Max(p.y, q.y) + Reach; if (from.y < lo || from.y > hi) continue; t = (p.x - from.x) * d.x; }
                        else { if (Mathf.Abs(p.y - from.y) > Reach) continue; var a = (Mathf.Min(p.x, q.x) - from.x) * d.x; var c = (Mathf.Max(p.x, q.x) - from.x) * d.x; t = Mathf.Min(a, c); if (Mathf.Max(a, c) < 0f) continue; if (t < 0f) t = 0f; }
                    }
                    else
                    {
                        if (Mathf.Abs(p.y - q.y) < 1e-4f) { lo = Mathf.Min(p.x, q.x) - Reach; hi = Mathf.Max(p.x, q.x) + Reach; if (from.x < lo || from.x > hi) continue; t = (p.y - from.y) * d.y; }
                        else { if (Mathf.Abs(p.x - from.x) > Reach) continue; var a = (Mathf.Min(p.y, q.y) - from.y) * d.y; var c = (Mathf.Max(p.y, q.y) - from.y) * d.y; t = Mathf.Min(a, c); if (Mathf.Max(a, c) < 0f) continue; if (t < 0f) t = 0f; }
                    }
                    if (t >= -.01f && t < best) best = Mathf.Max(0f, t);
                }
            }
            return best;
        }

        /// Moves a bike on by dt; it dies if it would pass through a wall. Returns true if it just died.
        public static bool Advance(Bike b, IList<Bike> all, float dt)
        {
            if (!b.alive) return false; var step = Speed * dt; var free = Free(b.at, b.dir, all, b, step + 1f);
            if (free <= step) { b.at += b.Heading * Mathf.Max(0f, free - .05f); b.alive = false; return true; }
            b.at += b.Heading * step; return false;
        }

        /// A simple rider: turns when the way ahead is short, toward the side with more room; now and then turns for no reason.
        public static void Robot(Bike b, IList<Bike> all, System.Random rng, float dt)
        {
            if (!b.alive) return; var ahead = Free(b.at, b.dir, all, b, 60f);
            var whim = rng.NextDouble() < dt * .25; if (ahead > 9f && !whim) return;
            float l = Free(b.at, (b.dir + 3) % 4, all, b, 60f), r = Free(b.at, (b.dir + 1) % 4, all, b, 60f);
            if (whim && Mathf.Max(l, r) < 20f) return; if (Mathf.Max(l, r) < 2f && ahead > 2f) return;
            b.Turn(l > r ? -1 : 1);
        }
    }
}
