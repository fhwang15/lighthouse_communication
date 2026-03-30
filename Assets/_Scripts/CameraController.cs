using UnityEngine;
using System.Collections;

public class PartyCameraController : MonoBehaviour
{
    public Camera targetCamera;

    [Header("카메라 이동 속도")]
    public float followSmoothSpeed = 8f;
    public float transitionDuration = 2.0f;

    [Header("게임 카메라 목표")]
    [Tooltip("게임 씬에서 카메라가 이동할 Transform (position + rotation 그대로 사용)")]
    public Transform gameCameraTarget;

    private bool isReady = false; // 목표 위치에 도달했는지

    private void Start()
    {
        // GameScene 시작 시 카메라를 바로 목표 위치로 이동
        if (gameCameraTarget != null)
            StartCoroutine(TransitionToTarget());
    }

    // ───────────────────────────────────────────
    // 외부에서 호출 (GameStarter에서)
    // ───────────────────────────────────────────

    public void TransitionToGame()
    {
        if (gameCameraTarget != null)
            StartCoroutine(TransitionToTarget());
    }

    // ───────────────────────────────────────────
    // 전환 애니메이션
    // ───────────────────────────────────────────

    private IEnumerator TransitionToTarget()
    {
        if (targetCamera == null || gameCameraTarget == null) yield break;

        isReady = false;

        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;

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

        isReady = true;
        Debug.Log("[Camera] 목표 위치 도달!");
    }

    // ───────────────────────────────────────────
    // 매 프레임 (전환 완료 후 고정 유지)
    // ───────────────────────────────────────────

    private void LateUpdate()
    {
        if (!isReady || targetCamera == null || gameCameraTarget == null) return;

        // 목표 위치에 부드럽게 고정 유지
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
}