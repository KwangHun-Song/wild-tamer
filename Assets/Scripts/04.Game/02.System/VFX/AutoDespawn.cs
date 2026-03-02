using System.Collections;
using Base;
using UnityEngine;

/// <summary>
/// 오브젝트 풀에서 꺼낸 VFX 오브젝트를 자동으로 반환한다.
/// ParticleSystem이 있으면 재생 완료 후, 없으면 duration 초 후 Facade.Pool.Despawn()을 호출한다.
/// 이펙트 프리팹 루트에 붙여 사용한다.
/// </summary>
public class AutoDespawn : MonoBehaviour
{
    [SerializeField] private float duration = 2f;

    private ParticleSystem particle;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        if (particle != null)
            StartCoroutine(WaitForParticle());
        else
            Invoke(nameof(Despawn), duration);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        CancelInvoke(nameof(Despawn));
    }

    private IEnumerator WaitForParticle()
    {
        yield return new WaitUntil(() => !particle.IsAlive(withChildren: true));
        Despawn();
    }

    private void Despawn()
    {
        Facade.Pool.Despawn(gameObject);
    }
}
