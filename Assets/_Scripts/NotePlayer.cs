using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class NotePlayer : MonoBehaviour
{
    [Header("시각 피드백")]
    [SerializeField] private GameObject flashObject;
    [SerializeField] private Color flashColor = Color.yellow;   // Long press 색상
    [SerializeField] private Color idleColor = Color.white;     // 기본 색상

    [Header("소리 피드백")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beatSound;

    [Header("대기 시간")]
    [SerializeField] private float prePlayDelay = 3.0f; // 재생 전 대기 시간

    [Header("이벤트")]
    public UnityEvent OnPlaybackComplete;

    public TextMeshProUGUI phaseText;

    // NoteData 리스트로 변경
    public List<NoteData> CurrentNotes { get; private set; }

    // 하위 호환성을 위해 press 타임만 뽑아주는 프로퍼티
    public List<float> CurrentNoteTimestamps
    {
        get
        {
            if (CurrentNotes == null) return null;
            var list = new List<float>();
            foreach (var n in CurrentNotes)
                list.Add(n.pressTime);
            return list;
        }
    }

    public bool IsPlaying { get; private set; } = false;

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    /// <summary>NoteRecorder.OnRecordingComplete 에 연결</summary>
    public void PlayNotes(List<NoteData> notes)
    {
        if (IsPlaying) StopAllCoroutines();

        CurrentNotes = new List<NoteData>(notes);
        StartCoroutine(PlaybackCoroutine(notes));
    }

    // ───────────────────────────────────────────
    // 재생 코루틴
    // ───────────────────────────────────────────

    private IEnumerator PlaybackCoroutine(List<NoteData> notes)
    {
        IsPlaying = true;

        if (phaseText != null) phaseText.text = "Now playing the signal";

        // flash 초기화
        SetFlash(false);

        // ── 핵심: 3초 대기 후 재생 ──
        yield return new WaitForSeconds(prePlayDelay);

        if (notes.Count == 0)
        {
            IsPlaying = false;
            if (phaseText != null) phaseText.text = "Lighthouse's turn";
            OnPlaybackComplete?.Invoke();
            yield break;
        }

        float startTime = Time.time;

        for (int i = 0; i < notes.Count; i++)
        {
            NoteData note = notes[i];

            // press 타이밍까지 대기
            while (Time.time - startTime < note.pressTime)
                yield return null;

            // 버튼 눌림 시각 효과 + 소리
            SetFlash(true);
            PlaySound();

            // release 타이밍까지 대기 (Long press 시각 유지)
            while (Time.time - startTime < note.releaseTime)
                yield return null;

            // 버튼 뗌 시각 효과
            SetFlash(false);
        }

        IsPlaying = false;
        if (phaseText != null) phaseText.text = "Lighthouse's turn";
        OnPlaybackComplete?.Invoke();
    }

    // ───────────────────────────────────────────
    // 시각/소리 헬퍼
    // ───────────────────────────────────────────

    private void SetFlash(bool on)
    {
        if (flashObject == null) return;
        flashObject.SetActive(on);
    }

    private void PlaySound()
    {
        if (audioSource != null && beatSound != null)
            audioSource.PlayOneShot(beatSound);
    }
}