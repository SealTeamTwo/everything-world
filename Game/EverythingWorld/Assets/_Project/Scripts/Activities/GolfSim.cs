using System;
using UnityEngine;

namespace EverythingWorld.Activities
{
    public enum GolfEvent { Bounce, Kick, Splash, Holed, Pipe, Rest, Boost, Spring, Land, LipOut }

    /// The ball on one hole: rolls over the pads (slopes pull it, each surface slows it differently), leaves the floor
    /// at an edge or the lip of a ramp and flies, bounces off walls and bumpers (moving ones carry their own speed
    /// into the bounce), is pushed by boosters, currents and fans, drops into pipes, and goes back to where it last
    /// lay if it finds the water. Stepped in small fixed steps; no Unity physics, so it behaves the same everywhere.
    public class GolfSim
    {
        public const float Step = 1f / 240f, Gravity = 13f, MaxSpeed = 17f, HitMax = 30f, CupSpeed = 5.2f;

        public GolfHole hole;
        public Vector3 p, v;                 // p is the bottom of the ball
        public bool moving, holed, grounded;
        public Vector3 lastRest; public float moveTime;
        public GolfSurface under;
        public Action<GolfEvent, Vector3, float> onEvent;
        public bool endless;                 // never called to rest by the clock (pinball: the ball is in play until it drains)

        float restFor, slopeVy, pipeCool; int inBoost = -1;
        Vector2[] offs, offsWas; float[] angs, angsWas, ups;

        public GolfSim(GolfHole h) { hole = h; var n = h.movers.Count; offs = new Vector2[n]; offsWas = new Vector2[n]; angs = new float[n]; angsWas = new float[n]; ups = new float[n]; Place(h.tee); }

        public void Place(Vector3 at) { p = at; p.y = hole.TopAt(at.x, at.z); v = Vector3.zero; moving = false; holed = false; grounded = true; lastRest = p; restFor = 0f; }

        public void Hit(Vector2 dir, float speed)
        {
            if (moving || holed) return;
            dir = dir.normalized; speed = Mathf.Min(speed, HitMax); v = new Vector3(dir.x * speed, 0, dir.y * speed);
            moving = true; grounded = true; moveTime = 0f; restFor = 0f; lastRest = p;
        }

        /// True if something moving (a boat, a propeller, a gate) has reached the ball where it lies: it should be
        /// woken (Wake) so it gets pushed along instead of being passed through.
        public bool Touched(double t)
        {
            if (moving || holed) return false; const float R = GolfHole.BallR - .01f; var q = new Vector2(p.x, p.z);
            for (var i = 0; i < hole.movers.Count; i++) hole.movers[i].Pose(t, out offs[i], out angs[i], out ups[i]);
            foreach (var w in hole.walls)
            {
                var m = w.mover; if (m < 0 || p.y > w.Top + ups[m] - .04f || p.y + 2f * R < w.Bottom + ups[m]) continue; var mv = hole.movers[m];
                Vector2 a = GolfMover.Place(mv.pivot, offs[m], angs[m], w.a), b = GolfMover.Place(mv.pivot, offs[m], angs[m], w.b); var d = b - a; var len2 = d.sqrMagnitude;
                var u = len2 < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, d) / len2); if ((q - (a + d * u)).sqrMagnitude < R * R) return true;
            }
            foreach (var bm in hole.bumpers)
            {
                var m = bm.mover; if (m < 0 || p.y > bm.y + bm.h + ups[m] - .04f || p.y + 2f * R < bm.y + ups[m]) continue;
                var c = GolfMover.Place(hole.movers[m].pivot, offs[m], angs[m], bm.c); if ((q - c).sqrMagnitude < (R + bm.r) * (R + bm.r)) return true;
            }
            return false;
        }
        /// Lets a ball that was lying still roll again without a stroke (something has pushed it).
        public void Wake() { if (holed) return; moving = true; grounded = true; moveTime = 0f; restFor = 0f; }

        static float Friction(GolfSurface s) => s == GolfSurface.Sand ? 9f : s == GolfSurface.Ice ? .3f : s == GolfSurface.Deck ? 1.5f : s == GolfSurface.Stone ? 1.7f : 1.9f;

        void Back(float strength = 1f) { onEvent?.Invoke(GolfEvent.Splash, p, strength); p = lastRest; v = Vector3.zero; moving = false; grounded = true; restFor = 0f; }

        /// One step of Step seconds, at shared-clock time t.
        public void Advance(double t)
        {
            if (!moving || holed) return;
            const float dt = Step; moveTime += dt; pipeCool -= dt;
            for (var i = 0; i < hole.movers.Count; i++) { hole.movers[i].Pose(t, out offs[i], out angs[i], out ups[i]); hole.movers[i].Pose(t - dt, out offsWas[i], out angsWas[i], out _); if (hole.movers[i].manual) angsWas[i] = hole.movers[i].manualPrev; }

            var noRest = false; var hv = new Vector2(v.x, v.z);
            // ---- zones
            for (var i = 0; i < hole.zones.Count; i++)
            {
                var z = hole.zones[i]; if (!z.shape.Contains(p.x, p.z)) { if (inBoost == i) inBoost = -1; continue; }
                var zy = z.shape.Height(p.x, p.z); if (p.y < zy - .2f || p.y > zy + z.reach) { if (inBoost == i) inBoost = -1; continue; }
                switch (z.kind)
                {
                    case GolfZoneKind.Water: if (grounded) { Back(); return; } break;
                    case GolfZoneKind.Boost: if (grounded) { hv += z.dir * z.power * dt; if (inBoost != i) { inBoost = i; onEvent?.Invoke(GolfEvent.Boost, p, 1f); } } break;
                    case GolfZoneKind.Current: if (grounded) { hv = Vector2.MoveTowards(hv, z.dir * z.power, 7f * dt); noRest = true; } break;
                    case GolfZoneKind.Fan: if (z.On(t)) { hv += z.dir * z.power * dt; noRest = true; } break;
                    case GolfZoneKind.Spring: if (grounded) { grounded = false; v.y = z.power; onEvent?.Invoke(GolfEvent.Spring, p, 1f); } break;
                    case GolfZoneKind.Pipe:
                        if (grounded && pipeCool <= 0f)
                        {
                            onEvent?.Invoke(GolfEvent.Pipe, p, 1f); p = z.exit; hv = z.exitDir * Mathf.Max(z.exitSpeed, hv.magnitude * .6f); v.y = 0f; pipeCool = .5f;
                            onEvent?.Invoke(GolfEvent.Pipe, p, 1f);
                        }
                        break;
                }
            }

            if (grounded)
            {
                hole.Ground(p.x, p.z, p.y, out var g0, out var pad0); if (pad0 != null) under = pad0.surface;
                // the slope here, from the floor a hand's width either side
                const float e = .06f; float gx = 0f, gz = 0f;
                if (pad0 != null)
                {
                    // (at an edge only one side has floor: the slope is taken from that side, so a ramp still throws the ball off its lip)
                    bool x1 = hole.Ground(p.x + e, p.z, p.y + .1f, out var hx1, out _), x0 = hole.Ground(p.x - e, p.z, p.y + .1f, out var hx0, out _);
                    bool z1 = hole.Ground(p.x, p.z + e, p.y + .1f, out var hz1, out _), z0 = hole.Ground(p.x, p.z - e, p.y + .1f, out var hz0, out _);
                    gx = x1 && x0 ? (hx1 - hx0) / (2f * e) : x1 ? (hx1 - g0) / e : x0 ? (g0 - hx0) / e : 0f;
                    gz = z1 && z0 ? (hz1 - hz0) / (2f * e) : z1 ? (hz1 - g0) / e : z0 ? (g0 - hz0) / e : 0f;
                }
                gx = Mathf.Clamp(gx, -3f, 3f); gz = Mathf.Clamp(gz, -3f, 3f);
                var steep = 1f + gx * gx + gz * gz; var pull = new Vector2(-gx, -gz) * (Gravity / steep);
                hv += pull * dt;
                // the cup draws a slow ball in
                var toCup = new Vector2(hole.cup.x - p.x, hole.cup.z - p.z); var cupD = toCup.magnitude;
                if (cupD < GolfHole.CupR * 2.2f && Mathf.Abs(p.y - hole.cup.y) < .3f && hv.magnitude < 3.5f && cupD > .01f) hv += toCup / cupD * 5f * dt;
                // rolling friction, and a little drag
                var mu = Friction(under); var sp = hv.magnitude; var dec = (mu + .1f * sp) * dt;
                hv = sp <= dec ? Vector2.zero : hv * ((sp - dec) / sp);
                sp = hv.magnitude; var cap = endless ? MaxSpeed * 1.5f : HitMax * 1.07f; if (sp > cap) hv *= cap / sp;
                slopeVy = gx * hv.x + gz * hv.y;
                var want = p.y + slopeVy * dt;
                p.x += hv.x * dt; p.z += hv.y * dt;
                Collide(ref hv, dt);
                if (!hole.Ground(p.x, p.z, p.y, out var g1, out var pad1) || g1 < want - .035f)
                {
                    grounded = false; v.y = Mathf.Clamp(slopeVy, -6f, 14f);   // off an edge, or off the lip of a ramp
                }
                else { p.y = g1; under = pad1.surface; }

                // into the cup?
                if (grounded && cupD < GolfHole.CupR && Mathf.Abs(p.y - hole.cup.y) < .3f)
                {
                    if (hv.magnitude < CupSpeed) { holed = true; moving = false; p = hole.cup; v = Vector3.zero; onEvent?.Invoke(GolfEvent.Holed, p, 1f); return; }
                    if (restFor >= 0f) { onEvent?.Invoke(GolfEvent.LipOut, p, 1f); restFor = -1f; }   // too fast: it skips over
                }
                else if (restFor < 0f) restFor = 0f;

                // lying still?
                if (grounded && !noRest && !endless && hv.sqrMagnitude < .0064f && pull.magnitude < mu + .05f)
                {
                    if (restFor >= 0f) restFor += dt;
                    if (restFor > .3f) { v = Vector3.zero; moving = false; lastRest = p; onEvent?.Invoke(GolfEvent.Rest, p, 0f); return; }
                }
                else if (restFor > 0f) restFor = 0f;
            }
            else
            {
                var was = p.y; v.y -= Gravity * dt; p.x += hv.x * dt; p.z += hv.y * dt; p.y += v.y * dt;
                Collide(ref hv, dt);
                // (a floor up to half a metre above the falling ball counts: it has flown into a rising slope)
                if (v.y <= 0f && hole.Ground(p.x, p.z, Mathf.Max(was, p.y) + .5f, out var g, out var pad) && p.y <= g)
                {
                    p.y = g; under = pad.surface;
                    if (v.y < -4f) { onEvent?.Invoke(GolfEvent.Land, p, -v.y); v.y = -v.y * .32f; hv *= .9f; }
                    else { v.y = 0f; grounded = true; }
                }
                if (p.y < GolfHole.Sea - .05f) { v.x = hv.x; v.z = hv.y; Back(); return; }
            }
            v.x = hv.x; v.z = hv.y;
            if (moveTime > 30f && !endless) { if (grounded) { v = Vector3.zero; moving = false; lastRest = p; onEvent?.Invoke(GolfEvent.Rest, p, 0f); } else Back(); }
        }

        /// Pushes the ball out of walls and bumpers and turns its speed round.
        void Collide(ref Vector2 hv, float dt)
        {
            const float R = GolfHole.BallR;
            for (var pass = 0; pass < 2; pass++)
            {
                var q = new Vector2(p.x, p.z);
                for (var i = 0; i < hole.walls.Count; i++)
                {
                    var w = hole.walls[i]; Vector2 a = w.a, b = w.b; var up = 0f; var wv = Vector2.zero; var m = w.mover;
                    if (m >= 0) { var mv = hole.movers[m]; a = GolfMover.Place(mv.pivot, offs[m], angs[m], w.a); b = GolfMover.Place(mv.pivot, offs[m], angs[m], w.b); up = ups[m]; }
                    if (p.y > w.Top + up - .04f || p.y + 2f * R < w.Bottom + up) continue;
                    var d = b - a; var len2 = d.sqrMagnitude; var t = len2 < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, d) / len2);
                    var c = a + d * t; var off = q - c; var dist = off.magnitude; if (dist >= R) continue;
                    var n = dist > 1e-5f ? off / dist : (hv.sqrMagnitude > 1e-6f ? -hv.normalized : Vector2.up);
                    if (m >= 0) { var mv = hole.movers[m]; var lc = Vector2.Lerp(w.a, w.b, t); wv = (GolfMover.Place(mv.pivot, offs[m], angs[m], lc) - GolfMover.Place(mv.pivot, offsWas[m], angsWas[m], lc)) / dt; }
                    q = c + n * R; Bounce(ref hv, n, wv, w.bounce, 0f);
                }
                for (var i = 0; i < hole.bumpers.Count; i++)
                {
                    var bm = hole.bumpers[i]; var c = bm.c; var up = 0f; var wv = Vector2.zero; var m = bm.mover;
                    if (m >= 0) { var mv = hole.movers[m]; c = GolfMover.Place(mv.pivot, offs[m], angs[m], bm.c); up = ups[m]; wv = (c - GolfMover.Place(mv.pivot, offsWas[m], angsWas[m], bm.c)) / dt; }
                    if (p.y > bm.y + bm.h + up - .04f || p.y + 2f * R < bm.y + up) continue;
                    var off = q - c; var dist = off.magnitude; if (dist >= R + bm.r) continue;
                    var n = dist > 1e-5f ? off / dist : Vector2.up;
                    q = c + n * (R + bm.r); Bounce(ref hv, n, wv, bm.bounce, bm.kick);
                }
                p.x = q.x; p.z = q.y;
            }
        }

        void Bounce(ref Vector2 hv, Vector2 n, Vector2 wallV, float bounce, float kick)
        {
            var rel = hv - wallV; var into = Vector2.Dot(rel, n); if (into >= 0f) return;
            rel -= n * (into * (1f + bounce));
            var tan = rel - n * Vector2.Dot(rel, n); rel -= tan * .04f;   // a little rub along the wall
            if (kick > 0f) { var away = Vector2.Dot(rel, n); if (away < kick) rel += n * (kick - away); }
            hv = rel + wallV;
            var hard = -into; if (hard > .6f) onEvent?.Invoke(kick > 0f ? GolfEvent.Kick : GolfEvent.Bounce, p, hard);
        }
    }
}
