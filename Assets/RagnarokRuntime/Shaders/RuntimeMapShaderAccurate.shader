Shader "Custom/RuntimeMapShaderAccurate"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Atlas", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 200
        Cull Off
        ZWrite On

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : COLOR;
                half fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _AmbientStrength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS =
                    TransformObjectToWorldNormal(input.normalOS);

                output.uv =
                    TRANSFORM_TEX(input.uv, _MainTex);

                output.color =
                    input.color * _Color;

                output.fogFactor =
                    ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                clip(tex.a - _Cutoff);

                half3 normalWS =
                    normalize(input.normalWS);

                Light mainLight =
                    GetMainLight(
                        TransformWorldToShadowCoord(
                            input.positionWS
                        )
                    );

                half ndotl =
                    saturate(
                        dot(
                            normalWS,
                            mainLight.direction
                        )
                    );

                half lighting =
                    saturate(
                        _AmbientStrength +
                        ndotl *
                        mainLight.shadowAttenuation *
                        mainLight.distanceAttenuation
                    );

                half4 finalColor =
                    tex * input.color;

                finalColor.rgb *=
                    lighting;

                finalColor.rgb =
                    MixFog(
                        finalColor.rgb,
                        input.fogFactor
                    );

                finalColor.a = 1;
                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _AmbientStrength;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS =
                    TransformObjectToWorld(
                        input.positionOS.xyz
                    );

                float3 normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS
                    );

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS =
                        normalize(
                            _LightPosition -
                            positionWS
                        );
                #else
                    float3 lightDirectionWS =
                        _LightDirection;
                #endif

                float4 positionCS =
                    TransformWorldToHClip(
                        ApplyShadowBias(
                            positionWS,
                            normalWS,
                            lightDirectionWS
                        )
                    );

                #if UNITY_REVERSED_Z
                    positionCS.z =
                        min(
                            positionCS.z,
                            UNITY_NEAR_CLIP_VALUE
                        );
                #else
                    positionCS.z =
                        max(
                            positionCS.z,
                            UNITY_NEAR_CLIP_VALUE
                        );
                #endif

                return positionCS;
            }

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    GetShadowPositionHClip(input);

                output.uv =
                    TRANSFORM_TEX(
                        input.uv,
                        _MainTex
                    );

                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                half alpha =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    ).a;

                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
