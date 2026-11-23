using System.Collections.Generic;
using UnityEngine;

public class AudioPoolManager : MonoBehaviour
{
    public static AudioPoolManager Instance { get; private set; }

    [SerializeField] private int poolSize = 25;
    private List<AudioSource> pool;
    private int currentIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePool();
    }

    private void InitializePool()
    {
        pool = new List<AudioSource>(poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = new GameObject($"PooledAudioSource_{i}");
            obj.transform.SetParent(transform);

            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;

            pool.Add(source);
        }
    }

    public void PlaySound(SoundEvent soundEvent, Vector3 position, float volumeMultiplier = 1f)
    {
        if (soundEvent == null || soundEvent.clips == null || soundEvent.clips.Length == 0)
            return;

        AudioSource source = GetAvailableSource();

        source.transform.position = position;
        source.clip = soundEvent.clips[Random.Range(0, soundEvent.clips.Length)];
        source.outputAudioMixerGroup = soundEvent.mixerGroup;

        source.volume = Random.Range(soundEvent.minVolume, soundEvent.maxVolume) * volumeMultiplier;
        source.pitch = Random.Range(soundEvent.minPitch, soundEvent.maxPitch);

        source.spatialBlend = soundEvent.spatialBlend;
        source.maxDistance = soundEvent.maxDistance;

        source.Play();
    }

    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].isPlaying)
            {
                return pool[i];
            }
        }

        AudioSource oldestSource = pool[currentIndex];
        currentIndex = (currentIndex + 1) % pool.Count;
        return oldestSource;
    }
}
