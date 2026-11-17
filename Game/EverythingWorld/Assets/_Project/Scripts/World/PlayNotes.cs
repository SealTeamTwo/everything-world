using System.IO;
using EverythingWorld.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// Notes while you play: press T, type what you think, press Enter. The note is saved to Logs/Live/notes.txt
    /// (next to the snapshots) with where you were when you pressed T: the land, position, facing, nearest artist,
    /// the song and how far into it, indoors or not. Esc throws the note away. No picture is taken (press P for one).
    /// Then tell Claude "read my notes". Nothing here is committed (Logs is ignored).
    public class PlayNotes : MonoBehaviour
    {
        public static bool Active { get; private set; }
        string typing = "", where, file; int lockedFrames; float flash; string flashText;
        SimplePlayerController walker; PlayerInteractor hands;
        const int MaxLength = 600;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("Play notes");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayNotes>();
        }

        void Start()
        {
            file = Path.Combine(Feedback.Dir, "notes.txt");   // (Logs/Live in the editor; the game's own data folder in a built game)
        }

        void Update()
        {
            flash -= Time.unscaledDeltaTime;
            var kb = Keyboard.current; if (kb == null) return;
            if (!Active)
            {
                if (!kb.tKey.wasPressedThisFrame || GameKeys.Off) return;
                walker = FindAnyObjectByType<SimplePlayerController>(); if (walker == null || !walker.enabled) return;   // only while walking about
                where = LiveSnapshot.Where(); if (where == null) return;
                hands = FindAnyObjectByType<PlayerInteractor>();
                walker.enabled = false; if (hands != null) hands.enabled = false;
                kb.onTextInput += OnChar; typing = ""; lockedFrames = 2; Active = true;   // skip the T that opened it
                return;
            }
            if (lockedFrames > 0) lockedFrames--;
            if (kb.escapeKey.wasPressedThisFrame) { Close(); flashText = "Note thrown away"; flash = 2f; return; }
            if (kb.backspaceKey.wasPressedThisFrame && typing.Length > 0) typing = typing.Substring(0, typing.Length - 1);
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                var note = typing.Trim();
                if (note.Length > 0)
                {
                    File.AppendAllText(file, $"NOTE {System.DateTime.Now:yyyy-MM-dd HH:mm}: \"{note}\" ({where})\n");
                    Debug.Log("Note for Claude: " + note);
                    flashText = Application.isEditor ? "Note saved for Claude" : "Note saved  ·  F9 packs your notes and photos into one file to send";
                }
                else flashText = "Empty note, nothing saved";
                flash = 2f; Close();
            }
        }

        void OnChar(char c)
        {
            if (!Active || lockedFrames > 0 || char.IsControl(c) || typing.Length >= MaxLength) return;
            typing += c;
        }

        void Close()
        {
            Active = false; var kb = Keyboard.current; if (kb != null) kb.onTextInput -= OnChar;
            StartCoroutine(GiveBack());
        }

        System.Collections.IEnumerator GiveBack()
        {
            yield return null;   // a frame later, so the Esc or Enter that closed the note doesn't also reach the game
            if (walker != null) walker.enabled = true;
            if (hands != null) hands.enabled = true;
        }

        void OnDestroy() { var kb = Keyboard.current; if (kb != null) kb.onTextInput -= OnChar; Active = false; }

        void OnGUI()
        { EverythingWorld.World.Ui.Begin();
            if (Active)
            {
                var w = Mathf.Min(900f, EverythingWorld.World.Ui.W - 80f); var box = new Rect((EverythingWorld.World.Ui.W - w) / 2f, EverythingWorld.World.Ui.H - 230f, w, 170f);
                GUI.color = new Color(0, 0, 0, .75f); GUI.DrawTexture(box, Texture2D.whiteTexture); GUI.color = Color.white;
                var head = new GUIStyle(GUI.skin.label) { fontSize = 16 }; head.normal.textColor = new Color(1f, .85f, .5f);
                GUI.Label(new Rect(box.x + 16, box.y + 10, box.width - 32, 24), "A note for Claude (Enter to save, Esc to throw away)", head);
                var body = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true }; body.normal.textColor = Color.white;
                var caret = Time.unscaledTime % 1f < .5f ? "|" : " ";
                GUI.Label(new Rect(box.x + 16, box.y + 40, box.width - 32, box.height - 50), typing + caret, body);
                return;
            }
            if (flash <= 0f) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = new Color(1f, 1f, 1f, Mathf.Clamp01(flash));
            GUI.Label(new Rect(0, EverythingWorld.World.Ui.H - 70, EverythingWorld.World.Ui.W, 40), flashText, style);
        }
    }
}
