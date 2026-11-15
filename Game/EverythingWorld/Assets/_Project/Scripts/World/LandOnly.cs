using UnityEngine;

namespace EverythingWorld.World
{
    /// Shows a thing only while you're in its own land. For a land's giant sky pieces (the Synthwave Coast's striped
    /// sun, the Trance Steppe's sunrise): they're flat and stand far out past the land, so from the next land over
    /// they showed edge-on, in the wrong sky (the owner's notes, 2026-11-15).
    public class LandOnly : MonoBehaviour
    {
        public string land;
        Renderer[] renderers; float next; bool shown = true;

        public static LandOnly Add(GameObject go, string land)
        {
            var l = go.AddComponent<LandOnly>(); l.land = land; return l;
        }

        void Start() { renderers = GetComponentsInChildren<Renderer>(true); Apply(); }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + .5f; Apply();
        }

        void Apply()
        {
            var sky = SkyDirector.Instance; var on = sky == null || sky.CurrentLand == land;
            if (on == shown) return; shown = on;
            foreach (var r in renderers) if (r != null) r.enabled = on;
        }
    }
}
