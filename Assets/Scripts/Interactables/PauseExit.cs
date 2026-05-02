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
        isPaused = !isPaused;

        if (isPaused)
        {
            // 게임 시간을 멈춥니다. (물리, 애니메이션 등이 멈춤)
            Time.timeScale = 0f;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            Debug.Log("게임 일시정지");
        }
        else
        {
            // 게임 시간을 다시 흐르게 합니다.
            ResumeGame();
        }
    }

    // 게임 계속하기 (UI 버튼용)
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Debug.Log("게임 재개");
    }

    // [핵심] 게임 종료 함수
    public void QuitGame()
    {
        Debug.Log("게임 종료 버튼 눌림");

        // 1. 에디터에서 플레이 중일 때 끄기
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        // 2. 실제 빌드된 게임 끄기
        Application.Quit();
    }
}