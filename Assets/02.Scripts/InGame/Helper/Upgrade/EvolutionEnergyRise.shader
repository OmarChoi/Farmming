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
        _RainbowStrength ("Rainbow Strength", Range(0, 1)) = 1
        _RainbowSpeed ("Rainbow Speed", Range(0, 2)) = 0.25
        _SurfaceOffset ("Surface Offset", Float) = 0.018
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
                float _RainbowStrength;
                float _RainbowSpeed;
                float _SurfaceOffset;
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
                float3 worldPosition : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                float3 worldPosition = TransformObjectToWorld(positionOS);
                output.positionHCS = TransformWorldToHClip(worldPosition);
                output.worldY = worldPosition.y;
                output.worldPosition = worldPosition;
                return output;
            }

            half3 Rainbow(float phase)
            {
                const float tau = 6.2831853;
                half3 color;
                color.r = 0.5h + 0.5h * cos(tau * (phase + 0.00));
                color.g = 0.5h + 0.5h * cos(tau * (phase + 0.33));
                color.b = 0.5h + 0.5h * cos(tau * (phase + 0.67));
                return lerp(color, half3(1.0h, 1.0h, 1.0h), 0.08h);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float height = max(0.0001, _MaxY - _MinY);
                float edge = lerp(_MinY - _BandWidth, _MaxY + _BandWidth, _Progress);
                float filled = saturate((edge - input.worldY) / max(0.0001, _BandWidth));
                float edgeGlow = 1.0 - saturate(abs(input.worldY - edge) / max(0.0001, _BandWidth));
                float verticalFade = saturate((input.worldY - _MinY) / height);
                float phase = input.worldPosition.y * 0.65 + input.worldPosition.x * 0.18 + _Time.y * _RainbowSpeed;
                half3 color = lerp(_Color.rgb, Rainbow(phase), _RainbowStrength);
                float alpha = saturate((filled * 0.16 + edgeGlow * 0.85) * _GlowAlpha * (0.35 + verticalFade * 0.45));
                return half4(color, alpha);
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
                float _RainbowStrength;
                float _RainbowSpeed;
                float _SurfaceOffset;
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
                float3 worldPosition : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz + normalize(input.normalOS) * _SurfaceOffset;
                float3 worldPosition = TransformObjectToWorld(positionOS);
                output.positionHCS = TransformWorldToHClip(worldPosition);
                output.worldY = worldPosition.y;
                output.worldPosition = worldPosition;
                return output;
            }

            half3 Rainbow(float phase)
            {
                const float tau = 6.2831853;
                half3 color;
                color.r = 0.5h + 0.5h * cos(tau * (phase + 0.00));
                color.g = 0.5h + 0.5h * cos(tau * (phase + 0.33));
                color.b = 0.5h + 0.5h * cos(tau * (phase + 0.67));
                return lerp(color, half3(1.0h, 1.0h, 1.0h), 0.08h);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float edge = lerp(_MinY - _BandWidth, _MaxY + _BandWidth, _Progress);
                float filled = saturate((edge - input.worldY) / max(0.0001, _BandWidth));
                float edgeBand = 1.0 - saturate(abs(input.worldY - edge) / max(0.0001, _BandWidth));
                float phase = input.worldPosition.y * 0.65 + input.worldPosition.x * 0.18 + _Time.y * _RainbowSpeed;
                half3 color = lerp(_Color.rgb, Rainbow(phase), _RainbowStrength);
                float alpha = saturate(filled * _FillAlpha + edgeBand * _GlowAlpha * 0.18);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
