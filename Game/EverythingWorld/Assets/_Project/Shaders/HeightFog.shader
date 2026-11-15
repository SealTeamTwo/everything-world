// Low mist that lies in hollows and thickens with distance, the way Valheim's air does: drawn over the whole
// picture after everything solid (a full-screen pass on the PC renderer, added by Editor/HeightFogSetup.cs).
// Each land sets it through SkyDirector (land_moods.json heightFog...); at heightFog 0 it changes nothing.
//   _EW_FogColor  rgb: the mist's colour, a: strength 0..1
//   _EW_FogSun    rgb: the sun's colour, a: how much the mist glows looking toward the sun
//   _EW_FogParams x: height (m) where it's thickest, y: how fast it thins going up (per m),
//                 z: density at that height (per m), w: the most it may cover (0..1); the first 35 m stay clear
//   _EW_SunDir    the direction toward the sun
Shader "Everything World/Height Fog"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "HeightFog"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _EW_FogColor;
            float4 _EW_FogSun;
            float4 _EW_FogParams;
            float4 _EW_SunDir;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_EW_FogColor.a <= 0.001) return col;

                #if UNITY_REVERSED_Z
                    float raw = SampleSceneDepth(uv);
                #else
                    float raw = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif
                float3 world = ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP);
                float3 ray = world - _WorldSpaceCameraPos;
                float dist = min(length(ray), 2500.0);
                float3 dir = normalize(ray);

                // exponential mist that thins with height, summed along the view ray
                float k = max(_EW_FogParams.y, 0.0001);
                float h0 = _WorldSpaceCameraPos.y - _EW_FogParams.x;
                float dy = dir.y * dist;
                float kd = k * dy;
                float along = abs(kd) > 0.0001 ? (1.0 - exp(-kd)) / kd : 1.0;
                float clear = max(dist - 35.0, 0.0);   // the first 35 m stay clear, so what is near keeps its full colour
                float amount = _EW_FogParams.z * exp(-k * max(h0, -20.0)) * along * clear;
                float fog = min(1.0 - exp(-amount), _EW_FogParams.w) * _EW_FogColor.a;

                // looking toward the sun, the mist takes on the sun's warm light
                float toward = pow(saturate(dot(dir, normalize(_EW_SunDir.xyz))), 6.0) * _EW_FogSun.a;
                float3 mist = lerp(_EW_FogColor.rgb, _EW_FogColor.rgb * 0.6 + _EW_FogSun.rgb * 0.7, toward);
                col.rgb = lerp(col.rgb, mist, fog);
                return col;
            }
            ENDHLSL
        }
    }
}
