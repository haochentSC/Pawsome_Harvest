using UnityEngine;
using TMPro;

public class TutorialPopupAnimator : MonoBehaviour {
    private float _timer = 0f;
    private Vector3 _targetScale = Vector3.one;
    private Vector3 _overshootScale = new Vector3(1.2f, 1.2f, 1.2f);

    private enum State { Growing, Shrinking, Holding }
    private State _state = State.Growing;

    void Start() {
        transform.localScale = Vector3.zero;
    }

    void Update() {
        _timer += Time.deltaTime;

        switch (_state) {
            case State.Growing:
                transform.localScale = Vector3.Lerp(Vector3.zero, _overshootScale, _timer / 0.25f);
                if (_timer >= 0.25f) {
                    _timer = 0f;
                    _state = State.Shrinking;
                }
                break;

            case State.Shrinking:
                transform.localScale = Vector3.Lerp(_overshootScale, _targetScale, _timer / 0.15f);
                if (_timer >= 0.15f) {
                    _timer = 0f;
                    _state = State.Holding;
                }
                break;

            case State.Holding:
                if (_timer >= 3.5f) {
                    Destroy(gameObject);
                }
                break;
        }
    }
}
