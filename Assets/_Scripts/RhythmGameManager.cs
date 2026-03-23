using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RhythmGameManager : MonoBehaviour
{
    [Header("핵심 시스템 연결")]
    [SerializeField] private NoteRecorder recorder;
    [SerializeField] private NotePlayer notePlayer;
    [SerializeField] private JudgeManager judgeManager;
    [SerializeField] private AccuracyJudge[] judges;

    [Header("배 이동 설정")]
    [SerializeField] private Transform shipTransform;
    [SerializeField] private Transform[] lighthousePositions;
    [SerializeField] private float shipMoveSpeed = 3.0f;

    [Header("라운드당 이동 비율 (0.1 = 10%)")]
    [SerializeField] private float moveStepRatio = 0.1f;

    [Header("우승 거리")]
    [SerializeField] private float winDistance = 1.0f;

    [Header("이벤트")]
    public UnityEvent<int> OnGameOver;

    public enum GameState { Idle, ARecording, Playing, BCD_Mimicking, Moving }
    public GameState CurrentState { get; private set; } = GameState.Idle;

    private bool gameRunning = false;

    public void StartGame()
    {
        if (gameRunning) return;
        gameRunning = true;

        // NoteData 기반 이벤트 연결
        recorder.OnRecordingComplete.AddListener(notePlayer.PlayNotes);
        notePlayer.OnPlaybackComplete.AddListener(StartMimicPhase);
        judgeManager.OnWinnerDecided.AddListener(OnRoundWinnerDecided);

        StartCoroutine(StartRound());
    }

    private IEnumerator StartRound()
    {
        Debug.Log("====== 새 라운드 시작 ======");
        judgeManager.ResetForNewRound();

        yield return new WaitForSeconds(1.0f);

        CurrentState = GameState.ARecording;
        recorder.StartRecording();
    }

    private void StartMimicPhase()
    {
        CurrentState = GameState.BCD_Mimicking;

        if (RoleManager.Instance == null)
        {
            Debug.LogError("[RhythmGameManager] RoleManager 없음!");
            return;
        }

        var lighthousePlayers = RoleManager.Instance.LighthousePlayerIndices;

        for (int i = 0; i < judges.Length; i++)
        {
            if (i >= lighthousePlayers.Count) continue;

            int playerIdx = lighthousePlayers[i];
            var slot = GameManager.Instance.players[playerIdx];

            judges[i].playerIndex = i;
            judges[i].SetPlayerSlot(slot);
            judges[i].StartListening();

            Debug.Log($"[RhythmGameManager] Judge {i} → Player {playerIdx + 1} (Lighthouse)");
        }

        judgeManager.SetExpectedResults(Mathf.Min(judges.Length, lighthousePlayers.Count));
    }

    private void OnRoundWinnerDecided(int winnerJudgeIndex, float errorMs)
    {
        CurrentState = GameState.Moving;
        StartCoroutine(MoveShipStep(winnerJudgeIndex));
    }

    private IEnumerator MoveShipStep(int lighthouseIndex)
    {
        if (shipTransform == null || lighthousePositions == null
            || lighthouseIndex >= lighthousePositions.Length)
        {
            StartCoroutine(StartRound());
            yield break;
        }

        Vector3 targetPos = lighthousePositions[lighthouseIndex].position;
        Vector3 startPos = shipTransform.position;
        Vector3 stepDestination = Vector3.Lerp(startPos, targetPos, moveStepRatio);

        while (Vector3.Distance(shipTransform.position, stepDestination) > 0.01f)
        {
            shipTransform.position = Vector3.MoveTowards(
                shipTransform.position,
                stepDestination,
                shipMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        shipTransform.position = stepDestination;
        Debug.Log("[이동 완료]");

        if (CheckWinCondition(out int winnerLighthouse))
        {
            int winnerPlayerIndex = RoleManager.Instance.LighthousePlayerIndices[winnerLighthouse];
            Debug.Log($"🎉 게임 오버! Player {winnerPlayerIndex + 1} 우승!");
            OnGameOver?.Invoke(winnerPlayerIndex);
        }

        StartCoroutine(StartRound());
    }

    private bool CheckWinCondition(out int winnerIndex)
    {
        winnerIndex = -1;
        if (shipTransform == null || lighthousePositions == null) return false;

        for (int i = 0; i < lighthousePositions.Length; i++)
        {
            if (Vector3.Distance(shipTransform.position, lighthousePositions[i].position) <= winDistance)
            {
                winnerIndex = i;
                return true;
            }
        }
        return false;
    }
}