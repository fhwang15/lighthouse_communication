using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 페이즈 전환 시 중앙 UI 텍스트로 카운트다운 + 안내 표시
/// 
/// 사용법:
/// 1. Canvas 안에 TMP 텍스트 오브젝트 만들기 (중앙 배치)
/// 2. 이 스크립트를 아무 오브젝트에 붙이고 phaseText 연결
/// 3. RhythmGameManager에서 ShowPhase() 호출
/// </summary>
public class PhaseUIManager : MonoBehaviour
{
    public static PhaseUIManager Instance;

    [Header("UI 연결")]
    [SerializeField] private TextMeshProUGUI phaseText;

    [Header("카운트다운 설정")]
    [SerializeField] private float countdownInterval = 1.0f; // 숫자 간격 (초)
    [SerializeField] private float messageHoldTime = 0.8f;   // GO!/WATCH! 등 표시 시간

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (phaseText != null)
            phaseText.gameObject.SetActive(false);
    }

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    /// <summary>
    /// 카운트다운 후 페이즈 시작 메시지 표시
    /// RhythmGameManager에서 yield return StartCoroutine(ShowPhase(...)) 로 호출
    /// </summary>
    public IEnumerator ShowPhase(string phaseMessage, int countFrom = 3)
    {
        if (phaseText == null) yield break;

        phaseText.gameObject.SetActive(true);

        // 3, 2, 1 카운트다운
        for (int i = countFrom; i >= 1; i--)
        {
            phaseText.text = i.ToString();
            yield return new WaitForSeconds(countdownInterval);
        }

        // 페이즈 메시지 (GO!, WATCH!, COPY! 등)
        phaseText.text = phaseMessage;
        yield return new WaitForSeconds(messageHoldTime);

        // 숨기기
        phaseText.gameObject.SetActive(false);
    }

    /// <summary>카운트다운 없이 메시지만 표시 (이동 페이즈 결과 등)</summary>
    public IEnumerator ShowMessage(string message, float duration = 1.5f)
    {
        if (phaseText == null) yield break;

        phaseText.gameObject.SetActive(true);
        phaseText.text = message;

        yield return new WaitForSeconds(duration);

        phaseText.gameObject.SetActive(false);
    }

    /// <summary>즉시 숨기기</summary>
    public void Hide()
    {
        if (phaseText != null)
            phaseText.gameObject.SetActive(false);
    }
}