# 코드 리뷰 — 2026-03-01

> **범위**: 커밋 `035a529` (Phase 2 코어 시스템 코드 리뷰 문서 추가) 이후 ~ `HEAD`
> **리뷰 대상**: 27개 스크립트 파일 (1 삭제, 1 신규, 25 수정)
> **핵심 주제**: GameLoop 삭제 → InPlayState 오케스트레이션 이전, PlayerView 구현, UnitHealth pure C# 리팩토링, 버그 수정 및 방어 코드 보강

---

## 요약

이번 변경은 크게 네 가지 축으로 구성된다:

1. **GameLoop 삭제 → InPlayState 오케스트레이션 이전**: `GameLoop.cs`(MonoBehaviour)를 삭제하고 `InPlayState`가 `GameController`의 생성·Update·Cleanup 생명주기를 모두 관리하도록 이전했다. 이는 씬 상태 기반 아키텍처를 더 일관성 있게 만드는 올바른 방향이다.

2. **PlayerView 완성**: Animator 연동, Subscribe/Unsubscribe 패턴, OnDestroy 해제가 구현되었다.

3. **UnitHealth → pure C# 리팩토링**: MonoBehaviour 의존성을 완전히 제거하고, Character 기반 클래스에서 Health를 직접 소유하도록 변경했다. MVP 아키텍처 원칙에 부합한다.

4. **버그 수정 및 방어 코드**: UnitMovement.MoveTo 방향 반전 수정, CombatSystem/EntitySpawner 스냅샷 복사로 컬렉션 변경 방지, FogOfWar 성능 개선(SetPixels), Minimap pivot 보정, CameraShake 중복 방지, HitStop timeScale 복원, FlockBehavior 거리 필터링, Squad.Update에 Combat.Tick 추가 등.

**전체 평가**: 아키텍처 방향은 올바르며, 이전 리뷰에서 지적된 많은 이슈가 해결되었다. 그러나 리소스 해제 누락(TamingSystem, Monster 이벤트), MonsterView의 이벤트 구독 해제 미구현, CameraShake와 QuarterViewCamera 간의 position 충돌 등 런타임 안정성에 영향을 줄 수 있는 이슈가 남아있다.

---

## 이슈 목록

### 1. GameController.Cleanup()에서 TamingSystem.Dispose() 미호출 — Notifier 리스너 누수
- **파일**: `Assets/Scripts/04.Game/02.System/Game/GameController.cs:126-132`
- **심각도**: Major
- **설명**: `GameController.Cleanup()`이 Squad/EntitySpawner 이벤트 구독만 해제하고, `TamingSystem.Dispose()`를 호출하지 않는다. `TamingSystem`은 생성자에서 `notifier.Subscribe(this)`를 호출하므로(`TamingSystem.cs:16`), Dispose가 호출되지 않으면 `Notifier` 내부에 `TamingSystem` 참조가 잔류하여 메모리 누수가 발생한다. `CameraShake`와 `HitStop`도 동일한 패턴이지만, 이들은 `GameController`에서 아직 생성되지 않으므로 향후 통합 시 동일하게 고려해야 한다.
- **현재 코드**:
  ```csharp
  public void Cleanup()
  {
      Squad.OnMemberAdded   -= combatSystem.RegisterUnit;
      Squad.OnMemberRemoved -= combatSystem.UnregisterUnit;
      entitySpawner.OnMonsterSpawned   -= combatSystem.RegisterUnit;
      entitySpawner.OnMonsterDespawned -= combatSystem.UnregisterUnit;
  }
  ```
- **제안**:
  ```csharp
  public void Cleanup()
  {
      Squad.OnMemberAdded   -= combatSystem.RegisterUnit;
      Squad.OnMemberRemoved -= combatSystem.UnregisterUnit;
      entitySpawner.OnMonsterSpawned   -= combatSystem.RegisterUnit;
      entitySpawner.OnMonsterDespawned -= combatSystem.UnregisterUnit;
      tamingSystem.Dispose();
  }
  ```

---

### 2. MonsterView.Subscribe()에서 람다로 이벤트 등록 — 해제 불가능
- **파일**: `Assets/Scripts/04.Game/01.Entity/Monster/MonsterView.cs:7`
- **심각도**: Major
- **설명**: `PlayerView`와 `SquadMemberView`는 이번 변경에서 Subscribe/Unsubscribe 패턴으로 올바르게 리팩토링되었으나, `MonsterView`는 여전히 람다를 사용하여 이벤트를 등록한다. 람다는 매번 새 delegate 인스턴스를 생성하므로 `-=`로 해제할 수 없다. 몬스터가 풀에서 디스폰된 후 재사용될 때, 이전 구독이 해제되지 않아 이벤트가 중복 호출되거나 파괴된 오브젝트에 접근할 수 있다.
- **현재 코드**:
  ```csharp
  public void Subscribe(Monster monster)
  {
      monster.OnMoveRequested += direction => Movement.Move(direction);
  }
  ```
- **제안**: `PlayerView`/`SquadMemberView`와 동일한 패턴으로 리팩토링:
  ```csharp
  private Monster subscribedMonster;

  public void Subscribe(Monster monster)
  {
      subscribedMonster = monster;
      monster.OnMoveRequested += OnMoveRequested;
  }

  public void Unsubscribe()
  {
      if (subscribedMonster != null)
      {
          subscribedMonster.OnMoveRequested -= OnMoveRequested;
          subscribedMonster = null;
      }
  }

  private void OnMoveRequested(Vector2 direction) => Movement.Move(direction);

  private void OnDestroy() => Unsubscribe();
  ```

---

### 3. Monster 생성자에서 Health 이벤트에 람다 등록 — 해제 누락
- **파일**: `Assets/Scripts/04.Game/01.Entity/Monster/Monster.cs:21-22`
- **심각도**: Major
- **설명**: Monster 생성자에서 `Health.OnDamaged`와 `Health.OnDeath`에 람다를 등록한다. Monster가 디스폰되어 풀로 돌아갈 때 이 이벤트가 해제되지 않는다. `UnitHealth`가 Character마다 새로 생성(`Character.cs:17`)되므로 풀 재사용 시 새 Monster 인스턴스가 생기긴 하지만, 만약 Health 인스턴스를 재사용하는 방향으로 리팩토링된다면 문제가 된다. 또한 Health가 GC되기 전까지 MonsterView 참조를 잡고 있으므로, MonsterView가 풀에 의해 비활성화된 상태에서 메서드가 호출될 수 있다.
- **현재 코드**:
  ```csharp
  Health.OnDamaged += _ => monsterView.PlayHitEffect();
  Health.OnDeath += monsterView.PlayDeathEffect;
  ```
- **제안**: 명시적 메서드 참조를 사용하고, EntitySpawner.DespawnMonster에서 정리하거나 Monster에 Cleanup 메서드를 추가:
  ```csharp
  // Monster.cs 생성자에서
  Health.OnDamaged += OnDamaged;
  Health.OnDeath += OnDeath;

  private void OnDamaged(int _) => monsterView.PlayHitEffect();
  private void OnDeath() => monsterView.PlayDeathEffect();

  public void Cleanup()
  {
      Health.OnDamaged -= OnDamaged;
      Health.OnDeath -= OnDeath;
  }
  ```

---

### 4. CameraShake의 origin 캡처와 QuarterViewCamera의 LateUpdate 충돌
- **파일**: `Assets/Scripts/04.Game/02.System/VFX/CameraShake.cs:37-46`
- **심각도**: Major
- **설명**: `CameraShake.Shake()` 코루틴은 시작 시 `origin = cameraTransform.position`을 캡처하고, 매 프레임 position을 흔든 뒤 최종적으로 origin으로 복원한다. 그런데 `QuarterViewCamera.LateUpdate()`도 매 프레임 카메라 position을 Lerp으로 갱신한다. 실행 순서에 따라:
  - CameraShake가 `Update` 이후/`LateUpdate` 이전에 position을 변경하면, `QuarterViewCamera.LateUpdate`가 즉시 덮어쓰므로 쉐이크가 보이지 않는다.
  - `yield return null`은 다음 프레임의 Update 후에 재개되므로, origin으로 복원한 직후 다시 Shake가 덮어쓰고, 최종 복원 시 `origin`이 과거 위치라 카메라가 순간이동할 수 있다.
- **현재 코드**:
  ```csharp
  private IEnumerator Shake()
  {
      isShaking = true;
      var elapsed = 0f;
      var origin = cameraTransform.position;
      while (elapsed < duration)
      {
          cameraTransform.position = origin + (Vector3)(Random.insideUnitCircle * intensity);
          elapsed += Time.unscaledDeltaTime;
          yield return null;
      }
      cameraTransform.position = origin;
      isShaking = false;
  }
  ```
- **제안**: CameraShake를 QuarterViewCamera와 통합하거나, offset 기반으로 변경:
  ```csharp
  // QuarterViewCamera에 shakeOffset 프로퍼티 추가
  public Vector3 ShakeOffset { get; set; }

  private void LateUpdate()
  {
      if (target == null) return;
      var desired = target.position + offset + ShakeOffset;
      transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
  }

  // CameraShake에서 position 대신 offset 조작
  private IEnumerator Shake()
  {
      isShaking = true;
      var elapsed = 0f;
      while (elapsed < duration)
      {
          quarterViewCamera.ShakeOffset = (Vector3)(Random.insideUnitCircle * intensity);
          elapsed += Time.unscaledDeltaTime;
          yield return null;
      }
      quarterViewCamera.ShakeOffset = Vector3.zero;
      isShaking = false;
  }
  ```

---

### 5. InPlayState에서 Camera.main 사용 — 성능 및 안전성
- **파일**: `Assets/Scripts/01.Scene/PlayScene/States/InPlayState.cs:33`
- **심각도**: Minor
- **설명**: `Camera.main`은 내부적으로 `FindObjectWithTag("MainCamera")`를 호출하며, Unity 2020 이전 버전에서는 매 호출마다 씬을 검색한다. Unity 2020+에서는 캐싱되지만, 명시적으로 `[SerializeField]`로 주입하는 것이 더 안전하고 의도를 명확히 한다. 또한 `Camera.main`이 null일 때 `GetComponent`가 NullReferenceException을 던질 수 있으나 null-conditional(`?.`)로 방어하고 있어 크래시 위험은 없다.
- **현재 코드**:
  ```csharp
  var quarterViewCamera = Camera.main?.GetComponent<QuarterViewCamera>();
  ```
- **제안**:
  ```csharp
  [SerializeField] private QuarterViewCamera quarterViewCamera;
  ```

---

### 6. PlayerView.OnMoveRequested에서 animator null 체크 누락
- **파일**: `Assets/Scripts/04.Game/01.Entity/Player/PlayerView.cs:27`
- **심각도**: Minor
- **설명**: `animator`가 `[SerializeField]`로 선언되어 있으나, Inspector에서 연결되지 않았을 경우 `animator.SetBool()`이 NullReferenceException을 던진다. 프리팹 설정 실수로 Animator가 빠진 경우 디버깅이 어려워질 수 있다.
- **현재 코드**:
  ```csharp
  private void OnMoveRequested(Vector2 direction)
  {
      animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
      Movement.Move(direction);
  }
  ```
- **제안**:
  ```csharp
  private void OnMoveRequested(Vector2 direction)
  {
      if (animator != null)
          animator.SetBool(IsMoving, direction.sqrMagnitude > 0.01f);
      Movement.Move(direction);
  }
  ```

---

### 7. PlayerView.Subscribe() 중복 호출 방어 누락
- **파일**: `Assets/Scripts/04.Game/01.Entity/Player/PlayerView.cs:10-14`
- **심각도**: Minor
- **설명**: `Subscribe()`가 여러 번 호출되면 이전 구독이 해제되지 않은 채 새 구독이 추가된다. `SquadMemberView`도 동일한 이슈가 있다. 현재 코드 흐름에서는 한 번만 호출되지만, 방어적 코딩 관점에서 기존 구독을 먼저 해제하는 것이 안전하다.
- **현재 코드**:
  ```csharp
  public void Subscribe(Player player)
  {
      subscribedPlayer = player;
      player.OnMoveRequested += OnMoveRequested;
  }
  ```
- **제안**:
  ```csharp
  public void Subscribe(Player player)
  {
      Unsubscribe(); // 기존 구독 해제
      subscribedPlayer = player;
      player.OnMoveRequested += OnMoveRequested;
  }
  ```

---

### 8. GameController.Update() 장애물 충돌 체크 — 매직 넘버 0.5f
- **파일**: `Assets/Scripts/04.Game/02.System/Game/GameController.cs:70-73`
- **심각도**: Minor
- **설명**: 축별 장애물 충돌 체크에서 0.5f 오프셋이 매직 넘버로 사용된다. 이 값은 유닛의 반경이나 그리드 셀 크기의 절반을 의미하는 것으로 보이지만, 의미를 알기 어렵고 cellSize가 변경되면 동기화 문제가 생긴다.
- **현재 코드**:
  ```csharp
  var resolvedDir = new Vector2(
      obstacleGrid.IsWalkable(new Vector2(pos.x + rawDir.x * 0.5f, pos.y)) ? rawDir.x : 0f,
      obstacleGrid.IsWalkable(new Vector2(pos.x, pos.y + rawDir.y * 0.5f)) ? rawDir.y : 0f
  );
  ```
- **제안**: 상수로 추출하여 의미를 명확히 한다:
  ```csharp
  private const float CollisionProbeDistance = 0.5f;

  // Update() 내
  var resolvedDir = new Vector2(
      obstacleGrid.IsWalkable(new Vector2(pos.x + rawDir.x * CollisionProbeDistance, pos.y)) ? rawDir.x : 0f,
      obstacleGrid.IsWalkable(new Vector2(pos.x, pos.y + rawDir.y * CollisionProbeDistance)) ? rawDir.y : 0f
  );
  ```

---

### 9. HitStop과 CameraShake가 동시 발생 시 TimeScale 간섭
- **파일**: `Assets/Scripts/04.Game/02.System/VFX/HitStop.cs:37-40`, `Assets/Scripts/04.Game/02.System/VFX/CameraShake.cs:42`
- **심각도**: Minor
- **설명**: `HitStop`이 `Time.timeScale = 0`으로 설정한 상태에서 `CameraShake`가 `Time.unscaledDeltaTime`을 사용하므로, 쉐이크 자체는 동작한다. 그러나 `CameraShake.Shake()`에서 `Facade.Coroutine.StartCoroutine`을 통해 코루틴이 실행되는데, 코루틴의 `yield return null`은 `Time.timeScale = 0`이면 다음 프레임에서 재개되긴 한다. 문제는 HitStop이 `previousTimeScale`을 캡처한 후 CameraShake가 시작되면, CameraShake 종료 후 카메라 position이 origin으로 복원되어야 하는데 `timeScale = 0` 동안 Lerp이 동작하지 않아 카메라가 잠시 이상한 위치에 고정될 수 있다는 점이다. 현재 규모에서는 체감하기 어렵지만 VFX가 복잡해지면 문제가 될 수 있다.
- **제안**: VFX 시스템을 통합하여 timeScale과 카메라 position 조작을 하나의 매니저가 조율하도록 향후 리팩토링을 고려할 것.

---

### 10. InPlayState.OnExecuteAsync()에서 playPage null 시 조용한 종료
- **파일**: `Assets/Scripts/01.Scene/PlayScene/States/InPlayState.cs:23-27`
- **심각도**: Minor
- **설명**: `playPage == null`이면 Warning 로그만 남기고 `return`한다. 이렇게 되면 `OnExecuteAsync()`가 완료되어 상태 머신이 다음 상태로 전이하거나 종료할 수 있다. 게임 플레이 상태에서 PlayPage 없이 진행되면 사용자는 빈 화면을 보게 된다. 이것이 의도된 동작인지, 아니면 에러 상태로 처리해야 하는지 확인이 필요하다.
- **현재 코드**:
  ```csharp
  if (playPage == null)
  {
      Facade.Logger?.Log("[InPlayState] PlayPage를 찾을 수 없습니다.", LogLevel.Warning);
      return;
  }
  ```
- **제안**: 복구 불가능한 상황이라면 Error 레벨로 로그하고, 이전 상태로 복귀하거나 에러 화면을 표시하는 것을 고려:
  ```csharp
  if (playPage == null)
  {
      Facade.Logger?.Log("[InPlayState] PlayPage를 찾을 수 없습니다. 게임 진행 불가.", LogLevel.Error);
      // 에러 처리 (예: 타이틀 화면으로 복귀)
      return;
  }
  ```

---

### 11. PlayPage.ShowAsync()에서 settingButton null 체크 누락
- **파일**: `Assets/Scripts/02.Page/PlayPage.cs:23`
- **심각도**: Minor
- **설명**: `settingButton`이 `[SerializeField]`로 선언되어 있으나, Inspector에서 연결되지 않은 경우 `onClick.AddListener`가 NullReferenceException을 던진다. Page가 표시되는 핵심 경로에서 발생하므로 UI 전체가 중단될 수 있다.
- **현재 코드**:
  ```csharp
  public override UniTask ShowAsync(object param = null)
  {
      settingButton.onClick.AddListener(OnSettingButtonClicked);
      return base.ShowAsync(param);
  }
  ```
- **제안**:
  ```csharp
  public override UniTask ShowAsync(object param = null)
  {
      if (settingButton != null)
          settingButton.onClick.AddListener(OnSettingButtonClicked);
      return base.ShowAsync(param);
  }
  ```

---

### 12. EntitySpawner.Update()에서 매 프레임 List 할당 — GC 압박
- **파일**: `Assets/Scripts/04.Game/02.System/Entity/EntitySpawner.cs:55`
- **심각도**: Minor
- **설명**: `new List<Monster>(activeMonsters)`가 매 프레임 호출되어 GC 할당이 발생한다. CombatSystem은 `ToArray()`를 사용하는데, 이도 매 프레임 할당이다. 몬스터가 적을 때는 문제 없지만, 수십~수백 마리가 될 경우 GC spike가 발생할 수 있다.
- **현재 코드**:
  ```csharp
  var snapshot = new List<Monster>(activeMonsters);
  foreach (var monster in snapshot)
  ```
- **제안**: 재사용 가능한 버퍼를 필드로 선언:
  ```csharp
  private readonly List<Monster> updateBuffer = new();

  public void Update(float deltaTime)
  {
      updateBuffer.Clear();
      updateBuffer.AddRange(activeMonsters);
      foreach (var monster in updateBuffer)
      {
          monster.Combat.Tick(deltaTime);
          monster.Update();
      }
  }
  ```

---

### 13. GameController 주석이 삭제된 GameLoop을 참조
- **파일**: `Assets/Scripts/04.Game/02.System/Game/GameController.cs:60`, `Assets/Scripts/04.Game/02.System/Game/GameController.cs:125`
- **심각도**: Minor
- **설명**: XML 주석에 "GameLoop(MonoBehaviour)에서 매 프레임 호출한다", "GameLoop.OnDestroy()에서 호출" 이라고 되어 있으나, GameLoop.cs는 삭제되었고 실제로는 InPlayState에서 호출한다. 오래된 주석은 혼란을 유발한다.
- **현재 코드**:
  ```csharp
  /// <summary>GameLoop(MonoBehaviour)에서 매 프레임 호출한다.</summary>
  public void Update()

  /// <summary>GameLoop.OnDestroy()에서 호출. 이벤트 구독을 해제하여 메모리 누수를 방지한다.</summary>
  public void Cleanup()
  ```
- **제안**:
  ```csharp
  /// <summary>InPlayState.Update()에서 매 프레임 호출한다.</summary>
  public void Update()

  /// <summary>InPlayState 종료 시 호출. 이벤트 구독을 해제하여 메모리 누수를 방지한다.</summary>
  public void Cleanup()
  ```

---

## 긍정적 변경사항

1. **GameLoop 제거와 InPlayState 통합**: GameLoop MonoBehaviour를 제거하고 InPlayState에서 GameController의 전체 생명주기를 관리하도록 한 것은 상태 기반 아키텍처의 일관성을 크게 향상시켰다. `try-finally`로 Cleanup을 보장한 점도 우수하다.

2. **UnitHealth pure C# 리팩토링 완성**: Character 기반 클래스에서 Health를 직접 생성하고, CharacterView에서 Health 참조를 완전히 제거한 것은 MVP 패턴의 원칙(Model에 로직, View는 표현만)에 부합하는 깔끔한 변경이다.

3. **PlayerView/SquadMemberView Subscribe 패턴 개선**: 람다 대신 명시적 메서드 참조를 사용하고, Unsubscribe + OnDestroy 해제를 추가한 것은 이벤트 누수를 방지하는 올바른 패턴이다.

4. **CombatSystem/EntitySpawner 스냅샷 복사**: 순회 중 컬렉션 변경(테이밍에 의한 RegisterUnit 호출 등)으로 인한 `InvalidOperationException`을 방지한 것은 중요한 런타임 안정성 개선이다.

5. **UnitMovement.MoveTo() 방향 버그 수정**: `(Vector2)transform.position - target`을 `target - (Vector2)transform.position`으로 수정하여 올바른 방향으로 이동하도록 고쳤다.

6. **UnitCombat.Tick() 무한 증가 방지**: `elapsed`가 `cooldown`을 초과하면 더 이상 증가하지 않도록 하여 float 오버플로우 가능성을 제거했다.

7. **FogOfWar 성능 개선**: 개별 `SetPixel` 호출을 `Color[] + SetPixels`로 일괄 처리하고, 루프 순서를 y-outer/x-inner로 변경하여 메모리 접근 지역성을 개선했다.

8. **MapGenerator 장애물 판정 개선**: ground 타일맵 기준으로 맵 크기를 결정하고, water 타일 및 ground 없는 영역도 통행 불가로 처리하는 것은 더 정확한 맵 표현이다.

9. **Minimap pivot 보정**: `WorldToMinimap`에서 RectTransform의 pivot을 고려하여 아이콘 위치를 계산하도록 수정했다.

10. **CameraShake/HitStop 중복 실행 방지**: `isShaking`/`isActive` 플래그를 추가하고, HitStop에서 `previousTimeScale`을 캡처하여 timeScale 복원을 안전하게 처리했다.

11. **FlockBehavior 거리 필터링**: 실제 거리 계산을 추가하여 `NeighborRadius` 내의 이웃만 고려하도록 개선했다.

12. **Squad.Update()에 Combat.Tick 추가**: 부대원의 공격 쿨다운이 갱신되지 않던 버그를 수정했다.

13. **WorldMap 컴포넌트 도입**: MapGenerator와 PlayerSpawn을 하나의 프리팹 루트로 묶어 PlayPage에서 깔끔하게 접근할 수 있게 했다.

14. **QuarterViewCamera null 안전성**: Target이 null일 때 LateUpdate를 조기 종료하는 방어 코드를 추가했다.

15. **FogOfWar.RestoreFogGrid() 크기 검증**: 그리드 크기가 불일치하면 복원을 건너뛰도록 방어 코드를 추가했다.

16. **Minimap.AdjustIconPool() prefab null 방어**: prefab이 null이면 null을 pool에 추가하는 대신 즉시 return하도록 수정했다.

---

## 통계 요약

| 심각도 | 건수 |
|--------|------|
| Critical | 0 |
| Major | 4 |
| Minor | 9 |
| **합계** | **13** |

---

## 체크리스트

- [ ] GameController.Cleanup()에 tamingSystem.Dispose() 호출 추가 (#1)
- [ ] MonsterView에 Subscribe/Unsubscribe/OnDestroy 패턴 적용 (#2)
- [ ] Monster 생성자의 Health 이벤트를 명시적 메서드 참조로 변경 및 정리 추가 (#3)
- [ ] CameraShake를 offset 기반으로 리팩토링하여 QuarterViewCamera와의 충돌 해결 (#4)
- [ ] InPlayState에서 Camera.main 대신 SerializeField 주입 고려 (#5)
- [ ] PlayerView.OnMoveRequested에 animator null 체크 추가 (#6)
- [ ] PlayerView/SquadMemberView.Subscribe()에 중복 호출 방어 추가 (#7)
- [ ] GameController 장애물 체크 매직 넘버를 상수로 추출 (#8)
- [ ] VFX 시스템 간 timeScale 간섭 문제 향후 리팩토링 계획 (#9)
- [ ] InPlayState playPage null 시 에러 처리 강화 (#10)
- [ ] PlayPage.ShowAsync()에 settingButton null 체크 추가 (#11)
- [ ] EntitySpawner/CombatSystem 매 프레임 스냅샷 할당 최적화 (#12)
- [ ] GameController 주석을 InPlayState 기준으로 갱신 (#13)
