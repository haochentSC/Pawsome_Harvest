using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LotteryMachine
{
    public sealed class RewardDisplayBoard : MonoBehaviour
    {
        private const string SimpleGemsAnimTypeName = "Benjathemaker.SimpleGemsAnim";
        private const string SimpleGemsAnimFloatHeightFieldName = "floatHeight";
        private static readonly Vector2 DefaultSlotDisplaySize = new Vector2(0.19f, 0.27f);
        private const float DefaultSlotFillRatio = 0.93f;
        private const float DefaultCompletionTrophyFloatHeight = 0.08f;

        [Serializable]
        public sealed class RewardDisplaySlotConfiguration
        {
            [SerializeField] private RewardDefinition reward;
            [SerializeField] private Transform anchor;
            [SerializeField] private RewardDisplaySlot trigger;

            public RewardDisplaySlotConfiguration()
            {
            }

            public RewardDisplaySlotConfiguration(RewardDefinition reward, Transform anchor, RewardDisplaySlot trigger)
            {
                this.reward = reward;
                this.anchor = anchor;
                this.trigger = trigger;
            }

            public RewardDefinition Reward => reward;
            public Transform Anchor => anchor;
            public RewardDisplaySlot Trigger => trigger;
        }

        [SerializeField] private Vector2 slotDisplaySize = DefaultSlotDisplaySize;
        [SerializeField, Range(0f, 1f)] private float slotFillRatio = DefaultSlotFillRatio;
        [SerializeField] private GameObject completionTrophyPrefab;
        [SerializeField] private Transform completionTrophyAnchor;
        [SerializeField, Min(0f)] private float completionTrophyFloatHeight = DefaultCompletionTrophyFloatHeight;
        [SerializeField] private bool completionTrophyShown;
        [SerializeField] private List<RewardDisplaySlotConfiguration> slots = new();

        private readonly Dictionary<string, RewardDisplaySlotConfiguration> slotByRewardId = new();
        private readonly Dictionary<string, GameObject> placedCardByRewardId = new();
        private GameObject spawnedCompletionTrophy;

        public IReadOnlyList<RewardDisplaySlotConfiguration> Slots => slots;
        public GameObject CompletionTrophyPrefab => completionTrophyPrefab;
        public Transform CompletionTrophyAnchor => completionTrophyAnchor;
        public GameObject SpawnedCompletionTrophy => spawnedCompletionTrophy;
        public bool CompletionTrophyShown => completionTrophyShown && spawnedCompletionTrophy != null;
        public int PlacedCount
        {
            get
            {
                this.RemoveMissingPlacedCards();
                return placedCardByRewardId.Count;
            }
        }

        private void Awake()
        {
            this.RebuildSlotLookup();
            this.ConfigureSlotTriggers();
        }

        private void OnValidate()
        {
            slotDisplaySize = new Vector2(Mathf.Max(0f, slotDisplaySize.x), Mathf.Max(0f, slotDisplaySize.y));
            slotFillRatio = Mathf.Clamp01(slotFillRatio);
            completionTrophyFloatHeight = Mathf.Max(0f, completionTrophyFloatHeight);
            this.ConfigureSlotTriggers();
        }

        public void ConfigureSlots(IEnumerable<RewardDisplaySlotConfiguration> configuredSlots)
        {
            slots.Clear();
            if (configuredSlots != null)
            {
                slots.AddRange(configuredSlots);
            }

            this.RebuildSlotLookup();
            this.ConfigureSlotTriggers();
        }

        public void ConfigureCompletionTrophy(GameObject trophyPrefab, Transform trophyAnchor)
        {
            completionTrophyPrefab = trophyPrefab;
            completionTrophyAnchor = trophyAnchor;
        }

        public bool IsRewardPlaced(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            this.RemoveMissingPlacedCards();
            return placedCardByRewardId.ContainsKey(rewardId);
        }

        public bool HasSlotForRewardId(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            this.RebuildSlotLookup();
            return slotByRewardId.ContainsKey(rewardId);
        }

        public bool CanAcceptCard(RewardCardInstance card)
        {
            return card != null && HasSlotForRewardId(card.RewardId);
        }

        public bool TryPlaceFromCollider(Collider cardCollider)
        {
            if (cardCollider == null)
            {
                return false;
            }

            var card = cardCollider.GetComponentInParent<RewardCardInstance>();
            if (!CanUseTriggerFallback(card))
            {
                return false;
            }

            return TryPlaceCard(card);
        }

        public bool TryPlaceCard(GameObject cardObject)
        {
            if (cardObject == null)
            {
                return false;
            }

            return TryPlaceCard(cardObject.GetComponent<RewardCardInstance>());
        }

        public bool CanUseTriggerFallback(RewardCardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            var grabInteractable = card.GetComponent<XRGrabInteractable>();
            return grabInteractable == null || !grabInteractable.isSelected || grabInteractable.firstInteractorSelecting is XRSocketInteractor;
        }

        public bool TryPlaceCard(RewardCardInstance card)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.RewardId))
            {
                return false;
            }

            this.RebuildSlotLookup();
            this.RemoveMissingPlacedCards();

            if (!slotByRewardId.TryGetValue(card.RewardId, out var slot))
            {
                return false;
            }

            if (placedCardByRewardId.TryGetValue(card.RewardId, out var placedCard) && placedCard != null)
            {
                if (placedCard == card.gameObject)
                {
                    return false;
                }

                DestroyCard(card.gameObject);
                return true;
            }

            this.SnapCardIntoSlot(card, slot);
            placedCardByRewardId[card.RewardId] = card.gameObject;
            this.TryShowCompletionTrophy();
            return true;
        }

        private void RebuildSlotLookup()
        {
            slotByRewardId.Clear();
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Reward == null || string.IsNullOrWhiteSpace(slot.Reward.RewardId) || slot.Anchor == null)
                {
                    continue;
                }

                slotByRewardId[slot.Reward.RewardId] = slot;
            }
        }

        private void ConfigureSlotTriggers()
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.Trigger != null)
                {
                    slot.Trigger.Configure(this);
                }
            }
        }

        private void RemoveMissingPlacedCards()
        {
            var missingRewardIds = ListPool<string>.Get();
            foreach (var pair in placedCardByRewardId)
            {
                if (pair.Value == null)
                {
                    missingRewardIds.Add(pair.Key);
                }
            }

            for (var i = 0; i < missingRewardIds.Count; i++)
            {
                placedCardByRewardId.Remove(missingRewardIds[i]);
            }

            ListPool<string>.Release(missingRewardIds);
        }

        private void TryShowCompletionTrophy()
        {
            if (completionTrophyShown && spawnedCompletionTrophy != null)
            {
                return;
            }

            completionTrophyShown = false;
            var trophyPrefab = this.ResolveCompletionTrophyPrefab();
            if (trophyPrefab == null || completionTrophyAnchor == null)
            {
                return;
            }

            if (!HasCollectedAllConfiguredRewards())
            {
                return;
            }

            spawnedCompletionTrophy = Instantiate(trophyPrefab, completionTrophyAnchor);
            spawnedCompletionTrophy.name = trophyPrefab.name;
            var trophyTransform = spawnedCompletionTrophy.transform;
            trophyTransform.localPosition = Vector3.zero;
            trophyTransform.localRotation = Quaternion.identity;
            trophyTransform.localScale = Vector3.one;

            ConfigureCompletionTrophyVisual(spawnedCompletionTrophy, completionTrophyFloatHeight);
            completionTrophyShown = true;
        }

        private bool HasCollectedAllConfiguredRewards()
        {
            this.RebuildSlotLookup();
            this.RemoveMissingPlacedCards();
            return slotByRewardId.Count > 0 && placedCardByRewardId.Count >= slotByRewardId.Count;
        }

        private GameObject ResolveCompletionTrophyPrefab()
        {
#if UNITY_EDITOR
            if (completionTrophyPrefab != null)
            {
                var trophyPrefabPath = AssetDatabase.GetAssetPath(completionTrophyPrefab);
                if (!string.IsNullOrWhiteSpace(trophyPrefabPath))
                {
                    var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(trophyPrefabPath);
                    if (rootPrefab != null && completionTrophyPrefab != rootPrefab)
                    {
                        completionTrophyPrefab = rootPrefab;
                    }
                }
            }
#endif
            return completionTrophyPrefab;
        }

        private void SnapCardIntoSlot(RewardCardInstance card, RewardDisplaySlotConfiguration slot)
        {
            var cardTransform = card.transform;
            var fittedScale = this.GetFittedCardScale(cardTransform);
            cardTransform.SetParent(slot.Anchor, false);
            cardTransform.localPosition = Vector3.zero;
            cardTransform.localRotation = Quaternion.identity;
            cardTransform.localScale = fittedScale;
            MakeCardDisplayOnly(card.gameObject);
        }

        private Vector3 GetFittedCardScale(Transform cardTransform)
        {
            var localScale = cardTransform.localScale;
            if (slotDisplaySize.x <= Mathf.Epsilon || slotDisplaySize.y <= Mathf.Epsilon)
            {
                return localScale;
            }

            if (!TryGetLocalCardFootprint(cardTransform, out var footprint))
            {
                return localScale;
            }

            var scaledFootprint = new Vector2(
                footprint.x * Mathf.Abs(localScale.x),
                footprint.y * Mathf.Abs(localScale.z));
            if (scaledFootprint.x <= Mathf.Epsilon || scaledFootprint.y <= Mathf.Epsilon)
            {
                return localScale;
            }

            var targetDisplaySize = slotDisplaySize * slotFillRatio;
            var scaleMultiplier = Mathf.Min(targetDisplaySize.x / scaledFootprint.x, targetDisplaySize.y / scaledFootprint.y);
            if (scaleMultiplier <= Mathf.Epsilon || float.IsNaN(scaleMultiplier) || float.IsInfinity(scaleMultiplier))
            {
                return localScale;
            }

            return localScale * scaleMultiplier;
        }

        private static bool TryGetLocalCardFootprint(Transform cardTransform, out Vector2 footprint)
        {
            if (cardTransform.TryGetComponent<BoxCollider>(out var rootBoxCollider))
            {
                footprint = new Vector2(rootBoxCollider.size.x, rootBoxCollider.size.z);
                return footprint.x > Mathf.Epsilon && footprint.y > Mathf.Epsilon;
            }

            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            var hasBounds = false;

            foreach (var cardRenderer in cardTransform.GetComponentsInChildren<Renderer>(true))
            {
                if (cardRenderer != null)
                {
                    EncapsulateLocalBounds(cardTransform, cardRenderer.transform, cardRenderer.localBounds, ref min, ref max, ref hasBounds);
                }
            }

            foreach (var cardCollider in cardTransform.GetComponentsInChildren<Collider>(true))
            {
                if (cardCollider is BoxCollider boxCollider)
                {
                    EncapsulateLocalBounds(cardTransform, boxCollider.transform, new Bounds(boxCollider.center, boxCollider.size), ref min, ref max, ref hasBounds);
                }
                else if (cardCollider != null)
                {
                    EncapsulateWorldBounds(cardTransform, cardCollider.bounds, ref min, ref max, ref hasBounds);
                }
            }

            footprint = hasBounds ? new Vector2(max.x - min.x, max.z - min.z) : Vector2.zero;
            return footprint.x > Mathf.Epsilon && footprint.y > Mathf.Epsilon;
        }

        private static void EncapsulateLocalBounds(
            Transform cardTransform,
            Transform boundsTransform,
            Bounds localBounds,
            ref Vector3 min,
            ref Vector3 max,
            ref bool hasBounds)
        {
            var center = localBounds.center;
            var extents = localBounds.extents;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var localPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        EncapsulateCardLocalPoint(cardTransform.InverseTransformPoint(boundsTransform.TransformPoint(localPoint)), ref min, ref max, ref hasBounds);
                    }
                }
            }
        }

        private static void EncapsulateWorldBounds(
            Transform cardTransform,
            Bounds worldBounds,
            ref Vector3 min,
            ref Vector3 max,
            ref bool hasBounds)
        {
            var center = worldBounds.center;
            var extents = worldBounds.extents;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var worldPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        EncapsulateCardLocalPoint(cardTransform.InverseTransformPoint(worldPoint), ref min, ref max, ref hasBounds);
                    }
                }
            }
        }

        private static void EncapsulateCardLocalPoint(Vector3 point, ref Vector3 min, ref Vector3 max, ref bool hasBounds)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
            hasBounds = true;
        }

        private static void MakeCardDisplayOnly(GameObject cardObject)
        {
            var rigidbody = cardObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }

            var grabInteractable = cardObject.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
            }

            var grabbableReward = cardObject.GetComponent<GrabbableReward>();
            if (grabbableReward != null)
            {
                grabbableReward.enabled = false;
            }

            foreach (var cardCollider in cardObject.GetComponentsInChildren<Collider>())
            {
                cardCollider.enabled = false;
            }
        }

        private static void ConfigureCompletionTrophyVisual(GameObject trophyObject, float floatHeight)
        {
            foreach (var rigidbody in trophyObject.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }

            foreach (var grabInteractable in trophyObject.GetComponentsInChildren<XRGrabInteractable>(true))
            {
                grabInteractable.enabled = false;
            }

            foreach (var grabbableReward in trophyObject.GetComponentsInChildren<GrabbableReward>(true))
            {
                grabbableReward.enabled = false;
            }

            foreach (var trophyCollider in trophyObject.GetComponentsInChildren<Collider>(true))
            {
                trophyCollider.enabled = false;
            }

            foreach (var animation in trophyObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                SetCompletionTrophyFloatHeight(animation, floatHeight);
            }
        }

        private static void SetCompletionTrophyFloatHeight(MonoBehaviour animation, float floatHeight)
        {
            if (animation == null || animation.GetType().FullName != SimpleGemsAnimTypeName)
            {
                return;
            }

            var floatHeightField = animation.GetType().GetField(
                SimpleGemsAnimFloatHeightFieldName,
                BindingFlags.Instance | BindingFlags.Public);
            if (floatHeightField != null && floatHeightField.FieldType == typeof(float))
            {
                floatHeightField.SetValue(animation, floatHeight);
            }
        }

        private static void DestroyCard(GameObject cardObject)
        {
            if (cardObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(cardObject);
            }
            else
            {
                DestroyImmediate(cardObject);
            }
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
