Shader "Experiments/VolumeMarch"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Unlit"
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE3D(_Volume);
            SAMPLER(sampler_Volume);
            float _Steps;

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 cameraOS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.positionOS = input.positionOS;
                output.cameraOS = TransformWorldToObject(GetCameraPositionWS());
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 exit = input.positionOS;
                float3 direction = normalize(exit - input.cameraOS);
                float3 toLow = (-0.5 - input.cameraOS) / direction;
                float3 toHigh = (0.5 - input.cameraOS) / direction;
                float3 nearest = min(toLow, toHigh);
                float enter = max(max(max(nearest.x, nearest.y), nearest.z), 0.0);
                float3 start = input.cameraOS + direction * enter;

                float3 step = (exit - start) / _Steps;
                float3 position = start + step * 0.5;
                half4 total = half4(0, 0, 0, 0);
                int count = (int)_Steps;
                for (int i = 0; i < count && total.a < 0.97; i++)
                {
                    half density = SAMPLE_TEXTURE3D_LOD(_Volume, sampler_Volume, position + 0.5, 0).r;
                    half alpha = density * (half)(24.0 / _Steps) * 0.25;
                    half3 color = lerp(half3(0.1, 0.3, 0.9), half3(1.0, 0.8, 0.2), density);
                    total.rgb += (1.0 - total.a) * color * alpha;
                    total.a += (1.0 - total.a) * alpha;
                    position += step;
                }
                return total;
            }
            ENDHLSL
        }
    }
}
