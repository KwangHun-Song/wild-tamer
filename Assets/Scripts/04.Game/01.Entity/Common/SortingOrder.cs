/// <summary>
/// 스프라이트 렌더러의 Order in Layer 기준값.
/// 레이어 간 간격은 1000으로 유닛 간 Y-소팅 오프셋이 ±999 범위를 넘지 않으면 안전하다.
/// </summary>
public static class SortingOrder
{
    public const int Water = 0;
    public const int Ground = 1000;
    public const int Obstacle = 2000;
    public const int Unit = 2000; // Obstacle과 동일 레이어 — z=y로 Y-소팅
    public const int Fog  = 3000; // 모든 지형·유닛 위에 렌더링
}
