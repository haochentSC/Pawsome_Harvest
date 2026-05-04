using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace LotteryMachine
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class LotteryCoinCounterDepositSocket : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private LotteryCoinCounterStation station;
        [SerializeField] private XRSocketInteractor socketInteractor;
        [SerializeField, Min(0.01f)] private float socketSnappingRadius = 0.18f;

        public LotteryCoinCounterStation Station
        {
            get => station;
            set => station = value;
        }

        public bool canProcess => isActiveAndEnabled;

        public void Configure(LotteryCoinCounterStation counterStation, XRSocketInteractor socket, Transform attachTransform)
        {
            if (counterStation != null)
            {
                station = counterStation;
            }

            if (socket != null)
            {
                socketInteractor = socket;
            }

            ConfigureTriggerCollider();
            ConfigureSocketInteractor(attachTransform);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            var coin = LotteryCoin.GetCoin(interactable);
            return coin != null && !coin.CountedInInventory;
        }

        public bool TryPlaceSocketInteractable(IXRSelectInteractable interactable)
        {
            if (!Process(null, interactable))
            {
                return false;
            }

            var coin = LotteryCoin.GetCoin(interactable);
            if (ResolveStation() == null || !station.TryDepositCoin(coin))
            {
                return false;
            }

            DestroyCoinObject(coin != null ? coin.gameObject : interactable.transform.gameObject);
            return true;
        }

        private void Reset()
        {
            station = GetComponentInParent<LotteryCoinCounterStation>();
            socketInteractor = GetComponent<XRSocketInteractor>();
            ConfigureTriggerCollider();
            ConfigureSocketInteractor(null);
        }

        private void Awake()
        {
            ResolveStation();
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
            if (station == null)
            {
                station = GetComponentInParent<LotteryCoinCounterStation>();
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

        private LotteryCoinCounterStation ResolveStation()
        {
            if (station != null)
            {
                return station;
            }

            station = GetComponentInParent<LotteryCoinCounterStation>();
            return station;
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
