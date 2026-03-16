using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class JudgeManager : MonoBehaviour
{
    [Header("이벤트")]
    public UnityEvent<int, float> OnWinnerDecided;

    private Dictionary<int, float> results = new Dictionary<int, float>();
    private int expectedResults = 3; // RhythmGameManager가 매 라운드 세팅해줌

    public void ResetForNewRound()
    {
        results.Clear();
    }

    /// <summary>이번 라운드에 참여하는 Lighthouse 수 세팅. RhythmGameManager가 호출.</summary>
    public void SetExpectedResults(int count)
    {
        expectedResults = count;
        Debug.Log($"[JudgeManager] 이번 라운드 판정 인원: {expectedResults}명");
    }

    public void ReceiveResult(int playerIndex, float averageErrorMs)
    {
        results[playerIndex] = averageErrorMs;
        Debug.Log($"[JudgeManager] Judge {playerIndex} 결과: {averageErrorMs:F1}ms ({results.Count}/{expectedResults})");

        if (results.Count >= expectedResults)
            DecideWinner();
    }

    private void DecideWinner()
    {
        int winnerIndex = -1;
        float bestScore = float.MaxValue;

        foreach (var pair in results)
        {
            if (pair.Value < bestScore)
            {
                bestScore = pair.Value;
                winnerIndex = pair.Key;
            }
        }

        Debug.Log($"[JudgeManager] 승자: Judge {winnerIndex} (오차 {bestScore:F1}ms)");
        OnWinnerDecided?.Invoke(winnerIndex, bestScore);
    }
}