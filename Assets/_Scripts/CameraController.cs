using UnityEngine;
using System.Linq;
using System.Collections;

public class PartyCameraController : MonoBehaviour
{
    public Camera targetCamera;

    [Header("카메라 이동 속도")]
    public float followSmoothSpeed = 8f;
    public float transitionDuration = 2.0f;

    [Header("로비 카메라 설정")]
    public Transform lobbyLookTarget;
    public Vector3 lobbyOffset = new Vector3(0, 8, -10);

    [Header("게임 카메라 설정")]
    [Tooltip("게임 시작 시 카메라가 이동할 Transform (position + rotation 그대로 사용)")]
    public Transform gameCameraTarget;

    [Header("게임 중 플레이어 추적 설정")]
    public float minDistance = 6f;
    public Vector3 gameOffset = new Vector3(0, 8, 10);
    public float lookHeightOffset = 1.5f;
    [Tooltip("true = gameCameraTarget 고정 / false = 플레이어 추적")]
    public bool useFixedGameCamera = true;

    private enum CameraPhase { Lobby, Transitioning, Game }
    private CameraPhase phase = CameraPhase.Lobby;

    // ───────────────────────────────────────────
    // 외부에서 호출
    // ───────────────────────────────────────────

    public void TransitionToGame()
    {
        if (phase != CameraPhase.Lobby) return;
        StartCoroutine(TransitionCoroutine());
    }

    // ───────────────────────────────────────────
    // 매 프레임
    // ───────────────────────────────────────────

    void LateUpdate()
    {
        if (targetCamera == null) return;

        if (phase == CameraPhase.Lobby)
            UpdateLobbyCamera();
        else if (phase == CameraPhase.Game)
            UpdateGameCamera();
    }

    // ───────────────────────────────────────────
    // 로비 카메라
    // ───────────────────────────────────────────

    private void UpdateLobbyCamera()
    {
        if (lobbyLookTarget == null) return;

        Vector3 desiredPos = lobbyLookTarget.position + lobbyOffset;

        targetCamera.transform.position = Vector3.Lerp(
            targetCamera.transform.position,
            desiredPos,
            Time.deltaTime * followSmoothSpeed
        );

        targetCamera.transform.LookAt(lobbyLookTarget.position);
    }

    // ───────────────────────────────────────────
    // 게임 카메라
    // ───────────────────────────────────────────

    private void UpdateGameCamera()
    {
        if (useFixedGameCamera)
        {
            // 고정 모드: gameCameraTarget 위치/회전 유지
            if (gameCameraTarget == null) return;

            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                gameCameraTarget.position,
                Time.deltaTime * followSmoothSpeed
            );

            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                gameCameraTarget.rotation,
                Time.deltaTime * followSmoothSpeed
            );
        }
        else
        {
            // 추적 모드: 플레이어들 평균 위치 따라감
            UpdateFollowCamera();
        }
    }

    private void UpdateFollowCamera()
    {
        if (GameManager.Instance == null) return;

        var players = GameManager.Instance.players
            .Where(p => p.currentAvatar != null)
            .ToList();

        if (players.Count == 0) return;

        Vector3 avg = Vector3.zero;
        foreach (var p in players)
            avg += p.currentAvatar.transform.position;
        avg /= players.Count;

        float maxDistance = 0f;
        for (int i = 0; i < players.Count; i++)
            for (int j = i + 1; j < players.Count; j++)
            {
                float d = Vector3.Distance(
                    players[i].currentAvatar.transform.position,
                    players[j].currentAvatar.transform.position);
                maxDistance = Mathf.Max(maxDistance, d);
            }

        float targetZoom = Mathf.Max(minDistance, maxDistance);
        Vector3 desiredPos = avg + gameOffset.normalized * targetZoom;

        targetCamera.transform.position = Vector3.Lerp(
            targetCamera.transform.position,
            desiredPos,
            Time.deltaTime * followSmoothSpeed
        );

        targetCamera.transform.LookAt(avg + Vector3.up * lookHeightOffset);
    }

    // ───────────────────────────────────────────
    // 전환 애니메이션
    // ───────────────────────────────────────────

    private IEnumerator TransitionCoroutine()
    {
        phase = CameraPhase.Transitioning;

        if (gameCameraTarget == null)
        {
            Debug.LogError("[Camera] gameCameraTarget이 없어! Inspector에서 연결해줘.");
            phase = CameraPhase.Game;
            yield break;
        }

        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;

        // 목표 = gameCameraTarget의 position + rotation 그대로!
        Vector3 endPos = gameCameraTarget.position;
        Quaternion endRot = gameCameraTarget.rotation;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            targetCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            targetCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        // 딱 맞게 고정
        targetCamera.transform.position = endPos;
        targetCamera.transform.rotation = endRot;

        phase = CameraPhase.Game;
        Debug.Log("[Camera] 게임 카메라 전환 완료!");
    }
}