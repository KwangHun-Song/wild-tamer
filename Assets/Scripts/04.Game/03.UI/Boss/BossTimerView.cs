using TMPro;
using UnityEngine;

/// <summary>
/// 보스 등장까지 남은 시간을 화면 상단 중앙에 표시하는 UI.
/// BossSpawnSystem이 매 프레임 SetTime을 호출하며, 타이머가 0이 되면 Hide한다.
/// </summary>
public class BossTimerView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    public void SetTime(float remainingSeconds)
    {
        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
        timerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
