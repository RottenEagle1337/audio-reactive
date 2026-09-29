// SpectrumBars.shader
// URP unlit sample shader: draws spectrum bars from an RFloat spectrum texture
// (SpectrumAnalyzer.Texture, one texel per band, values 0..1) across the UV range.
Shader "AudioReactive/Samples/SpectrumBars"
{
    Properties
    {
        _SpectrumTex ("Spectrum", 2D) = "black" {}
        _Bars ("Bar Count", Float) = 64
        _Gap ("Gap", Range(0, 0.9)) = 0.2
        [HDR] _BarColor ("Bar Color", Color) = (0.35, 0.8, 1, 1)
        _BackColor ("Back Color", Color) = (0.03, 0.03, 0.04, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SpectrumTex);
            SAMPLER(sampler_SpectrumTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _SpectrumTex_ST;
                float _Bars;
                float _Gap;
                half4 _BarColor;
                half4 _BackColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float bars = max(_Bars, 1);
                float x = i.uv.x * bars;
                float cell = floor(x);
                float level = SAMPLE_TEXTURE2D_LOD(_SpectrumTex, sampler_SpectrumTex, float2((cell + 0.5) / bars, 0.5), 0).r;
                float f = frac(x);
                bool inBar = f > _Gap * 0.5 && f < 1 - _Gap * 0.5 && i.uv.y < level;
                return inBar ? _BarColor * lerp(0.4, 1.0, i.uv.y) : _BackColor;
            }
            ENDHLSL
        }
    }
}
