using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리듬게임 악보 UI
/// 
/// Canvas 구조:
/// ScoreBoardPanel
/// ├── ShipLine    (Image들이 가로로 놓임)
/// ├── LHLine_0
/// ├── LHLine_1
/// └── LHLine_2
/// 
/// 노트 = 작은 원형 Image 프리팹
/// 시간이 지남에 따라 왼쪽에서 오른쪽으로 위치
/// </summary>
public class RhythmScoreUI : MonoBehaviour
{
    public static RhythmScoreUI Instance;

    [Header("라인 컨테이너 (Ship, LH0, LH1, LH2)")]
    [SerializeField] private RectTransform[] lineContainers; // 4개

    [Header("노트 프리팹 (작은 원형 Image)")]
    [SerializeField] private GameObject notePrefab;

    [Header("색상")]
    [SerializeField] private Color shipNoteColor = Color.white;
    [SerializeField]
    private Color[] lighthouseNoteColors = {
        new Color(1f, 0.5f, 0.5f),   // LH0: 빨강
        new Color(0.5f, 1f, 0.5f),   // LH1: 초록
        new Color(0.5f, 0.5f, 1f)    // LH2: 파랑
    };

    [Header("악보 설정")]
    [SerializeField] private float boardWidth = 600f;   // 악보 가로 길이 (px)
    [SerializeField] private float timeWindow = 5f;     // 악보에 표시할 시간 범위 (초)

    // 생성된 노트 오브젝트들
    private List<GameObject> spawnedNotes = new List<GameObject>();

    // 현재 라운드 시작 시간
    private float roundStartTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ───────────────────────────────────────────
    // 외부에서 호출
    // ───────────────────────────────────────────

    /// <summary>라운드 시작 시 악보 초기화</summary>
    public void ResetBoard()
    {
        foreach (var note in spawnedNotes)
            if (note != null) Destroy(note);

        spawnedNotes.Clear();
        roundStartTime = Time.time;
    }

    /// <summary>재생 페이즈 준비 (시간 리셋)</summary>
    public void PreparePlayback()
    {
        roundStartTime = Time.time;
    }

    /// <summary>배 플레이어가 누를 때 호출 (NoteRecorder에서)</summary>
    public void AddShipNote(float timestamp)
    {
        SpawnNote(0, timestamp, shipNoteColor);
    }

    /// <summary>등대 플레이어가 누를 때 호출 (AccuracyJudge에서)</summary>
    public void AddLighthouseNote(int judgeIndex, float timestamp)
    {
        int lineIndex = judgeIndex + 1; // 0=Ship, 1~3=Lighthouse
        Color color = judgeIndex < lighthouseNoteColors.Length
            ? lighthouseNoteColors[judgeIndex]
            : Color.white;

        SpawnNote(lineIndex, timestamp, color);
    }

    // ───────────────────────────────────────────
    // 노트 생성
    // ───────────────────────────────────────────

    private void SpawnNote(int lineIndex, float timestamp, Color color)
    {
        if (lineIndex >= lineContainers.Length) return;
        if (notePrefab == null) return;

        RectTransform container = lineContainers[lineIndex];

        GameObject noteObj = Instantiate(notePrefab, container);
        RectTransform noteRect = noteObj.GetComponent<RectTransform>();

        // X 위치: timestamp를 악보 너비에 매핑
        float xPos = (timestamp / timeWindow) * boardWidth;
        xPos = Mathf.Clamp(xPos, 0f, boardWidth);

        noteRect.anchoredPosition = new Vector2(xPos, 0f);

        // 색상 설정
        var img = noteObj.GetComponent<Image>();
        if (img != null) img.color = color;

        spawnedNotes.Add(noteObj);
    }
}