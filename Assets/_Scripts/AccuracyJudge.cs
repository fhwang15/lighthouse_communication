using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class AccuracyJudge : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerIndex;

    [Header("Connection")]
    [SerializeField] private NotePlayer notePlayer;

    [Header("판정 허용 오차")]
    [SerializeField] private float listenWindowExtra = 2.0f;

    [Header("시각 피드백")]
    public Material originalMaterial;
    public Renderer matRenderer;
    public Material flickeringMaterial;

    [Header("Test Mode")]
    [Tooltip("켜면 키보드 / 끄면 gamepad")]
    public bool testMode = true;
    public KeyCode testKey = KeyCode.Z;

    public float AverageErrorMs { get; private set; } = float.MaxValue;
    public bool IsFinished { get; private set; } = false;

    public UnityEvent<int, float> OnJudgeDone;

    private PlayerSlot assignedSlot;

    // press 타이밍만 기록 (판정용)
    private List<float> myPressTimestamps = new List<float>();
    private bool isListening = false;
    private float listenStartTime;
    private List<NoteData> referenceNotes;

    private void Start()
    {
        if (matRenderer != null && originalMaterial != null)
            matRenderer.material.color = originalMaterial.color;
    }

    public void SetPlayerSlot(PlayerSlot slot)
    {
        assignedSlot = slot;
    }

    public void StartListening()
    {
        if (notePlayer == null || notePlayer.CurrentNotes == null || notePlayer.CurrentNotes.Count == 0)
        {
            Debug.LogError($"[Judge {playerIndex}] NotePlayer 연결 안 됨 또는 노트 없음!");
            return;
        }

        if (!testMode && (assignedSlot == null || assignedSlot.gamepad == null))
        {
            Debug.LogError($"[Judge {playerIndex}] Gamepad 없음!");
            return;
        }

        // 색 리셋
        if (matRenderer != null && originalMaterial != null)
            matRenderer.material.color = originalMaterial.color;

        myPressTimestamps.Clear();
        IsFinished = false;
        AverageErrorMs = float.MaxValue;
        referenceNotes = notePlayer.CurrentNotes;

        isListening = true;
        listenStartTime = Time.time;

        // 마지막 노트 release 시간 + 여유 시간 후 자동 마감
        float lastRelease = referenceNotes[referenceNotes.Count - 1].releaseTime;
        StartCoroutine(AutoFinishCoroutine(lastRelease + listenWindowExtra));

        Debug.Log($"[Judge {playerIndex}] 모방 시작!");
    }

    public void StopListening()
    {
        isListening = false;
        StopAllCoroutines();
    }

    private void Update()
    {
        if (!isListening) return;

        bool pressed = false;
        bool released = false;

        if (testMode)
        {
            pressed = Input.GetKeyDown(testKey);
            released = Input.GetKeyUp(testKey);
        }
        else
        {
            if (assignedSlot?.gamepad == null) return;
            pressed = assignedSlot.gamepad.buttonSouth.wasPressedThisFrame;
            released = assignedSlot.gamepad.buttonSouth.wasReleasedThisFrame;
        }

        if (pressed) OnButtonPressed();
        if (released) OnButtonReleased();
    }

    private void OnButtonPressed()
    {
        // 시각 피드백
        if (matRenderer != null && flickeringMaterial != null)
            matRenderer.material.color = flickeringMaterial.color;

        float elapsed = Time.time - listenStartTime;
        myPressTimestamps.Add(elapsed);

        Debug.Log($"[Judge {playerIndex}] Press! t={elapsed:F3}s ({myPressTimestamps.Count}/{referenceNotes.Count})");

        if (myPressTimestamps.Count >= referenceNotes.Count)
        {
            isListening = false;
            StopAllCoroutines();
            CalculateScore();
        }
    }

    private void OnButtonReleased()
    {
        // Long press 끝 → 원래 색으로
        if (matRenderer != null && originalMaterial != null)
            matRenderer.material.color = originalMaterial.color;
    }

    private IEnumerator AutoFinishCoroutine(float deadline)
    {
        yield return new WaitForSeconds(deadline);

        if (isListening)
        {
            isListening = false;
            Debug.Log($"[Judge {playerIndex}] 시간 초과 → 강제 마감");
            CalculateScore();
        }
    }

    private void CalculateScore()
    {
        IsFinished = true;

        int refCount = referenceNotes.Count;
        int myCount = myPressTimestamps.Count;

        if (myCount == 0)
        {
            AverageErrorMs = float.MaxValue;
            Debug.Log($"[Judge {playerIndex}] 입력 없음");
            OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
            return;
        }

        // press 타이밍 오차만 계산
        float totalError = 0f;
        for (int i = 0; i < refCount; i++)
        {
            if (i < myCount)
            {
                float diffMs = Mathf.Abs(referenceNotes[i].pressTime - myPressTimestamps[i]) * 1000f;
                totalError += diffMs;
            }
            else
            {
                totalError += 1000f; // 패널티
            }
        }

        AverageErrorMs = totalError / refCount;
        Debug.Log($"[Judge {playerIndex}] 평균 오차: {AverageErrorMs:F1}ms");
        OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
    }
}