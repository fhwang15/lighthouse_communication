using System.Collections;
using UnityEngine;

/// <summary>
/// GameScene 시작 트리거
/// 1. GameManager 플레이어 등록 확인
/// 2. RoleManager 역할 배정
/// 3. PlayerLabelManager 닉네임/점수 라벨 세팅
/// 4. 카메라 전환
/// 5. RhythmGameManager 시작
/// </summary>
public class GameStarter : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private RhythmGameManager rhythmGameManager;

    [Header("Game UI")]
    [SerializeField] private GameObject gameUI;

    private IEnumerator Start()
    {
        // GameManager.Start()가 플레이어 등록 마칠 때까지 한 프레임 대기
        yield return null;

        if (GameManager.Instance == null || GameManager.Instance.players.Count == 0)
        {
            Debug.LogError("[GameStarter] 플레이어가 없어! LobbyScene을 먼저 거쳐야 해.");
            yield break;
        }

        StartGame();
    }

    private void StartGame()
    {
        Debug.Log("[GameStarter] 게임 시작!");

        GameManager.Instance.gameStarted = true;
        GameManager.Instance.movementEnabled = false;

        // Game UI 켜기
        if (gameUI != null) gameUI.SetActive(true);

        // 역할 랜덤 배정
        if (RoleManager.Instance != null)
            RoleManager.Instance.AssignRoles();
        else
            Debug.LogError("[GameStarter] RoleManager 없음!");

        // 닉네임/점수 라벨 세팅 (역할 배정 후에 해야 함!)
        PlayerLabelManager.Instance?.SetupLabels();

        // 카메라 전환
        FindObjectOfType<PartyCameraController>()?.TransitionToGame();

        // 리듬 게임 시작
        if (rhythmGameManager != null)
            rhythmGameManager.StartGame();
        else
            Debug.LogError("[GameStarter] RhythmGameManager 연결 안 됨!");
    }
}