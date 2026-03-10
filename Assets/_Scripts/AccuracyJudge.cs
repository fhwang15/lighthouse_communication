using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AccuracyJudge : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] public int playerIndex;   
    [SerializeField] private KeyCode inputKey = KeyCode.Space;

    [Header("Connection")]
    [SerializeField] private NotePlayer notePlayer;

    [Header("판정 허용 오차")]
    [SerializeField] private float listenWindowExtra = 2.0f;

    public float AverageErrorMs { get; private set; } = float.MaxValue;
    public bool IsFinished { get; private set; } = false;

    public Material originalMaterial;
    public Renderer material;
    public Material flickeringMaterial;


    public UnityEvent<int, float> OnJudgeDone; 

     private List<float> myTimestamps = new List<float>();
    private bool isListening = false;
    private float listenStartTime;
    private List<float> referenceTimestamps;

    private void Start()
    {
        material.material.color = originalMaterial.color;
        
    }
    public void StartListening()
    {


        if (notePlayer == null || notePlayer.CurrentNoteTimestamps == null)
        {
            Debug.LogError($"[Player {playerIndex}] is not connected bro");
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

    }

    private void Update()
    {
        if (!isListening) return;

        if (Input.GetKeyDown(inputKey))
        {
            material.material.color = flickeringMaterial.color;
            float elapsed = Time.time - listenStartTime;
            myTimestamps.Add(elapsed);
            Debug.Log($"[Player {playerIndex}] 노트 입력: {elapsed:F3}초 ({myTimestamps.Count}/{referenceTimestamps.Count})");

            if (myTimestamps.Count >= referenceTimestamps.Count)
            {
                isListening = false;
                StopAllCoroutines();
                CalculateScore();
            }
        }
        else if (Input.GetKeyUp(inputKey))
        {
            material.material.color = originalMaterial.color;
        }
    }

    private IEnumerator AutoFinishCoroutine(float deadline)
    {
        yield return new WaitForSeconds(deadline);

        if (isListening) 
        {
            isListening = false;
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

        //
        // 원본 i번째 노트 vs 내가 입력한 i번째 노트의 시간 차이(ms)를 평균냄
        // 입력 개수가 다를 경우, 없는 노트는 패널티(1000ms) 부여
        // 

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
                totalError += 1000f; // 패널티: 1초(1000ms)
            }
        }

        AverageErrorMs = totalError / refCount;
        OnJudgeDone?.Invoke(playerIndex, AverageErrorMs);
    }
}
