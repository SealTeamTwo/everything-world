using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using EverythingWorld.Activities;
using EverythingWorld.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// Feedback from someone playing a built copy of the game (2026-11-17), made to be easy:
    /// - T writes a note and P takes a photo, as always (PlayNotes, LiveSnapshot). In a built game they are kept in
    ///   the game's own data folder (Feedback.Dir); in the editor, in Logs/Live as before.
    /// - F9, or "Pack my feedback" in the menu, puts everything into ONE zip file on the Desktop and shows it in the
    ///   file explorer, ready to send: the notes (each with where it was written), the photos (as JPGs, so the file
    ///   stays small enough to send), a page about the computer and the game, a page about each play session (how
    ///   long, which lands and activities, how smoothly it ran, where it stuttered, any errors), the activity
    ///   records, and the game's own log.
    /// Nothing leaves the computer by itself: the player sends the file themselves.
    /// What is NOT in the file: anything of Spotify's (no login, no tokens), and nothing from outside the game.
    public class Feedback : MonoBehaviour
    {
        /// Where notes, photos and session pages are kept.
        public static string Dir
        {
            get
            {
                if (dir != null) return dir;
                dir = Application.isEditor ? Path.Combine(Application.dataPath, "..", "Logs", "Live") : Path.Combine(Application.persistentDataPath, "Feedback");
                Directory.CreateDirectory(dir); return dir;
            }
        }
        static string dir;
        public static Feedback Instance { get; private set; }
        /// What the last packing said (shown in the menu and on screen).
        public static string LastResult = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { var go = new GameObject("Feedback"); DontDestroyOnLoad(go); Instance = go.AddComponent<Feedback>(); }

        // ---------------------------------------------------------------- this session, as it goes
        readonly DateTime started = DateTime.Now; float played, sampleT, fpsFrames, fpsTime, worstFrame; int hitches;
        readonly Dictionary<string, float> landSeconds = new Dictionary<string, float>(), activitySeconds = new Dictionary<string, float>();
        readonly List<string> hitchNotes = new List<string>(); readonly Dictionary<string, int> errors = new Dictionary<string, int>(); readonly List<string> errorOrder = new List<string>();
        float flash; string flashText = ""; bool packing, tested; string sessionFile, testTo;

        void OnEnable() { Application.logMessageReceived += OnLog; }
        void OnDisable() { Application.logMessageReceived -= OnLog; }

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            var first = stack != null ? stack.Split('\n')[0].Trim() : ""; var key = message.Length > 300 ? message.Substring(0, 300) : message; if (first.Length > 0) key += "   [" + first + "]";
            if (errors.TryGetValue(key, out var n)) errors[key] = n + 1; else if (errorOrder.Count < 60) { errors[key] = 1; errorOrder.Add(key); }
        }

        static string ActivityNow()
        {
            if (GolfGame.Playing) return "Mini golf"; if (PoolGame.Active != null) return "Pool"; if (HoopsGame.Active != null) return "Basketball"; if (HockeyGame.Active != null) return "Air hockey";
            if (BikeGame.Active != null) return "Light bikes"; if (PinballGame.Active != null) return "Pinball"; if (SkateGame.Active != null) return "Skating"; if (BowlGame.Active != null) return "Bowling"; if (ArcadeGame.Active != null) return "BRICKS";
            return null;
        }

        void Update()
        {
            flash -= Time.unscaledDeltaTime;
            var kb = Keyboard.current; if (kb != null && kb.f9Key.wasPressedThisFrame && !PlayNotes.Active) Pack();
            if (MainMenu.Showing || MusicLoading()) return;   // only time spent actually in the world counts
            var dt = Time.unscaledDeltaTime; played += dt; fpsFrames++; fpsTime += dt;
            if (dt > .25f && played > 5f)
            {
                hitches++; worstFrame = Mathf.Max(worstFrame, dt);
                if (hitchNotes.Count < 40) hitchNotes.Add($"{DateTime.Now:HH:mm:ss}  a pause of {dt:0.0} s  ({Land()}{(ActivityNow() != null ? ", playing " + ActivityNow() : "")})");
            }
            // (for checking the packing without a person: with EW_PACK_FEEDBACK set to a folder, a zip is made there a few seconds into the world)
            if (!tested && played > 4f) { tested = true; testTo = Environment.GetEnvironmentVariable("EW_PACK_FEEDBACK"); if (!string.IsNullOrEmpty(testTo) && Directory.Exists(testTo)) { played = 30f; Pack(); } testTo = null; }
            sampleT += dt; if (sampleT < 1f) return;
            Add(landSeconds, Land(), sampleT); var a = ActivityNow(); if (a != null) Add(activitySeconds, a, sampleT); sampleT = 0f;
        }
        static bool MusicLoading() => IslandWorldBuilder.Instance != null && IslandWorldBuilder.Instance.Loading;
        static string Land() => SkyDirector.Instance != null && !string.IsNullOrEmpty(SkyDirector.Instance.CurrentLand) ? SkyDirector.Instance.CurrentLand : "between lands";
        static void Add(Dictionary<string, float> d, string k, float v) { d.TryGetValue(k, out var was); d[k] = was + v; }

        void OnApplicationQuit() { try { WriteSession(); } catch (Exception) { } }

        /// This session's page: written when you quit, and again whenever the feedback is packed.
        void WriteSession()
        {
            if (played < 20f) return;
            sessionFile ??= Path.Combine(Dir, $"session_{started:yyyy-MM-dd_HH-mm-ss}.txt"); var sb = new StringBuilder();
            sb.AppendLine($"Play session, started {started:yyyy-MM-dd HH:mm}, written {DateTime.Now:HH:mm}");
            sb.AppendLine($"Time in the world: {Minutes(played)}");
            sb.AppendLine($"Smoothness: {(fpsTime > 0 ? fpsFrames / fpsTime : 0):0} frames a second on average; {hitches} pauses longer than a quarter second; the longest {worstFrame:0.0} s");
            sb.AppendLine(); sb.AppendLine("Lands (time in each):"); foreach (var kv in Sorted(landSeconds)) sb.AppendLine($"  {kv.Key}: {Minutes(kv.Value)}");
            sb.AppendLine(); sb.AppendLine("Activities (time in each):"); if (activitySeconds.Count == 0) sb.AppendLine("  none"); foreach (var kv in Sorted(activitySeconds)) sb.AppendLine($"  {kv.Key}: {Minutes(kv.Value)}");
            sb.AppendLine(); sb.AppendLine("Pauses (the first forty):"); if (hitchNotes.Count == 0) sb.AppendLine("  none"); foreach (var h in hitchNotes) sb.AppendLine("  " + h);
            sb.AppendLine(); sb.AppendLine("Errors the game reported (each once, with how many times):"); if (errorOrder.Count == 0) sb.AppendLine("  none"); foreach (var e in errorOrder) sb.AppendLine($"  x{errors[e]}  {e.Replace("\n", " ")}");
            File.WriteAllText(sessionFile, sb.ToString());
        }
        static string Minutes(float seconds) => seconds < 90f ? $"{seconds:0} s" : $"{seconds / 60f:0} min";
        static List<KeyValuePair<string, float>> Sorted(Dictionary<string, float> d) { var l = new List<KeyValuePair<string, float>>(d); l.Sort((a, b) => b.Value.CompareTo(a.Value)); return l; }

        // ---------------------------------------------------------------- packing it all into one file
        /// The page about the computer, the game and the records.
        static string Info(int notes, int photos)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Everything World feedback, packed {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"From: {Friends.PlayerName}");
            sb.AppendLine($"In this file: {notes} notes, {photos} photos (notes.txt says where each was taken)");
            sb.AppendLine(); sb.AppendLine("The game");
            sb.AppendLine($"  Version {Application.version}, build {Application.buildGUID}, Unity {Application.unityVersion}, {(Application.isEditor ? "in the editor" : "a built game")}");
            sb.AppendLine($"  Window {Screen.width} x {Screen.height}, {(Screen.fullScreen ? "full screen" : "windowed")}, quality \"{QualitySettings.names[QualitySettings.GetQualityLevel()]}\"");
            sb.AppendLine(); sb.AppendLine("The computer");
            sb.AppendLine($"  {SystemInfo.operatingSystem}");
            sb.AppendLine($"  {SystemInfo.deviceModel}");
            sb.AppendLine($"  Processor: {SystemInfo.processorType} ({SystemInfo.processorCount} threads)");
            sb.AppendLine($"  Memory: {SystemInfo.systemMemorySize / 1024f:0.0} GB");
            sb.AppendLine($"  Graphics: {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsMemorySize / 1024f:0.0} GB, {SystemInfo.graphicsDeviceVersion}");
            sb.AppendLine($"  Screen: {Screen.currentResolution.width} x {Screen.currentResolution.height} at {Screen.currentResolution.refreshRateRatio.value:0} Hz");
            sb.AppendLine(); sb.AppendLine("Activity records on this computer");
            void Rec(string name, string key, string best, string bestWord)
            {
                var b = PlayerPrefs.GetInt($"{key}_{best}", 0); var un = PlayerPrefs.GetInt($"{key}_unlocked", 0) == 1;
                sb.AppendLine($"  {name}: {(b > 0 ? $"{bestWord} {b}" : "nothing yet")}{(un ? ", Everything Playlist unlocked" : "")}");
            }
            Rec("Mini golf (the Riviera)", "EW_Golf_riviera", "best", "best round"); sb.AppendLine($"    rounds finished: {PlayerPrefs.GetInt("EW_Golf_riviera_rounds", 0)}");
            Rec("Pool", "EW_Pool_jazz", "best", "best clearance, shots:"); Rec("Basketball", "EW_Hoops_hiphop", "made", "baskets scored in (as bits):"); Rec("Air hockey", "EW_Hockey_bit", "wins", "wins");
            Rec("BRICKS", "EW_Arcade_bricks", "best", "high score"); Rec("Light bikes", "EW_Bikes_synth", "wins", "rounds won"); Rec("Pinball", "EW_Pinball_oddball", "best", "high score");
            Rec("Skating", "EW_Skate_punk", "best", "best run"); Rec("Bowling", "EW_Bowl_glit", "best", "best game");
            return sb.ToString();
        }

        /// Packs everything into one zip on the Desktop and shows it. Safe to call as often as you like: each
        /// packing holds everything so far.
        public void Pack()
        {
            if (packing) return; packing = true;
            try
            {
                WriteSession();
                var who = Clean(Friends.PlayerName); var name = $"EverythingWorld feedback - {who} - {DateTime.Now:yyyy-MM-dd HH-mm}.zip";
                var desktop = testTo ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); if (string.IsNullOrEmpty(desktop) || !Directory.Exists(desktop)) desktop = Dir;
                var zipPath = Path.Combine(desktop, name); if (File.Exists(zipPath)) File.Delete(zipPath);
                var notesFile = Path.Combine(Dir, "notes.txt"); var notes = 0; if (File.Exists(notesFile)) foreach (var l in File.ReadAllLines(notesFile)) if (l.StartsWith("NOTE ")) notes++;
                var pngs = Directory.GetFiles(Dir, "*.png"); Array.Sort(pngs);
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    void Text(string entry, string text) { var e = zip.CreateEntry(entry, System.IO.Compression.CompressionLevel.Optimal); using (var w = new StreamWriter(e.Open(), Encoding.UTF8)) w.Write(text); }
                    Text("READ ME FIRST - info.txt", Info(notes, pngs.Length));
                    Text("notes.txt", File.Exists(notesFile) ? File.ReadAllText(notesFile) : "No notes or photos were taken.\n");
                    foreach (var f in Directory.GetFiles(Dir, "session_*.txt")) zip.CreateEntryFromFile(f, "sessions/" + Path.GetFileName(f));
                    // the photos, as JPGs (a tenth of the size)
                    foreach (var f in pngs)
                    {
                        try
                        {
                            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false); byte[] jpg = null;
                            if (tex.LoadImage(File.ReadAllBytes(f))) jpg = tex.EncodeToJPG(85); Destroy(tex);
                            if (jpg == null) { zip.CreateEntryFromFile(f, "photos/" + Path.GetFileName(f)); continue; }
                            var e = zip.CreateEntry("photos/" + Path.GetFileNameWithoutExtension(f) + ".jpg", System.IO.Compression.CompressionLevel.NoCompression); using (var s = e.Open()) s.Write(jpg, 0, jpg.Length);
                        }
                        catch (Exception ex) { Debug.LogWarning("Feedback: a photo could not be packed: " + ex.Message); }
                    }
                    // the game's own log, this run and the one before (the last 3 MB of each: it is a shared file, so it is copied as it stands)
                    var log = Application.consoleLogPath;
                    if (!string.IsNullOrEmpty(log))
                        foreach (var (path, entry) in new[] { (log, "log/this run.log"), (Path.Combine(Path.GetDirectoryName(log) ?? "", Path.GetFileNameWithoutExtension(log) + "-prev.log"), "log/the run before.log") })
                        {
                            try
                            {
                                if (!File.Exists(path)) continue;
                                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    const long max = 3L * 1024 * 1024; if (fs.Length > max) fs.Seek(-max, SeekOrigin.End);
                                    var e = zip.CreateEntry(entry, System.IO.Compression.CompressionLevel.Optimal); using (var s = e.Open()) fs.CopyTo(s);
                                }
                            }
                            catch (Exception ex) { Debug.LogWarning("Feedback: the log could not be packed: " + ex.Message); }
                        }
                }
                var mb = new FileInfo(zipPath).Length / (1024f * 1024f);
                LastResult = $"Packed {notes} notes and {pngs.Length} photos into one file on your Desktop ({mb:0.0} MB):\n{name}\nSend that file to whoever gave you the game.";
                if (testTo == null) Reveal(zipPath); else Debug.Log("Feedback test: " + LastResult.Replace("\n", " | ") + " -> " + zipPath);
            }
            catch (Exception e) { LastResult = "The feedback could not be packed: " + e.Message; Debug.LogException(e); }
            finally { packing = false; flashText = LastResult; flash = 12f; }
        }

        static string Clean(string s) { if (string.IsNullOrWhiteSpace(s)) return "player"; var sb = new StringBuilder(); foreach (var c in s) sb.Append(char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' ? c : '_'); var r = sb.ToString().Trim(); return r.Length == 0 ? "player" : r.Length > 30 ? r.Substring(0, 30) : r; }

        /// Opens the file explorer with the new file picked out.
        static void Reveal(string path)
        {
            try
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + path.Replace('/', '\\') + "\"");
                else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor) System.Diagnostics.Process.Start("open", "-R \"" + path + "\"");
                else Application.OpenURL("file://" + Path.GetDirectoryName(path));
            }
            catch (Exception) { }
        }

        GUIStyle style;
        void OnGUI()
        { EverythingWorld.World.Ui.Begin();
            if (flash <= 0f || MainMenu.Showing || string.IsNullOrEmpty(flashText)) return;
            style ??= GameFonts.Box(18, TextAnchor.MiddleCenter, true);
            var keep = GUI.color; GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(flash)); var w = Mathf.Min(EverythingWorld.World.Ui.W - 40f, 860f);
            var h = style.CalcHeight(new GUIContent(flashText), w) + 6f; GUI.Box(new Rect(EverythingWorld.World.Ui.W / 2f - w / 2f, EverythingWorld.World.Ui.H * .3f, w, h), flashText, style); GUI.color = keep;
        }
    }
}
