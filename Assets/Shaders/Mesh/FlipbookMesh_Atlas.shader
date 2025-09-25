Shader "Unlit/FlipbookMesh_Atlas"
{
    Properties
    {
        _AtlasTex ("Atlas", 2D) = "white" {}
        _Cols     ("Columns", Float) = 8
        _Rows     ("Rows",    Float) = 8
        _BaseTint ("Base Tint", Color) = (1,1,1,1)
        _Cutoff   ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }
        Cull Off
        ZWrite On
        Blend One Zero

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTint;
                float  _Cols, _Rows, _Cutoff;
            CBUFFER_END

            TEXTURE2D(_AtlasTex);
            SAMPLER(sampler_AtlasTex);

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float , _IFrame)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ITint)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f      { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };

            float2 FlipbookUV(float2 uv, float frame, float cols, float rows)
            {
                cols = max(cols, 1.0); rows = max(rows, 1.0);
                float total = cols * rows;
                frame = fmod(max(frame, 0.0), total);
                float fcol = fmod(frame, cols);
                float frow = floor(frame / cols);
                frow = (rows - 1.0) - frow;
                float2 cell = float2(1.0/cols, 1.0/rows);
                float2 baseUV = float2(fcol, frow) * cell;
                float2 pad = cell * 0.001;
                float2 size = cell - pad * 2.0;
                return baseUV + pad + uv * size;
            }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o; UNITY_TRANSFER_INSTANCE_ID(v,o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(posWS);
                o.uv = v.uv;

                float4 instTint = UNITY_ACCESS_INSTANCED_PROP(Props, _ITint);

                // use _BaseTint when _ITint is "unset" ( zero)
                float useBase = 1.0 - step(1e-6, length(instTint)); // <-- was inverted
                float4 chosen = lerp(instTint, _BaseTint, useBase);

                // If instancing isn't enabled in this variant, always use material tint
                #ifndef UNITY_INSTANCING_ENABLED
                    chosen = _BaseTint;
                #endif

                o.color = v.color * chosen;
                return o;
            }

            half4 frag(v2f i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float frame = UNITY_ACCESS_INSTANCED_PROP(Props, _IFrame);
                float2 uv = FlipbookUV(i.uv, frame, _Cols, _Rows);
                half4 tex = SAMPLE_TEXTURE2D(_AtlasTex, sampler_AtlasTex, uv);
                clip(tex.a - _Cutoff);      // <-- alpha clip -> opaque path
                return tex * i.color;
            }
            ENDHLSL
        }
    }
}
