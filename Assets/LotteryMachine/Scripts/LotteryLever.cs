using UnityEngine;
using UnityEngine.Events;

namespace LotteryMachine
{
    public sealed class LotteryLever : MonoBehaviour
    {
        [SerializeField] private LotteryMachine lotteryMachine;
        [SerializeField] private LotteryCoinPlacer coinPlacer;
        [SerializeField] private Transform leverVisual;
        [SerializeField, Range(0f, 90f)] private float pulledAngle = 55f;
        [SerializeField, Min(0f)] private float returnSpeed = 260f;
        [SerializeField] private UnityEvent pulled = new();

        private Quaternion restRotation;
        private Quaternion targetRotation;
        private bool returning;

        public UnityEvent PulledEvent => pulled;
        public LotteryCoinPlacer CoinPlacer
        {
            get => coinPlacer;
            set => coinPlacer = value;
        }

        private void Awake()
        {
            if (leverVisual == null)
            {
                leverVisual = transform;
            }

            restRotation = leverVisual.localRotation;
            targetRotation = restRotation;
        }

        private void Update()
        {
            if (!returning || leverVisual == null)
            {
                return;
            }

            leverVisual.localRotation = Quaternion.RotateTowards(leverVisual.localRotation, targetRotation, returnSpeed * Time.deltaTime);
            if (Quaternion.Angle(leverVisual.localRotation, targetRotation) <= 0.1f)
            {
                leverVisual.localRotation = targetRotation;
                returning = false;
            }
        }

        public bool Pull()
        {
            if (coinPlacer != null && !coinPlacer.HasArmedCoin)
            {
                return false;
            }

            if (lotteryMachine != null && !lotteryMachine.TryStartDraw())
            {
                return false;
            }

            if (coinPlacer != null)
            {
                coinPlacer.TryConsumeArmedCoin();
            }

            if (leverVisual != null)
            {
                leverVisual.localRotation = restRotation * Quaternion.Euler(pulledAngle, 0f, 0f);
                returning = true;
            }

            pulled.Invoke();
            return true;
        }

        private void OnMouseDown()
        {
            Pull();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Pull();
            }
        }
    }
}
