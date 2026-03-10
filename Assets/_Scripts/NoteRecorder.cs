using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class NoteRecorder : MonoBehaviour
{
    [Header("노트 개수 설정")]
    [SerializeField] private int minNotes = 3;
    [SerializeField] private int maxNotes = 5;

    [Header("이벤트")]
    public UnityEvent<List<float>> OnRecordingComplete;

    [SerializeField] private GameObject FlashObject;

    private List<float> noteTimestamps = new List<float>();
    private int targetNoteCount; 
    private bool isRecording = false;
    private float recordingStartTime;

    public NotePlayer npText;

    public List<float> RecordedTimestamps => noteTimestamps;


    public TextMeshProUGUI numberText;


    public void StartRecording()
    {
        // 초기화
        noteTimestamps.Clear();
        npText.phaseText.text = "Signal Recording on process...";

        targetNoteCount = Random.Range(minNotes, maxNotes + 1);
        numberText.text = targetNoteCount.ToString();
        isRecording = true;
        recordingStartTime = Time.time;

        Debug.Log($"[녹화 시작] 목표 노트 수: {targetNoteCount}개");
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            FlashObject.SetActive(true);
            RegisterNote();
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            FlashObject.SetActive(false);
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
            FinishRecording();
        }
    }

    private void FinishRecording()
    {
        if (noteTimestamps.Count == 0)
        {
            npText.phaseText.text = "Finished Recording";
            return;
        }

        Debug.Log($"[녹화 완료] {noteTimestamps.Count}개 노트 저장됨");
        OnRecordingComplete?.Invoke(noteTimestamps);
    }
}
