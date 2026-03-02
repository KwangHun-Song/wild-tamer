using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;

/// <summary>
/// 보스 전용 뷰. 존 인디케이터, P2 돌진 코루틴, P6 투사체 발사 코루틴을 담당한다.
/// </summary>
public class BossMonsterView : MonsterView
{
    [Header("존 인디케이터")]
    [SerializeField] private ZoneIndicatorView circleIndicator;
    [SerializeField] private ZoneIndicatorView crossIndicator;
    [SerializeField] private ZoneIndicatorView xIndicator;
    [SerializeField] private ZoneIndicatorView lineIndicator;

    [Header("P6 투사체")]
    [SerializeField] private GameObject projectilePrefab;

    private Action chargeCompleteCallback;
    private bool   isCharging;
    private float  originalMoveSpeed;

    // ── 인디케이터 API ──────────────────────────────────────────────

    /// <summary>Warning 시작 시 인디케이터를 표시한다.</summary>
    public void ShowIndicator(BossPatternType type, Vector2 origin, BossPatternData data)
    {
        if (type == BossPatternType.Charge)
        {
            // Charge는 OnWarningTick에서 UpdateChargeIndicator로 갱신하므로 여기서는 초기 표시만
            lineIndicator?.Show(origin, data.chargeWidth, data.chargeDistance);
            return;
        }

        var ind = GetZoneIndicator(type);
        if (ind == null) return;
        float size = data.range * 2f;
        ind.Show(origin, size, size);
    }

    /// <summary>Warning 동안 원형/점 인디케이터 위치를 갱신한다 (P1/P3/P4/P5/P6).</summary>
    public void UpdateIndicatorPosition(BossPatternType type, Vector2 worldPos)
        => GetZoneIndicator(type)?.UpdatePosition(worldPos);

    /// <summary>Warning 동안 돌진 방향 인디케이터를 갱신한다 (P2).</summary>
    public void UpdateChargeIndicator(Vector2 dir, float distance, float width)
    {
        if (lineIndicator == null) return;
        var   pos   = (Vector2)transform.position + dir * (distance * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        lineIndicator.Show(pos, width, distance);
        lineIndicator.transform.SetPositionAndRotation(
            new Vector3(pos.x, pos.y, pos.y),
            Quaternion.Euler(0f, 0f, angle));
    }

    /// <summary>Active 진입 시 인디케이터를 붉게 번쩍인다.</summary>
    public void FlashIndicator(BossPatternType type) => GetZoneIndicator(type)?.FlashActive();

    /// <summary>특정 타입 인디케이터를 숨긴다.</summary>
    public void HideIndicator(BossPatternType type) => GetZoneIndicator(type)?.Hide();

    /// <summary>모든 인디케이터를 숨긴다 (CastState.OnExit).</summary>
    public void HideAllIndicators()
    {
        circleIndicator?.Hide();
        crossIndicator?.Hide();
        xIndicator?.Hide();
        lineIndicator?.Hide();
    }

    private ZoneIndicatorView GetZoneIndicator(BossPatternType type) => type switch
    {
        BossPatternType.TrackingZone      => circleIndicator,
        BossPatternType.CurseMark         => circleIndicator,
        BossPatternType.CrossZone         => crossIndicator,
        BossPatternType.XZone             => xIndicator,
        BossPatternType.Charge            => lineIndicator,
        BossPatternType.ProjectileBarrage => circleIndicator,
        _                                 => null,
    };

    // ── P2 돌진 ─────────────────────────────────────────────────────

    public void RegisterChargeComplete(Action onComplete) => chargeCompleteCallback = onComplete;

    public void StartCharge(Vector2 direction, BossPatternData data, Action<List<IUnit>> onHit)
    {
        if (isCharging) return;
        StartCoroutine(ChargeRoutine(direction, data, onHit));
    }

    private IEnumerator ChargeRoutine(Vector2 dir, BossPatternData data, Action<List<IUnit>> onHit)
    {
        isCharging      = true;
        originalMoveSpeed  = Movement.MoveSpeed;
        Movement.MoveSpeed = data.chargeSpeed;

        float elapsed  = 0f;
        float duration = data.chargeDistance / data.chargeSpeed;
        var   hitUnits = new List<IUnit>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Movement.Move(dir);
            yield return null;
        }

        Movement.Move(Vector2.zero);
        Movement.MoveSpeed = originalMoveSpeed;
        isCharging = false;

        onHit?.Invoke(hitUnits);
        chargeCompleteCallback?.Invoke();
        chargeCompleteCallback = null;
    }

    // ── P6 투사체 발사 ───────────────────────────────────────────────

    public void FireProjectiles(IUnit owner, Vector2 baseDir, BossPatternData data, Notifier notifier)
    {
        StartCoroutine(FireRoutine(owner, baseDir, data, notifier));
    }

    private IEnumerator FireRoutine(IUnit owner, Vector2 baseDir, BossPatternData data, Notifier notifier)
    {
        float halfSpread = data.spreadAngle * 0.5f;
        float step       = data.projectileCount > 1
            ? data.spreadAngle / (data.projectileCount - 1)
            : 0f;

        for (int i = 0; i < data.projectileCount; i++)
        {
            float angle = -halfSpread + step * i;
            var   dir   = (Vector2)(Quaternion.Euler(0f, 0f, angle) * (Vector3)baseDir);

            var go   = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            var proj = go.GetComponent<BossProjectile>();
            proj.Initialize(owner, dir, data, notifier);

            if (i < data.projectileCount - 1)
                yield return new WaitForSeconds(data.fireInterval);
        }
    }

    // ── 사망 연출 ────────────────────────────────────────────────────

    /// <summary>
    /// 기반 클래스 PlayDeathSequence를 재정의.
    /// 인디케이터 숨김 + 코루틴 정지 후 DOTween 사망 연출 재생.
    /// </summary>
    public new void PlayDeathSequence(Action onComplete = null)
    {
        HideAllIndicators();
        StopAllCoroutines();
        base.PlayDeathSequence(onComplete);
    }
}
