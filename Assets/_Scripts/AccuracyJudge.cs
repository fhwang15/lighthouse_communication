using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [모방 판정용] B/C/D 플레이어가 스페이스바를 누른 타이밍을 기록하고,
/// 원본 패턴과 비교해서 오차(ms)를 계산하는 시스템
///
/// 사용법:
/// 1. 빈 GameObject 3개 만들고 (B, C, D용) 이 스크립트 각각 붙여
/// 2. playerIndex: 0=B, 1=C, 2=D 로 설정
/// 3. inputKey: 각 플레이어의 키 설정 (B=Space, C=Z, D=M 등)
/// 4. NotePlayer.OnPlaybackComplete → AccuracyJudge.StartListening() 연결
/// </summary>
public class AccuracyJudge : MonoBehaviour
{
    [Header("플레이어 설정")]
    [SerializeField] public int playerIndex;        // 0=B, 1=C, 2=D
    [SerializeField] private KeyCode inputKey = KeyCode.Space;

    [Header("연결")]
    [SerializeField] private NotePlayer notePlayer; // 원본 패턴 참조용

    [Header("판정 허용 오차")]
    [SerializeField] private float listenWindowExtra = 2.0f; // 마지막 노트 후 여유 시간(초)

    // 결과
    public float AverageErrorMs { get; private set; } = float.MaxValue; // 낮을수록 정확
    public bool IsFinished { get; private set; } = false;

    // 이벤트: 이 플레이어의 판정이 끝났을 때 → JudgeManager가 받아서 처리
    public UnityEvent<int, float> OnJudgeDone; // (playerIndex, averageErrorMs)

    // 내부 상태
    private List<float> myTimestamps = new List<float>();
    private bool isListening = false;
    private float listenStartTime;
    private List<float> referenceTimestamps; // 원본 노트 타이밍

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    /// <summary>모방 입력 수집 시작. NotePlayer.OnPlaybackComplete 에 연결.</summary>
    public void StartListening()
    {
        if (notePlayer == null || notePlayer.CurrentNoteTimestamps == null)
        {
            Debug.LogError($"[Player {playerIndex}] NotePlayer가 연결되지 않았어!");
            return;
        }

        myTimestamps.Clear();
        IsFinished = false;
        AverageErrorMs = float.MaxValue;
        referenceTimestamps = notePlayer.CurrentNoteTimestamps;

        isListening = true;
        listenStartTime = Time.time;

        // 마지막 노트 시각 + 여유 시간 후 자동 마감
        float deadline = referenceTimestamps[referenceTimestamps.Count - 1] + listenWindowExtra;
        StartCoroutine(AutoFinishCoroutine(deadline));

        Debug.Log($"[Player {playerIndex}] 모방 시작! {referenceTimestamps.Count}개 노트 입력해");
    }

    // ───────────────────────────────────────────
    // 내부 로직
    // ───────────────────────────────────────────

    private void Update()
    {
        if (!isListening) return;

        if (Input.GetKeyDown(inputKey))
        {
            float elapsed = Time.time - listenStartTime;
            myTimestamps.Add(elapsed);
            Debug.Log($"[Player {playerIndex}] 노트 입력: {elapsed:F3}초 ({myTimestamps.Count}/{referenceTimestamps.Count})");

            // 원본 노트 수만큼 다 입력하면 즉시 마감
            if (myTimestamps.Count >= referenceTimestamps.Count)
            {
                isListening = false;
                StopAllCoroutines();
                CalculateScore();
            }
        }
    }

    private IEnumerator AutoFinishCoroutine(float deadline)
    {
        yield return new WaitForSeconds(deadline);

        if (isListening) // 아직 덜 입력했으면 강제 마감
        {
            isListening = false;
            Debug.Log($"[Player {playerIndex}] 시간 초과로 마감 ({myTimestamps.Count}개만 입력됨)");
            CalculateScore();
        }
    }

    private void CalculateScore()
    {
        IsFinished = true;

        int refCount = referenceTimestamps.Count;
        int myCount = myTimestamps.Count;

        if (myCount == 0)
        {
            // 아무것도 안 눌렀으면 최악의 점수
            AverageErrorMs = float.MaxValue;
            Debug.Log($"[Player {playerIndex}] 입력 없음 → 탈락");
            OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
            return;
        }

        // ── 오차 계산 방식 ──────────────────────────────
        // 원본 i번째 노트 vs 내가 입력한 i번째 노트의 시간 차이(ms)를 평균냄
        // 입력 개수가 다를 경우, 없는 노트는 패널티(1000ms) 부여
        // ────────────────────────────────────────────────

        float totalError = 0f;

        for (int i = 0; i < refCount; i++)
        {
            if (i < myCount)
            {
                float diffMs = Mathf.Abs(referenceTimestamps[i] - myTimestamps[i]) * 1000f;
                totalError += diffMs;
            }
            else
            {
                totalError += 1000f; // 패널티: 1초(1000ms)
            }
        }

        AverageErrorMs = totalError / refCount;

        Debug.Log($"[Player {playerIndex}] 평균 오차: {AverageErrorMs:F1}ms");
        OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
    }
}
