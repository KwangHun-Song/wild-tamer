# 전투 연출 (VFX) - 코드 리뷰

## 리뷰 대상 파일

| 파일 | 경로 | 역할 |
|------|------|------|
| `HitStop.cs` | `Assets/Scripts/04.Game/02.System/VFX/HitStop.cs` | 역경직 (timeScale 조작) |
| `CameraShake.cs` | `Assets/Scripts/04.Game/02.System/VFX/CameraShake.cs` | 카메라 흔들림 |
| `HitEffectPlayer.cs` | `Assets/Scripts/04.Game/02.System/VFX/HitEffectPlayer.cs` | 이펙트 스폰 + 사운드 재생 |

### 참조 문서

| 문서 | 경로 |
|------|------|
| 설계 문서 | `Assets/Docs/System/Phase2_Core/design/vfx.md` |
| 설계 리뷰 | `Assets/Docs/System/Phase2_Core/review/concept_design_review.md` |

---

## 설계 일관성 검증

| 항목 | 설계 문서 | 실제 구현 | 판단 |
|------|-----------|-----------|------|
| HitStop isActive 중첩 방지 | `isActive` 플래그로 중복 코루틴 차단 | 동일하게 구현 | 일치 |
| HitStop WaitForSecondsRealtime | `WaitForSecondsRealtime(duration)` 사용 | 동일하게 구현 | 일치 |
| CameraShake 오프셋 계산 | `Random.insideUnitCircle * intensity` | 동일하게 구현 | 일치 |
| HitEffectPlayer.OnHit 스폰 방식 | `Facade.Pool.Spawn(effectPrefab, target.Transform.position)` | `Facade.Pool.Spawn(effectPrefab).transform.position = target.Transform.position` | 구현이 개선된 형태 (아래 설명) |
| Dispose() Notifier.Unsubscribe | 3개 클래스 모두 호출 | 동일하게 구현 | 일치 |
| Notifier.Subscribe 생성자 호출 | 3개 클래스 모두 생성자에서 구독 | 동일하게 구현 | 일치 |

### HitEffectPlayer 스폰 방식 차이 설명

설계 문서의 `Facade.Pool.Spawn(effectPrefab, target.Transform.position)`은 두 번째 인자로 `Vector3` 위치를 전달하지만, 실제 `IObjectPool.Spawn` 시그니처는 `Spawn(GameObject prefab, Transform parent = null)`으로 두 번째 인자가 `Transform`이다. 구현에서는 `Spawn(effectPrefab)`으로 먼저 생성 후 `.transform.position`을 설정하는 방식을 택했는데, 이는 실제 인터페이스에 맞는 올바른 호출이다.

---

## 코딩 컨벤션 충족도

| 항목 | 충족 | 비고 |
|------|------|------|
| 접근 제한자 명시 | O | 모든 필드/메서드에 접근 제한자 명시 |
| 명명 규칙 (PascalCase/camelCase) | O | 클래스, 메서드, 필드 모두 규칙 준수 |
| 언더바 접두사 미사용 | O | 필드에 언더바 접두사 없음 |
| Allman 스타일 중괄호 | O | 모든 파일에서 BSD/Allman 스타일 적용 |
| using 문 최소화 | O | 필요한 using만 포함 |
| 파일명-클래스명 일치 | O | 3개 파일 모두 일치 |

---

## 긍정적인 점

1. **설계 리뷰 이슈 7 정확히 반영**: `HitStop`에 `isActive` 플래그를 도입하여 중첩 코루틴 문제를 깔끔하게 방지했다. 설계 리뷰에서 지적된 사항이 코드에 충실히 반영되었다.

2. **WaitForSecondsRealtime 올바른 사용**: `timeScale = 0` 상태에서 일반 `WaitForSeconds`는 영원히 대기하지만 `WaitForSecondsRealtime`을 사용하여 이를 정확히 우회했다.

3. **일관된 생명주기 관리**: 3개 클래스 모두 생성자에서 `notifier.Subscribe(this)`, `Dispose()`에서 `notifier.Unsubscribe(this)`로 구독/해제 패턴이 통일되어 있다.

4. **pure C# 원칙 준수**: 3개 클래스 모두 MonoBehaviour를 상속하지 않으며, `Facade.Coroutine`과 `Facade.Pool`, `Facade.Sound`를 통해 Unity 기능에 간접 접근한다. 설계 방침(MonoBehaviour 사용 기준표)과 일치한다.

5. **CameraShake의 unscaledDeltaTime 사용**: `Time.unscaledDeltaTime`을 사용하여 HitStop(`timeScale = 0`)과 동시에 발생해도 셰이크 타이밍이 정상 동작한다.

6. **HitEffectPlayer IObjectPool 인터페이스 올바른 호출**: 설계 문서의 시그니처 불일치를 구현 단계에서 올바르게 수정하여 실제 `IObjectPool.Spawn(GameObject)` 인터페이스에 맞게 호출한다.

---

## 이슈

### 1. HitStop에서 이전 timeScale 복원값 미저장 (중요도: 높음)

**파일**: `HitStop.cs:37-39`

```csharp
private IEnumerator ApplyHitStop()
{
    isActive = true;
    Time.timeScale = 0f;                          // ← 이전 값 저장 없이 0으로 설정
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1f;                          // ← 항상 1f로 복원
    isActive = false;
}
```

`timeScale`을 0으로 설정하기 전에 이전 값을 저장하지 않고, 복원 시 무조건 `1f`로 설정한다. 슬로우 모션, 배속 플레이 등 `timeScale != 1f`인 상태에서 HitStop이 발생하면 원래의 timeScale이 유실된다.

**제안**:

```csharp
private IEnumerator ApplyHitStop()
{
    isActive = true;
    var previousTimeScale = Time.timeScale;
    Time.timeScale = 0f;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = previousTimeScale;
    isActive = false;
}
```

---

### 2. CameraShake 코루틴 중첩 문제 (중요도: 중간)

**파일**: `CameraShake.cs:27-28`

```csharp
public void OnHit(IUnit attacker, IUnit target, int damage)
{
    Facade.Coroutine.StartCoroutine(Shake());
}
```

`HitStop`과 달리 `CameraShake`에는 중첩 방지 로직이 없다. 다수의 히트 이벤트가 빠르게 연속 발생하면 여러 `Shake()` 코루틴이 동시에 실행되며, 각각이 `origin` 위치를 캡처하기 때문에 셰이크 종료 시 카메라가 원래 위치가 아닌 엉뚱한 위치로 복원될 수 있다.

예시 시나리오:
1. 코루틴 A 시작: `origin = (0,0,0)`, 카메라를 `(0.1, 0.05, 0)`으로 이동
2. 코루틴 B 시작: `origin = (0.1, 0.05, 0)` (이미 흔들린 위치를 원점으로 캡처)
3. 코루틴 A 종료: `position = (0,0,0)` (원래 위치로 복원)
4. 코루틴 B 종료: `position = (0.1, 0.05, 0)` (잘못된 위치로 복원)

**제안**: `isShaking` 플래그를 도입하여 진행 중인 셰이크가 있으면 새 요청을 무시하거나, 기존 코루틴을 중지 후 재시작하는 방식을 택한다.

```csharp
private Coroutine currentShake;

public void OnHit(IUnit attacker, IUnit target, int damage)
{
    if (currentShake != null)
        Facade.Coroutine.StopCoroutine(currentShake);
    currentShake = Facade.Coroutine.StartCoroutine(Shake());
}
```

---

### 3. CameraShake의 `yield return null`이 HitStop 중 멈추는 문제 (중요도: 중간)

**파일**: `CameraShake.cs:36-41`

```csharp
while (elapsed < duration)
{
    cameraTransform.position = origin + (Vector3)(Random.insideUnitCircle * intensity);
    elapsed += Time.unscaledDeltaTime;
    yield return null;                           // ← timeScale=0이면 다음 프레임까지 대기
}
```

`elapsed`에 `Time.unscaledDeltaTime`을 사용하여 시간 계산은 올바르지만, `yield return null`은 Unity의 일반 프레임 루프를 따른다. `timeScale = 0`일 때 `yield return null`은 정상적으로 다음 프레임에 재개되므로 동작에는 문제가 없다.

단, HitStop과 CameraShake가 동시에 트리거되는 시나리오에서 HitStop 동안에도 카메라가 계속 흔들리는 것이 의도된 동작인지 확인이 필요하다. 역경직(화면 정지) 중에 카메라가 흔들리면 시각적으로 어색할 수 있다.

**제안**: 의도된 동작이라면 주석으로 명시하고, 아니라면 HitStop의 `isActive` 상태를 참조하여 셰이크를 일시 정지하는 로직을 추가한다.

---

### 4. HitEffectPlayer에서 스폰된 이펙트의 회수(Despawn) 처리 없음 (중요도: 중간)

**파일**: `HitEffectPlayer.cs:25`

```csharp
public void OnHit(IUnit attacker, IUnit target, int damage)
{
    Facade.Pool.Spawn(effectPrefab).transform.position = target.Transform.position;
    Facade.Sound.PlaySFX(sfxName);
}
```

`Facade.Pool.Spawn`으로 이펙트를 생성하지만, 이펙트 재생이 완료된 후 `Facade.Pool.Despawn`을 호출하는 로직이 없다. 오브젝트 풀에서 꺼낸 오브젝트가 반환되지 않으면 풀의 의미가 퇴색되고, 장시간 플레이 시 활성 오브젝트가 계속 누적된다.

설계 문서(`design.md`)에서도 `Facade.Pool`의 Phase 2 사용처에 "HitEffectPlayer(이펙트 생성/**회수**)"로 명시되어 있어 Despawn이 의도된 동작이다.

**제안**: 이펙트 프리팹에 자동 반환 컴포넌트(ParticleSystem 종료 시 Despawn 콜백 등)를 부착하거나, 코루틴으로 일정 시간 후 Despawn을 호출한다.

```csharp
public void OnHit(IUnit attacker, IUnit target, int damage)
{
    var effect = Facade.Pool.Spawn(effectPrefab);
    effect.transform.position = target.Transform.position;
    Facade.Sound.PlaySFX(sfxName);
    Facade.Coroutine.DoSecondsAfter(() => Facade.Pool.Despawn(effect), effectDuration);
}
```

---

### 5. VFX 클래스들이 `System.IDisposable`을 구현하지 않음 (중요도: 낮음)

**파일**: `HitStop.cs:19-21`, `CameraShake.cs:21-23`, `HitEffectPlayer.cs:18-20`

세 클래스 모두 `Dispose()` 메서드를 갖고 있지만 `System.IDisposable` 인터페이스를 구현하지 않는다. `IDisposable`을 구현하면 `using` 문이나 DI 컨테이너의 자동 해제, 정적 분석 도구의 리소스 누수 감지 등에서 이점이 있다.

**제안**: 현재 프로젝트에서 `Dispose()` 패턴이 관례적으로 사용되는 것이라면 일관성을 위해 그대로 유지해도 무방하다. 다만 향후 리소스 관리 자동화가 필요할 때 `IDisposable` 도입을 고려할 수 있다.

---

### 6. HitEffectPlayer 설계 문서와 Spawn 호출 시그니처 불일치 (중요도: 낮음)

**파일**: `HitEffectPlayer.cs:25`

```csharp
// 설계 문서 (vfx.md:103)
Facade.Pool.Spawn(effectPrefab, target.Transform.position);

// 실제 구현
Facade.Pool.Spawn(effectPrefab).transform.position = target.Transform.position;
```

설계 문서의 호출은 `IObjectPool.Spawn(GameObject, Transform)` 시그니처와 맞지 않는다 (두 번째 인자가 `Vector3`가 아닌 `Transform`). 구현이 실제 인터페이스에 맞게 올바르게 수정되었으므로 코드 자체는 문제없다. 설계 문서의 해당 부분을 실제 구현에 맞게 갱신하면 향후 혼동을 방지할 수 있다.

---

## 종합 평가

| 항목 | 등급 | 설명 |
|------|------|------|
| 설계 일관성 | **A** | 설계 문서와 설계 리뷰 이슈 반영이 충실하며, 인터페이스 시그니처 불일치도 구현에서 올바르게 해결함 |
| 코딩 컨벤션 준수 | **A** | 명명 규칙, 접근 제한자, 코드 스타일 모두 컨벤션 준수 |
| 에러 처리 | **B** | timeScale 복원값 미저장(높음), 이펙트 Despawn 누락(중간) 등 예외 상황 대비 부족 |
| 캡슐화 | **A** | Facade를 통한 간접 접근, Notifier 기반 이벤트 구독 등 캡슐화가 잘 유지됨 |
| 테스트 존재 | - | VFX 시스템은 Unity 런타임 의존(Time.timeScale, Transform 등)이 많아 별도 확인 필요 |

### 우선 보강이 필요한 3가지

1. **HitStop timeScale 복원값 저장** (높음) -- `Time.timeScale`을 0으로 설정하기 전에 이전 값을 저장하고, 복원 시 저장된 값을 사용해야 한다. 슬로우 모션 등 다른 시스템과 timeScale을 공유하는 시나리오에서 버그 가능성이 있다.

2. **CameraShake 코루틴 중첩 방지** (중간) -- 연속 히트 시 여러 Shake 코루틴이 동시 실행되어 카메라 원점 복원이 꼬일 수 있다. `HitStop`처럼 중첩 방지 로직 또는 기존 코루틴 중지 후 재시작 방식을 도입해야 한다.

3. **HitEffectPlayer 이펙트 Despawn 처리** (중간) -- 오브젝트 풀에서 스폰된 이펙트가 반환되지 않아 풀 활용이 불완전하다. 이펙트 수명 관리 로직(자동 반환 또는 타이머 기반 Despawn)을 추가해야 한다.
