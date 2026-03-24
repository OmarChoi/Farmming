// =============================================================================
// SoftToon/Character 셰이더
// =============================================================================
// 캐릭터 전용 소프트 툰 셰이딩 셰이더.
// smoothstep 기반의 부드러운 명암 경계를 사용하여 일반적인 하드 툰 셰이딩보다
// 자연스러운 느낌을 준다.
//
// 구성 Pass:
//   Pass 0 (ForwardLit)  - 메인 툰 셰이딩 (디퓨즈 + 스페큘러 + 림라이트 + 추가광원 + 앰비언트)
//   Pass 1 (Outline)     - 인버티드 헐(Inverted Hull) 방식 외곽선
//   Pass 2 (ShadowCaster)- 그림자 맵에 깊이를 기록 (커스텀 바이어스 적용)
//   Pass 3 (DepthOnly)   - 깊이 프리패스 (Depth Prepass)
// =============================================================================
Shader "Custom/SoftToon/Character"
{
    Properties
    {
        // ── 기본 텍스처/색상 ──
        [Header(Base)]
        _MainTex ("Albedo Texture", 2D) = "white" {}       // 기본 알베도(색상) 텍스처
        _BaseColor ("Base Color Tint", Color) = (1, 1, 1, 1) // 텍스처에 곱해지는 색상 틴트

        // ── 툰 셰이딩 파라미터 ──
        // SoftToonRamp 함수에서 smoothstep(threshold-smooth/2, threshold+smooth/2, NdotL)로
        // 그림자 경계의 위치와 부드러움을 제어한다.
        [Header(Toon Shading)]
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = 0.0        // 그림자 경계 위치 (NdotL 기준, 0이면 90도)
        _ShadowSmoothness ("Shadow Smoothness", Range(0.001, 1.0)) = 0.3 // 그림자 경계의 부드러움 (값이 클수록 그라데이션이 넓음)
        _ShadowColor ("Shadow Color", Color) = (0.6, 0.5, 0.7, 1)       // 그림자 영역의 색조 (보라빛으로 설정하면 따뜻한 느낌)
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.6         // 그림자 색 적용 강도 (0이면 그림자 색 무시)

        // ── 림라이트 (프레넬 기반) ──
        // 카메라에서 보았을 때 오브젝트 가장자리에 밝은 빛 테두리를 만든다.
        // 빛이 비치는 쪽에만 나타나도록 rimMask를 적용한다.
        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 0.95, 0.85, 1)          // 림라이트 색상
        _RimPower ("Rim Power", Range(0.5, 10.0)) = 3.0              // 림라이트 폭 (값이 클수록 가장자리에만 집중)
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.6           // 림라이트 밝기
        _RimSmoothness ("Rim Smoothness", Range(0.001, 1.0)) = 0.3   // 림라이트 경계 부드러움

        // ── 스페큘러 (Blinn-Phong 기반) ──
        // halfDir = normalize(lightDir + viewDir) 기반 하이라이트.
        // smoothstep으로 툰 느낌의 스페큘러 경계를 만든다.
        [Header(Specular)]
        _SpecColor2 ("Specular Color", Color) = (1, 1, 1, 1)         // 스페큘러 색상 (_SpecColor는 URP 내장 이름과 충돌하므로 _SpecColor2 사용)
        _SpecSmoothness ("Specular Smoothness", Range(0.001, 1.0)) = 0.1 // 스페큘러 경계 부드러움
        _SpecPower ("Specular Power", Range(1, 128)) = 32            // 스페큘러 집중도 (값이 클수록 작고 날카로운 하이라이트)
        _SpecIntensity ("Specular Intensity", Range(0, 2)) = 0.3     // 스페큘러 밝기

        // ── 아웃라인 (인버티드 헐) ──
        // Pass 1에서 Cull Front로 뒷면만 렌더링하고 노말 방향으로 확장하여 외곽선을 만든다.
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.2, 0.15, 0.15, 1) // 외곽선 색상
        _OutlineWidth ("Outline Width", Range(0, 0.03)) = 0.004       // 외곽선 두께 (오브젝트 스페이스 단위)

        // ── 앰비언트 (환경광) ──
        // 씬의 기본 환경광 색상. 그림자 영역에서도 완전히 검게 되지 않도록 해준다.
        [Header(Ambient)]
        _AmbientColor ("Ambient Color", Color) = (0.85, 0.85, 0.9, 1) // 앰비언트 색상
        _AmbientIntensity ("Ambient Intensity", Range(0, 1)) = 0.15   // 앰비언트 강도
        _MinLight ("Minimum Light", Range(0, 1)) = 0.35       

        // ── 그림자 Acne 보정 ──
        // 셀프 셰도잉(shadow acne) 아티팩트를 줄이기 위한 바이어스 값.
        // ShadowCaster 패스와 메인 패스 양쪽에서 사용된다.
        [Header(Shadow Acne Fix)]
        _ShadowDepthBias ("Shadow Depth Bias", Range(0, 10)) = 1.5   // 라이트 방향으로 밀어내는 깊이 바이어스
        _ShadowNormalBias ("Shadow Normal Bias", Range(0, 10)) = 1.0  // 표면 노말 방향으로 밀어내는 노말 바이어스
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"              // 불투명 렌더 타입
            "RenderPipeline" = "UniversalPipeline" // URP 전용
            "Queue" = "Geometry"                   // 일반 지오메트리 큐
        }

        // ============================================================
        // Pass 0: 메인 툰 셰이딩 패스 (ForwardLit)
        // ============================================================
        // 디퓨즈(툰 램프) + 스페큘러(Blinn-Phong) + 림라이트(프레넬) +
        // 추가 광원 + 앰비언트를 합산하여 최종 색상을 출력한다.
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" } // URP Forward 렌더링 경로에서 실행

            Cull Back   // 앞면만 렌더링
            ZWrite On   // 깊이 버퍼에 기록

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // ── 멀티 컴파일 키워드 (URP 그림자 및 조명 변형) ──
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE  // 메인 라이트 그림자 (캐스케이드 포함)
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS       // 추가 광원 (버텍스/픽셀)
            #pragma multi_compile _ _SHADOWS_SOFT                                      // 소프트 섀도우
            #pragma multi_compile_fog                                                  // 포그

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"     // URP 핵심 함수
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"  // 조명 관련 함수 (GetMainLight 등)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"   // 그림자 관련 함수

            // ── 상수 정의 ──
            #define RECEIVER_NORMAL_BIAS_SCALE 0.005  // 리시버 측 노말 바이어스 단위 변환 (Inspector 값 → 월드 스케일)
            #define RIM_MASK_NDOTL_MIN       -0.1     // 림라이트 마스크 시작점 (이 NdotL 이하에서는 림 없음)
            #define RIM_MASK_NDOTL_MAX        0.3     // 림라이트 마스크 끝점 (이 NdotL 이상에서 림 최대)
            #define ADDITIONAL_LIGHT_SCALE    0.5     // 추가 광원의 메인 라이트 대비 강도 비율

            // ── 머티리얼 프로퍼티 상수 버퍼 ──
            // SRP Batcher 호환을 위해 모든 머티리얼 프로퍼티를 하나의 CBUFFER에 선언한다.
            // 모든 패스에서 동일한 레이아웃을 유지해야 SRP Batcher가 동작한다.
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;          // 텍스처 타일링(xy) 및 오프셋(zw)
                half4 _BaseColor;            // 기본 색상 틴트
                half _ShadowThreshold;       // 툰 그림자 경계 위치
                half _ShadowSmoothness;      // 툰 그림자 경계 부드러움
                half4 _ShadowColor;          // 그림자 색조
                half _ShadowIntensity;       // 그림자 색 적용 강도
                half4 _RimColor;             // 림라이트 색상
                half _RimPower;              // 림라이트 프레넬 지수
                half _RimIntensity;          // 림라이트 강도
                half _RimSmoothness;         // 림라이트 경계 부드러움
                half4 _SpecColor2;           // 스페큘러 색상
                half _SpecSmoothness;        // 스페큘러 경계 부드러움
                half _SpecPower;             // 스페큘러 지수 (Blinn-Phong)
                half _SpecIntensity;         // 스페큘러 강도
                half4 _OutlineColor;         // 외곽선 색상 (이 패스에서는 미사용, CBUFFER 일관성을 위해 포함)
                float _OutlineWidth;         // 외곽선 두께 (이 패스에서는 미사용)
                half4 _AmbientColor;         // 앰비언트 색상
                half _AmbientIntensity;      // 앰비언트 강도
                half _MinLight;              // 그림자 영역 최소 밝기
                float _ShadowDepthBias;      // 그림자 깊이 바이어스
                float _ShadowNormalBias;     // 그림자 노말 바이어스
            CBUFFER_END

            // ── 텍스처 선언 ──
            TEXTURE2D(_MainTex);             // 알베도 텍스처
            SAMPLER(sampler_MainTex);        // 텍스처 샘플러

            // ── 버텍스 입력 구조체 ──
            struct Attributes
            {
                float4 positionOS : POSITION;  // 오브젝트 공간 위치
                float3 normalOS   : NORMAL;    // 오브젝트 공간 노말
                float2 uv         : TEXCOORD0; // UV 좌표
            };

            // ── 버텍스→프래그먼트 전달 구조체 ──
            struct Varyings
            {
                float4 positionCS  : SV_POSITION; // 클립 공간 위치 (래스터라이저 입력)
                float2 uv          : TEXCOORD0;   // UV 좌표
                float3 normalWS    : TEXCOORD1;   // 월드 공간 노말
                float3 positionWS  : TEXCOORD2;   // 월드 공간 위치
                float3 viewDirWS   : TEXCOORD3;   // 카메라→정점 방향 벡터 (월드)
                float  fogFactor   : TEXCOORD4;   // 포그 팩터
                float4 shadowCoord : TEXCOORD5;   // 섀도우 맵 샘플링 좌표
            };

            // ── 버텍스 셰이더 ──
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // URP 유틸리티를 사용하여 오브젝트→클립/월드 공간 변환
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;                             // 클립 공간 위치
                OUT.positionWS = posInputs.positionWS;                             // 월드 공간 위치
                OUT.normalWS = normInputs.normalWS;                                // 월드 공간 노말
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);                          // 타일링/오프셋 적용된 UV
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS); // 정규화된 뷰 방향
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);          // 깊이 기반 포그 계산

                // 리시버 측 그림자 바이어스: 노말 방향으로 약간 밀어내서 shadow acne를 줄임
                // (그림자를 받는 표면의 샘플링 위치를 노말 방향으로 오프셋)
                float3 biasedPosWS = posInputs.positionWS + normInputs.normalWS * _ShadowNormalBias * RECEIVER_NORMAL_BIAS_SCALE;
                OUT.shadowCoord = TransformWorldToShadowCoord(biasedPosWS);

                return OUT;
            }

            // ── 소프트 툰 램프 함수 ──
            // NdotL 값을 smoothstep으로 0~1로 매핑하여 부드러운 명암 경계를 만든다.
            // threshold: 경계 중심 위치, smoothness: 경계 폭
            // 예) threshold=0, smoothness=0.3이면 NdotL이 -0.15~0.15 구간에서 그라데이션
            half SoftToonRamp(half NdotL, half threshold, half smoothness)
            {
                half lower = threshold - smoothness * 0.5; // 그라데이션 시작점
                half upper = threshold + smoothness * 0.5; // 그라데이션 끝점
                return smoothstep(lower, upper, NdotL);    // 하한~상한 범위에서 0→1 보간
            }

            // ── 프래그먼트(픽셀) 셰이더 ──
            half4 frag(Varyings IN) : SV_Target
            {
                // 1) 알베도: 텍스처 색상 × 기본 색상 틴트
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;

                // 2) 노말 및 뷰 방향 정규화 (보간 후 길이가 1이 아닐 수 있으므로)
                half3 normalWS = normalize(IN.normalWS);
                half3 viewDirWS = normalize(IN.viewDirWS);

                // 3) 메인 라이트 정보 가져오기 (그림자 좌표 포함)
                Light mainLight = GetMainLight(IN.shadowCoord);
                half3 lightDir = normalize(mainLight.direction);  // 라이트 방향
                half3 lightColor = mainLight.color;               // 라이트 색상

                // ── 소프트 디퓨즈 (툰 램프 기반) ──
                half NdotL = dot(normalWS, lightDir);             // 노말·라이트 내적 (-1~1)
                half toonRamp = SoftToonRamp(NdotL, _ShadowThreshold, _ShadowSmoothness); // 0~1 툰 램프
                half shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation; // 그림자맵 감쇠 × 거리 감쇠
                toonRamp *= shadowAtten;                          // 실시간 그림자를 툰 램프에 곱함

                // 그림자 색 적용: toonRamp가 0(어두움)이면 _ShadowColor, 1(밝음)이면 흰색
                half3 shadowTint = lerp(_ShadowColor.rgb, half3(1, 1, 1), toonRamp);
                // _ShadowIntensity로 그림자 색의 영향력 조절 (0이면 그림자 색 무효)
                shadowTint = lerp(half3(1, 1, 1), shadowTint, _ShadowIntensity);
                half minLit = lerp(_MinLight, 1.0h, toonRamp);
                half3 diffuse = albedo.rgb * lightColor * shadowTint; // 최종 디퓨즈 색상

                // ── 스페큘러 (Blinn-Phong, 소프트 경계) ──
                half3 halfDir = normalize(lightDir + viewDirWS);      // 하프 벡터 (라이트+뷰의 중간)
                half NdotH = saturate(dot(normalWS, halfDir));        // 노말·하프벡터 내적 (0~1)
                half specRaw = pow(NdotH, _SpecPower);                // Blinn-Phong 스페큘러 원시값
                // smoothstep으로 스페큘러 경계를 부드럽게 만들어 툰 느낌 유지
                half spec = smoothstep(0.5 - _SpecSmoothness, 0.5 + _SpecSmoothness, specRaw);
                // toonRamp를 곱해서 그림자 영역에서는 스페큘러가 나타나지 않도록 함
                half3 specular = spec * _SpecColor2.rgb * _SpecIntensity * lightColor * toonRamp;

                // ── 림라이트 (프레넬 기반, 소프트, 라이트 방향 마스크) ──
                half NdotV = saturate(dot(normalWS, viewDirWS));      // 노말·뷰 내적 (0~1)
                half rimRaw = pow(1.0 - NdotV, _RimPower);           // 프레넬: 가장자리(NdotV≈0)에서 강해짐
                // smoothstep으로 림라이트 경계를 부드럽게
                half rim = smoothstep(0.5 - _RimSmoothness, 0.5 + _RimSmoothness, rimRaw);
                // 라이트가 비치는 쪽에만 림라이트 표시 (NdotL > -0.1인 영역)
                half rimMask = smoothstep(RIM_MASK_NDOTL_MIN, RIM_MASK_NDOTL_MAX, NdotL);
                half3 rimColor = rim * rimMask * _RimColor.rgb * _RimIntensity;

                // ── 추가 광원 (Additional Lights) ──
                half3 additionalLight = half3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS
                    uint additionalLightCount = GetAdditionalLightsCount(); // 씬의 추가 광원 수
                    for (uint i = 0; i < additionalLightCount; i++)
                    {
                        Light addLight = GetAdditionalLight(i, IN.positionWS); // i번째 추가 광원 정보
                        half addNdotL = dot(normalWS, normalize(addLight.direction));
                        half addRamp = SoftToonRamp(addNdotL, _ShadowThreshold, _ShadowSmoothness);
                        addRamp *= addLight.shadowAttenuation * addLight.distanceAttenuation;
                        half addMinLit = lerp(_MinLight, 1.0h, addRamp); // 추가 광원도 암부 최소 밝기 보장
                        // 추가 광원은 0.5를 곱하여 메인 라이트보다 약하게 적용
                        additionalLight += albedo.rgb * addLight.color * addRamp * ADDITIONAL_LIGHT_SCALE;
                    }
                #endif

                // ── 앰비언트 (환경광) ──
                // 전체적으로 더해지는 기본 환경광. 그림자 영역도 완전히 검지 않게 만듦
                half3 ambient = albedo.rgb * _AmbientColor.rgb * _AmbientIntensity;

                // ── 최종 합산 ──
                half3 finalColor = diffuse + specular + rimColor + additionalLight + ambient;
                finalColor = MixFog(finalColor, IN.fogFactor); // 포그 적용

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 1: 아웃라인 (인버티드 헐 방식)
        // ============================================================
        // 원리: Cull Front로 앞면을 제거하고 뒷면만 렌더링한다.
        // 버텍스를 노말 방향으로 _OutlineWidth만큼 확장(extrude)하면,
        // 확장된 뒷면이 원래 메시 바깥에 보여서 외곽선처럼 보인다.
        //
        // LightMode = "SRPDefaultUnlit"으로 설정하여 URP의 기본 Unlit 패스에서 실행된다.
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front  // 앞면 제거 → 뒷면(확장된 부분)만 렌더링
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // SRP Batcher 호환을 위해 메인 패스와 동일한 CBUFFER 레이아웃 유지
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _ShadowThreshold;
                half _ShadowSmoothness;
                half4 _ShadowColor;
                half _ShadowIntensity;
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
                half _RimSmoothness;
                half4 _SpecColor2;
                half _SpecSmoothness;
                half _SpecPower;
                half _SpecIntensity;
                half4 _OutlineColor;
                float _OutlineWidth;
                half4 _AmbientColor;
                half _AmbientIntensity;
                half _MinLight;
                float _ShadowDepthBias;
                float _ShadowNormalBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION; // 오브젝트 공간 위치
                float3 normalOS   : NORMAL;   // 오브젝트 공간 노말
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION; // 클립 공간 위치
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // 오브젝트 공간에서 노말 방향으로 정점을 확장하여 외곽선 생성
                // 장점: 구현이 간단. 단점: 오브젝트 스케일에 따라 두께가 변함
                float3 expandedPos = IN.positionOS.xyz + IN.normalOS * _OutlineWidth;
                OUT.positionCS = TransformObjectToHClip(expandedPos);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 외곽선은 단색으로 출력
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 2: 섀도우 캐스터 (커스텀 바이어스 적용)
        // ============================================================
        // 이 오브젝트가 다른 오브젝트에 드리우는 그림자를 섀도우 맵에 기록한다.
        // 커스텀 깊이/노말 바이어스를 적용하여 shadow acne(자기 그림자 줄무늬)를 방지한다.
        //
        // ColorMask 0: 색상 출력 없음 (깊이만 기록)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0  // 깊이만 기록, 색상 버퍼에는 쓰지 않음
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_shadowcaster // 섀도우 캐스터 변형 컴파일

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ── 상수 정의 ──
            #define CASTER_BIAS_SCALE 0.01  // 캐스터 측 바이어스 단위 변환 (Inspector 값 → 월드 스케일)

            // SRP Batcher 호환을 위한 동일 CBUFFER
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _ShadowThreshold;
                half _ShadowSmoothness;
                half4 _ShadowColor;
                half _ShadowIntensity;
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
                half _RimSmoothness;
                half4 _SpecColor2;
                half _SpecSmoothness;
                half _SpecPower;
                half _SpecIntensity;
                half4 _OutlineColor;
                float _OutlineWidth;
                half4 _AmbientColor;
                half _AmbientIntensity;
                half _MinLight;
                float _ShadowDepthBias;
                float _ShadowNormalBias;
            CBUFFER_END

            float3 _LightDirection; // URP가 자동으로 설정하는 메인 라이트 방향

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            // ── 캐스터 측 커스텀 그림자 바이어스 ──
            // shadow acne를 방지하기 위해 그림자를 드리우는 지오메트리를
            // 라이트 방향(depth bias)과 표면 노말 방향(normal bias)으로 밀어낸다.
            //
            // depthBias: 라이트 방향으로 밀어내기 (라이트에서 멀어짐 → 그림자가 뒤로 밀림)
            // normalBias: 노말 방향으로 밀어내기 (표면에서 떨어짐)
            //   - invNdotL: 빛이 비스듬할수록(grazing angle) 더 많이 밀어냄
            //     빛이 수직(NdotL≈1)이면 바이어스가 거의 없고,
            //     빛이 수평(NdotL≈0)이면 바이어스가 최대
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

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);       // 오브젝트→월드
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);      // 노말 변환

                // 1) 커스텀 바이어스 적용
                posWS = ApplyCustomShadowBias(posWS, normWS, _LightDirection, _ShadowDepthBias, _ShadowNormalBias);
                // 2) URP 내장 바이어스도 추가 적용 (이중 바이어스로 안전하게)
                posWS = ApplyShadowBias(posWS, normWS, _LightDirection);
                OUT.positionCS = TransformWorldToHClip(posWS);

                // 깊이 클램핑: near plane 뒤로 밀려나지 않도록 보정
                #if UNITY_REVERSED_Z
                    OUT.positionCS.z = min(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionCS.z = max(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return OUT;
            }

            // 프래그먼트는 깊이만 기록하므로 색상 출력 불필요
            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 3: 깊이 전용 패스 (Depth Only)
        // ============================================================
        // 깊이 프리패스(Depth Prepass)로, 깊이 버퍼에만 값을 기록한다.
        // URP의 SSAO, 깊이 기반 이펙트(예: EdgeDetection) 등에서 사용된다.
        // ColorMask R: 빨강 채널만 기록 (깊이 텍스처 포맷에 맞춤)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R  // R 채널만 기록 (깊이 텍스처용)
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz); // 단순 변환
                return OUT;
            }

            half4 DepthFrag(Varyings IN) : SV_Target
            {
                return 0; // 색상 무관, 깊이만 기록됨
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit" // 이 셰이더가 지원되지 않는 환경에서 URP Lit으로 폴백
}
