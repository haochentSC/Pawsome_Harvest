using UnityEngine;
using UnityEngine.SceneManagement; // 종료 후 메인화면으로 갈 때 필요 (선택사항)

public class PauseExit : MonoBehaviour
{
    public GameObject pauseMenuPanel; // 일시정지 버튼들이 있는 캔버스/패널

    private bool isPaused = false;

    void Start()
    {
        // 게임 시작 시 메뉴는 숨겨둡니다.
        //if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        // (선택사항) VR 컨트롤러의 특정 버튼(예: 메뉴 버튼)으로 메뉴를 켜고 끄고 싶다면
        // 여기에 입력 처리를 추가할 수 있습니다. 지금은 Ray로 끄는 버튼을 만들게요.
    }

    // [핵심] 게임 일시정지 함수
    public void TogglePause()
    {
        // 현재 게임이 흐르고 있다면 멈추고, 멈춰 있다면 다시 흐르게 합니다.
        if (Time.timeScale == 1.0f)
        {
            Time.timeScale = 0.0f; // 게임 정지
            Debug.Log("Game Paused!");
        }
        else
        {
            Time.timeScale = 1.0f; // 게임 재개
            Debug.Log("Game Resumed!");
        }
    }

    // [핵심] 게임 종료 함수
    public void QuitGame()
    {
        Debug.Log("Game Quit!");

        // 1. 에디터에서 플레이 중일 때 끄기
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        // 2. 실제 빌드된 게임 끄기
        Application.Quit();
    }
}