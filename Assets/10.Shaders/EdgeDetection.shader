// =============================================================================
// EdgeDetection 포스트 프로세싱 셰이더
// =============================================================================
// 화면 전체에 적용되는 포스트 프로세싱 셰이더로,
// 깊이(Depth)와 노말(Normal) 차이를 기반으로 외곽선을 검출한다.
//
// Roberts Cross 연산자를 사용하여 인접 픽셀 간의 차이를 계산하고,
// 일정 임계값(threshold)을 넘으면 외곽선으로 판정한다.
//
// 구성 Pass:
//   Pass 0 (EdgeDetection) - 외곽선 검출 후 씬 색상과 블렌딩
//   Pass 1 (CopyBack)      - 임시 텍스처에서 원본 렌더 타겟으로 복사
//
// "Hidden/" 접두사: 셰이더 선택 드롭다운에 표시되지 않음 (코드에서만 사용)
// EdgeDetectionFeature.cs (ScriptableRendererFeature)를 통해 URP 렌더 파이프라인에 삽입됨.
// =============================================================================
Shader "Hidden/Custom/EdgeDetection"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        // ============================================================
        // Pass 0: 외곽선 검출 (Edge Detection)
        // ============================================================
        // 원본 씬 색상(source)을 입력받아 외곽선을 검출한 뒤,
        // 외곽선 부분만 _EdgeColor로 덮어쓴 결과를 출력한다.
        //
        // 렌더 상태:
        //   ZWrite Off  - 깊이 기록 안 함 (화면 후처리이므로)
        //   ZTest Always - 깊이 테스트 무시
        //   Cull Off     - 풀스크린 쿼드이므로 컬링 불필요
        Pass
        {
            Name "EdgeDetection"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert   // Blit.hlsl에서 제공하는 풀스크린 버텍스 셰이더
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"   // SampleSceneDepth 제공
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl" // SampleSceneNormals 제공
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"                   // 풀스크린 Vert, _BlitTexture 제공

            // ── 외곽선 검출 파라미터 (EdgeDetectionFeature.cs에서 설정) ──
            CBUFFER_START(EdgeDetectionParams)
                half4 _EdgeColor;       // 외곽선 색상
                half _DepthThreshold;   // 깊이 차이 임계값 (이 값 이상이면 외곽선)
                half _NormalThreshold;  // 노말 차이 임계값
                half _EdgeThickness;    // 외곽선 두께 (샘플링 간격의 배수)
            CBUFFER_END

            // ── 선형 깊이 샘플링 ──
            // 원시 깊이 버퍼 값(비선형)을 카메라 거리(미터 단위 선형)로 변환한다.
            // 선형 깊이를 사용해야 가까운 곳과 먼 곳에서 일관된 외곽선 검출이 가능.
            float SampleLinearDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);                // 0~1 비선형 깊이
                return LinearEyeDepth(rawDepth, _ZBufferParams);      // 선형 카메라 거리
            }

            // ── 노말 샘플링 ──
            // URP의 _CameraNormalsTexture에서 월드 노말을 가져온다.
            // DepthNormals 프리패스가 활성화되어 있어야 동작한다.
            half3 SampleNormal(float2 uv)
            {
                return SampleSceneNormals(uv);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                // texel: 한 픽셀의 UV 크기 × 두께 배수
                // _EdgeThickness가 클수록 더 먼 픽셀을 샘플링하여 두꺼운 외곽선
                float2 texel = (1.0 / _ScreenParams.xy) * _EdgeThickness;

                // ── Roberts Cross 연산 - 깊이 기반 외곽선 ──
                // Roberts Cross: 대각선 방향 2쌍의 차이를 구하는 2x2 미분 연산자.
                // Sobel(3x3)보다 가볍고 얇은 외곽선을 생성한다.
                //
                // 샘플링 패턴:
                //   d0(좌상) ─── d1(우상)
                //      │    ╲╱     │
                //      │    ╱╲     │
                //   d2(좌하) ─── d3(우하)
                //
                // 교차 차이: |d0 - d3| + |d1 - d2|
                float d0 = SampleLinearDepth(uv + float2(-texel.x, -texel.y)); // 좌상
                float d1 = SampleLinearDepth(uv + float2( texel.x, -texel.y)); // 우상
                float d2 = SampleLinearDepth(uv + float2(-texel.x,  texel.y)); // 좌하
                float d3 = SampleLinearDepth(uv + float2( texel.x,  texel.y)); // 우하

                // 중심 깊이로 나누어 상대적 차이를 구함 (거리 정규화)
                // → 먼 곳에서도 가까운 곳과 동일한 감도로 외곽선 검출
                float centerDepth = SampleLinearDepth(uv);
                float depthDiff = (abs(d0 - d3) + abs(d1 - d2));
                float depthEdge = step(_DepthThreshold, depthDiff / max(centerDepth, 0.001));
                // step: 임계값 이상이면 1(외곽선), 미만이면 0

                // ── Roberts Cross 연산 - 노말 기반 외곽선 ──
                // 깊이가 같아도 노말이 다르면 외곽선으로 검출 (같은 평면의 접힌 부분 등)
                half3 n0 = SampleNormal(uv + float2(-texel.x, -texel.y));
                half3 n1 = SampleNormal(uv + float2( texel.x, -texel.y));
                half3 n2 = SampleNormal(uv + float2(-texel.x,  texel.y));
                half3 n3 = SampleNormal(uv + float2( texel.x,  texel.y));

                // 노말 차이의 각 성분 합산 (dot(diff, (1,1,1)) = diff.x + diff.y + diff.z)
                half3 normalDiff = abs(n0 - n3) + abs(n1 - n2);
                half normalEdge = step(_NormalThreshold, dot(normalDiff, half3(1, 1, 1)));

                // ── 최종 외곽선 합산 ──
                // 깊이 외곽선 또는 노말 외곽선 중 하나라도 검출되면 외곽선으로 판정
                half edge = saturate(depthEdge + normalEdge);

                // ── 씬 색상과 외곽선 블렌딩 ──
                // _BlitTexture: EdgeDetectionFeature에서 설정한 원본 씬 렌더 결과
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                // edge가 1이면 _EdgeColor, 0이면 원본 씬 색상
                half3 finalColor = lerp(sceneColor.rgb, _EdgeColor.rgb, edge);

                return half4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }

        // ============================================================
        // Pass 1: 단순 복사 (Copy Back)
        // ============================================================
        // Pass 0에서 임시 텍스처(temp)에 기록한 결과를
        // 원래 렌더 타겟(source)으로 다시 복사하는 패스.
        //
        // 이유: RenderGraph에서는 동일 텍스처를 동시에 읽고 쓸 수 없으므로,
        // source → temp (Pass 0) → source (Pass 1) 두 단계로 처리해야 한다.
        Pass
        {
            Name "CopyBack"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert   // Blit.hlsl의 풀스크린 버텍스 셰이더
            #pragma fragment FragCopy

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // 입력 텍스처를 그대로 출력 (변환 없이 1:1 복사)
            half4 FragCopy(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
            }
            ENDHLSL
        }
    }
}
