// Ink outlines: a dark line wherever the picture has an edge (one thing in front of another, or two faces of a
// shape meeting at an angle), drawn over the whole picture after everything solid and before bloom (a full-screen
// pass on the PC renderer, added by Editor/OutlineSetup.cs). It makes the low-poly world read as drawn.
// Each land sets it through SkyDirector (Outlines.For); at strength 0 it changes nothing.
//   _EW_Outline        rgb: the ink's colour, a: strength 0..1
//   _EW_OutlineParams  x: line width in pixels, y: how big a jump in depth counts as an edge (as a share of the
//                      distance), z: how sharp a corner counts (0..2), w: the distance (m) by which the lines have faded out
Shader "Everything World/Outline"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _EW_Outline;
            float4 _EW_OutlineParams;

            float EyeDepth(float2 uv) { return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_EW_Outline.a <= 0.001) return col;

                float2 px = _EW_OutlineParams.x / _ScreenParams.xy;
                float d0 = EyeDepth(uv);
                float3 n0 = SampleSceneNormals(uv);
                float2 offs[4] = { float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1) };
                float nearest = d0, jump = 0.0, bend = 0.0;
                for (int i = 0; i < 4; i++)
                {
                    float2 u = uv + offs[i] * px;
                    float d = EyeDepth(u);
                    nearest = min(nearest, d);
                    jump = max(jump, abs(d - d0));
                    bend = max(bend, 1.0 - dot(n0, SampleSceneNormals(u)));
                }
                // a jump in depth, judged against how far away it is (so distant ground doesn't turn to lines);
                // a corner, only where there is no big jump (so the two never double up)
                float edgeDepth = smoothstep(_EW_OutlineParams.y, _EW_OutlineParams.y * 2.0, jump / max(nearest, 0.1));
                float edgeBend = smoothstep(_EW_OutlineParams.z, _EW_OutlineParams.z * 1.6, bend) * 0.75;
                float edge = saturate(max(edgeDepth, edgeBend));
                // the lines thin out with distance and are gone by the fade distance (and never on the sky)
                float fade = saturate(1.0 - nearest / max(_EW_OutlineParams.w, 1.0));
                edge *= fade * fade * _EW_Outline.a;
                return half4(lerp(col.rgb, _EW_Outline.rgb, edge), col.a);
            }
            ENDHLSL
        }
    }
}
