using UnityEngine;

/// <summary>
/// 테이밍 확률 파라미터를 저장하는 ScriptableObject.
/// Facade.DB.Get&lt;TamingData&gt;("TamingData")로 조회한다.
/// </summary>
[CreateAssetMenu(menuName = "Data/TamingData")]
public class TamingData : ScriptableObject
{
    [Header("테이밍 성공 확률 (0~1)")]
    public float tamingChance = 0.3f;
}
