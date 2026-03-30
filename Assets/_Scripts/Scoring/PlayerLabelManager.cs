using UnityEngine;
using TMPro;

public class PlayerLabelManager : MonoBehaviour
{
    public static PlayerLabelManager Instance;

    [Header("InGameCanvas")]
    [SerializeField] private GameObject inGameCanvas;

    [Header("닉네임 텍스트 (Ship, LH0, LH1, LH2 순서)")]
    [SerializeField] private TextMeshProUGUI[] nicknameTexts; // 4개

    [Header("점수 텍스트 (Ship, LH0, LH1, LH2 순서)")]
    [SerializeField] private TextMeshProUGUI[] scoreTexts;    // 4개

    [Header("Ship 패널 추적 설정")]
    [SerializeField] private RectTransform shipPanel;         // Ship 패널 RectTransform
    [SerializeField] private Transform shipTransform;         // 배 오브젝트 Transform
    [SerializeField] private Vector2 shipPanelOffset = new Vector2(0, 60f); // 배 위로 얼마나

    [Header("점수 색상")]
    [SerializeField] private Color positiveColor = Color.yellow;
    [SerializeField] private Color negativeColor = Color.red;
    [SerializeField] private Color neutralColor = Color.white;

    private int[] playerToSlot = new int[4];
    private Camera mainCam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        mainCam = Camera.main;
    }

    // ───────────────────────────────────────────
    // 매 프레임: Ship 패널 위치 업데이트
    // ───────────────────────────────────────────

    private void LateUpdate()
    {
        if (shipPanel == null || shipTransform == null || mainCam == null) return;

        // 배의 월드 좌표 → 스크린 좌표 변환
        Vector3 screenPos = mainCam.WorldToScreenPoint(shipTransform.position);

        // 화면 밖으로 나가면 숨기기
        if (screenPos.z < 0)
        {
            shipPanel.gameObject.SetActive(false);
            return;
        }

        shipPanel.gameObject.SetActive(true);

        // Ship 패널 위치 = 배의 스크린 좌표 + 오프셋
        shipPanel.position = new Vector3(
            screenPos.x + shipPanelOffset.x,
            screenPos.y + shipPanelOffset.y,
            0f
        );
    }

    // ───────────────────────────────────────────
    // 외부에서 호출
    // ───────────────────────────────────────────

    public void SetupLabels()
    {
        if (RoleManager.Instance == null)
        {
            Debug.LogError("[PlayerLabelManager] RoleManager 없음!");
            return;
        }

        if (inGameCanvas != null) inGameCanvas.SetActive(true);

        int shipIdx = RoleManager.Instance.ShipPlayerIndex;
        var lighthousePlayers = RoleManager.Instance.LighthousePlayerIndices;

        // Ship → slot 0
        playerToSlot[shipIdx] = 0;
        SetLabel(0, GetNickname(shipIdx), "Ship", 0);

        // Lighthouse → slot 1, 2, 3
        for (int i = 0; i < lighthousePlayers.Count; i++)
        {
            int playerIdx = lighthousePlayers[i];
            playerToSlot[playerIdx] = i + 1;
            SetLabel(i + 1, GetNickname(playerIdx), $"Lighthouse {i + 1}", 0);
        }

        Debug.Log("[PlayerLabelManager] 라벨 세팅 완료!");
    }

    public void UpdateScore(int playerIndex, int score)
    {
        int slot = playerToSlot[playerIndex];
        if (slot >= scoreTexts.Length || scoreTexts[slot] == null) return;

        scoreTexts[slot].text = $"{score}";
        scoreTexts[slot].color = score > 0 ? positiveColor
                               : score < 0 ? negativeColor
                               : neutralColor;
    }

    // ───────────────────────────────────────────
    // 헬퍼
    // ───────────────────────────────────────────

    private void SetLabel(int slot, string nickname, string role, int score)
    {
        if (slot < nicknameTexts.Length && nicknameTexts[slot] != null)
            nicknameTexts[slot].text = $"{nickname}: {role}";

        if (slot < scoreTexts.Length && scoreTexts[slot] != null)
        {
            scoreTexts[slot].text = $"{score}";
            scoreTexts[slot].color = neutralColor;
        }
    }

    private string GetNickname(int playerIndex)
    {
        var sessionData = GameSessionBridge.Instance?.SessionData;
        if (sessionData != null)
            return sessionData.GetNickname(playerIndex);

        return $"Player {playerIndex + 1}";
    }
}