using System.Collections.Generic;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.Activities
{
    /// Keeps a built course's moving parts where the shared clock says they are (the big course and the little model
    /// of it both carry one, so the model's propellers turn in step with the real ones).
    public class GolfCourseView : MonoBehaviour
    {
        public string courseId;
        public List<Transform> movers = new List<Transform>(); public List<int> moverHole = new List<int>(), moverIndex = new List<int>();
        public List<Transform> rotors = new List<Transform>();   // scenery that just turns (fan blades)
        public List<Transform> holes = new List<Transform>();
        GolfCourse course;

        void LateUpdate()
        {
            course ??= GolfCourses.ById(courseId); if (course == null) return;
            var t = SharedChance.Now;
            for (var i = 0; i < movers.Count; i++)
            {
                if (movers[i] == null) continue; var m = course.holes[moverHole[i]].movers[moverIndex[i]];
                m.Pose(t, out var off, out var ang, out var up);
                movers[i].localPosition = new Vector3(m.pivot.x + off.x, m.y + up, m.pivot.y + off.y); movers[i].localRotation = Quaternion.Euler(0, -ang, 0);
            }
            var spin = (float)(t * 300.0 % 360.0);
            foreach (var r in rotors) if (r != null) r.localRotation = Quaternion.Euler(0, 0, spin);
        }
    }

    /// A mark on the big course: LandCuller leaves everything under a "...Life" script alone, and it must leave this
    /// alone, because the course is switched off when the land is built (so it has no size or place to judge it by)
    /// and is only ever switched on with the player standing in it.
    public class GolfSkyLife : MonoBehaviour { }

    /// Draws a course from its description: every hole's floor, kerbs, obstacles, cup and flag, tee, sign and scenery,
    /// on a wide calm sea. Everything is built at the origin under one root, which the caller then puts where it
    /// likes (and copies, shrunk, for the model).
    public static class GolfBuild
    {
        static Material green, teak, stone, sand, ice, white, navy, red, black, brass, glass, pool, river, arrow, bark, leaf, pink, cloth1, cloth2, warm, sunM, rock, cup, gold, springM, hullM;
        static readonly Color Navy = new Color(.1f, .16f, .38f);

        static void Materials()
        {
            // bright greens, dark decks, pale stone: told apart by brightness as much as by colour
            green = WorldMaterials.Surface(new Color(.42f, .86f, .5f), Surface.Cloth); teak = WorldMaterials.Surface(new Color(.5f, .33f, .2f), Surface.Wood);
            stone = WorldMaterials.Surface(new Color(.9f, .87f, .8f), Surface.Stone); sand = WorldMaterials.Surface(new Color(.97f, .86f, .58f), Surface.Stone);
            ice = new Material(WorldMaterials.Glow(new Color(.72f, .92f, 1f), .35f)) { name = "Golf ice" }; ice.SetFloat("_Smoothness", .95f);
            white = WorldMaterials.Surface(new Color(.96f, .95f, .92f), Surface.Painted); navy = WorldMaterials.Surface(Navy, Surface.Painted); red = WorldMaterials.Surface(new Color(.85f, .16f, .14f), Surface.Painted);
            black = WorldMaterials.Solid(new Color(.05f, .05f, .06f), .7f); brass = WorldMaterials.Surface(new Color(.9f, .7f, .25f), Surface.Metal);
            glass = Clear("Golf glass", new Color(.7f, .9f, 1f, .28f)); pool = new Material(WorldMaterials.Glow(new Color(.2f, .78f, .92f), .75f)) { name = "Golf pool" }; pool.SetFloat("_Smoothness", .9f);
            river = new Material(WorldMaterials.Glow(new Color(.18f, .5f, .9f), .55f)) { name = "Golf river" }; river.SetFloat("_Smoothness", .9f);
            arrow = WorldMaterials.Glow(new Color(1f, .9f, .3f), 2.2f); bark = WorldMaterials.Surface(new Color(.55f, .45f, .35f), Surface.Bark); leaf = WorldMaterials.Surface(new Color(.22f, .5f, .3f), Surface.Foliage);
            pink = WorldMaterials.Surface(new Color(1f, .5f, .65f), Surface.Painted); cloth1 = WorldMaterials.Surface(new Color(1f, .6f, .5f), Surface.Cloth); cloth2 = WorldMaterials.Surface(new Color(.45f, .7f, .92f), Surface.Cloth);
            warm = WorldMaterials.Glow(new Color(1f, .82f, .5f), 2.4f); sunM = WorldMaterials.Glow(new Color(1f, .6f, .2f), 3f); rock = WorldMaterials.Surface(new Color(.5f, .5f, .52f), Surface.Stone);
            cup = WorldMaterials.Solid(new Color(.02f, .02f, .03f)); gold = WorldMaterials.Glow(new Color(1f, .8f, .3f), 1.8f); springM = WorldMaterials.Glow(new Color(1f, .3f, .3f), 1.5f); hullM = WorldMaterials.Surface(new Color(.94f, .94f, .96f), Surface.Painted);
        }

        public static Material Clear(string name, Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", .92f); return m;
        }

        static Material Floor(GolfSurface s) => s == GolfSurface.Deck ? teak : s == GolfSurface.Stone ? stone : s == GolfSurface.Sand ? sand : s == GolfSurface.Ice ? ice : green;
        static readonly Vector3 up = Vector3.up;
        static Vector3 V(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        public static GameObject Build(GolfCourse c)
        {
            Materials();
            var root = new GameObject("Golf course: " + c.name); var view = root.AddComponent<GolfCourseView>(); view.courseId = c.id;
            var rng = new System.Random(1978);
            for (var i = 0; i < c.holes.Length; i++)
            {
                var hole = c.holes[i]; var ht = new GameObject($"Hole {i + 1}: {hole.name}").transform; ht.SetParent(root.transform, false);
                ht.localPosition = c.origins[i]; ht.localRotation = Quaternion.Euler(0, c.yaws[i], 0); view.holes.Add(ht);
                var mk = new MeshKit(); Hole(mk, hole, i, ht, view, rng); mk.BuildSingle("Hole", ht, false);
                for (var m = 0; m < hole.movers.Count; m++)
                {
                    var mm = new MeshKit(); Mover(mm, hole, m, rng); var go = mm.BuildSingle("Mover: " + hole.movers[m].look, ht, false); if (go == null) { go = new GameObject("Mover"); go.transform.SetParent(ht, false); }
                    go.transform.localPosition = new Vector3(hole.movers[m].pivot.x, hole.movers[m].y, hole.movers[m].pivot.y);
                    view.movers.Add(go.transform); view.moverHole.Add(i); view.moverIndex.Add(m);
                }
            }
            Sea(root.transform, Mathf.Max(c.width, c.depth) * .5f + 520f);
            return root;
        }

        // ---------------------------------------------------------------- the sea the course stands in
        static void Sea(Transform root, float radius)
        {
            var water = WorldMaterials.Water("Links sea", new Color(.25f, .8f, .85f), new Color(.06f, .32f, .55f), 2.6f, .35f, .5f);
            var floor = WorldMaterials.Surface(new Color(.5f, .82f, .86f), Surface.Painted);
            var w = new MeshKit(); var f = new MeshKit(); const int N = 48;
            for (var k = 0; k < N; k++)
            {
                float a0 = k * Mathf.PI * 2f / N, a1 = (k + 1) * Mathf.PI * 2f / N; var p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius; var p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
                w.Tri(water, Vector3.zero, p0, p1, Vector3.down * 5f); f.Tri(floor, Vector3.down * 2.6f, p0 + Vector3.down * 2.6f, p1 + Vector3.down * 2.6f, Vector3.down * 9f);
            }
            var wg = w.BuildSingle("Sea", root, false); if (wg != null) { var r = wg.GetComponent<MeshRenderer>(); r.shadowCastingMode = ShadowCastingMode.Off; }
            var fg = f.BuildSingle("Sea floor", root, false); if (fg != null) fg.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;   // (no vast shadow on the land far below)
        }

        // ---------------------------------------------------------------- one hole
        static void Hole(MeshKit mk, GolfHole h, int index, Transform ht, GolfCourseView view, System.Random rng)
        {
            for (var i = 0; i < h.pads.Count; i++) Pad(mk, h.pads[i], i);
            foreach (var w in h.walls) if (w.mover < 0) Wall(mk, w, 0f);
            foreach (var b in h.bumpers) if (b.mover < 0) Bumper(mk, b, 0f, rng, ht);
            foreach (var z in h.zones) Zone(mk, z);
            // a lifting gate's two posts stay put, a lamp on each
            for (var m = 0; m < h.movers.Count; m++)
                if (h.movers[m].look == "gate")
                    foreach (var w in h.walls) if (w.mover == m) foreach (var e in new[] { w.a, w.b })
                    {
                        var q = V(h.movers[m].pivot + e + e.normalized * .3f, h.movers[m].y); mk.Prism(white, q, up, .13f, h.movers[m].lift + 1.1f, 6); mk.Blob(warm, q + up * (h.movers[m].lift + 1.25f), Vector3.one * .16f, rng, 0f);
                    }
            foreach (var p in h.props) if (p.mover < 0) Prop(mk, p, rng, ht, view);

            // the cup, its flag and a light over the green
            var c = h.cup; mk.Prism(cup, c + up * .012f, up, GolfHole.CupR, .012f, 16); mk.Prism(gold, c + up * .006f, up, GolfHole.CupR + .07f, .012f, 16);
            mk.Prism(white, c, up, .035f, 2.6f, 6); mk.Tri(red, c + up * 2.6f, c + up * 2f, c + up * 2.3f + new Vector3(.95f, 0, 0), c + up * 2.3f - Vector3.forward); mk.Tri(red, c + up * 2.6f, c + up * 2f, c + up * 2.3f + new Vector3(.95f, 0, 0), c + up * 2.3f + Vector3.forward);
            mk.Blob(gold, c + up * 2.68f, Vector3.one * .08f, rng, 0f);
            Light(ht, c + up * 3.2f, new Color(1f, .85f, .6f), 15f, 1.5f);
            // the tee: a navy mat with two markers, and the hole's board
            var t = h.tee; mk.Box(navy, t + up * .012f, new Vector3(1.5f, .02f, 1.5f)); foreach (var e in new[] { -1f, 1f }) mk.Blob(white, t + new Vector3(e * .6f, .1f, 0), Vector3.one * .09f, rng, 0f);
            Light(ht, t + up * 3f, new Color(1f, .9f, .75f), 11f, 1.1f);
            var ahead = h.route.Count > 1 ? (h.route[1] - h.route[0]).normalized : Vector2.up; var fwd = new Vector3(ahead.x, 0, ahead.y); var left = Vector3.Cross(fwd, up);
            var off = 1f; while (off < 14f && h.TopAt(t.x + left.x * off + fwd.x * 1.5f, t.z + left.z * off + fwd.z * 1.5f) > GolfHole.Sea + .01f) off += .4f;   // out past the edge of the floor, however wide the tee's deck is
            var sp = t + left * (off + 1.5f) + fwd * 1.5f; var board = new Vector3(sp.x, t.y + 1.9f, sp.z); var rot = Quaternion.LookRotation(fwd);
            mk.Prism(white, new Vector3(sp.x, -1.2f, sp.z), up, .09f, t.y + 2.8f, 6); mk.Box(white, board, new Vector3(2.6f, 1.1f, .08f), rot); mk.Box(navy, board + up * .6f, new Vector3(2.7f, .08f, .1f), rot);
            ArtistBits.Label(ht, $"{index + 1}", ht.TransformPoint(board + up * .22f - fwd * .05f), ht.TransformDirection(-fwd), .5f, Navy, TextAnchor.MiddleCenter);
            ArtistBits.Label(ht, $"{h.name}\npar {h.par}", ht.TransformPoint(board - up * .28f - fwd * .05f), ht.TransformDirection(-fwd), .17f, Navy, TextAnchor.MiddleCenter);
        }

        static void Light(Transform parent, Vector3 local, Color c, float range, float intensity)
        {
            var go = new GameObject("Golf light"); go.transform.SetParent(parent, false); go.transform.localPosition = local;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
        }

        static void Pad(MeshKit mk, GolfPad p, int index)
        {
            var m = Floor(p.surface);
            if (p.poly != null)
            {
                var lift = .003f * (index % 3); var n = p.poly.Length; var top = new Vector3[n]; var mid = Vector3.zero;
                for (var i = 0; i < n; i++) { top[i] = V(p.poly[i], p.Height(p.poly[i].x, p.poly[i].y) + lift); mid += top[i] / n; }
                for (var i = 1; i + 1 < n; i++) mk.Tri(m, top[0], top[i], top[i + 1], mid + Vector3.down * 5f);
                for (var i = 0; i < n; i++) { var a = top[i]; var b = top[(i + 1) % n]; mk.Quad(white, a, b, b + Vector3.down * .42f, a + Vector3.down * .42f, mid + Vector3.down * .2f); }
                mk.Tri(white, top[0] + Vector3.down * .42f, top[1] + Vector3.down * .42f, top[2] + Vector3.down * .42f, mid); if (n > 3) mk.Tri(white, top[0] + Vector3.down * .42f, top[2] + Vector3.down * .42f, top[3] + Vector3.down * .42f, mid);
                // piles down into the sea under the start of each strip
                for (var i = 0; i < 2 && i < n; i++) { var q = Vector3.Lerp(top[i], mid, .25f); mk.Prism(teak, new Vector3(q.x, -1.4f, q.z), up, .16f, q.y + 1.4f - .4f, 6); }
                return;
            }
            // a disc: rings from the middle out, following the bowl or dome
            const int N = 28; var overlay = p.overlay; var liftD = overlay ? .014f : .007f; float[] rings = { .34f, .68f, 1f }; var cen = V(p.c, p.ya + liftD);
            Vector3 R(int k, float f) { var a = k * Mathf.PI * 2f / N; var q = p.c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (p.r * f); return V(q, p.Height(q.x, q.y) + liftD); }
            for (var k = 0; k < N; k++)
            {
                mk.Tri(m, cen, R(k, rings[0]), R(k + 1, rings[0]), cen + Vector3.down * 5f);
                for (var j = 0; j + 1 < rings.Length; j++) mk.Quad(m, R(k, rings[j]), R(k + 1, rings[j]), R(k + 1, rings[j + 1]), R(k, rings[j + 1]), cen + Vector3.down * 5f);
                if (!overlay) { var a = R(k, 1f); var b = R(k + 1, 1f); Vector3 Foot(Vector3 q) => new Vector3(p.c.x + (q.x - p.c.x) * 1.05f, -1.4f, p.c.y + (q.z - p.c.y) * 1.05f); mk.Quad(stone, a, b, Foot(b), Foot(a), new Vector3(p.c.x, 0, p.c.y)); }
            }
        }

        /// A wall drawn as a box along its line, standing on the floor at each end.
        static void WallBox(MeshKit mk, Material m, Vector2 a, Vector2 b, float ya, float yb, float lo, float hi, float thick)
        {
            var d = (b - a).normalized; var n = new Vector2(-d.y, d.x) * (thick / 2f);
            Vector3 a0 = V(a + n, ya + lo), a1 = V(a - n, ya + lo), b0 = V(b + n, yb + lo), b1 = V(b - n, yb + lo), a2 = V(a + n, ya + hi), a3 = V(a - n, ya + hi), b2 = V(b + n, yb + hi), b3 = V(b - n, yb + hi);
            var mid = (a0 + b3) / 2f;
            mk.Quad(m, a0, b0, b2, a2, mid); mk.Quad(m, a1, b1, b3, a3, mid); mk.Quad(m, a2, b2, b3, a3, mid); mk.Quad(m, a0, a1, a3, a2, mid); mk.Quad(m, b0, b1, b3, b2, mid); mk.Quad(m, a0, b0, b1, a1, mid);
        }

        static void Wall(MeshKit mk, GolfWall w, float baseY)
        {
            float ya = w.ya - baseY, yb = w.yb - baseY;
            switch (w.look)
            {
                case "none": return;
                case "key": WallBox(mk, black, w.a, w.b, ya, yb, 0f, w.h, .75f); break;
                case "glass": WallBox(mk, glass, w.a, w.b, ya, yb, 0f, w.h, .08f); WallBox(mk, brass, w.a, w.b, ya, yb, w.h, w.h + .06f, .12f); break;
                case "blade": WallBox(mk, red, w.a, Vector2.Lerp(w.a, w.b, .72f), ya, yb, .05f, w.h, .16f); WallBox(mk, white, Vector2.Lerp(w.a, w.b, .72f), w.b, ya, yb, .05f, w.h, .16f); break;
                case "gate":
                    for (var k = 0; k < 6; k++) WallBox(mk, k % 2 == 0 ? red : white, Vector2.Lerp(w.a, w.b, k / 6f), Vector2.Lerp(w.a, w.b, (k + 1) / 6f), ya, yb, .05f, w.h, .2f);
                    break;
                default: WallBox(mk, white, w.a, w.b, ya, yb, -.12f, w.h - .05f, .16f); WallBox(mk, navy, w.a, w.b, ya, yb, w.h - .05f, w.h, .2f); break;
            }
        }

        static readonly Color[] Drinks = { new Color(1f, .45f, .6f), new Color(1f, .7f, .2f), new Color(.3f, .9f, .85f), new Color(.75f, .5f, 1f) };

        static void Bumper(MeshKit mk, GolfBumper b, float baseY, System.Random rng, Transform lights)
        {
            var at = V(b.c, b.y - baseY);
            switch (b.look)
            {
                case "hub": mk.Prism(brass, at, up, b.r, b.h, 10); mk.Blob(gold, at + up * (b.h + .08f), Vector3.one * .12f, rng, 0f); break;
                case "parasol": mk.Prism(white, at, up, b.r * .5f, 2.5f, 6); mk.Prism(white, at, up, b.r, .12f, 8); mk.Cone(rng.Next(2) == 0 ? cloth1 : cloth2, at + up * 2.3f, 1.7f, .6f, 8); break;
                case "lantern": mk.Prism(white, at, up, b.r, .2f, 8); mk.Prism(white, at, up, .1f, b.h, 6); mk.Blob(warm, at + up * (b.h + .2f), Vector3.one * .28f, rng, 0f); if (lights != null) Light(lights, at + up * (b.h + .3f), new Color(1f, .8f, .5f), 9f, 1.4f); break;
                case "rock": mk.Blob(rock, at + up * b.h * .35f, new Vector3(b.r * 1.1f, b.h * .8f, b.r * 1.1f), rng, .22f); break;
                case "icecube": { var m = new Material(WorldMaterials.Glow(new Color(.8f, .95f, 1f), .6f)); mk.Box(m, at + up * b.h / 2f, new Vector3(b.r * 1.5f, b.h, b.r * 1.5f), Quaternion.Euler(0, rng.Next(90), 0)); break; }
                case "glass":
                case "bumper":
                {
                    // a cocktail glass that kicks: a foot the size of what you hit, a stem, a glowing bowl
                    var drink = WorldMaterials.Glow(Drinks[rng.Next(Drinks.Length)], 1.7f);
                    mk.Prism(white, at, up, b.r, .1f, 14); mk.Prism(drink, at + up * .1f, up, b.r * .85f, .04f, 14); mk.Prism(white, at + up * .1f, up, .07f, .7f, 6);
                    mk.Prism(drink, at + up * .8f, up, .1f, .55f, 12, 5.5f); mk.Blob(red, at + up * 1.42f + new Vector3(.2f, 0, .1f), Vector3.one * .09f, rng, 0f);
                    break;
                }
                default: mk.Prism(white, at, up, b.r, b.h, 10); mk.Prism(navy, at + up * b.h, up, b.r * 1.08f, .06f, 10); break;
            }
        }

        static void Zone(MeshKit mk, GolfZone z)
        {
            var s = z.shape; const float lift = .022f;
            Vector3 On(Vector2 q) => V(q, s.Height(q.x, q.y) + lift);
            void Fill(Material m)
            {
                if (s.poly != null) { var c = (On(s.poly[0]) + On(s.poly[2])) / 2f; mk.Quad(m, On(s.poly[0]), On(s.poly[1]), On(s.poly[2]), On(s.poly[3]), c + Vector3.down * 5f); }
                else for (var k = 0; k < 20; k++) { float a0 = k * Mathf.PI / 10f, a1 = (k + 1) * Mathf.PI / 10f; mk.Tri(m, On(s.c), On(s.c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * s.r), On(s.c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * s.r), On(s.c) + Vector3.down * 5f); }
            }
            void Chevrons(Material m, float every, float size)
            {
                var d = s.b - s.a; var len = d.magnitude; d /= len; var n = new Vector2(-d.y, d.x);
                for (var t = every * .5f; t < len - size; t += every)
                {
                    var tip = s.a + d * (t + size); var bl = s.a + d * t + n * size; var br = s.a + d * t - n * size; var notch = s.a + d * (t + size * .45f);
                    mk.Tri(m, On(tip) + up * .006f, On(bl) + up * .006f, On(notch) + up * .006f, On(tip) + Vector3.down); mk.Tri(m, On(tip) + up * .006f, On(br) + up * .006f, On(notch) + up * .006f, On(tip) + Vector3.down);
                }
            }
            switch (z.kind)
            {
                case GolfZoneKind.Water:
                    Fill(pool);
                    if (s.poly != null) for (var i = 0; i < 4; i++) WallBox(mk, white, s.poly[i], s.poly[(i + 1) % 4], s.Height(s.poly[i].x, s.poly[i].y), s.Height(s.poly[(i + 1) % 4].x, s.poly[(i + 1) % 4].y), 0f, .05f, .35f);
                    break;
                case GolfZoneKind.Boost: Chevrons(arrow, 1.5f, .8f); break;
                case GolfZoneKind.Current: Fill(river); Chevrons(WorldMaterials.Glow(new Color(.7f, .9f, 1f), 1.2f), 2.4f, .55f); break;
                case GolfZoneKind.Spring: Fill(springM); break;
                case GolfZoneKind.Fan: Chevrons(WorldMaterials.Glow(new Color(.85f, .95f, 1f), .9f), 1.1f, .4f); break;   // pale arrows where the gusts cross, pointing the way they push
                case GolfZoneKind.Pipe: mk.Prism(brass, On(s.c) - up * .01f, up, s.r + .1f, .03f, 16); mk.Prism(cup, On(s.c) + up * .012f, up, s.r, .02f, 16); break;
            }
        }

        static void Mover(MeshKit mk, GolfHole h, int index, System.Random rng)
        {
            var m = h.movers[index]; float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
            foreach (var w in h.walls) if (w.mover == index)
            {
                var doorway = m.look == "door";
                if (doorway) { WallBox(mk, glass, w.a, w.b, 0, 0, 0f, 1.5f, .07f); WallBox(mk, brass, w.a, w.b, 0, 0, 0f, .12f, .12f); WallBox(mk, brass, w.a, w.b, 0, 0, 1.5f, 1.6f, .12f); }
                else Wall(mk, w, m.y);
                x0 = Mathf.Min(x0, Mathf.Min(w.a.x, w.b.x)); x1 = Mathf.Max(x1, Mathf.Max(w.a.x, w.b.x)); z0 = Mathf.Min(z0, Mathf.Min(w.a.y, w.b.y)); z1 = Mathf.Max(z1, Mathf.Max(w.a.y, w.b.y));
            }
            foreach (var b in h.bumpers) if (b.mover == index) { if (m.look == "door") mk.Prism(brass, V(b.c, 0), up, b.r, 1.7f, 10); else Bumper(mk, b, m.y, rng, null); }
            if (m.look == "boat" && x1 > x0)
            {
                // a little rowing boat: hull, gunwale, a thwart and a rower's hat
                var c = new Vector3((x0 + x1) / 2f, 0, (z0 + z1) / 2f); float w = x1 - x0, d = z1 - z0;
                mk.Box(navy, c + up * .22f, new Vector3(w, .44f, d)); mk.Box(white, c + up * .47f, new Vector3(w + .08f, .08f, d + .08f)); mk.Box(teak, c + up * .5f, new Vector3(.3f, .06f, d * .9f));
                mk.Blob(pink, c + up * .78f, new Vector3(.2f, .24f, .2f), rng, 0f); mk.Prism(white, c + up * .98f, up, .26f, .05f, 8);
                foreach (var e in new[] { -1f, 1f }) mk.Box(teak, c + up * .55f + new Vector3(0, 0, e * (d / 2f + .35f)), new Vector3(.06f, .04f, .9f), Quaternion.Euler(e * 14f, 0, 0));
            }
        }

        // ---------------------------------------------------------------- scenery
        static void Prop(MeshKit mk, GolfProp p, System.Random rng, Transform ht, GolfCourseView view)
        {
            var at = p.at; var s = p.scale; var rot = Quaternion.Euler(0, p.yaw, 0); var atSea = at.y < .05f;
            Vector3 L(float x, float y, float z) => at + rot * new Vector3(x * s, y * s, z * s);
            switch (p.kind)
            {
                case "palm":
                {
                    if (atSea) { mk.Blob(sand, at + up * .05f, new Vector3(2.4f, .55f, 2.4f), rng, .12f); at.y = .35f; }
                    var lean = new Vector3((float)rng.NextDouble() * .3f - .15f, 1f, (float)rng.NextDouble() * .3f - .15f).normalized; var hgt = 5f + (float)rng.NextDouble() * 2f;
                    mk.Prism(bark, at, lean, .24f, hgt, 6, .6f); var top = at + lean * hgt;
                    for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51.4f + rng.Next(20), 0) * Vector3.forward; mk.Box(leaf, top + d * 1.25f - up * .28f, new Vector3(.55f, .05f, 2.7f), Quaternion.LookRotation(d) * Quaternion.Euler(22f, 0, 0)); }
                    mk.Blob(bark, top - up * .2f, Vector3.one * .22f, rng, 0f);
                    break;
                }
                case "buoy":
                    at.y = 0; mk.Prism(red, at - up * .3f, up, .5f, .95f, 8, .5f); mk.Prism(white, at + up * .65f, up, .22f, .55f, 6); mk.Blob(WorldMaterials.Glow(new Color(1f, .3f, .25f), 3f), at + up * 1.3f, Vector3.one * .13f, rng, 0f);
                    break;
                case "sailboat":
                    at.y = 0; mk.Box(hullM, L(0, .25f, 0), new Vector3(1.7f, .8f, 4.8f) * s, rot); mk.Box(navy, L(0, .55f, 0), new Vector3(1.76f, .12f, 4.86f) * s, rot); mk.Prism(white, L(0, .6f, .3f), up, .07f * s, 6.2f * s, 6);
                    mk.Tri(white, L(0, 6.6f, .3f), L(0, 1.2f, .3f), L(0, 1.2f, -2.1f), L(3, 3, 0)); mk.Tri(white, L(0, 6.6f, .3f), L(0, 1.2f, .3f), L(0, 1.2f, -2.1f), L(-3, 3, 0));
                    mk.Tri(cloth2, L(0, 5.6f, .45f), L(0, 1.1f, .45f), L(0, 1.1f, 2.2f), L(3, 3, 1)); mk.Tri(cloth2, L(0, 5.6f, .45f), L(0, 1.1f, .45f), L(0, 1.1f, 2.2f), L(-3, 3, 1));
                    break;
                case "yacht":
                    at.y = 0; mk.Box(hullM, L(0, .5f, 0), new Vector3(3.2f, 1.5f, 10f) * s, rot); mk.Box(navy, L(0, 1.05f, 0), new Vector3(3.28f, .16f, 10.08f) * s, rot);
                    mk.Box(hullM, L(0, 1.9f, -.6f), new Vector3(2.4f, 1.1f, 4.6f) * s, rot); mk.Box(hullM, L(0, 2.8f, -1.1f), new Vector3(1.8f, .7f, 2.4f) * s, rot);
                    foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) mk.Box(warm, L(e * 1.21f, 1.95f, -2.1f + k * 1.5f), new Vector3(.04f, .5f, 1f) * s, rot);
                    mk.Prism(white, L(0, 3.1f, -1.1f), up, .05f * s, 1.6f * s, 5); mk.Blob(WorldMaterials.Glow(new Color(1f, .35f, .3f), 3f), L(0, 4.75f, -1.1f), Vector3.one * .1f * s, rng, 0f);
                    break;
                case "hull":
                    mk.Box(hullM, L(0, -.05f, 0), new Vector3(6.2f, 1.55f, 15f) * s, rot); mk.Box(navy, L(0, .5f, 0), new Vector3(6.28f, .2f, 15.08f) * s, rot);
                    break;
                case "lamp":
                    if (atSea) at.y = -1.2f;
                    mk.Prism(white, at, up, .07f, 2.7f + (atSea ? 2.2f : 0f), 6); mk.Blob(warm, at + up * (2.85f + (atSea ? 2.2f : 0f)), Vector3.one * .2f, rng, 0f);
                    break;
                case "lounger":
                    if (atSea) { mk.Blob(sand, at + up * .05f, new Vector3(2.2f, .5f, 2.2f), rng, .1f); at.y = .4f; }
                    mk.Box(white, L(0, .3f, 0), new Vector3(.75f, .08f, 1.7f), rot); mk.Box(rng.Next(2) == 0 ? cloth1 : cloth2, L(0, .36f, 0), new Vector3(.66f, .05f, 1.6f), rot); mk.Box(white, L(0, .62f, -1f), new Vector3(.75f, .08f, .75f), rot * Quaternion.Euler(-55f, 0, 0));
                    break;
                case "parasol":
                    if (atSea) { mk.Blob(sand, at + up * .05f, new Vector3(2.2f, .5f, 2.2f), rng, .1f); at.y = .4f; }
                    mk.Prism(white, at, up, .05f, 2.5f, 5); mk.Cone(cloth1, at + up * 2.3f, 1.8f, .6f, 8);
                    break;
                case "flamingo":
                    for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; mk.Box(pink, at + new Vector3(Mathf.Cos(a) * .6f, .14f, Mathf.Sin(a) * .6f), new Vector3(.36f, .28f, .46f), Quaternion.Euler(0, -k * 36f, 0)); }
                    mk.Prism(pink, L(0, .2f, .55f), new Vector3(0, 1f, .2f), .09f, 1f, 6); mk.Blob(pink, L(0, 1.25f, .85f), new Vector3(.16f, .14f, .22f), rng, 0f); mk.Cone(black, L(0, 1.2f, 1.02f), .06f, .16f, 5);
                    break;
                case "ring":
                    for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; mk.Box(k % 2 == 0 ? white : red, at + new Vector3(Mathf.Cos(a) * .55f, .12f, Mathf.Sin(a) * .55f), new Vector3(.32f, .24f, .42f), Quaternion.Euler(0, -k * 36f, 0)); }
                    break;
                case "cabana":
                    if (atSea) { mk.Box(teak, at + up * .7f, new Vector3(6f, .3f, 4.4f), rot); foreach (var (x, z) in new[] { (-2.7f, -1.9f), (2.7f, -1.9f), (2.7f, 1.9f), (-2.7f, 1.9f) }) mk.Prism(teak, at + rot * new Vector3(x, -1.3f, z), up, .15f, 2f, 6); at.y = .85f; }
                    foreach (var (x, z) in new[] { (-2.2f, -1.4f), (2.2f, -1.4f), (2.2f, 1.4f), (-2.2f, 1.4f) }) mk.Prism(white, L(x, 0, z), up, .08f, 2.6f, 6);
                    for (var k = 0; k < 8; k++) mk.Box(k % 2 == 0 ? navy : white, L(-2.1f + k * .6f, 2.7f, 0), new Vector3(.6f, .07f, 3.4f), rot);
                    mk.Box(white, L(0, .5f, .8f), new Vector3(3.6f, 1f, .55f), rot); mk.Box(teak, L(0, 1.03f, .8f), new Vector3(3.8f, .08f, .7f), rot);
                    for (var k = 0; k < 4; k++) mk.Prism(WorldMaterials.Glow(Drinks[k], 1.6f), L(-1.2f + k * .8f, 1.07f, .8f), up, .07f, .26f, 6, 1.4f);
                    for (var k = 0; k < 9; k++) mk.Blob(warm, L(-2.2f + k * .55f, 2.45f - Mathf.Sin(k / 8f * Mathf.PI) * .25f, 1.5f), Vector3.one * .08f, rng, 0f);
                    break;
                case "piano":
                    if (atSea) { mk.Blob(sand, at + up * .05f, new Vector3(3f, .55f, 3f), rng, .1f); at.y = .42f; }
                    mk.Box(white, L(0, .85f, 0), new Vector3(1.55f, .32f, 2f), rot); foreach (var (x, z) in new[] { (-.6f, .8f), (.6f, .8f), (0f, -.8f) }) mk.Prism(white, L(x, 0, z), up, .06f, .7f, 5);
                    mk.Box(white, L(-.2f, 1.45f, -.1f), new Vector3(1.5f, .05f, 1.9f), rot * Quaternion.Euler(0, 0, 32f)); mk.Box(white, L(0, .95f, 1.1f), new Vector3(1.45f, .06f, .3f), rot);
                    for (var k = 0; k < 9; k++) mk.Box(black, L(-.64f + k * .16f, 1f, 1.04f), new Vector3(.07f, .03f, .17f), rot);
                    break;
                case "tower":
                    // the lighthouse's bands, round the foot of its green
                    foreach (var y in new[] { .2f, 1.5f }) mk.Prism(red, at + up * y, up, 5.5f, .6f, 28);
                    break;
                case "fan":
                {
                    if (atSea) at.y = -1.2f; var hgt = atSea ? 3.5f : 1.9f; var face = rot * Vector3.forward;
                    mk.Prism(white, at, up, .12f, hgt, 6); mk.Prism(navy, at + up * hgt - face * .25f, face, 1.25f, .5f, 14, 1f, 0f, false); mk.Prism(white, at + up * hgt - face * .3f, face, .22f, .65f, 8);
                    var rm = new MeshKit(); for (var k = 0; k < 4; k++) rm.Box(white, Quaternion.Euler(0, 0, k * 90f) * new Vector3(.55f, 0, 0), new Vector3(1f, .26f, .04f), Quaternion.Euler(0, 0, k * 90f) * Quaternion.Euler(25f, 0, 0));
                    var rgo = rm.BuildSingle("Fan blades", ht, false); if (rgo != null) { var holder = new GameObject("Fan").transform; holder.SetParent(ht, false); holder.localPosition = at + up * hgt + face * .3f; holder.localRotation = Quaternion.LookRotation(face); rgo.transform.SetParent(holder, false); view.rotors.Add(rgo.transform); }
                    break;
                }
                case "sax":
                {
                    // a saxophone standing behind the hole you putt into: the tube up, the crook over, the bell beside the mouth
                    var b = at + Vector3.forward * .75f; mk.Prism(brass, b, up, .2f, 2f, 8); mk.Prism(brass, b + up * 2f, new Vector3(0, .5f, -1f), .12f, .8f, 6, .6f);
                    mk.Prism(brass, b + up * .15f, new Vector3(0, .35f, -1f), .22f, .75f, 10, 2.2f, 0f, false);
                    for (var k = 0; k < 4; k++) mk.Blob(gold, b + up * (.6f + k * .32f) - Vector3.forward * .22f, Vector3.one * .07f, rng, 0f);
                    break;
                }
                case "bell":
                {
                    // where a pipe lets the ball out: a horn's bell pointing the way it will roll
                    var a = p.yaw * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    mk.Prism(brass, at - d * 1.1f + up * .75f, d - up * .45f, .16f, 1.15f, 10, 2.6f, 0f, false); mk.Prism(brass, new Vector3(at.x - d.x * 1.1f, -1.2f, at.z - d.z * 1.1f), up, .07f, at.y + 1.95f, 6);
                    break;
                }
                case "glassbig":
                {
                    // a giant cocktail standing in the sea
                    at.y = -1.2f; var drink = WorldMaterials.Glow(new Color(1f, .5f, .3f), 1.5f);
                    mk.Prism(glassClear(), at, up, 2.2f, .3f, 16); mk.Prism(glassClear(), at + up * .3f, up, .22f, 6f, 8); mk.Prism(drink, at + up * 6.3f, up, .3f, 3.4f, 16, 12f);
                    mk.Blob(red, at + up * 9.9f + new Vector3(1.4f, 0, .5f), Vector3.one * .5f, rng, 0f); mk.Prism(white, at + up * 8.5f + new Vector3(-1f, 0, 0), new Vector3(-.35f, 1f, .1f), .06f, 3.4f, 5); mk.Cone(cloth1, at + up * 11.6f + new Vector3(-2.2f, 0, .35f), 1.5f, .5f, 8);
                    break;
                }
                case "sunring":
                {
                    // the sun to fly through: a ring of light standing across the way
                    const float R = 3.6f; for (var k = 0; k < 24; k++) { var a = k * Mathf.PI / 12f; mk.Box(sunM, at + new Vector3(Mathf.Cos(a) * R, Mathf.Sin(a) * R, 0), new Vector3(.98f, .4f, .4f), Quaternion.Euler(0, 0, k * 15f + 90f)); }
                    for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; mk.Box(gold, at + new Vector3(Mathf.Cos(a) * (R + 1.3f), Mathf.Sin(a) * (R + 1.3f), 0), new Vector3(1.2f, .16f, .16f), Quaternion.Euler(0, 0, k * 30f)); }
                    foreach (var e in new[] { -1f, 1f }) mk.Prism(white, new Vector3(at.x + e * R, -1.2f, at.z), up, .16f, at.y + 1.2f, 6);
                    Light(ht, at, new Color(1f, .65f, .3f), 22f, 2.4f);
                    break;
                }
            }
        }

        static Material clear;
        static Material glassClear() => clear ??= Clear("Golf big glass", new Color(.85f, .95f, 1f, .35f));
    }
}
