Shader "Farmming/EvolutionEnergyRise"
{
    Properties
    {
        _Color ("Fallback Color", Color) = (1, 1, 1, 1)
        _MinY ("Min Y", Float) = 0
        _MaxY ("Max Y", Float) = 1
        _Progress ("Progress", Range(0, 1)) = 0
        _RevealMode ("Reveal Mode", Float) = 0
        _BandWidth ("Band Width", Float) = 0.45
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.55
        _GlowAlpha ("Glow Alpha", Range(0, 2)) = 0.85
        _EffectStrength ("Effect Strength", Range(0, 3)) = 1
        _OutlineWidth ("Outline Width", Float) = 0.01
        _RainbowStrength ("Fallback Rainbow Strength", Range(0, 1)) = 1
        _RainbowSpeed ("Fallback Rainbow Speed", Range(0, 2)) = 0.25
        _SurfaceOffset ("Surface Offset", Float) = 0.02

        [Header(Galaxy Background)]
        _GalaxyTexture ("Galaxy Texture", CUBE) = "white" {}
        _GalaxyEmissionIntensity ("Galaxy Emission Intensity", Float) = 1
        _GalaxyBaseColorIntensity ("Galaxy Base Color Intensity", Range(0, 1)) = 0
        _GalaxyTint ("Galaxy Tint", Color) = (0.6, 0.44, 1, 0)

        [Header(Stars)]
        _StarsTexture ("Stars Texture", 2D) = "white" {}
        _StarsTile ("Stars Tile", Vector) = (1, 1, 0, 0)
        _StarsTileOverall ("Stars Tile Overall", Float) = 7
        _StarsSpeed ("Stars Speed", Vector) = (-0.03, -0.02, 0, 0)
        _StarsColor01 ("Stars Color 01", Color) = (0, 0.94, 1, 0)
        _StarsColor02 ("Stars Color 02", Color) = (1, 1, 1, 0)
        _StarsEmissionIntensity ("Stars Emission Intensity", Float) = 100

        [Header(Stars Noise)]
        _StarsNoiseTexture ("Stars Noise Texture", 2D) = "black" {}
        _StarsNoiseTile ("Stars Noise Tile", Vector) = (1, 1, 0, 0)
        _StarsNoiseTileOverall ("Stars Noise Tile Overall", Float) = 1
        _StarsNoiseSpeed ("Stars Noise Speed", Vector) = (0.1, 0.03, 0, 0)

        [Header(Fresnel)]
        _FresnelColor ("Fresnel Color", Color) = (0, 0.69, 1, 0)
        _FresnelEmissionIntensity ("Fresnel Emission Intensity", Float) = 1
        _FresnelBias ("Fresnel Bias", Float) = 0
        _FresnelScale ("Fresnel Scale", Float) = 1
        _FresnelPower ("Fresnel Power", Float) = 0.1
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
            Name "GalaxyRise"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURECUBE(_GalaxyTexture);
            SAMPLER(sampler_GalaxyTexture);
            TEXTURE2D(_StarsTexture);
            SAMPLER(sampler_StarsTexture);
            TEXTURE2D(_StarsNoiseTexture);
            SAMPLER(sampler_StarsNoiseTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _MinY;
                float _MaxY;
                float _Progress;
                float _RevealMode;
                float _BandWidth;
                float _FillAlpha;
                float _GlowAlpha;
                float _EffectStrength;
                float _OutlineWidth;
                float _RainbowStrength;
                float _RainbowSpeed;
                float _SurfaceOffset;
                float _GalaxyEmissionIntensity;
                float _GalaxyBaseColorIntensity;
                half4 _GalaxyTint;
                float4 _StarsTile;
                float _StarsTileOverall;
                float4 _StarsSpeed;
                half4 _StarsColor01;
                half4 _StarsColor02;
                float _StarsEmissionIntensity;
                float4 _StarsNoiseTile;
                float _StarsNoiseTileOverall;
                float4 _StarsNoiseSpeed;
                half4 _FresnelColor;
                float _FresnelEmissionIntensity;
                float _FresnelBias;
                float _FresnelScale;
                float _FresnelPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float worldY : TEXCOORD1;
                float3 worldPosition : TEXCOORD2;
                half3 worldNormal : TEXCOORD3;
                half3 viewDirection : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 normalOS = normalize(input.normalOS);
                float3 positionOS = input.positionOS.xyz + normalOS * max(_SurfaceOffset, _OutlineWidth);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.worldY = positionInputs.positionWS.y;
                output.worldPosition = positionInputs.positionWS;
                output.worldNormal = normalize(normalInputs.normalWS);
                output.viewDirection = normalize(GetWorldSpaceViewDir(positionInputs.positionWS));
                return output;
            }

            half3 FallbackRainbow(float phase)
            {
                const float tau = 6.2831853;
                half3 color;
                color.r = 0.5h + 0.5h * cos(tau * (phase + 0.00));
                color.g = 0.5h + 0.5h * cos(tau * (phase + 0.33));
                color.b = 0.5h + 0.5h * cos(tau * (phase + 0.67));
                return lerp(_Color.rgb, color, _RainbowStrength);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float bandWidth = max(0.0001, _BandWidth);
                float edge = lerp(_MinY - bandWidth, _MaxY + bandWidth, _Progress);
                float riseMask = 1.0 - smoothstep(edge - bandWidth, edge, input.worldY);
                float clearFromBottomMask = smoothstep(edge, edge + bandWidth, input.worldY);
                float revealMask = lerp(riseMask, clearFromBottomMask, step(0.5, _RevealMode));
                float edgeBand = 1.0 - smoothstep(0.0, bandWidth, abs(input.worldY - edge));

                half3 normalWS = normalize(input.worldNormal);
                half3 viewDir = normalize(input.viewDirection);
                half3 reflectionDir = reflect(-viewDir, normalWS);

                half3 galaxy = SAMPLE_TEXTURECUBE(_GalaxyTexture, sampler_GalaxyTexture, reflectionDir).rgb;
                galaxy *= lerp(half3(1.0h, 1.0h, 1.0h), _GalaxyTint.rgb, 0.65h);
                galaxy *= max(0.0, _GalaxyEmissionIntensity + _GalaxyBaseColorIntensity);

                float2 starsUv = input.uv * _StarsTile.xy * max(0.0001, _StarsTileOverall) + _Time.y * _StarsSpeed.xy;
                float2 noiseUv = input.uv * _StarsNoiseTile.xy * max(0.0001, _StarsNoiseTileOverall) + _Time.y * _StarsNoiseSpeed.xy;
                half star = SAMPLE_TEXTURE2D(_StarsTexture, sampler_StarsTexture, starsUv).r;
                half noise = SAMPLE_TEXTURE2D(_StarsNoiseTexture, sampler_StarsNoiseTexture, noiseUv).r;
                half3 starColor = lerp(_StarsColor01.rgb, _StarsColor02.rgb, noise);
                half3 stars = starColor * saturate(star * (0.35h + noise * 1.25h)) * (_StarsEmissionIntensity * 0.01h);

                half fresnel = pow(saturate(_FresnelBias + _FresnelScale * (1.0h - dot(normalWS, viewDir))), max(0.01h, _FresnelPower));
                half3 fresnelColor = _FresnelColor.rgb * fresnel * (_FresnelEmissionIntensity * 0.25h);

                float fallbackPhase = input.worldPosition.y * 0.65 + input.worldPosition.x * 0.18 + _Time.y * _RainbowSpeed;
                half3 fallback = FallbackRainbow(fallbackPhase);
                half galaxyLuma = dot(galaxy, half3(0.2126h, 0.7152h, 0.0722h));
                half3 color = galaxyLuma > 0.001h ? galaxy + stars + fresnelColor : fallback + fresnelColor;
                color *= max(0.0, _EffectStrength);

                float fillAlpha = revealMask * _FillAlpha;
                float edgeAlpha = edgeBand * _GlowAlpha * 0.45;
                float alpha = saturate((fillAlpha + edgeAlpha) * _EffectStrength);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
