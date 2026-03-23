using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 점수 계산 전담 매니저
/// 
/// 상황 1: 배 타임아웃 → 배 -1, 나머지 +2
/// 상황 2: 라운드 정상 완료 → 1등 +3, 배 +1, 2등 +1, 나머지 -1
/// 상황 3: 게임 종료 → 도착 등대 +10, 배 +12
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    // playerIndex → 점수
    private Dictionary<int, int> scores = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
        UpdateAllDisplays();
    }

    // ───────────────────────────────────────────
    // 상황 1: 배 타임아웃
    // ───────────────────────────────────────────

    public void OnShipTimeout()
    {
        if (RoleManager.Instance == null) return;

        int shipIdx = RoleManager.Instance.ShipPlayerIndex;

        // 배: -1
        AddScore(shipIdx, -1);

        // 나머지 전원: +2
        foreach (var lhIdx in RoleManager.Instance.LighthousePlayerIndices)
            AddScore(lhIdx, 2);

        Debug.Log("[ScoreManager] 상황1 - 배 타임아웃");
        LogScores();
    }

    // ───────────────────────────────────────────
    // 상황 2: 라운드 정상 완료
    // results = { judgeIndex → errorMs } 딕셔너리
    // ───────────────────────────────────────────

    public void OnRoundComplete(Dictionary<int, float> results)
    {
        if (RoleManager.Instance == null) return;

        int shipIdx = RoleManager.Instance.ShipPlayerIndex;
        var lighthousePlayers = RoleManager.Instance.LighthousePlayerIndices;

        // errorMs 오름차순 정렬 (낮을수록 정확)
        var sorted = new List<KeyValuePair<int, float>>(results);
        sorted.Sort((a, b) => a.Value.CompareTo(b.Value));

        // 배: +1
        AddScore(shipIdx, 1);

        for (int rank = 0; rank < sorted.Count; rank++)
        {
            int judgeIndex = sorted[rank].Key;

            // judgeIndex → 실제 playerIndex 변환
            if (judgeIndex >= lighthousePlayers.Count) continue;
            int playerIdx = lighthousePlayers[judgeIndex];

            if (rank == 0)
            {
                // 1등: +3
                AddScore(playerIdx, 3);
                Debug.Log($"[ScoreManager] 1등 Player {playerIdx + 1} +3");
            }
            else if (rank == 1)
            {
                // 2등: +1
                AddScore(playerIdx, 1);
                Debug.Log($"[ScoreManager] 2등 Player {playerIdx + 1} +1");
            }
            else
            {
                // 나머지: -1
                AddScore(playerIdx, -1);
                Debug.Log($"[ScoreManager] Player {playerIdx + 1} -1");
            }
        }

        Debug.Log("[ScoreManager] 상황2 - 라운드 완료");
        LogScores();
    }

    // ───────────────────────────────────────────
    // 상황 3: 게임 종료 (배가 등대 도달)
    // ───────────────────────────────────────────

    public void OnGameEnd(int winnerLighthouseJudgeIndex)
    {
        if (RoleManager.Instance == null) return;

        int shipIdx = RoleManager.Instance.ShipPlayerIndex;
        var lighthousePlayers = RoleManager.Instance.LighthousePlayerIndices;

        // 배: +12
        AddScore(shipIdx, 12);

        // 도착한 등대 플레이어: +10
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

        // 머리 위 점수 업데이트
        UpdateDisplay(playerIndex);
    }

    public int GetScore(int playerIndex)
    {
        return scores.ContainsKey(playerIndex) ? scores[playerIndex] : 0;
    }

    private void UpdateDisplay(int playerIndex)
    {
        var players = GameManager.Instance?.players;
        if (players == null || playerIndex >= players.Count) return;

        var avatar = players[playerIndex].currentAvatar;
        if (avatar == null) return;

        var display = avatar.GetComponentInChildren<ScoreDisplay>();
        display?.UpdateScore(scores[playerIndex]);
    }

    private void UpdateAllDisplays()
    {
        foreach (var pair in scores)
            UpdateDisplay(pair.Key);
    }

    private void LogScores()
    {
        string log = "[ScoreManager] 현재 점수:\n";
        foreach (var pair in scores)
            log += $"  Player {pair.Key + 1}: {pair.Value}점\n";
        Debug.Log(log);
    }
}