using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RhythmScoreUI : MonoBehaviour
{
    public static RhythmScoreUI Instance;

    [Header("라인 컨테이너 (Ship, LH0, LH1, LH2)")]
    [SerializeField] private RectTransform[] lineContainers; // 4개

    [Header("타임바")]
    [SerializeField] private RectTransform timerBar; // 얇은 세로 Image

    [Header("노트 프리팹 (작은 원형 Image)")]
    [SerializeField] private GameObject notePrefab;

    [Header("색상")]
    [SerializeField] private Color shipNoteColor = Color.white;
    [SerializeField]
    private Color[] lighthouseNoteColors = {
        new Color(1f, 0.5f, 0.5f),
        new Color(0.5f, 1f, 0.5f),
        new Color(0.5f, 0.5f, 1f)
    };
    [SerializeField] private Color timerBarColor = Color.yellow;

    [Header("악보 설정")]
    [SerializeField] private float timeWindow = 5f; // 악보에 표시할 시간 범위 (초)

    // 내부 상태
    private List<GameObject> spawnedNotes = new List<GameObject>();
    private float phaseStartTime = -1f;
    private bool timerRunning = false;
    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 타임바 색상 설정
        if (timerBar != null)
        {
            var img = timerBar.GetComponent<Image>();
            if (img != null) img.color = timerBarColor;

            // 타임바 왼쪽 끝에서 시작
            MoveTimerBar(0f);
        }
    }

    // ───────────────────────────────────────────
    // 외부에서 호출
    // ───────────────────────────────────────────

    /// <summary>라운드 시작 시 전체 초기화</summary>
    public void ResetBoard()
    {
        // 노트 전부 삭제
        foreach (var note in spawnedNotes)
            if (note != null) Destroy(note);
        spawnedNotes.Clear();

        // 타임바 왼쪽으로
        StopTimer();
        MoveTimerBar(0f);
    }

    /// <summary>Recording 페이즈 시작 시 호출 (NoteRecorder에서)</summary>
    public void StartRecordingPhase(float duration)
    {
        ResetBoard();
        timeWindow = duration;
        StartTimer(duration);
    }

    /// <summary>재생 페이즈 시작 시 호출 (NotePlayer에서)</summary>
    public void PreparePlayback(float duration)
    {
        // 노트는 유지, 타임바만 리셋
        StopTimer();
        MoveTimerBar(0f);
        timeWindow = duration;
        StartTimer(duration);
    }

    /// <summary>모방 페이즈 시작 시 호출 (RhythmGameManager에서)</summary>
    public void StartMimicPhase(float duration)
    {
        // Ship 노트 유지, LH 노트만 삭제, 타임바 리셋
        StopTimer();
        MoveTimerBar(0f);
        timeWindow = duration;
        StartTimer(duration);
    }

    /// <summary>배 플레이어 노트 (NoteRecorder에서)</summary>
    public void AddShipNote(float timestamp)
    {
        SpawnNote(0, timestamp, shipNoteColor);
    }

    /// <summary>등대 플레이어 노트 (AccuracyJudge에서)</summary>
    public void AddLighthouseNote(int judgeIndex, float timestamp)
    {
        int lineIndex = judgeIndex + 1;
        Color color = judgeIndex < lighthouseNoteColors.Length
            ? lighthouseNoteColors[judgeIndex]
            : Color.white;

        SpawnNote(lineIndex, timestamp, color);
    }

    // ───────────────────────────────────────────
    // 타임바
    // ───────────────────────────────────────────

    private void StartTimer(float duration)
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerCoroutine(duration));
    }

    private void StopTimer()
    {
        timerRunning = false;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private IEnumerator TimerCoroutine(float duration)
    {
        timerRunning = true;
        phaseStartTime = Time.time;

        while (timerRunning)
        {
            float elapsed = Time.time - phaseStartTime;
            float t = Mathf.Clamp01(elapsed / duration);

            MoveTimerBar(t);

            if (elapsed >= duration)
            {
                timerRunning = false;
                yield break;
            }

            yield return null;
        }
    }

    private void MoveTimerBar(float t)
    {
        if (timerBar == null || lineContainers.Length == 0) return;

        float width = lineContainers[0].rect.width;
        float startX = -width / 2f;
        float xPos = startX + t * width;

        timerBar.anchoredPosition = new Vector2(xPos, timerBar.anchoredPosition.y);
    }

    // ───────────────────────────────────────────
    // 노트 생성
    // ───────────────────────────────────────────

    private void SpawnNote(int lineIndex, float timestamp, Color color)
    {
        if (lineIndex >= lineContainers.Length) return;
        if (notePrefab == null) return;

        RectTransform container = lineContainers[lineIndex];
        float width = container.rect.width;
        float startX = -width / 2f;

        float xPos = startX + (timestamp / timeWindow) * width;
        xPos = Mathf.Clamp(xPos, startX, -startX);

        GameObject noteObj = Instantiate(notePrefab, container);
        RectTransform noteRect = noteObj.GetComponent<RectTransform>();
        noteRect.anchoredPosition = new Vector2(xPos, 0f);

        var img = noteObj.GetComponent<Image>();
        if (img != null) img.color = color;

        spawnedNotes.Add(noteObj);
    }
}