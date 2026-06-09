using UnityEngine;

/// <summary>
/// ノベルゲームの音楽再生を担当するクラス。
/// 初期版ではBGMのみ扱う。
/// </summary>
public class NovelAudioManager : MonoBehaviour
{
    [Header("BGM再生用AudioSource")]
    [SerializeField] private AudioSource bgmSource;

    /// <summary>
    /// BGMを再生する。
    /// すでに同じ曲が再生中なら、最初から再生し直さない。
    /// </summary>
    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("bgmSourceが設定されていません。");
            return;
        }

        if (clip == null)
        {
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// BGMを停止する。
    /// </summary>
    public void StopBgm()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = null;
    }
}