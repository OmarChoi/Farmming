# Map System Guide

## 전체 구조

```
4개의 맵
├── 마을 (Village)     → 최초 1회 랜덤 생성, 이후 세이브/로드
├── 던전 1층 (Dungeon1) → 입장마다 랜덤 생성, 언덕 형태
├── 던전 2층 (Dungeon2) → 입장마다 랜덤 생성, 동굴 형태 (천장+벽)
└── 던전 3층 (Dungeon3) → 추후 구성
```

```
공통 규칙
├── 맵 크기: 75 x 75
├── 높이: 기본 4층, 최대 8층
├── 맨 밑 블록(y=0): 파괴 불가
├── 나무/돌: 지형 최상단에 랜덤 배치
├── 마을: 농사 가능 (FarmTile 있는 프리팹)
└── 던전: 농사 불가 (FarmTile 없는 프리팹), 저장 안 함
```

---

## 파일 구조

```
Assets/02.Scripts/Map/
├── EMapType.cs                  맵 종류 enum (Village, Dungeon1~3)
├── ETileType.cs                 타일 비주얼 enum (VillageDirt, Dungeon1Ground, ...)
├── MapManager.cs                맵 전환 총괄 (씬마다 배치)
│
├── Config/
│   ├── MapConfig.cs             공통 설정 ScriptableObject
│   │                            (크기, 높이, NoiseScale, 기본타일, 자원배치)
│   └── DungeonMapConfig.cs      던전 전용 설정 ScriptableObject
│                                (시간제한, 입장료, 복수타일비율, 동굴설정)
│
├── Generator/
│   ├── IMapGenerator.cs         생성 인터페이스
│   ├── MapGenerationResult.cs   생성 결과 (GridData + SpawnPoint)
│   ├── HeightMapGenerator.cs    Perlin Noise 언덕 생성 (마을 + 던전1)
│   ├── CaveMapGenerator.cs      Cellular Automata 동굴 생성 (던전2)
│   └── ResourcePlacer.cs        나무/돌 배치
│
└── Tile/
    └── TilePrefabDatabase.cs    ETileType → 프리팹 매핑 ScriptableObject
```

---

## 기존 파일 수정 내역

### TerrainCellData.cs
- TileType (ETileType) 추가 → 어떤 프리팹을 쓸지 결정
- IsIndestructible (bool) 추가 → true면 캘 수 없음 (맨 밑층, 벽)

### TerrainCell.cs
- _initialTileType 필드 추가
- TryConvertToFarm()에 FarmTile null 방어 → 던전 프리팹이면 농사 전환 차단

### TerrainGridManager.cs
- 기존 _cellPrefab → TilePrefabDatabase로 교체
- SpawnCell()에서 TileType 보고 프리팹 선택
- _defaultCellPrefab: TileDatabase에 없을 때 fallback

### TerrainCellSaveData.cs
- TileType, IsIndestructible 필드 추가

### SaveManager.cs
- MapManager 참조 추가
- IsVillage일 때만 지형 저장

### TerrainBrush.cs / TerrainEditorWindow.cs
- 에디터에서 TileType 선택 가능

---

## 생성 알고리즘

### HeightMapGenerator (마을 + 던전1)

```
1. Perlin Noise로 각 (x, z) 위치의 높이 계산
   → 기본 4층 + 노이즈 * 추가 높이(0~4) = 총 4~8층

2. 해당 높이만큼 블록 쌓기
   → y=0은 IsIndestructible = true

3. 최상단 셀에 나무/돌 확률 배치

결과 (XY 단면):
y=7          ■ ■
y=6        ■ ■ ■ ■
y=5      ■ ■ ■ ■ ■ ■
y=4    ■ ■ ■ ■ ■ ■ ■ ■
y=3  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■    ← 기본 4층
y=2  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■
y=1  ■ ■ ■ ■ ■ ■ ■ ■ ■ ■
y=0  ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣    ▣ = 파괴 불가
```

### CaveMapGenerator (던전2)

```
1. Cellular Automata로 벽/빈공간 결정
   → 랜덤 45% 채움 → 5회 스무딩 (주변 벽 4개 이상이면 벽)
   → 테두리는 강제 벽

2. Flood Fill로 연결성 검증
   → 분리된 영역은 터널로 강제 연결

3. 벽: 바닥~천장 전부 채움 (파괴 불가)
   빈공간: 바닥(Perlin 높이) + 천장(y=7) + 사이 비어있음

결과 (XY 단면):
y=7  ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣    ← 천장 (파괴 불가)
y=6  ▣ ▣ · · · · · ▣ ▣ ▣
y=5  ▣ · · · ▣ ▣ · · ▣ ▣    ▣ = 벽 (파괴 불가)
y=4  ▣ · · · ▣ ▣ · · · ▣    ■ = 일반 블록 (캘 수 있음)
y=3  ▣ ■ ■ ■ ▣ ▣ ■ ■ ■ ▣    · = 빈 공간
y=2  ▣ ■ ■ ■ ▣ ▣ ■ ■ ■ ▣
y=1  ▣ ■ ■ ■ ▣ ▣ ■ ■ ■ ▣
y=0  ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣ ▣    ← 바닥 (파괴 불가)
```

---

## 타일 프리팹 구성

```
Cell_Village (프리팹)               Cell_Dungeon (프리팹)
├── TerrainCell (스크립트)           ├── TerrainCell (같은 스크립트)
├── DirtBlock (메시)                ├── StoneBlock (다른 메시)
├── ObjectPoint                     ├── ObjectPoint
└── FarmTile (농사 기능)            └── (FarmTile 없음 → 농사 불가)
```

TilePrefabDatabase (ScriptableObject)에서 매핑:
```
ETileType        → 프리팹
─────────────────────────────
VillageDirt      → Cell_Village     (FarmTile O)
Dungeon1Ground   → Cell_D1Ground    (FarmTile X)
Dungeon1Sand     → Cell_D1Sand      (FarmTile X)
Dungeon2Stone    → Cell_D2Stone     (FarmTile X)
Dungeon2Ice      → Cell_D2Ice       (FarmTile X)
Dungeon2Lava     → Cell_D2Lava      (FarmTile X)
```

---

## 씬 구성 (B 방식: 씬마다 독립 배치)

```
VillageScene
├── TerrainGridManager   (TileDatabase, DefaultCellPrefab 연결)
├── MapManager           (VillageConfig 연결)
├── SaveManager          (MapManager 연결)
└── 플레이어, NPC 등

Dungeon1Scene
├── TerrainGridManager
├── MapManager           (DungeonConfig[0] 연결)
├── DungeonSceneInit     (floor=1, Start에서 자동 생성)
└── 플레이어

Dungeon2Scene
├── TerrainGridManager
├── MapManager           (DungeonConfig[1] 연결)
├── DungeonSceneInit     (floor=2, Start에서 자동 생성)
└── 플레이어
```

---

## 게임 흐름

```
[신규 게임]
  VillageScene → MapManager.GenerateVillage()
  → Perlin Noise로 75x75 언덕 + 나무/돌 생성
  → 자동 저장

[이어하기]
  VillageScene → SaveManager.LoadAsync()
  → 세이브 파일에서 마을 지형 복원

[던전 입장]
  마을에서 던전 입구 상호작용
  → SaveManager.SaveAsync() (마을 저장)
  → SceneManager.LoadScene("Dungeon1Scene")
  → DungeonSceneInit.Start() → MapManager.EnterDungeon(1)
  → 랜덤 지형 생성 (저장 안 함)

[던전 퇴장 / 마을 복귀]
  던전에서 출구 상호작용
  → SceneManager.LoadScene("VillageScene")
  → SaveManager.LoadAsync() (마을 복원)
  → 던전 데이터는 폐기됨

[던전 재입장]
  → 매번 새로운 seed → 매번 다른 지형
```

---

## 테스트 방법

```
1. F1 → 마을 랜덤 생성 (TestMapGenerator)
2. 9  → 저장 (TestSaveLoader)
3. 0  → 로드 (TestSaveLoader)
```

NoiseScale 조절:
- 0.03 → 넓고 완만한 언덕
- 0.05 → 보통
- 0.08 → 좁고 자주 변하는 지형