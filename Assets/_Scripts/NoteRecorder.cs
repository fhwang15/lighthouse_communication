using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class NoteRecorder : MonoBehaviour
{
    [Header("노트 개수 설정")]
    [SerializeField] private int minNotes = 3;
    [SerializeField] private int maxNotes = 5;

    [Header("Recording 타이머")]
    [SerializeField] public float recordingTimeLimit = 10f;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("이벤트")]
    public UnityEvent<List<float>> OnRecordingComplete;

    [SerializeField] private GameObject flashObject;

    public NotePlayer npText;
    public TextMeshProUGUI numberText;

    [Header("Test Mode")]
    public bool testMode = false;

    private List<float> noteTimestamps = new List<float>();
    private int targetNoteCount;
    private bool isRecording = false;
    private float firstPressTime = -1f;
    private Coroutine timerCoroutine;

    private Gamepad shipGamepad;
    private bool shipIsKeyboard = false;

    public List<float> RecordedTimestamps => noteTimestamps;

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    public void StartRecording()
    {
        noteTimestamps.Clear();
        firstPressTime = -1f;

        if (testMode)
        {
            shipIsKeyboard = true;
        }
        else
        {
            if (RoleManager.Instance == null) { Debug.LogError("[NoteRecorder] RoleManager 없음!"); return; }

            int shipIndex = RoleManager.Instance.ShipPlayerIndex;
            var shipSlot = GameManager.Instance.players[shipIndex];

            shipIsKeyboard = shipSlot.gamepad == null;
            if (!shipIsKeyboard) shipGamepad = shipSlot.gamepad;

            Debug.Log($"[NoteRecorder] Ship = Player {shipIndex + 1} ({(shipIsKeyboard ? "키보드" : shipGamepad.name)})");
        }

        if (npText != null) npText.phaseText.text = "Signal Recording...";

        targetNoteCount = Random.Range(minNotes, maxNotes + 1);
        if (numberText != null) numberText.text = targetNoteCount.ToString();

        isRecording = true;

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
            if (timerText != null) timerText.text = Mathf.CeilToInt(remaining).ToString();
            remaining -= Time.deltaTime;
            yield return null;
        }

        if (timerText != null) timerText.text = "0";

        if (isRecording)
        {
            isRecording = false;
            Debug.Log("[NoteRecorder] 시간 초과!");
            FinishRecording(true);
        }
    }

    // ───────────────────────────────────────────
    // 입력 감지 (press 시작 시간만 기록)
    // ───────────────────────────────────────────

    private void Update()
    {
        if (!isRecording) return;

        bool pressed = testMode || shipIsKeyboard
            ? Input.GetKeyDown(KeyCode.Space)
            : shipGamepad != null && shipGamepad.buttonSouth.wasPressedThisFrame;

        if (!pressed) return;

        if (flashObject != null) flashObject.SetActive(true);
        StartCoroutine(FlashOff());

        // 첫 버튼 누른 순간 = t=0
        if (firstPressTime < 0f) firstPressTime = Time.time;

        float elapsed = Time.time - firstPressTime;
        noteTimestamps.Add(elapsed);

        // 악보 UI에 노트 표시
        RhythmScoreUI.Instance?.AddShipNote(elapsed);

        Debug.Log($"[노트 {noteTimestamps.Count}/{targetNoteCount}] t={elapsed:F3}s");

        if (noteTimestamps.Count >= targetNoteCount)
        {
            isRecording = false;
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            if (timerText != null) timerText.text = "";
            FinishRecording(false);
        }
    }

    private IEnumerator FlashOff()
    {
        yield return new WaitForSeconds(0.1f);
        if (flashObject != null) flashObject.SetActive(false);
    }

    private void FinishRecording(bool isTimeout)
    {
        if (flashObject != null) flashObject.SetActive(false);

        if (noteTimestamps.Count == 0)
        {
            if (npText != null) npText.phaseText.text = isTimeout ? "Time Out!" : "No input!";
            OnRecordingComplete?.Invoke(noteTimestamps);
            return;
        }

        Debug.Log($"[녹화 완료] {noteTimestamps.Count}개 | 시간초과: {isTimeout}");
        OnRecordingComplete?.Invoke(noteTimestamps);
    }
}