using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections;

public class SimpleTrigger : MonoBehaviour
{
    public Sounds soundsManager;
    public Money money;

    [Header("Настройки подлета монетки к игроку")]
    public Transform targetPoint;      // Точка, к которой двигаемся
    public Transform objectToMove;
    public float speed = 2.0f;  
    bool canMonetMove;
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"));
        targetPoint = other.gameObject.transform;
        // Код, который выполняется при входе в триггер
        Debug.Log("Объект вошел в триггер: " + other.gameObject.name);
        money.current_money += 150;
        canMonetMove = true;
        StartCoroutine(DelayFromDestroyTriggerMonet(gameObject));

    }
    public IEnumerator DelayFromDestroyTriggerMonet(GameObject triggerMoneyObj)
    {
        yield return new WaitForSeconds(0.1f);
        Destroy(triggerMoneyObj);
    }

    private void Update()
    {
        // Проверяем, что оба объекта назначены в инспекторе
        if (targetPoint != null && objectToMove != null && canMonetMove == true)
        {
            // Плавно двигаем объект от его текущей позиции к позиции цели
            objectToMove.position = Vector3.MoveTowards(
                objectToMove.position, 
                targetPoint.position, 
                speed * Time.deltaTime
            );
        }
    }
}