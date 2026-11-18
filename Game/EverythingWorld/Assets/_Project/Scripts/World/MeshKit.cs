using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.World
{
    /// Builds low-poly, flat-shaded meshes from simple shapes (boxes, prisms, blobs, roofs, quads), one
    /// submesh per material. Many small objects go into one mesh, so a whole forest is a few draw calls.
    /// Every shape is given its centre, and each triangle is turned to face away from it, so the winding
    /// is always right.
    public class MeshKit
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        /// Vertex colours, kept only once a surface-kind material (WorldMaterials.Surface) is used; until then null.
        List<Color> cols;
        readonly Dictionary<Material, List<int>> tris = new Dictionary<Material, List<int>>();

        public int VertexCount => verts.Count;
        Material lastIn, lastOut; bool lastSet, lastKind; Color lastCol; List<int> lastList;
        /// Triangles that are only skin (a bevel's edge strips and corners), by their first vertex: colliders leave
        /// them out, since nobody can feel a 4 cm edge, and every collider triangle costs time to prepare.
        HashSet<int> skin; bool skinning;

        /// One triangle facing away from 'inside'.
        public void Tri(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Vector2 ua = default, Vector2 ub = default, Vector2 uc = default)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0) { (b, c) = (c, b); (ub, uc) = (uc, ub); }
            if (ua == default && ub == default && uc == default) { ua = GrainUV(a, n); ub = GrainUV(b, n); uc = GrainUV(c, n); }
            // a surface kind: draw with the kind's shared material, the colour going into the mesh (remembered for
            // the last material asked for, since shapes come many triangles at a time)
            if (!lastSet || m != lastIn)
            {
                lastSet = true; lastIn = m; lastKind = WorldMaterials.KindOf(m, out var shared, out var lin);
                lastOut = lastKind ? shared : m; lastCol = lastKind ? lin : Color.white;
                if (!tris.TryGetValue(lastOut, out lastList)) tris[lastOut] = lastList = new List<int>();
            }
            var col = lastCol; var list = lastList;
            if (lastKind && cols == null) { cols = new List<Color>(verts.Count + 3); for (var i = 0; i < verts.Count; i++) cols.Add(Color.white); }
            if (skinning) (skin ??= new HashSet<int>()).Add(verts.Count);
            list.Add(verts.Count); verts.Add(a); uvs.Add(ua);
            list.Add(verts.Count); verts.Add(b); uvs.Add(ub);
            list.Add(verts.Count); verts.Add(c); uvs.Add(uc);
            if (cols != null) { cols.Add(col); cols.Add(col); cols.Add(col); }
        }

        /// Metres per repeat of the surface grain (WorldMaterials.Grain).
        public const float GrainScale = 5f;
        /// Texture coordinates for a face given none: projected from the world along the face's main axis (from
        /// above for floors and roofs, from the side for walls), so the grain is even and continuous.
        public static Vector2 GrainUV(Vector3 p, Vector3 n)
        {
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            var uv = ay >= ax && ay >= az ? new Vector2(p.x, p.z) : ax >= az ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
            return uv / GrainScale + new Vector2(.37f, .61f);   // (off the corner, so no face lands on the plain white texel)
        }

        /// A four-corner face (a, b, c, d in order round the edge) facing away from 'inside'.
        public void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
        {
            Tri(m, a, b, c, inside);
            Tri(m, a, c, d, inside);
        }

        /// A flat picture (for example an album cover) facing 'normal', with the given texture rectangle.
        public void Picture(Material m, Vector3 center, Vector3 right, Vector3 up, Vector3 normal, Rect uv)
        {
            center += normal.normalized * .012f;   // lifted 12 mm off its wall, so a surface grown by Box can never hide it
            Vector3 a = center - right - up, b = center + right - up, c = center + right + up, d = center - right + up;
            var inside = center - normal;
            Tri(m, a, b, c, inside, new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax));
            Tri(m, a, c, d, inside, new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
        }

        /// How much a box with this material is grown on every side (a few millimetres), so two boxes of different
        /// materials never share a face exactly and flicker against each other: glowing panels sit proud of whatever
        /// they're mounted on, and solid surfaces step apart from each other by material.
        static float Grow(Material m)
        {
            if (m == null) return 0f;
            if (m.IsKeywordEnabled("_EMISSION") || m.name.StartsWith("Glow") || m.name.StartsWith("Song glow")) return .014f;
            if (!m.HasProperty("_BaseColor")) return 0f;
            var c = m.GetColor("_BaseColor");
            int r = Mathf.RoundToInt(c.r * 255f), g = Mathf.RoundToInt(c.g * 255f), b = Mathf.RoundToInt(c.b * 255f);
            return .0018f * ((r + 3 * g + 5 * b) % 7);   // 0 to 11 mm in steps, chosen so the land audits' pairs separate
        }

        /// Box edges are cut back this far (metres) into a narrow slanted face that catches the light, so things look
        /// made rather than generated. 0 = sharp. Set by a land while it builds (WorldLook).
        public static float Bevel;

        /// A box of the given full size, rotated.
        public void Box(Material m, Vector3 center, Vector3 size, Quaternion rot)
        {
            var h = size / 2f + Vector3.one * Grow(m);
            var bv = Mathf.Min(Bevel, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * .35f);
            if (bv > .004f) { Bevelled(m, center, h, rot, bv); return; }
            var p = new Vector3[8];
            for (var i = 0; i < 8; i++)
                p[i] = center + rot * new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            Quad(m, p[0], p[1], p[3], p[2], center); // back (-z)
            Quad(m, p[4], p[5], p[7], p[6], center); // front (+z)
            Quad(m, p[0], p[2], p[6], p[4], center); // left
            Quad(m, p[1], p[3], p[7], p[5], center); // right
            Quad(m, p[2], p[3], p[7], p[6], center); // top
            Quad(m, p[0], p[1], p[5], p[4], center); // bottom
        }

        public void Box(Material m, Vector3 center, Vector3 size) => Box(m, center, size, Quaternion.identity);

        /// A box with its edges and corners cut back by 'b': six faces, twelve slanted edge strips, eight corner facets.
        void Bevelled(Material m, Vector3 center, Vector3 h, Quaternion rot, float b)
        {
            var hi = h - Vector3.one * b;
            // the point at a corner (signs sx, sy, sz) pushed out fully along one axis (0 = x, 1 = y, 2 = z)
            Vector3 C(int sx, int sy, int sz, int axis)
            {
                var v = new Vector3(sx * hi.x, sy * hi.y, sz * hi.z);
                if (axis == 0) v.x = sx * h.x; else if (axis == 1) v.y = sy * h.y; else v.z = sz * h.z;
                return center + rot * v;
            }
            int[] s = { -1, 1 };
            foreach (var e in s)
            {
                Quad(m, C(e, -1, -1, 0), C(e, 1, -1, 0), C(e, 1, 1, 0), C(e, -1, 1, 0), center);   // the x faces
                Quad(m, C(-1, e, -1, 1), C(1, e, -1, 1), C(1, e, 1, 1), C(-1, e, 1, 1), center);   // the y faces
                Quad(m, C(-1, -1, e, 2), C(1, -1, e, 2), C(1, 1, e, 2), C(-1, 1, e, 2), center);   // the z faces
            }
            skinning = true;
            foreach (var a in s) foreach (var c in s)
            {
                Quad(m, C(a, c, -1, 0), C(a, c, -1, 1), C(a, c, 1, 1), C(a, c, 1, 0), center);     // edges along z
                Quad(m, C(a, -1, c, 0), C(a, -1, c, 2), C(a, 1, c, 2), C(a, 1, c, 0), center);     // edges along y
                Quad(m, C(-1, a, c, 1), C(-1, a, c, 2), C(1, a, c, 2), C(1, a, c, 1), center);     // edges along x
            }
            foreach (var x in s) foreach (var y in s) foreach (var z in s) Tri(m, C(x, y, z, 0), C(x, y, z, 1), C(x, y, z, 2), center);
            skinning = false;
        }

        /// A tapered prism (low-poly trunk, post, log or candle) from 'start' along 'dir' for 'length'.
        public void Prism(Material m, Vector3 start, Vector3 dir, float radius, float length, int sides, float topScale = 1f, float twist = 0f, bool capTop = true)
        {
            dir = dir.normalized;
            var rot = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0, twist, 0);
            var end = start + dir * length;
            var mid = (start + end) / 2f;
            var lo = new Vector3[sides]; var hi = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var a = i * Mathf.PI * 2f / sides;
                var o = rot * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                lo[i] = start + o * radius;
                hi[i] = end + o * radius * topScale;
            }
            // a prism lying down (a log, a beam) shows both ends, so it gets a lid at the start too
            var capStart = Mathf.Abs(dir.y) < .7f;
            for (var i = 0; i < sides; i++)
            {
                var j = (i + 1) % sides;
                Quad(m, lo[i], lo[j], hi[j], hi[i], mid);
                if (capTop) Tri(m, hi[i], hi[j], end, mid);
                if (capStart) Tri(m, lo[i], lo[j], start, mid);
            }
        }

        /// A cone or pyramid (roof tip, spruce layer, candle flame).
        public void Cone(Material m, Vector3 baseCenter, float radius, float height, int sides, float twist = 0f)
        {
            var apex = baseCenter + Vector3.up * height;
            var inside = baseCenter + Vector3.up * height * .3f;
            for (var i = 0; i < sides; i++)
            {
                float a0 = twist * Mathf.Deg2Rad + i * Mathf.PI * 2f / sides, a1 = twist * Mathf.Deg2Rad + (i + 1) * Mathf.PI * 2f / sides;
                var p0 = baseCenter + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius;
                var p1 = baseCenter + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
                Tri(m, p0, p1, apex, inside);
                Tri(m, p0, p1, baseCenter, inside + Vector3.up * height); // underside
            }
        }

        static readonly Vector3[] Ico = MakeIco();
        static readonly int[] IcoFaces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        static Vector3[] MakeIco()
        {
            var t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (var i = 0; i < v.Length; i++) v[i] = v[i].normalized;
            return v;
        }

        /// A lumpy low-poly blob (tree crown, bush, rock, snow pile).
        public void Blob(Material m, Vector3 center, Vector3 radii, System.Random rng, float lumpiness = .18f)
        {
            var p = new Vector3[12];
            for (var i = 0; i < 12; i++)
            {
                var j = 1f + ((float)rng.NextDouble() * 2f - 1f) * lumpiness;
                p[i] = center + Vector3.Scale(Ico[i], radii) * j;
            }
            for (var f = 0; f < IcoFaces.Length; f += 3) Tri(m, p[IcoFaces[f]], p[IcoFaces[f + 1]], p[IcoFaces[f + 2]], center);
        }

        /// A gable roof: 'size' is the footprint (x = width across the ridge, z = length along it), ridge height
        /// above 'eaveCenter', with an overhang. Optional snow on top.
        public void GableRoof(Material m, Vector3 eaveCenter, Vector3 size, float ridge, Quaternion rot, Material snowOnTop = null)
        {
            float hx = size.x / 2f, hz = size.z / 2f;
            Vector3 P(float x, float y, float z) => eaveCenter + rot * new Vector3(x, y, z);
            Vector3 l0 = P(-hx, 0, -hz), l1 = P(-hx, 0, hz), r0 = P(hx, 0, -hz), r1 = P(hx, 0, hz), t0 = P(0, ridge, -hz), t1 = P(0, ridge, hz);
            var inside = P(0, ridge * .35f, 0);
            Quad(m, l0, l1, t1, t0, inside);
            Quad(m, r0, r1, t1, t0, inside);
            Tri(m, l0, r0, t0, inside);
            Tri(m, l1, r1, t1, inside);
            Quad(m, l0, l1, r1, r0, inside + rot * Vector3.up * ridge); // underside
            if (snowOnTop != null)
            {
                var lift = rot * Vector3.up * .12f;
                Quad(snowOnTop, Vector3.Lerp(l0, t0, .25f) + lift, Vector3.Lerp(l1, t1, .25f) + lift, t1 + lift, t0 + lift, inside);
                Quad(snowOnTop, Vector3.Lerp(r0, t0, .25f) + lift, Vector3.Lerp(r1, t1, .25f) + lift, t1 + lift, t0 + lift, inside);
            }
        }

        public const float ChunkAbove = 200f, ChunkSize = 80f;

        /// Draws this kit's hard edges into 'into' as thin lines of 'line' (the Delta Crossroads, where everything is
        /// drawn in gold line like a printed card): every edge at least 'minLen' long where two faces meet at a real
        /// corner, or where a face simply ends (the side of a road). Bevel skin is left out, so a bevelled box gives the
        /// edges of its faces; lines that fall on top of one another are drawn once. 'skip' names materials to leave
        /// plain (leaves, glows, the lines themselves).
        public int InkEdges(MeshKit into, Material line, float minLen, float thickness, System.Predicate<Material> skip = null)
        {
            var edges = new Dictionary<(long, long), (Vector3 a, Vector3 b, Vector3 n, int count, bool hard)>();
            long Q(Vector3 p) => ((long)Mathf.RoundToInt(p.x * 50f) & 0x1FFFFF) | (((long)Mathf.RoundToInt(p.y * 50f) & 0x1FFFFF) << 21) | (((long)Mathf.RoundToInt(p.z * 50f) & 0x1FFFFF) << 42);
            foreach (var kv in tris)
            {
                if (kv.Key == line || (skip != null && skip(kv.Key))) continue;
                var l = kv.Value;
                for (var i = 0; i < l.Count; i += 3)
                {
                    if (skin != null && skin.Contains(l[i])) continue;
                    Vector3 a = verts[l[i]], b = verts[l[i + 1]], c = verts[l[i + 2]]; var n = Vector3.Cross(b - a, c - a).normalized;
                    for (var e = 0; e < 3; e++)
                    {
                        var p = e == 0 ? a : e == 1 ? b : c; var q = e == 0 ? b : e == 1 ? c : a;
                        if ((p - q).sqrMagnitude < minLen * minLen) continue;
                        long kp = Q(p), kq = Q(q); var key = kp < kq ? (kp, kq) : (kq, kp);
                        if (edges.TryGetValue(key, out var had)) edges[key] = (had.a, had.b, had.n, had.count + 1, had.hard || Vector3.Dot(had.n, n) < .72f);
                        else edges[key] = (p, q, n, 1, false);
                    }
                }
            }
            var drawn = new HashSet<(int, int, int)>(); var made = 0;
            foreach (var kv in edges)
            {
                var e = kv.Value; if (e.count > 1 && !e.hard) continue;
                var mid = (e.a + e.b) / 2f; var cell = (Mathf.RoundToInt(mid.x / .14f), Mathf.RoundToInt(mid.y / .14f), Mathf.RoundToInt(mid.z / .14f));
                if (!drawn.Add(cell)) continue;
                var len = (e.b - e.a).magnitude; var r = Mathf.Clamp(len * .012f, thickness, thickness * 1.8f);
                into.Prism(line, e.a, e.b - e.a, r, len, 3, 1f, 0f, false); made++;
            }
            return made;
        }

        /// Turns every face of the chosen materials into stained glass (the Soul Lowlands): each face is cut into
        /// irregular three-cornered panes no longer than 'size' on a side, and each pane is given one of 'panes' by
        /// 'pick' (its middle, the way it faces, and the colour the face was; by default at random). The pane materials draw
        /// the lead themselves: every pane's corners are given the texture corners (0,0), (1,0), (0,1), and
        /// LeadTexture is dark along those three edges. Bevel skin stays as it was, a dark seam along every box edge.
        public int StainedGlass(Material[] panes, float size, System.Predicate<Material> which, System.Func<Vector3, Vector3, Color, int> pick = null)
        {
            var made = 0; var keys = new List<Material>(tris.Keys); var lists = new List<int>[panes.Length];
            for (var i = 0; i < panes.Length; i++) if (!tris.TryGetValue(panes[i], out lists[i])) tris[panes[i]] = lists[i] = new List<int>();
            static float Hash(Vector3 p) { var h = Mathf.Sin(p.x * 12.9898f + p.y * 78.233f + p.z * 37.719f) * 43758.5453f; return h - Mathf.Floor(h); }
            void Cut(Vector3 a, Vector3 b, Vector3 c, int depth, Color was)
            {
                float ab = (b - a).sqrMagnitude, bc = (c - b).sqrMagnitude, ca = (a - c).sqrMagnitude; var longest = Mathf.Max(ab, Mathf.Max(bc, ca));
                if (longest <= size * size || depth > 12)
                {
                    var mid = (a + b + c) / 3f; var n = Vector3.Cross(b - a, c - a).normalized;
                    var k = pick != null ? pick(mid, n, was) : (int)(Hash(mid) * panes.Length); k = Mathf.Clamp(k, 0, panes.Length - 1); var l = lists[k];
                    l.Add(verts.Count); verts.Add(a); uvs.Add(new Vector2(0, 0)); l.Add(verts.Count); verts.Add(b); uvs.Add(new Vector2(1, 0)); l.Add(verts.Count); verts.Add(c); uvs.Add(new Vector2(0, 1));
                    if (cols != null) { cols.Add(Color.white); cols.Add(Color.white); cols.Add(Color.white); }
                    made++; return;
                }
                // split the longest side a little off its middle (the same place whichever face is asked), keeping the winding
                if (bc >= ab && bc >= ca) (a, b, c) = (b, c, a); else if (ca >= ab && ca >= bc) (a, b, c) = (c, a, b);
                var m = Vector3.Lerp(a, b, .38f + .24f * Hash(a + b));
                Cut(a, m, c, depth + 1, was); Cut(m, b, c, depth + 1, was);
            }
            foreach (var mat in keys)
            {
                if (System.Array.IndexOf(panes, mat) >= 0 || !which(mat)) continue;
                var l = tris[mat]; var old = l.ToArray(); l.Clear(); var tint = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor").linear : Color.white;
                for (var i = 0; i < old.Length; i += 3)
                {
                    if (skin != null && skin.Contains(old[i])) { l.Add(old[i]); l.Add(old[i + 1]); l.Add(old[i + 2]); continue; }
                    Cut(verts[old[i]], verts[old[i + 1]], verts[old[i + 2]], 0, cols != null ? cols[old[i]] * tint : tint);
                }
            }
            lastSet = false;
            return made;
        }

        static Texture2D leadTex;
        /// The texture a stained-glass pane wears: bright in the middle, dark lead along its three edges.
        public static Texture2D LeadTexture
        {
            get
            {
                if (leadTex != null) return leadTex;
                const int S = 64; leadTex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Lead", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var y = 0; y < S; y++) for (var x = 0; x < S; x++)
                {
                    float u = (x + .5f) / S, v = (y + .5f) / S; var d = Mathf.Min(u, Mathf.Min(v, 1f - u - v));
                    var g = d <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.014f, .05f, d)) * (.72f + .28f * Mathf.Clamp01(d / .28f));   // (Unity's SmoothStep blends between its first two numbers: it is not the shader one)
                    leadTex.SetPixel(x, y, new Color(g, g, g, 1f));
                }
                leadTex.Apply(true); return leadTex;
            }
        }

        /// Makes the mesh as one object, never split (for a mesh that is moved as a whole, like a cloud, or whose
        /// renderer the caller needs).
        public GameObject BuildSingle(string name, Transform parent, bool collider) => verts.Count == 0 ? null : BuildOne(name, parent, collider);

        GameObject BuildChunks(string name, Transform parent, bool collider)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.isStatic = true;
            var chunks = new Dictionary<(int, int), MeshKit>();
            foreach (var kv in tris)
            {
                var list = kv.Value;
                for (var i = 0; i < list.Count; i += 3)
                {
                    Vector3 a = verts[list[i]], b = verts[list[i + 1]], c = verts[list[i + 2]];
                    var centre = (a + b + c) / 3f;
                    var key = (Mathf.FloorToInt(centre.x / ChunkSize), Mathf.FloorToInt(centre.z / ChunkSize));
                    if (!chunks.TryGetValue(key, out var mk)) chunks[key] = mk = new MeshKit();
                    if (!mk.tris.TryGetValue(kv.Key, out var t)) mk.tris[kv.Key] = t = new List<int>();
                    if (skin != null && skin.Contains(list[i])) (mk.skin ??= new HashSet<int>()).Add(mk.verts.Count);
                    t.Add(mk.verts.Count); mk.verts.Add(a); mk.uvs.Add(uvs[list[i]]);
                    t.Add(mk.verts.Count); mk.verts.Add(b); mk.uvs.Add(uvs[list[i + 1]]);
                    t.Add(mk.verts.Count); mk.verts.Add(c); mk.uvs.Add(uvs[list[i + 2]]);
                    if (cols != null) { mk.cols ??= new List<Color>(); mk.cols.Add(cols[list[i]]); mk.cols.Add(cols[list[i + 1]]); mk.cols.Add(cols[list[i + 2]]); }
                }
            }
            foreach (var kv in chunks) kv.Value.BuildOne($"{name} {kv.Key.Item1},{kv.Key.Item2}", root.transform, collider);
            return root;
        }

        /// Makes an invisible mesh that only blocks movement (walls, benches, firewood).
        public GameObject BuildCollider(string name, Transform parent)
        {
            if (verts.Count == 0) return null;
            var mesh = ColliderMesh(name);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            return go;
        }

        /// The mesh for a collider: every triangle but the skin, only the corners those use.
        Mesh ColliderMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            var all = new List<int>();
            if (skin == null) { mesh.SetVertices(verts); foreach (var kv in tris) all.AddRange(kv.Value); }
            else
            {
                var vs = new List<Vector3>(verts.Count);
                foreach (var kv in tris)
                {
                    var l = kv.Value;
                    for (var i = 0; i < l.Count; i += 3)
                    {
                        if (skin.Contains(l[i])) continue;
                        all.Add(vs.Count); vs.Add(verts[l[i]]); all.Add(vs.Count); vs.Add(verts[l[i + 1]]); all.Add(vs.Count); vs.Add(verts[l[i + 2]]);
                    }
                }
                mesh.SetVertices(vs);
            }
            mesh.SetTriangles(all, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// Makes the mesh as a child of 'parent' (in world coordinates). Returns null if nothing was added.
        /// A mesh spread over a whole land is split into chunks (children of the returned object), so Unity
        /// only draws the parts in view and LandCuller can hide far ones: one land-wide mesh is drawn whole
        /// whenever any corner of it is on screen.
        public GameObject Build(string name, Transform parent, bool collider)
        {
            if (verts.Count == 0) return null;
            Vector3 lo = verts[0], hi = verts[0];
            foreach (var p in verts) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
            if (hi.x - lo.x > ChunkAbove || hi.z - lo.z > ChunkAbove) return BuildChunks(name, parent, collider);
            return BuildOne(name, parent, collider);
        }

        GameObject BuildOne(string name, Transform parent, bool collider)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            if (cols != null && cols.Count == verts.Count) mesh.SetColors(cols);
            mesh.subMeshCount = tris.Count;
            var mats = new Material[tris.Count];
            var i = 0;
            foreach (var kv in tris) { mesh.SetTriangles(kv.Value, i); mats[i++] = kv.Key; }
            mesh.RecalculateNormals(); // separate vertices per triangle, so the shading is flat and faceted
            foreach (var m in mats) if (RichSurfaces.IsRich(m)) { mesh.RecalculateTangents(); break; }   // rich surfaces' relief needs them
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = skin == null ? mesh : ColliderMesh(name + " (solid)");
            go.isStatic = true;
            return go;
        }
    }
}
