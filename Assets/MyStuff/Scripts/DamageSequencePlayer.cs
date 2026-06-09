using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
/// <summary>
/// /// ワールド座標だけで「particle ↁEdamage popup」を再生する管琁E��ラス
/// /// Canvas / RectTransform は使わなぁE
/// /// BattleManager から征E��めE��ぁE��ぁE�� Routine 形式�EラチE��ーも用意してぁE��、E
/// /// SpellData に登録した個別 particle めE
/// /// 単発ヒッチE/ 多段ヒット�E両方で使える、E
/// /// /// こ�E版では、多段ヒット時の popup は 
/// /// 「同じ場所」に「少しずつ時間差で」表示する、E
/// /// 位置のランダムずらしや段階的な位置オフセチE��は行わなぁE��E
/// /// </summary> 
public class DamageSequencePlayer : MonoBehaviour
{
    public enum ShakeTargetType
    {
        None,
        Player,
        Enemy,
    }

    [Serializable]
    public struct DamageHitRequest
    {
        public DamageTargetAnchor target; public int damage;
        public OneShotParticleCallback effectPrefab;
        public AudioClip effectSe;
        public ShakeTargetType shakeTargetType;
        public bool suppressHitSound;
        public bool suppressPopup;
        public bool isHealing;
    }

    [Header("Parents (Optional)")]
    [SerializeField] private Transform effectParent;
    [SerializeField] private Transform popupParent;

    [Header("Prefabs")]
    [SerializeField] private OneShotParticleCallback hitEffectPrefab;
    [SerializeField] private DamageNumberPopup damagePopupPrefab;

    [Header("Effect")]
    [SerializeField] private float effectScale = 1f;
    [SerializeField] private string effectSortingLayerName = "Effect";
    [SerializeField] private int effectSortingOrder = 100;

    [Header("Attack Effect Audio")]
    [SerializeField] private AudioSource effectAudioSource;
    [SerializeField, Range(0f, 1f)] private float effectSeVolume = 1f;

    [Header("Popup")]
    [SerializeField] private string popupSortingLayerName = "DamageText";
    [SerializeField] private int popupBaseSortingOrder = 200;
    [SerializeField] private int popupSortingOrderStep = 1;
    [SerializeField] private float popupSpawnDelayAfterEffect = 0f;
    [SerializeField] private Color damagePopupColor = Color.white;
    [SerializeField] private Color healingPopupColor = new Color(0.35f, 1f, 0.45f, 1f);

    [Header("Sequence")]
    [SerializeField] private float delayBetweenSequentialHits = 0.05f;
    [SerializeField] private bool allowPopupWithoutEffect = true;
    [SerializeField, Min(0.1f)] private float maxEffectWaitSeconds = 3f;

    [Header("Multi Hit Popup")]
    [SerializeField] private float multiHitPopupInterval = 0.08f;


    [Header("Hit Shake")]
    [SerializeField] private Transform playerShakeTarget;
    [SerializeField] private Transform enemyShakeTarget;
    [SerializeField] private float shakeDuration = 0.14f;
    [SerializeField] private float shakeStrength = 0.08f;
    [SerializeField] private int shakeVibrato = 18;
    [SerializeField] private float shakeRandomness = 90f;

    [Header("Hit Audio")]
    [SerializeField] private AudioSource hitAudioSource;
    [SerializeField] private AudioClip defaultHitSe;
    [SerializeField] private AudioClip playerHitSe;
    [SerializeField] private AudioClip enemyHitSe;
    [SerializeField, Range(0f, 1f)] private float hitSeVolume = 1f;

    private readonly Dictionary<Transform, Tween> activeShakeTweens = new Dictionary<Transform, Tween>();
    private readonly Dictionary<Transform, Vector3> cachedShakeLocalPositions = new Dictionary<Transform, Vector3>();

    public Coroutine PlayHit(DamageTargetAnchor target, int damage, Action onFinished = null)
    {
        return StartCoroutine(CoPlayHit(target, damage, ShakeTargetType.None, null, onFinished, null));
    }

    public Coroutine PlayHit(DamageTargetAnchor target, int damage, OneShotParticleCallback effectPrefab, Action onFinished = null)
    {
        return StartCoroutine(CoPlayHit(target, damage, ShakeTargetType.None, effectPrefab, onFinished, null));
    }

    public Coroutine PlayHit(DamageTargetAnchor target, int damage, ShakeTargetType shakeTargetType, OneShotParticleCallback effectPrefab, Action onFinished = null)
    {
        return StartCoroutine(CoPlayHit(target, damage, shakeTargetType, effectPrefab, onFinished, null));
    }

    public Coroutine PlayMultiHit(DamageHitRequest[] requests, Action onFinished = null)
    {
        return StartCoroutine(CoPlayMultiHitSameEffect(requests, onFinished));
    }

    public Coroutine PlayMultiHit(IEnumerable<DamageHitRequest> requests, Action onFinished = null)
    {
        return StartCoroutine(CoPlayMultiHitSameEffect(requests, onFinished));
    }

    public Coroutine PlayHitsSequentially(IEnumerable<DamageHitRequest> requests, Action onFinished = null)
    {
        return StartCoroutine(CoPlayHitsSequentially(requests, onFinished));
    }

    public IEnumerator PlayHitRoutine(DamageTargetAnchor target, int damage)
    {
        yield return CoPlayHit(target, damage, ShakeTargetType.None, null, null, null);
    }

    public IEnumerator PlayHitRoutine(DamageTargetAnchor target, int damage, OneShotParticleCallback effectPrefab)
    {
        yield return CoPlayHit(target, damage, ShakeTargetType.None, effectPrefab, null, null);
    }

    public IEnumerator PlayHitRoutine(DamageTargetAnchor target, int damage, ShakeTargetType shakeTargetType, OneShotParticleCallback effectPrefab)
    {
        yield return CoPlayHit(target, damage, shakeTargetType, effectPrefab, null, null);
    }

    public IEnumerator PlayHitRoutine(DamageTargetAnchor target, int damage, ShakeTargetType shakeTargetType, OneShotParticleCallback effectPrefab, AudioClip effectSe)
    {
        yield return CoPlayHit(target, damage, shakeTargetType, effectPrefab, null, effectSe);
    }

    public IEnumerator PlayHitRoutine(DamageTargetAnchor target, int damage, ShakeTargetType shakeTargetType, OneShotParticleCallback effectPrefab, AudioClip effectSe, bool suppressHitSound, bool suppressPopup = false, bool isHealing = false)
    {
        yield return CoPlayHit(target, damage, shakeTargetType, effectPrefab, null, effectSe, suppressHitSound, suppressPopup, isHealing);
    }

    public IEnumerator PlayMultiHitRoutine(IEnumerable<DamageHitRequest> requests)
    {
        yield return CoPlayMultiHitSameEffect(requests);
    }

    public IEnumerator PlayMultiHitRoutine(DamageHitRequest[] requests)
    {
        yield return CoPlayMultiHitSameEffect(requests);
    }

    public IEnumerator CoPlayHit(
     DamageTargetAnchor target,
     int damage,
     ShakeTargetType shakeTargetType = ShakeTargetType.None,
     OneShotParticleCallback effectPrefab = null,
     Action onFinished = null,
     AudioClip effectSe = null,
     bool suppressHitSound = false,
     bool suppressPopup = false,
     bool isHealing = false)
    {
        if (target == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        yield return CoPlayOneInternal(
            target,
            damage,
            shakeTargetType,
            popupBaseSortingOrder,
            effectPrefab,
            effectSe,
            suppressHitSound,
            suppressPopup,
            isHealing);   // ↁEnull ではなぁEeffectSe を渡ぁE
        onFinished?.Invoke();
    } /// <summary> 
      /// 多段ヒット用、E
      /// /// effect は1回だけ�E生し、popup は褁E��回�E時間差ありで出す、E
      /// /// popup の位置は毎回同じ、E
      /// /// </summary>
    public IEnumerator CoPlayMultiHitSameEffect(IEnumerable<DamageHitRequest> requests, Action onFinished = null)
    {
        List<DamageHitRequest> requestList = CollectValidRequests(requests);
        if (requestList.Count == 0)
        {
            onFinished?.Invoke(); yield break;
        }
        DamageTargetAnchor effectAnchor = requestList[0].target;
        OneShotParticleCallback selectedEffectPrefab = requestList[0].effectPrefab != null ? requestList[0].effectPrefab : hitEffectPrefab;
        if (selectedEffectPrefab != null && effectAnchor != null)
        {
            bool effectFinished = false; SpawnEffect(selectedEffectPrefab, requestList[0].effectSe, effectAnchor.GetEffectWorldPosition(), () => effectFinished = true);
            yield return WaitForEffectFinishedOrTimeout(() => effectFinished, selectedEffectPrefab);
        }
        else if (!allowPopupWithoutEffect) { onFinished?.Invoke(); yield break; }
        if (popupSpawnDelayAfterEffect > 0f)
        {
            yield return WaitForSecondsSafe(popupSpawnDelayAfterEffect);
        }

        for (int i = 0; i < requestList.Count; i++)
        {
            DamageHitRequest request = requestList[i];
            if (request.target == null) { continue; }
            int popupSortingOrder = popupBaseSortingOrder + (i * popupSortingOrderStep);
            if (!request.suppressPopup)
            {
                SpawnPopup(request.target.GetDamageWorldPosition(), request.damage, popupSortingOrder, request.shakeTargetType, request.suppressHitSound, request.isHealing);
            }
            if (i < requestList.Count - 1 && multiHitPopupInterval > 0f)
            {
                yield return WaitForSecondsSafe(multiHitPopupInterval);
            }
        }
        onFinished?.Invoke();
    } /// <summary> 
      /// ヒットごとに effect ↁEpopup を頁E��に再生したぁE��合用、E
      /// /// </summary> 
    public IEnumerator CoPlayHitsSequentially(IEnumerable<DamageHitRequest> requests, Action onFinished = null)
    {
        List<DamageHitRequest> requestList = CollectValidRequests(requests);
        if (requestList.Count == 0)
        {
            onFinished?.Invoke(); yield break;
        }
        for (int i = 0; i < requestList.Count; i++)
        {
            int sortingOrder = popupBaseSortingOrder + (i * popupSortingOrderStep);
            yield return CoPlayOneInternal(requestList[i].target, requestList[i].damage, requestList[i].shakeTargetType, sortingOrder, requestList[i].effectPrefab, requestList[i].effectSe, requestList[i].suppressHitSound, requestList[i].suppressPopup, requestList[i].isHealing);
            if (i < requestList.Count - 1 && delayBetweenSequentialHits > 0f)
            {
                yield return WaitForSecondsSafe(delayBetweenSequentialHits);
            }
        }
        onFinished?.Invoke();
    }
    private IEnumerator CoPlayOneInternal(DamageTargetAnchor target, int damage, ShakeTargetType shakeTargetType, int popupSortingOrder, OneShotParticleCallback overrideEffectPrefab, AudioClip overrideEffectSe = null, bool suppressHitSound = false, bool suppressPopup = false, bool isHealing = false)
    {
        if (target == null) { yield break; }
        OneShotParticleCallback selectedEffectPrefab = overrideEffectPrefab != null ? overrideEffectPrefab : hitEffectPrefab;
        if (selectedEffectPrefab != null)
        {
            bool effectFinished = false;
            SpawnEffect(selectedEffectPrefab, overrideEffectSe, target.GetEffectWorldPosition(), () => effectFinished = true);
            yield return WaitForEffectFinishedOrTimeout(() => effectFinished, selectedEffectPrefab);
        }
        else if (!allowPopupWithoutEffect) { yield break; }
        if (popupSpawnDelayAfterEffect > 0f)
        {
            yield return WaitForSecondsSafe(popupSpawnDelayAfterEffect);
        }
        if (!suppressPopup)
        {
            SpawnPopup(target.GetDamageWorldPosition(), damage, popupSortingOrder, shakeTargetType, suppressHitSound, isHealing);
        }
    }
    private OneShotParticleCallback SpawnEffect(OneShotParticleCallback effectPrefab, AudioClip effectSe, Vector3 worldPosition, Action onFinished)
    {
        if (effectPrefab == null)
        {
            onFinished?.Invoke();
            return null;
        }
        OneShotParticleCallback effect = InstantiateWorldObject(effectPrefab, worldPosition, effectParent);
        PlayAttackEffectSound(effectSe, worldPosition);
        effect.transform.localScale = Vector3.one * effectScale;
        ApplyParticleSorting(effect.gameObject, effectSortingLayerName, effectSortingOrder);
        effect.Play(onFinished);
        return effect;
    }

    private IEnumerator WaitForEffectFinishedOrTimeout(Func<bool> effectFinishedGetter, OneShotParticleCallback effectPrefab)
    {
        float waitTime = 0f;
        float timeout = Mathf.Max(0.1f, maxEffectWaitSeconds);

        while (!effectFinishedGetter() && waitTime < timeout)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        if (!effectFinishedGetter())
        {
            string effectName = effectPrefab != null ? effectPrefab.name : "(null)";
            Debug.LogWarning($"DamageSequencePlayer: Particle effect '{effectName}' did not finish within {timeout:0.00} seconds. Check Looping or Stop Action settings.");
        }
    }
    private DamageNumberPopup SpawnPopup(Vector3 worldPosition, int damage, int popupSortingOrder, ShakeTargetType shakeTargetType, bool suppressHitSound = false, bool isHealing = false)
    {
        if (!suppressHitSound)
        {
            TriggerHitShake(shakeTargetType);
            PlayHitSound(shakeTargetType, worldPosition);
        }

        if (damagePopupPrefab == null) { return null; }
        DamageNumberPopup popup = InstantiateWorldObject(damagePopupPrefab, worldPosition, popupParent);
        popup.SetSorting(popupSortingLayerName, popupSortingOrder);
        popup.SetDigitColor(isHealing ? healingPopupColor : damagePopupColor);
        popup.SetWorldPosition(worldPosition);
        popup.Play(damage);
        return popup;
    }


    private void PlayAttackEffectSound(AudioClip clip, Vector3 worldPosition)
    {
        if (clip == null)
        {
            return;
        }

        if (effectAudioSource != null)
        {
            effectAudioSource.PlayOneShot(clip, effectSeVolume);
            return;
        }

        Camera mainCamera = Camera.main;
        Vector3 playPosition = mainCamera != null ? mainCamera.transform.position : worldPosition;
        AudioSource.PlayClipAtPoint(clip, playPosition, effectSeVolume);
    }


    private void PlayHitSound(ShakeTargetType shakeTargetType, Vector3 worldPosition)
    {
        AudioClip clip = ResolveHitSoundClip(shakeTargetType);
        if (clip == null)
        {
            return;
        }

        if (hitAudioSource != null)
        {
            hitAudioSource.PlayOneShot(clip, hitSeVolume);
            return;
        }

        Camera mainCamera = Camera.main;
        Vector3 playPosition = mainCamera != null ? mainCamera.transform.position : worldPosition;
        AudioSource.PlayClipAtPoint(clip, playPosition, hitSeVolume);
    }

    private AudioClip ResolveHitSoundClip(ShakeTargetType shakeTargetType)
    {
        switch (shakeTargetType)
        {
            case ShakeTargetType.Player:
                return playerHitSe != null ? playerHitSe : defaultHitSe;

            case ShakeTargetType.Enemy:
                return enemyHitSe != null ? enemyHitSe : defaultHitSe;

            default:
                return defaultHitSe;
        }
    }

    private void TriggerHitShake(ShakeTargetType shakeTargetType)
    {
        Transform target = GetShakeTargetTransform(shakeTargetType);
        if (target == null || shakeDuration <= 0f || shakeStrength <= 0f)
        {
            return;
        }

        if (!cachedShakeLocalPositions.ContainsKey(target))
        {
            cachedShakeLocalPositions[target] = target.localPosition;
        }

        if (activeShakeTweens.TryGetValue(target, out Tween activeTween) && activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill(false);
        }

        target.localPosition = cachedShakeLocalPositions[target];

        Tween tween = target.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false, true)
            .SetUpdate(false)
            .OnKill(() => RestoreShakeTargetPosition(target))
            .OnComplete(() => RestoreShakeTargetPosition(target));

        activeShakeTweens[target] = tween;
    }

    private Transform GetShakeTargetTransform(ShakeTargetType shakeTargetType)
    {
        switch (shakeTargetType)
        {
            case ShakeTargetType.Player:
                return playerShakeTarget;

            case ShakeTargetType.Enemy:
                return enemyShakeTarget;

            default:
                return null;
        }
    }

    private void RestoreShakeTargetPosition(Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (cachedShakeLocalPositions.TryGetValue(target, out Vector3 cachedLocalPosition))
        {
            target.localPosition = cachedLocalPosition;
        }

        if (activeShakeTweens.TryGetValue(target, out Tween tween) && (tween == null || !tween.IsActive()))
        {
            activeShakeTweens.Remove(target);
        }
    }

    private void OnDisable()
    {
        ResetAllShakeTargets();
    }

    private void OnDestroy()
    {
        ResetAllShakeTargets();
    }

    private void ResetAllShakeTargets()
    {
        foreach (KeyValuePair<Transform, Tween> pair in activeShakeTweens)
        {
            if (pair.Value != null && pair.Value.IsActive())
            {
                pair.Value.Kill(false);
            }
        }

        activeShakeTweens.Clear();

        foreach (KeyValuePair<Transform, Vector3> pair in cachedShakeLocalPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.localPosition = pair.Value;
            }
        }
    }
    private static T InstantiateWorldObject<T>(T prefab, Vector3 worldPosition, Transform parent) where T : Component
    {
        T instance = Instantiate(prefab, worldPosition, prefab.transform.rotation);
        if (parent != null)
        {
            instance.transform.SetParent(parent, true);
        }
        instance.transform.position = worldPosition;
        return instance;
    }

    private static void ApplyParticleSorting(GameObject root, string sortingLayerName, int sortingOrder)
    {
        ParticleSystemRenderer[] renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = sortingLayerName; renderers[i].sortingOrder = sortingOrder;
        }
    }
    private static List<DamageHitRequest> CollectValidRequests(IEnumerable<DamageHitRequest> requests)
    {
        List<DamageHitRequest> list = new List<DamageHitRequest>();
        if (requests == null) { return list; }
        foreach (DamageHitRequest request in requests)
        {
            if (request.target != null)
            {
                list.Add(request);
            }
        }
        return list;
    }

    private static IEnumerator WaitForSecondsSafe(float seconds)
    {
        float time = 0f;
        while (time < seconds)
        {
            time += Time.deltaTime;
            yield return null;
        }
    }
}
