using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieBunker
{
    public class TurretManager : MonoBehaviour
    {
        public static TurretManager Instance { get; private set; }

        [Header("Bullet Turret Settings")]
        [SerializeField] private float bulletConsumptionRate = 2f; // bullets per shot
        [SerializeField] private float cashPerBullet = 1f;
        [SerializeField] private float bulletCooldown = 1f; // seconds per shot

        [Header("Rocket Turret Settings")]
        [SerializeField] private float rocketConsumptionRate = 1f; // rockets per shot
        [SerializeField] private float cashPerRocket = 10f;
        [SerializeField] private float rocketCooldown = 1f; // seconds per shot

        [Header("Upgrade Settings")]
        [SerializeField] private float consumptionMultiplierPerLevel = 1.5f; // x1.5 per level (exponential)

        [Header("Turret Visuals")]
        [SerializeField] private GameObject bulletTurretObject;
        [SerializeField] private GameObject rocketTurretObject;
        [SerializeField] private GameObject[] turretUpgradeMeshes;

        private float efficiencyMultiplier = 1f;
        private int turretLevel = 0;

        private bool bulletTurretActive = true;
        private bool rocketTurretActive = false;

        private float bulletCooldownTimer = 0f;
        private float rocketCooldownTimer = 0f;

        private List<TurretView> bulletTurrets = new List<TurretView>();
        private List<TurretView> rocketTurrets = new List<TurretView>();

        public event Action<float> OnCashEarned;
        public event Action<int> OnTurretUpgraded;

        public float EfficiencyMultiplier => efficiencyMultiplier;
        public int TurretLevel => turretLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            bulletTurrets.Clear();
            rocketTurrets.Clear();

            foreach (var turret in FindObjectsByType<TurretView>(FindObjectsSortMode.None))
            {
                if (turret.turretType == TurretType.Bullet)
                    bulletTurrets.Add(turret);
                else if (turret.turretType == TurretType.Rocket)
                    rocketTurrets.Add(turret);
            }
        }

        private void Start()
        {
            RecalculateCashRate();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (bulletTurretActive)
            {
                bulletCooldownTimer -= dt;
                if (bulletCooldownTimer <= 0f)
                {
                    ShootBulletTurrets();
                    bulletCooldownTimer = bulletCooldown;
                }
            }

            if (rocketTurretActive)
            {
                rocketCooldownTimer -= dt;
                if (rocketCooldownTimer <= 0f)
                {
                    ShootRocketTurrets();
                    rocketCooldownTimer = rocketCooldown;
                }
            }
        }

        private float GetEffectiveBulletConsumption()
        {
            return bulletConsumptionRate * Mathf.Pow(consumptionMultiplierPerLevel, turretLevel);
        }

        private float GetEffectiveRocketConsumption()
        {
            return rocketConsumptionRate * Mathf.Pow(consumptionMultiplierPerLevel, turretLevel);
        }

        private void ShootBulletTurrets()
        {
            float available = ResourceManager.Instance.GetResourceCount(ResourceType.Bullets);
            if (available <= 0f) return;

            float consumption = GetEffectiveBulletConsumption();
            float actualConsumption = Mathf.Min(consumption, available);
            if (actualConsumption <= 0f) return;

            ResourceManager.Instance.AddResource(ResourceType.Bullets, -actualConsumption);

            float cashEarned = actualConsumption * cashPerBullet * efficiencyMultiplier;
            ResourceManager.Instance.AddResource(ResourceType.Cash, cashEarned);
            OnCashEarned?.Invoke(cashEarned);

            foreach (var turret in bulletTurrets)
                turret.PlayShoot();
        }

        private void ShootRocketTurrets()
        {
            float available = ResourceManager.Instance.GetResourceCount(ResourceType.Rockets);
            if (available <= 0f) return;

            float consumption = GetEffectiveRocketConsumption();
            float actualConsumption = Mathf.Min(consumption, available);
            if (actualConsumption <= 0f) return;

            ResourceManager.Instance.AddResource(ResourceType.Rockets, -actualConsumption);

            float cashEarned = actualConsumption * cashPerRocket * efficiencyMultiplier;
            ResourceManager.Instance.AddResource(ResourceType.Cash, cashEarned);
            OnCashEarned?.Invoke(cashEarned);

            foreach (var turret in rocketTurrets)
                turret.PlayShoot();
        }

        /// <summary>
        /// Updates the Cash base rate in ResourceManager so the display reflects
        /// the turret's effective cash generation per second.
        /// </summary>
        public void RecalculateCashRate()
        {
            float cashRate = 0f;
            float bulletConsumption = 0f;
            float rocketConsumption = 0f;

            if (bulletTurretActive && bulletCooldown > 0f)
            {
                float consumption = GetEffectiveBulletConsumption() / bulletCooldown;
                bulletConsumption = consumption;
                cashRate += consumption * cashPerBullet * efficiencyMultiplier;
            }

            if (rocketTurretActive && rocketCooldown > 0f)
            {
                float consumption = GetEffectiveRocketConsumption() / rocketCooldown;
                rocketConsumption = consumption;
                cashRate += consumption * cashPerRocket * efficiencyMultiplier;
            }

            ResourceManager.Instance.SetBaseRate(ResourceType.Cash, cashRate);
            ResourceManager.Instance.SetConsumptionRate(ResourceType.Bullets, bulletConsumption);
            ResourceManager.Instance.SetConsumptionRate(ResourceType.Rockets, rocketConsumption);
        }

        public void UpgradeTurret()
        {
            turretLevel++;
            if (turretUpgradeMeshes != null && turretLevel < turretUpgradeMeshes.Length)
            {
                for (int i = 0; i < turretUpgradeMeshes.Length; i++)
                    if (turretUpgradeMeshes[i] != null)
                        turretUpgradeMeshes[i].SetActive(i == turretLevel);
            }
            RecalculateCashRate();
            OnTurretUpgraded?.Invoke(turretLevel);
        }

        public void ApplyEfficiencyMultiplier(float multiplier)
        {
            efficiencyMultiplier *= multiplier;
            RecalculateCashRate();
        }

        public void SetEfficiencyMultiplier(float multiplier)
        {
            efficiencyMultiplier = multiplier;
            RecalculateCashRate();
        }

        public void SetBulletTurretActive(bool active)
        {
            bulletTurretActive = active;
            if (bulletTurretObject != null)
                bulletTurretObject.SetActive(active);
            RecalculateCashRate();
        }

        public void SetRocketTurretActive(bool active)
        {
            rocketTurretActive = active;
            if (rocketTurretObject != null)
                rocketTurretObject.SetActive(active);
            RecalculateCashRate();
        }

        public void ResetForPrestige()
        {
            turretLevel = 0;
            efficiencyMultiplier = 1f;
            bulletTurretActive = true;
            rocketTurretActive = false;

            if (bulletTurretObject != null)
                bulletTurretObject.SetActive(true);
            if (rocketTurretObject != null)
                rocketTurretObject.SetActive(false);

            if (turretUpgradeMeshes != null && turretUpgradeMeshes.Length > 0)
            {
                for (int i = 0; i < turretUpgradeMeshes.Length; i++)
                    if (turretUpgradeMeshes[i] != null)
                        turretUpgradeMeshes[i].SetActive(i == 0);
            }

            RecalculateCashRate();
        }

        public float GetBulletConsumptionRate() => GetEffectiveBulletConsumption();
        public float GetRocketConsumptionRate() => GetEffectiveRocketConsumption();
    }
}
