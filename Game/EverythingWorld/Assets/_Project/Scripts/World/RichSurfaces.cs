using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Rich surfaces (2026-11-15, tried on Lanternwood first): each surface kind gets a detailed texture with relief
    /// (a normal map), so light catches the wood grain and the gaps between boards, the cracks in stone, the furrows
    /// in bark and the weave of cloth. Same colours as before: the texture is greyscale and averages 1, and the colour
    /// still comes from the mesh. Made in code at start-up (256 x 256 per kind, tiling), nothing imported.
    public static class RichSurfaces
    {
        /// Build rich surfaces into meshes made while this is on (set by IslandWorldBuilder while a land is built).
        public static bool On;

        const int N = 256;
        static readonly Dictionary<Surface, Material> mats = new Dictionary<Surface, Material>();
        static readonly HashSet<Material> all = new HashSet<Material>();

        public static bool Has(Surface k) => k == Surface.Stone || k == Surface.Wood || k == Surface.Bark || k == Surface.Cloth
                                            || k == Surface.Metal || k == Surface.Painted || k == Surface.Foliage || k == Surface.Paper;
        /// A mesh drawn with this needs tangents (for its normal map).
        public static bool IsRich(Material m) => m != null && all.Contains(m);

        /// Metres per repeat of each kind's texture (MeshKit's coordinates repeat every GrainScale metres).
        static float Repeat(Surface k) => k switch
        {
            Surface.Stone => 3f, Surface.Wood => 2.4f, Surface.Bark => 1.6f, Surface.Cloth => 1.2f,
            Surface.Metal => 2.5f, Surface.Painted => 2.5f, Surface.Foliage => 1.8f, Surface.Paper => 2.2f, _ => 5f,
        };
        /// How strongly the relief bends the light.
        static float Bump(Surface k) => k switch
        {
            Surface.Stone => .9f, Surface.Wood => .75f, Surface.Bark => 1f, Surface.Cloth => .55f,
            Surface.Metal => .35f, Surface.Painted => .3f, Surface.Foliage => .8f, Surface.Paper => .45f, _ => .5f,
        };

        /// The rich version of a kind's shared material (made from it, so it keeps its finish and shading).
        public static Material For(Surface k, Material plainKind)
        {
            if (mats.TryGetValue(k, out var m)) return m;
            m = new Material(plainKind) { name = "Rich surface: " + k };
            Make(k.ToString(), (float u, float v, out float h, out float sh) => Sample(k, u, v, out h, out sh), out var albedo, out var normal);
            m.SetTexture("_BaseMap", albedo);
            var s = MeshKit.GrainScale / Repeat(k); m.SetTextureScale("_BaseMap", new Vector2(s, s));
            m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", Bump(k)); m.EnableKeyword("_NORMALMAP");
            all.Add(m);
            return mats[k] = m;
        }

        // ------------------------------------------------------------ the ground
        /// What a patch of ground is: each has its own texture (the colour still comes from the land).
        public enum GroundKind { Grass, Earth, Sand, Rock, Snow }
        static readonly Dictionary<GroundKind, Material> groundMats = new Dictionary<GroundKind, Material>();

        /// Guess what ground of this colour and steepness (metres of rise across one 8 m triangle) is made of.
        public static GroundKind GroundOf(Color c, float slope)
        {
            Color.RGBToHSV(c, out var hue, out var sat, out var val);
            if (slope > 6.5f) return GroundKind.Rock;
            if (sat < .2f && val > .6f) return GroundKind.Snow;              // snow, frost, pale ice
            if (hue > .45f && hue < .7f && sat < .4f && val > .55f) return GroundKind.Snow;   // bluish ice
            if (sat < .2f) return GroundKind.Rock;                            // grey: stone, gravel, ash
            if (hue >= .17f && hue <= .47f) return GroundKind.Grass;          // greens: grass, moss
            if (hue >= .09f && hue < .17f && val > .5f) return GroundKind.Sand;   // pale yellows: sand
            return GroundKind.Earth;                                          // browns, reds, purples: soil, clay, peat
        }

        static float Repeat(GroundKind g) => g switch { GroundKind.Grass => 3.5f, GroundKind.Earth => 4f, GroundKind.Sand => 5f, GroundKind.Rock => 6f, _ => 6f };
        static float Bump(GroundKind g) => g switch { GroundKind.Grass => .9f, GroundKind.Earth => .95f, GroundKind.Sand => .6f, GroundKind.Rock => 1.1f, _ => .9f };

        /// The rich ground material of a kind, made from the plain ground material (vertex colours, same shading).
        public static Material Ground(GroundKind g, Material plainGround)
        {
            if (groundMats.TryGetValue(g, out var m)) return m;
            m = new Material(plainGround) { name = "Rich ground: " + g };
            Make("ground " + g, (float u, float v, out float h, out float sh) => SampleGround(g, u, v, out h, out sh), out var albedo, out var normal);
            m.SetTexture("_BaseMap", albedo);
            var s = MeshKit.GrainScale / Repeat(g); m.SetTextureScale("_BaseMap", new Vector2(s, s));
            m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", Bump(g)); m.EnableKeyword("_NORMALMAP");
            all.Add(m);
            return groundMats[g] = m;
        }

        static void SampleGround(GroundKind g, float u, float v, out float h, out float shade)
        {
            switch (g)
            {
                case GroundKind.Grass:
                {
                    // tufts packed together, lighter and darker patches, the odd bare spot
                    var (f1, f2, id) = Cells(u, v, 64, 201); var tuft = Mathf.Clamp01(1f - f1 * 1.2f);
                    var patch = Fbm(u, v, 4, 4, 203); var blades = Fbm(u * .5f, v, 96, 2, 205); var bare = Smooth(.68f, .74f, Fbm(u, v, 8, 3, 207));
                    h = tuft * .4f + blades * .3f - bare * .3f;
                    shade = (patch - .5f) * .42f + (id - .5f) * .14f + (tuft - .5f) * .14f + (blades - .5f) * .08f - bare * .12f;
                    break;
                }
                case GroundKind.Earth:
                {
                    // soil in clods, scattered pebbles
                    var clod = Fbm(u, v, 12, 4, 211); var (f1, _, id) = Cells(u, v, 22, 213); var pebble = id > .72f ? Mathf.Clamp01(1f - f1 * 3f) : 0f;
                    h = clod * .6f + pebble * .8f;
                    shade = (clod - .5f) * .28f + (Fbm(u, v, 3, 3, 215) - .5f) * .2f + pebble * .1f;
                    break;
                }
                case GroundKind.Sand:
                {
                    // wind ripples and fine grains
                    var warp = (Fbm(u, v, 3, 3, 221) - .5f) * 1.6f; var ripple = Mathf.Sin((u * 18f + v * 5f + warp) * Mathf.PI * 2f) * .5f + .5f;
                    var grains = Hash(Mathf.FloorToInt(u * N), Mathf.FloorToInt(v * N), 223);
                    h = ripple * .5f + grains * .08f;
                    shade = (ripple - .5f) * .1f + (grains - .5f) * .1f + (Fbm(u, v, 4, 3, 225) - .5f) * .14f;
                    break;
                }
                case GroundKind.Rock:
                {
                    // weathered rock: rough lumps, a few long cracks
                    var (f1, f2, id) = Cells(u + (Fbm(u, v, 6, 2, 231) - .5f) * .15f, v + (Fbm(u, v, 6, 2, 232) - .5f) * .15f, 4, 233); var crack = (1f - Smooth(.015f, .06f, f2 - f1)) * Smooth(.42f, .55f, Fbm(u, v, 3, 2, 237));
                    var rough = Fbm(u, v, 8, 5, 235);
                    h = rough * .9f - crack * .5f;
                    shade = (rough - .5f) * .3f + (id - .5f) * .08f - crack * .18f;
                    break;
                }
                default:
                {
                    // snow: soft drifts, ridges carved by the wind (in patches), a glint here and there
                    var drift = Fbm(u, v, 4, 4, 241);
                    var warp = (Fbm(u, v, 3, 3, 243) - .5f) * 1.2f; var ridge = Mathf.Pow(Mathf.Sin((u * 3f + v * 11f + warp) * Mathf.PI * 2f) * .5f + .5f, 3f) * Smooth(.4f, .65f, Fbm(u, v, 3, 2, 247));
                    var glint = Hash(Mathf.FloorToInt(u * N), Mathf.FloorToInt(v * N), 245) > .985f ? 1f : 0f;
                    h = drift * .9f + ridge * .45f + Fbm(u, v, 32, 2, 249) * .1f;
                    shade = (drift - .5f) * .16f + (ridge - .3f) * .08f + glint * .25f;
                    break;
                }
            }
        }

        // ------------------------------------------------------------ tiling noise (u, v in 0..1, repeating)
        static float Hash(int x, int y, int seed)
        {
            unchecked { var h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041); h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16; return (h & 0xFFFFFF) / (float)0xFFFFFF; }
        }
        static int Wrap(int i, int p) => ((i % p) + p) % p;
        /// Smooth value noise with 'p' cells across the tile, 0..1.
        static float Noise(float u, float v, int p, int seed)
        {
            float x = u * p, y = v * p; int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y); float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(Wrap(x0, p), Wrap(y0, p), seed), b = Hash(Wrap(x0 + 1, p), Wrap(y0, p), seed), c = Hash(Wrap(x0, p), Wrap(y0 + 1, p), seed), d = Hash(Wrap(x0 + 1, p), Wrap(y0 + 1, p), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
        /// Layers of noise, the first with 'p' cells, each next twice as fine and half as strong. 0..1.
        static float Fbm(float u, float v, int p, int octaves, int seed)
        {
            float s = 0f, w = .5f, tw = 0f;
            for (var o = 0; o < octaves; o++) { s += Noise(u, v, p, seed + o * 31) * w; tw += w; w *= .5f; p *= 2; }
            return s / tw;
        }
        /// Cells round scattered points ('p' x 'p' across the tile): distance to the nearest and second nearest point
        /// (in cell units) and the nearest cell's random number.
        static (float f1, float f2, float id) Cells(float u, float v, int p, int seed)
        {
            float x = u * p, y = v * p; int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y); float f1 = 9f, f2 = 9f, id = 0f;
            for (var j = -1; j <= 1; j++) for (var i = -1; i <= 1; i++)
            {
                int gx = cx + i, gy = cy + j, wx = Wrap(gx, p), wy = Wrap(gy, p);
                float px = gx + .15f + .7f * Hash(wx, wy, seed), py = gy + .15f + .7f * Hash(wx, wy, seed + 7);
                var d = Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py));
                if (d < f1) { f2 = f1; f1 = d; id = Hash(wx, wy, seed + 13); } else if (d < f2) f2 = d;
            }
            return (f1, f2, id);
        }
        static float Smooth(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        // ------------------------------------------------------------ each kind: height (relief) and shade (around 0)
        static void Sample(Surface k, float u, float v, out float h, out float shade)
        {
            switch (k)
            {
                case Surface.Stone:
                {
                    // stones fitted together: rough faces, darker cracks between them, each stone its own shade
                    var (f1, f2, id) = Cells(u + (Fbm(u, v, 8, 2, 5) - .5f) * .03f, v, 6, 11);
                    var crack = 1f - Smooth(.02f, .09f, f2 - f1);
                    var rough = Fbm(u, v, 16, 4, 21);
                    h = rough * .55f + Smooth(0f, .5f, f2 - f1) * .35f - crack * .9f;
                    shade = (id - .5f) * .16f + (rough - .5f) * .22f - crack * .32f + (Fbm(u, v, 64, 2, 29) - .5f) * .1f;
                    break;
                }
                case Surface.Wood:
                {
                    // boards along u: eight per tile, each its own shade and with its own end joint, grain lines
                    // swirling round the odd knot, dark gaps between boards
                    const int Boards = 8; var bi = Mathf.FloorToInt(v * Boards); var fv = v * Boards - bi; bi = Wrap(bi, Boards);
                    var off = Hash(bi, 0, 41); var fu = u * 2f + off; var joint = fu - Mathf.Floor(fu);
                    var gap = Mathf.Max(1f - Smooth(0f, .07f, fv), 1f - Smooth(0f, .07f, 1f - fv), 1f - Smooth(0f, .012f, Mathf.Min(joint, 1f - joint)));
                    float ku = Hash(bi, 1, 43), kv = .3f + .4f * Hash(bi, 2, 47), kd = Mathf.Sqrt(Mathf.Pow(Mathf.DeltaAngle(u * 360f, ku * 360f) / 360f * 3f, 2) + Mathf.Pow(fv - kv, 2)); var knot = Hash(bi, 3, 53) < .3f ? Mathf.Clamp01(1f - kd * 9f) : 0f;
                    var warp = (Fbm(u, v, 4, 3, 61) - .5f) * 2.2f + knot * 2.5f;
                    var grain = Mathf.Sin((v * 72f + warp) * Mathf.PI * 2f) * .5f + .5f;
                    var fine = Fbm(u * 1f, v, 128, 2, 67);
                    h = grain * .22f + (Hash(bi, 4, 71) - .5f) * .2f + fine * .1f - gap * 1.1f - knot * .2f;
                    shade = (Hash(bi, 5, 73) - .5f) * .2f + (grain - .5f) * .2f + (fine - .5f) * .06f - gap * .45f - knot * .22f;
                    break;
                }
                case Surface.Bark:
                {
                    // deep furrows running up the trunk (v), plates between them
                    var warp = (Fbm(u, v, 6, 3, 81) - .5f) * 1.4f;
                    var ridge = Mathf.Abs(Mathf.Sin((u * 9f + warp) * Mathf.PI));
                    var plates = Fbm(u, v, 24, 3, 83);
                    h = Mathf.Pow(ridge, .55f) * .9f + plates * .2f;
                    shade = (ridge - .6f) * .4f + (plates - .5f) * .14f;
                    break;
                }
                case Surface.Cloth:
                {
                    // a woven cloth: threads over and under, a little unevenness in the dye
                    const int T = 96; float x = u * T, y = v * T; int i = Mathf.FloorToInt(x), j = Mathf.FloorToInt(y); float fx = x - i, fy = y - j;
                    var over = ((i + j) & 1) == 0; var t = over ? Mathf.Sin(fy * Mathf.PI) : Mathf.Sin(fx * Mathf.PI);
                    var dye = Fbm(u, v, 6, 3, 91);
                    h = t * .8f + (over ? .1f : 0f);
                    shade = (t - .6f) * .16f + (dye - .5f) * .12f;
                    break;
                }
                case Surface.Metal:
                {
                    // brushed: fine streaks along u, a soft worn patina, the odd scratch
                    var streak = Fbm(u * .25f, v, 256, 2, 101); var patina = Fbm(u, v, 5, 4, 103);
                    var sc = Mathf.Abs(Mathf.Sin((u * 3f + v * 7f + Noise(u, v, 4, 107)) * Mathf.PI * 2f)); var scratch = Noise(u, v, 8, 109) > .72f ? 1f - Smooth(0f, .015f, sc) : 0f;
                    h = streak * .25f + patina * .25f - scratch * .4f;
                    shade = (streak - .5f) * .1f + (patina - .5f) * .2f + scratch * .15f;
                    break;
                }
                case Surface.Painted:
                {
                    // paint: faint brush strokes, worn lighter in patches, a few chips
                    var stroke = Fbm(u * .2f, v, 64, 2, 121); var wear = Fbm(u, v, 6, 4, 123); var chip = Smooth(.75f, .78f, Fbm(u, v, 24, 3, 127));
                    h = stroke * .2f + wear * .1f - chip * .5f;
                    shade = (stroke - .5f) * .07f + (wear - .5f) * .1f - chip * .2f;
                    break;
                }
                case Surface.Foliage:
                {
                    // leaves: rounded overlapping blobs, each its own shade, darker where they meet
                    var (f1, f2, id) = Cells(u, v, 14, 131); var (g1, _, id2) = Cells(u + .37f, v + .21f, 22, 137);
                    var leaf = Mathf.Clamp01(1f - f1 * 1.3f); var leaf2 = Mathf.Clamp01(1f - g1 * 1.4f); var edge = 1f - Smooth(0f, .12f, f2 - f1);
                    h = Mathf.Max(leaf, leaf2 * .8f) - edge * .15f;
                    shade = (id - .5f) * .2f + (id2 - .5f) * .1f + (leaf - .5f) * .2f - edge * .1f;
                    break;
                }
                case Surface.Paper:
                {
                    // paper: fine fibres running every way, a soft mottle, and a couple of gentle creases where it was folded
                    var fib = Fbm(u, v, 128, 2, 141); var mott = Fbm(u, v, 5, 3, 145);
                    var c1 = Mathf.Abs(Mathf.Repeat(u + v, 1f) - .5f); var c2 = Mathf.Abs(Mathf.Repeat(2f * v - u + .3f, 1f) - .5f);   // whole-number slopes, so the creases tile
                    var crease = (1f - Smooth(0f, .012f, c1)) * .6f + (1f - Smooth(0f, .01f, c2)) * .4f;
                    h = fib * .25f + mott * .2f + crease * .5f;
                    shade = (fib - .5f) * .06f + (mott - .5f) * .05f - crease * .03f;
                    break;
                }
                default: h = 0f; shade = 0f; break;
            }
        }

        delegate void Sampler(float u, float v, out float h, out float shade);
        static void Make(string name, Sampler sample, out Texture2D albedo, out Texture2D normal)
        {
            var h = new float[N * N]; var s = new float[N * N];
            // every row on its own core: the noise is plain arithmetic, nothing shared
            System.Threading.Tasks.Parallel.For(0, N, y =>
            {
                for (var x = 0; x < N; x++) { sample((x + .5f) / N, (y + .5f) / N, out var hh, out var ss); h[y * N + x] = hh; s[y * N + x] = ss; }
            });
            var mean = 0f; foreach (var e in s) mean += e; mean /= N * N;
            var px = new Color[N * N];
            for (var i = 0; i < px.Length; i++) { var g = Mathf.Max(.25f, 1f + s[i] - mean); px[i] = new Color(g, g, g, 1f); }
            albedo = new Texture2D(N, N, TextureFormat.RGBAHalf, true, true) { name = "Rich " + name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            albedo.SetPixels(px); albedo.Apply(true, true);
            // the normal map from the relief's slope (wrapping round the edges, so it tiles)
            const float Depth = 6f;   // how many texels' rise the full relief is
            var np = new Color32[N * N];
            for (var y = 0; y < N; y++) for (var x = 0; x < N; x++)
            {
                float dx = (h[y * N + Wrap(x + 1, N)] - h[y * N + Wrap(x - 1, N)]) * .5f * Depth, dy = (h[Wrap(y + 1, N) * N + x] - h[Wrap(y - 1, N) * N + x]) * .5f * Depth;
                var n = new Vector3(-dx, -dy, 1f).normalized;
                np[y * N + x] = new Color32((byte)Mathf.RoundToInt((n.x * .5f + .5f) * 255f), (byte)Mathf.RoundToInt((n.y * .5f + .5f) * 255f), (byte)Mathf.RoundToInt((n.z * .5f + .5f) * 255f), 255);
            }
            normal = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { name = "Rich relief " + name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8 };
            normal.SetPixels32(np); normal.Apply(true, true);
        }
    }
}
