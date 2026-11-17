using UnityEngine;

namespace EverythingWorld.World
{
    /// The game's two typefaces (Resources/Fonts, both under the SIL Open Font License; licences beside them):
    /// - Sign: Fraunces 72pt SemiBold, a warm, characterful serif for signs, plaques, boards and titles;
    /// - Speech: Nunito SemiBold, soft and rounded, for what people say and the text on the screen.
    /// Falls back to Unity's built-in font if a file is missing.
    public static class GameFonts
    {
        static Font sign, speech, builtin;
        static Font Builtin => builtin != null ? builtin : builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Sign { get { if (sign == null) { sign = Resources.Load<Font>("Fonts/Fraunces72pt-SemiBold"); if (sign == null) sign = Builtin; } return sign; } }
        public static Font Speech { get { if (speech == null) { speech = Resources.Load<Font>("Fonts/Nunito-SemiBold"); if (speech == null) speech = Builtin; } return speech; } }

        static Texture2D panel;
        /// The panel behind every box of text: deep ink blue, rounded corners, a thin warm line round it and a
        /// brighter one along the top (drawn once, stretched from its corners so it keeps its shape at any size).
        static Texture2D Panel()
        {
            if (panel != null) return panel;
            const int n = 40; const float r = 8f; panel = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Panel", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            // (kept without a colour of its own: Ui.Begin tints every box with the land's colour, which turns the fill a dark shade of it and the line a bright one)
            var fill = new Color(.1f, .1f, .12f, .92f); var line = new Color(1f, 1f, 1f, .7f); var top = new Color(1f, 1f, 1f, .95f);
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            {
                // how far inside the rounded rectangle this pixel is (negative: outside)
                float cx = Mathf.Clamp(x + .5f, r, n - r), cy = Mathf.Clamp(y + .5f, r, n - r); var d = r - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(cx, cy));
                var edge = Mathf.Clamp01(d); var c = d < 2.2f ? Color.Lerp(line, top, Mathf.Clamp01((y - n * .5f) / (n * .5f))) : fill; c.a *= edge;
                if (d >= 2.2f && d < 3.2f) c = Color.Lerp(c, fill, d - 2.2f);
                panel.SetPixel(x, y, c);
            }
            panel.Apply(); return panel;
        }

        /// A box for text on the screen: Nunito on the game's panel, with room round the words. Every box in the game is
        /// made here, so they all look alike; sizes are in the screen sheet's units (World/Ui.cs), not pixels.
        public static GUIStyle Box(int size, TextAnchor anchor, bool wrap = false, bool signFont = false)
        {
            var s = new GUIStyle(GUI.skin.box) { font = signFont ? Sign : Speech, fontSize = size, alignment = anchor, wordWrap = wrap, padding = new RectOffset(16, 16, 8, 8), border = new RectOffset(10, 10, 10, 10), richText = true };
            s.normal.background = Panel(); s.normal.textColor = new Color(.98f, .96f, .9f);
            return s;
        }
    }
}
