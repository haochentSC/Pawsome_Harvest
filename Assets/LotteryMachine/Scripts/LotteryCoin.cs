using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace LotteryMachine
{
    [DisallowMultipleComponent]
    public sealed class LotteryCoin : MonoBehaviour
    {
        private const string SimpleGemsAnimTypeName = "Benjathemaker.SimpleGemsAnim";

        [SerializeField] private LotteryGameManager gameManager;
        [SerializeField] private bool countedInInventory;
        [SerializeField] private bool countPickupInInventory = true;

        private XRGrabInteractable grabInteractable;

        public LotteryGameManager GameManager
        {
            get => gameManager;
            set => gameManager = value;
        }

        public bool CountedInInventory => countedInInventory;
        public bool CountPickupInInventory
        {
            get => countPickupInInventory;
            set => countPickupInInventory = value;
        }

        public static LotteryCoin PrepareCoinObject(
            GameObject coinObject,
            bool disablePresentationAnimation,
            LotteryGameManager owner = null)
        {
            if (coinObject == null)
            {
                return null;
            }

            var coin = coinObject.GetComponent<LotteryCoin>();
            if (coin == null)
            {
                coin = coinObject.AddComponent<LotteryCoin>();
            }

            EnsureCollider(coinObject);
            EnsureRigidbody(coinObject);
            DisableGemPresentationAnimation(coinObject);
            coin.grabInteractable = EnsureGrabInteractable(coinObject);
            if (coin.isActiveAndEnabled)
            {
                coin.RegisterGrabListener();
            }

            if (owner != null)
            {
                coin.gameManager = owner;
            }

            return coin;
        }

        public static LotteryCoin GetCoin(IXRSelectInteractable interactable)
        {
            return interactable?.transform != null ? interactable.transform.GetComponentInParent<LotteryCoin>() : null;
        }

        public void MarkCountedInInventory()
        {
            countedInInventory = true;
        }

        public void ClearCountedInInventory()
        {
            countedInInventory = false;
        }

        public void SetPickupCountingEnabled(bool enabled)
        {
            countPickupInInventory = enabled;
        }

        public bool RegisterPickup()
        {
            if (!countPickupInInventory)
            {
                return false;
            }

            var manager = ResolveGameManager();
            return manager != null && manager.TryRegisterCoinPickup(this);
        }

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            DisableGemPresentationAnimation(gameObject);
        }

        private void OnEnable()
        {
            if (grabInteractable == null)
            {
                grabInteractable = GetComponent<XRGrabInteractable>();
            }

            RegisterGrabListener();
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
            {
                return;
            }

            RegisterPickup();
        }

        private void RegisterGrabListener()
        {
            if (grabInteractable == null)
            {
                return;
            }

            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectEntered.AddListener(OnSelectEntered);
        }

        private LotteryGameManager ResolveGameManager()
        {
            if (gameManager != null)
            {
                return gameManager;
            }

            gameManager = FindFirstObjectByType<LotteryGameManager>();
            return gameManager;
        }

        private static void EnsureCollider(GameObject coinObject)
        {
            if (coinObject.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            var collider = coinObject.AddComponent<BoxCollider>();
            ConfigureColliderFromRenderers(coinObject, collider);
        }

        private static void EnsureRigidbody(GameObject coinObject)
        {
            var rigidbody = coinObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = coinObject.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
        }

        private static XRGrabInteractable EnsureGrabInteractable(GameObject coinObject)
        {
            var grabInteractable = coinObject.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = coinObject.AddComponent<XRGrabInteractable>();
            }

            return grabInteractable;
        }

        private static void ConfigureColliderFromRenderers(GameObject coinObject, BoxCollider collider)
        {
            var renderers = coinObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                collider.center = Vector3.zero;
                collider.size = Vector3.one * 0.08f;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var transform = coinObject.transform;
            collider.center = transform.InverseTransformPoint(bounds.center);
            var localSize = transform.InverseTransformVector(bounds.size);
            collider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private static void DisableGemPresentationAnimation(GameObject coinObject)
        {
            var behaviours = coinObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().FullName == SimpleGemsAnimTypeName)
                {
                    behaviour.enabled = false;
                }
            }
        }
    }
}
