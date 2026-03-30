using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private Dictionary<int, int> scores = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ───────────────────────────────────────────
    // 초기화
    // ───────────────────────────────────────────

    public void InitScores()
    {
        scores.Clear();

        if (GameManager.Instance == null) return;

        for (int i = 0; i < GameManager.Instance.players.Count; i++)
            scores[i] = 0;

        Debug.Log($"[ScoreManager] {scores.Count}명 점수 초기화");
        UpdateAllLabels();
    }

    // ───────────────────────────────────────────
    // 상황 1: 배 타임아웃
    // ───────────────────────────────────────────

    public void OnShipTimeout()
    {
        if (RoleManager.Instance == null) return;

        int shipIdx = RoleManager.Instance.ShipPlayerIndex;
        AddScore(shipIdx, -1);

        foreach (var lhIdx in RoleManager.Instance.LighthousePlayerIndices)
            AddScore(lhIdx, 2);

        Debug.Log("[ScoreManager] 상황1 - 배 타임아웃");
        LogScores();
    }

    // ───────────────────────────────────────────
    // 상황 2: 라운드 정상 완료
    // ───────────────────────────────────────────

    public void OnRoundComplete(Dictionary<int, float> results)
    {
        if (RoleManager.Instance == null) return;

        int shipIdx = RoleManager.Instance.ShipPlayerIndex;
        var lighthousePlayers = RoleManager.Instance.LighthousePlayerIndices;

        // 오차 오름차순 정렬
        var sorted = new List<KeyValuePair<int, float>>(results);
        sorted.Sort((a, b) => a.Value.CompareTo(b.Value));

        // 배: +1
        AddScore(shipIdx, 1);

        for (int rank = 0; rank < sorted.Count; rank++)
        {
            int judgeIndex = sorted[rank].Key;
            if (judgeIndex >= lighthousePlayers.Count) continue;

            int playerIdx = lighthousePlayers[judgeIndex];

            if (rank == 0) { AddScore(playerIdx, 3); Debug.Log($"1등 Player {playerIdx + 1} +3"); }
            else if (rank == 1) { AddScore(playerIdx, 1); Debug.Log($"2등 Player {playerIdx + 1} +1"); }
            else { AddScore(playerIdx, -1); Debug.Log($"Player {playerIdx + 1} -1"); }
        }

        LogScores();
    }

    // ───────────────────────────────────────────
    // 상황 3: 게임 종료
    // ───────────────────────────────────────────

    public void OnGameEnd(int winnerLighthouseJudgeIndex)
    {
        if (RoleManager.Instance == null) return;

        int shipIdx = RoleManager.Instance.ShipPlayerIndex;
        var lighthousePlayers = RoleManager.Instance.LighthousePlayerIndices;

        AddScore(shipIdx, 12);

        if (winnerLighthouseJudgeIndex < lighthousePlayers.Count)
        {
            int winnerPlayerIdx = lighthousePlayers[winnerLighthouseJudgeIndex];
            AddScore(winnerPlayerIdx, 10);
            Debug.Log($"[ScoreManager] 상황3 - 등대 Player {winnerPlayerIdx + 1} +10 / 배 Player {shipIdx + 1} +12");
        }

        LogScores();
    }

    // ───────────────────────────────────────────
    // 헬퍼
    // ───────────────────────────────────────────

    private void AddScore(int playerIndex, int amount)
    {
        if (!scores.ContainsKey(playerIndex))
            scores[playerIndex] = 0;

        scores[playerIndex] += amount;

        // PlayerLabelManager에 점수 업데이트
        PlayerLabelManager.Instance?.UpdateScore(playerIndex, scores[playerIndex]);
    }

    public int GetScore(int playerIndex)
    {
        return scores.ContainsKey(playerIndex) ? scores[playerIndex] : 0;
    }

    private void UpdateAllLabels()
    {
        foreach (var pair in scores)
            PlayerLabelManager.Instance?.UpdateScore(pair.Key, pair.Value);
    }

    private void LogScores()
    {
        string log = "[ScoreManager] 현재 점수:\n";
        foreach (var pair in scores)
            log += $"  Player {pair.Key + 1}: {pair.Value}점\n";
        Debug.Log(log);
    }
}