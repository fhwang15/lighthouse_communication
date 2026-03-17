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

    // ── 테스트 모드 ──────────────────────────────
    [Header("Test Mode")]
    [Tooltip("켜면 키보드 사용 / 끄면 gamepad 사용")]
    public bool testMode = true;

    [Tooltip("testMode=true 일 때 사용할 키 (B=Z, C=X, D=C)")]
    public KeyCode testKey = KeyCode.Z;
    // ────────────────────────────────────────────

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

    public void SetPlayerSlot(PlayerSlot slot)
    {
        assignedSlot = slot;
    }

    public void StartListening()
    {

        if (matRenderer != null && originalMaterial != null)
        {
            matRenderer.material.color = originalMaterial.color;
        }

        if (notePlayer == null || notePlayer.CurrentNoteTimestamps == null)
        {
            Debug.LogError($"[Judge {playerIndex}] NotePlayer 연결 안 됨!");
            return;
        }

        // testMode=false 일 때만 gamepad 체크
        if (!testMode && (assignedSlot == null || assignedSlot.gamepad == null))
        {
            Debug.LogError($"[Judge {playerIndex}] Gamepad 없음! testMode를 켜거나 컨트롤러 연결해.");
            return;
        }

        myTimestamps.Clear();
        IsFinished = false;
        AverageErrorMs = float.MaxValue;
        referenceTimestamps = notePlayer.CurrentNoteTimestamps;

        isListening = true;
        listenStartTime = Time.time;

        float deadline = referenceTimestamps[referenceTimestamps.Count - 1] + listenWindowExtra;
        StartCoroutine(AutoFinishCoroutine(deadline));

        string inputInfo = testMode ? $"키보드 {testKey}" : $"Gamepad {assignedSlot.gamepad.name}";
        Debug.Log($"[Judge {playerIndex}] 모방 시작! 입력: {inputInfo}");
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
            // 키보드 입력
            pressed = Input.GetKeyDown(testKey);
            released = Input.GetKeyUp(testKey);
        }
        else
        {
            // 컨트롤러 입력
            if (assignedSlot?.gamepad == null) return;
            pressed = assignedSlot.gamepad.buttonSouth.wasPressedThisFrame;
            released = assignedSlot.gamepad.buttonSouth.wasReleasedThisFrame;
        }

        if (pressed) OnButtonPressed();
        if (released) OnButtonReleased();
    }

    private void OnButtonPressed()
    {
        if (matRenderer != null && flickeringMaterial != null)
            matRenderer.material.color = flickeringMaterial.color;

        float elapsed = Time.time - listenStartTime;
        myTimestamps.Add(elapsed);
        Debug.Log($"[Judge {playerIndex}] 입력! {elapsed:F3}초 ({myTimestamps.Count}/{referenceTimestamps.Count})");

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
            Debug.Log($"[Judge {playerIndex}] 입력 없음");
            OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
            return;
        }

        float totalError = 0f;
        for (int i = 0; i < refCount; i++)
        {
            if (i < myCount)
            {
                float diffMs = Mathf.Abs(referenceTimestamps[i] - myTimestamps[i]) * 1000f;
                totalError += diffMs;
            }
            else
            {
                totalError += 1000f;
            }
        }

        AverageErrorMs = totalError / refCount;
        Debug.Log($"[Judge {playerIndex}] 평균 오차: {AverageErrorMs:F1}ms");
        OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
    }
}