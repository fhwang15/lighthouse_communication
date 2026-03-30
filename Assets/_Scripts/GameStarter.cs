using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// GameScene 시작 시 자동으로 게임을 트리거하는 스크립트
/// CharacterSelectManager를 완전히 대체
/// 
/// 역할:
/// 1. GameManager 플레이어 등록 완료 확인
/// 2. RoleManager로 역할 배정
/// 3. 카메라 전환
/// 4. RhythmGameManager 시작
/// </summary>
public class GameStarter : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private RhythmGameManager rhythmGameManager;

    [Header("Game UI")]
    [SerializeField] private GameObject gameUI;

    [Header("페이즈 UI")]
    [SerializeField] private TextMeshProUGUI phaseText;

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

        // 역할 랜덤 배정 (Ship/Lighthouse)
        if (RoleManager.Instance != null)
            RoleManager.Instance.AssignRoles();
        else
            Debug.LogError("[GameStarter] RoleManager 없음!");

        // 카메라 전환
        FindObjectOfType<PartyCameraController>()?.TransitionToGame();

        // 리듬 게임 시작
        if (rhythmGameManager != null)
            rhythmGameManager.StartGame();
        else
            Debug.LogError("[GameStarter] RhythmGameManager 연결 안 됨!");
    }
}