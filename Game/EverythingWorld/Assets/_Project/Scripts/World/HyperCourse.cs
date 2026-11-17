using System.Collections.Generic;
using EverythingWorld.Player;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The moving parts of a land you cross at speed (the Kawaii Cascades): pads that throw you when you touch them
    /// (jelly bounce pads, soda geysers, boost strips) and rails you ride. One of these per land keeps the lists and
    /// watches the player, so the pieces themselves are only meshes.
    /// - A pad throws you when your feet come down on it (PlayerController.Launch). Its jelly squashes and springs back.
    /// - A rail carries you from wherever you touch it to its lower end, faster and faster; Space jumps off, and the end
    ///   throws you on the way you were going. You can look around while you ride.
    public class HyperCourse : MonoBehaviour
    {
        public enum Kind { Bounce, Geyser, Boost }
        public class Pad { public Kind kind; public Vector3 at; public float radius; public Vector3 velocity; public Transform visual; public float hit = -9f; }
        public class Rail { public Vector3[] pts; public float[] along; public float length; public float speed = 24f; }

        public readonly List<Pad> pads = new List<Pad>();
        public readonly List<Rail> rails = new List<Rail>();
        /// Called when a pad throws the player or a rail is mounted (for a puff of sprinkles, a sound later).
        public System.Action<Vector3, Kind> onPad; public System.Action<Vector3> onRail;
        /// False while the land is not the one you are on: nothing is checked.
        public bool live = true;

        SimplePlayerController player; CharacterController body;
        Rail riding; float rideAt, rideSpeed, railCooldown;
        public bool Riding => riding != null;
        public float RideSpeed => riding != null ? rideSpeed : 0f;

        public Pad AddPad(Kind kind, Vector3 at, float radius, Vector3 velocity, Transform visual = null)
        {
            var p = new Pad { kind = kind, at = at, radius = radius, velocity = velocity, visual = visual }; pads.Add(p); return p;
        }

        public Rail AddRail(IList<Vector3> points, float speed = 24f)
        {
            var r = new Rail { pts = new Vector3[points.Count], along = new float[points.Count], speed = speed };
            for (var i = 0; i < points.Count; i++) { r.pts[i] = points[i]; if (i > 0) r.length += Vector3.Distance(points[i - 1], points[i]); r.along[i] = r.length; }
            rails.Add(r); return r;
        }

        static Vector3 PointAt(Rail r, float d, out Vector3 tangent)
        {
            d = Mathf.Clamp(d, 0f, r.length);
            var i = 1; while (i < r.pts.Length - 1 && r.along[i] < d) i++;
            var seg = r.along[i] - r.along[i - 1]; var t = seg > .001f ? (d - r.along[i - 1]) / seg : 0f;
            tangent = (r.pts[i] - r.pts[i - 1]).normalized; return Vector3.Lerp(r.pts[i - 1], r.pts[i], t);
        }

        void Update()
        {
            var t = Time.time;
            // the jelly springs back after a bounce
            foreach (var p in pads)
            {
                if (p.visual == null) continue; var since = t - p.hit;
                var s = since < .6f ? 1f - Mathf.Exp(-since * 7f) * Mathf.Cos(since * 26f) * .55f : 1f;
                p.visual.localScale = new Vector3(2f - s, s, 2f - s) * 1f;
            }
            if (player == null) { player = FindAnyObjectByType<SimplePlayerController>(); if (player != null) body = player.GetComponent<CharacterController>(); }
            if (player == null || body == null) return;
            if (riding != null) { Ride(); return; }
            if (!live || player.Flying || player.Seated || player.Controlled) return;
            var half = body.height / 2f; var feet = player.transform.position - Vector3.up * half;
            foreach (var p in pads)
            {
                if (t - p.hit < .35f) continue;
                float dx = feet.x - p.at.x, dz = feet.z - p.at.z; if (dx * dx + dz * dz > p.radius * p.radius) continue;
                var dy = feet.y - p.at.y; if (dy < -.6f || dy > (p.kind == Kind.Geyser ? 3f : .9f)) continue;
                p.hit = t;
                var v = p.velocity;
                // a boost strip throws you the way you are facing, so a road of them works in both directions
                if (p.kind == Kind.Boost) { var f = player.transform.forward; f.y = 0; v = f.normalized * new Vector3(v.x, 0, v.z).magnitude + Vector3.up * v.y; }
                player.Launch(v); onPad?.Invoke(p.at, p.kind);
                break;
            }
            if (t < railCooldown) return;
            // touching a rail anywhere along it (from above or beside) puts you on it
            foreach (var r in rails)
            {
                for (var i = 1; i < r.pts.Length; i++)
                {
                    Vector3 a = r.pts[i - 1], b = r.pts[i]; var ab = b - a; var u = Mathf.Clamp01(Vector3.Dot(feet - a, ab) / ab.sqrMagnitude); var q = a + ab * u;
                    if (Mathf.Abs(feet.y - q.y) > 1.3f) continue; float qx = feet.x - q.x, qz = feet.z - q.z; if (qx * qx + qz * qz > 1.5f * 1.5f) continue;
                    var d = r.along[i - 1] + ab.magnitude * u; if (d > r.length - 3f) continue;
                    riding = r; rideAt = d; rideSpeed = Mathf.Max(player.GroundSpeed, 12f); player.Take(); player.LookWhileControlled = true; onRail?.Invoke(q);
                    return;
                }
            }
        }

        void Ride()
        {
            var dt = Time.deltaTime; var kb = UnityEngine.InputSystem.Keyboard.current;
            rideSpeed = Mathf.MoveTowards(rideSpeed, riding.speed * 1.5f, dt * 9f);
            rideAt += rideSpeed * dt;
            var pos = PointAt(riding, rideAt, out var tan);
            player.transform.position = pos + Vector3.up * (body.height / 2f + .35f);
            var off = kb != null && kb.spaceKey.wasPressedThisFrame; var end = rideAt >= riding.length;
            if (!off && !end) return;
            var flat = new Vector3(tan.x, 0, tan.z).normalized; var speed = rideSpeed;
            riding = null; railCooldown = Time.time + .8f;
            player.LetGo();
            player.Launch(flat * speed * (end ? .75f : .9f) + Vector3.up * (end ? 9f : 13f));
        }

        void OnDisable() { if (riding != null && player != null) { riding = null; player.LetGo(); } }
    }
}
