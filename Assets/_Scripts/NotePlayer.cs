using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class NotePlayer : MonoBehaviour
{
    [Header("시각 피드백")]
    [SerializeField] private GameObject flashObject;  
    [SerializeField] private float flashDuration = 0.1f;

    [Header("소리 피드백")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beatSound; 

    [Header("이벤트")]
    public UnityEvent OnPlaybackComplete;

    public TextMeshProUGUI phaseText;

    public List<float> CurrentNoteTimestamps { get; private set; }
    public bool IsPlaying { get; private set; } = false;


    public void PlayNotes(List<float> timestamps)
    {
        if (IsPlaying)
        {
            StopAllCoroutines();
        }

        CurrentNoteTimestamps = new List<float>(timestamps); // 복사해서 보관
        StartCoroutine(PlaybackCoroutine(timestamps));
    }

    private IEnumerator PlaybackCoroutine(List<float> timestamps)
    {
        phaseText.text = "Now playing the signal";

        IsPlaying = true;

        flashObject.SetActive(false);
        yield return new WaitForSeconds(3.0f);

        float startTime = Time.time;

        for (int i = 0; i < timestamps.Count; i++)
        {
            float waitUntil = startTime + timestamps[i];
            while (Time.time < waitUntil)
            {
                yield return null;
            }

            TriggerNoteFeedback(i + 1, timestamps.Count);
        }

        IsPlaying = false;
        phaseText.text = "Lighthouse's turn";
        OnPlaybackComplete?.Invoke();
    }

    private void TriggerNoteFeedback(int noteIndex, int totalNotes)
    {
        
        // 소리 출력
        if (audioSource != null && beatSound != null)
        {
            audioSource.PlayOneShot(beatSound);
        }

        // 시각 번쩍임
        if (flashObject != null)
        {
            StartCoroutine(FlashCoroutine());
        }
    }

    private IEnumerator FlashCoroutine()
    {
        flashObject.SetActive(true);
        yield return new WaitForSeconds(flashDuration);
        flashObject.SetActive(false);
    }
}
