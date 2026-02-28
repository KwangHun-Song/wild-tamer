# TRD - FiniteStateMachine

## 핵심 타입
- `StateMachine<TEntity, TEnumTrigger>`
- `State<TEntity, TEnumTrigger>`
- `StateTransition<TEntity, TEnumTrigger>`
- `IStateChangeEvent<TEntity, TEnumTrigger>`

## 설계 포인트
- `SetUp()` 시 `transitionLookup` 딕셔너리 빌드
- 전이 탐색은 현재 상태 기반 리스트 조회로 최적화
- 상태 전환 시 `OnExit -> OnEnter` 및 Notifier 이벤트 발행

## 확장 방식
- 상태 클래스 추가
- 전이 배열 정의
- 트리거 enum 정의

## 제약
- 상태 인스턴스 동일성/해시 규칙을 일관되게 유지해야 함
