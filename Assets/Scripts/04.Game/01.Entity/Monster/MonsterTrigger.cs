public enum MonsterTrigger
{
    DetectEnemy,     // 트리거 + 조건 — 탐지 범위 내 적 존재 조건으로도 자동 처리
    LoseEnemy,       // 트리거 + 조건 — 탐지 범위 내 적 없음 조건으로도 자동 처리
    InAttackRange,   // 트리거 + 조건 — 공격 범위 내 적 존재 조건으로도 자동 처리
    OutOfAttackRange,// 트리거 + 조건 — 공격 범위 내 적 없음 조건으로도 자동 처리
    Die              // 트리거 전용 — Health.OnDeath 이벤트
}
