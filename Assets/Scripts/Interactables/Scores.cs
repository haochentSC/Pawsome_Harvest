using UnityEngine;
using TMPro;

public class Scores : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI comfortText;
    public TextMeshProUGUI bondText;
    public TextMeshProUGUI hungryText;
    public float HealthScore = 0f; // 돈이나 포인트 (소수점 고려 시 float)
    
    public float HungryScore = 100f; // 돈이나 포인트 (소수점 고려 시 float)

    public float ComfortScore = 50f;
    public float BondScore = 0f;
    public void AddBond(float amount)
    {
        BondScore += amount;
        if (bondText != null)
        {
            // 0.0 Fert 이런 식으로 소수점 첫째자리까지 표시
            bondText.text = BondScore.ToString("F1") + " %";
        }
    }
    public void AddHealth(float amount)
    {
        HealthScore += amount;
        if (healthText != null)
        {
            // 0.0 Fert 이런 식으로 소수점 첫째자리까지 표시
            healthText.text = HealthScore.ToString("F1") + " %";
        }
    }
    public void AddHungry(float amount)
    {
        HungryScore += amount;
        if (hungryText != null)
        {
            // 0.0 Fert 이런 식으로 소수점 첫째자리까지 표시
            hungryText.text = HungryScore.ToString("F1") + " %";
        }
    }
    public void AddComfort(float amount)
    {
        ComfortScore += amount;
        if (comfortText != null)
        {
            // 0.0 Fert 이런 식으로 소수점 첫째자리까지 표시
            comfortText.text = ComfortScore.ToString("F1") + " %";
        }
    }
}