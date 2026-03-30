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
    [SerializeField] private float shipRotateSpeed = 90f;

    [Header("라운드당 이동 거리 (고정값)")]
    [Tooltip("매 라운드 이동하는 고정 거리 (비율 아님!)")]
    [SerializeField] private float moveStepDistance = 1.0f;

    [Header("우승 거리")]
    [SerializeField] private float winDistance = 1.5f;

    [Header("이벤트")]
    public UnityEvent<int> OnGameOver;

    public enum GameState { Idle, ARecording, Playing, BCD_Mimicking, Moving }
    public GameState CurrentState { get; private set; } = GameState.Idle;

    private bool gameRunning = false;

    // ───────────────────────────────────────────
    // 외부에서 호출
    // ───────────────────────────────────────────

    public void StartGame()
    {
        if (gameRunning) return;
        gameRunning = true;

        recorder.OnRecordingComplete.AddListener(notePlayer.PlayNotes);
        recorder.OnRecordingComplete.AddListener(OnRecordingCompleted);
        notePlayer.OnPlaybackComplete.AddListener(StartMimicPhase);
        judgeManager.OnWinnerDecided.AddListener(OnRoundWinnerDecided);

        ScoreManager.Instance?.InitScores();

        StartCoroutine(StartRound());
    }

    // ───────────────────────────────────────────
    // 라운드 흐름
    // ───────────────────────────────────────────

    private IEnumerator StartRound()
    {
        Debug.Log("====== 새 라운드 시작 ======");
        judgeManager.ResetForNewRound();

        // 악보 UI 초기화
        RhythmScoreUI.Instance?.ResetBoard();

        yield return new WaitForSeconds(1.0f);

        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowPhase("RECORD!"));

        CurrentState = GameState.ARecording;
        recorder.StartRecording();
    }

    private void OnRecordingCompleted(List<float> notes)
    {
        if (notes.Count == 0)
        {
            ScoreManager.Instance?.OnShipTimeout();
            StartCoroutine(TimeoutSequence());
        }
        else
        {
            StartCoroutine(ShowPlayingPhaseUI());
        }
    }

    private IEnumerator ShowPlayingPhaseUI()
    {
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowPhase("WATCH!"));

        CurrentState = GameState.Playing;
    }

    private IEnumerator TimeoutSequence()
    {
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowMessage("TIME OUT! ⚠️", 1.5f));

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(StartRound());
    }

    private void StartMimicPhase()
    {
        if (notePlayer.CurrentNoteTimestamps == null || notePlayer.CurrentNoteTimestamps.Count == 0)
            return;

        StartCoroutine(StartMimicPhaseCoroutine());
    }

    private IEnumerator StartMimicPhaseCoroutine()
    {
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowPhase("COPY!"));

        CurrentState = GameState.BCD_Mimicking;

        if (RoleManager.Instance == null) { Debug.LogError("[RhythmGameManager] RoleManager 없음!"); yield break; }

        var lighthousePlayers = RoleManager.Instance.LighthousePlayerIndices;

        RhythmScoreUI.Instance?.StartMimicPhase(recorder.recordingTimeLimit);

        for (int i = 0; i < judges.Length; i++)
        {
            if (i >= lighthousePlayers.Count) continue;

            int playerIdx = lighthousePlayers[i];
            var slot = GameManager.Instance.players[playerIdx];

            judges[i].playerIndex = i;
            judges[i].SetPlayerSlot(slot);
            judges[i].StartListening();
        }

        judgeManager.SetExpectedResults(Mathf.Min(judges.Length, lighthousePlayers.Count));
    }

    private void OnRoundWinnerDecided(int winnerJudgeIndex, float errorMs)
    {
        CurrentState = GameState.Moving;
        ScoreManager.Instance?.OnRoundComplete(judgeManager.GetLastResults());
        StartCoroutine(MoveShipStep(winnerJudgeIndex));
    }

    // ───────────────────────────────────────────
    // 배 이동 - 고정 거리로 이동 (버그 수정!)
    // ───────────────────────────────────────────

    private IEnumerator MoveShipStep(int lighthouseIndex)
    {
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowMessage("MOVING! 🚢", 1.0f));

        if (shipTransform == null || lighthousePositions == null
            || lighthouseIndex >= lighthousePositions.Length)
        {
            StartCoroutine(StartRound());
            yield break;
        }

        Vector3 targetPos = lighthousePositions[lighthouseIndex].position;

        // ── 핵심 수정 ──────────────────────────────
        // 비율(%) 대신 고정 거리(moveStepDistance)만큼 이동
        // → 언젠가는 반드시 winDistance 이내로 들어옴!
        Vector3 direction = (targetPos - shipTransform.position).normalized;
        float currentDist = Vector3.Distance(shipTransform.position, targetPos);

        // 이미 등대 안에 있으면 바로 우승 체크
        if (currentDist <= winDistance)
        {
            HandleWin(lighthouseIndex);
            yield break;
        }

        // 이동 목적지: 현재 위치에서 방향으로 moveStepDistance만큼
        // 단, 등대를 넘어가지 않도록 clamp
        float actualStep = Mathf.Min(moveStepDistance, currentDist - winDistance + 0.1f);
        Vector3 stepDestination = shipTransform.position + direction * actualStep;

        // 배 방향 회전
        Vector3 dir = direction;
        dir.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up)
            * Quaternion.Euler(0, -90f, 0); // X축 보정

        // 이동 + 회전
        while (Vector3.Distance(shipTransform.position, stepDestination) > 0.01f)
        {
            shipTransform.position = Vector3.MoveTowards(
                shipTransform.position,
                stepDestination,
                shipMoveSpeed * Time.deltaTime
            );

            shipTransform.rotation = Quaternion.RotateTowards(
                shipTransform.rotation,
                targetRotation,
                shipRotateSpeed * Time.deltaTime
            );

            yield return null;
        }

        shipTransform.position = stepDestination;
        Debug.Log($"[이동 완료] 등대까지 남은 거리: {Vector3.Distance(shipTransform.position, targetPos):F2}");

        // 우승 체크
        if (Vector3.Distance(shipTransform.position, targetPos) <= winDistance)
        {
            HandleWin(lighthouseIndex);
            yield break;
        }

        StartCoroutine(StartRound());
    }

    private void HandleWin(int lighthouseIndex)
    {
        ScoreManager.Instance?.OnGameEnd(lighthouseIndex);

        int winnerPlayerIndex = RoleManager.Instance.LighthousePlayerIndices[lighthouseIndex];
        Debug.Log($"게임 오버! Player {winnerPlayerIndex + 1} 우승!");

        StartCoroutine(WinSequence(winnerPlayerIndex));
    }

    private IEnumerator WinSequence(int winnerPlayerIndex)
    {
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(
                PhaseUIManager.Instance.ShowMessage($"🎉 Player {winnerPlayerIndex + 1} WINS!", 3.0f));

        OnGameOver?.Invoke(winnerPlayerIndex);
    }
}