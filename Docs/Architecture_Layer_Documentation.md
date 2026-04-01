# 도메인 레이어 아키텍처 문서
## 농사 시스템 & Helper 시스템

> 작성일: 2026-04-01  
> 분석 범위: UI → Domain → Repository → Data 레이어 구조

---

## 목차

1. [전체 아키텍처 개요](#1-전체-아키텍처-개요)
2. [레이어 정의 및 책임](#2-레이어-정의-및-책임)
3. [Helper 시스템 레이어 구조](#3-helper-시스템-레이어-구조)
4. [농사 시스템 레이어 구조](#4-농사-시스템-레이어-구조)
5. [레이어 간 통신 방식](#5-레이어-간-통신-방식)
6. [Repository 패턴 구현](#6-repository-패턴-구현)
7. [도메인 규칙 캡슐화 위치](#7-도메인-규칙-캡슐화-위치)
8. [저장/로드 전체 흐름](#8-저장로드-전체-흐름)
9. [이벤트 채널 시스템](#9-이벤트-채널-시스템)
10. [두 시스템 비교](#10-두-시스템-비교)
11. [구조적 강점 및 개선 가능 영역](#11-구조적-강점-및-개선-가능-영역)

---

## 1. 전체 아키텍처 개요

이 프로젝트는 명시적인 4-레이어 구조를 채택하고 있으며, 레이어 간 통신은 **이벤트**, **ScriptableObject 채널**, **인터페이스**를 혼합하여 느슨한 결합(Loose Coupling)을 유지합니다.

```mermaid
flowchart TB
    subgraph UI["UI 레이어\n(표시 및 입력)"]
        direction LR
        UI_INV["UI_Inventory"]
        UI_HELPER_INV["UI_HelperInventory"]
        UI_HELPER_EXP["UI_HelperExperience"]
        UI_HARVEST["HarvestNotification\nManager"]
        UI_SLOT["UI_Slot"]
        UI_SEED_BUBBLE["HelperSeedBubbleUI"]
        UI_ACTION_INFO["UI_HelperActionInfo"]
    end

    subgraph ABILITY["Player Ability 레이어\n(Domain ↔ UI 브릿지)"]
        direction LR
        PA_INV["PlayerInventory\nAbility"]
        PA_HELPER_INV["PlayerHelperInventory\nAbility"]
        PA_HELPER_INT["PlayerHelperInteraction\nAbility"]
        PA_TERRAIN["PlayerTerrain\nAbility"]
    end

    subgraph DOMAIN["Domain 레이어\n(비즈니스 규칙)"]
        direction LR
        subgraph HELPER_DOMAIN["Helper 도메인"]
            HC["HelperController"]
            HL["HelperLevel"]
            HG["HelperGrade"]
            HE["HelperEnergy"]
            HEX["HelperExperience"]
        end
        subgraph FARM_DOMAIN["농사 도메인"]
            FT["FarmTile\n+ StateMachine"]
            CG["CropGrowth"]
            INV_DOM["InventoryDomain"]
            INV_SLOT["InventorySlot"]
        end
    end

    subgraph REPO["Repository 레이어\n(데이터 접근 추상화)"]
        direction LR
        SAVE_MGR["SaveManager"]
        ISAVE["ISaveRepository"]
        LSAVE["LocalJsonSave\nRepository"]
        SEED_DB["SeedDatabase\n(ScriptableObject)"]
        HELPER_DB["HelperDatabase\n(ScriptableObject)"]
        ITEM_DB["ItemDatabase\n(ScriptableObject)"]
        TGM["TerrainGrid\nManager"]
    end

    subgraph DATA["Data 레이어\n(직렬화 데이터)"]
        direction LR
        SAVE_DATA["SaveData\n(루트)"]
        PLAYER_SD["PlayerSaveData"]
        HELPER_SD["HelperSaveData"]
        FARM_SD["FarmSaveData"]
        CELL_SD["TerrainCellSaveData"]
        INV_SD["InventorySlotSaveData"]
        SO_DATA["ScriptableObject\n메타데이터\n(Seed/Helper/Item DataSO)"]
    end

    %% 레이어 간 연결
    UI -.->|이벤트 구독| ABILITY
    ABILITY -.->|이벤트 / 직접호출| DOMAIN
    DOMAIN -.->|인터페이스 / 조회| REPO
    REPO -.->|직렬화 / 역직렬화| DATA

    style UI fill:#3498db,color:#fff
    style ABILITY fill:#9b59b6,color:#fff
    style DOMAIN fill:#2ecc71,color:#fff
    style REPO fill:#e67e22,color:#fff
    style DATA fill:#e74c3c,color:#fff
```

---

## 2. 레이어 정의 및 책임

| 레이어 | 위치 | 책임 | 의존 방향 |
|--------|------|------|----------|
| **UI** | `InGame/*/UI/`, `OutGame/UI/` | 화면 표시, 사용자 입력 수신 | Ability 이벤트 구독 |
| **Player Ability** | `InGame/Player/Abilities/` | UI ↔ Domain 중개, ISaveableAbility 구현 | Domain 호출, UI에 이벤트 발행 |
| **Domain** | `InGame/Helper/Core/`, `InGame/Farming/Tile|Crop/` | 비즈니스 규칙, 상태 관리 | Repository 인터페이스 사용 |
| **Repository** | `OutGame/Save/`, `InGame/*/Data/Database` | 데이터 영속성, 조회 | Data 클래스 직렬화 |
| **Data** | `OutGame/Save/Data/`, `ScriptableObjects/` | 순수 데이터 구조 (직렬화 가능) | 없음 (최하단) |

### 의존성 방향 원칙

```
UI → Ability → Domain → Repository → Data
(상위 레이어는 하위 레이어를 알고, 하위 레이어는 상위 레이어를 모름)
```

---

## 3. Helper 시스템 레이어 구조

### 3.1 전체 레이어 맵

```mermaid
flowchart TB
    subgraph UI_LAYER["UI 레이어"]
        direction LR
        U1["UI_HelperInventory\n헬퍼 선택 카로셀"]
        U2["UI_HelperExperience\n경험치 바"]
        U3["UI_HelperActionInfo\n좌/우클릭 설명"]
        U4["HelperSeedBubbleUI\n씨앗 선택 표시"]
    end

    subgraph ABILITY_LAYER["Player Ability 레이어"]
        A1["PlayerHelperInventory\nAbility\n\n- 헬퍼 목록 관리\n- 소환/해제\n- 상태 저장/복원\n- ISaveableAbility"]
        A2["PlayerHelperInteraction\nAbility\n\n- 장착/해제 (F)\n- 좌클릭/우클릭 위임\n- currentHelper 관리"]
    end

    subgraph DOMAIN_LAYER["Domain 레이어"]
        HC["HelperController\n\n상태: Inventory→Summoned→Equipped\n소환/장착/해제 상태 전이\nAbility 컴포넌트 허브"]

        subgraph VALUE_OBJECTS["값 객체 (Value Objects)"]
            HL["HelperLevel\n\n현재 레벨 (1~MaxLevel)\nGetEnergyCost() 규칙"]
            HG["HelperGrade\n\nNormal/Epic/Legendary\nGetRange() 규칙\nUpgrade() 전이"]
            HE["HelperEnergy\n\nCurrent/Max\nTryConsume()\nRecover() 규칙"]
            HEX["HelperExperience\n\nCurrentExp/MaxExp\nIsReadyToUpgrade 조건\nAdd()/Reset() 규칙"]
        end

        subgraph ACTIONS["Action 컴포넌트들"]
            ACT_SOW["SowActionAbility"]
            ACT_HARV["HarvestActionAbility"]
            ACT_WATER["WaterActionAbility"]
            ACT_LIGHT["LightActionAbility"]
            ACT_GROUND["GroundActionAbility"]
        end
    end

    subgraph REPO_LAYER["Repository 레이어"]
        HELPER_DB["HelperDatabase\n(ScriptableObject)\n\nGetById(string)\nDictionary 캐시"]
        SAVE_MGR_H["SaveManager\n(헬퍼 저장 담당)"]
    end

    subgraph DATA_LAYER["Data 레이어"]
        HELPER_SO["HelperDataSO\n(ScriptableObject)\n\nId, Name, Icon, Prefab\nMaxEnergy, MaxLevel\nBaseEnergyCost\nEnergyReducePerLevel\nNormal/Epic/LegendaryRange\nNormal/EpicMaxExp"]
        HELPER_SD["HelperSaveData\n[Serializable]\n\nHelperId: string\nLevel: int\nGrade: int\nExperience: int"]
    end

    %% 연결
    U1 -- "OnSelectionChanged\nOnSummonChanged 구독" --> A1
    U2 -- "OnSummonChanged 구독\n→ HelperExperience.OnExpChanged 구독" --> A1
    U3 -- "HelperDataSO 직접 표시" --> A1
    U4 -- "SeedSelectAbility.OnSeedSelected 구독" --> A2

    A1 -- "Instantiate + Summon\nSaveActiveHelperState\nRestoreHelperState" --> HC
    A2 -- "Equip/Unequip\nInteractPrimary/Secondary" --> HC

    HC --> VALUE_OBJECTS
    HC --> ACTIONS

    A1 -- "GetById(helperId)" --> HELPER_DB
    SAVE_MGR_H -- "ExportTo/ImportFrom 위임" --> A1

    HELPER_DB -- "참조" --> HELPER_SO
    A1 -- "직렬화" --> HELPER_SD

    style UI_LAYER fill:#3498db,color:#fff
    style ABILITY_LAYER fill:#9b59b6,color:#fff
    style DOMAIN_LAYER fill:#2ecc71,color:#fff
    style REPO_LAYER fill:#e67e22,color:#fff
    style DATA_LAYER fill:#e74c3c,color:#fff
```

### 3.2 Helper 도메인 규칙 상세

```mermaid
flowchart LR
    subgraph HelperController["HelperController (오케스트레이터)"]
        STATE["EHelperState\nInventory / Summoned / Equipped"]
    end

    subgraph HelperLevel["HelperLevel (레벨 값 객체)"]
        L_RULE["GetEnergyCost()\n= Max(1, BaseEnergyCost\n- EnergyReducePerLevel × (Level-1))"]
        L_MAX["CurrentLevel <= MaxLevel\n레벨 상한 보장"]
    end

    subgraph HelperGrade["HelperGrade (등급 값 객체)"]
        G_RULE["GetRange()\nNormal=1 / Epic=2 / Legendary=3"]
        G_UP["Upgrade()\nNormal→Epic→Legendary\n(단방향)"]
    end

    subgraph HelperEnergy["HelperEnergy (에너지 값 객체)"]
        E_CONSUME["TryConsume(amount)\nCurrent >= amount → 차감\n아니면 false 반환"]
        E_RECOVER["Recover(deltaTime)\nCurrent += Recovery × dt\n(Max 초과 방지)"]
        E_EVENT["OnExhausted: 에너지 0\nOnRecovered: 완전 회복"]
    end

    subgraph HelperExperience["HelperExperience (경험치 값 객체)"]
        EX_MAX["MaxExp\nNormal=500 / Epic=1000\nLegendary=∞"]
        EX_READY["IsReadyToUpgrade\n= Grade≠Legendary\n&& CurrentExp>=MaxExp"]
        EX_ADD["Add(amount)\nIsReadyToUpgrade이면 무시\nMaxExp 도달 시 OnReadyToUpgrade"]
        EX_RESET["Reset()\n업그레이드 후 CurrentExp=0"]
    end

    HelperController --> HelperLevel
    HelperController --> HelperGrade
    HelperController --> HelperEnergy
    HelperController --> HelperExperience
    HelperGrade --> HelperExperience
```

### 3.3 Helper UI → Domain 이벤트 흐름

```mermaid
sequenceDiagram
    actor P as 플레이어
    participant A1 as PlayerHelper<br>InventoryAbility
    participant HC as HelperController
    participant HEX as HelperExperience
    participant U1 as UI_Helper<br>Inventory
    participant U2 as UI_Helper<br>Experience

    Note over P,U2: ── 헬퍼 소환 ──
    P->>A1: E 키 (ToggleSummon)
    A1->>A1: SaveActiveHelperState
    A1->>HC: Instantiate + RestoreState
    A1->>A1: OnSummonChanged.Invoke(index)
    A1-->>U1: [이벤트] OnSummonChanged
    U1->>U1: SummonedIndicator 갱신
    A1-->>U2: [이벤트] OnSummonChanged
    U2->>HC: Experience 인스턴스 획득
    U2->>HEX: OnExpChanged 구독

    Note over P,U2: ── 행동으로 경험치 획득 ──
    P->>HC: 좌/우클릭 행동
    HC->>HEX: Experience.Add(10)
    HEX->>HEX: CurrentExp += 10
    HEX-->>U2: [이벤트] OnExpChanged(current, max)
    U2->>U2: fillAmount Tween 업데이트

    Note over P,U2: ── 업그레이드 가능 ──
    HEX->>HEX: CurrentExp >= MaxExp
    HEX-->>U2: [이벤트] OnReadyToUpgrade
    U2->>U2: 업그레이드 인디케이터 활성화

    Note over P,U2: ── 헬퍼 전환 ──
    P->>A1: 1/3 키 (Rotate)
    A1->>A1: OnSelectionChanged.Invoke(direction)
    A1-->>U1: [이벤트] OnSelectionChanged
    U1->>U1: 카로셀 슬라이드 애니메이션
```

---

## 4. 농사 시스템 레이어 구조

### 4.1 전체 레이어 맵

```mermaid
flowchart TB
    subgraph UI_F["UI 레이어"]
        direction LR
        UF1["HarvestNotification\nManager\n\n수확 알림 표시"]
        UF2["HarvestNotification\n\n아이콘/이름/수량\n페이드아웃 애니메이션"]
    end

    subgraph ABILITY_F["Player Ability 레이어"]
        direction LR
        AF1["PlayerInventory\nAbility\n\n인벤토리 열기/닫기\n아이템 추가/제거\nISaveableAbility"]
        AF2["PlayerTerrain\nAbility\n\n지형 상호작용 위임"]
    end

    subgraph DOMAIN_F["Domain 레이어"]
        subgraph FARM_TILE["FarmTile (상태머신)"]
            FT_SM["FarmTileStateMachine\nFarmDry ↔ FarmWet"]
            FT_RULE["Interact() 분기 규칙\n- !HasSeed → PlantSeed\n- HasSeed, !Started → Water\n- IsHarvestable → Harvest"]
        end
        subgraph CROP["CropGrowth (성장 엔진)"]
            CG_STATE["_currentStageIndex\n_elapsedDays\n_isGrowing\n_hasStarted"]
            CG_RULE["TryGrow() 규칙\nelapsedDays >= RequireDays\n→ 다음 단계 또는 완료"]
            CG_HARVEST["IsHarvestable\n= !isGrowing && hasStarted"]
        end
        subgraph INVENTORY_DOM["InventoryDomain"]
            INV_RULE["슬롯 추가 우선순위\n1) 기존 스택 합산\n2) 빈 슬롯\n3) 슬롯 확장 (4칸씩)\n최소 16칸 유지"]
        end
        TC["TerrainCell\n\n3D 메시 + FarmTile 관리\nRefresh() 시각화 업데이트"]
    end

    subgraph REPO_F["Repository 레이어"]
        TGM["TerrainGridManager\n\n모든 셀 Dictionary 관리\nExportSaveData()\nImportSaveData()"]
        SEED_DB_F["SeedDatabase\n(ScriptableObject)\n\nGetById(int seedId)"]
        ITEM_DB_F["ItemDatabase\n(ScriptableObject)\n\nGetById(int itemId)"]
        SAVE_MGR_F["SaveManager\n\nISaveRepository 통해\nJSON 파일 저장/로드"]
    end

    subgraph DATA_F["Data 레이어"]
        SEED_SO["SeedItemDataSO\n\nId, DisplayName, Icon\nSeedGrowthStage[]\nHarvestAmountMin/Max\nHarvestItem\nSeedGrade"]
        STAGE_DATA["SeedGrowthStage\nData\n\nSeedStageName\nRequireDays\nSeedStageItem(Prefab)\nGrowthTiming"]
        FARM_SD_F["FarmSaveData\n[Serializable]\n\nFarmState\nSeedId\nCropStageIndex\nCropElapsedDays\nCropIsGrowing\nCropHasStarted"]
        CELL_SD_F["TerrainCellSaveData\n[Serializable]\n\nX, Y, Z\nCellType\nObjectType\nFarmSaveData"]
    end

    subgraph EVENT_CH["이벤트 채널 (ScriptableObject)"]
        HARVEST_SO["HarvestItemSO\n\nOnHarvested event\nRaise(icon, name, amount)"]
        TIME_EV["TimeEvents (static)\n\nOnDayStarted\nOnDayEnded"]
    end

    %% UI 연결
    UF1 -- "OnHarvested 구독" --> HARVEST_SO
    UF1 --> UF2

    %% Ability 연결
    AF1 -- "AddItem() 위임" --> INVENTORY_DOM
    AF1 -- "OnSlotChanged 이벤트" --> UF1

    %% Domain 연결
    CROP -- "수확 시 AddItem 호출" --> AF1
    CROP -- "Raise(icon, name, amount)" --> HARVEST_SO
    TC --> FARM_TILE
    TC --> CROP
    FT_SM --> FARM_TILE
    TIME_EV -- "OnDayStarted/Ended" --> FARM_TILE
    FARM_TILE --> CROP

    %% Repository 연결
    TGM -- "모든 셀 순회" --> TC
    TGM -- "씨앗 로드 시 조회" --> SEED_DB_F
    AF1 -- "아이템 로드 시 조회" --> ITEM_DB_F
    SAVE_MGR_F -- "ExportTo/ImportFrom" --> TGM

    %% Data 연결
    SEED_DB_F --> SEED_SO
    SEED_SO --> STAGE_DATA
    TGM -- "직렬화" --> CELL_SD_F
    FARM_TILE -- "직렬화" --> FARM_SD_F

    style UI_F fill:#3498db,color:#fff
    style ABILITY_F fill:#9b59b6,color:#fff
    style DOMAIN_F fill:#2ecc71,color:#fff
    style REPO_F fill:#e67e22,color:#fff
    style DATA_F fill:#e74c3c,color:#fff
    style EVENT_CH fill:#1abc9c,color:#fff
```

### 4.2 FarmTile 도메인 규칙 상세

```mermaid
flowchart TD
    subgraph FarmTile_Rules["FarmTile 도메인 규칙"]
        INTERACT["Interact(seed?) 진입점"]

        INTERACT --> R1{HasSeed?}

        R1 -->|"No\n(농지만 있음)"| R2{FarmDry?}
        R2 -->|Yes| PLANT["PlantSeed(seed)\n씨앗 심기 규칙:\n- PlantedSeed = seed\n- CropGrowth.ShowFirstStage()"]
        R2 -->|No| IGNORE1["무시\n(이미 습윤인데 씨앗 없음)"]

        R1 -->|Yes| R3{IsHarvestable?\n= !isGrowing && hasStarted}
        R3 -->|Yes| HARVEST["RemoveSeed()\n수확 완료 규칙:\n- PlantedSeed = null\n- CropGrowth.Harvest()"]

        R3 -->|No| R4{FarmDry &&\nhasStarted == false?}
        R4 -->|Yes| WATER["Water() 규칙:\n- FarmTransition(FarmWet)\n- CropGrowth.StartGrowth()"]
        R4 -->|No| IGNORE2["무시\n(이미 성장 진행 중)"]
    end

    subgraph CropGrowth_Rules["CropGrowth 성장 규칙"]
        TRYGROW["TryGrow() 진입점\n(아침 또는 밤에만 호출)"]
        TRYGROW --> INC["_elapsedDays++"]
        INC --> CHECK{elapsedDays >=\nRequireDays?}
        CHECK -->|No| WAIT["대기\n다음 Time 이벤트까지"]
        CHECK -->|Yes| RESET_DAYS["_elapsedDays = 0"]
        RESET_DAYS --> LAST{마지막 단계?}
        LAST -->|Yes| COMPLETE["_isGrowing = false\nIsHarvestable = true"]
        LAST -->|No| ADVANCE["_currentStageIndex++\nApplyStagePrefab()\n새 비주얼 표시"]
    end

    style FarmTile_Rules fill:#2ecc71,color:#fff
    style CropGrowth_Rules fill:#2ecc71,color:#fff
```

### 4.3 농사 UI → Domain 이벤트 흐름

```mermaid
sequenceDiagram
    participant TIME as TimeEvents
    participant FT as FarmTile
    participant CG as CropGrowth
    participant PA_INV as PlayerInventory<br>Ability
    participant HIS as HarvestItemSO<br>(이벤트 채널)
    participant MGR as HarvestNotification<br>Manager
    participant UI_N as HarvestNotification<br>UI

    Note over TIME,UI_N: ── 아침 성장 처리 ──
    TIME->>FT: OnDayStarted (static event)
    FT->>CG: CheckMorningGrowth()
    CG->>CG: TryGrow() → _elapsedDays++
    alt RequireDays 도달
        CG->>CG: ApplyStagePrefab() 비주얼 갱신
    end
    FT->>FT: FarmTransition(FarmDry)

    Note over TIME,UI_N: ── 수확 시 알림 ──
    CG->>PA_INV: AddItem(HarvestItem, amount)
    PA_INV->>PA_INV: InventoryDomain.AddItem()
    PA_INV-->>PA_INV: [이벤트] OnSlotChanged(index)

    CG->>HIS: Raise(icon, name, amount)
    Note right of HIS: ScriptableObject 이벤트 채널
    HIS-->>MGR: [이벤트] OnHarvested(icon, name, amount)
    MGR->>UI_N: Instantiate + Setup(icon, name, amount)
    UI_N->>UI_N: 위로 올라가며 페이드아웃
```

---

## 5. 레이어 간 통신 방식

이 프로젝트는 3가지 통신 방식을 레이어별로 다르게 사용합니다.

```mermaid
flowchart LR
    subgraph 방식1["방식 1: C# 이벤트 (Ability → UI)"]
        direction TB
        ABILITY_EV["PlayerHelperInventoryAbility\n\nevent OnSummonChanged\nevent OnSelectionChanged"]
        UI_EV["UI_HelperInventory\nUI_HelperExperience\n\n.OnSummonChanged += 구독"]
        ABILITY_EV -->|"이벤트 발행"| UI_EV
    end

    subgraph 방식2["방식 2: ScriptableObject 채널 (Domain → UI)"]
        direction TB
        DOMAIN_SO["CropGrowth\n\n_harvestItemSO.Raise()"]
        SO_CH["HarvestItemSO\n\nevent OnHarvested"]
        UI_SO["HarvestNotificationManager\n\n.OnHarvested += 구독"]
        DOMAIN_SO -->|"Raise()"| SO_CH
        SO_CH -->|"이벤트 발행"| UI_SO
    end

    subgraph 방식3["방식 3: 정적 이벤트 (System → Domain)"]
        direction TB
        TIME_SYS["TimeSystem\n\nTimeEvents.InvokeDayStarted()"]
        STATIC_EV["TimeEvents (static)\n\nevent OnDayStarted (static)"]
        FARM_DOM["FarmTile\n\nTimeEvents.OnDayStarted += OnMorning"]
        TIME_SYS -->|"Invoke"| STATIC_EV
        STATIC_EV -->|"이벤트 발행"| FARM_DOM
    end
```

### 통신 방식 비교표

| 방식 | 사용 위치 | 결합도 | 장점 | 단점 |
|------|----------|--------|------|------|
| **C# 이벤트** | Ability → UI | 약함 | 타입 안전, 컴파일 타임 검증 | 구독 해제 주의 필요 |
| **ScriptableObject 채널** | Domain → UI | 보통 | 에디터에서 연결 확인 가능 | ScriptableObject 의존 |
| **정적 이벤트** | TimeSystem → 모든 FarmTile | 매우 약함 | 전역 접근, 구조 단순 | 구독 해제 필수, 테스트 어려움 |
| **직접 참조** | Domain 내부 | 강함 | 명확한 의존성 | 교체 어려움 |

---

## 6. Repository 패턴 구현

### 6.1 명시적 Repository (저장/로드)

```mermaid
classDiagram
    direction LR

    class ISaveRepository {
        <<interface>>
        +SaveAsync(SaveData, int slot) UniTask
        +LoadAsync(int slot) UniTask~SaveData~
        +HasSaveAsync(int slot) UniTask~bool~
        +DeleteAsync(int slot) UniTask
    }

    class LocalJsonSaveRepository {
        -GetFilePath(int slot) string
        +SaveAsync(SaveData, int) UniTask
        +LoadAsync(int) UniTask~SaveData~
        +HasSaveAsync(int) UniTask~bool~
        +DeleteAsync(int) UniTask
    }

    class SaveManager {
        -ISaveRepository _repository
        -TerrainGridManager _terrainManager
        -Dictionary _players
        +SaveAsync(int slot)
        +LoadAsync(int slot)
        +RegisterPlayer(PlayerController)
    }

    class ISaveableAbility {
        <<interface>>
        +ExportTo(PlayerSaveData)
        +ImportFrom(PlayerSaveData)
    }

    class PlayerHelperInventoryAbility {
        +ExportTo(PlayerSaveData)
        +ImportFrom(PlayerSaveData)
    }

    class PlayerInventoryAbility {
        +ExportTo(PlayerSaveData)
        +ImportFrom(PlayerSaveData)
    }

    ISaveRepository <|.. LocalJsonSaveRepository
    SaveManager --> ISaveRepository
    ISaveableAbility <|.. PlayerHelperInventoryAbility
    ISaveableAbility <|.. PlayerInventoryAbility
    SaveManager --> ISaveableAbility
```

**LocalJsonSaveRepository 동작**:
```
SaveAsync(data, slot):
  path = Application.persistentDataPath/save_{slot}.json
  json = JsonUtility.ToJson(data)
  await File.WriteAllTextAsync(path, json)

LoadAsync(slot):
  path = Application.persistentDataPath/save_{slot}.json
  json = await File.ReadAllTextAsync(path)
  return JsonUtility.FromJson<SaveData>(json)
```

### 6.2 암묵적 Repository (Database - 조회 전용)

```mermaid
classDiagram
    direction TB

    class SeedDatabase {
        -Dictionary~int, SeedItemDataSO~ _dict
        +GetById(int) SeedItemDataSO
        -EnsureDictionary()
    }

    class HelperDatabase {
        -Dictionary~string, HelperDataSO~ _dict
        +GetById(string) HelperDataSO
    }

    class ItemDatabase {
        -Dictionary~int, ItemDataSO~ _dict
        +GetById(int) ItemDataSO
    }

    class SeedItemDataSO {
        +string Id
        +List~SeedGrowthStageData~ SeedGrowthStage
        +int HarvestAmountMin
        +int HarvestAmountMax
        +ItemDataSO HarvestItem
    }

    class HelperDataSO {
        +string HelperId
        +float MaxEnergy
        +int MaxLevel
        +float BaseEnergyCost
        +float EnergyReducePerLevel
        +int NormalRange
        +int EpicRange
        +int LegendaryRange
        +int NormalMaxExp
        +int EpicMaxExp
    }

    class ItemDataSO {
        +int Id
        +string DisplayName
        +Sprite Icon
        +EItemType Type
        +int MaxStack
    }

    SeedDatabase "1" --> "*" SeedItemDataSO
    HelperDatabase "1" --> "*" HelperDataSO
    ItemDatabase "1" --> "*" ItemDataSO
```

---

## 7. 도메인 규칙 캡슐화 위치

### 7.1 Helper 시스템 도메인 규칙

```mermaid
flowchart TD
    subgraph HelperLevel_Rules["HelperLevel — 레벨 규칙"]
        LR1["GetEnergyCost()\n= Max(1f,\n  BaseEnergyCost\n  - EnergyReducePerLevel × (Level-1))\n\n레벨업 시 행동 비용 감소"]
        LR2["LevelUp()\n= Current < MaxLevel일 때만 허용"]
    end

    subgraph HelperGrade_Rules["HelperGrade — 등급 규칙"]
        GR1["GetRange()\nNormal → 1칸\nEpic   → 2칸\nLegendary → 3칸"]
        GR2["Upgrade()\nNormal→Epic→Legendary\n(역방향 불가)"]
    end

    subgraph HelperEnergy_Rules["HelperEnergy — 에너지 규칙"]
        ER1["TryConsume(amount)\nCurrent >= amount이면 차감\n아니면 false 반환 (행동 취소)"]
        ER2["Recover(dt)\nCurrent = Min(Max,\n  Current + Recovery × dt)"]
        ER3["IsExhausted = Current <= 0\n→ HelperInteractionAbility.CanInteract() false"]
    end

    subgraph HelperExperience_Rules["HelperExperience — 경험치 규칙"]
        EXR1["MaxExp 계산\nNormal = 500\nEpic   = 1000\nLegendary = int.MaxValue (∞)"]
        EXR2["IsReadyToUpgrade\n= Grade ≠ Legendary\n  && CurrentExp >= MaxExp"]
        EXR3["Add(amount)\nIsReadyToUpgrade이면 → 무시\nMaxExp 도달 → OnReadyToUpgrade 발행"]
        EXR4["Reset()\n업그레이드 완료 후 CurrentExp = 0\nMaxExp 재계산"]
    end

    style HelperLevel_Rules fill:#2ecc71,color:#fff
    style HelperGrade_Rules fill:#2ecc71,color:#fff
    style HelperEnergy_Rules fill:#2ecc71,color:#fff
    style HelperExperience_Rules fill:#2ecc71,color:#fff
```

### 7.2 농사 시스템 도메인 규칙

```mermaid
flowchart TD
    subgraph FarmTile_Rules["FarmTile — 농지 상태 규칙"]
        FR1["IsReadyToSow\n= FarmDry && !HasSeed"]
        FR2["Water()\n조건: FarmDry\n효과: FarmWet + StartGrowth()"]
        FR3["Interact() 분기\n!HasSeed → PlantSeed\nHasSeed && !Started → Water\nIsHarvestable → Harvest"]
        FR4["OnMorning()\n성장 체크 후 FarmWet → FarmDry 전환\n(매일 아침 건조화)"]
    end

    subgraph CropGrowth_Rules["CropGrowth — 성장 규칙"]
        CGR1["TryGrow() 호출 조건\n= FarmWet 상태에서만\n(아침 또는 밤, EGrowthTiming 기반)"]
        CGR2["단계 진행 조건\n_elapsedDays >= RequireDays\n→ 다음 단계로"]
        CGR3["수확 가능 조건\nIsHarvestable\n= !_isGrowing && _hasStarted\n= 마지막 단계 완료"]
        CGR4["물 없으면 성장 없음\nFarmDry 상태에서 TryGrow 호출 안됨"]
    end

    subgraph Inventory_Rules["InventoryDomain — 인벤토리 규칙"]
        IR1["슬롯 추가 우선순위\n1순위: 같은 아이템 기존 스택\n2순위: 빈 슬롯\n3순위: 슬롯 4칸 확장 후 추가"]
        IR2["슬롯 최소 유지\n마지막 4칸이 전부 비면 제거\n최소 16칸 보장"]
        IR3["스택 상한 (InventorySlot)\nItem.MaxStack 초과 방지"]
    end

    style FarmTile_Rules fill:#2ecc71,color:#fff
    style CropGrowth_Rules fill:#2ecc71,color:#fff
    style Inventory_Rules fill:#2ecc71,color:#fff
```

### 도메인 규칙 위치 요약표

| 규칙 | 클래스 | 레이어 |
|------|--------|--------|
| 에너지 비용 = f(레벨) | `HelperLevel.GetEnergyCost()` | Domain |
| 행동 범위 = f(등급) | `HelperGrade.GetRange()` | Domain |
| 업그레이드 가능 여부 | `HelperExperience.IsReadyToUpgrade` | Domain |
| 최대 경험치 = f(등급) | `HelperExperience.MaxExp` | Domain |
| 에너지 소비/회복 | `HelperEnergy.TryConsume(), Recover()` | Domain |
| 행동 가능 여부 | `HelperInteractionAbility.CanInteract()` | Domain |
| 농지 전환 조건 | `TerrainCell.TryConvertToFarm()` | Domain |
| 씨앗 심기 조건 | `FarmTile.IsReadyToSow` | Domain |
| 성장 진행 조건 | `CropGrowth.TryGrow()` | Domain |
| 수확 가능 여부 | `CropGrowth.IsHarvestable` | Domain |
| 슬롯 확장/축소 규칙 | `InventoryDomain` | Domain |
| 스택 상한 | `InventorySlot.TryAdd()` | Domain |

---

## 8. 저장/로드 전체 흐름

### 8.1 저장 흐름 (Save)

```mermaid
sequenceDiagram
    participant SM as SaveManager
    participant REPO as LocalJson<br>SaveRepository
    participant TGM as TerrainGrid<br>Manager
    participant TC as TerrainCell[]
    participant FT as FarmTile
    participant CG as CropGrowth
    participant PA as PlayerAbility<br>(ISaveableAbility)
    participant PA_H as PlayerHelper<br>InventoryAbility
    participant FILE as JSON 파일

    SM->>SM: SaveAsync(slot)

    SM->>TGM: ExportSaveData()
    TGM->>TC: foreach cell → ExportTo(cellSaveData)
    TC->>FT: ExportTo(cellSaveData)
    FT->>FT: FarmState 저장 (Dry/Wet)
    FT->>FT: SeedId 저장
    FT->>CG: ExportTo(farmSaveData)
    CG->>CG: StageIndex, ElapsedDays\nIsGrowing, HasStarted 저장
    TGM-->>SM: TerrainSaveData 반환

    SM->>PA: ExportTo(playerSaveData)
    PA->>PA: 인벤토리 슬롯 직렬화
    SM->>PA_H: ExportTo(playerSaveData)
    PA_H->>PA_H: SaveActiveHelperState()
    PA_H->>PA_H: foreach helper →\nnew HelperSaveData(Level, Grade, Exp)
    PA_H-->>SM: helpers 리스트 반환

    SM->>REPO: SaveAsync(saveData, slot)
    REPO->>REPO: JsonUtility.ToJson(data)
    REPO->>FILE: File.WriteAllTextAsync(path, json)
```

### 8.2 로드 흐름 (Load)

```mermaid
sequenceDiagram
    participant FILE as JSON 파일
    participant REPO as LocalJson<br>SaveRepository
    participant SM as SaveManager
    participant TGM as TerrainGrid<br>Manager
    participant TC as TerrainCell
    participant FT as FarmTile
    participant CG as CropGrowth
    participant SEED_DB as SeedDatabase
    participant PA_H as PlayerHelper<br>InventoryAbility
    participant HELPER_DB as HelperDatabase
    participant HC as HelperController

    SM->>REPO: LoadAsync(slot)
    REPO->>FILE: File.ReadAllTextAsync(path)
    FILE-->>REPO: json 문자열
    REPO->>REPO: JsonUtility.FromJson~SaveData~(json)
    REPO-->>SM: SaveData 반환

    SM->>TGM: ImportSaveData(terrainData)
    TGM->>TC: foreach cellData → ImportFrom(cellSaveData)
    TC->>TC: CellType, ObjectType 복원
    TC->>TC: Refresh() (메시 갱신)

    alt ObjectType == FarmLand
        TC->>FT: ImportFrom(cellSaveData, seedDB)
        FT->>FT: FarmState 복원 (Dry/Wet)

        alt SeedId > 0
            FT->>SEED_DB: GetById(seedId)
            SEED_DB-->>FT: SeedItemDataSO
            FT->>FT: PlantSeed(seed)
            FT->>CG: ImportFrom(farmSaveData, seed)
            CG->>CG: StageIndex, ElapsedDays 복원
            CG->>CG: ApplyStagePrefab() 비주얼 복원
        end
    end

    SM->>PA_H: ImportFrom(playerSaveData)
    loop 저장된 헬퍼마다
        PA_H->>HELPER_DB: GetById(helperId)
        HELPER_DB-->>PA_H: HelperDataSO
        PA_H->>PA_H: _savedStates 등록
    end

    Note over PA_H,HC: 다음 소환 시 상태 복원
    PA_H->>HC: RestoreHelperState(helper)
    HC->>HC: Level, Grade, Experience 복원
```

### 8.3 SaveData 계층 구조

```mermaid
flowchart TD
    ROOT["SaveData\n(루트 저장 데이터)"]

    ROOT --> TERRAIN["TerrainSaveData\n\n지형 전체"]
    ROOT --> PLAYERS["List~PlayerSaveData~\n\n플레이어 목록"]

    TERRAIN --> CELLS["List~TerrainCellSaveData~\n\n모든 셀"]

    CELLS --> CELL_ITEM["TerrainCellSaveData\n\nX, Y, Z\nCellType\nTileType, DirtLevel\nObjectType\nIsIndestructible\nIsTop"]

    CELL_ITEM --> FARM_DATA["FarmSaveData\n(농지인 셀만)\n\nFarmState: Dry/Wet\nSeedId: int\nCropStageIndex: int\nCropElapsedDays: int\nCropIsGrowing: bool\nCropHasStarted: bool"]

    PLAYERS --> PLAYER_ITEM["PlayerSaveData\n\nPosition, Rotation\nCustomize 정보"]

    PLAYER_ITEM --> INV_DATA["List~InventorySlot\nSaveData~\n\nSlotIndex, ItemId, Count"]

    PLAYER_ITEM --> HELPER_DATA["List~HelperSaveData~\n\nHelperId: string\nLevel: int\nGrade: int\nExperience: int"]

    style ROOT fill:#e74c3c,color:#fff
    style TERRAIN fill:#e74c3c,color:#fff
    style PLAYERS fill:#e74c3c,color:#fff
    style CELLS fill:#e74c3c,color:#fff
    style CELL_ITEM fill:#e74c3c,color:#fff
    style FARM_DATA fill:#e74c3c,color:#fff
    style PLAYER_ITEM fill:#e74c3c,color:#fff
    style INV_DATA fill:#e74c3c,color:#fff
    style HELPER_DATA fill:#e74c3c,color:#fff
```

---

## 9. 이벤트 채널 시스템

```mermaid
flowchart LR
    subgraph PUBLISHERS["발행자 (Publishers)"]
        P1["TimeSystem\nInvokeDayStarted/Ended"]
        P2["CropGrowth\nRaise(icon, name, amount)"]
        P3["PlayerHelperInventoryAbility\nOnSummonChanged\nOnSelectionChanged"]
        P4["HelperEnergy\nOnExhausted\nOnRecovered"]
        P5["HelperExperience\nOnExpChanged\nOnReadyToUpgrade"]
        P6["PlayerInventoryAbility\nOnSlotChanged\nOnToggle"]
    end

    subgraph CHANNELS["채널 (Channels)"]
        C1["TimeEvents\n(static class)"]
        C2["HarvestItemSO\n(ScriptableObject)"]
        C3["C# event (Ability 내부)"]
        C4["C# event (Domain 내부)"]
    end

    subgraph SUBSCRIBERS["구독자 (Subscribers)"]
        S1["FarmTile[]\nOnMorning / OnNight"]
        S2["HarvestNotification\nManager\nShow()"]
        S3["UI_HelperInventory\nSlide / Refresh"]
        S4["UI_HelperExperience\nUpdateFill"]
        S5["HelperInteractionAbility\nCanInteract 갱신"]
        S6["UI_Inventory\nRefreshSlot"]
    end

    P1 --> C1 --> S1
    P2 --> C2 --> S2
    P3 --> C3 --> S3
    P3 --> C3 --> S4
    P4 --> C4 --> S5
    P5 --> C4 --> S4
    P6 --> C3 --> S6

    style PUBLISHERS fill:#3498db,color:#fff
    style CHANNELS fill:#1abc9c,color:#fff
    style SUBSCRIBERS fill:#9b59b6,color:#fff
```

---

## 10. 두 시스템 비교

```mermaid
flowchart TB
    subgraph HELPER_ARCH["Helper 시스템 아키텍처"]
        direction TB
        H_UI["UI\nUI_HelperInventory\nUI_HelperExperience"]
        H_AB["PlayerHelper\nInventoryAbility\n(ISaveableAbility)"]
        H_DOMAIN["HelperController\n+ 값 객체들\n(Level, Grade, Energy, Exp)"]
        H_REPO["HelperDatabase\n(ScriptableObject)"]
        H_DATA["HelperDataSO\nHelperSaveData"]

        H_UI -->|"C# event 구독"| H_AB
        H_AB -->|"직접 호출"| H_DOMAIN
        H_AB -->|"GetById()"| H_REPO
        H_REPO --> H_DATA
        H_DOMAIN -.->|"값 객체 이벤트"| H_UI
    end

    subgraph FARM_ARCH["농사 시스템 아키텍처"]
        direction TB
        F_UI["UI\nHarvestNotification\nManager"]
        F_AB["PlayerInventory\nAbility\n(ISaveableAbility)"]
        F_DOMAIN["FarmTile\n+ CropGrowth\n+ InventoryDomain"]
        F_REPO["SeedDatabase\nTerrainGridManager"]
        F_DATA["SeedItemDataSO\nFarmSaveData"]
        F_SO["HarvestItemSO\n(SO 이벤트 채널)"]

        F_SO -->|"이벤트 발행"| F_UI
        F_AB -->|"event 구독"| F_UI
        F_AB -->|"위임"| F_DOMAIN
        F_DOMAIN -->|"Raise()"| F_SO
        F_REPO -->|"GetById()"| F_DATA
        F_DOMAIN -.->|"AddItem()"| F_AB
    end
```

### 핵심 차이점

| 항목 | Helper 시스템 | 농사 시스템 |
|------|--------------|------------|
| **도메인 구조** | 값 객체 패턴 (Level, Grade, Energy, Exp) | 상태 머신 패턴 (FarmDry ↔ FarmWet) |
| **UI 통신** | C# 이벤트 (Ability 레이어 경유) | ScriptableObject 채널 (도메인 직접 발행) |
| **상태 저장** | Dictionary 캐시 + ISaveableAbility | TerrainGridManager 순회 + ExportTo 패턴 |
| **성장 트리거** | 플레이어 입력 기반 | 시간 이벤트 기반 (TimeEvents.OnDayStarted) |
| **데이터 조회** | HelperDatabase (string ID) | SeedDatabase (int ID) |
| **네트워크 동기화** | Photon RPC (모든 행동) | RPC (땅 파기/채우기만) |

---

## 11. 구조적 강점 및 개선 가능 영역

### 강점 (잘 설계된 부분)

```mermaid
flowchart LR
    subgraph STRENGTHS["구조적 강점"]
        S1["ISaveRepository 인터페이스\n→ 저장소 교체 용이\n(로컬, 클라우드 등)"]
        S2["ISaveableAbility 인터페이스\n→ 모든 Ability의\n저장/로드 표준화"]
        S3["값 객체 (Value Objects)\n→ 도메인 규칙 캡슐화\n(HelperLevel, Grade, Energy, Exp)"]
        S4["ScriptableObject Database\n→ 런타임 참조 없이\n에디터에서 데이터 관리"]
        S5["이벤트 기반 UI 갱신\n→ 도메인이 UI를 모름\n(단방향 의존성)"]
        S6["EGrowthTiming 설계\n→ 씨앗별 성장 시점 제어\n(Morning/Night/Both)"]
    end
```

### 개선 가능 영역

```mermaid
flowchart TD
    subgraph IMPROVEMENTS["개선 가능한 영역"]
        I1["TerrainGridManager\n역할 과부하\n\nManager + Repository\n+ Domain을 모두 수행\n\n→ TerrainRepository 분리 권장"]

        I2["CropGrowth → PlayerInventoryAbility\n직접 참조\n\n도메인이 Ability 레이어를 앎\n(역방향 의존성)\n\n→ IInventoryChannel 인터페이스 또는\nScriptableObject 이벤트 채널 사용 권장"]

        I3["정적 TimeEvents\n테스트 어려움\n\n정적 이벤트는 단위 테스트 시\n모킹 불가\n\n→ ITimeEventChannel 인터페이스 권장"]

        I4["저장 실패 처리 부재\n\n파일 IO 실패 시\n롤백 메커니즘 없음\n\n→ try/catch + 백업 파일 권장"]
    end
```

#### 개선 예시: CropGrowth의 역방향 의존성 해결

```
현재 구조:
  CropGrowth (Domain) → PlayerInventoryAbility (Ability)  ← 역방향 의존

개선 구조:
  CropGrowth (Domain) → IHarvestReceiver (인터페이스)
                              ↑ 구현
                        PlayerInventoryAbility (Ability)

또는:
  CropGrowth (Domain) → HarvestItemSO (이벤트 채널)
                              ↓ 구독
                        PlayerInventoryAbility (Ability)
```

---

## 레이어별 파일 위치 최종 정리

### Helper 시스템

| 레이어 | 파일 경로 | 역할 |
|--------|----------|------|
| **UI** | `Helper/UI/UI_HelperInventory.cs` | 헬퍼 카로셀 UI |
| **UI** | `Helper/UI/UI_HelperExperience.cs` | 경험치 바 |
| **UI** | `Helper/UI/UI_HelperActionInfo.cs` | 행동 설명 |
| **UI** | `Helper/UI/HelperSeedBubbleUI.cs` | 씨앗 선택 표시 |
| **Ability** | `Player/Abilities/PlayerHelperInventoryAbility.cs` | 헬퍼 인벤토리 + ISaveableAbility |
| **Ability** | `Player/Abilities/PlayerHelperInteractionAbility.cs` | 상호작용 중개 |
| **Domain** | `Helper/Core/HelperController.cs` | 헬퍼 상태 머신 |
| **Domain** | `Helper/Core/HelperLevel.cs` | 레벨 값 객체 |
| **Domain** | `Helper/Core/HelperGrade.cs` | 등급 값 객체 |
| **Domain** | `Helper/Core/HelperEnergy.cs` | 에너지 값 객체 |
| **Domain** | `Helper/Upgrade/HelperExperience.cs` | 경험치 값 객체 |
| **Repository** | `Helper/Data/HelperDatabase.cs` | 헬퍼 ID 조회 |
| **Data** | `Helper/Data/HelperDataSO.cs` | 헬퍼 메타데이터 (SO) |
| **Data** | `OutGame/Save/Data/HelperSaveData.cs` | 헬퍼 저장 데이터 |

### 농사 시스템

| 레이어 | 파일 경로 | 역할 |
|--------|----------|------|
| **UI** | `Farming/Crop/HarvestNotificationManager.cs` | 수확 알림 관리 |
| **UI** | `Farming/Crop/HarvestNotification.cs` | 알림 단위 UI |
| **Ability** | `Player/Abilities/PlayerInventoryAbility.cs` | 인벤토리 + ISaveableAbility |
| **Domain** | `Farming/Tile/FarmTile.cs` | 농지 상태 머신 |
| **Domain** | `Farming/Tile/FarmTileStateMachine.cs` | Dry/Wet 전이 |
| **Domain** | `Farming/Crop/CropGrowth.cs` | 작물 성장 엔진 |
| **Domain** | `Inventory/Core/InventoryDomain.cs` | 인벤토리 규칙 |
| **Domain** | `Inventory/Core/InventorySlot.cs` | 슬롯 값 객체 |
| **Repository** | `Farming/Grid/TerrainGridManager.cs` | 격자 관리 + 저장 포맷 변환 |
| **Repository** | `Farming/Data/SeedDatabase.cs` | 씨앗 ID 조회 |
| **Repository** | `OutGame/Save/LocalJsonSaveRepository.cs` | JSON 파일 저장소 |
| **Data** | `Farming/Data/SeedItemDataSO.cs` | 씨앗 메타데이터 (SO) |
| **Data** | `OutGame/Save/Data/FarmSaveData.cs` | 농지 저장 데이터 |
| **Data** | `OutGame/Save/Data/TerrainCellSaveData.cs` | 셀 저장 데이터 |
| **이벤트 채널** | `Farming/Data/HarvestItemSO.cs` | 수확 이벤트 채널 (SO) |
| **이벤트 채널** | `TimeSystem/TimeEvents.cs` | 시간 이벤트 채널 (static) |

---

*이 문서는 Helper 및 농사 시스템의 도메인 레이어 아키텍처를 분석하여 작성되었습니다.*
