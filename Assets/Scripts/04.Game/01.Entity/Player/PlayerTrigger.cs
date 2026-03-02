public enum PlayerTrigger
{
    StartAttack, // 트리거 전용 — OnAttackFired 이벤트 (폴링 불가, 이벤트만 감지 가능)
    StopAttack,  // 트리거 + 조건 — Combat.CanAttack 조건으로도 자동 처리
    Die          // 트리거 전용 — Health.OnDeath 이벤트
}
