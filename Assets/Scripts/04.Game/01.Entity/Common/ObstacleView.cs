using UnityEngine;

/// <summary>
/// 이동하지 않는 장애물(나무, 바위, 건물 등)의 뎁스 정렬 컴포넌트.
/// </summary>
public class ObstacleView : MonoBehaviour
{
    private void Start()
    {
        //: 같은 레이어의 오브젝트들은 z축을 이용해 뎁스를 조절.
        //: y를 이용해, y방향으로 낮은 오브젝트가 더 위에 그려지도록 한다.
        //: 장애물은 이동하지 않으므로 생성 시 한 번만 설정한다.
        var pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, pos.y);
    }
}
