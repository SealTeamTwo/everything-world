using UnityEngine;

namespace EverythingWorld.World
{
    /// One size for everything written on the screen, whatever the screen. Every OnGUI starts with Ui.Begin() and lays
    /// itself out on a sheet Ui.W wide and Ui.H high (never Screen.width and Screen.height): the sheet is always 1080
    /// units tall, and is scaled to fill the real screen, so a box that is 34 units high is the same share of the
    /// picture at 1080p, 1440p and 4K (it used to be 34 pixels on all of them, and tiny on the big ones).
    /// On a small window the sheet stops shrinking at three quarters size, so the words stay readable.
    public static class Ui
    {
        public const float Design = 1080f;
        public static float Scale => Mathf.Clamp(Screen.height / Design, .75f, 4f);
        public static float W => Screen.width / Scale;
        public static float H => Screen.height / Scale;
        /// Call first thing in OnGUI.
        public static void Begin() { var s = Scale; GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f)); }
        /// A point on the real screen (pixels, from the bottom left, as a camera gives it) as a point on the sheet.
        public static Vector2 FromScreen(Vector3 screenPoint) { var s = Scale; return new Vector2(screenPoint.x / s, (Screen.height - screenPoint.y) / s); }
    }
}
