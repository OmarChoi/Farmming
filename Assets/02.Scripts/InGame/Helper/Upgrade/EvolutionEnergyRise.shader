Shader "Farmming/EvolutionEnergyRise"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MinY ("Min Y", Float) = 0
        _MaxY ("Max Y", Float) = 1
        _Progress ("Progress", Range(0, 1)) = 0
        _BandWidth ("Band Width", Float) = 0.35
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.45
        _GlowAlpha ("Glow Alpha", Range(0, 2)) = 0.85
        _OutlineWidth ("Outline Width", Float) = 0.045
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "OutlineGlow"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _MinY;
                float _MaxY;
                float _Progress;
                float _BandWidth;
                float _FillAlpha;
                float _GlowAlpha;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float worldY : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                float3 worldPosition = TransformObjectToWorld(positionOS);
                output.positionHCS = TransformWorldToHClip(worldPosition);
                output.worldY = worldPosition.y;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float height = max(0.0001, _MaxY - _MinY);
                float edge = lerp(_MinY - _BandWidth, _MaxY + _BandWidth, _Progress);
                float filled = saturate((edge - input.worldY) / max(0.0001, _BandWidth));
                float edgeGlow = 1.0 - saturate(abs(input.worldY - edge) / max(0.0001, _BandWidth));
                float verticalFade = saturate((input.worldY - _MinY) / height);
                float alpha = saturate((filled * 0.45 + edgeGlow) * _GlowAlpha * (0.45 + verticalFade * 0.55));
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "RisingFill"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _MinY;
                float _MaxY;
                float _Progress;
                float _BandWidth;
                float _FillAlpha;
                float _GlowAlpha;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float worldY : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPosition = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(worldPosition);
                output.worldY = worldPosition.y;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float edge = lerp(_MinY - _BandWidth, _MaxY + _BandWidth, _Progress);
                float filled = saturate((edge - input.worldY) / max(0.0001, _BandWidth));
                float edgeBand = 1.0 - saturate(abs(input.worldY - edge) / max(0.0001, _BandWidth));
                float alpha = saturate(filled * _FillAlpha + edgeBand * _GlowAlpha * 0.35);
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
