using UnityEngine;
using System.Collections;

public class EnvrSoundEvent : MonoBehaviour
{
    [Header("Звук птиц")]
    [SerializeField] private SoundEvent birdsSound;

    [Header("Интервал в секундах")]
    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 6f;

    private Coroutine soundRoutine;

    private void OnEnable()
    {
        soundRoutine = StartCoroutine(PlaySoundRandomly());
    }

    private void OnDisable()
    {
        if (soundRoutine != null)
        {
            StopCoroutine(soundRoutine);
        }
    }

    private IEnumerator PlaySoundRandomly()
    {
        while (true)
        {
            float waitTime = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(waitTime);

            PlayBirdsSound();
        }
    }

    public void PlayBirdsSound()
    {
        if (birdsSound != null)
        {
            birdsSound.Play(transform.position);
        }
    }
}
