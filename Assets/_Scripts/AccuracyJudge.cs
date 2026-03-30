using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class AccuracyJudge : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerIndex; // 0=B, 1=C, 2=D

    [Header("Connection")]
    [SerializeField] private NotePlayer notePlayer;

    [Header("판정 허용 오차")]
    [SerializeField] private float listenWindowExtra = 2.0f;

    [Header("시각 피드백")]
    public Material originalMaterial;
    public Renderer matRenderer;
    public Material flickeringMaterial;

    [Header("Test Mode")]
    public bool testMode = true;
    public KeyCode testKey = KeyCode.Z;

    public float AverageErrorMs { get; private set; } = float.MaxValue;
    public bool IsFinished { get; private set; } = false;

    public UnityEvent<int, float> OnJudgeDone;

    private PlayerSlot assignedSlot;
    private List<float> myTimestamps = new List<float>();
    private bool isListening = false;
    private float listenStartTime;
    private List<float> referenceTimestamps;

    private void Start()
    {
        if (matRenderer != null && originalMaterial != null)
            matRenderer.material.color = originalMaterial.color;
    }

    public void SetPlayerSlot(PlayerSlot slot) => assignedSlot = slot;

    public void StartListening()
    {
        if (notePlayer == null || notePlayer.CurrentNoteTimestamps == null || notePlayer.CurrentNoteTimestamps.Count == 0)
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

        myTimestamps.Clear();
        IsFinished = false;
        AverageErrorMs = float.MaxValue;
        referenceTimestamps = notePlayer.CurrentNoteTimestamps;

        isListening = true;
        listenStartTime = Time.time;

        float deadline = referenceTimestamps[referenceTimestamps.Count - 1] + listenWindowExtra;
        StartCoroutine(AutoFinishCoroutine(deadline));

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

        bool pressed = testMode
            ? Input.GetKeyDown(testKey)
            : assignedSlot?.gamepad?.buttonSouth.wasPressedThisFrame ?? false;

        bool released = testMode
            ? Input.GetKeyUp(testKey)
            : assignedSlot?.gamepad?.buttonSouth.wasReleasedThisFrame ?? false;

        if (pressed) OnButtonPressed();
        if (released) OnButtonReleased();
    }

    private void OnButtonPressed()
    {
        if (matRenderer != null && flickeringMaterial != null)
            matRenderer.material.color = flickeringMaterial.color;

        float elapsed = Time.time - listenStartTime;
        myTimestamps.Add(elapsed);

        // 악보 UI에 노트 표시
        RhythmScoreUI.Instance?.AddLighthouseNote(playerIndex, elapsed);

        Debug.Log($"[Judge {playerIndex}] Press! t={elapsed:F3}s ({myTimestamps.Count}/{referenceTimestamps.Count})");

        if (myTimestamps.Count >= referenceTimestamps.Count)
        {
            isListening = false;
            StopAllCoroutines();
            CalculateScore();
        }
    }

    private void OnButtonReleased()
    {
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

        int refCount = referenceTimestamps.Count;
        int myCount = myTimestamps.Count;

        if (myCount == 0)
        {
            AverageErrorMs = float.MaxValue;
            OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
            return;
        }

        float totalError = 0f;
        for (int i = 0; i < refCount; i++)
        {
            totalError += i < myCount
                ? Mathf.Abs(referenceTimestamps[i] - myTimestamps[i]) * 1000f
                : 1000f;
        }

        AverageErrorMs = totalError / refCount;
        Debug.Log($"[Judge {playerIndex}] 평균 오차: {AverageErrorMs:F1}ms");
        OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
    }
}