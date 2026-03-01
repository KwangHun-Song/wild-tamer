using UnityEngine;

/// <summary>
/// WorldMap 프리팹의 루트 컴포넌트.
/// MapGenerator에 대한 직렬화 참조를 외부에 제공한다.
/// </summary>
public class WorldMap : MonoBehaviour
{
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private Transform    playerSpawn;
    [SerializeField] private Transform    unitRoot;

    public MapGenerator MapGenerator => mapGenerator;
    public Transform    PlayerSpawn  => playerSpawn;
    public Transform    UnitRoot     => unitRoot;
}
