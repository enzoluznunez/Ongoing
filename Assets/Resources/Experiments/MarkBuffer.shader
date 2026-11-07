Shader "Experiments/MarkBuffer"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ MARK_ID_IN_UV

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Mark
            {
                float4 centerAndSize;
                float4 color;
            };

            StructuredBuffer<Mark> _Marks;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float idInUv : TEXCOORD0;
#if UNITY_ANY_INSTANCING_ENABLED
                UNITY_VERTEX_INPUT_INSTANCE_ID
#else
                uint instanceID : SV_InstanceID;
#endif
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if defined(MARK_ID_IN_UV)
                uint index = (uint)input.idInUv;
#elif UNITY_ANY_INSTANCING_ENABLED
                uint index = unity_InstanceID;
#else
                uint index = input.instanceID;
#endif
                Mark mark = _Marks[index];
                float3 world = mark.centerAndSize.xyz + input.positionOS * mark.centerAndSize.w;
                output.positionCS = TransformWorldToHClip(world);
                output.color = half4(mark.color);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.color;
            }
            ENDHLSL
        }
    }
}
