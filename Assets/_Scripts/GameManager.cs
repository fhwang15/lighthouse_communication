using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GameScene 전용 GameManager
/// LobbyScene에서 PlayerSessionData를 받아서 플레이어 등록
/// 로비/캐릭터 선택 관련 코드 없음
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("SessionData (LobbyScene에서 넘어온 데이터)")]
    [SerializeField] private PlayerSessionData sessionData;

    [Header("Settings")]
    public bool gameStarted = false;
    public bool movementEnabled = false;

    public List<PlayerSlot> players = new List<PlayerSlot>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SetupPlayers();
    }

    // ───────────────────────────────────────────
    // 플레이어 등록
    // ───────────────────────────────────────────

    private void SetupPlayers()
    {
        if (sessionData == null || sessionData.PlayerCount == 0)
        {
            Debug.LogError("[GameManager] PlayerSessionData 없음! LobbyScene을 먼저 거쳐야 해.");
            return;
        }

        var gamepads = new List<Gamepad>(Gamepad.all);
        int gamepadIndex = 0;

        foreach (var entry in sessionData.players)
        {
            PlayerSlot slot;

            if (entry.isKeyboard)
            {
                slot = new PlayerSlot(null);
            }
            else
            {
                Gamepad pad = gamepadIndex < gamepads.Count ? gamepads[gamepadIndex] : null;
                gamepadIndex++;
                slot = new PlayerSlot(pad);

                if (pad == null)
                    Debug.LogWarning($"[GameManager] Player {entry.playerIndex + 1} 컨트롤러 없음!");
            }

            players.Add(slot);
            Debug.Log($"[GameManager] Player {entry.playerIndex + 1} ({entry.nickname}) 등록");
        }

        Debug.Log($"[GameManager] 총 {players.Count}명 등록 완료");
    }
}