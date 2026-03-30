using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 시 플레이어들에게 Ship/Lighthouse 역할을 랜덤 배정
/// </summary>
public class RoleManager : MonoBehaviour
{
    public static RoleManager Instance;

    [Header("위치 연결")]
    [SerializeField] private Transform shipPosition;
    [SerializeField] private Transform[] lighthousePositions;

    public int ShipPlayerIndex { get; private set; } = -1;
    public List<int> LighthousePlayerIndices { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AssignRoles()
    {
        var players = GameManager.Instance.players;

        if (players.Count == 0)
        {
            Debug.LogError("[RoleManager] 플레이어가 없어!");
            return;
        }

        ShipPlayerIndex = Random.Range(0, players.Count);
        LighthousePlayerIndices = new List<int>();

        for (int i = 0; i < players.Count; i++)
        {
            if (i != ShipPlayerIndex)
                LighthousePlayerIndices.Add(i);
        }

        Debug.Log($"[RoleManager] Ship: Player {ShipPlayerIndex + 1} / Lighthouse: {string.Join(", ", LighthousePlayerIndices.ConvertAll(i => "Player " + (i + 1)))}");
    }
}