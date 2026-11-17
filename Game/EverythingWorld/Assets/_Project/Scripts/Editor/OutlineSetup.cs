using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EverythingWorld.EditorTools
{
    /// Puts the ink-outline pass (Shaders/Outline.shader) on the PC renderer, once, when the editor loads: a
    /// full-screen pass drawn after everything solid and before bloom and colour grading. It does nothing on a land
    /// whose outline strength is 0 (Outlines.For in SkyDirector.cs). Its material is saved at Materials/Outline.mat.
    [InitializeOnLoad]
    static class OutlineSetup
    {
        const string RendererPath = "Assets/Settings/PC_Renderer.asset", MaterialPath = "Assets/_Project/Materials/Outline.mat", FeatureName = "Ink outlines";

        static OutlineSetup() { EditorApplication.delayCall += Ensure; }

        [MenuItem("Everything World/Setup/Add the ink outline pass")]
        static void Ensure()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null) return;
            foreach (var f in data.rendererFeatures) if (f != null && f.name == FeatureName) return;
            var shader = Shader.Find("Everything World/Outline");
            if (shader == null) { Debug.LogWarning("Ink outlines: the shader isn't compiled yet; will try again next time the editor loads."); return; }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null) { mat = new Material(shader) { name = "Outline" }; AssetDatabase.CreateAsset(mat, MaterialPath); }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = FeatureName;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color;
            feature.fetchColorBuffer = true;
            feature.passMaterial = mat;
            AssetDatabase.AddObjectToAsset(feature, data);
            // before the mist, so the mist lies over the lines (the mist's pass is already on the renderer)
            data.rendererFeatures.Insert(0, feature);
            var map = typeof(ScriptableRendererData).GetField("m_RendererFeatureMap", BindingFlags.NonPublic | BindingFlags.Instance);
            if (map != null && map.GetValue(data) is System.Collections.Generic.List<long> ids && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id)) ids.Insert(0, id);
            data.SetDirty();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("Ink outlines: added the full-screen outline pass to the PC renderer.");
        }
    }
}
