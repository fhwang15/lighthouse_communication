using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 대기 씬 UI 전담
/// 
/// Canvas 구조 예시:
/// Canvas
/// ├── PlayerSlots (4개 패널)
/// │   ├── PlayerPanel_0  → nameText, nicknameText, statusText
/// │   ├── PlayerPanel_1
/// │   ├── PlayerPanel_2
/// │   └── PlayerPanel_3
/// ├── CountdownText (중앙)
/// ├── WaitingText ("Waiting for players...")
/// └── StartPromptText ("Player 1: Press A to Start!")
/// </summary>
public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance;

    [Header("플레이어 패널 (4개)")]
    [SerializeField] private GameObject[] playerPanels;         // 패널 오브젝트
    [SerializeField] private TextMeshProUGUI[] playerNameTexts;     // "Player 1"
    [SerializeField] private TextMeshProUGUI[] nicknameTexts;       // 선택한 닉네임
    [SerializeField] private TextMeshProUGUI[] statusTexts;         // "Selecting..." / "Ready!"

    [Header("전체 안내 텍스트")]
    [SerializeField] private TextMeshProUGUI waitingText;       // "Waiting for players..."
    [SerializeField] private TextMeshProUGUI startPromptText;   // "Player 1: Press A to Start!"
    [SerializeField] private TextMeshProUGUI countdownText;     // 카운트다운 숫자

    [Header("색상")]
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color selectingColor = Color.white;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 처음엔 전부 비활성화
        foreach (var panel in playerPanels)
            if (panel != null) panel.SetActive(false);

        if (startPromptText != null) startPromptText.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (waitingText != null) waitingText.text = "Press A / Enter to Join!";
    }

    // ───────────────────────────────────────────
    // LobbyManager가 호출
    // ───────────────────────────────────────────

    public void OnPlayerJoined(LobbyPlayerSlot slot)
    {
        int i = slot.playerIndex;
        if (i >= playerPanels.Length) return;

        playerPanels[i].SetActive(true);

        if (playerNameTexts[i] != null)
            playerNameTexts[i].text = $"Player {i + 1}";

        if (nicknameTexts[i] != null)
            nicknameTexts[i].text = LobbyManager.Instance.GetNickname(slot.selectedNicknameIndex);

        if (statusTexts[i] != null)
        {
            statusTexts[i].text = "Selecting...";
            statusTexts[i].color = selectingColor;
        }
    }

    public void OnNicknameChanged(LobbyPlayerSlot slot, string nickname)
    {
        int i = slot.playerIndex;
        if (i >= nicknameTexts.Length) return;

        if (nicknameTexts[i] != null)
            nicknameTexts[i].text = $"{nickname}";
    }


    public void OnPlayerReady(LobbyPlayerSlot slot)
    {
        int i = slot.playerIndex;
        if (i >= statusTexts.Length) return;

        if (nicknameTexts[i] != null)
            nicknameTexts[i].text = LobbyManager.Instance.GetNickname(slot.selectedNicknameIndex);

        if (statusTexts[i] != null)
        {
            statusTexts[i].text = "Ready!";
            statusTexts[i].color = readyColor;
        }
    }

    /// <summary>전원 Ready 시</summary>
    public void OnAllReady()
    {
        if (startPromptText != null)
        {
            startPromptText.gameObject.SetActive(true);
            startPromptText.text = "Press A to Start!";
        }
    }
       
    public void UpdateCountdown(int seconds)
    {
        if (countdownText == null) return;

        countdownText.gameObject.SetActive(true);

        if (seconds > 0)
            countdownText.text = seconds.ToString();
        else
            countdownText.text = "GO!";
    }
}