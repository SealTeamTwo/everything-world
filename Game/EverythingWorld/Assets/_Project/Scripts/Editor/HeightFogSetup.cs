using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EverythingWorld.EditorTools
{
    /// Puts the low-mist pass (Shaders/HeightFog.shader) on the PC renderer, once, when the editor loads: a
    /// full-screen pass drawn after everything solid and before bloom and colour grading. It does nothing on a land
    /// whose land_moods.json entry has heightFog 0. Its material is saved at Materials/HeightFog.mat.
    [InitializeOnLoad]
    static class HeightFogSetup
    {
        const string RendererPath = "Assets/Settings/PC_Renderer.asset", MaterialPath = "Assets/_Project/Materials/HeightFog.mat", FeatureName = "Height fog";

        static HeightFogSetup() { EditorApplication.delayCall += Ensure; }

        [MenuItem("Everything World/Setup/Add the height fog pass")]
        static void Ensure()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null) return;
            foreach (var f in data.rendererFeatures) if (f != null && f.name == FeatureName) return;
            var shader = Shader.Find("Everything World/Height Fog");
            if (shader == null) { Debug.LogWarning("Height fog: the shader isn't compiled yet; will try again next time the editor loads."); return; }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null) { mat = new Material(shader) { name = "HeightFog" }; AssetDatabase.CreateAsset(mat, MaterialPath); }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = FeatureName;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color;
            feature.fetchColorBuffer = true;
            feature.passMaterial = mat;
            AssetDatabase.AddObjectToAsset(feature, data);
            data.rendererFeatures.Add(feature);
            // the renderer keeps a list of its features' ids beside the list of features; keep the two in step
            var map = typeof(ScriptableRendererData).GetField("m_RendererFeatureMap", BindingFlags.NonPublic | BindingFlags.Instance);
            if (map != null && map.GetValue(data) is System.Collections.Generic.List<long> ids && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id)) ids.Add(id);
            data.SetDirty();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("Height fog: added the full-screen mist pass to the PC renderer.");
        }
    }
}
