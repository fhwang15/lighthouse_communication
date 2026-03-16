using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 플레이어들에게 Ship/Lighthouse 역할을 랜덤 배정
/// 
/// 사용법:
/// 1. 빈 GameObject에 이 스크립트 붙여
/// 2. Inspector에서 shipPosition, lighthousePositions 연결
/// 3. CharacterSelectManager.StartGame() 에서 AssignRoles() 호출
/// </summary>
public class RoleManager : MonoBehaviour
{
    public static RoleManager Instance;

    [Header("위치 연결")]
    [SerializeField] private Transform shipPosition;           // Ship 스폰 위치
    [SerializeField] private Transform[] lighthousePositions;  // Lighthouse 스폰 위치 [0~2]

    // 역할 결과 (외부에서 읽기용)
    public int ShipPlayerIndex { get; private set; } = -1;          // Ship 담당 플레이어 인덱스
    public List<int> LighthousePlayerIndices { get; private set; }  // Lighthouse 담당 플레이어 인덱스들

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    /// <summary>
    /// CharacterSelectManager.StartGame() 에서 호출.
    /// 역할 랜덤 배정 + 캐릭터 위치 이동
    /// </summary>
    public void AssignRoles()
    {
        var players = GameManager.Instance.players;

        if (players.Count == 0)
        {
            Debug.LogError("[RoleManager] 플레이어가 없어!");
            return;
        }

        // ── 랜덤으로 Ship 역할 뽑기 ──
        ShipPlayerIndex = Random.Range(0, players.Count);
        LighthousePlayerIndices = new List<int>();

        for (int i = 0; i < players.Count; i++)
        {
            if (i != ShipPlayerIndex)
                LighthousePlayerIndices.Add(i);
        }

        Debug.Log($"[RoleManager] Ship: Player {ShipPlayerIndex + 1} / Lighthouse: {string.Join(", ", LighthousePlayerIndices.ConvertAll(i => "Player " + (i + 1)))}");

        // ── 캐릭터 위치 이동 + UI 표시 ──
        MoveToPosition(ShipPlayerIndex, shipPosition, "🚢 Ship");

        for (int i = 0; i < LighthousePlayerIndices.Count; i++)
        {
            int playerIdx = LighthousePlayerIndices[i];

            if (i < lighthousePositions.Length)
                MoveToPosition(playerIdx, lighthousePositions[i], $"💡 Lighthouse {i + 1}");
            else
                Debug.LogWarning($"[RoleManager] Lighthouse 위치 {i}번이 없어! Inspector에서 추가해줘.");
        }
    }

    // ───────────────────────────────────────────
    // 내부 로직
    // ───────────────────────────────────────────

    private void MoveToPosition(int playerIndex, Transform targetPos, string roleName)
    {
        var players = GameManager.Instance.players;

        if (playerIndex < 0 || playerIndex >= players.Count) return;
        if (targetPos == null) return;

        PlayerSlot player = players[playerIndex];

        // 캐릭터 위치 이동
        if (player.currentAvatar != null)
        {
            // CharacterController 있으면 먼저 끄고 이동!
            var cc = player.currentAvatar.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.currentAvatar.transform.position = targetPos.position;
            player.currentAvatar.transform.rotation = targetPos.rotation;

            if (cc != null) cc.enabled = true; // 다시 켜기
        }

        // UI에 역할 표시
        if (player.playerCharacterController != null)
        {
            player.playerCharacterController.SetLine1($"Player {playerIndex + 1}");
            player.playerCharacterController.SetLine2(roleName);
        }

        Debug.Log($"[RoleManager] Player {playerIndex + 1} → {roleName} ({targetPos.position})");
    }
}
