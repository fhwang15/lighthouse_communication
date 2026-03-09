using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [A 플레이어용] 스페이스바 입력을 녹화하는 시스템
/// 
/// 사용법:
/// 1. 빈 GameObject에 이 스크립트를 붙여
/// 2. Inspector에서 minNotes=3, maxNotes=5 확인
/// 3. OnRecordingComplete 이벤트에 NotePlayer.PlayNotes() 연결
/// </summary>
public class NoteRecorder : MonoBehaviour
{
    [Header("노트 개수 설정")]
    [SerializeField] private int minNotes = 3;
    [SerializeField] private int maxNotes = 5;

    [Header("이벤트")]
    public UnityEvent<List<float>> OnRecordingComplete; // 녹화 완료 시 호출

    // 내부 상태
    private List<float> noteTimestamps = new List<float>(); // 각 노트를 누른 시각(초)
    private int targetNoteCount;   // 이번 라운드에 받을 노트 수 (3~5 랜덤)
    private bool isRecording = false;
    private float recordingStartTime;

    // 외부에서 읽을 수 있도록 (NotePlayer가 사용)
    public List<float> RecordedTimestamps => noteTimestamps;

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    /// <summary>녹화를 시작해. GameManager에서 호출.</summary>
    public void StartRecording()
    {
        // 초기화
        noteTimestamps.Clear();
        targetNoteCount = Random.Range(minNotes, maxNotes + 1); // 3,4,5 중 하나
        isRecording = true;
        recordingStartTime = Time.time;

        Debug.Log($"[녹화 시작] 목표 노트 수: {targetNoteCount}개");
    }

    /// <summary>강제로 녹화 중단 (타임아웃 등에 사용)</summary>
    public void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        FinishRecording();
    }

    // ───────────────────────────────────────────
    // 내부 로직
    // ───────────────────────────────────────────

    private void Update()
    {
        if (!isRecording) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            RegisterNote();
        }
    }

    private void RegisterNote()
    {
        // 녹화 시작 시각으로부터 경과한 시간을 저장
        float elapsed = Time.time - recordingStartTime;
        noteTimestamps.Add(elapsed);

        Debug.Log($"[노트 {noteTimestamps.Count}/{targetNoteCount}] {elapsed:F3}초");

        // 목표 노트 수에 도달하면 자동 완료
        if (noteTimestamps.Count >= targetNoteCount)
        {
            isRecording = false;
            FinishRecording();
        }
    }

    private void FinishRecording()
    {
        if (noteTimestamps.Count == 0)
        {
            Debug.LogWarning("[녹화 실패] 노트가 없어!");
            return;
        }

        Debug.Log($"[녹화 완료] {noteTimestamps.Count}개 노트 저장됨");
        OnRecordingComplete?.Invoke(noteTimestamps);
    }
}
