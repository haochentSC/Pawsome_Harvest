using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace LotteryMachine
{
    public sealed class LotteryMachine : MonoBehaviour
    {
        [Header("Rewards")]
        [SerializeField] private RewardPool rewardPool;
        [SerializeField] private Transform capsuleSpawnPoint;
        [SerializeField] private Transform cardRevealPoint;
        [SerializeField] private Transform rewardParent;
        [SerializeField] private GameObject capsulePrefab;

        [Header("Feedback")]
        [SerializeField] private ParticleSystem rewardRevealEffect;
        [SerializeField] private AudioSource rewardRevealAudioSource;
        [SerializeField] private AudioClip rewardRevealSound;
        [SerializeField, Range(0f, 1f)] private float rewardRevealVolume = 0.9f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float capsuleDropDuration = 0.65f;
        [SerializeField, Min(0f)] private float revealDelay = 0.45f;
        [SerializeField, Min(0f)] private float cardRiseDuration = 0.35f;

        [Header("Events")]
        [SerializeField] private RewardResultEvent rewardCompleted = new();
        [SerializeField] private UnityEvent drawStarted = new();
        [SerializeField] private UnityEvent drawFailed = new();

        private GameObject currentRewardObject;
        private GameObject currentCapsuleObject;
        private Coroutine drawRoutine;
        private int drawIndex;

        public event System.Action<RewardResult> RewardCompleted;
        public event System.Action DrawStarted;
        public event System.Action DrawFailed;

        public RewardPool RewardPool
        {
            get => rewardPool;
            set => rewardPool = value;
        }

        public bool IsDrawing => drawRoutine != null;
        public RewardResultEvent RewardCompletedEvent => rewardCompleted;
        public UnityEvent DrawStartedEvent => drawStarted;
        public UnityEvent DrawFailedEvent => drawFailed;

        public bool TryStartDraw()
        {
            if (IsDrawing)
            {
                return false;
            }

            if (rewardPool == null || !rewardPool.TryDraw(out var reward))
            {
                Debug.LogWarning("Lottery draw failed because no drawable reward is available.", this);
                drawFailed.Invoke();
                DrawFailed?.Invoke();
                return false;
            }

            drawRoutine = StartCoroutine(RunDraw(reward));
            return true;
        }

        public void StartDraw()
        {
            TryStartDraw();
        }

        public void ClearCurrentReward()
        {
            if (ShouldPreserveCurrentReward())
            {
                currentRewardObject = null;
                return;
            }

            DestroyIfPresent(currentRewardObject);
            currentRewardObject = null;
        }

        public bool DrawImmediateForTests(out RewardResult result)
        {
            result = default;
            if (rewardPool == null || !rewardPool.TryDraw(out var reward))
            {
                drawFailed.Invoke();
                DrawFailed?.Invoke();
                return false;
            }

            drawIndex++;

            PlayRewardRevealFeedback();
            var spawnedReward = SpawnReward(reward);
            result = new RewardResult(reward, spawnedReward, drawIndex);
            rewardCompleted.Invoke(result);
            RewardCompleted?.Invoke(result);
            return true;
        }

        private IEnumerator RunDraw(RewardDefinition reward)
        {
            drawIndex++;
            drawStarted.Invoke();
            DrawStarted?.Invoke();

            SpawnCapsule();

            if (currentCapsuleObject != null && capsuleSpawnPoint != null && cardRevealPoint != null)
            {
                yield return MoveOverTime(currentCapsuleObject.transform, capsuleSpawnPoint.position, cardRevealPoint.position, capsuleDropDuration);
            }
            else if (capsuleDropDuration > 0f)
            {
                yield return new WaitForSeconds(capsuleDropDuration);
            }

            if (revealDelay > 0f)
            {
                yield return new WaitForSeconds(revealDelay);
            }

            DestroyIfPresent(currentCapsuleObject);
            currentCapsuleObject = null;

            PlayRewardRevealFeedback();
            var spawnedReward = SpawnReward(reward);
            if (spawnedReward != null && cardRevealPoint != null && cardRiseDuration > 0f)
            {
                var start = cardRevealPoint.position + Vector3.down * 0.18f;
                var end = cardRevealPoint.position;
                spawnedReward.transform.position = start;
                yield return MoveOverTime(spawnedReward.transform, start, end, cardRiseDuration);
            }

            var result = new RewardResult(reward, spawnedReward, drawIndex);
            rewardCompleted.Invoke(result);
            RewardCompleted?.Invoke(result);
            drawRoutine = null;
        }

        private void SpawnCapsule()
        {
            DestroyIfPresent(currentCapsuleObject);

            if (capsulePrefab == null || capsuleSpawnPoint == null)
            {
                return;
            }

            currentCapsuleObject = Instantiate(capsulePrefab, capsuleSpawnPoint.position, capsuleSpawnPoint.rotation, rewardParent);
        }

        private void PlayRewardRevealFeedback()
        {
            PlayRewardRevealEffect();
            PlayRewardRevealSound();
        }

        private void PlayRewardRevealEffect()
        {
            if (rewardRevealEffect == null)
            {
                return;
            }

            rewardRevealEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rewardRevealEffect.Play(true);
        }

        private void PlayRewardRevealSound()
        {
            if (rewardRevealAudioSource == null || rewardRevealSound == null)
            {
                return;
            }

            rewardRevealAudioSource.PlayOneShot(rewardRevealSound, rewardRevealVolume);
        }

        private GameObject SpawnReward(RewardDefinition reward)
        {
            if (reward == null || reward.RewardPrefab == null || cardRevealPoint == null)
            {
                return null;
            }

            currentRewardObject = Instantiate(reward.RewardPrefab, cardRevealPoint.position, cardRevealPoint.rotation, rewardParent);
            EnsureRewardIdentity(currentRewardObject, reward);
            EnsureRewardIsGrabbable(currentRewardObject);
            return currentRewardObject;
        }

        private bool ShouldPreserveCurrentReward()
        {
            if (currentRewardObject == null)
            {
                return false;
            }

            var grabbableReward = currentRewardObject.GetComponent<GrabbableReward>();
            return grabbableReward != null && grabbableReward.HasBeenPickedUp;
        }

        private static void EnsureRewardIsGrabbable(GameObject rewardObject)
        {
            if (rewardObject == null)
            {
                return;
            }

            if (rewardObject.GetComponent<Collider>() == null)
            {
                var boxCollider = rewardObject.AddComponent<BoxCollider>();
                ConfigureColliderFromRenderers(rewardObject, boxCollider);
            }

            var rigidbody = rewardObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = rewardObject.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            if (rewardObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() == null)
            {
                rewardObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            }

            if (rewardObject.GetComponent<GrabbableReward>() == null)
            {
                rewardObject.AddComponent<GrabbableReward>();
            }
        }

        private static void EnsureRewardIdentity(GameObject rewardObject, RewardDefinition reward)
        {
            if (rewardObject == null)
            {
                return;
            }

            var rewardInstance = rewardObject.GetComponent<RewardCardInstance>();
            if (rewardInstance == null)
            {
                rewardInstance = rewardObject.AddComponent<RewardCardInstance>();
            }

            rewardInstance.Initialize(reward);
        }

        private static void ConfigureColliderFromRenderers(GameObject rewardObject, BoxCollider boxCollider)
        {
            var renderers = rewardObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                boxCollider.size = Vector3.one * 0.35f;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            var localCenter = rewardObject.transform.InverseTransformPoint(bounds.center);
            var localSize = rewardObject.transform.InverseTransformVector(bounds.size);
            boxCollider.center = localCenter;
            boxCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        private static IEnumerator MoveOverTime(Transform target, Vector3 start, Vector3 end, float duration)
        {
            if (target == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                target.position = end;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            target.position = end;
        }

        private static void DestroyIfPresent(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                ClearEditorSelectionIfUnderRoot(instance);
                DisableTextMeshProComponents(instance);
                DestroyImmediate(instance);
            }
        }

        private static void ClearEditorSelectionIfUnderRoot(GameObject root)
        {
#if UNITY_EDITOR
            foreach (var selectedObject in UnityEditor.Selection.objects)
            {
                if (IsEditorSelectionUnderRoot(selectedObject, root))
                {
                    UnityEditor.Selection.objects = new UnityEngine.Object[0];
                    return;
                }
            }
#endif
        }

#if UNITY_EDITOR
        private static bool IsEditorSelectionUnderRoot(UnityEngine.Object selectedObject, GameObject root)
        {
            if (selectedObject == null || root == null)
            {
                return false;
            }

            if (selectedObject == root)
            {
                return true;
            }

            if (selectedObject is GameObject selectedGameObject)
            {
                return selectedGameObject.transform.IsChildOf(root.transform);
            }

            if (selectedObject is Component selectedComponent)
            {
                return selectedComponent != null && selectedComponent.transform.IsChildOf(root.transform);
            }

            return false;
        }
#endif

        private static void DisableTextMeshProComponents(GameObject instance)
        {
            foreach (var behaviour in instance.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Namespace == "TMPro")
                {
                    behaviour.enabled = false;
                }
            }
        }
    }
}
