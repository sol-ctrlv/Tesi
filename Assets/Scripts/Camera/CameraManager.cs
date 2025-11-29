using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    public CinemachineCamera ActiveVirtualCamera;
    [SerializeField] private CinemachineCamera virtualCamera;

    float oldOrtoLens, newOrtoLens;
    Timer ortoLensLerpTimer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        ActiveVirtualCamera = virtualCamera;
        ActiveVirtualCamera.Follow = new GameObject().transform;

        ortoLensLerpTimer = new Timer(1f, false, false);

    }

    public static void SetTargetPosition(Vector3 newPositon)
    {
        Instance.ActiveVirtualCamera.Follow.position = newPositon;
    }

    public static void SetCameraPosition(Vector3 newPositon)
    {
        SetTargetPosition(newPositon);
        Instance.ActiveVirtualCamera.OnTargetObjectWarped(Instance.ActiveVirtualCamera.Follow, new Vector3(newPositon.x, newPositon.y, Instance.ActiveVirtualCamera.transform.position.z));
        Instance.ActiveVirtualCamera.transform.position = new Vector3(newPositon.x, newPositon.y, Instance.ActiveVirtualCamera.transform.position.z);
    }

    public static void SetLensOrtoSize(float size)
    {
        Instance.oldOrtoLens = Instance.ActiveVirtualCamera.Lens.OrthographicSize;
        Instance.newOrtoLens = size;
        Instance.ortoLensLerpTimer.Reset();
    }

    private void Update()
    {
        if (ortoLensLerpTimer.ShouldTick)
        {
            if (ortoLensLerpTimer.Tick(Time.deltaTime))
            {
                ActiveVirtualCamera.Lens.OrthographicSize = newOrtoLens;
            }

            float lerpValue = ortoLensLerpTimer.GetCounter() / ortoLensLerpTimer.GetTimer();
            ActiveVirtualCamera.Lens.OrthographicSize = Mathf.Lerp(oldOrtoLens, newOrtoLens, 1f - lerpValue);
        }
    }
}
