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

    [Header("이벤트")]
    public UnityEvent<List<float>> OnRecordingComplete;

    [SerializeField] private GameObject FlashObject;

    public NotePlayer npText;
    public TextMeshProUGUI numberText;

    [Header("Test Mode")]
    [Tooltip("켜면 키보드 Space / 끄면 Ship 플레이어 gamepad")]
    public bool testMode = false;

    private List<float> noteTimestamps = new List<float>();
    private int targetNoteCount;
    private bool isRecording = false;
    private float recordingStartTime;

    // Ship 플레이어 정보
    private Gamepad shipGamepad;
    private bool shipIsKeyboard = false; // Ship이 키보드 플레이어면 true

    public List<float> RecordedTimestamps => noteTimestamps;

    public void StartRecording()
    {
        noteTimestamps.Clear();

        if (testMode)
        {
            // 테스트: 키보드 Space 사용
            shipIsKeyboard = true;
            Debug.Log("[NoteRecorder] 테스트 모드 - 키보드 Space로 입력");
        }
        else
        {
            // RoleManager에서 Ship 플레이어 찾기
            if (RoleManager.Instance == null)
            {
                Debug.LogError("[NoteRecorder] RoleManager 없음!");
                return;
            }

            int shipIndex = RoleManager.Instance.ShipPlayerIndex;

            if (shipIndex < 0 || shipIndex >= GameManager.Instance.players.Count)
            {
                Debug.LogError("[NoteRecorder] Ship 플레이어 인덱스 오류!");
                return;
            }

            var shipSlot = GameManager.Instance.players[shipIndex];

            if (shipSlot.gamepad == null)
            {
                // Ship이 키보드 플레이어인 경우
                shipIsKeyboard = true;
                Debug.Log($"[NoteRecorder] Ship = Player {shipIndex + 1} (키보드 Space)");
            }
            else
            {
                // Ship이 컨트롤러 플레이어인 경우
                shipIsKeyboard = false;
                shipGamepad = shipSlot.gamepad;
                Debug.Log($"[NoteRecorder] Ship = Player {shipIndex + 1} (Gamepad {shipGamepad.name})");
            }
        }

        if (npText != null) npText.phaseText.text = "Signal Recording...";

        targetNoteCount = Random.Range(minNotes, maxNotes + 1);
        if (numberText != null) numberText.text = targetNoteCount.ToString();

        isRecording = true;
        recordingStartTime = Time.time;

        Debug.Log($"[녹화 시작] 목표 {targetNoteCount}개");
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        FinishRecording();
    }

    private void Update()
    {
        if (!isRecording) return;

        bool pressed = false;
        bool released = false;

        if (testMode || shipIsKeyboard)
        {
            // 키보드 Space
            pressed = Input.GetKeyDown(KeyCode.Space);
            released = Input.GetKeyUp(KeyCode.Space);
        }
        else
        {
            // 컨트롤러 A버튼
            if (shipGamepad == null) return;
            pressed = shipGamepad.buttonSouth.wasPressedThisFrame;
            released = shipGamepad.buttonSouth.wasReleasedThisFrame;
        }

        if (pressed)
        {
            if (FlashObject != null) FlashObject.SetActive(true);
            RegisterNote();
        }
        if (released)
        {
            if (FlashObject != null) FlashObject.SetActive(false);
        }
    }

    private void RegisterNote()
    {
        float elapsed = Time.time - recordingStartTime;
        noteTimestamps.Add(elapsed);
        Debug.Log($"[노트 {noteTimestamps.Count}/{targetNoteCount}] {elapsed:F3}초");

        if (noteTimestamps.Count >= targetNoteCount)
        {
            isRecording = false;
            if (FlashObject != null) FlashObject.SetActive(false);
            FinishRecording();
        }
    }

    private void FinishRecording()
    {
        if (noteTimestamps.Count == 0)
        {
            if (npText != null) npText.phaseText.text = "No input!";
            return;
        }

        Debug.Log($"[녹화 완료] {noteTimestamps.Count}개 노트 저장됨");
        OnRecordingComplete?.Invoke(noteTimestamps);
    }
}