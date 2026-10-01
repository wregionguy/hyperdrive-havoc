using UnityEngine;
using System.Collections;

public class FiveParticlesWhileW : MonoBehaviour
{
    public ParticleSystem particle1;
    public ParticleSystem particle2;
    public ParticleSystem particle3;
    public ParticleSystem particle4;
    public ParticleSystem particle5;

    private Coroutine stopCoroutine;

    void Start()
    {
        StopAllParticles();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (stopCoroutine != null)
                StopCoroutine(stopCoroutine);

            PlayAllParticles();
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            stopCoroutine = StartCoroutine(DelayedStop());
        }
    }

    void PlayAllParticles()
    {
        RestartParticle(particle1);
        RestartParticle(particle2);
        RestartParticle(particle3);
        RestartParticle(particle4);
        RestartParticle(particle5);
    }

    void RestartParticle(ParticleSystem particle)
    {
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play();
    }

    IEnumerator DelayedStop()
    {
        // Stop creating new particles immediately
        particle1.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        particle2.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        particle3.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        particle4.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        particle5.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        // Existing particles remain for 1 second
        yield return new WaitForSeconds(2f);

        StopAllParticles();
    }

    void StopAllParticles()
    {
        particle1.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle2.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle3.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle4.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle5.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}