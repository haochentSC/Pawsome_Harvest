using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Exist : MonoBehaviour
{
    public float interval = 3.0f; 
    private float timer;          
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private MeshRenderer mainRenderer;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    

    // --- 추가된 변수 ---
    public string targetTag = "Goal"; // 점수를 주는 영역의 태그
    // ------------------

    void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        mainRenderer = GetComponent<MeshRenderer>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (grabInteractable != null)
        {
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    void Update()
    {
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            SetRenderersEnabled(true);
            timer = 0; 
            return; 
        }

        timer += Time.deltaTime;

        if (timer >= interval)
        {
            if (mainRenderer != null)
            {
                SetRenderersEnabled(!mainRenderer.enabled);
            }
            timer = 0;
        }
    }

    // 공통으로 쓰기 위해 함수로 따로 뺐어요
    void OnRelease(SelectExitEventArgs args)
    {
        ResetToInitialState();
    }
    

    public void ResetToInitialState()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
    // --------------------------------

    void SetRenderersEnabled(bool isEnabled)
    {
        if (mainRenderer != null) mainRenderer.enabled = isEnabled;

        foreach (var childRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            childRenderer.enabled = isEnabled;
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }
}