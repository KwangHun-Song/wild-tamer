using UnityEngine;
using Base;

/// <summary>
/// 보스 패턴 인터페이스.
/// OnWarningTick은 Warning 단계 매 프레임 호출 (추적이 필요한 패턴만 구현, 기본 no-op).
/// Activate는 Active 단계 진입 시 데미지 판정 + 인디케이터 확정.
/// </summary>
public interface IBossPattern
{
    /// <summary>Warning 단계 매 프레임 — 위치 추적이 필요한 패턴만 구현.</summary>
    void OnWarningTick(BossMonster boss, BossPatternData data, ref Vector2 lockedTarget) { }

    /// <summary>Active 단계 진입 — 인디케이터 확정 + 데미지 판정 시작.</summary>
    void Activate(BossMonster boss, BossPatternData data, Vector2 lockedTarget,
                  SpatialGrid<IUnit> unitGrid, Notifier notifier, BossMonsterView view);
}
