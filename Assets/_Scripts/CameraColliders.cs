using UnityEngine;

public class CameraColliders : MonoBehaviour
{
    private Camera m_camera;

    [SerializeField] private BoxCollider _leftCollider;
    [SerializeField] private BoxCollider _rightCollider;
    [SerializeField] private BoxCollider _topCollider;
    [SerializeField] private BoxCollider _bottomCollider;

    void Start()
    {
        m_camera = GetComponent<Camera>();

        if (m_camera.orthographic)
            SetCameraEdges();
    }

    private void SetCameraEdges()
    {
        float halfHeight = m_camera.orthographicSize;
        float halfWidth = m_camera.orthographicSize * m_camera.aspect;

        _leftCollider.center = new Vector3(-halfWidth - _leftCollider.size.x * 0.5f, 0f, 0f);
        _leftCollider.size = new Vector3(_leftCollider.size.x, halfHeight * 2f + _leftCollider.size.x * 2, 20f);

        _topCollider.center = new Vector3(0f, halfHeight + _topCollider.size.y * 0.5f, 0f);
        _topCollider.size = new Vector3(halfWidth * 2f, _topCollider.size.y, 20f);

        _rightCollider.center = new Vector3(_leftCollider.center.x * -1f, _leftCollider.center.y, _leftCollider.center.z);
        _rightCollider.size = _leftCollider.size;

        _bottomCollider.center = new Vector3(_topCollider.center.x, _topCollider.center.y * -1f, _topCollider.center.z);
        _bottomCollider.size = _topCollider.size;

        WiiGameManager.Instance.spawnDepth = _bottomCollider.center.y - _bottomCollider.size.y / 2;
    }
}
