using UnityEngine;

/// <summary>
/// PlayerSessionData를 GameScene에서 접근하기 위한 브릿지
/// GameManager 오브젝트에 같이 붙여줘
/// </summary>
public class GameSessionBridge : MonoBehaviour
{
    public static GameSessionBridge Instance;

    [SerializeField] private PlayerSessionData sessionData;
    public PlayerSessionData SessionData => sessionData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}