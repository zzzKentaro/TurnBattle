using UnityEngine;

/// <summary>
/// アニメーションする魔法陣のビュー制御を行う。
/// </summary>
public class AnimatedMagicCircleView : MonoBehaviour
{
    [Header("Particle Prefab")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool parentInstanceToSpawnPoint = true;

    private GameObject spawnedInstance;

    public bool IsVisible => spawnedInstance != null;
    public GameObject CurrentInstance => spawnedInstance;

    public void ShowImmediate()
    {
        Show();
    }

    public void Show()
    {
        if (particlePrefab == null)
        {
            Debug.LogWarning($"{nameof(AnimatedMagicCircleView)} on {name}: particlePrefab が未設定です。", this);
            return;
        }

        if (spawnedInstance != null)
        {
            return;
        }

        Transform anchor = spawnPoint != null ? spawnPoint : transform;

        if (parentInstanceToSpawnPoint)
        {
            spawnedInstance = Instantiate(particlePrefab, anchor, false);

            // 親と同じ位置・回転にそろえる
            spawnedInstance.transform.localPosition = Vector3.zero;
            spawnedInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            spawnedInstance = Instantiate(particlePrefab, anchor.position, anchor.rotation);
        }

        spawnedInstance.name = $"{particlePrefab.name}_Instance";
        PlayAllParticles(spawnedInstance);
    }

    public void Hide()
    {
        ResetImmediate();
    }

    public void ResetImmediate()
    {
        if (spawnedInstance == null)
        {
            return;
        }

        SafeDestroy(spawnedInstance);
        spawnedInstance = null;
    }

    private static void PlayAllParticles(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
            {
                continue;
            }

            systems[i].Clear(true);
            systems[i].Play(true);
        }
    }

    private static void SafeDestroy(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
