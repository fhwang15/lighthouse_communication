using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("Character Options")]
    [SerializeField] private List<GameObject> characterPrefabs = new List<GameObject>();

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Rhythm Game")]
    [SerializeField] private RhythmGameManager rhythmGameManager;

    [Header("카운트다운")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownSeconds = 10f;

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.gameStarted) return;

        var players = GameManager.Instance.players;

        for (int i = 0; i < players.Count; i++)
        {
            HandleInput(i);
        }
    }

    private void HandleInput(int index)
    {
        PlayerSlot player = GameManager.Instance.players[index];
        if (player == null) return;

        if (player.gamepad == null)
        {
            HandleKeyboardInput(index, player);
            return;
        }

        if (player.isLocked) return;

        if (player.gamepad.dpad.left.wasPressedThisFrame)
            CycleCharacter(index, -1);

        if (player.gamepad.dpad.right.wasPressedThisFrame)
            CycleCharacter(index, 1);

        if (player.gamepad.buttonSouth.wasPressedThisFrame)
            LockPlayer(index);
    }

    private void HandleKeyboardInput(int index, PlayerSlot player)
    {
        if (player.isLocked) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            CycleCharacter(index, -1);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            CycleCharacter(index, 1);

        if (Input.GetKeyDown(KeyCode.Space))
            LockPlayer(index);
    }

    private void CycleCharacter(int playerIndex, int direction)
    {
        var players = GameManager.Instance.players;

        if (playerIndex < 0 || playerIndex >= players.Count) return;

        PlayerSlot player = players[playerIndex];
        if (player.isLocked) return;

        player.selectedIndex += direction;

        if (player.selectedIndex < 0)
            player.selectedIndex = characterPrefabs.Count - 1;

        if (player.selectedIndex >= characterPrefabs.Count)
            player.selectedIndex = 0;

        SpawnPlayerLobbyVisuals(playerIndex);
    }

    private void LockPlayer(int playerIndex)
    {
        PlayerSlot player = GameManager.Instance.players[playerIndex];
        if (player == null) return;

        player.isLocked = true;

        if (player.playerCharacterController != null)
            player.playerCharacterController.SetLine2("Ready!");

        CheckAllLocked();
    }

    public void SpawnPlayerLobbyVisuals(int playerIndex)
    {
        var players = GameManager.Instance.players;

        if (playerIndex < 0 || playerIndex >= players.Count) return;

        PlayerSlot player = players[playerIndex];

        if (playerIndex >= spawnPoints.Length)
        {
            Debug.LogWarning("Not enough spawn points assigned.");
            return;
        }

        if (characterPrefabs.Count == 0) return;

        if (player.selectedIndex < 0 || player.selectedIndex >= characterPrefabs.Count)
            player.selectedIndex = 0;

        if (player.currentAvatar != null)
        {
            Destroy(player.currentAvatar);
            player.currentAvatar = null;
        }

        GameObject prefab = characterPrefabs[player.selectedIndex];
        GameObject instance = Instantiate(
            prefab,
            spawnPoints[playerIndex].position,
            spawnPoints[playerIndex].rotation
        );

        PlayerCharacterController controller = instance.GetComponent<PlayerCharacterController>();

        if (controller == null)
        {
            Debug.LogError("Prefab missing PlayerCharacterController");
            return;
        }

        player.playerCharacterController = controller;
        player.currentAvatar = instance;

        controller.playerSlot = player;
        controller.SetLine1("Player " + (playerIndex + 1));
        controller.SetLine2(player.isLocked ? "Ready!" : "Selecting...");
    }

    private void CheckAllLocked()
    {
        var players = GameManager.Instance.players;

        if (players.Count < 2) return;

        foreach (var player in players)
        {
            if (!player.isLocked) return;
        }

        // 전원 Ready → 카운트다운 시작!
        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {

        GameManager.Instance.movementEnabled = true;


        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        float remaining = countdownSeconds;

        while (remaining > 0f)
        {
            // 숫자 표시 (소수점 없이)
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remaining).ToString();

            remaining -= Time.deltaTime;
            yield return null;
        }

        // 0! 표시 후 잠깐 대기
        if (countdownText != null)
            countdownText.text = "GO!";

        yield return new WaitForSeconds(0.5f);

        // 카운트다운 텍스트 숨기기
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        StartGame();
    }

    private void StartGame()
    {
        GameManager.Instance.gameStarted = true;
        GameManager.Instance.movementEnabled = false;

        // 역할 랜덤 배정
        if (RoleManager.Instance != null)
            RoleManager.Instance.AssignRoles();
        else
            Debug.LogError("RoleManager 없음!");

        // 카메라 전환
        FindObjectOfType<PartyCameraController>()?.TransitionToGame();

        // 리듬 게임 시작
        if (rhythmGameManager != null)
            rhythmGameManager.StartGame();
        else
            Debug.LogError("RhythmGameManager 연결 안 됨!");
    }
}