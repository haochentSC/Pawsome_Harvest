using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace LotteryMachine
{
    [RequireComponent(typeof(Collider))]
    public sealed class RewardDisplaySlot : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private RewardDisplayBoard board;
        [SerializeField] private XRSocketInteractor socketInteractor;

        public RewardDisplayBoard Board => board;
        public XRSocketInteractor SocketInteractor => socketInteractor;
        public bool canProcess => isActiveAndEnabled;

        public void Configure(RewardDisplayBoard displayBoard)
        {
            Configure(displayBoard, null, null);
        }

        public void Configure(RewardDisplayBoard displayBoard, XRSocketInteractor socket, Transform attachTransform)
        {
            board = displayBoard;
            if (socket != null)
            {
                socketInteractor = socket;
            }

            this.ConfigureTriggerCollider();
            this.ConfigureSocketInteractor(attachTransform);
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return board != null && board.CanAcceptCard(GetCardInstance(interactable));
        }

        public bool TryPlaceSocketInteractable(IXRSelectInteractable interactable)
        {
            if (board == null)
            {
                return false;
            }

            return board.TryPlaceCard(GetCardInstance(interactable));
        }

        private void Reset()
        {
            board = GetComponentInParent<RewardDisplayBoard>();
            socketInteractor = GetComponent<XRSocketInteractor>();
            this.ConfigureTriggerCollider();
            this.ConfigureSocketInteractor(null);
        }

        private void Awake()
        {
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
                this.RegisterSocketFilter();
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
            if (board == null)
            {
                board = GetComponentInParent<RewardDisplayBoard>();
            }

            if (socketInteractor == null)
            {
                socketInteractor = GetComponent<XRSocketInteractor>();
            }

            this.ConfigureTriggerCollider();
            this.ConfigureSocketInteractor(null);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPlace(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryPlace(other);
        }

        private void TryPlace(Collider other)
        {
            if (board == null || other == null)
            {
                return;
            }

            board.TryPlaceFromCollider(other);
        }

        private void OnSocketSelectEntered(SelectEnterEventArgs args)
        {
            TryPlaceSocketInteractable(args.interactableObject);
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
            socketInteractor.socketSnappingRadius = 0.18f;
            socketInteractor.recycleDelayTime = 0.05f;
            this.RegisterSocketFilter();
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

        private static RewardCardInstance GetCardInstance(IXRSelectInteractable interactable)
        {
            return interactable?.transform != null ? interactable.transform.GetComponentInParent<RewardCardInstance>() : null;
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
