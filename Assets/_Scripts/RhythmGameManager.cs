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
    [SerializeField] private float shipRotateSpeed = 3.0f;

    [Header("라운드당 이동 비율 (0.1 = 10%)")]
    [SerializeField] private float moveStepRatio = 0.1f;

    [Header("우승 거리")]
    [SerializeField] private float winDistance = 1.0f;

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

        // ── Recording Phase 카운트다운 ──
        // "3, 2, 1, RECORD!" 표시 후 녹화 시작
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowPhase("RECORD!"));

        CurrentState = GameState.ARecording;
        recorder.StartRecording();
    }

    private void OnRecordingCompleted(List<NoteData> notes)
    {
        if (notes.Count == 0)
        {
            Debug.Log("[RhythmGameManager] 배 타임아웃!");
            ScoreManager.Instance?.OnShipTimeout();
            StartCoroutine(TimeoutSequence());
        }
        // 정상 완료 시 → PlayNotes 이벤트가 자동으로 NotePlayer.PlayNotes 호출
        // → PlaybackCoroutine 시작 → OnPlaybackComplete → StartMimicPhase
        // 단, Playing Phase 카운트다운은 NotePlayer가 내부적으로 prePlayDelay로 처리 중
        // → 여기서 추가로 표시하고 싶으면 아래 주석 해제
        else
        {
            StartCoroutine(ShowPlayingPhaseUI());
        }
    }

    private IEnumerator ShowPlayingPhaseUI()
    {
        // ── Playing Phase 카운트다운 ──
        // "3, 2, 1, WATCH!" 표시
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowPhase("WATCH!"));

        CurrentState = GameState.Playing;
        // NotePlayer는 이미 PlayNotes 실행 중 (prePlayDelay로 대기 중)
        // → PhaseUIManager 카운트다운 시간(3초)이 NotePlayer의 prePlayDelay와 맞아야 함!
        // NotePlayer.prePlayDelay = PhaseUIManager.countdownInterval * 3 + messageHoldTime
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
        if (notePlayer.CurrentNotes == null || notePlayer.CurrentNotes.Count == 0)
            return;

        StartCoroutine(StartMimicPhaseCoroutine());
    }

    private IEnumerator StartMimicPhaseCoroutine()
    {
        // ── Mimic Phase 카운트다운 ──
        // "3, 2, 1, COPY!" 표시
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowPhase("COPY!"));

        CurrentState = GameState.BCD_Mimicking;

        if (RoleManager.Instance == null)
        {
            Debug.LogError("[RhythmGameManager] RoleManager 없음!");
            yield break;
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
    // 배 이동 + 방향 전환
    // ───────────────────────────────────────────

    private IEnumerator MoveShipStep(int lighthouseIndex)
    {
        // ── Moving Phase 메시지 ──
        if (PhaseUIManager.Instance != null)
            yield return StartCoroutine(PhaseUIManager.Instance.ShowMessage("MOVING! 🚢", 1.0f));

        if (shipTransform == null || lighthousePositions == null
            || lighthouseIndex >= lighthousePositions.Length)
        {
            StartCoroutine(StartRound());
            yield break;
        }

        Vector3 targetPos = lighthousePositions[lighthouseIndex].position;
        Vector3 startPos = shipTransform.position;
        Vector3 stepDestination = Vector3.Lerp(startPos, targetPos, moveStepRatio);

        // 배 방향을 목표 등대 쪽으로
        Vector3 directionToTarget = (targetPos - shipTransform.position).normalized;
        directionToTarget.y = 0f;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        while (Vector3.Distance(shipTransform.position, stepDestination) > 0.01f)
        {
            shipTransform.position = Vector3.MoveTowards(
                shipTransform.position,
                stepDestination,
                shipMoveSpeed * Time.deltaTime
            );

            shipTransform.rotation = Quaternion.Slerp(
                shipTransform.rotation,
                targetRotation,
                shipRotateSpeed * Time.deltaTime
            );

            yield return null;
        }

        shipTransform.position = stepDestination;
        Debug.Log("[이동 완료]");

        if (CheckWinCondition(out int winnerLighthouse))
        {
            ScoreManager.Instance?.OnGameEnd(winnerLighthouse);

            int winnerPlayerIndex = RoleManager.Instance.LighthousePlayerIndices[winnerLighthouse];

            if (PhaseUIManager.Instance != null)
                yield return StartCoroutine(PhaseUIManager.Instance.ShowMessage($"🎉 Player {winnerPlayerIndex + 1} WINS!", 3.0f));

            OnGameOver?.Invoke(winnerPlayerIndex);
            yield break;
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