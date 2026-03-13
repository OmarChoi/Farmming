# 📌 C# 코딩 컨벤션 및 코드 리뷰 스타일 가이드

이 문서는 Unity 기반 C# 프로젝트의 코드 일관성, 가독성, 유지 보수성 확보를 위한 공식 코딩 규칙을 정의합니다.
모든 Pull Request(PR)는 아래 명시된 규칙을 준수해야 하며, 코드 리뷰 시 본 가이드를 기준으로 검토합니다.

---

## 0. 🤖 코드 리뷰 봇 지침

### 0.1 리뷰 언어

* 모든 코드 리뷰 요약 및 코멘트는 **한국어(Korean)**로 작성합니다.
* 한국인이 쉽게 이해할 수 있도록 명확하고 자연스러운 한글 설명을 사용하며, 영어나 기타 언어 사용은 허용되지 않습니다.

### 0.2 필수 검토 항목

모든 PR에 대해 다음을 반드시 검토합니다:

* **SOLID 원칙** 위반 여부
* **디미터의 법칙(Law of Demeter)** 위반 여부
* **계층 구조** 위반 여부
* **의존성 방향** 위반 여부
* **이벤트 구독/해제** 누락 여부
* **런타임 데이터와 설정 데이터** 혼재 여부
* **성능상 위험 요소** 존재 여부
* **보이스카우트 법칙** 적용 여부
* 함수가 **한 가지 일만 수행**하는지 여부

### 0.3 리뷰 출력 형식

리뷰는 항상 다음 구조를 따릅니다:

```
## 📌 PR 요약
- 무엇을 변경했는지 요약

## ❗ 주요 문제점
- [심각] 계층 위반
- [중간] SOLID 위반
- [경미] 네이밍 문제

## 💡 개선 제안
- 코드 예시 포함

## ✅ 잘된 점
- 설계상 우수한 부분
```

---

## 1. 🏗️ 아키텍처 규칙

### 1.1 계층 분리 원칙

프로젝트는 최소한 다음 계층을 분리해야 합니다:

* **Presentation (UI / View)**
* **Application / Manager (유스케이스 조정, 상태 변경, 이벤트 발행)**
* **Domain (비즈니스 로직, Value Object, Enum)**
* **Infrastructure (저장소, 네트워크, 외부 SDK 연동)**

#### 의존성 규칙

* Presentation → Application → Domain 방향으로만 의존합니다.
* Infrastructure는 Domain 인터페이스를 구현합니다.
* Domain은 Unity 또는 외부 SDK에 의존하지 않습니다.
* 하위 계층은 상위 계층을 참조하면 안 됩니다.

#### 리뷰 체크

* ❌ Infrastructure가 UI를 직접 참조
* ❌ Domain이 MonoBehaviour 상속
* ❌ UI가 Repository를 직접 호출
* ❌ 순환 참조

### 1.2 Repository 패턴

* 반드시 인터페이스를 먼저 정의합니다.
* Manager는 구현체가 아닌 인터페이스에 의존해야 합니다.
* 저장/로드는 Infrastructure 계층에서만 수행합니다.

```csharp
public interface ICurrencyRepository
{
    UniTask<CurrencySaveData> Load();
    UniTask Save(CurrencySaveData data);
}
```

#### 리뷰 체크

* 구현체에 직접 의존하고 있지 않은가?
* Repository에 비즈니스 로직이 들어가 있지 않은가?

### 1.3 이벤트 기반 통신

계층 간 결합도를 낮추기 위해 이벤트 기반 통신을 사용합니다.

```csharp
public static event Action OnDataChanged;
```

#### 리뷰 체크

* 이벤트 구독 시 해제 코드가 반드시 존재하는가?
* Awake/OnEnable에서 구독하고 OnDisable/OnDestroy에서 해제하는가?
* 직접 참조 대신 이벤트로 통신할 수 있는데 강결합을 만들고 있지는 않은가?

### 1.4 설정 데이터 vs 런타임 데이터 분리

| 분류 | 설명 | 예시 |
| :--- | :--- | :--- |
| **설정 데이터** | 변경 불가 데이터 | ScriptableObject, JSON 설정 |
| **런타임 데이터** | 플레이 중 변경되는 값 | 저장 대상 데이터, 상태 값 |

#### 리뷰 체크

* ScriptableObject 값을 런타임에 수정하고 있지 않은가?
* 설정 데이터와 상태 데이터가 한 클래스에 섞여 있지 않은가?

---

## 2. 🎯 디자인 원칙

### 2.1 SOLID 원칙

| 약어 | 원칙 | 핵심 요약 | 리뷰 포인트 |
| :---: | :--- | :--- | :--- |
| **S** | **단일 책임 원칙 (SRP)** | 클래스는 **단 하나의 변경 이유**만 가져야 합니다. | UI + 계산 로직 + 저장 로직이 한 클래스에 섞여 있지 않은가? |
| **O** | **개방-폐쇄 원칙 (OCP)** | **확장에는 열려 있고, 수정에는 닫혀 있어야** 합니다. | 기능 추가 시 기존 코드를 수정해야 하는 구조인가? switch/case 남용 여부 |
| **L** | **리스코프 치환 원칙 (LSP)** | 상위 타입을 하위 타입으로 **치환해도 문제없이 작동**해야 합니다. | 하위 타입이 상위 타입을 완전히 대체 가능한가? |
| **I** | **인터페이스 분리 원칙 (ISP)** | **단일 목적의 작은 인터페이스**를 선호합니다. | 거대한 인터페이스를 강요하고 있지 않은가? |
| **D** | **의존성 역전 원칙 (DIP)** | **고수준/저수준 모듈 모두 추상화에 의존**해야 합니다. | 구체 클래스 대신 인터페이스에 의존하는가? |

### 2.2 디미터의 법칙 (Law of Demeter)

**"오직 가장 가까운 친구와만 이야기하라"**는 원칙을 따릅니다.

메서드 내부에서 호출 가능한 대상:

1. 현재 객체 **자신**
2. 메서드의 **매개변수로 전달된 객체**
3. 현재 객체의 **필드(멤버 변수)**로 가지고 있는 객체
4. 메서드 내에서 **새로 생성된 객체**

#### ❌ 위반 예시

```csharp
manager.Get(type).MetaData.Effects[0].BaseValue;
```

#### ✅ 권장

```csharp
manager.GetEffectValue(type);
```

#### 리뷰 체크

* 객체 내부 구조를 과도하게 탐색하고 있지 않은가?
* 체이닝이 2 depth 이상 반복되는가?

---

## 3. 🏷️ 명명 규칙 (Naming Conventions)

### 3.1 네이밍 컨벤션

| 대상 | 규칙 | 예시 |
| :--- | :--- | :--- |
| 클래스, 구조체, 레코드, 대리자 | PascalCase | `PlayerController` |
| Interface | `I` + PascalCase | `ICurrencyRepository` |
| Enum | `E` + PascalCase | `EItemType` |
| Public 속성 | PascalCase | `CurrentHealth` |
| 메서드 | PascalCase | `CalculateDamage` |
| 상수 (const, static readonly) | PascalCase | `MaxRetryCount` |
| Private 인스턴스 필드 | `_` + camelCase | `_workerQueue` |
| Protected 필드 | `_` + camelCase | `_baseValue` |
| SerializeField | `_` + camelCase | `_spawnPoint` |
| 정적 필드 | `s_` + camelCase | `s_defaultLogger` |
| 스레드 정적 필드 | `t_` + camelCase | `t_timeSpan` |
| 메서드 매개변수, 지역 변수 | camelCase | `playerName` |
| Coroutine | `MethodName_Coroutine` | `FadeOut_Coroutine` |
| UI 클래스 | `UI_` + 기능명 | `UI_Inventory` |
| ScriptableObject | 기능명 + `SO` | `WeaponDataSO` |
| 레코드 기본 생성자 매개변수 | PascalCase | `record Player(string Name)` |
| 클래스/구조체 기본 생성자 매개변수 | camelCase | `class Foo(int value)` |

### 3.2 함수(메서드) 명명 규칙

* 함수명은 **동사 + 목적어**, 또는 의미 단위 전체를 사용합니다.
* 함수는 반드시 **한 가지 일만 수행**합니다.
* Boolean 반환 함수는 반드시 **질문형**으로 작성합니다. (예: `IsValid`, `HasPermission`, `CanExecute`)
* 이벤트 핸들러는 **`On` 접두사**를 반드시 붙입니다. (예: `OnPlayerDeath`, `OnButtonClicked`)

### 3.3 일반 명명 원칙

* **명확성 우선:** 간결성보다 명확성을 우선합니다.
* 두 개의 연속된 밑줄(`__`)은 사용하지 않습니다.
* 단순 루프 카운터를 제외하고 단일 문자 이름 사용을 피합니다.
* 네임스페이스는 역방향 도메인 이름 표기법을 따릅니다.

### 3.4 클래스 멤버 순서

1. Static 멤버
2. SerializeField
3. Private 필드
4. Properties
5. Unity Lifecycle (Awake, OnEnable, Start, Update, OnDisable, OnDestroy)
6. Public 메서드
7. Private 메서드
8. Coroutine

---

## 4. 📐 코드 스타일 및 레이아웃

### 4.1 기본 서식

* **들여쓰기:** 4개의 공백을 사용하며, 탭은 사용하지 않습니다.
* **중괄호:** **Allman 스타일**을 사용합니다. 여는/닫는 중괄호는 각각 자체 줄에 위치합니다.
* **코드 밀도:** 한 줄에 하나의 문장, 하나의 선언만 작성합니다.
* **빈 줄:** 메서드 정의와 속성 정의 간에는 한 줄 이상의 빈 줄을 추가합니다.
* **접근 제어자:** 항상 명시합니다.
* **readonly:** 적극적으로 사용합니다.

### 4.2 주석 규칙

* **코드 주석:** `//`를 사용하며, 코드 줄 끝이 아닌 **별도의 줄**에 배치합니다.
* 주석 텍스트는 **대문자로 시작**하고 끝에 **마침표**를 찍습니다.
* **XML 주석(`<summary>`, `<param>`, `<returns>` 등)은 작성하지 않습니다.** 메서드명과 매개변수명이 충분히 설명적이어야 합니다.

### 4.3 도구 및 자동화

* **`.editorconfig`** 파일을 사용하여 IDE에서 스타일 지침을 자동 적용합니다.
* **코드 분석(Code Analysis)**을 사용하며, CI 빌드 단계에서 규칙 위반 시 경고가 발생하도록 구성합니다.

---

## 5. 📝 C# 언어 사용 규칙

* **최신 기능 활용:** 가능하면 최신 C# 언어 기능을 활용합니다.
* **데이터 형식:** 런타임 형식(예: `System.Int32`) 대신 **언어 키워드**(`int`, `string`)를 사용합니다.
* **`var` 사용:** 변수 형식을 **명확히 유추할 수 있는 경우에만** `var`를 사용합니다.
* **문자열 처리:**
    * 짧은 연결에는 **문자열 보간**(`$"{}"`)을 사용합니다.
    * 루프 내 대용량에는 **`System.Text.StringBuilder`**를 사용합니다.
    * 긴 메시지에는 **원시 문자열 리터럴**(`"""..."""`) 사용을 권장합니다.
* **컬렉션 초기화:** **컬렉션 식(`[]`)**을 사용하여 모든 컬렉션 형식을 초기화합니다.
* **대리자:** 사용자 정의 대리자 대신 **`Func<>` 또는 `Action<>`**을 사용합니다.

---

## 6. ⚡ 비동기 및 에러 처리

### 6.1 비동기 처리 기준

| 상황 | 사용 기술 |
| :--- | :--- |
| I/O 작업 | UniTask (async/await) |
| 타이밍 기반 연출 | Coroutine |

#### 리뷰 체크

* I/O에 Coroutine을 사용하고 있지 않은가?
* `async UniTaskVoid`에 `.Forget()` 누락 여부
* 교착 상태 주의 및 필요시 `ConfigureAwait` 사용 고려

### 6.2 에러 처리 패턴

* **처리할 수 있는 특정 예외만 `catch`**하며, 일반적인 `System.Exception`을 포괄적으로 잡지 않습니다.

| 패턴 | 사용 시점 | 예시 |
| :--- | :--- | :--- |
| 단순 성공/실패 | 결과가 bool로 충분할 때 | `public bool TrySpend(...)` |
| 실패 원인 필요 | 실패 사유를 구분해야 할 때 | `public UniTask<LoginResult> TryLogin(...)` |

#### 리뷰 체크

* 불필요한 Result 남용 여부
* `TryXxx`에서 예외 throw 여부

---

## 7. 🚀 성능 리뷰 포인트

### 7.1 일반 성능

* Update() 내 GC Alloc을 유발하는 코드는 없는가? (LINQ, `new` 객체 생성, string 결합, Find/GetComponent 반복 호출 등)
* `GetComponent<T>()`를 캐싱했는가?
* 문자열 할당 반복 여부
* 오브젝트 풀링 미사용 여부
* LINQ 남용 여부

---

## 8. 🧹 보이스카우트 법칙 (Boy Scout Rule)

> "코드를 수정하기 위해 열었다면, 원래보다 더 깨끗하게 만들고 나올 것"

기능 구현과 무관하더라도, 해당 파일 또는 주변 코드의 품질을 다음 기준에 따라 개선합니다:

### 8.1 가독성 개선

* 변수명, 함수명을 더 명확하게 다듬었는지 확인합니다.
* 불필요하게 복잡한 조건문, 중첩 블록 등을 단순화했는지 검토합니다.
* 긴 메서드는 작은 메서드로 적절히 분리했는지 확인합니다.

### 8.2 중복 코드 정리

* 동일한 로직이 반복되는 부분을 함수화하거나 제거했는지 확인합니다.

### 8.3 불필요한 코드 제거

* 사용하지 않는 변수, 필드, 함수, 주석, 디버그 코드(`Debug.Log`)를 삭제했는지 확인합니다.
* 의미 없는 빈 메서드, 빈 `Update()` 등이 남아 있지 않은지 검토합니다.

### 8.4 안정성 강화

* null-check 및 guard clause를 추가하여 안전성을 향상했는지 확인합니다.
* 예외 처리 시 더 구체적인 예외 타입을 사용했는지 검토합니다.

### 8.5 매직 넘버 처리

* 고정값을 `const` 또는 `static readonly`로 분리했는지 확인합니다.

---
