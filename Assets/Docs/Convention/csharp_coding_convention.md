# 클라이언트 C# 코딩 컨벤션 지침

## 개요

본 문서는 클라이언트 프로그래머들이 일관된 코드 스타일과 가독성, 유지보수성을 확보하기 위한 코딩 컨벤션을 정의한다.

충돌되는 항목이 있는 경우 **MSDN 기준을 우선 적용**한다.

## 기본 가이드라인

다음 문서를 기본 가이드라인으로 삼는다:

- [Microsoft C# Coding Conventions (MSDN)](https://learn.microsoft.com/ko-kr/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Unity C# Programming Guidelines](https://unity.com/resources/create-code-c-sharp-style-guide-e-book)
- [C# at Google 스타일 가이드](https://google.github.io/styleguide/csharp-style.html)
- [Microsoft Framework Design Guide](https://learn.microsoft.com/ko-kr/dotnet/standard/design-guidelines/)

### 적용 원칙

- 세 문서의 공통 사항은 그대로 준수한다.
- 중복되거나 충돌되는 항목의 경우 **MSDN 스타일**을 채택한다.
- Unity 특화 환경(예: `MonoBehaviour` 관련 규칙)은 **Unity 공식 컨벤션**을 따른다.

## 코드 스타일

### 인덴테이션

- **Allman(BSD) 스타일**을 적용한다.

```csharp
// Good
if (condition)
{
    DoSomething();
}

// Bad
if (condition) {
    DoSomething();
}
```

### 접근 제한자

- 모든 선언에는 접근 제한자를 **명시**한다. (인터페이스 멤버 제외)

```csharp
// Good
private int health;
public void TakeDamage(int amount) { }

// Bad
int health;
void TakeDamage(int amount) { }
```

### 중괄호

- `if`, `for`, `foreach`, `while` 등의 본문이 **한 줄**이면 중괄호를 **생략**한다.
- 단, else/else-if 체인에서 하나라도 여러 줄이 필요하면 체인 전체에 중괄호를 유지한다.

```csharp
// Good - 한 줄 본문은 중괄호 생략
if (other is null)
    return false;

if (!isValid)
    continue;

// Bad - 한 줄인데 중괄호 사용
if (other is null)
{
    return false;
}

// Good - else 체인에서 한 곳이 여러 줄이면 전체 유지
if (a)
{
    DoA();
}
else if (b)
{
    DoB1();
    DoB2();
}
else
{
    DoC();
}
```

### var 사용

- 우변에서 타입이 **명확히 드러나는** 경우 `var`를 사용한다.
- 우변만 보고 타입을 즉시 알 수 없는 경우 타입을 명시한다.

```csharp
// Good - 우변에서 타입이 명확
var player = new Player();
var states = GetComponentsInChildren<SceneState>(true);
var members = new List<SquadMember>();

// Good - 타입이 불명확하면 명시
IUnit closest = null;
float minDist = float.MaxValue;

// Bad - 우변이 명확한데 타입 반복
Player player = new Player();
List<SquadMember> members = new List<SquadMember>();
```

## 명명 규칙

### 파일명

- **클래스명과 파일명은 일치**시킨다. (`PlayerController` 클래스 → `PlayerController.cs`)
- 한 클래스는 한 파일로 분리한다.
  - 대표 클래스의 하위 클래스들이고, 함께 보는 것이 가독성에 유리한 경우 일부 예외 허용
  - **enum과 interface는 클래스가 아니므로**, 밀접하게 연관된 타입을 같은 파일에 함께 정의할 수 있다.
    - 예: `UnitTeam` enum과 `IUnit` interface → `IUnit.cs` 한 파일에 공존 허용
- 클래스명과 에셋명은 일치시킨다.
- 문서 파일에 한해 **snake_case**를 사용한다. (`csharp_coding_convention.md`)

### 변수명

- 변수 이름에 **언더바(`_`) 접두사를 사용하지 않는다**. 클래스 필드와 지역 변수를 구분하기 위한 접두사는 불필요하다.

```csharp
// Good
private int health;
private AudioSource bgmSource;

// Bad
private int _health;
private AudioSource _bgmSource;
```

### 메서드명

- 함수 이름에 **언더바 등 접두사는 사용하지 않는다**.
- 비동기 메서드는 **`Async` 접미사**를 붙인다.

```csharp
// Good
public async Task LoadDataAsync() { }
public void Initialize() { }

// Bad
public async Task LoadData() { }
public void _Initialize() { }
```

## using 문

- 필요 없는 항목을 정리하여 **최소화**한다.

## 아키텍처 레이어 규칙

MVP 패턴을 기반으로 레이어를 분리한다. **MonoBehaviour는 View/Bridge 레이어에만 허용**한다.

| 레이어 | 형태 | 역할 | 예시 |
|--------|------|------|------|
| Model / Logic | pure C# | 게임 로직, 상태 관리 | `Character`, `UnitHealth`, `UnitCombat`, `GameController` |
| View | MonoBehaviour | Unity 렌더링, 컴포넌트 연결 | `CharacterView`, `PlayerView` |
| Bridge | MonoBehaviour | Unity 생명주기(Update 등)를 Controller에 위임 | `InPlayState`, `SceneState` 계열 |

```csharp
// Good - 로직은 pure C#
public class UnitHealth
{
    public int CurrentHp { get; private set; }
    public void TakeDamage(int damage) { ... }
}

// Bad - 로직에 MonoBehaviour 사용
public class UnitHealth : MonoBehaviour  // ← MVP 레이어 위반
{
    public void TakeDamage(int damage) { ... }
}
```

- Model/Logic 레이어 클래스가 `MonoBehaviour`를 상속하면 MVP 레이어 위반으로 간주한다.
- Bridge 레이어는 직접 로직을 구현하지 않고, Controller의 메서드를 호출하는 역할만 한다.
- `Update()`가 필요한 경우, 독립적인 MonoBehaviour를 추가로 만들지 않고 **이미 존재하는 MonoBehaviour(SceneState 등)의 Update()를 활용**한다.

## 컴포넌트 참조 규칙

### GetComponent 지양

런타임 `GetComponent` / `GetComponentInChildren` 호출을 **지양**한다.
`GetComponent`는 타입 안전성이 낮고, 참조 누락이 런타임에서야 발견된다.

대신 **SerializeField + 공개 프로퍼티** 패턴으로 Inspector에서 연결한다.

```csharp
// Good - SerializeField로 Inspector에서 연결
public class PlayPage : Page
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private WorldMap worldMap;
    [SerializeField] private PlayerView playerView;

    public Canvas Canvas => canvas;
    public WorldMap WorldMap => worldMap;
    public PlayerView PlayerView => playerView;
}

// Bad - 런타임 GetComponent 탐색
public class InPlayState : SceneState
{
    protected override async UniTask OnExecuteAsync()
    {
        var canvas = playPage.GetComponentInChildren<Canvas>();       // ← 지양
        var mapGen = worldMapRoot.GetComponentInChildren<MapGenerator>(); // ← 지양
    }
}
```

- 컴포넌트 간 의존성은 Unity Inspector(SerializeField)에서 명시적으로 연결한다.
- 부득이하게 `GetComponent`를 사용해야 할 경우 `Awake()`나 초기화 단계에서 **1회만** 호출하고 결과를 캐시한다.

### Update() 집중화

개별 MonoBehaviour에 `Update()`를 분산하지 않고, **중앙 게임 루프가 각 시스템을 순서대로 호출**한다.

```csharp
// Good - GameController가 순서를 명시적으로 제어
public class GameController
{
    public void Update()
    {
        Player.Move(playerInput.MoveDirection); // 1. 입력
        Squad.Update(dt);                        // 2. 부대
        entitySpawner.Update(dt);               // 3. 몬스터
        combatSystem.Update();                  // 4. 전투
    }
}

// Bad - 각 시스템이 독립적인 MonoBehaviour Update() 보유
public class CombatSystem : MonoBehaviour  // ← 분산 Update, 호출 순서 불명확
{
    private void Update() { ... }
}
```

- 게임 시스템 클래스(`CombatSystem`, `EntitySpawner` 등)는 `MonoBehaviour`를 상속하지 않고, `Tick(float dt)` 또는 `Update()` 메서드를 순수 C# 메서드로 구현한다.
- Unity의 `Update()` 진입점은 Bridge 레이어(`InPlayState` 등) **한 곳**에서만 사용한다.

## 기타 규칙

- 사소하고 특별히 옳은 것을 정하기 어려운 규칙은 필수하지 않고 **자율적으로 적용**한다.
- 엔진 코드가 확정되어 통합/배포되기 전까지는 **엔진 코드의 기본 컨벤션**에 맞춰서 작성한다.

## EditorConfig

본 컨벤션에 부합하는 `.editorconfig` 파일을 프로젝트 루트 디렉토리에 위치시켜 자동 코드 포맷팅에 활용한다.

- 리샤퍼 세팅을 포함한 `.editorconfig` 파일 사용
- MSDN에서 제공하는 파일과 C# 부분은 동일하며, 리샤퍼/라이더가 자동 생성하는 세팅들이 추가됨

## 논의가 필요한 사항

- **변수 선언시 여백 맞춤**: 가독성 향상에 대한 의견이 엇갈림
- **TG 접두사** 사용 여부
- **추상클래스의 파일명에 점 추가** 여부
- **네임스페이스 규칙**

## 참고 자료

- [유니티 코딩 가이드 (영문)](https://unity.com/resources/create-code-c-sharp-style-guide-e-book)
- [유니티 코딩 가이드 (한글 번역)](https://unity.com/kr/resources/create-code-c-sharp-style-guide-e-book)
- [MSDN .editorconfig 파일](https://learn.microsoft.com/ko-kr/dotnet/fundamentals/code-analysis/code-style-rule-options)
