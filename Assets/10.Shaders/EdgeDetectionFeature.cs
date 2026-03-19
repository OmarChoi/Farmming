// =============================================================================
// EdgeDetectionFeature - URP ScriptableRendererFeature
// =============================================================================
// URP 렌더 파이프라인에 외곽선 검출 포스트 프로세싱을 삽입하는 기능.
//
// 동작 흐름:
//   1. URP Renderer에 이 Feature를 추가하고, edgeMaterial에 EdgeDetection.mat을 할당
//   2. 매 프레임 AddRenderPasses()에서 EdgeDetectionPass를 렌더 큐에 삽입
//   3. RecordRenderGraph()에서 RenderGraph API를 사용하여:
//      - Pass 1: 원본 씬 색상(source)을 EdgeDetection 셰이더로 처리 → 임시 텍스처(temp)에 기록
//      - Pass 2: temp를 다시 source로 복사 (CopyBack)
//
// Settings 클래스를 통해 Inspector에서 파라미터를 조절할 수 있다.
// =============================================================================
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class EdgeDetectionFeature : ScriptableRendererFeature
{
    // ── Inspector에서 설정 가능한 파라미터 ──
    [System.Serializable]
    public class Settings
    {
        // 이 패스가 실행될 렌더 파이프라인 시점
        // AfterRenderingTransparents: 불투명+투명 오브젝트가 모두 그려진 후 실행
        // → 모든 오브젝트에 외곽선이 적용됨
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        // EdgeDetection.shader를 사용하는 머티리얼
        // Inspector에서 반드시 할당해야 동작함 (null이면 패스 스킵)
        public Material edgeMaterial;

        [Header("Edge Detection")]
        public Color edgeColor = new Color(0.1f, 0.08f, 0.08f, 1f); // 외곽선 색상 (진한 갈색)

        [Range(0.0001f, 0.1f)]
        public float depthThreshold = 0.01f;  // 깊이 차이 임계값 (작을수록 민감)

        [Range(0.01f, 2.0f)]
        public float normalThreshold = 0.2f;  // 노말 차이 임계값 (작을수록 민감)

        [Range(0.5f, 5.0f)]
        public float edgeThickness = 1.5f;    // 외곽선 두께 (픽셀 간격 배수)
    }

    public Settings settings = new Settings();
    EdgeDetectionPass _pass;

    // ── Feature 초기화 ──
    // URP가 시작될 때 호출. 렌더 패스를 생성한다.
    public override void Create()
    {
        _pass = new EdgeDetectionPass(settings);
        _pass.renderPassEvent = settings.renderPassEvent;
    }

    // ── 매 프레임 렌더 패스 삽입 ──
    // 머티리얼이 할당되어 있을 때만 패스를 렌더 큐에 추가한다.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.edgeMaterial == null) return; // 머티리얼 미할당 시 스킵
        renderer.EnqueuePass(_pass);
    }

    // ── 실제 렌더 패스 구현 ──
    class EdgeDetectionPass : ScriptableRenderPass
    {
        readonly Settings _settings;

        // 셰이더 프로퍼티 ID 캐싱 (매 프레임 문자열 해싱 방지)
        static readonly int EdgeColorID = Shader.PropertyToID("_EdgeColor");
        static readonly int DepthThresholdID = Shader.PropertyToID("_DepthThreshold");
        static readonly int NormalThresholdID = Shader.PropertyToID("_NormalThreshold");
        static readonly int EdgeThicknessID = Shader.PropertyToID("_EdgeThickness");
        static readonly int BlitTextureID = Shader.PropertyToID("_BlitTexture");

        // RenderGraph 패스에 전달할 데이터 컨테이너
        class PassData
        {
            public TextureHandle source;   // 입력 텍스처 핸들
            public Material material;      // EdgeDetection 머티리얼
        }

        public EdgeDetectionPass(Settings settings)
        {
            _settings = settings;
            // 이 패스가 깊이 텍스처와 노말 텍스처를 필요로 함을 URP에 알림
            // → URP가 DepthNormals 프리패스를 자동으로 활성화
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        // ── RenderGraph 기반 렌더링 기록 ──
        // URP 6+ RenderGraph API를 사용하여 렌더 패스를 기록한다.
        // 실제 GPU 명령은 SetRenderFunc의 람다에서 실행된다.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_settings.edgeMaterial == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            // 백버퍼에 직접 렌더링 중이면 포스트 프로세싱 불가
            if (resourceData.isActiveTargetBackBuffer) return;

            // Inspector 설정값을 셰이더 유니폼으로 전달
            _settings.edgeMaterial.SetColor(EdgeColorID, _settings.edgeColor);
            _settings.edgeMaterial.SetFloat(DepthThresholdID, _settings.depthThreshold);
            _settings.edgeMaterial.SetFloat(NormalThresholdID, _settings.normalThreshold);
            _settings.edgeMaterial.SetFloat(EdgeThicknessID, _settings.edgeThickness);

            // 원본 씬 렌더 결과 텍스처
            var source = resourceData.activeColorTexture;
            // 동일 사양의 임시 텍스처 생성 (source와 동시에 읽고 쓸 수 없으므로)
            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_EdgeDetectionTemp";
            var temp = renderGraph.CreateTexture(desc);

            // ── Pass 1: 외곽선 검출 (source → temp) ──
            // source 텍스처를 읽어서 EdgeDetection 셰이더(Pass 0)로 처리한 결과를
            // temp 텍스처에 기록한다.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("EdgeDetection", out var passData))
            {
                passData.source = source;
                passData.material = _settings.edgeMaterial;

                builder.UseTexture(source);                 // source를 읽기 전용으로 사용
                builder.SetRenderAttachment(temp, 0);       // temp를 렌더 타겟으로 설정
                builder.AllowPassCulling(false);             // RenderGraph 최적화에 의한 패스 컬링 방지

                // static 람다: 캡처 없이 실행하여 GC 할당 방지
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    data.material.SetTexture(BlitTextureID, data.source); // 입력 텍스처 바인딩
                    // Blitter로 풀스크린 쿼드를 그려 셰이더 실행 (pass index = 0: EdgeDetection)
                    Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // ── Pass 2: 복사 (temp → source) ──
            // temp에 기록된 외곽선 결과를 원래 렌더 타겟(source)으로 복사한다.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("EdgeDetection_CopyBack", out var passData2))
            {
                passData2.source = temp;
                passData2.material = _settings.edgeMaterial;

                builder.UseTexture(temp);                   // temp를 읽기 전용으로 사용
                builder.SetRenderAttachment(source, 0);     // source를 렌더 타겟으로 설정
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    data.material.SetTexture(BlitTextureID, data.source);
                    // pass index = 1: CopyBack (단순 텍스처 복사)
                    Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 1);
                });
            }
        }
    }

    // Feature가 파괴될 때 호출 (현재 정리할 리소스 없음)
    protected override void Dispose(bool disposing) { }
}
