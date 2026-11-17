using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Kawaii Cascades as a cake standing in a dish of strawberry milk (part of KawaiiKit). The land is three rings:
    /// - the Cake, in the middle: the tiered summit with the Sugar Bowl, the Parfait and every artist, its sides falling
    ///   sheer into the milk;
    /// - the Milk, all round it: nothing to stand on but what was put there. Fall in and you are set back on the last
    ///   firm thing you stood on (KawaiiLife);
    /// - the Crust, the biscuit rim along the shore: a ring road right round the land, where the rails come down and the
    ///   courses start.
    /// Across the milk: the four rails from the Parfait out to the Crust, four parkour courses back from the Crust to the
    /// Cake (one between each pair of rails: the Macaron Steps, the Cupcake Rooftops, Lollipop Lane and the Donut Hops,
    /// each ending at a geyser that fires you up onto the Cake), and a plain wafer causeway from each bridge, for anyone
    /// who would rather walk. Round the whole Cake, half way out, runs the Grand Circuit: a ring of the same pieces a
    /// couple of kilometres long. Nothing here is scattered: every piece is on a line.
    public partial class KawaiiKit
    {
        const float IslandR = 235f, IslandCoast = 80f, CrustW = 34f, CrustY = 9f, MilkY = 6.5f, CakeMin = 28f;
        byte[] zone; IslandWorldData zdata;          // per ground cell of this land: 1 crust, 2 milk, 3 cake
        readonly Vector2[] courseDir = new Vector2[4]; readonly Vector2[] runCrust = new Vector2[4];
        readonly List<(Vector2 crust, Vector2 cake)> causeways = new List<(Vector2, Vector2)>();
        static readonly string[] CourseName = { "MACARON STEPS", "CUPCAKE ROOFTOPS", "LOLLIPOP LANE", "DONUT HOPS" };

        int ZoneAt(float x, float z)
        {
            if (zone == null) return 0;
            var c = Mathf.RoundToInt(x / zdata.Step); var r = Mathf.RoundToInt((zdata.layout.depth - z) / zdata.Step);
            if (c < 0 || r < 0 || c >= zdata.Nx || r >= zdata.Nz) return 0;
            return zone[r * zdata.Nx + c];
        }
        int ZoneAt(Vector2 p) => ZoneAt(p.x, p.y);
        bool InMilk(float x, float z) => ZoneAt(x, z) == 2;

        /// Works out the three rings, and gives the Crust and the Milk their ground (the Cake's tiers come after).
        void ShapeZones(IslandWorldData data, int index)
        {
            zdata = data; zone = new byte[data.heights.Length]; int nx = data.Nx, nz = data.Nz; var step = data.Step;
            int r0 = nz, r1 = 0, c0 = nx, c1 = 0;
            for (var i = 0; i < data.heights.Length; i++) if (data.islandIds[i] == index && data.heights[i] > 1.5f) { int r = i / nx, c = i % nx; r0 = Mathf.Min(r0, r); r1 = Mathf.Max(r1, r); c0 = Mathf.Min(c0, c); c1 = Mathf.Max(c1, c); }
            r0 = Mathf.Max(1, r0 - 1); c0 = Mathf.Max(1, c0 - 1); r1 = Mathf.Min(nz - 2, r1 + 1); c1 = Mathf.Min(nx - 2, c1 + 1);
            // how far each cell is from the shore (two sweeps over the land)
            var dist = new float[data.heights.Length]; bool Land(int i) => data.islandIds[i] == index && data.heights[i] > 1.5f;
            for (var r = r0; r <= r1; r++) for (var c = c0; c <= c1; c++) { var i = r * nx + c; dist[i] = Land(i) ? 1e6f : 0f; }
            const float diag = 1.414f;
            for (var r = r0; r <= r1; r++) for (var c = c0; c <= c1; c++) { var i = r * nx + c; if (dist[i] == 0f) continue; dist[i] = Mathf.Min(dist[i], Mathf.Min(Mathf.Min(dist[i - 1] + 1f, dist[i - nx] + 1f), Mathf.Min(dist[i - nx - 1] + diag, dist[i - nx + 1] + diag))); }
            for (var r = r1; r >= r0; r--) for (var c = c1; c >= c0; c--) { var i = r * nx + c; if (dist[i] == 0f) continue; dist[i] = Mathf.Min(dist[i], Mathf.Min(Mathf.Min(dist[i + 1] + 1f, dist[i + nx] + 1f), Mathf.Min(dist[i + nx + 1] + diag, dist[i + nx - 1] + diag))); }
            for (var r = r0; r <= r1; r++) for (var c = c0; c <= c1; c++)
            {
                var i = r * nx + c; if (!Land(i)) continue; var d = dist[i] * step;
                var toHeart = Vector2.Distance(new Vector2(c * step, data.layout.depth - r * step), heart2);
                if (d > IslandCoast && toHeart < IslandR) zone[i] = 3;
                else if (d <= CrustW) { zone[i] = 1; data.heights[i] = Mathf.Lerp(Mathf.Min(data.heights[i], 3f), CrustY, Ease(0f, 22f, d)); }
                else { zone[i] = 2; data.heights[i] = 1f; }
            }
        }

        /// How far from the summit, going 'dir', the first cell of the given ring is (or the last, for 'last').
        float March(Vector2 dir, int ring, float from = 0f, bool last = false)
        {
            var found = -1f;
            for (var d = from; d < 900f; d += 4f) { var z = ZoneAt(heart2 + dir * d); if (z == ring) { if (!last) return d; found = d; } else if (last && found >= 0f) return found; else if (z == 0 && d > 100f) break; }
            return found;
        }

        public override bool GroundAt(float x, float z, float height, float slope, float patch, out Color color)
        {
            var ring = ZoneAt(x, z);
            if (ring == 1) { color = Color.Lerp(new Color(.93f, .7f, .4f), new Color(.82f, .56f, .3f), patch); color.a = 1f; return true; }      // biscuit crust
            if (ring == 2) { color = new Color(1f, .7f, .82f, 1f); return true; }                                                                // under the milk
            return Ground(height, slope, patch, out color);
        }

        // ---------------------------------------------------------------- the milk
        void Moat(LandContext ctx, MeshKit mk)
        {
            var d = zdata; var n = 0; var milk = new MeshKit(); var s = d.Step / 2f + .02f;
            // the sea's own shader, in strawberry: ripples that liven with the music, paler in the shallows against the
            // Cake and the Crust, foam round everything that stands in it, sun glitter. (A flat glowing pink if it is missing.)
            var surface = milkM; var water = Shader.Find("Everything World/Water");
            if (water != null)
            {
                surface = new Material(water) { name = "Kawaii milk sea" };
                surface.SetColor("_Shallow", new Color(1f, .88f, .93f)); surface.SetColor("_Deep", new Color(1f, .6f, .78f)); surface.SetColor("_Foam", new Color(1f, .97f, .98f));
                surface.SetFloat("_DepthRange", 5f); surface.SetFloat("_FoamDepth", 1.1f); surface.SetFloat("_Ripple", .75f); surface.SetFloat("_Reflect", .18f); surface.SetFloat("_Glitter", 2.2f);
            }
            for (var i = 0; i < zone.Length; i++)
            {
                if (zone[i] != 2) continue; int r = i / d.Nx, c = i % d.Nx; var p = new Vector3(c * d.Step, MilkY, d.layout.depth - r * d.Step);
                milk.Quad(surface, p + new Vector3(-s, 0, -s), p + new Vector3(-s, 0, s), p + new Vector3(s, 0, s), p + new Vector3(s, 0, -s), p - Vector3.up); n++;
            }
            milk.Build("Strawberry milk", ctx.Root, false);
            life.inMilk = InMilk; life.milkY = MilkY;
            // falls of milk off the Cake's edge, one every so often all the way round
            for (var k = 0; k < 14; k++)
            {
                var dir = new Vector2(Mathf.Sin((k * 25.7f + 9f) * Mathf.Deg2Rad), Mathf.Cos((k * 25.7f + 9f) * Mathf.Deg2Rad)); var e = March(dir, 3, 60f, true); if (e < 0f) continue;
                var top = G(V3(heart2 + dir * (e - 3f))); var foot = V3(heart2 + dir * (e + 5f)) + Vector3.up * MilkY; var span = top - foot; var w = 4f + (k % 3) * 2f;
                mk.Box(milkM, (top + foot) / 2f + Vector3.up * .3f, new Vector3(w, .25f, span.magnitude + 2f), Quaternion.LookRotation(span.normalized));
                life.milkFeet.Add(foot + Vector3.up * .4f); halos.Add(foot + Vector3.up, w, new Color(1f, .7f, .85f), .18f);
            }
            Debug.Log($"The Kawaii Cascades: {n * d.Step * d.Step / 10000f:0.0} ha of strawberry milk");
        }

        // ---------------------------------------------------------------- the causeways from the bridges
        void Causeways(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var wafer = Paint(new Color(.95f, .76f, .46f)); var waferD = Paint(new Color(.82f, .58f, .32f)); var n = 0;
            foreach (var (crust2, cake2) in causeways)
            {
                var a = G(V3(crust2)); a.y = Mathf.Max(a.y, CrustY) + .15f; var b = G(V3(cake2)); b.y += .15f; var len = Vector3.Distance(a, b); var segs = Mathf.Max(2, Mathf.RoundToInt(len / 6f));
                var flat = new Vector3(b.x - a.x, 0, b.z - a.z).normalized; var side = Vector3.Cross(up, flat);
                for (var i = 0; i < segs; i++)
                {
                    var p0 = Vector3.Lerp(a, b, i / (float)segs); var p1 = Vector3.Lerp(a, b, (i + 1f) / segs); var mid = (p0 + p1) / 2f; var rot = Quaternion.LookRotation((p1 - p0).normalized); var sl = Vector3.Distance(p0, p1) + .06f;
                    mk.Box(i % 2 == 0 ? wafer : waferD, mid - up * .25f, new Vector3(6f, .5f, sl), rot); solid.Box(wafer, mid - up * .25f, new Vector3(6f, .5f, sl), rot);
                    foreach (var e in new[] { -1f, 1f }) mk.Box(i % 2 == 0 ? pinkM : cream, mid + side * e * 2.9f + up * .12f, new Vector3(.3f, .3f, sl), rot);
                    if (i % 4 == 2) { foreach (var e in new[] { -1f, 1f }) { var foot = mid + side * e * 2.4f; mk.Prism(waferD, new Vector3(foot.x, .8f, foot.z), up, .35f, mid.y - 1.2f, 7); } }
                    if (i % 6 == 3) { var lp = mid + side * 3.3f; mk.Prism(stick, lp, up, .07f, 2.3f, 6); mk.Prism(lampM, lp + up * 2.6f, up, .3f, .5f, 8, .6f); halos.Add(lp + up * 2.8f, 1.5f, Yellow, .45f); }
                    Remember(mid, 20 + n);
                }
                // over the Crust to the bridge, a wafer road on the ground
                var outDir = -flat; var pts = new List<Vector3>(); for (var d = 0f; d < 60f; d += 2f) { var q = a + outDir * d; if (!ctx.IsLand(q.x, q.z, 0f)) break; pts.Add(G(q)); } if (pts.Count > 1) Strip(mk, pts, wafer, 6f, .1f, ctx);
                Board(mk, G(a + outDir * 6f + side * 6f), outDir, "CAUSEWAY TO THE CAKE", Cream, 7f, 1.3f, .32f);
                Shot(ctx, "A causeway over the milk", Vector3.Lerp(a, b, .3f) + side * 14f + up * 8f, flat * 2f - side - up * .25f);
                n++;
            }
        }

        // ---------------------------------------------------------------- the courses
        /// A firm round raft floating on the milk: wafer, with a coloured rim.
        void Raft(MeshKit mk, MeshKit solid, Vector3 c, float r, int k)
        {
            var up = Vector3.up; mk.Prism(Paint(new Color(.95f, .76f, .46f)), new Vector3(c.x, MilkY - .5f, c.z), up, r, c.y - MilkY + .5f - .14f, 14);   /* (stops under the coloured top, so the two don't flicker) */ mk.Prism(SweetM(k), c - up * .12f, up, r * 1.04f, .12f, 14);
            solid.Prism(cream, new Vector3(c.x, MilkY - .5f, c.z), up, r, c.y - MilkY + .5f, 10);
        }

        readonly List<Vector3> laid = new List<Vector3>();   // every course piece so far (so lines that cross don't pile up)

        /// One piece of a course, of the given kind (0 macaron, 1 cupcake, 2 lollipop, 3 jelly raft that throws you on to 'next').
        void Piece(LandContext ctx, MeshKit mk, MeshKit solid, int kind, int i, Vector3 p, Vector3 next, System.Random rng)
        {
            var up = Vector3.up; var k = i + kind * 3; var wave = Mathf.Sin(i * .8f + kind); laid.Add(p);
            switch (kind)
            {
                case 0: { var c = new Vector3(p.x, 9.4f + wave * 1.1f, p.z); Macaron(mk, solid, c, i % 4 == 0 ? 3.1f : 2.3f, k); if (i % 7 == 3) Star(c + up * 2.3f); break; }
                case 1:
                {
                    // a cupcake standing in the milk: its top is the roof you run across
                    var top = 9.6f + wave * 1.6f + (i % 5 == 0 ? 1.4f : 0f); var R = 2.8f + (i % 3) * .4f; var c = new Vector3(p.x, top, p.z);
                    mk.Prism(SweetM(k), new Vector3(p.x, MilkY - .6f, p.z), up, R * .72f, top - 1f - MilkY + .6f, 12, 1.3f); mk.Prism(k % 2 == 0 ? white : pinkM, c - up * 1f, up, R, 1f, 14, .96f); solid.Prism(cream, new Vector3(p.x, MilkY - .6f, p.z), up, R, top - MilkY + .6f, 10);
                    if (i % 6 == 2) { mk.Blob(cherryM, c + up * .5f, Vector3.one * .5f, rng, .08f); ArtistBits.PostCollider(ctx.Root, c, 1f, .5f); }
                    if (i % 7 == 5) Star(c + up * 2.6f);
                    break;
                }
                case 2:
                {
                    // a lollipop lying flat on a tall stick: a small round top to land on
                    var top = 9.2f + wave * .9f; var c = new Vector3(p.x, top, p.z); var R = i % 5 == 0 ? 2.4f : 1.6f;
                    mk.Prism(stick, new Vector3(p.x, MilkY - .6f, p.z), up, .14f, top - MilkY + .3f, 7); mk.Prism(SweetM(k), c - up * .35f, up, R, .35f, 14); mk.Prism(white, c + up * .005f, up, R * .68f, .02f, 12); mk.Prism(SweetM(k + 2), c + up * .01f, up, R * .36f, .02f, 10);
                    solid.Prism(cream, c - up * .35f, up, R, .35f, 10); if (i % 8 == 4) Star(c + up * 2.2f);
                    break;
                }
                default:
                {
                    // a raft with a jelly on it that throws you to the next raft, through a floating donut
                    var c = new Vector3(p.x, 8.6f, p.z); Raft(mk, solid, c, 3.8f, k); var to = new Vector3(next.x - c.x, 0, next.z - c.z); var d = to.magnitude; if (d < 3f) break;
                    const float vy = 19f; var flight = 2f * vy / 38f; var vh = d / (flight * .72f);
                    Jelly(c, 2f, to.normalized * vh + up * vy, Sweet[k % 4]);
                    var ring = c + to / 2f + up * 4.4f; var rr = Quaternion.LookRotation(to.normalized);
                    for (var a = 0; a < 12; a++) { var ang = a * 30f * Mathf.Deg2Rad; mk.Blob(a % 3 == 0 ? white : SweetM(k), ring + rr * new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0) * 3.4f, Vector3.one * .7f, rng, .08f); }
                    if (i % 3 == 1) Star(ring);
                    break;
                }
            }
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; var u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return Vector2.Distance(p, a + ab * u); }
        bool Clear(Vector3 p, float gap)
        {
            foreach (var q in laid) { float dx = q.x - p.x, dz = q.z - p.z; if (dx * dx + dz * dz < gap * gap) return false; }
            foreach (var (c2, k2) in causeways) if (SegDist(new Vector2(p.x, p.z), c2, k2) < 8f) return false;
            return ZoneAt(p.x, p.z) == 2;
        }

        void Courses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(6610); int spokes = 0, circuit = 0; laid.Clear();
            // the four spokes: from the Crust in to the Cake, one between each pair of rails
            for (var w = 0; w < 4; w++)
            {
                var dir2 = courseDir[w]; var cakeEdge = March(dir2, 3, 60f, true); var crustIn = March(dir2, 1, cakeEdge + 8f); if (cakeEdge < 0f || crustIn < 0f) continue;
                var dir = V3(dir2); var side = Vector3.Cross(up, dir); var width = crustIn - cakeEdge;
                var A = V3(heart2 + dir2 * (crustIn - 5f)); var B = V3(heart2 + dir2 * (cakeEdge + 13f));
                var amp = Mathf.Clamp(width * .24f, 10f, 46f); var waves = Mathf.Max(1, Mathf.RoundToInt(width / 85f));
                Vector3 At(float t) { var p = Vector3.Lerp(A, B, t) + side * (Mathf.Sin(t * Mathf.PI * waves) * amp * Mathf.Sqrt(Mathf.Sin(t * Mathf.PI))); if (ZoneAt(p.x, p.z) != 2) p = Vector3.Lerp(A, B, t); return p; }
                var spacing = w == 0 ? 8.6f : w == 1 ? 9.4f : w == 2 ? 6.6f : 21f; var pieces = new List<Vector3>(); var last = At(0f); var acc = spacing * .6f;
                for (var t = 0f; t <= 1f; t += .002f) { var p = At(t); acc += Vector3.Distance(p, last); last = p; if (acc >= spacing && ZoneAt(p.x, p.z) == 2) { pieces.Add(p); acc = 0f; } }
                var cakeTop = G(V3(heart2 + dir2 * (cakeEdge - 6f)));
                for (var i = 0; i < pieces.Count; i++) Piece(ctx, mk, solid, w, i, pieces[i], i + 1 < pieces.Count ? pieces[i + 1] : B, rng);
                spokes += pieces.Count;
                // the far end: a raft at the Cake's foot with a geyser that fires you up onto the Cake
                var raft = new Vector3(B.x, 8.6f, B.z); Raft(mk, solid, raft, 4.6f, w); laid.Add(raft);
                var rise = cakeTop.y - raft.y + 6f; Jelly(raft, 2.2f, up * Mathf.Sqrt(2f * 38f * rise) - dir * 15f, RunColor[w], HyperCourse.Kind.Geyser);
                Board(mk, G(A + dir * 9f + side * 5f), dir, CourseName[w], RunColor[w], 6.4f, 1.4f, .4f);
                Board(mk, G(V3(heart2 + dir2 * (cakeEdge - 12f)) + side * 6f), -dir, CourseName[w], RunColor[w], 6.4f, 1.4f, .4f);
                Remember(cakeTop, 30 + w);
                Shot(ctx, CourseName[w], Vector3.Lerp(A, B, .25f) + side * (amp + 16f) + up * 16f, Vector3.Lerp(A, B, .5f) - (Vector3.Lerp(A, B, .25f) + side * (amp + 16f) + up * 16f));
            }
            // the Grand Circuit: a ring of pieces all the way round the Cake, half way out across the milk, changing kind
            // every eighth of the way (macarons, cupcakes, lollipops, jelly hops, and again). It runs clockwise.
            const int N = 720; var rad = new float[N];
            for (var k = 0; k < N; k++) { var d2 = new Vector2(Mathf.Sin(k * .5f * Mathf.Deg2Rad), Mathf.Cos(k * .5f * Mathf.Deg2Rad)); var ce = March(d2, 3, 60f, true); var ci = March(d2, 1, Mathf.Max(ce, 60f) + 8f); rad[k] = ce > 0f && ci > 0f ? Mathf.Lerp(ce, ci, .5f) : -1f; }
            for (var k = 0; k < N; k++) if (rad[k] < 0f) rad[k] = rad[(k + N - 1) % N] > 0f ? rad[(k + N - 1) % N] : IslandR + 60f;
            var smooth = new float[N]; for (var k = 0; k < N; k++) { var s = 0f; for (var j = -40; j <= 40; j++) s += rad[(k + j + N) % N]; smooth[k] = s / 81f; }
            Vector3 Ring(float deg) { var k = Mathf.Repeat(deg * 2f, N); int k0 = (int)k % N, k1 = (k0 + 1) % N; var r = Mathf.Lerp(smooth[k0], smooth[k1], k - (int)k) + Mathf.Sin(deg * Mathf.Deg2Rad * 9f) * 14f; return V3(heart2) + new Vector3(Mathf.Sin(deg * Mathf.Deg2Rad), 0, Mathf.Cos(deg * Mathf.Deg2Rad)) * r; }
            var ringPieces = new List<(Vector3 p, int kind)>(); var prev = Ring(0f); var run = 0f;
            for (var deg = 0f; deg < 360f; deg += .05f)
            {
                var p = Ring(deg); run += Vector3.Distance(p, prev); prev = p; var kind = Mathf.FloorToInt(deg / 45f) % 4; var sp = kind == 0 ? 8.6f : kind == 1 ? 9.4f : kind == 2 ? 6.6f : 21f;
                if (run < sp) continue; if (!Clear(p, kind == 3 ? 9f : 5f)) continue; run = 0f; ringPieces.Add((p, kind)); laid.Add(p);
            }
            for (var i = 0; i < ringPieces.Count; i++) { var (p, kind) = ringPieces[i]; laid.RemoveAt(laid.Count - 1); }
            for (var i = 0; i < ringPieces.Count; i++) { var (p, kind) = ringPieces[i]; var nx = ringPieces[(i + 1) % ringPieces.Count].p; Piece(ctx, mk, solid, kind, i, p, Vector3.Distance(nx, p) < 40f ? nx : p, rng); }
            circuit = ringPieces.Count;
            for (var q = 0; q < 4; q++) Shot(ctx, "The Grand Circuit " + (q + 1), Ring(q * 90f + 20f) + up * 14f + (V3(heart2) - Ring(q * 90f + 20f)).normalized * -26f, Ring(q * 90f + 40f) - (Ring(q * 90f + 20f) + up * 14f));
            Debug.Log($"The Kawaii Cascades: {spokes} pieces on the four courses in to the Cake, {circuit} on the Grand Circuit round it");
        }

        // ---------------------------------------------------------------- avenues and the Crust road (nothing scattered)
        void LolliTree(MeshKit mk, Vector3 p, Vector3 f, int k, float s)
        {
            var up = Vector3.up; var h = 4f + s * 3.5f; var lo = Lowest(p, .3f) - .2f; mk.Prism(stick, new Vector3(p.x, lo, p.z), up, .14f, p.y + h - lo, 7);
            var c = p + up * (h + 1.2f); var R = 1.5f + s * .5f; mk.Prism(SweetM(k), c - f * .2f, f, R, .4f, 16); mk.Prism(white, c + f * .21f, f, R * .7f, .02f, 14); mk.Prism(SweetM(k + 2), c + f * .22f, f, R * .4f, .02f, 12); mk.Prism(white, c - f * .21f, -f, R * .7f, .02f, 14);
            ArtistBits.PostCollider(rootT, p, Mathf.Min(h, 4f), .24f); Mark(p, 1f);
        }

        void GummyBear(MeshKit mk, Vector3 p, Vector3 f, int k, float sc, System.Random rng)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, f); var m = GlowM(Sweet[k % 4], .5f);
            mk.Blob(m, p + up * sc * .8f, new Vector3(.9f, .9f, .75f) * sc, rng, .08f); mk.Blob(m, p + up * sc * 1.9f, new Vector3(.62f, .58f, .56f) * sc, rng, .08f);
            foreach (var e in new[] { -1f, 1f }) { mk.Blob(m, p + up * sc * 2.45f + side * e * sc * .45f, Vector3.one * sc * .22f, rng, .05f); mk.Blob(m, p + up * sc * 1.1f + side * e * sc * .85f + f * sc * .2f, new Vector3(.3f, .3f, .4f) * sc, rng, .05f); mk.Blob(m, p + up * sc * .3f + side * e * sc * .55f + f * sc * .7f, new Vector3(.34f, .3f, .5f) * sc, rng, .05f); mk.Box(Paint(Ink), p + up * sc * 2f + side * e * sc * .22f + f * sc * .5f, Vector3.one * sc * .1f, Quaternion.LookRotation(f)); }
            ArtistBits.PostCollider(rootT, p, sc * 2.4f, sc * .8f); Mark(p, sc * 1.2f);
        }

        void Edging(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(6611); int trees = 0, lamps = 0, benches = 0, bears = 0;
            // an avenue of lollipop trees down each Run
            for (var r = 0; r < 4; r++)
            {
                var dir = V3(runDir[r]); var side = Vector3.Cross(up, dir);
                for (var d = 40f; d < runLen[r] - 8f; d += 24f) foreach (var e in new[] { -1f, 1f }) { var p = G(center + dir * d + side * e * (RoadW / 2f + 3.4f)); if (ZoneAt(p.x, p.z) != 3 || InFootprint(ctx, p.x, p.z, 2f) || NearPath(p.x, p.z, 2.4f)) continue; LolliTree(mk, p, -side * e, r + trees, (float)rng.NextDouble()); trees++; }
            }
            // the Crust: a ring road with a lamp every so often, benches that look out over the milk, a tree between them
            Vector3 lastP = default; var along = 0f; var ringPts = new List<Vector3>(); var step = 0;
            for (var a = 0f; a < 360f; a += .5f)
            {
                var dir2 = new Vector2(Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad)); var inner = March(dir2, 1, 200f); if (inner < 0f) continue;
                var p = G(V3(heart2 + dir2 * (inner + 15f))); if (ZoneAt(p.x, p.z) != 1) continue;
                if (lastP != default && Vector3.Distance(p, lastP) < 60f) along += Vector3.Distance(p, lastP); lastP = p;
                if (along < 26f) continue; along = 0f; step++; var inward = -V3(dir2);
                ringPts.Add(p); Remember(p, 40);
                if (step % 4 == 0) { var bp = G(p + inward * 7f); if (ZoneAt(bp.x, bp.z) == 1) { Bench(mk, bp, inward, step); benches++; } }
                else if (step % 4 == 2) { LolliTree(mk, G(p - inward * 6f), inward, step, (float)rng.NextDouble()); trees++; }
                else { Lamp(ctx, mk, G(p - inward * 4f), step % 3 == 0, step); lamps++; }
            }
            // spectators: gummy bears watching each course from the Crust, and the rails come down beside a flag
            for (var w = 0; w < 4; w++)
            {
                var dir2 = courseDir[w]; var inner = March(dir2, 1, 200f); if (inner < 0f) continue; var dir = V3(dir2); var side = Vector3.Cross(up, dir);
                for (var i = 0; i < 3; i++) { var p = G(V3(heart2 + dir2 * (inner + 7f)) + side * (-9f - i * 3.2f)); if (ZoneAt(p.x, p.z) == 1) { GummyBear(mk, p, -dir, w + i, 1.1f + i * .25f, rng); bears++; } }
            }
            Debug.Log($"The Kawaii Cascades: {trees} lollipop trees in avenues, {lamps} lamps and {benches} benches round the Crust ({ringPts.Count} points), {bears} gummy bears watching");
        }
    }
}
