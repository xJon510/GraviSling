Shader "Unlit/Flipbook2D_Atlas"
{
    Properties
    {
        _AtlasTex ("Atlas", 2D) = "white" {}   // <-- not _MainTex
        _Cols ("Columns", Float) = 8
        _Rows ("Rows", Float)  = 8
        _Frame ("Frame Index", Float) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags{ "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct app { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };

            TEXTURE2D(_AtlasTex);
            SAMPLER(sampler_AtlasTex);

            float _Cols, _Rows, _Frame;
            float4 _Tint;

            v2f vert(app v){
                v2f o;
                o.pos = TransformObjectToHClip(v.pos.xyz);
                o.uv  = v.uv;               // 0..1 quad UVs from Full Rect sprite
                o.col = v.col * _Tint;
                return o;
            }

            float2 FlipbookUV(float2 uv, float frame, float cols, float rows){
                cols = max(cols, 1.0);
                rows = max(rows, 1.0);
                float total = cols * rows;
                frame = fmod(max(frame, 0.0), total);

                float fcol = fmod(frame, cols);
                float frow = floor(frame / cols);

                // invert if your first frame is at top-left in the PNG
                frow = (rows - 1.0) - frow;

                float2 cell = float2(1.0/cols, 1.0/rows);
                float2 baseUV = float2(fcol, frow) * cell;

                // small inset to avoid bleeding
                float2 pad  = cell * 0.001;
                float2 size = cell - pad * 2.0;

                return baseUV + pad + uv * size;
            }

            half4 frag(v2f i):SV_Target{
                float2 uv = FlipbookUV(i.uv, _Frame, _Cols, _Rows);
                return SAMPLE_TEXTURE2D(_AtlasTex, sampler_AtlasTex, uv) * i.col;
            }
            ENDHLSL
        }
    }
}
