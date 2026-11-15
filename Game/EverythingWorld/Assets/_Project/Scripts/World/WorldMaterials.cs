using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// What a surface is made of: each kind catches the light its own way and has its own fine texture (see
    /// WorldMaterials.Surface). Plain = the old one-material-per-colour look.
    public enum Surface { Plain, Stone, Wood, Bark, Cloth, Metal, Painted, Glass, Foliage }

    /// Shared materials for everything built in code: plain colours, glowing colours and pictures.
    /// One material per colour, reused everywhere, so many objects can share a draw call.
    public static class WorldMaterials
    {
        /// Draw surfaces of a kind (Surface(colour, kind)) with one shared material per kind, the colour carried in
        /// the mesh (IslandWorldBuilder "Surface Kinds"; needs Toon shading, else they fall back to plain colours).
        public static bool UseSurfaces = true;
        /// Use the Toon Shaders Pro shader for plain colours when it's imported (set by IslandWorldBuilder).
        public static bool UseToon;
        /// Put a faint grain on plain colours (IslandWorldBuilder "Surface Grain"). Meshes made with MeshKit get
        /// texture coordinates projected from the world, so the grain is the same size everywhere and lines up
        /// across neighbouring pieces; a mesh without coordinates reads the one pure-white texel and looks as before.
        public static bool UseGrain;
        static Texture2D grain;
        /// The grain: 64 x 64 texels, about 13 to the metre (MeshKit.GrainScale), crisp rather than smoothed
        /// (the slightly pixelly look of Valheim's low-res textures), soft blotches plus fine speckle, averaging
        /// exactly 1 so it never darkens or lightens a colour overall (also far away, where it averages out).
        public static Texture2D Grain
        {
            get
            {
                if (grain != null) return grain;
                const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBAHalf, true, true) { name = "Surface grain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, anisoLevel = 4 };
                var rng = new System.Random(4242); var px = new Color[n * n]; var sum = 0f;
                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    // blotches that tile (sums of whole-period waves) plus a speck per texel
                    float u = x / (float)n * Mathf.PI * 2f, v = y / (float)n * Mathf.PI * 2f;
                    var blot = Mathf.Sin(u * 2f + 1.3f) * Mathf.Cos(v * 3f + .4f) * .5f + Mathf.Sin((u + v) * 4f + 2.1f) * .3f + Mathf.Cos(u * 5f - v * 2f) * .2f;
                    var g = blot * .045f + ((float)rng.NextDouble() - .5f) * .075f;
                    px[y * n + x] = new Color(g, g, g, 0f); sum += g;
                }
                var mean = sum / (n * n);
                for (var i = 0; i < px.Length; i++) { var g = 1f + px[i].r - mean; px[i] = new Color(g, g, g, 1f); }
                px[0] = Color.white;   // the texel a mesh with no texture coordinates reads
                t.SetPixels(px); t.Apply(true, true);
                return grain = t;
            }
        }
        static Shader lit, toon, unlit;
        static readonly Dictionary<(Color, float), Material> solid = new Dictionary<(Color, float), Material>();
        static readonly Dictionary<(Color, float), Material> glow = new Dictionary<(Color, float), Material>();
        static readonly Dictionary<Texture, Material> pictures = new Dictionary<Texture, Material>();

        static Shader Lit => lit != null ? lit : lit = Shader.Find("Universal Render Pipeline/Lit");
        static Shader Toon => toon != null ? toon : toon = Shader.Find("Toon Shaders Pro/URP/Toon");
        static Shader Unlit => unlit != null ? unlit : unlit = Shader.Find("Universal Render Pipeline/Unlit");

        /// A matte (or slightly shiny) colour.
        public static Material Solid(Color c, float smooth = .08f)
        {
            if (solid.TryGetValue((c, smooth), out var m)) return m;
            var useToon = UseToon && Toon != null;
            m = new Material(useToon ? Toon : Lit) { name = "Solid " + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            if (UseGrain) m.SetTexture("_BaseMap", Grain);
            if (useToon)
            {
                // soft two-tone shading: lit side the true colour, shaded side a cooler, darker version
                m.SetColor("_MiddleTint", new Color(.82f, .82f, .86f));
                m.SetColor("_ShadowTint", new Color(.5f, .52f, .62f, 1f));
                // no rim light: it brightens every surface seen at a grazing angle, so the ground beyond a
                // certain distance turned bright and a dark ring followed the player round
                m.SetColor("_RimColor", Color.black);
                m.SetColor("_SpecularColor", new Color(.05f, .05f, .05f, 1f));
            }
            return solid[(c, smooth)] = m;
        }

        // ---------------------------------------------------------------- surface kinds
        // Surface(colour, kind) hands back a stand-in material for that colour: anything can still draw with it as
        // before, but a MeshKit swaps it for the kind's one shared material and writes the colour into the mesh, so a
        // whole land's wood (whatever its colours) is one material. KindOf tells MeshKit what a stand-in stands for.
        static readonly Dictionary<(Color, Surface), Material> standIns = new Dictionary<(Color, Surface), Material>();
        static readonly Dictionary<Material, (Surface kind, Color linear)> standInOf = new Dictionary<Material, (Surface, Color)>();
        static readonly Dictionary<Surface, Material> kindMats = new Dictionary<Surface, Material>();

        static bool SurfacesOn => UseSurfaces && UseToon && Toon != null;

        /// A colour made of something: stone, wood, bark, cloth, metal, paint, glass or leaves.
        public static Material Surface(Color c, Surface kind)
        {
            if (kind == World.Surface.Plain || !SurfacesOn) return Solid(c, PlainSmooth(kind));
            if (standIns.TryGetValue((c, kind), out var m)) return m;
            m = new Material(Solid(c, PlainSmooth(kind))) { name = $"{kind} {ColorUtility.ToHtmlStringRGB(c)}" };
            standInOf[m] = (kind, c.linear);
            return standIns[(c, kind)] = m;
        }

        static float PlainSmooth(Surface k) => k == World.Surface.Metal ? .55f : k == World.Surface.Glass ? .85f : k == World.Surface.Painted ? .35f : .08f;

        /// If 'm' stands for a colour of some kind: the kind's shared material and the colour (linear, for the mesh).
        public static bool KindOf(Material m, out Material shared, out Color linear)
        {
            shared = null; linear = Color.white;
            if (m == null || standInOf.Count == 0 || !standInOf.TryGetValue(m, out var e)) return false;
            shared = KindMaterial(e.kind); linear = e.linear; return true;
        }

        /// The one material for a kind: Toon shading like every plain colour (same two tones), white, taking its
        /// colour from the mesh, with the kind's own finish and its own texture.
        static Material KindMaterial(Surface k)
        {
            if (kindMats.TryGetValue(k, out var m)) return m;
            m = new Material(Solid(Color.white)) { name = "Surface: " + k };
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_UseVertexColors", 1f); m.EnableKeyword("_USE_VERTEX_COLORS");
            m.SetTexture("_BaseMap", KindTexture(k));
            float smooth = .06f, metal = 0f, boost = 1f;
            switch (k)
            {
                case World.Surface.Wood: smooth = .22f; break;                              // a soft sheen along the boards
                case World.Surface.Painted: smooth = .45f; boost = 1.2f; break;              // an even gloss
                case World.Surface.Metal: smooth = .62f; metal = .75f; boost = 1.6f; break;  // a sharp glint, a darker body that takes the sky's colour
                case World.Surface.Glass: smooth = .88f; metal = .25f; boost = 2.2f; break;  // a bright point of light
                case World.Surface.Cloth: smooth = 0f; break;                                // dead matte
            }
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); m.SetFloat("_SpecularBoost", boost);
            return kindMats[k] = m;
        }

        static readonly Dictionary<Surface, Texture2D> kindTex = new Dictionary<Surface, Texture2D>();
        /// Each kind's fine texture: greyscale, averaging exactly 1 (so colours keep their brightness overall and far
        /// away), 64 x 64 texels over MeshKit.GrainScale (5 m), crisp like the grain. Texel (0,0) is plain white for
        /// meshes without texture coordinates.
        static Texture2D KindTexture(Surface k)
        {
            if (kindTex.TryGetValue(k, out var t)) return t;
            const int n = 64; t = new Texture2D(n, n, TextureFormat.RGBAHalf, true, true) { name = "Surface " + k, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point, anisoLevel = 4 };
            var rng = new System.Random(900 + (int)k); var g = new float[n * n];
            // tiling noise from whole-period waves, so the texture repeats without seams
            float Wave(float u, float v, int a, int b, float ph) => Mathf.Sin((u * a + v * b) * Mathf.PI * 2f + ph);
            var rowShade = new float[n]; for (var i = 0; i < n; i++) rowShade[i] = (float)rng.NextDouble() - .5f;
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n, r = (float)rng.NextDouble() - .5f, val;
                switch (k)
                {
                    case World.Surface.Stone:     // speckle, soft blotches and the odd dark pit
                        val = r * .12f + (Wave(u, v, 3, 2, 1f) * .5f + Wave(u, v, 5, -4, 2f) * .3f) * .05f + (rng.NextDouble() < .03 ? -.14f : 0f); break;
                    case World.Surface.Wood:      // grain along the boards (across the texture), boards of slightly different shades
                        val = Wave(u, v, 0, 13, Wave(u, v, 2, 0, .5f) * 1.6f) * .06f + rowShade[y / 8] * .08f + r * .04f + (y % 8 == 0 ? -.1f : 0f); break;
                    case World.Surface.Bark:      // deep furrows running up the trunk
                        val = Wave(u, v, 11, 0, Wave(u, v, 0, 3, .3f) * 1.2f) * .1f + Wave(u, v, 23, 1, 1.1f) * .05f + r * .06f; break;
                    case World.Surface.Cloth:     // a fine weave
                        val = ((x + y) % 2 == 0 ? .035f : -.035f) + ((x / 2 + y / 2) % 2 == 0 ? .02f : -.02f) + r * .03f; break;
                    case World.Surface.Metal:     // brushed streaks
                        val = rowShade[y] * .07f + r * .025f + Wave(u, v, 1, 2, .7f) * .03f; break;
                    case World.Surface.Foliage:   // soft leafy mottling
                        val = (Wave(u, v, 4, 3, .2f) * .5f + Wave(u, v, -3, 6, 1.7f) * .5f) * .07f + r * .07f; break;
                    case World.Surface.Painted:   // nearly flat: a faint brush texture
                        val = r * .025f + Wave(u, v, 0, 9, .4f) * .012f; break;
                    default: val = 0f; break;     // glass: clear
                }
                g[y * n + x] = val;
            }
            var mean = 0f; foreach (var e in g) mean += e; mean /= g.Length;
            var px = new Color[n * n]; for (var i = 0; i < px.Length; i++) { var c = 1f + g[i] - mean; px[i] = new Color(c, c, c, 1f); }
            px[0] = Color.white;
            t.SetPixels(px); t.Apply(true, true);
            return kindTex[k] = t;
        }

        /// A colour that glows (lantern glass, lit windows, candle flames). strength 1 = softly lit.
        public static Material Glow(Color c, float strength)
        {
            if (glow.TryGetValue((c, strength), out var m)) return m;
            m = new Material(Lit) { name = "Glow " + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * strength);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return glow[(c, strength)] = m;
        }

        /// A picture that ignores lighting, so album covers stay readable at dusk.
        public static Material Picture(Texture t)
        {
            if (t == null) return Solid(new Color(.12f, .12f, .14f));
            if (pictures.TryGetValue(t, out var m)) return m;
            m = new Material(Unlit) { name = "Picture " + t.name };
            m.SetTexture("_BaseMap", t);
            m.SetColor("_BaseColor", new Color(.92f, .92f, .92f));
            return pictures[t] = m;
        }
    }
}
