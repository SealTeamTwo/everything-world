using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The visual upgrade (2026-11-15, tried on Lanternwood, then every land): deeper ambient occlusion (land_moods.json
    /// "ao", "aoRadius" to set a land's own), bevelled box edges (MeshKit.Bevel) and lamp shadows near you (NearShadows). Old = the look before it, for comparing: set the environment variable EW_OLD_LOOK=1 (the photo
    /// tour passes it through) or PlayerPrefs EW_OldLook = 1.
    public static class WorldLook
    {
        /// How far box edges are cut back (metres), and the ambient occlusion every land gets unless its land_moods.json
        /// entry asks for its own: a little lighter on sunny lands, whose sun shadows already give depth.
        public const float Bevel = .045f, AO = .95f, AOSunny = .7f, AORadius = .7f;
        static int old = -1;
        public static bool Old
        {
            get
            {
                if (old < 0) old = System.Environment.GetEnvironmentVariable("EW_OLD_LOOK") == "1" || PlayerPrefs.GetInt("EW_OldLook", 0) == 1 ? 1 : 0;
                return old == 1;
            }
        }
    }

    /// Point lights under this object cast soft shadows only while they're among the few nearest you, so a land full
    /// of lanterns gets shadows from posts, trees and people where you are without paying for all of them.
    public class NearShadows : MonoBehaviour
    {
        public int count = 4; public float within = 34f;
        readonly List<Light> lights = new List<Light>(); readonly HashSet<Light> always = new HashSet<Light>(); float next;

        void Start()
        {
            foreach (var l in GetComponentsInChildren<Light>(true)) { if (l.type != LightType.Point) continue; lights.Add(l); if (l.shadows != LightShadows.None) always.Add(l); }
        }

        void Update()
        {
            if (Time.unscaledTime < next) return; next = Time.unscaledTime + .4f;
            var cam = Camera.main; if (cam == null) return; var cp = cam.transform.position;
            lights.RemoveAll(l => l == null);
            lights.Sort((a, b) => (a.transform.position - cp).sqrMagnitude.CompareTo((b.transform.position - cp).sqrMagnitude));
            var n = 0;
            foreach (var l in lights)
            {
                if (always.Contains(l)) continue;
                var on = n < count && (l.transform.position - cp).sqrMagnitude < within * within; if (on) n++;
                var want = on ? LightShadows.Soft : LightShadows.None;
                if (l.shadows != want) { l.shadows = want; l.shadowStrength = .85f; l.shadowNearPlane = .2f; }
            }
        }
    }
}
