using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 대기 씬 전담 매니저
/// 
/// 흐름:
/// 1. 컨트롤러/키보드 연결 → 플레이어 조인
/// 2. 좌/우로 닉네임 선택
/// 3. A버튼(또는 Space)으로 Ready
/// 4. 전원 Ready → Player 1이 A버튼으로 게임 시작
/// 5. 5초 카운트다운 후 게임 씬 로드
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("ScriptableObject 연결")]
    [SerializeField] private PlayerSessionData sessionData;

    [Header("닉네임 목록 (Inspector에서 입력)")]
    [SerializeField]
    private List<string> nicknameList = new List<string>
    {
        "Sailor", "Captain", "Navigator", "Helmsman",
        "Crow", "Anchor", "Compass", "Lighthouse"
    };

    [Header("설정")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float countdownSeconds = 5f;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("테스트 모드")]
    [SerializeField] private bool testMode = false; // 키보드 조인 허용

    // 내부 상태
    public List<LobbyPlayerSlot> lobbyPlayers = new List<LobbyPlayerSlot>();
    private bool allReadyPhase = false;   // 전원 Ready → Player 1 대기 중
    private bool countingDown = false;
    private bool keyboardJoined = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        sessionData?.Clear();
        DetectExistingGamepads();
    }

    private void Update()
    {
        PollNewGamepads();

        // 테스트 모드: Enter로 키보드 플레이어 조인
        if (testMode && !keyboardJoined && lobbyPlayers.Count < maxPlayers)
        {
            if (Input.GetKeyDown(KeyCode.Return))
                AddKeyboardPlayer();
        }

        // 조인된 플레이어들 입력 처리
        foreach (var slot in lobbyPlayers)
            HandlePlayerInput(slot);

        // 전원 Ready 상태에서 Player 1이 A버튼으로 게임 시작
        if (allReadyPhase && !countingDown)
        {
            var p1 = lobbyPlayers.Count > 0 ? lobbyPlayers[0] : null;
            if (p1 == null) return;

            bool startPressed = p1.isKeyboard
                ? Input.GetKeyDown(KeyCode.Return)
                : p1.gamepad != null && p1.gamepad.buttonSouth.wasPressedThisFrame;

            if (startPressed)
                StartCoroutine(StartCountdown());
        }
    }

    // ───────────────────────────────────────────
    // 플레이어 조인
    // ───────────────────────────────────────────

    private void DetectExistingGamepads()
    {
        foreach (var pad in Gamepad.all)
            AddGamepadPlayer(pad);
    }

    private void PollNewGamepads()
    {
        foreach (var pad in Gamepad.all)
        {
            bool exists = lobbyPlayers.Exists(p => p.gamepad == pad);
            if (!exists) AddGamepadPlayer(pad);
        }
    }

    private void AddGamepadPlayer(Gamepad pad)
    {
        if (lobbyPlayers.Count >= maxPlayers) return;

        var slot = new LobbyPlayerSlot
        {
            playerIndex = lobbyPlayers.Count,
            gamepad = pad,
            isKeyboard = false,
            selectedNicknameIndex = lobbyPlayers.Count % nicknameList.Count,
            isReady = false
        };

        lobbyPlayers.Add(slot);
        LobbyUIManager.Instance?.OnPlayerJoined(slot);
        Debug.Log($"[Lobby] Player {slot.playerIndex + 1} joined (Gamepad)");
    }

    private void AddKeyboardPlayer()
    {
        if (lobbyPlayers.Count >= maxPlayers) return;

        keyboardJoined = true;
        var slot = new LobbyPlayerSlot
        {
            playerIndex = lobbyPlayers.Count,
            gamepad = null,
            isKeyboard = true,
            selectedNicknameIndex = lobbyPlayers.Count % nicknameList.Count,
            isReady = false
        };

        lobbyPlayers.Add(slot);
        LobbyUIManager.Instance?.OnPlayerJoined(slot);
        Debug.Log($"[Lobby] Player {slot.playerIndex + 1} joined (Keyboard)");
    }

    // ───────────────────────────────────────────
    // 플레이어 입력 처리
    // ───────────────────────────────────────────

    private void HandlePlayerInput(LobbyPlayerSlot slot)
    {
        if (slot.isReady) return;
        if (allReadyPhase) return;

        bool leftPressed, rightPressed, readyPressed;

        if (slot.isKeyboard)
        {
            leftPressed = Input.GetKeyDown(KeyCode.LeftArrow);
            rightPressed = Input.GetKeyDown(KeyCode.RightArrow);
            readyPressed = Input.GetKeyDown(KeyCode.Space);
        }
        else
        {
            if (slot.gamepad == null) return;
            leftPressed = slot.gamepad.dpad.left.wasPressedThisFrame;
            rightPressed = slot.gamepad.dpad.right.wasPressedThisFrame;
            readyPressed = slot.gamepad.buttonSouth.wasPressedThisFrame;
        }

        if (leftPressed) CycleNickname(slot, -1);
        if (rightPressed) CycleNickname(slot, 1);
        if (readyPressed) SetReady(slot);
    }

    private void CycleNickname(LobbyPlayerSlot slot, int direction)
    {
        slot.selectedNicknameIndex += direction;

        if (slot.selectedNicknameIndex < 0)
            slot.selectedNicknameIndex = nicknameList.Count - 1;
        if (slot.selectedNicknameIndex >= nicknameList.Count)
            slot.selectedNicknameIndex = 0;

        string nickname = nicknameList[slot.selectedNicknameIndex];
        LobbyUIManager.Instance?.OnNicknameChanged(slot, nickname);
        Debug.Log($"[Lobby] Player {slot.playerIndex + 1} → {nickname}");
    }

    private void SetReady(LobbyPlayerSlot slot)
    {
        slot.isReady = true;
        LobbyUIManager.Instance?.OnPlayerReady(slot);
        Debug.Log($"[Lobby] Player {slot.playerIndex + 1} Ready!");

        CheckAllReady();
    }

    private void CheckAllReady()
    {
        if (lobbyPlayers.Count < minPlayersToStart) return;

        foreach (var p in lobbyPlayers)
            if (!p.isReady) return;

        // 전원 Ready!
        allReadyPhase = true;
        LobbyUIManager.Instance?.OnAllReady();
        Debug.Log("[Lobby] 전원 Ready! Player 1 A버튼으로 시작");
    }

    // ───────────────────────────────────────────
    // 카운트다운 & 씬 전환
    // ───────────────────────────────────────────

    private IEnumerator StartCountdown()
    {
        countingDown = true;

        // ScriptableObject에 닉네임 저장
        SaveToSessionData();

        float remaining = countdownSeconds;

        while (remaining > 0f)
        {
            LobbyUIManager.Instance?.UpdateCountdown(Mathf.CeilToInt(remaining));
            remaining -= Time.deltaTime;
            yield return null;
        }

        LobbyUIManager.Instance?.UpdateCountdown(0);

        // 게임 씬 로드
        SceneManager.LoadScene(gameSceneName);
    }

    private void SaveToSessionData()
    {
        if (sessionData == null) return;

        sessionData.Clear();

        foreach (var slot in lobbyPlayers)
        {
            string nickname = nicknameList[slot.selectedNicknameIndex];
            sessionData.SetPlayer(slot.playerIndex, nickname, slot.isKeyboard);
        }

        Debug.Log("[Lobby] SessionData 저장 완료!");
    }

    public string GetNickname(int index)
    {
        if (index < 0 || index >= nicknameList.Count) return "???";
        return nicknameList[index];
    }
}

/// <summary>대기 씬에서 쓰는 간단한 플레이어 슬롯</summary>
[System.Serializable]
public class LobbyPlayerSlot
{
    public int playerIndex;
    public Gamepad gamepad;
    public bool isKeyboard;
    public int selectedNicknameIndex;
    public bool isReady;
}