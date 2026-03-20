// =============================================================================
// SoftToon/Environment 셰이더
// =============================================================================
// 환경(배경, 지형, 식물 등) 전용 소프트 툰 셰이딩 셰이더.
// Character 셰이더와 동일한 SoftToonRamp를 사용하되, 환경에 필요한
// 추가 기능들을 토글(shader_feature_local) 방식으로 제공한다.
//
// 토글 가능한 기능:
//   _USE_VERTEX_COLOR    - 버텍스 컬러를 알베도에 곱함
//   _USE_DETAIL          - 디테일 텍스처 블렌딩
//   _USE_HEIGHT_GRADIENT - 월드 Y축 기반 색상 그라데이션
//   _USE_FRESNEL         - 프레넬 림라이트
//
// 구성 Pass:
//   Pass 0 (ForwardLit)  - 메인 툰 셰이딩
//   Pass 1 (ShadowCaster)- 그림자 캐스터
//   Pass 2 (DepthOnly)   - 깊이 프리패스
//
// 외곽선은 EdgeDetection 포스트 프로세싱으로 화면 전체에 적용됨.
// =============================================================================
Shader "Custom/SoftToon/Environment"
{
    Properties
    {
        // ── 기본 텍스처/색상 ──
        [Header(Base)]
        _MainTex ("Albedo Texture", 2D) = "white" {}       // 기본 알베도 텍스처
        _BaseColor ("Base Color Tint", Color) = (1, 1, 1, 1) // 텍스처에 곱해지는 색상 틴트
        [Toggle(_USE_VERTEX_COLOR)] _UseVertexColor ("Use Vertex Color", Float) = 0
        // ↑ 켜면 메시의 버텍스 컬러(RGB)를 알베도에 곱한다.
        //   핸드페인팅 스타일 에셋에서 텍스처 없이 버텍스 컬러로 색을 입힐 때 유용.

        // ── 디테일 텍스처 ──
        [Header(Detail Texture)]
        [Toggle(_USE_DETAIL)] _UseDetail ("Enable Detail Texture", Float) = 0
        _DetailTex ("Detail Texture", 2D) = "white" {}     // 디테일 텍스처 (Overlay 블렌딩)
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.3 // 디테일 블렌딩 강도
        // ↑ 큰 지형이나 벽면에 근접했을 때 디테일을 추가하는 용도.
        //   albedo와 detail*2.0을 lerp하여 Overlay 스타일 블렌딩을 한다.

        // ── 툰 셰이딩 파라미터 ──
        [Header(Toon Shading)]
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = 0.0        // 그림자 경계 위치
        _ShadowSmoothness ("Shadow Smoothness", Range(0.001, 1.0)) = 0.4 // 그림자 경계 부드러움
        _ShadowColor ("Shadow Color", Color) = (0.65, 0.6, 0.75, 1)     // 그림자 색조
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.5         // 그림자 색 강도

        // ── 앰비언트 및 환경 ──
        [Header(Ambient and Environment)]
        _AmbientColor ("Ambient Color", Color) = (0.85, 0.85, 0.9, 1)   // 앰비언트 색상
        _AmbientIntensity ("Ambient Intensity", Range(0, 1)) = 0.2      // 앰비언트 강도
        _AmbientOcclusionStrength ("AO from Vertex Alpha", Range(0, 1)) = 0.0
        // ↑ 버텍스 컬러의 알파 채널을 AO(Ambient Occlusion)로 사용.
        //   0이면 AO 무시, 1이면 버텍스 알파가 앰비언트를 완전히 조절.
        //   모델러가 구석/틈새 부분의 버텍스 알파를 낮게 설정하면 자연스러운 음영.

        // ── 높이 그라데이션 ──
        // 월드 Y 좌표에 따라 아래쪽↔위쪽 색상을 블렌딩한다.
        // 풀, 나무, 산 등에서 자연스러운 높이 기반 색 변화를 줄 때 유용.
        [Header(Height Gradient)]
        [Toggle(_USE_HEIGHT_GRADIENT)] _UseHeightGradient ("Enable Height Gradient", Float) = 0
        _GradientBottomColor ("Bottom Color", Color) = (0.4, 0.55, 0.3, 1) // 하단 색상
        _GradientTopColor ("Top Color", Color) = (0.6, 0.8, 0.45, 1)      // 상단 색상
        _GradientBottomY ("Bottom Y (World)", Float) = 0.0                  // 그라데이션 시작 Y (월드 좌표)
        _GradientTopY ("Top Y (World)", Float) = 5.0                        // 그라데이션 끝 Y (월드 좌표)
        _GradientBlend ("Gradient Blend", Range(0, 1)) = 0.3               // 원래 색과의 블렌딩 비율

        // ── 프레넬 림라이트 ──
        // 캐릭터 셰이더의 림라이트와 유사하지만, 라이트 방향 마스크가 없는 단순한 형태.
        // 환경 오브젝트의 가장자리에 빛 테두리를 추가한다.
        [Header(Fresnel Rim)]
        [Toggle(_USE_FRESNEL)] _UseFresnel ("Enable Fresnel Rim", Float) = 0
        _FresnelColor ("Rim Color", Color) = (1, 1, 1, 1)               // 림 색상
        _FresnelPower ("Rim Power", Range(0.5, 8.0)) = 3.0              // 림 집중도
        _FresnelIntensity ("Rim Intensity", Range(0, 1)) = 0.4          // 림 강도

        // ── 그림자 Acne 보정 ──
        [Header(Shadow Acne Fix)]
        _ShadowDepthBias ("Shadow Depth Bias", Range(0, 10)) = 2.0      // 깊이 바이어스
        _ShadowNormalBias ("Shadow Normal Bias", Range(0, 10)) = 1.5    // 노말 바이어스
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // ============================================================
        // Pass 0: 메인 툰 셰이딩 패스 (환경용)
        // ============================================================
        // 디퓨즈(툰 램프) + 추가 광원 + 앰비언트(AO 포함) + 프레넬 림을
        // 합산하여 최종 색상을 출력한다.
        // Character 셰이더와 달리 스페큘러가 없고, 대신 디테일 텍스처,
        // 높이 그라데이션 등 환경 전용 기능이 추가되어 있다.
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // ── URP 멀티 컴파일 키워드 ──
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            // ── 로컬 셰이더 피처 토글 ──
            // shader_feature_local: 머티리얼별로 켜고 끌 수 있는 키워드.
            // 사용하지 않는 조합은 빌드 시 컴파일되지 않아 성능에 유리.
            #pragma shader_feature_local _USE_VERTEX_COLOR     // 버텍스 컬러 사용
            #pragma shader_feature_local _USE_DETAIL           // 디테일 텍스처 사용
            #pragma shader_feature_local _USE_HEIGHT_GRADIENT  // 높이 그라데이션 사용
            #pragma shader_feature_local _USE_FRESNEL          // 프레넬 림 사용

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ── 상수 정의 ──
            #define RECEIVER_NORMAL_BIAS_SCALE 0.02   // 리시버 측 노말 바이어스 단위 변환 (Inspector 값 → 월드 스케일)
            #define RECEIVER_DEPTH_BIAS_SCALE  0.005  // 리시버 측 뎁스 바이어스 단위 변환
            #define ADDITIONAL_LIGHT_SCALE     0.4    // 추가 광원의 메인 라이트 대비 강도 비율

            // ── 머티리얼 상수 버퍼 (SRP Batcher 호환) ──
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _ShadowThreshold;
                half _ShadowSmoothness;
                half4 _ShadowColor;
                half _ShadowIntensity;
                half4 _AmbientColor;
                half _AmbientIntensity;
                half _AmbientOcclusionStrength;
                float4 _DetailTex_ST;       // 디테일 텍스처 타일링/오프셋
                half _DetailStrength;
                half4 _GradientBottomColor;
                half4 _GradientTopColor;
                float _GradientBottomY;
                float _GradientTopY;
                half _GradientBlend;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelIntensity;
                float _ShadowDepthBias;
                float _ShadowNormalBias;
            CBUFFER_END

            TEXTURE2D(_MainTex);             // 알베도 텍스처
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DetailTex);           // 디테일 텍스처
            SAMPLER(sampler_DetailTex);

            struct Attributes
            {
                float4 positionOS : POSITION;  // 오브젝트 공간 위치
                float3 normalOS   : NORMAL;    // 오브젝트 공간 노말
                float2 uv         : TEXCOORD0; // UV 좌표
                float4 color      : COLOR;     // 버텍스 컬러 (RGB: 색상, A: AO)
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION; // 클립 공간 위치
                float2 uv          : TEXCOORD0;   // 메인 텍스처 UV
                float2 uvDetail    : TEXCOORD1;   // 디테일 텍스처 UV (별도 타일링 가능)
                float3 normalWS    : TEXCOORD2;   // 월드 노말
                float3 positionWS  : TEXCOORD3;   // 월드 위치
                float  fogFactor   : TEXCOORD4;   // 포그 팩터
                float4 shadowCoord : TEXCOORD5;   // 섀도우 좌표 (이 셰이더에서는 픽셀에서 재계산)
                float4 vertexColor : TEXCOORD6;   // 버텍스 컬러 (RGB: 색상, A: AO)
            };

            // ── 소프트 툰 램프 ──
            // Character 셰이더와 동일한 함수. smoothstep 기반 부드러운 명암 경계.
            half SoftToonRamp(half NdotL, half threshold, half smoothness)
            {
                half lower = threshold - smoothness * 0.5;
                half upper = threshold + smoothness * 0.5;
                return smoothstep(lower, upper, NdotL);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvDetail = TRANSFORM_TEX(IN.uv, _DetailTex);    // 디테일 텍스처는 별도 타일링 적용
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                OUT.vertexColor = IN.color;

                // 섀도우 좌표는 프래그먼트에서 픽셀별로 계산 (보간 아티팩트 방지)
                OUT.shadowCoord = float4(0, 0, 0, 0);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1) 알베도: 텍스처 × 기본 색상
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;

                // 2) 버텍스 컬러 적용 (옵션)
                // 버텍스 컬러를 곱하여 핸드페인팅 에셋의 색상을 반영
                #ifdef _USE_VERTEX_COLOR
                    albedo.rgb *= IN.vertexColor.rgb;
                #endif

                // 3) 디테일 텍스처 블렌딩 (옵션)
                // detail * 2.0을 곱하여 Overlay 블렌딩 효과:
                //   detail < 0.5 → 어두워짐, detail > 0.5 → 밝아짐, detail = 0.5 → 변화 없음
                #ifdef _USE_DETAIL
                    half3 detail = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, IN.uvDetail).rgb;
                    albedo.rgb = lerp(albedo.rgb, albedo.rgb * detail * 2.0, _DetailStrength);
                #endif

                // 4) 높이 그라데이션 (옵션)
                // 월드 Y 좌표를 Bottom~Top 범위로 정규화(0~1)하여
                // 하단 색과 상단 색을 보간한 뒤 알베도에 곱한다.
                #ifdef _USE_HEIGHT_GRADIENT
                    half heightT = saturate((IN.positionWS.y - _GradientBottomY) / max(_GradientTopY - _GradientBottomY, 0.001));
                    half3 gradientColor = lerp(_GradientBottomColor.rgb, _GradientTopColor.rgb, heightT);
                    albedo.rgb = lerp(albedo.rgb, albedo.rgb * gradientColor, _GradientBlend);
                #endif

                half3 normalWS = normalize(IN.normalWS);

                // 5) 픽셀별 그림자 좌표 계산 (리시버 측 바이어스 적용)
                // 버텍스에서 보간하면 그림자 경계가 왜곡될 수 있으므로 픽셀에서 직접 계산.
                // normalBias: 노말 방향으로 밀어서 shadow acne 방지
                // depthBias: z값에 오프셋을 더해서 추가 보정
                float3 biasedPosWS = IN.positionWS + normalWS * _ShadowNormalBias * RECEIVER_NORMAL_BIAS_SCALE;
                float4 shadowCoord = TransformWorldToShadowCoord(biasedPosWS);
                shadowCoord.z += _ShadowDepthBias * RECEIVER_DEPTH_BIAS_SCALE;

                // 6) 메인 라이트 정보
                Light mainLight = GetMainLight(shadowCoord);
                half3 lightDir = normalize(mainLight.direction);
                half3 lightColor = mainLight.color;

                // ── 소프트 디퓨즈 (툰 램프) ──
                half NdotL = dot(normalWS, lightDir);
                half toonRamp = SoftToonRamp(NdotL, _ShadowThreshold, _ShadowSmoothness);
                half shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                toonRamp *= shadowAtten;

                half3 shadowTint = lerp(_ShadowColor.rgb, half3(1, 1, 1), toonRamp);
                shadowTint = lerp(half3(1, 1, 1), shadowTint, _ShadowIntensity);
                half3 diffuse = albedo.rgb * lightColor * shadowTint;

                // ── 추가 광원 ──
                half3 additionalLight = half3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                    uint additionalLightCount = GetAdditionalLightsCount();
                    for (uint i = 0; i < additionalLightCount; i++)
                    {
                        Light addLight = GetAdditionalLight(i, IN.positionWS);
                        half addNdotL = dot(normalWS, normalize(addLight.direction));
                        half addRamp = SoftToonRamp(addNdotL, _ShadowThreshold, _ShadowSmoothness);
                        addRamp *= addLight.shadowAttenuation * addLight.distanceAttenuation;
                        // 추가 광원은 0.4를 곱하여 메인 라이트보다 약하게
                        additionalLight += albedo.rgb * addLight.color * addRamp * ADDITIONAL_LIGHT_SCALE;
                    }
                #endif

                // ── 앰비언트 + AO ──
                // vertexColor.a를 AO로 사용: 값이 낮을수록 앰비언트가 줄어듦
                // _AmbientOcclusionStrength가 0이면 ao=1로 AO 효과 없음
                half ao = lerp(1.0, IN.vertexColor.a, _AmbientOcclusionStrength);
                half3 ambient = albedo.rgb * _AmbientColor.rgb * _AmbientIntensity * ao;

                // ── 프레넬 림라이트 (옵션) ──
                // 가장자리(NdotV가 작은 곳)에서 밝아지는 프레넬 효과.
                // Character 셰이더와 달리 라이트 방향 마스크가 없어 전방향에서 보임.
                #ifdef _USE_FRESNEL
                    half3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                    half fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower);
                    half3 rim = _FresnelColor.rgb * fresnel * _FresnelIntensity;
                #else
                    half3 rim = half3(0, 0, 0);
                #endif

                // ── 최종 합산 ──
                half3 finalColor = diffuse + additionalLight + ambient + rim;
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 1: 섀도우 캐스터 (커스텀 바이어스)
        // ============================================================
        // 그림자 맵에 깊이를 기록하는 패스.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0  // 깊이만 기록
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ── 상수 정의 ──
            #define CASTER_BIAS_SCALE 0.01  // 캐스터 측 바이어스 단위 변환 (Inspector 값 → 월드 스케일)

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _ShadowThreshold;
                half _ShadowSmoothness;
                half4 _ShadowColor;
                half _ShadowIntensity;
                half4 _AmbientColor;
                half _AmbientIntensity;
                half _AmbientOcclusionStrength;
                float4 _DetailTex_ST;
                half _DetailStrength;
                half4 _GradientBottomColor;
                half4 _GradientTopColor;
                float _GradientBottomY;
                float _GradientTopY;
                half _GradientBlend;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelIntensity;
                float _ShadowDepthBias;
                float _ShadowNormalBias;
            CBUFFER_END

            float3 _LightDirection; // URP가 설정하는 메인 라이트 방향

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            // ── 캐스터 측 커스텀 바이어스 ──
            // shadow acne를 방지하기 위해 그림자를 드리우는 지오메트리를
            // 라이트 방향(depth bias)과 표면 노말 방향(normal bias)으로 밀어낸다.
            float3 ApplyCustomShadowBias(float3 posWS, float3 normalWS, float3 lightDir, float depthBias, float normalBias)
            {
                posWS -= lightDir * depthBias * CASTER_BIAS_SCALE;          // 라이트 반대 방향으로 밀기
                float invNdotL = 1.0 - saturate(dot(normalWS, lightDir));  // 빗각 계수
                posWS += normalWS * normalBias * invNdotL * CASTER_BIAS_SCALE; // 노말 방향으로 밀기
                return posWS;
            }

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);

                posWS = ApplyCustomShadowBias(posWS, normWS, _LightDirection, _ShadowDepthBias, _ShadowNormalBias);
                OUT.positionCS = TransformWorldToHClip(posWS);

                // 깊이 클램핑
                #if UNITY_REVERSED_Z
                    OUT.positionCS.z = min(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionCS.z = max(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 2: 깊이 전용 (Depth Only)
        // ============================================================
        // URP 깊이 프리패스. SSAO, EdgeDetection 등 깊이 기반 이펙트에 사용.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _ShadowThreshold;
                half _ShadowSmoothness;
                half4 _ShadowColor;
                half _ShadowIntensity;
                half4 _AmbientColor;
                half _AmbientIntensity;
                half _AmbientOcclusionStrength;
                float4 _DetailTex_ST;
                half _DetailStrength;
                half4 _GradientBottomColor;
                half4 _GradientTopColor;
                float _GradientBottomY;
                float _GradientTopY;
                half _GradientBlend;
                half4 _FresnelColor;
                half _FresnelPower;
                half _FresnelIntensity;
                float _ShadowDepthBias;
                float _ShadowNormalBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
