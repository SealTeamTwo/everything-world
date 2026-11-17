using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// One size for everything written on the screen, whatever the screen. Every OnGUI starts with Ui.Begin() and lays
    /// itself out on a sheet Ui.W wide and Ui.H high (never Screen.width and Screen.height): the sheet is always 1080
    /// units tall, and is scaled to fill the real screen, so a box that is 34 units high is the same share of the
    /// picture at 1080p, 1440p and 4K (it used to be 34 pixels on all of them, and tiny on the big ones).
    /// On a small window the sheet stops shrinking at three quarters size, so the words stay readable.
    ///
    /// It also gives every box the colour of the land you are in: the panel stays dark (so it never melts into the
    /// ground behind it) and the line round it takes the land's most vivid colour, kept bright (Accent).
    public static class Ui
    {
        public const float Design = 1080f;
        public static float Scale => Mathf.Clamp(Screen.height / Design, .75f, 4f);
        public static float W => Screen.width / Scale;
        public static float H => Screen.height / Scale;

        static readonly Color Plain = new Color(1f, .9f, .72f);
        static string land = ""; static Color accent = Plain, shown = Plain; static int polled = -1;
        /// The land you are in's colour for lines and titles: its most vivid colour, lifted until it is bright.
        public static Color Accent => shown;
        /// The land you are in (or were last in), its realm and the one line that says how it feels.
        public static string Land => land;
        public static string Realm { get; private set; } = "";
        public static string Mood { get; private set; } = "";
        /// When you last walked into a land (Time.time), for the arrival card.
        public static float ArrivedAt { get; private set; } = -99f;

        /// Call first thing in OnGUI.
        public static void Begin()
        {
            var s = Scale; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            if (polled != Time.frameCount) { polled = Time.frameCount; Poll(); }
            GUI.backgroundColor = shown;
        }

        static void Poll()
        {
            shown = Color.Lerp(shown, accent, 1f - Mathf.Exp(-3f * Time.deltaTime));
            var now = SkyDirector.Instance != null ? SkyDirector.Instance.CurrentLand : null;
            if (string.IsNullOrEmpty(now) || now == land) return;
            var data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null; var cat = MusicDirector.Instance != null ? MusicDirector.Instance.Catalog : null;
            if (data == null) return;
            land = now; Realm = ""; Mood = ""; accent = Plain;
            foreach (var isl in data.layout.islands)
            {
                if (isl.name != now) continue;
                var scene = cat != null ? cat.Get(isl.sceneId) : null; Realm = isl.sceneId == 1000 ? "" : isl.realm ?? ""; Mood = scene != null && scene.mood != null ? scene.mood : "";
                var c = ZoneFields.VividColor(scene?.palette, Plain); Color.RGBToHSV(c, out var h, out var sat, out _);
                accent = Color.HSVToRGB(h, Mathf.Clamp(sat, .35f, .7f), 1f);       // bright whatever the land's colour, and never so strong it is hard to read
                break;
            }
            ArrivedAt = Time.time;
        }

        /// A point on the real screen (pixels, from the bottom left, as a camera gives it) as a point on the sheet.
        public static Vector2 FromScreen(Vector3 screenPoint) { var s = Scale; return new Vector2(screenPoint.x / s, (Screen.height - screenPoint.y) / s); }
    }

    /// The card that comes up as you walk into a land: its realm in small letters, its name large in the land's own
    /// colour, a line that draws itself out from the middle, and the one line that says how the land feels. It fades in,
    /// stays a few seconds and fades away. Not shown over a menu or while you are on an activity.
    public class ArrivalCard : MonoBehaviour
    {
        const float In = .5f, Hold = 3.4f, Out = 1f;
        GUIStyle title, small, mood; string last = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { var go = new GameObject("Arrival card"); DontDestroyOnLoad(go); go.AddComponent<ArrivalCard>(); }

        void OnGUI()
        {
            Ui.Begin();
            if (Event.current.type != EventType.Repaint) return;
            var t = Time.time - Ui.ArrivedAt; if (t < 0f || t > In + Hold + Out || string.IsNullOrEmpty(Ui.Land)) return;
            if (EverythingWorld.Player.MainMenu.Showing || EverythingWorld.Activities.Activity.Any) return;
            title ??= new GUIStyle { font = GameFonts.Sign, fontSize = 76, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow, normal = { textColor = Color.white } };
            small ??= new GUIStyle { font = GameFonts.Speech, fontSize = 22, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow, normal = { textColor = Color.white } };
            mood ??= new GUIStyle { font = GameFonts.Speech, fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Italic, normal = { textColor = Color.white } };
            var a = t < In ? t / In : t < In + Hold ? 1f : 1f - (t - In - Hold) / Out; a = a * a * (3f - 2f * a);
            var rise = (1f - Mathf.Clamp01(t / In)) * 18f; float cx = Ui.W / 2f, y = Ui.H * .17f + rise; const float w = 1100f;
            var keep = GUI.color; var white = Texture2D.whiteTexture; var acc = Ui.Accent;
            // a soft dark band behind the words, so they read over any land (sand, snow or sky)
            for (var k = 0; k < 6; k++) { GUI.color = new Color(.03f, .03f, .07f, a * .11f); var grow = k * 16f; GUI.DrawTexture(new Rect(cx - w / 2f - grow, y - 26f - grow * .35f, w + grow * 2f, 226f + grow * .7f), white); }
            var realm = Spaced(Ui.Realm.ToUpperInvariant());
            if (realm.Length > 0) { Shadowed(new Rect(cx - w / 2f, y, w, 30f), realm, small, new Color(.95f, .93f, .88f, a * .9f), a); }
            Shadowed(new Rect(cx - w / 2f, y + 34f, w, 96f), Ui.Land, title, new Color(acc.r, acc.g, acc.b, a), a);
            // the line draws itself out from the middle
            var grown = Mathf.Clamp01((t - .15f) / .9f); grown = 1f - (1f - grown) * (1f - grown); var lw = 520f * grown;
            GUI.color = new Color(acc.r, acc.g, acc.b, a); GUI.DrawTexture(new Rect(cx - lw / 2f, y + 136f, lw, 3f), white); GUI.DrawTexture(new Rect(cx - 5f, y + 132f, 10f, 10f), white);
            if (Ui.Mood.Length > 0) Shadowed(new Rect(cx - 430f, y + 150f, 860f, 60f), Ui.Mood, mood, new Color(.96f, .95f, .9f, a * Mathf.Clamp01((t - .5f) / .6f)), a);
            GUI.color = keep;
        }

        static string Spaced(string s) { var sb = new System.Text.StringBuilder(); foreach (var ch in s) { sb.Append(ch); sb.Append(' '); } return sb.ToString().TrimEnd(); }
        static void Shadowed(Rect r, string text, GUIStyle style, Color c, float a)
        {
            GUI.color = new Color(0f, 0f, 0f, a * .75f); GUI.Label(new Rect(r.x + 2f, r.y + 3f, r.width, r.height), text, style);
            GUI.color = c; GUI.Label(r, text, style);
        }
    }
}
