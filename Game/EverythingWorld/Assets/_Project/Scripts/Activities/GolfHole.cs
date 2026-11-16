using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.Activities
{
    /// Mini golf (the Riviera's activity, 2026-11-16). One hole, described as plain shapes so the same description
    /// drives the ball (GolfSim), the picture of it (GolfBuild) and the little model of the course you shrink onto:
    /// - pads: the floor (flat or sloping strips, round greens that can be bowls or domes), each with a surface;
    /// - walls: kerbs and obstacles the ball bounces off, some riding on movers;
    /// - movers: things that turn, slide, lift or swing, timed by the shared clock so friends see them the same;
    /// - bumpers: round posts, some of which kick;
    /// - zones: water, boosters, currents, fans, springs and pipes.
    /// Everything is in the hole's own metres: x to the right, z ahead from the tee, y up; the sea is at y = 0.
    public enum GolfSurface { Green, Deck, Sand, Ice, Stone }

    public class GolfPad
    {
        public Vector2[] poly;                              // a convex outline, or null for a disc
        public Vector2 a, b; public float ya, yb, tilt;     // height runs from ya at a to yb at b, plus 'tilt' per metre to the right
        public Vector2 c; public float r, bowl;             // a disc: centre, radius; ya at the centre, rising 'bowl' at the rim (negative = a dome)
        public GolfSurface surface;
        public bool overlay;                                // a patch lying on other floor (a bunker, a hump): drawn without sides
        public float minX, maxX, minZ, maxZ; float sign;

        public void Bake()
        {
            if (poly == null) { minX = c.x - r; maxX = c.x + r; minZ = c.y - r; maxZ = c.y + r; return; }
            minX = minZ = float.MaxValue; maxX = maxZ = float.MinValue; var area = 0f;
            for (var i = 0; i < poly.Length; i++)
            {
                var p = poly[i]; var q = poly[(i + 1) % poly.Length]; area += p.x * q.y - q.x * p.y;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minZ = Mathf.Min(minZ, p.y); maxZ = Mathf.Max(maxZ, p.y);
            }
            sign = area >= 0f ? 1f : -1f; minX -= .05f; maxX += .05f; minZ -= .05f; maxZ += .05f;
        }

        public bool Contains(float x, float z)
        {
            if (x < minX || x > maxX || z < minZ || z > maxZ) return false;
            if (poly == null) { float dx = x - c.x, dz = z - c.y; return dx * dx + dz * dz <= r * r; }
            for (var i = 0; i < poly.Length; i++)
            {
                var p = poly[i]; var q = poly[(i + 1) % poly.Length]; float ex = q.x - p.x, ez = q.y - p.y;
                var cross = (ex * (z - p.y) - ez * (x - p.x)) * sign;
                if (cross < -.02f * Mathf.Sqrt(ex * ex + ez * ez)) return false;   // 2 cm of give, so strips that share an edge leave no crack
            }
            return true;
        }

        public float Height(float x, float z)
        {
            if (poly == null) { float dx = x - c.x, dz = z - c.y; return ya + bowl * Mathf.Min(1f, (dx * dx + dz * dz) / (r * r)); }
            float ax = b.x - a.x, az = b.y - a.y; var len2 = ax * ax + az * az; if (len2 < 1e-6f) return ya;
            var t = Mathf.Clamp01(((x - a.x) * ax + (z - a.y) * az) / len2); var y = ya + (yb - ya) * t;
            if (tilt != 0f) { var len = Mathf.Sqrt(len2); y += tilt * ((x - a.x) * az - (z - a.y) * ax) / len; }
            return y;
        }
    }

    public class GolfWall
    {
        public Vector2 a, b;             // in the hole's metres, or the mover's own if it rides one
        public float ya, yb, h = GolfHole.Kerb;   // the ground under each end and how tall it stands
        public float bounce = .68f; public int mover = -1;
        public string look = "kerb";     // for GolfBuild: kerb, rail, glass, hull, blade, none (unseen)
        public float Bottom => Mathf.Min(ya, yb); public float Top => Mathf.Max(ya, yb) + h;
    }

    public class GolfMover
    {
        public enum Kind { Spin, Slide, Lift, Swing }
        public Kind kind; public Vector2 pivot; public float y;
        public float rate;                // Spin: degrees a second. The others: seconds for one cycle.
        public float phase;               // where in its cycle it starts, 0..1
        public Vector2 travel;            // Slide: how far it goes, there and back
        public float lift = 1.3f, duty = .5f;   // Lift: how high it rises, and how much of the cycle it stays up
        public float swing = 60f;         // Swing: degrees each way
        public string look = "";          // for GolfBuild
        // a mover worked by hand instead of by the clock (a pinball flipper): whoever owns it sets its angle each step
        public bool manual; public float manualAngle, manualPrev;

        public void Pose(double t, out Vector2 offset, out float angle, out float up)
        {
            offset = Vector2.zero; angle = 0f; up = 0f;
            if (manual) { angle = manualAngle; return; }
            switch (kind)
            {
                case Kind.Spin: angle = (float)((rate * t + phase * 360.0) % 360.0); break;
                case Kind.Slide: { var u = Frac(t / rate + phase); offset = travel * (.5f - .5f * Mathf.Cos(u * Mathf.PI * 2f)); break; }
                case Kind.Swing: { var u = Frac(t / rate + phase); angle = swing * Mathf.Sin(u * Mathf.PI * 2f); break; }
                case Kind.Lift:
                {
                    var u = Frac(t / rate + phase); const float ease = .1f;
                    var f = u < ease ? u / ease : u < duty ? 1f : u < duty + ease ? 1f - (u - duty) / ease : 0f;
                    up = lift * f * f * (3f - 2f * f); break;
                }
            }
        }
        static float Frac(double v) => (float)(v - Math.Floor(v));

        /// A point given in the mover's own metres, where it is now.
        public static Vector2 Place(Vector2 pivot, Vector2 offset, float angle, Vector2 local)
        {
            var r = angle * Mathf.Deg2Rad; float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            return new Vector2(pivot.x + offset.x + local.x * cs - local.y * sn, pivot.y + offset.y + local.x * sn + local.y * cs);
        }
    }

    public class GolfBumper
    {
        public Vector2 c; public float r, y, h = .6f, bounce = .75f, kick; public int mover = -1; public string look = "post";
    }

    public enum GolfZoneKind { Water, Boost, Current, Fan, Spring, Pipe }

    public class GolfZone
    {
        public GolfZoneKind kind; public GolfPad shape;   // where, and the floor it lies on (a pad shape: its outline and heights)
        public float reach = 1.2f;                        // how far above its floor it still works
        public Vector2 dir; public float power;           // Boost and Fan: the push, metres a second squared. Current: its speed. Spring: the upward speed.
        public Vector3 exit; public Vector2 exitDir; public float exitSpeed;   // Pipe: where and how the ball comes out
        public float period, duty = .5f, phase;           // Fan: seconds on and off (0 = always on)
        public bool On(double t) { if (period <= 0f) return true; var u = t / period + phase; return u - Math.Floor(u) < duty; }
    }

    public class GolfProp { public string kind; public Vector3 at; public float yaw, scale = 1f; public int mover = -1; }

    public class GolfHole
    {
        public const float Kerb = .38f, BallR = .15f, CupR = .34f, Sea = 0f;

        public string name = ""; public int par = 3; public string blurb = "";
        public string floorLook = "";   // for GolfBuild: "night" draws every floor dark (the pinball table), whatever it is made of
        public Vector3 tee, cup;
        public readonly List<GolfPad> pads = new List<GolfPad>();
        public readonly List<GolfWall> walls = new List<GolfWall>();
        public readonly List<GolfMover> movers = new List<GolfMover>();
        public readonly List<GolfBumper> bumpers = new List<GolfBumper>();
        public readonly List<GolfZone> zones = new List<GolfZone>();
        public readonly List<GolfProp> props = new List<GolfProp>();
        public readonly List<Vector2> route = new List<Vector2>();   // the way from tee to cup, for the camera and the checks
        public float minX, maxX, minZ, maxZ, maxY;

        GolfSurface surf = GolfSurface.Green; int onMover = -1;

        // ---------------------------------------------------------------- the floor
        /// The highest floor at this spot that a ball at height y could be on (or step up onto).
        public bool Ground(float x, float z, float y, out float g, out GolfPad pad)
        {
            g = float.MinValue; pad = null;
            for (var i = 0; i < pads.Count; i++)
            {
                var p = pads[i]; if (!p.Contains(x, z)) continue;
                var h = p.Height(x, z); if (h <= y + .16f && h >= g) { g = h; pad = p; }   // (a later pad wins a tie: overlays go on top)
            }
            return pad != null;
        }
        public float TopAt(float x, float z) => Ground(x, z, 1e6f, out var g, out _) ? g : Sea;

        // ---------------------------------------------------------------- building a hole
        public GolfHole Named(string n, int p, string about = "") { name = n; par = p; blurb = about; return this; }
        public GolfHole On(GolfSurface s) { surf = s; return this; }

        /// A strip of floor along points given as x, z, height triples: 'half' wide to each side, mitred at bends.
        /// 'kerbs': L and R for a kerb down each side, S and E to close the start and the end.
        public GolfHole Path(float half, string kerbs, params float[] xzy)
        {
            var n = xzy.Length / 3; var p = new Vector2[n]; var hy = new float[n]; var l = new Vector2[n]; var r = new Vector2[n];
            for (var i = 0; i < n; i++) { p[i] = new Vector2(xzy[i * 3], xzy[i * 3 + 1]); hy[i] = xzy[i * 3 + 2]; }
            for (var i = 0; i < n; i++)
            {
                var d0 = i > 0 ? (p[i] - p[i - 1]).normalized : (p[1] - p[0]).normalized; var d1 = i < n - 1 ? (p[i + 1] - p[i]).normalized : d0;
                var n0 = new Vector2(-d0.y, d0.x); var n1 = new Vector2(-d1.y, d1.x); var m = (n0 + n1).normalized; var k = half / Mathf.Max(.35f, Vector2.Dot(m, n0));
                l[i] = p[i] + m * k; r[i] = p[i] - m * k;
            }
            for (var i = 0; i < n - 1; i++)
            {
                var pad = new GolfPad { poly = new[] { l[i], r[i], r[i + 1], l[i + 1] }, a = p[i], b = p[i + 1], ya = hy[i], yb = hy[i + 1], surface = surf }; pad.Bake(); pads.Add(pad);
                if (kerbs.Contains("L")) walls.Add(new GolfWall { a = l[i], b = l[i + 1], ya = hy[i], yb = hy[i + 1] });
                if (kerbs.Contains("R")) walls.Add(new GolfWall { a = r[i], b = r[i + 1], ya = hy[i], yb = hy[i + 1] });
            }
            if (kerbs.Contains("S")) walls.Add(new GolfWall { a = l[0], b = r[0], ya = hy[0], yb = hy[0] });
            if (kerbs.Contains("E")) walls.Add(new GolfWall { a = l[n - 1], b = r[n - 1], ya = hy[n - 1], yb = hy[n - 1] });
            return this;
        }

        /// A round piece of floor: 'bowl' raises its rim (a funnel) or, negative, lowers it (a dome).
        public GolfHole Disc(float x, float z, float r, float y, float bowl = 0f, bool kerb = true, float kerbH = Kerb)
        {
            var pad = new GolfPad { c = new Vector2(x, z), r = r, ya = y, bowl = bowl, surface = surf, overlay = !kerb }; pad.Bake(); pads.Add(pad);
            if (kerb) Ring(x, z, r, y + bowl, kerbH);
            return this;
        }

        /// A ring of kerb (or anything else: 'look').
        public GolfHole Ring(float x, float z, float r, float y, float h = Kerb, string look = "kerb", float from = 0f, float to = 360f)
        {
            var n = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(to - from) / 15f)); var c = new Vector2(x, z);
            for (var i = 0; i < n; i++)
            {
                float a0 = Mathf.Lerp(from, to, i / (float)n) * Mathf.Deg2Rad, a1 = Mathf.Lerp(from, to, (i + 1f) / n) * Mathf.Deg2Rad;
                walls.Add(new GolfWall { a = c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, b = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r, ya = y, yb = y, h = h, look = look, mover = onMover });
            }
            return this;
        }

        /// One wall. Its ends stand on the floor found there unless 'y' is given.
        public GolfHole Wall(float ax, float az, float bx, float bz, float h = Kerb, string look = "kerb", float y = float.NaN, float bounce = .68f)
        {
            var w = new GolfWall { a = new Vector2(ax, az), b = new Vector2(bx, bz), h = h, look = look, bounce = bounce, mover = onMover };
            if (onMover >= 0) { w.ya = w.yb = float.IsNaN(y) ? movers[onMover].y : y; }
            else { w.ya = float.IsNaN(y) ? TopAt(ax, az) : y; w.yb = float.IsNaN(y) ? TopAt(bx, bz) : y; }
            walls.Add(w); return this;
        }

        /// Opens a gap in the fixed walls: everything within 'r' of the point is taken away (only walls standing at
        /// 'above' or higher, if given: for a gap on an upper level).
        public GolfHole Cut(float x, float z, float r, float above = float.MinValue)
        {
            var c = new Vector2(x, z);
            for (var i = walls.Count - 1; i >= 0; i--)
            {
                var w = walls[i]; if (w.mover >= 0 || w.Bottom < above) continue;
                var d = w.b - w.a; var len = d.magnitude; if (len < 1e-4f) continue; d /= len;
                var along = Vector2.Dot(c - w.a, d); var off = (w.a + d * along - c).magnitude; if (off >= r) continue;
                var half = Mathf.Sqrt(r * r - off * off); float t0 = along - half, t1 = along + half; if (t1 <= 0f || t0 >= len) continue;
                walls.RemoveAt(i);
                if (t0 > .05f) walls.Insert(i, new GolfWall { a = w.a, b = w.a + d * t0, ya = w.ya, yb = Mathf.Lerp(w.ya, w.yb, t0 / len), h = w.h, look = w.look, bounce = w.bounce });
                if (t1 < len - .05f) walls.Insert(i, new GolfWall { a = w.a + d * t1, b = w.b, ya = Mathf.Lerp(w.ya, w.yb, t1 / len), yb = w.yb, h = w.h, look = w.look, bounce = w.bounce });
            }
            return this;
        }

        public GolfHole Tee(float x, float z) { tee = new Vector3(x, TopAt(x, z), z); return this; }
        public GolfHole Cup(float x, float z) { cup = new Vector3(x, TopAt(x, z), z); return this; }
        public GolfHole Route(params float[] xz) { route.Clear(); for (var i = 0; i + 1 < xz.Length; i += 2) route.Add(new Vector2(xz[i], xz[i + 1])); return this; }

        public GolfHole Post(float x, float z, float r = .3f, float h = .7f, string look = "post")
        { bumpers.Add(new GolfBumper { c = new Vector2(x, z), r = r, y = onMover >= 0 ? movers[onMover].y : TopAt(x, z), h = h, look = look, mover = onMover }); return this; }

        /// A round bumper that kicks the ball away.
        public GolfHole Bumper(float x, float z, float r = .55f, float kick = 7f, string look = "bumper")
        { bumpers.Add(new GolfBumper { c = new Vector2(x, z), r = r, y = onMover >= 0 ? movers[onMover].y : TopAt(x, z), h = .7f, bounce = .9f, kick = kick, look = look, mover = onMover }); return this; }

        // ---- movers: Wall, Ring, Post, Bumper and Prop calls after one of these ride it (in its own metres) until Still()
        GolfHole Mover(GolfMover m) { m.y = TopAt(m.pivot.x, m.pivot.y); movers.Add(m); onMover = movers.Count - 1; return this; }
        public GolfHole Spinning(float x, float z, float degPerSecond, float phase = 0f, string look = "") => Mover(new GolfMover { kind = GolfMover.Kind.Spin, pivot = new Vector2(x, z), rate = degPerSecond, phase = phase, look = look });
        public GolfHole Sliding(float x, float z, float dx, float dz, float seconds, float phase = 0f, string look = "") => Mover(new GolfMover { kind = GolfMover.Kind.Slide, pivot = new Vector2(x, z), travel = new Vector2(dx, dz), rate = seconds, phase = phase, look = look });
        public GolfHole Lifting(float x, float z, float seconds, float duty = .5f, float phase = 0f, float lift = 1.3f, string look = "") => Mover(new GolfMover { kind = GolfMover.Kind.Lift, pivot = new Vector2(x, z), rate = seconds, duty = duty, phase = phase, lift = lift, look = look });
        public GolfHole Swinging(float x, float z, float degrees, float seconds, float phase = 0f, string look = "") => Mover(new GolfMover { kind = GolfMover.Kind.Swing, pivot = new Vector2(x, z), swing = degrees, rate = seconds, phase = phase, look = look });
        public GolfHole Still() { onMover = -1; return this; }
        /// The mover's height (it stands on the floor found at its pivot unless told otherwise).
        public GolfHole At(float y) { if (onMover >= 0) movers[onMover].y = y; return this; }

        /// A turning set of arms (a propeller, a ceiling fan, a revolving door): 'arms' blades 'len' long.
        public GolfHole Spinner(float x, float z, float len, int arms, float degPerSecond, float phase = 0f, string look = "propeller", float h = .5f)
        {
            Spinning(x, z, degPerSecond, phase, look);
            for (var k = 0; k < arms; k++) { var a = k * Mathf.PI * 2f / arms; Wall(Mathf.Cos(a) * .25f, Mathf.Sin(a) * .25f, Mathf.Cos(a) * len, Mathf.Sin(a) * len, h, "blade"); }
            Post(0, 0, .28f, h + .25f, "hub");
            return Still();
        }

        /// A box that slides to and fro (a rowing boat, a drinks trolley): 'w' across by 'd' along its own z.
        public GolfHole Slider(float x, float z, float dx, float dz, float w, float d, float seconds, float phase = 0f, string look = "boat", float h = .6f)
        {
            Sliding(x, z, dx, dz, seconds, phase, look);
            Wall(-w / 2, -d / 2, w / 2, -d / 2, h, "none"); Wall(w / 2, -d / 2, w / 2, d / 2, h, "none"); Wall(w / 2, d / 2, -w / 2, d / 2, h, "none"); Wall(-w / 2, d / 2, -w / 2, -d / 2, h, "none");
            return Still();
        }

        /// A gate across the way that lifts clear for part of each cycle.
        public GolfHole Gate(float ax, float az, float bx, float bz, float seconds, float duty = .5f, float phase = 0f, string look = "gate")
        {
            float mx = (ax + bx) / 2f, mz = (az + bz) / 2f; Lifting(mx, mz, seconds, duty, phase, 1.3f, look);
            Wall(ax - mx, az - mz, bx - mx, bz - mz, .8f, "gate"); return Still();
        }

        /// A boom that swings to and fro about one end.
        public GolfHole Boom(float x, float z, float len, float restDeg, float swingDeg, float seconds, float phase = 0f, string look = "boom")
        {
            Swinging(x, z, swingDeg, seconds, phase, look); var a = restDeg * Mathf.Deg2Rad;
            Wall(Mathf.Cos(a) * .2f, Mathf.Sin(a) * .2f, Mathf.Cos(a) * len, Mathf.Sin(a) * len, .5f, "blade"); Post(0, 0, .25f, 1.1f, "hub"); return Still();
        }

        /// A flipper: an arm 'len' long on a pivot, worked by hand (it rests at restDeg; its owner swings it).
        public GolfHole Flipper(float x, float z, float len, float restDeg)
        {
            Mover(new GolfMover { kind = GolfMover.Kind.Swing, pivot = new Vector2(x, z), manual = true, manualAngle = restDeg, manualPrev = restDeg, look = "flipper" });
            Wall(.2f, 0, len, 0, .5f, "blade", float.NaN, .5f); Post(0, 0, .3f, .7f, "hub"); return Still();
        }

        // ---- zones
        GolfPad Strip(float ax, float az, float bx, float bz, float half)
        {
            var a = new Vector2(ax, az); var b = new Vector2(bx, bz); var d = (b - a).normalized; var n = new Vector2(-d.y, d.x) * half;
            var p = new GolfPad { poly = new[] { a + n, a - n, b - n, b + n }, a = a, b = b, ya = TopAt(ax, az), yb = TopAt(bx, bz) }; p.Bake(); return p;
        }
        GolfPad Round(float x, float z, float r) { var p = new GolfPad { c = new Vector2(x, z), r = r, ya = TopAt(x, z) }; p.Bake(); return p; }
        GolfHole Zone(GolfZone z, float x, float zz) { zones.Add(z); return this; }

        public GolfHole Pond(float x, float z, float r) => Zone(new GolfZone { kind = GolfZoneKind.Water, shape = Round(x, z, r), reach = .4f }, x, z);
        public GolfHole PondStrip(float ax, float az, float bx, float bz, float half) => Zone(new GolfZone { kind = GolfZoneKind.Water, shape = Strip(ax, az, bx, bz, half), reach = .4f }, (ax + bx) / 2, (az + bz) / 2);
        /// Arrows on the floor that speed the ball along a to b.
        public GolfHole Boost(float ax, float az, float bx, float bz, float half, float power = 16f)
            => Zone(new GolfZone { kind = GolfZoneKind.Boost, shape = Strip(ax, az, bx, bz, half), dir = new Vector2(bx - ax, bz - az).normalized, power = power, reach = .4f }, (ax + bx) / 2, (az + bz) / 2);
        /// Moving water (or a conveyor) that carries the ball along a to b at 'speed'.
        public GolfHole Current(float ax, float az, float bx, float bz, float half, float speed = 3.5f)
            => Zone(new GolfZone { kind = GolfZoneKind.Current, shape = Strip(ax, az, bx, bz, half), dir = new Vector2(bx - ax, bz - az).normalized, power = speed, reach = .4f }, (ax + bx) / 2, (az + bz) / 2);
        /// A fan blowing across a to b, on and off.
        public GolfHole Fan(float ax, float az, float bx, float bz, float half, float power = 14f, float period = 0f, float duty = .5f, float phase = 0f)
            => Zone(new GolfZone { kind = GolfZoneKind.Fan, shape = Strip(ax, az, bx, bz, half), dir = new Vector2(bx - ax, bz - az).normalized, power = power, period = period, duty = duty, phase = phase, reach = 4f }, (ax + bx) / 2, (az + bz) / 2);
        /// A springboard that throws the ball up.
        public GolfHole Spring(float x, float z, float r = .7f, float up = 9f) => Zone(new GolfZone { kind = GolfZoneKind.Spring, shape = Round(x, z, r), power = up, reach = .3f }, x, z);
        /// A pipe: in at one place, out at another, heading 'outDeg' (0 = along x, 90 = ahead) at 'speed'.
        public GolfHole Pipe(float x, float z, float ox, float oz, float outDeg, float speed = 5f, float r = .45f)
        {
            var a = outDeg * Mathf.Deg2Rad;
            return Zone(new GolfZone { kind = GolfZoneKind.Pipe, shape = Round(x, z, r), exit = new Vector3(ox, TopAt(ox, oz), oz), exitDir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), exitSpeed = speed, reach = .3f }, x, z);
        }

        /// Scenery (GolfBuild knows the kinds). It stands on the floor there, or on the sea, unless 'y' is given.
        public GolfHole Prop(string kind, float x, float z, float yaw = 0f, float scale = 1f, float y = float.NaN)
        { props.Add(new GolfProp { kind = kind, at = new Vector3(x, float.IsNaN(y) ? (onMover >= 0 ? movers[onMover].y : TopAt(x, z)) : y, z), yaw = yaw, scale = scale, mover = onMover }); return this; }

        public GolfHole Done()
        {
            minX = minZ = float.MaxValue; maxX = maxZ = maxY = float.MinValue;
            foreach (var p in pads)
            {
                p.Bake(); minX = Mathf.Min(minX, p.minX); maxX = Mathf.Max(maxX, p.maxX); minZ = Mathf.Min(minZ, p.minZ); maxZ = Mathf.Max(maxZ, p.maxZ);
                maxY = Mathf.Max(maxY, Mathf.Max(p.ya, p.yb) + Mathf.Max(0f, p.bowl));
            }
            if (route.Count == 0) { route.Add(new Vector2(tee.x, tee.z)); route.Add(new Vector2(cup.x, cup.z)); }
            tee.y = TopAt(tee.x, tee.z); cup.y = TopAt(cup.x, cup.z);
            return this;
        }

        /// How far along the route a spot is (metres from the tee), for the checks and for "who is ahead".
        public float Progress(float x, float z)
        {
            var best = float.MaxValue; var at = 0f; var run = 0f; var p = new Vector2(x, z);
            for (var i = 0; i + 1 < route.Count; i++)
            {
                var d = route[i + 1] - route[i]; var len = d.magnitude; var t = len < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(p - route[i], d) / (len * len));
                var off = (route[i] + d * t - p).sqrMagnitude; if (off < best) { best = off; at = run + len * t; }
                run += len;
            }
            return at - Mathf.Sqrt(best) * .5f;
        }
    }
}
