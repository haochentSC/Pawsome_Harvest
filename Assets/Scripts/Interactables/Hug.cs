using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;

public class Hug : MonoBehaviour
{
    public Scores scoreManager;
    public GameObject targetObject;    // 2초간 나타날 오브젝트
    public float holdDuration = 3.0f;  // 얼마나 쥐고 있어야 하는지
    public float activeDuration = 2.0f; // 나타난 후 유지될 시간

    private Coroutine holdCoroutine;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Exist existScript;

    void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        existScript = GetComponent<Exist>();

        // 시작할 때 타겟 오브젝트는 숨겨둡니다.
        if (targetObject != null) targetObject.SetActive(false);

        // 집었을 때와 놓았을 때의 이벤트를 연결합니다.
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    // 물체를 집었을 때 호출
    private void OnGrab(SelectEnterEventArgs args)
    {
        // 2초를 세기 시작합니다.
        holdCoroutine = StartCoroutine(CheckHoldTime());
    }

    // 물체를 놓았을 때 호출
    private void OnRelease(SelectExitEventArgs args)
    {
        // 2초가 되기 전에 놓으면 타이머를 취소합니다.
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
        Exist existScript = GetComponent<Exist>();
        if (existScript != null)
        {
            existScript.ResetToInitialState();
        }
        else
        {
            Debug.LogWarning("토끼에 Exist 스크립트가 붙어 있는지 확인해 주세요!");
        }
    }
    

    private IEnumerator CheckHoldTime()
    {
        // 1. 지정된 시간(2초) 동안 기다립니다.
        yield return new WaitForSeconds(holdDuration);

        // 2. 시간이 다 되면 특정 오브젝트를 활성화합니다.
        if (targetObject != null)
        {
            targetObject.SetActive(true);
            scoreManager.AddBond(100.0f);
            Debug.Log("3초 유지 성공! 오브젝트 활성화");

            // 3. 다시 2초 동안 보여준 뒤 사라지게 합니다.
            yield return new WaitForSeconds(activeDuration);
            targetObject.SetActive(false);
            Debug.Log("2초 경과: 오브젝트 다시 사라짐");
        }
        
        holdCoroutine = null;
    }

    // 스크립트가 파괴될 때 이벤트 연결 해제 (메모리 관리)
    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }
}