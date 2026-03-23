using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 노트 하나 = press 시작 시간 + release 시간
/// → 판정은 press 시작 타이밍만 사용
/// → 시각적으로는 release까지 flash 유지
/// </summary>
[System.Serializable]
public class NoteData
{
    public float pressTime;    // 버튼 누른 시간 (t=0 기준)
    public float releaseTime;  // 버튼 뗀 시간 (long press 시각 효과용)

    public NoteData(float press, float release)
    {
        pressTime = press;
        releaseTime = release;
    }
}

public class NoteRecorder : MonoBehaviour
{
    [Header("노트 개수 설정")]
    [SerializeField] private int minNotes = 3;
    [SerializeField] private int maxNotes = 5;

    [Header("Recording 타이머")]
    [SerializeField] public float recordingTimeLimit = 10f; // Inspector에서 조절
    [SerializeField] private TextMeshProUGUI timerText;     // 타이머 표시 TMP (선택)

    [Header("이벤트")]
    public UnityEvent<List<NoteData>> OnRecordingComplete;

    [SerializeField] private GameObject FlashObject;

    public NotePlayer npText;
    public TextMeshProUGUI numberText;

    [Header("Test Mode")]
    [Tooltip("켜면 키보드 Space / 끄면 Ship gamepad")]
    public bool testMode = false;

    // 내부 상태
    private List<NoteData> notes = new List<NoteData>();
    private int targetNoteCount;
    private bool isRecording = false;

    // ── 핵심 변경: 첫 버튼 누른 순간을 t=0으로 ──
    private float firstPressTime = -1f;  // 첫 버튼 누른 Time.time
    private float currentPressStart = -1f; // 현재 누르고 있는 버튼의 시작 시간

    private Gamepad shipGamepad;
    private bool shipIsKeyboard = false;

    // 타이머 코루틴
    private Coroutine timerCoroutine;

    public List<NoteData> RecordedNotes => notes;

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    public void StartRecording()
    {
        notes.Clear();
        firstPressTime = -1f;
        currentPressStart = -1f;

        if (testMode)
        {
            shipIsKeyboard = true;
        }
        else
        {
            if (RoleManager.Instance == null)
            {
                Debug.LogError("[NoteRecorder] RoleManager 없음!");
                return;
            }

            int shipIndex = RoleManager.Instance.ShipPlayerIndex;
            var shipSlot = GameManager.Instance.players[shipIndex];

            if (shipSlot.gamepad == null)
            {
                shipIsKeyboard = true;
                Debug.Log($"[NoteRecorder] Ship = Player {shipIndex + 1} (키보드)");
            }
            else
            {
                shipIsKeyboard = false;
                shipGamepad = shipSlot.gamepad;
                Debug.Log($"[NoteRecorder] Ship = Player {shipIndex + 1} (Gamepad)");
            }
        }

        if (npText != null) npText.phaseText.text = "Signal Recording...";

        targetNoteCount = Random.Range(minNotes, maxNotes + 1);
        if (numberText != null) numberText.text = targetNoteCount.ToString();

        isRecording = true;

        // 타이머 시작
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(RecordingTimerCoroutine());

        Debug.Log($"[녹화 시작] 목표 {targetNoteCount}개 | 제한 {recordingTimeLimit}초");
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        FinishRecording(false);
    }

    // ───────────────────────────────────────────
    // 타이머
    // ───────────────────────────────────────────

    private IEnumerator RecordingTimerCoroutine()
    {
        float remaining = recordingTimeLimit;

        while (remaining > 0f)
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(remaining).ToString();

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (timerText != null) timerText.text = "0";

        // 시간 초과!
        if (isRecording)
        {
            isRecording = false;
            Debug.Log("[NoteRecorder] 시간 초과! 페널티");
            FinishRecording(true); // true = 시간 초과 페널티
        }
    }

    // ───────────────────────────────────────────
    // 입력 감지
    // ───────────────────────────────────────────

    private void Update()
    {
        if (!isRecording) return;

        bool pressed = false;
        bool released = false;

        if (testMode || shipIsKeyboard)
        {
            pressed = Input.GetKeyDown(KeyCode.Space);
            released = Input.GetKeyUp(KeyCode.Space);
        }
        else
        {
            if (shipGamepad == null) return;
            pressed = shipGamepad.buttonSouth.wasPressedThisFrame;
            released = shipGamepad.buttonSouth.wasReleasedThisFrame;
        }

        if (pressed) OnButtonPressed();
        if (released) OnButtonReleased();
    }

    private void OnButtonPressed()
    {
        if (FlashObject != null) FlashObject.SetActive(true);

        // 첫 버튼 누른 순간 기록 (이게 t=0!)
        if (firstPressTime < 0f)
            firstPressTime = Time.time;

        // 현재 press 시작 시간 기록 (t=0 기준 상대 시간)
        currentPressStart = Time.time - firstPressTime;

        Debug.Log($"[노트 Press] t={currentPressStart:F3}초");
    }

    private void OnButtonReleased()
    {
        if (FlashObject != null) FlashObject.SetActive(false);

        // press가 시작된 적 없으면 무시
        if (currentPressStart < 0f) return;

        float releaseTime = Time.time - firstPressTime;
        NoteData note = new NoteData(currentPressStart, releaseTime);
        notes.Add(note);
        currentPressStart = -1f;

        Debug.Log($"[노트 {notes.Count}/{targetNoteCount}] press={note.pressTime:F3}s release={note.releaseTime:F3}s 길이={(note.releaseTime - note.pressTime):F3}s");

        if (notes.Count >= targetNoteCount)
        {
            isRecording = false;
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            if (timerText != null) timerText.text = "";
            FinishRecording(false);
        }
    }

    private void FinishRecording(bool isTimeout)
    {
        if (FlashObject != null) FlashObject.SetActive(false);

        if (notes.Count == 0)
        {
            if (npText != null) npText.phaseText.text = isTimeout ? "Time Out!" : "No input!";
            // 노트가 없어도 일단 다음 라운드로 (빈 리스트 전달)
            OnRecordingComplete?.Invoke(notes);
            return;
        }

        Debug.Log($"[녹화 완료] {notes.Count}개 노트 | 시간초과: {isTimeout}");
        OnRecordingComplete?.Invoke(notes);
    }
}