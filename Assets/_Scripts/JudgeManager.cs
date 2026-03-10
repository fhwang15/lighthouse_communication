using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class JudgeManager : MonoBehaviour
{
    [Header("판정기 연결 (B, C, D 순서)")]
    [SerializeField] private AccuracyJudge[] judges; 

    [Header("이벤트")]
    public UnityEvent<int, float> OnWinnerDecided;

    private Dictionary<int, float> results = new Dictionary<int, float>();
    private int expectedResults = 3;

    public void ResetForNewRound()
    {
        results.Clear();
    }


    public void ReceiveResult(int playerIndex, float averageErrorMs)
    {
        results[playerIndex] = averageErrorMs;
        Debug.Log($"[JudgeManager] Player {playerIndex} 결과 수신: {averageErrorMs:F1}ms ({results.Count}/{expectedResults})");


        if (results.Count >= expectedResults)
        {
            DecideWinner();
        }
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

        string[] names = { "B", "C", "D" };

        OnWinnerDecided?.Invoke(winnerIndex, bestScore);
    }
}
