using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "NewSoundLibrary", menuName = "Audio/Sound Interface")]
public class SoundLibrary : ScriptableObject
{
    public AudioClip[] sounds;

    public AudioClip GetSound(int index)
    {
        if (sounds == null || index < 0 || index >= sounds.Length)
        {
            Debug.LogWarning($"Звук с индексом {index} не найден!");
            return null;
        }
        return sounds[index];
    }
}