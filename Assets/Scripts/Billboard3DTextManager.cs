using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class Billboard3DTextManager : MonoBehaviour
{
    [Header("Настройки")]
    public string targetLayerName = "3dText"; // Слой для 3D-текста и картинок
    public Camera targetCamera;               // Главная камера (найдется сама, если пусто)

    private List<Transform> targetTransforms = new List<Transform>();
    private int targetLayer;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        targetLayer = LayerMask.NameToLayer(targetLayerName);

        if (targetLayer == -1)
        {
            Debug.LogWarning($"Слой '{targetLayerName}' не найден! Проверь название в Tags & Layers.");
            return;
        }

        RefreshList();
    }

    // Собираем все 3D-тексты, UI-картинки (Image) и 2D-спрайты (SpriteRenderer) на нужном слое
    public void RefreshList()
    {
        targetTransforms.Clear();

        // Находим все объекты на сцене (включая выключенные)
        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in allTransforms)
        {
            if (t.gameObject.layer == targetLayer)
            {
                // Проверяем, что на объекте есть текст или картинка (чтобы не крутить пустые контейнеры)
                bool hasVisual = t.GetComponent<TMP_Text>() != null || 
                                 t.GetComponent<Image>() != null || 
                                 t.GetComponent<SpriteRenderer>() != null;

                if (hasVisual)
                {
                    targetTransforms.Add(t);
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        Quaternion cameraRotation = targetCamera.transform.rotation;

        for (int i = targetTransforms.Count - 1; i >= 0; i--)
        {
            if (targetTransforms[i] == null)
            {
                targetTransforms.RemoveAt(i);
                continue;
            }

            // Поворачиваем лицевой стороной прямо в камеру
            targetTransforms[i].rotation = cameraRotation;
        }
    }
}
