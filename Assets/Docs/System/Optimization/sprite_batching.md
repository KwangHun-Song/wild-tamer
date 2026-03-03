# 스프라이트 배칭 최적화

## 문제

모든 유닛과 오브젝트가 개별 배치(SetPass Call)를 생성하고 있음.
원인: Y-소팅을 위해 `transform.position.z = y`로 Z값을 개별 설정하여 동일 머티리얼/텍스처임에도 배칭이 깨짐.

### 기존 방식 (Z=Y 수동 설정)
- `CharacterView.LateUpdate()`: 매 프레임 `pos.z = pos.y`
- `ObstacleView.Start()`: 생성 시 `pos.z = pos.y`
- `MapGenerator`: waterBackground `pos.z = pos.y`
- `MapDecorationGenerator`: 나무/바위/덤불 배치 시 `pos.z = pos.y`
- `BossMonsterView`: lineIndicator `pos.z = pos.y`

결과: 오브젝트마다 Z값이 다르므로 SRP Batcher/동적 배칭 불가 → Batches ≈ 유닛 수

## 해결: Camera Custom Sort Axis

Unity 카메라의 `TransparencySortMode.CustomAxis`를 `{0, 1, 0}` (Y축)으로 설정.

### 원리
- 카메라가 스프라이트의 Y좌표를 기준으로 렌더 순서를 결정
- 모든 오브젝트의 Z값을 0으로 통일 가능
- Z값이 동일하므로 동일 머티리얼/텍스처의 스프라이트들이 배칭됨
- `SortingOrder` 상수로 레이어 간 우선순위 유지 (Water=0, Ground=1000, Obstacle/Unit=2000, Fog=3000)

### 변경 사항

| 파일 | 변경 내용 |
|------|-----------|
| `QuarterViewCamera.cs` | `Awake()`에서 `transparencySortMode = CustomAxis`, `transparencySortAxis = {0,1,0}` 설정 |
| `CharacterView.cs` | `LateUpdate()` Z=Y 코드 제거 |
| `ObstacleView.cs` | `Start()` Z=Y 코드 제거 → 빈 클래스 (향후 확장용 유지) |
| `MapGenerator.cs` | waterBackground Z=Y → Z=0 |
| `MapDecorationGenerator.cs` | 나무/바위/덤불 Z=Y → Z=0, 바위/덤불 sortingOrder → `SortingOrder.Obstacle` 고정값 |
| `BossMonsterView.cs` | lineIndicator Z=Y → Z=0 |
| `SortingOrder.cs` | 주석 업데이트 (z=y 방식 → Camera CustomAxis 방식) |

### 주의 사항
- 에디터에서 MapDecorationGenerator로 장식을 **재생성** 해야 기존 Z=Y 좌표가 Z=0으로 갱신됨
- 씬에 수작업으로 배치한 오브젝트 중 Z≠0인 것이 있으면 수동 수정 필요
