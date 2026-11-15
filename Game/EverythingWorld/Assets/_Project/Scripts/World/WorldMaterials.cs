using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Shared materials for everything built in code: plain colours, glowing colours and pictures.
    /// One material per colour, reused everywhere, so many objects can share a draw call.
    public static class WorldMaterials
    {
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
