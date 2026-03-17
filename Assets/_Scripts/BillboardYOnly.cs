using UnityEngine;

public class BillboardYOnly : MonoBehaviour
{
    Transform cam;

    void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main.transform;

        // 모든 축으로 카메라를 향함 (위에서 내려다봐도 잘 보임)
        transform.LookAt(transform.position + cam.rotation * Vector3.forward,
                         cam.rotation * Vector3.up);
    }
}