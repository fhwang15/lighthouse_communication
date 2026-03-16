using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GameCore
{
    public class GameManager : MonoBehaviour
    {
        [Header("핵심 시스템 연결")]
        [SerializeField] private NoteRecorder recorder;
        [SerializeField] private NotePlayer notePlayer;
        [SerializeField] private JudgeManager judgeManager;
        [SerializeField] private AccuracyJudge[] judges;

        [Header("배 이동 설정")]
        [SerializeField] private Transform shipTransform;       
        [SerializeField] private Transform[] lighthousePositions; 
        [SerializeField] private float shipMoveSpeed = 2.0f;    
        
        [Header("우승 거리 설정")]
        [SerializeField] private float winDistance;

        [Header("이벤트")]
        public UnityEvent<int> OnGameOver;

        public enum GameState { Idle, ARecording, Playing, BCD_Mimicking, Judging, Moving }
        public GameState CurrentState { get; private set; } = GameState.Idle;

        private void Start()
        {

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
            Debug.Log("Ship: press space to do record the notes");
            recorder.StartRecording();

        }

        private void StartMimicPhase()
        {
            CurrentState = GameState.BCD_Mimicking;
            Debug.Log("mimic rn");

            foreach (var judge in judges)
            {
                judge.StartListening();
            }
        }

        public void OnRoundWinnerDecided(int winnerIndex, float errorMs)
        {
            CurrentState = GameState.Moving;

            StartCoroutine(MoveShipToWinner(winnerIndex));
        }

        private IEnumerator MoveShipToWinner(int winnerIndex)
        {
            if (shipTransform == null || lighthousePositions == null
                || winnerIndex >= lighthousePositions.Length)
            {
                StartCoroutine(StartRound());
                yield break;
            }

            Vector3 targetPos = lighthousePositions[winnerIndex].position;

            while (Vector3.Distance(shipTransform.position, targetPos) > shipMoveSpeed * Time.deltaTime)
            {
                shipTransform.position = Vector3.MoveTowards(
                    shipTransform.position,
                    targetPos,
                    shipMoveSpeed * Time.deltaTime
                );
                yield return null;

                if (Vector3.Distance(shipTransform.position, targetPos) <= shipMoveSpeed * 0.5f)
                {
                    break;
                }
            }

            Debug.Log("[이동 완료]");

            // 우승 체크
            if (CheckWinCondition(out int winnerLighthouse))
            {
                OnGameOver?.Invoke(winnerLighthouse);
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
}
