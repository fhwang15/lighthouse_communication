using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 게임 전체 흐름을 관리하는 GameManager
/// 
/// 게임 흐름:
/// [대기] → [A 녹화] → [재생] → [B/C/D 모방] → [판정] → [배 이동] → 반복
/// 
/// 사용법:
/// 1. 빈 GameObject "GameManager"에 이 스크립트 붙여
/// 2. Inspector에서 recorder, player, judgeManager, judges 연결
/// 3. Play 누르면 자동 시작!
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("핵심 시스템 연결")]
    [SerializeField] private NoteRecorder recorder;
    [SerializeField] private NotePlayer notePlayer;
    [SerializeField] private JudgeManager judgeManager;
    [SerializeField] private AccuracyJudge[] judges; // B, C, D (3개)

    [Header("배 이동 설정")]
    [SerializeField] private Transform shipTransform;       // 배 오브젝트
    [SerializeField] private Transform[] lighthousePositions; // 등대 위치 [0=B, 1=C, 2=D]
    [SerializeField] private float shipMoveSpeed = 2.0f;    // 배 이동 속도

    [Header("우승 거리 설정")]
    [SerializeField] private float winDistance = 1.5f; // 등대에 이 거리 이내면 우승

    [Header("이벤트")]
    public UnityEvent<int> OnGameOver; // 우승 플레이어 인덱스 (0=B,1=C,2=D)

    // 현재 게임 상태
    public enum GameState { Idle, ARecording, Playing, BCD_Mimicking, Judging, Moving }
    public GameState CurrentState { get; private set; } = GameState.Idle;

    // ───────────────────────────────────────────
    // 시작
    // ───────────────────────────────────────────

    private void Start()
    {
        // 이벤트 연결 (Inspector 연결 대신 코드로도 가능)
        recorder.OnRecordingComplete.AddListener(notePlayer.PlayNotes);
        notePlayer.OnPlaybackComplete.AddListener(StartMimicPhase);
        judgeManager.OnWinnerDecided.AddListener(OnRoundWinnerDecided);

        // 게임 시작!
        StartCoroutine(StartRound());
    }

    // ───────────────────────────────────────────
    // 라운드 흐름
    // ───────────────────────────────────────────

    private IEnumerator StartRound()
    {
        Debug.Log("====== 새 라운드 시작 ======");
        judgeManager.ResetForNewRound();

        // 잠깐 대기 후 A 녹화 시작
        yield return new WaitForSeconds(1.0f);

        CurrentState = GameState.ARecording;
        Debug.Log("[페이즈 1] A 플레이어: 스페이스바로 노트를 입력하세요!");
        recorder.StartRecording();

        // recorder는 자동으로 완료되면 OnRecordingComplete 발생
        // → NotePlayer.PlayNotes() 자동 호출
    }

    private void StartMimicPhase()
    {
        CurrentState = GameState.BCD_Mimicking;
        Debug.Log("[페이즈 3] B/C/D 플레이어: 지금 모방하세요!");

        foreach (var judge in judges)
        {
            judge.StartListening();
        }
    }

    private void OnRoundWinnerDecided(int winnerIndex, float errorMs)
    {
        CurrentState = GameState.Moving;
        Debug.Log($"[라운드 결과] Player {winnerIndex} 승! 배가 이동합니다.");

        StartCoroutine(MoveShipToWinner(winnerIndex));
    }

    // ───────────────────────────────────────────
    // 배 이동
    // ───────────────────────────────────────────

    private IEnumerator MoveShipToWinner(int winnerIndex)
    {
        if (shipTransform == null || lighthousePositions == null
            || winnerIndex >= lighthousePositions.Length)
        {
            Debug.LogWarning("배 또는 등대 위치가 설정되지 않았어!");
            StartCoroutine(StartRound());
            yield break;
        }

        Vector3 targetPos = lighthousePositions[winnerIndex].position;

        // 배를 등대 쪽으로 조금씩 이동
        while (Vector3.Distance(shipTransform.position, targetPos) > shipMoveSpeed * Time.deltaTime)
        {
            shipTransform.position = Vector3.MoveTowards(
                shipTransform.position,
                targetPos,
                shipMoveSpeed * Time.deltaTime
            );
            yield return null;

            // 배가 특정 거리 이내면 이동 멈추고 다음 라운드
            if (Vector3.Distance(shipTransform.position, targetPos) <= shipMoveSpeed * 0.5f)
            {
                break;
            }
        }

        Debug.Log("[이동 완료]");

        // 우승 체크
        if (CheckWinCondition(out int winnerLighthouse))
        {
            Debug.Log($"🎉 게임 오버! 등대 {winnerLighthouse} 우승!");
            OnGameOver?.Invoke(winnerLighthouse);
        }

        // 아직 안 끝났으면 다음 라운드
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
