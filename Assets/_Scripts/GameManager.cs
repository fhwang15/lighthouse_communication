using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Central authority for player detection and session management
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    [SerializeField] private CharacterSelectManager characterSelectManager;

    [Header("Settings")]
    public int maxPlayers = 4;
    public bool gameStarted = false;
    public bool debugDisplay = false;

    [Header("Debug / Test")]
    public bool testMode = false;

    public bool movementEnabled = false;


    public List<PlayerSlot> players = new List<PlayerSlot>();

    // 키보드 플레이어가 이미 조인했는지 체크
    private bool keyboardPlayerJoined = false;

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
        if (characterSelectManager == null)
        {
            Debug.LogError("CharacterSelectManager is not assigned.");
            return;
        }

        DetectExistingGamepads();
    }

    private void Update()
    {
        PollForNewControllers();

        // Enter 키로 키보드 플레이어 조인
        if (testMode && !keyboardPlayerJoined && !gameStarted && players.Count < maxPlayers)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                AddKeyboardPlayer();
            }
        }

        if (debugDisplay && players.Count > 0)
            DebugControllerState(players[0]);
    }

    private void DetectExistingGamepads()
    {
        foreach (var pad in Gamepad.all)
            AddGamepadPlayer(pad);
    }

    private void PollForNewControllers()
    {
        foreach (var pad in Gamepad.all)
        {
            bool alreadyAdded = false;

            foreach (var player in players)
            {
                if (player.gamepad != null && player.gamepad == pad)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
                AddGamepadPlayer(pad);
        }
    }

    // 컨트롤러 플레이어 추가
    private void AddGamepadPlayer(Gamepad pad)
    {

        Debug.Log($"디바이스 정보: name={pad.name} | interface={pad.description.interfaceName} | manufacturer={pad.description.manufacturer} | deviceClass={pad.description.deviceClass}");


        if (players.Count >= maxPlayers) return;


        if (pad.description.interfaceName == "Virtual")
        {
            Debug.Log($"[GameManager] 가상 디바이스 스킵: {pad.name}");
            return;
        }

        PlayerSlot newPlayer = new PlayerSlot(pad);
        players.Add(newPlayer);

        Debug.Log($"[GameManager] 컨트롤러 플레이어 추가! ({pad.name}) 총 {players.Count}명");
        characterSelectManager.SpawnPlayerLobbyVisuals(players.Count - 1);
    }

    // 키보드 플레이어 추가 (gamepad = null)
    private void AddKeyboardPlayer()
    {
        if (players.Count >= maxPlayers) return;

        // gamepad 없이 PlayerSlot 생성 (null 전달)
        PlayerSlot newPlayer = new PlayerSlot(null);
        players.Add(newPlayer);
        keyboardPlayerJoined = true;

        Debug.Log($"[GameManager] 키보드 플레이어 추가! 총 {players.Count}명");
        characterSelectManager.SpawnPlayerLobbyVisuals(players.Count - 1);
    }

    private void DebugControllerState(PlayerSlot player)
    {
        if (player == null || player.gamepad == null) return;

        Gamepad pad = player.gamepad;
        string state = "Gamepad State\n";
        state += "A: " + pad.buttonSouth.isPressed + "\n";
        state += "B: " + pad.buttonEast.isPressed + "\n";
        state += "Start: " + pad.startButton.isPressed + "\n";
        Debug.Log(state);
    }
}