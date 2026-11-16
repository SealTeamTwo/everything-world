// Long grass and bunting that move in the wind. Vertex colour gives the colour; its alpha says how much that
// vertex moves (0 at the root or the string, 1 at the tip). The wind's strength and direction come from the
// globals _EW_Wind (0..1) and _EW_WindDir (x, z), set by the land (the Indie Downs follows each song's energy),
// and gusts roll across the ground as waves. Simple soft lighting from the sun and the sky, with fog.
Shader "Everything World/Wind Grass"
{
    Properties
    {
        _Sway ("Sway (m at full wind)", Float) = 0.45
        _Flutter ("Flutter speed", Float) = 1
        _ShadowTint ("Shadow tint", Color) = (0.62, 0.66, 0.72, 1)
        _Rich ("Rich: blade detail and lamp light (0 or 1)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Grass"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Sway, _Flutter, _Rich;
                float4 _ShadowTint;
            CBUFFER_END
            float _EW_Wind;
            float4 _EW_WindDir;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 color : COLOR; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float fog : TEXCOORD2; float2 uv : TEXCOORD3; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 p = TransformObjectToWorld(i.positionOS.xyz);
                float2 dir = normalize(_EW_WindDir.xz + float2(1e-4, 0));
                float wind = saturate(_EW_Wind);
                float t = _Time.y * _Flutter;
                // waves of gusts rolling downwind, plus a quicker shiver on top
                float wave = sin(t * (1.2 + wind * 1.6) - dot(p.xz, dir) * 0.08) * 0.5 + 0.5;
                float shiver = sin(t * 7.0 + p.x * 1.3 + p.z * 1.7) * 0.15;
                float bend = (0.25 + wind * 0.75) * (0.35 + wave * 0.65 + shiver) * _Sway * i.color.a;
                p.xz += dir * bend;
                p.y -= bend * bend * 0.35;                 // tips dip a little as they bend
                o.positionWS = p;
                o.positionCS = TransformWorldToHClip(p);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.color = i.color;
                o.uv = i.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float lit = smoothstep(0.0, 0.35, saturate(dot(n, sun.direction) * 0.5 + 0.5) - 0.25) * sun.shadowAttenuation;
                float3 ambient = SampleSH(n);
                float3 light = lerp(_ShadowTint.rgb, 1.0, lit) * sun.color * 0.85 + ambient * 0.45;
                float3 col = i.color.rgb;
                if (_Rich > 0.5)
                {
                    // each blade (uv: x across, 0..1, y root to tip): a pale midrib, fine veins along it, darker edges
                    float across = abs(i.uv.x - 0.5) * 2.0 / max(1.0 - i.uv.y, 0.06);
                    float rib = 1.0 - smoothstep(0.0, 0.16, across);
                    float veins = sin(across * 22.0) * 0.5 + 0.5;
                    col *= 0.86 + rib * 0.26 + veins * 0.07 - smoothstep(0.75, 1.0, across) * 0.12;
                    // lamps, lanterns and fires light the grass round them
                    #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    uint count = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                        light += l.color * l.distanceAttenuation * l.shadowAttenuation * (abs(dot(n, l.direction)) * 0.5 + 0.5);
                    LIGHT_LOOP_END
                    #endif
                }
                float3 c = col * light;
                c = MixFog(c, i.fog);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
