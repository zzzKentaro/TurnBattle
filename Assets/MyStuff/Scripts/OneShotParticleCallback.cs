using System;
using UnityEngine;

/// <summary>
/// 1回再生した ParticleSystem が停止したら callback を呼んで自壊する。
/// Looping は OFF 前提。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class OneShotParticleCallback : MonoBehaviour
{
    private ParticleSystem particleSystemRef;
    private Action onFinished;

    private void Awake()
    {
        particleSystemRef = GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystemRef.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    public void Play(Action finishedCallback)
    {
        onFinished = finishedCallback;

        if (particleSystemRef == null)
        {
            particleSystemRef = GetComponent<ParticleSystem>();
        }

        particleSystemRef.Play(true);
    }

    private void OnParticleSystemStopped()
    {
        onFinished?.Invoke();
        Destroy(gameObject);
    }
}
