using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// [재생용] 녹화된 노트 패턴을 시각/소리로 출력하는 시스템
/// 
/// 사용법:
/// 1. 빈 GameObject에 이 스크립트를 붙여
/// 2. flashObject: 번쩍일 UI 이미지나 오브젝트 연결
/// 3. beatSound: 노트 소리 클립 연결 (없어도 동작함)
/// 4. NoteRecorder.OnRecordingComplete → PlayNotes() 연결
/// </summary>
public class NotePlayer : MonoBehaviour
{
    [Header("시각 피드백")]
    [SerializeField] private GameObject flashObject;   // 노트 칠 때 번쩍이는 오브젝트
    [SerializeField] private float flashDuration = 0.1f; // 번쩍임 지속 시간(초)

    [Header("소리 피드백")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beatSound;      // 노트 소리

    [Header("이벤트")]
    public UnityEvent OnPlaybackComplete; // 재생 끝나면 호출 → 모방 페이즈 시작

    // 재생 중인 노트 데이터를 외부(판정 시스템)에서도 참조할 수 있게 공개
    public List<float> CurrentNoteTimestamps { get; private set; }
    public bool IsPlaying { get; private set; } = false;

    // ───────────────────────────────────────────
    // 공개 메서드
    // ───────────────────────────────────────────

    /// <summary>
    /// 노트 재생 시작. NoteRecorder.OnRecordingComplete 이벤트에 연결해.
    /// </summary>
    public void PlayNotes(List<float> timestamps)
    {
        if (IsPlaying)
        {
            StopAllCoroutines();
        }

        CurrentNoteTimestamps = new List<float>(timestamps); // 복사해서 보관
        StartCoroutine(PlaybackCoroutine(timestamps));
    }

    // ───────────────────────────────────────────
    // 내부 로직
    // ───────────────────────────────────────────

    private IEnumerator PlaybackCoroutine(List<float> timestamps)
    {
        IsPlaying = true;
        Debug.Log($"[재생 시작] {timestamps.Count}개 노트");

        // 잠깐 대기 후 재생 (플레이어들 준비 시간)
        yield return new WaitForSeconds(1.0f);

        float startTime = Time.time;

        for (int i = 0; i < timestamps.Count; i++)
        {
            // 다음 노트 타이밍까지 대기
            float waitUntil = startTime + timestamps[i];
            while (Time.time < waitUntil)
            {
                yield return null; // 다음 프레임까지 대기
            }

            // 노트 실행!
            TriggerNoteFeedback(i + 1, timestamps.Count);
        }

        // 재생 완료
        IsPlaying = false;
        Debug.Log("[재생 완료] 모방 페이즈 시작!");
        OnPlaybackComplete?.Invoke();
    }

    private void TriggerNoteFeedback(int noteIndex, int totalNotes)
    {
        Debug.Log($"[노트 {noteIndex}/{totalNotes}] 재생!");

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
