using System;
using System.Collections.Generic;
using System.Linq;
using Base;
using DG.Tweening;
using UnityEngine;

public abstract class CharacterView : MonoBehaviour
{
    [SerializeField] private UnitMovement movement;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private string moveAnimTrigger = "move";
    [SerializeField] private string idleAnimTrigger = "idle";
    [SerializeField] private string attackAnimTrigger = "attack";
    [SerializeField] private string deadAnimTrigger = "dead";
    [SerializeField] private string moveAnimStateName = "Run";

    private const int QueueSize = 5;
    private readonly Queue<Vector2> directionQueue = new();

    private DeathSequenceData deathSequenceData;
    private CreateSequenceData createSequenceData;

    protected virtual void Awake()
    {
        spriteRenderer.sortingOrder = SortingOrder.Unit;
        deathSequenceData = Facade.DB.Get<DeathSequenceData>("DeathSequence");
        createSequenceData = Facade.DB.Get<CreateSequenceData>("CreateSequence");
    }

    private void OnEnable()
    {
        spriteRenderer.DOKill();
        spriteRenderer.color = Color.white;
        spriteRenderer.flipX = false;
        transform.DOKill();
        transform.localRotation = Quaternion.identity;
        directionQueue.Clear();
        OnSpawnedFromPool();
    }

    /// <summary>풀에서 꺼내질 때(OnEnable) 호출. 서브클래스에서 상태 초기화에 사용한다.</summary>
    protected virtual void OnSpawnedFromPool() { }

    /// <summary>HP 바 뷰를 Health에 바인딩한다. HP 바가 있는 서브클래스에서 오버라이드한다.</summary>
    public virtual void BindHpBar(UnitHealth health) { }

    /// <summary>HP 바를 숨긴다. HP 바가 있는 서브클래스에서 오버라이드한다.</summary>
    protected virtual void HideHpBar() { }

    private void LateUpdate()
    {
        //: 같은 레이어의 오브젝트들은 z축을 이용해 뎁스를 조절.
        //: y를 이용해, y방향으로 낮은 오브젝트가 더 위에 그려지도록 한다.
        var pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, pos.y);
    }

    public UnitMovement Movement => movement;

    /// <summary>
    /// 현재 Animator가 이동(Run) 상태를 재생 중인지 확인한다.
    /// PlayerIdleState 등에서 wasMoving bool 대신 사용.
    /// </summary>
    public bool IsPlayingMoveAnimation() =>
        animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(moveAnimStateName);

    public void PlayDamageFlash()
    {
        spriteRenderer.DOKill();
        spriteRenderer.color = Color.red;
        spriteRenderer.DOColor(Color.white, 0.25f);
    }

    public void PlayIdleAnimation() => SetTriggerSafe(idleAnimTrigger);
    public void PlayMoveAnimation() => SetTriggerSafe(moveAnimTrigger);
    public void PlayAttackAnimation() => SetTriggerSafe(attackAnimTrigger);
    public void PlayDeadAnimation() => SetTriggerSafe(deadAnimTrigger);

    /// <summary>
    /// 사망 연출 시퀀스.
    /// Idle로 전환 + Flip 해제 → Z축 회전 → FadeOut 순서로 재생한다.
    /// 파라미터는 DeathSequenceData("DeathSequence") 에셋에서 조회한다.
    /// </summary>
    public void PlayDeathSequence(Action onComplete = null)
    {
        HideHpBar();
        PlayIdleAnimation();
        spriteRenderer.flipX = false;

        spriteRenderer.DOKill();
        transform.DOKill();

        if (deathSequenceData == null)
        {
            Debug.LogWarning("[CharacterView] DeathSequenceData를 찾을 수 없습니다. Resources/DeathSequence.asset을 확인하세요.");
            onComplete?.Invoke();
            return;
        }

        DOTween.Sequence()
            .Append(transform.DOLocalRotate(
                new Vector3(0f, 0f, deathSequenceData.rotationDegrees),
                deathSequenceData.rotateDuration)
                .SetEase(deathSequenceData.rotateEase))
            .AppendInterval(deathSequenceData.fadeDelay)
            .Append(spriteRenderer.DOFade(0f, deathSequenceData.fadeDuration)
                .SetEase(deathSequenceData.fadeEase))
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 스폰 연출 시퀀스.
    /// 누운 상태(alpha 0, lying 각도)에서 시작 → FadeIn → 일어나는 회전 순서로 재생한다.
    /// 파라미터는 CreateSequenceData("CreateSequence") 에셋에서 조회한다.
    /// </summary>
    public void PlayCreateAnimation(Action onComplete = null)
    {
        if (deathSequenceData == null || createSequenceData == null)
        {
            onComplete?.Invoke();
            return;
        }

        spriteRenderer.DOKill();
        transform.DOKill();

        spriteRenderer.color = Color.clear;
        transform.localRotation = Quaternion.Euler(0f, 0f, deathSequenceData.rotationDegrees);

        DOTween.Sequence()
            .Append(spriteRenderer.DOFade(1f, createSequenceData.fadeInDuration)
                .SetEase(createSequenceData.fadeEase))
            .AppendInterval(createSequenceData.riseDelay)
            .Append(transform.DOLocalRotate(Vector3.zero, createSequenceData.riseDuration)
                .SetEase(createSequenceData.riseEase))
            .OnComplete(() => onComplete?.Invoke());
    }

    private void SetTriggerSafe(string triggerName)
    {
        if (animator == null) return;
#if UNITY_EDITOR
        bool found = false;
        foreach (var p in animator.parameters)
        {
            if (p.name == triggerName)
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            Debug.LogWarning($"[CharacterView] Animator에 '{triggerName}' 트리거가 없습니다. " +
                             $"Inspector에서 트리거 이름을 확인하세요. ({gameObject.name})", gameObject);
            return;
        }
#endif
        animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// 방향 큐를 비우고 즉시 dir 방향으로 flipX를 설정한다.
    /// 공격 상태 진입처럼 순간적인 방향 전환이 필요한 경우 OnEnter에서 호출한다.
    /// </summary>
    public void SetFacingImmediate(Vector2 dir)
    {
        directionQueue.Clear();
        directionQueue.Enqueue(dir);
        if (dir.x < 0) spriteRenderer.flipX = true;
        else if (dir.x > 0) spriteRenderer.flipX = false;
    }

    /// <summary>
    /// 이동 방향에 따라 스프라이트 flipX를 업데이트한다.
    /// 방향 큐로 몇 프레임 평균을 내어 흔들림을 방지한다.
    /// 이동 State의 OnUpdate()에서 매 프레임 호출한다.
    /// </summary>
    public void UpdateFacing(Vector2 dir)
    {
        directionQueue.Enqueue(dir);
        if (directionQueue.Count > QueueSize)
            directionQueue.Dequeue();

        var averageDirection = directionQueue.Aggregate((a, b) => a + b) / directionQueue.Count;

        var xAxisAbs = Mathf.Abs(averageDirection.x);
        var yAxisAbs = Mathf.Abs(averageDirection.y);
        if (xAxisAbs > yAxisAbs)
        {
            if (dir.x < 0) spriteRenderer.flipX = true;
            else if (dir.x > 0) spriteRenderer.flipX = false;
        }
    }

#if UNITY_EDITOR
    private string gizmoLabel;

    public void SetGizmoLabel(string label) => gizmoLabel = label;

    private static GUIStyle gizmoStyle;

    private void OnDrawGizmos()
    {
        if (string.IsNullOrEmpty(gizmoLabel)) return;

        if (gizmoStyle == null)
        {
            gizmoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
            };
            gizmoStyle.normal.textColor = Color.white;
        }

        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, gizmoLabel, gizmoStyle);
    }
#endif
}
