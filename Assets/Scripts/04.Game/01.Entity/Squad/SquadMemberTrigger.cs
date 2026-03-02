public enum SquadMemberTrigger
{
    StartAttack, // 트리거 + 조건 — 공격 범위 내 적 탐지 조건으로도 자동 처리
    StopAttack,  // 트리거 + 조건 — 공격 범위 내 적 없음 조건으로도 자동 처리
    Die,         // 트리거 전용 — Health.OnDeath 이벤트
    Created,     // 트리거 전용 — CreateState 연출 완료 후 Idle로 전환
    Destroy      // 트리거 전용 — DeadState 연출 완료 후 DestroyState로 전환
}
