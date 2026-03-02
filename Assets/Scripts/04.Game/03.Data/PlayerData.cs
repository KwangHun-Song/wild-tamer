using UnityEngine;

/// <summary>
/// 플레이어 전투/이동 파라미터를 저장하는 ScriptableObject.
/// DefaultDatabase가 Resources 폴더에서 "PlayerData" 이름으로 조회한다.
/// 예) Facade.DB.Get&lt;PlayerData&gt;("PlayerData")
/// </summary>
[CreateAssetMenu(menuName = "Data/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("최대 HP")]
    public int maxHp = 100;

    [Header("공격 데미지 — 적에게 가하는 피해량")]
    public int attackDamage = 10;

    [Header("공격 범위 — 이 거리 이내의 적을 공격한다")]
    public float attackRange = 1.5f;

    [Header("공격 쿨다운 — 공격 간격 (초)")]
    public float attackCooldown = 1f;

    [Header("이동 속도")]
    public float moveSpeed = 3f;

    [Header("물리 반경 — 유닛 간 겹침 방지에 사용하는 크기 (단위: 월드)")]
    public float radius = 0.3f;
}
