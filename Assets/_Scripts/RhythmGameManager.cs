using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 노트 녹화 / 재생 / 판정 / 배 이동 전담 매니저
/// RoleManager에서 Ship/Lighthouse 역할을 받아서 동작
/// </summary>
public class RhythmGameManager : MonoBehaviour
{
    [Header("핵심 시스템 연결")]
    [SerializeField] private NoteRecorder recorder;
    [SerializeField] private NotePlayer notePlayer;
    [SerializeField] private JudgeManager judgeManager;
    [SerializeField] private AccuracyJudge[] judges; // Lighthouse 수만큼 (최대 3개)

    [Header("배 이동 설정")]
    [SerializeField] private Transform shipTransform;
    [SerializeField] private Transform[] lighthousePositions; // [0]=LH0, [1]=LH1, [2]=LH2
    [SerializeField] private float shipMoveSpeed = 3.0f;

    [Header("라운드당 이동 비율 (0.1 = 10%)")]
    [SerializeField] private float moveStepRatio = 0.1f;

    [Header("우승 거리")]
    [SerializeField] private float winDistance = 1.0f;

    [Header("이벤트")]
    public UnityEvent<int> OnGameOver; // 우승 플레이어 인덱스

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
        notePlayer.OnPlaybackComplete.AddListener(StartMimicPhase);
        judgeManager.OnWinnerDecided.AddListener(OnRoundWinnerDecided);

        StartCoroutine(StartRound());
    }

    // ───────────────────────────────────────────
    // 라운드 흐름
    // ───────────────────────────────────────────

    private IEnumerator StartRound()
    {
        Debug.Log("====== 새 라운드 시작 ======");
        judgeManager.ResetForNewRound();

        yield return new WaitForSeconds(1.0f);

        CurrentState = GameState.ARecording;

        // Ship 플레이어가 누군지 NoteRecorder에 알려줌
        // (NoteRecorder는 RoleManager.ShipPlayerIndex 기반으로 입력 받음)
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

        // Lighthouse 플레이어들에게 각각 AccuracyJudge 배정
        for (int i = 0; i < judges.Length; i++)
        {
            // Lighthouse 플레이어가 없으면 스킵
            if (i >= lighthousePlayers.Count) continue;

            int playerIdx = lighthousePlayers[i]; // 실제 players 리스트 인덱스
            var slot = GameManager.Instance.players[playerIdx];

            judges[i].playerIndex = i;
            judges[i].SetPlayerSlot(slot);
            judges[i].StartListening();

            Debug.Log($"[RhythmGameManager] Judge {i} → Player {playerIdx + 1} (Lighthouse)");
        }

        // 이번 라운드에 참여하는 Judge 수를 JudgeManager에 알려줌
        judgeManager.SetExpectedResults(Mathf.Min(judges.Length, lighthousePlayers.Count));
    }

    private void OnRoundWinnerDecided(int winnerJudgeIndex, float errorMs)
    {
        CurrentState = GameState.Moving;

        // winnerJudgeIndex = judges 배열 인덱스 (0,1,2)
        // → 실제 lighthouse 위치 인덱스로 변환
        Debug.Log($"[라운드 승자] Judge {winnerJudgeIndex} → 배 이동");
        StartCoroutine(MoveShipStep(winnerJudgeIndex));
    }

    //Ship moving
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

        // 우승 체크
        if (CheckWinCondition(out int winnerLighthouse))
        {
            // 실제 우승 플레이어 인덱스 찾기
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