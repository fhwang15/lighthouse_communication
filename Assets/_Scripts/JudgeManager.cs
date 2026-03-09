using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// B/C/D 세 명의 판정 결과를 모아서
/// "가장 정확한 플레이어"를 결정하고 배를 이동시키는 매니저
///
/// 사용법:
/// 1. 빈 GameObject에 이 스크립트 붙여
/// 2. judges 배열에 AccuracyJudge 3개 연결 (B, C, D 순서)
/// 3. AccuracyJudge.OnJudgeDone → JudgeManager.ReceiveResult() 연결
/// 4. OnWinnerDecided 이벤트에 배 이동 로직 연결
/// </summary>
public class JudgeManager : MonoBehaviour
{
    [Header("판정기 연결 (B, C, D 순서)")]
    [SerializeField] private AccuracyJudge[] judges; // 반드시 3개

    [Header("이벤트")]
    // 승자 인덱스(0=B, 1=C, 2=D)와 오차값 전달
    public UnityEvent<int, float> OnWinnerDecided;

    // 내부: 결과 수집
    private Dictionary<int, float> results = new Dictionary<int, float>();
    private int expectedResults = 3;

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    /// <summary>라운드 시작 시 초기화. GameManager에서 호출.</summary>
    public void ResetForNewRound()
    {
        results.Clear();
    }

    /// <summary>
    /// AccuracyJudge.OnJudgeDone 에 연결.
    /// 한 플레이어의 판정이 끝날 때마다 자동으로 호출됨.
    /// </summary>
    public void ReceiveResult(int playerIndex, float averageErrorMs)
    {
        results[playerIndex] = averageErrorMs;
        Debug.Log($"[JudgeManager] Player {playerIndex} 결과 수신: {averageErrorMs:F1}ms ({results.Count}/{expectedResults})");

        // 3명 모두 결과가 들어오면 최종 판정
        if (results.Count >= expectedResults)
        {
            DecideWinner();
        }
    }

    // ───────────────────────────────────────────
    // 내부 로직
    // ───────────────────────────────────────────

    private void DecideWinner()
    {
        int winnerIndex = -1;
        float bestScore = float.MaxValue;

        // 가장 오차가 작은 플레이어 찾기
        foreach (var pair in results)
        {
            Debug.Log($"  Player {pair.Key}: {pair.Value:F1}ms");

            if (pair.Value < bestScore)
            {
                bestScore = pair.Value;
                winnerIndex = pair.Key;
            }
        }

        string[] names = { "B", "C", "D" };
        Debug.Log($"[승자] Player {names[winnerIndex]}! (오차 {bestScore:F1}ms) → 배가 이쪽으로 이동!");

        OnWinnerDecided?.Invoke(winnerIndex, bestScore);
    }
}
