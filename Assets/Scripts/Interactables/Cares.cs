using UnityEngine;

public class Cares : MonoBehaviour
{
    public Scores scoreManager;
    public GameObject specialObject1; // 나타나게 할 첫 번째 물체
    public GameObject specialObject2; // 나타나게 할 두 번째 물체
    public GameObject starObject; // 나타나게 할 두 번째 물체
    public AudioSource audioSource; // 소리를 내보낼 스피커 역할
    public AudioClip successSound;  // 재생할 효과음 파일
    public Vector3 skyCenter = new Vector3(150, 95, 110); // 하늘의 중심 위치
    public Vector3 spawnRange = new Vector3(300, 100, 300); // 별이 생성될 범위 (가로, 높이, 세로)
    
    public float targetHealth = 100.0f;  // 예: 체력이 300 이상이면 활성화

    public float targetHungry = 0.0f; // 예: 배고픔이 -500 이하이면 활성화
    public float targetComfort = 100.0f; // 예: 배고픔이 -500 이하이면 활성화

    // 게임이 처음 시작될 때 실행됩니다.
    void Start()
    {
        // 시작하자마자 두 오브젝트를 보이지 않게 설정해요.
        if (specialObject1 != null) specialObject1.SetActive(false);
        if (specialObject2 != null) specialObject2.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void OnTriggerEnter(Collider other)
    {
        // 1. 닿은 물체의 태그가 무엇인지 확인합니다.
        Debug.Log("충돌 발생! 물체 이름: " + other.name + " | 태그: [" + other.tag + "]");
        string tag = other.tag;
        
        // 2. 태그에 따라 다른 함수를 호출합니다.
        switch (tag)
        {
            case "Pill":
                scoreManager.AddHealth(100.0f); // 알약은 체력 추가
                Debug.Log("Pill Detected: Health Added!");
                for (int i = 0; i < 10; i++)
                {
                    CreateStarInSky();
                }
                PlaySuccessSound(); // 소리 재생!
                ResetBlinker(other);
                break;

            case "Food":
                scoreManager.AddHungry(-100.0f);  // 씨앗은 점수 추가
                Debug.Log("Seed Detected: Score Added!");
                for (int i = 0; i < 10; i++)
                {
                    CreateStarInSky();
                }
                PlaySuccessSound(); // 소리 재생!
                ResetBlinker(other);
                break;

            case "Care":
                scoreManager.AddComfort(50.0f); // 물은 게이지 채우기
                Debug.Log("Water Detected: Gauge Filled!");
                for (int i = 0; i < 10; i++)
                {
                    CreateStarInSky();
                }
                ResetBlinker(other);
                break;

            default:
                // 설정되지 않은 태그의 물체가 닿으면 아무 일도 일어나지 않아요.
                break;
        }
        
        CheckProgress();
        
    }
    
    private void PlaySuccessSound()
    {
        if (audioSource != null && successSound != null)
        {
            // PlayOneShot을 사용하면 소리가 겹쳐도 자연스럽게 끊기지 않고 들려요!
            audioSource.PlayOneShot(successSound);
        }
    }
    
    // [핵심] 하늘의 랜덤한 위치에 별을 생성하는 함수
    private void CreateStarInSky()
    {
        if (starObject == null)
        {
            Debug.LogWarning("주의: Star Prefab이 연결되지 않았습니다!");
            return;
        }
        
        starObject.SetActive(true);

        // 설정한 범위 내에서 랜덤한 x, y, z 좌표를 계산합니다.
        float randomX = Random.Range(-spawnRange.x / 2f, spawnRange.x / 2f);
        float randomY = Random.Range(-spawnRange.y / 2f, spawnRange.y / 2f);
        float randomZ = Random.Range(-spawnRange.z / 2f, spawnRange.z / 2f);

        // 중심 위치에 랜덤 좌표를 더해 최종 생성 위치를 만듭니다.
        Vector3 spawnPosition = skyCenter + new Vector3(randomX, randomY, randomZ);

        // 해당 위치에 별을 생성합니다 (회전은 기본값).
        Instantiate(starObject, spawnPosition, Quaternion.identity);
        
    }
    
    // 특정 수치가 되었는지 확인하고 오브젝트를 켜주는 함수
    private void CheckProgress()
    {
        // 예: 체력이 목표치에 도달했거나, 배고픔 수치가 충분히 내려갔을 때
        // (Scores 스크립트에 해당 변수들이 public으로 선언되어 있어야 합니다)
        if (scoreManager.HealthScore >= targetHealth && scoreManager.HungryScore <= targetHungry && scoreManager.ComfortScore >= targetComfort)
        {
            if (specialObject1 != null && !specialObject1.activeSelf)
            {
                specialObject1.SetActive(true);
                Debug.Log("조건 달성: 오브젝트 1 활성화!");
            }

            if (specialObject2 != null && !specialObject2.activeSelf)
            {
                specialObject2.SetActive(true);
                Debug.Log("조건 달성: 오브젝트 2 활성화!");
            }
        }
    }

    // Blinker 리셋 로직이 반복되므로 따로 함수로 만들었어요.
    private void ResetBlinker(Collider target)
    {
        // Blinker 대신 Exist를 찾습니다.
        Exist existScript = target.GetComponent<Exist>(); 
        if (existScript != null)
        {
            existScript.ResetToInitialState(); // 위에서 public으로 만든 함수 호출!
        }
    }
    
    
}