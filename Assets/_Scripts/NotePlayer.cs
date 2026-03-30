using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class NotePlayer : MonoBehaviour
{
    [Header("시각 피드백")]
    [SerializeField] private GameObject flashObject;
    [SerializeField] private float flashDuration = 0.15f;

    [Header("소리 피드백")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beatSound;

    [Header("재생 전 대기")]
    [SerializeField] private float prePlayDelay = 0f; // PhaseUIManager 카운트다운이 대기 역할

    [Header("이벤트")]
    public UnityEvent OnPlaybackComplete;

    public TextMeshProUGUI phaseText;

    public List<float> CurrentNoteTimestamps { get; private set; }
    public bool IsPlaying { get; private set; } = false;

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    public void PlayNotes(List<float> timestamps)
    {
        if (IsPlaying) StopAllCoroutines();

        CurrentNoteTimestamps = new List<float>(timestamps);
        StartCoroutine(PlaybackCoroutine(timestamps));
    }

    // ───────────────────────────────────────────
    // 재생 코루틴
    // ───────────────────────────────────────────

    private IEnumerator PlaybackCoroutine(List<float> timestamps)
    {
        IsPlaying = true;
        if (phaseText != null) phaseText.text = "Now playing the signal";

        if (flashObject != null) flashObject.SetActive(false);

        if (prePlayDelay > 0f)
            yield return new WaitForSeconds(prePlayDelay);

        if (timestamps.Count == 0)
        {
            IsPlaying = false;
            if (phaseText != null) phaseText.text = "Lighthouse's turn";
            OnPlaybackComplete?.Invoke();
            yield break;
        }

        // 악보 UI 재생 라인 초기화
        RhythmScoreUI.Instance?.PreparePlayback();

        float startTime = Time.time;

        for (int i = 0; i < timestamps.Count; i++)
        {
            // 해당 타이밍까지 대기
            while (Time.time - startTime < timestamps[i])
                yield return null;

            TriggerNoteFeedback();
        }

        IsPlaying = false;
        if (phaseText != null) phaseText.text = "Lighthouse's turn";
        OnPlaybackComplete?.Invoke();
    }

    private void TriggerNoteFeedback()
    {
        if (audioSource != null && beatSound != null)
            audioSource.PlayOneShot(beatSound);

        if (flashObject != null)
            StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        flashObject.SetActive(true);
        yield return new WaitForSeconds(flashDuration);
        flashObject.SetActive(false);
    }
}