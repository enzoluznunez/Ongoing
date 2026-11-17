Shader "UI/ChartMarks"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ClipRadius ("Plot Clip Radius", Float) = 8

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Marks"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 shape : TEXCOORD0;
                float4 sizes : TEXCOORD1;
                float4 corners : TEXCOORD2;
                float4 clipBox : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 shape : TEXCOORD0;
                float4 sizes : TEXCOORD1;
                float4 corners : TEXCOORD2;
                float4 clipBox : TEXCOORD3;
                float4 worldPosition : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ClipRect;
            float _ClipRadius;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.shape = v.shape;
                o.sizes = v.sizes;
                o.corners = v.corners;
                o.clipBox = v.clipBox;
                return o;
            }

            float RoundBox(float2 p, float2 extent, float4 radii)
            {
                float r = p.x > 0.0 ? (p.y > 0.0 ? radii.z : radii.y) : (p.y > 0.0 ? radii.w : radii.x);
                float2 q = abs(p) - extent + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            float Join(float a, float b)
            {
                return length(max(float2(a, b), 0.0)) + min(max(a, b), 0.0);
            }

            float RingSlice(float2 p, float outer, float inner, float halfGap, float aperture, float corner)
            {
                float r = length(p);
                float radial = max(r - (outer - corner), (inner + corner) - r);
                if (aperture >= 3.14159) return radial - corner;
                float s = sin(aperture);
                float c = cos(aperture);
                float push = halfGap + corner;
                float d1 = dot(p, float2(-s, c)) + push;
                float d2 = dot(p, float2(-s, -c)) + push;
                float side = aperture <= 1.5707963 ? max(d1, d2) : min(d1, d2);
                return Join(radial, side) - corner;
            }

            float Coverage(float d)
            {
                float w = max(fwidth(d), 1e-4);
                return saturate(0.5 - d / w);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = i.color;
                float kind = i.sizes.w;
                float2 p = i.shape.xy;

                float box = RoundBox(p, max(i.sizes.xy, 1e-4), i.corners);
                box = i.sizes.z > 0.0 ? abs(box) - i.sizes.z * 0.5 : box;
                float ring = RingSlice(p, i.sizes.x, i.sizes.y, i.sizes.z, i.corners.x, i.corners.y);
                float band = abs(p.y) - i.sizes.x;
                float d = kind < 1.5 ? box : (kind < 2.5 ? ring : band);
                float shape = Coverage(d);
                color.a *= kind > 0.5 ? shape : 1.0;

                float clipped = Coverage(RoundBox(i.clipBox.xy, max(i.clipBox.zw, 1e-4), _ClipRadius.xxxx));
                color.a *= i.clipBox.z > 0.0 ? clipped : 1.0;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                color.rgb *= color.a;
                return color;
            }
        ENDCG
        }
    }
}
