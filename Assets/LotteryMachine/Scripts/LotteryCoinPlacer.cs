using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace LotteryMachine
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class LotteryCoinPlacer : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private LotteryGameManager gameManager;
        [SerializeField] private XRSocketInteractor socketInteractor;
        [SerializeField, Min(0.01f)] private float socketSnappingRadius = 0.18f;
        [SerializeField] private UnityEvent coinInserted = new();
        [SerializeField] private UnityEvent coinConsumed = new();

        private int armedCoinCount;

        public LotteryGameManager GameManager
        {
            get => gameManager;
            set => gameManager = value;
        }

        public XRSocketInteractor SocketInteractor => socketInteractor;
        public int ArmedCoinCount => armedCoinCount;
        public bool HasArmedCoin => armedCoinCount > 0;
        public UnityEvent CoinInsertedEvent => coinInserted;
        public UnityEvent CoinConsumedEvent => coinConsumed;
        public bool canProcess => isActiveAndEnabled;

        public void Configure(XRSocketInteractor socket, Transform attachTransform)
        {
            if (socket != null)
            {
                socketInteractor = socket;
            }

            ConfigureTriggerCollider();
            ConfigureSocketInteractor(attachTransform);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return !HasArmedCoin && LotteryCoin.GetCoin(interactable) != null;
        }

        public bool TryPlaceSocketInteractable(IXRSelectInteractable interactable)
        {
            if (!Process(null, interactable))
            {
                return false;
            }

            armedCoinCount = 1;
            coinInserted.Invoke();

            var coin = LotteryCoin.GetCoin(interactable);
            ResolveGameManager(coin)?.TrySpendCoinFromInventory(coin);
            DestroyCoinObject(coin != null ? coin.gameObject : interactable.transform.gameObject);
            return true;
        }

        public bool TryConsumeArmedCoin()
        {
            if (!HasArmedCoin)
            {
                return false;
            }

            armedCoinCount--;
            coinConsumed.Invoke();
            return true;
        }

        public void ClearArmedCoins()
        {
            armedCoinCount = 0;
        }

        private void Reset()
        {
            gameManager = GetComponentInParent<LotteryGameManager>();
            socketInteractor = GetComponent<XRSocketInteractor>();
            ConfigureTriggerCollider();
            ConfigureSocketInteractor(null);
        }

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponentInParent<LotteryGameManager>();
            }

            if (socketInteractor == null)
            {
                socketInteractor = GetComponent<XRSocketInteractor>();
            }
        }

        private void OnEnable()
        {
            if (socketInteractor == null)
            {
                socketInteractor = GetComponent<XRSocketInteractor>();
            }

            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.AddListener(OnSocketSelectEntered);
                RegisterSocketFilter();
            }
        }

        private void OnDisable()
        {
            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.RemoveListener(OnSocketSelectEntered);
                socketInteractor.selectFilters.Remove(this);
            }
        }

        private void OnValidate()
        {
            if (gameManager == null)
            {
                gameManager = GetComponentInParent<LotteryGameManager>();
            }

            if (socketInteractor == null)
            {
                socketInteractor = GetComponent<XRSocketInteractor>();
            }

            ConfigureTriggerCollider();
            ConfigureSocketInteractor(null);
        }

        private void OnSocketSelectEntered(SelectEnterEventArgs args)
        {
            TryPlaceSocketInteractable(args.interactableObject);
        }

        private LotteryGameManager ResolveGameManager(LotteryCoin coin)
        {
            if (gameManager != null)
            {
                return gameManager;
            }

            if (coin != null && coin.GameManager != null)
            {
                gameManager = coin.GameManager;
                return gameManager;
            }

            gameManager = GetComponentInParent<LotteryGameManager>();
            if (gameManager != null)
            {
                return gameManager;
            }

            gameManager = FindFirstObjectByType<LotteryGameManager>();
            return gameManager;
        }

        private void ConfigureTriggerCollider()
        {
            var triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        private void ConfigureSocketInteractor(Transform attachTransform)
        {
            if (socketInteractor == null)
            {
                socketInteractor = GetComponent<XRSocketInteractor>();
            }

            if (socketInteractor == null)
            {
                return;
            }

            if (attachTransform != null)
            {
                socketInteractor.attachTransform = attachTransform;
            }

            socketInteractor.socketActive = true;
            socketInteractor.hoverSocketSnapping = true;
            socketInteractor.showInteractableHoverMeshes = false;
            socketInteractor.socketScaleMode = SocketScaleMode.None;
            socketInteractor.socketSnappingRadius = socketSnappingRadius;
            socketInteractor.recycleDelayTime = 0.05f;
            RegisterSocketFilter();
        }

        private void RegisterSocketFilter()
        {
            if (socketInteractor == null)
            {
                return;
            }

            if (!socketInteractor.startingSelectFilters.Contains(this))
            {
                socketInteractor.startingSelectFilters.Add(this);
            }

            if (!ContainsRuntimeSelectFilter(socketInteractor, this))
            {
                socketInteractor.selectFilters.Add(this);
            }
        }

        private static bool ContainsRuntimeSelectFilter(XRSocketInteractor socket, IXRSelectFilter filter)
        {
            var filters = ListPool<IXRSelectFilter>.Get();
            socket.selectFilters.GetAll(filters);
            var containsFilter = filters.Contains(filter);
            ListPool<IXRSelectFilter>.Release(filters);
            return containsFilter;
        }

        private static void DestroyCoinObject(GameObject coinObject)
        {
            if (coinObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(coinObject);
            }
            else
            {
                DestroyImmediate(coinObject);
            }
        }

        private static class ListPool<T>
        {
            private static readonly System.Collections.Generic.Stack<System.Collections.Generic.List<T>> Pool = new();

            public static System.Collections.Generic.List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new System.Collections.Generic.List<T>();
            }

            public static void Release(System.Collections.Generic.List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
