using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The giant market's cloth (part of AfroKit, 2026-11-16): bold printed wax-print designs made in code (targets,
    /// spirals, chevrons, rosettes, woven blocks, leaves), each in three colours with strong light-dark contrast, faintly
    /// self-lit so they glow at sunset. Market Road is printed like a bolt of cloth unrolled down the hill; the stall
    /// awnings are printed; great cloths hang on the old stone walls and sway in the sea wind (AfroLife).
    public partial class AfroKit
    {
        static readonly (Color bg, Color a, Color b)[] PrintColours =
        {
            (new Color(.12f, .14f, .42f), new Color(1f, .78f, .15f), new Color(.98f, .96f, .9f)),   // indigo, gold, white
            (new Color(.95f, .45f, .08f), new Color(.08f, .06f, .08f), new Color(.98f, .92f, .78f)), // orange, black, cream
            (new Color(.08f, .45f, .22f), new Color(1f, .86f, .2f), new Color(.06f, .06f, .07f)),   // green, yellow, black
            (new Color(.8f, .12f, .12f), new Color(1f, .8f, .25f), new Color(.14f, .16f, .45f)),    // red, gold, indigo
            (new Color(.42f, .14f, .5f), new Color(1f, .58f, .12f), new Color(.98f, .95f, .88f)),   // purple, orange, cream
            (new Color(.05f, .45f, .48f), new Color(.06f, .06f, .07f), new Color(1f, .82f, .3f)),   // teal, black, gold
            (new Color(.07f, .06f, .08f), new Color(1f, .5f, .1f), new Color(.98f, .96f, .9f)),     // black, orange, white
            (new Color(1f, .8f, .2f), new Color(.75f, .1f, .12f), new Color(.08f, .06f, .08f)),     // gold, red, black
        };
        readonly Dictionary<int, Material> prints = new Dictionary<int, Material>();

        /// Printed cloth number k (any int): one of six designs in one of eight colourings.
        Material PrintM(int k)
        {
            k = ((k % 48) + 48) % 48; if (prints.TryGetValue(k, out var m)) return m;
            const int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { name = "Wax print " + k, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            var (bg, ca, cb) = PrintColours[k % PrintColours.Length]; var design = (k / PrintColours.Length + k) % 6; var px = new Color[n * n];
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                float u = (x + .5f) / n, v = (y + .5f) / n; var c = bg;
                switch (design)
                {
                    case 0:   // targets: rings round a dot, four to a tile
                    {
                        var p = new Vector2(Mathf.Repeat(u * 2f, 1f) - .5f, Mathf.Repeat(v * 2f, 1f) - .5f); var r = p.magnitude;
                        c = r < .08f ? cb : r < .42f ? (Mathf.FloorToInt(r * 14f) % 2 == 0 ? ca : bg) : bg; if (r > .44f && r < .47f) c = cb; break;
                    }
                    case 1:   // spirals
                    {
                        var p = new Vector2(Mathf.Repeat(u * 2f, 1f) - .5f, Mathf.Repeat(v * 2f, 1f) - .5f); var r = p.magnitude; var a = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f);
                        c = r < .45f ? (Mathf.Repeat(r * 9f - a, 1f) < .45f ? ca : bg) : (Mathf.Repeat((u + v) * 8f, 1f) < .2f ? cb : bg); break;
                    }
                    case 2:   // chevrons in bands
                    {
                        var band = Mathf.FloorToInt(v * 6f); var zig = Mathf.Abs(Mathf.Repeat(u * 8f, 1f) - .5f) * .35f; var fv = Mathf.Repeat(v * 6f + zig * 2f, 1f);
                        c = fv < .35f ? (band % 2 == 0 ? ca : cb) : bg; break;
                    }
                    case 3:   // rosettes: six petals round a dot, on scattered dots
                    {
                        var p = new Vector2(Mathf.Repeat(u * 3f, 1f) - .5f, Mathf.Repeat(v * 3f, 1f) - .5f); var r = p.magnitude; var a = Mathf.Atan2(p.y, p.x);
                        var petal = .32f * Mathf.Abs(Mathf.Cos(a * 3f)) + .06f; c = r < .07f ? cb : r < petal ? ca : (Mathf.Repeat(u * 12f, 1f) - .5f) * (Mathf.Repeat(u * 12f, 1f) - .5f) + (Mathf.Repeat(v * 12f, 1f) - .5f) * (Mathf.Repeat(v * 12f, 1f) - .5f) < .03f ? cb : bg; break;
                    }
                    case 4:   // woven blocks: stripes running one way, then the other
                    {
                        var bx = Mathf.FloorToInt(u * 4f); var by = Mathf.FloorToInt(v * 4f); var across = (bx + by) % 2 == 0; var s = across ? Mathf.Repeat(v * 24f, 1f) : Mathf.Repeat(u * 24f, 1f);
                        c = s < .34f ? ca : s < .5f ? cb : bg; if (Mathf.Repeat(u * 4f, 1f) < .04f || Mathf.Repeat(v * 4f, 1f) < .04f) c = cb; break;
                    }
                    default:  // leaves on the slant
                    {
                        var q = new Vector2(u + v, v - u) * 2.5f; var p = new Vector2(Mathf.Repeat(q.x, 1f) - .5f, Mathf.Repeat(q.y, 1f) - .5f);
                        var leaf = (p.x * p.x) / .16f + (p.y * p.y) / .025f; c = leaf < 1f ? (Mathf.Abs(p.y) < .02f ? cb : ca) : bg; break;
                    }
                }
                px[y * n + x] = c;
            }
            tex.SetPixels(px); tex.Apply(true);
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Wax print " + k };
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", .12f);
            m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", new Color(.32f, .32f, .32f));
            m.SetFloat("_Cull", 0f);
            return prints[k] = m;
        }

        readonly Dictionary<(int, float), Material> printsAt = new Dictionary<(int, float), Material>();
        /// The same print laid on something whose texture is projected from the world (a road, an awning): repeating
        /// every 'tile' metres.
        Material PrintWorld(int k, float tile)
        {
            if (printsAt.TryGetValue((k, tile), out var m)) return m;
            m = new Material(PrintM(k)) { name = $"Wax print {k} ({tile:0.#} m)" }; var s = MeshKit.GrainScale / tile; m.SetTextureScale("_BaseMap", new Vector2(s, s));
            return printsAt[(k, tile)] = m;
        }

        /// A great printed cloth hung from a pole: 'top' is the middle of its top edge, it hangs down 'h' and faces 'n';
        /// built as its own piece pivoting on its top edge, so it can sway (AfroLife).
        void HangingCloth(Vector3 top, Vector3 n, float w, float h, int k)
        {
            var m = PrintM(k); var mk = new MeshKit(); const int cols = 6, rows = 8; var right = Vector3.Cross(Vector3.up, n).normalized;
            Vector3 P(int i, int j) { float x = -w / 2f + w * i / cols, y = -h * j / rows; var belly = Mathf.Sin(Mathf.PI * i / cols) * .25f * (j / (float)rows); return Vector3.right * x + Vector3.up * y + Vector3.forward * belly; }
            Vector2 UV(int i, int j) => new Vector2(w * i / cols / 3.2f, (h - h * j / rows) / 3.2f);
            for (var i = 0; i < cols; i++) for (var j = 0; j < rows; j++)
            {
                Vector3 a = P(i, j), b = P(i + 1, j), c = P(i + 1, j + 1), d = P(i, j + 1);
                mk.Tri(m, a, b, c, Vector3.back * 5f, UV(i, j), UV(i + 1, j), UV(i + 1, j + 1)); mk.Tri(m, a, c, d, Vector3.back * 5f, UV(i, j), UV(i + 1, j + 1), UV(i, j + 1));
            }
            var go = mk.BuildSingle("Hanging cloth", life.transform, false); go.transform.SetPositionAndRotation(top, Quaternion.LookRotation(n));
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            life.cloths.Add(go.transform); life.clothRest.Add(go.transform.rotation);
        }
    }
}
