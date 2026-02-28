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

- 중괄호 추가 규칙은 **자율**로 하되, 추후 가독성이 좋은 규칙을 논의할 여지를 남긴다.
- `if`, `for`, `foreach`, `while` 등의 본문이 **한 줄**이면 중괄호를 생략할 수 있다.

```csharp
// 허용 - 한 줄 본문은 중괄호 생략 가능
if (other is null)
    return false;

if (!isValid)
    continue;

// 허용 - 중괄호를 넣어도 무방
if (other is null)
{
    return false;
}
```

## 명명 규칙

### 파일명

- **클래스명과 파일명은 일치**시킨다. (`PlayerController` 클래스 → `PlayerController.cs`)
- 한 클래스는 한 파일로 분리한다.
  - 대표 클래스의 하위 클래스들이고, 함께 보는 것이 가독성에 유리한 경우 일부 예외 허용
- 클래스명과 에셋명은 일치시킨다.
- 문서 파일에 한해 **snake_case**를 사용한다. (`csharp_coding_convention.md`)

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
