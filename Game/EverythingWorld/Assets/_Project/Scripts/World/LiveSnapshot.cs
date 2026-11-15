using System.IO;
using System.Text;
using EverythingWorld.Music;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// So Claude can see what the owner sees while playing: press P (or F12) and the game view is saved to
    /// Logs/Live/ in the Unity project, with a line in Logs/Live/notes.txt saying where you were: the land,
    /// position, which way you faced, the nearest artist, the song playing, and whether you were indoors.
    /// Then tell Claude "look at my snapshots". Nothing here is committed (Logs is ignored). Pictures older than two
    /// weeks are deleted when the game starts; the notes are kept.
    public class LiveSnapshot : MonoBehaviour
    {
        string dir; int count; float flash; string flashText; bool pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("Live snapshots");
            DontDestroyOnLoad(go);
            go.AddComponent<LiveSnapshot>();
        }

        void Start()
        {
            dir = Path.Combine(Application.dataPath, "..", "Logs", "Live");
            Directory.CreateDirectory(dir);
            // pictures are kept for two weeks, then deleted (the owner's choice, 2026-11-15); notes.txt is kept
            var cutoff = System.DateTime.Now.AddDays(-14);
            foreach (var d in new[] { dir, Path.Combine(dir, "..", "LiveSheets") })
                if (Directory.Exists(d))
                    foreach (var f in Directory.GetFiles(d, "*.png"))
                        try { if (File.GetLastWriteTime(f) < cutoff) File.Delete(f); } catch (IOException) { }
            count = 0;
            foreach (var f in Directory.GetFiles(dir, "*.png")) if (int.TryParse(Path.GetFileName(f).Split('_')[0], out var n) && n > count) count = n;
        }

        void Update()
        {
            flash -= Time.unscaledDeltaTime;
            if (pending) { pending = false; flash = 2f; }   // a frame later, so the message isn't in the picture
            var kb = Keyboard.current;
            if (kb == null || GameKeys.Off || !(kb.pKey.wasPressedThisFrame || kb.f12Key.wasPressedThisFrame)) return;
            var where = Where(); if (where == null) return;
            var stamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var file = $"{++count:000}_{stamp}.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, file));
            var line = $"{file}: {where}";
            File.AppendAllText(Path.Combine(dir, "notes.txt"), line + "\n");
            Debug.Log("Snapshot for Claude: " + line);
            pending = true; flashText = "Snapshot " + count + " saved for Claude";
        }

        /// Where you are right now, in words: the land, position, facing, nearest artist, the song (and how far into
        /// it) and whether you're indoors. Null before there's a camera. Used by snapshots and notes.
        public static string Where()
        {
            var cam = Camera.main; if (cam == null) return null;
            var land = SkyDirector.Instance != null ? SkyDirector.Instance.CurrentLand : "?";
            var p = cam.transform.position; var f = cam.transform.forward;
            string[] dirs = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
            var facing = dirs[(int)(Mathf.Repeat(Mathf.Atan2(f.z, f.x) * Mathf.Rad2Deg + 22.5f, 360f) / 45f) % 8];
            string near = "none"; var nd = float.MaxValue;
            var data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null;
            if (data != null)
                foreach (var a in data.layout.artists)
                {
                    var d = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(p.x, p.z));
                    if (d < nd) { nd = d; near = a.name; }
                }
            var md = MusicDirector.Instance;
            var song = "none";
            if (md != null && md.CurrentSlot.IsValid) { var ms = md.CurrentSlot.PositionMsAt(RadioSchedule.NowSeconds); song = $"{md.CurrentSlot.track.name} by {md.CurrentSlot.track.artist} ({ms / 60000}:{ms / 1000 % 60:00} in)"; }
            var ground = data != null ? data.HeightAt(p.x, p.z) : 0f;
            var sb = new StringBuilder();
            sb.Append($"{land}, at x {p.x:0} z {p.z:0}, eyes {p.y - ground:0.0} m above the ground, facing {facing} ");
            sb.Append($"(looking {(f.y > .25f ? "up" : f.y < -.25f ? "down" : "level")}), nearest artist {near} ({nd:0} m), ");
            sb.Append($"song: {song}{(IndoorSpaces.Contains(p) ? ", indoors" : "")}");
            return sb.ToString();
        }

        void OnGUI()
        {
            if (flash <= 0f) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = new Color(1f, 1f, 1f, Mathf.Clamp01(flash));
            GUI.Label(new Rect(0, Screen.height - 70, Screen.width, 40), flashText, style);
        }
    }
}
