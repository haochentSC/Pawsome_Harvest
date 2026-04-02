using UnityEngine;

public class TrophyManager : MonoBehaviour
{
    public static TrophyManager Instance { get; private set; }

    [Header("Trophy Objects (assign cubes)")]
    [SerializeField] private GameObject trophy1;
    [SerializeField] private GameObject trophy2;
    [SerializeField] private GameObject trophy3;

    [Header("Unlock Thresholds")]
    [SerializeField] private float threshold1 = 500f;
    [SerializeField] private float threshold2 = 1000f;
    [SerializeField] private float threshold3 = 1500f;

    [Header("Sound")]
    [SerializeField] private AudioClip trophySound;

    private bool earned1 = false;
    private bool earned2 = false;
    private bool earned3 = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Hide trophies at start
        if (trophy1) trophy1.SetActive(false);
        if (trophy2) trophy2.SetActive(false);
        if (trophy3) trophy3.SetActive(false);

        // Subscribe to money changes
        EconomyManager.Instance.OnMoneyChanged += OnMoneyChanged;
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    private void OnMoneyChanged(float money)
    {
        if (!earned1 && money >= threshold1)
            UnlockTrophy(trophy1, ref earned1);

        if (!earned2 && money >= threshold2)
            UnlockTrophy(trophy2, ref earned2);

        if (!earned3 && money >= threshold3)
            UnlockTrophy(trophy3, ref earned3);
    }

    private void UnlockTrophy(GameObject trophy, ref bool earnedFlag)
    {
        earnedFlag = true;

        if (trophy != null)
        {
            trophy.SetActive(true);

            // Play spatial sound using your existing pattern
            AudioSource src = trophy.GetComponent<AudioSource>();
            if (src != null && trophySound != null)
                FeedbackManager.Instance.PlaySpatialSound(src, trophySound);
        }
    }
}
