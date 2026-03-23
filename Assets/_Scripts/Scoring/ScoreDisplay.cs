using UnityEngine;
using TMPro;

/// <summary>
/// 캐릭터 프리팹 안에 있는 점수 표시용 TMP 텍스트 컴포넌트
/// 
/// 사용법:
/// 1. 캐릭터 프리팹 안에 빈 오브젝트 "ScoreDisplay" 만들기
/// 2. TMP_Text 컴포넌트 + 이 스크립트 붙이기
/// 3. 머리 위 적당한 위치에 배치
/// </summary>
public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshPro scoreText; // 3D TMP (월드 스페이스)

    [Header("표시 설정")]
    [SerializeField] private string prefix = "*";  // 점수 앞에 붙는 텍스트
    [SerializeField] private Color positiveColor = Color.yellow;
    [SerializeField] private Color negativeColor = Color.red;

    private int currentScore = 0;

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponent<TextMeshPro>();

        UpdateScore(0);
    }

    public void UpdateScore(int score)
    {
        currentScore = score;

        if (scoreText == null) return;

        scoreText.text = prefix + score.ToString();
        scoreText.color = score >= 0 ? positiveColor : negativeColor;
    }

    public int GetScore() => currentScore;
}