using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대기 씬 → 게임 씬으로 데이터 전달하는 ScriptableObject
/// 
/// 사용법:
/// 1. Assets에서 우클릭 → Create → Game → PlayerSessionData
/// 2. LobbyManager와 게임 씬 스크립트 양쪽에서 이 에셋 참조
/// </summary>
[CreateAssetMenu(fileName = "PlayerSessionData", menuName = "Game/PlayerSessionData")]
public class PlayerSessionData : ScriptableObject
{
    [System.Serializable]
    public class PlayerEntry
    {
        public int playerIndex;       // 0~3
        public string nickname;       // 선택한 닉네임
        public bool isKeyboard;       // 키보드 플레이어 여부
    }

    // 게임 시작 시 채워짐
    public List<PlayerEntry> players = new List<PlayerEntry>();

    /// <summary>초기화 (새 게임 시작 시 호출)</summary>
    public void Clear()
    {
        players.Clear();
    }

    /// <summary>플레이어 추가/업데이트</summary>
    public void SetPlayer(int index, string nickname, bool isKeyboard)
    {
        // 이미 있으면 업데이트
        var existing = players.Find(p => p.playerIndex == index);
        if (existing != null)
        {
            existing.nickname = nickname;
            existing.isKeyboard = isKeyboard;
            return;
        }

        players.Add(new PlayerEntry
        {
            playerIndex = index,
            nickname = nickname,
            isKeyboard = isKeyboard
        });
    }

    /// <summary>닉네임 가져오기</summary>
    public string GetNickname(int playerIndex)
    {
        var entry = players.Find(p => p.playerIndex == playerIndex);
        return entry != null ? entry.nickname : $"Player {playerIndex + 1}";
    }

    public int PlayerCount => players.Count;
}