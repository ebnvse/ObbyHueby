using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class GamepadTester : MonoBehaviour
{
    void Update()
    {
        // Проверяем, подключен ли хотя бы один геймпад
        if (Gamepad.current == null)
        {
            return;
        }

        // 1. Проверяем все кнопки (включая нажатия на стики, крестовину, бамперы и триггеры)
        foreach (var control in Gamepad.current.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
            {
                Debug.Log($"[Кнопка нажата]: {button.name} ({button.displayName})");
            }
        }

        // 2. Проверяем левый стик (добавлена мертвая зона 0.2f, чтобы консоль не спамила от дрифта)
        Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
        if (leftStick.magnitude > 0.2f)
        {
            Debug.Log($"[Левый стик]: {leftStick}");
        }

        // 3. Проверяем правый стик
        Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
        if (rightStick.magnitude > 0.2f)
        {
            Debug.Log($"[Правый стик]: {rightStick}");
        }
    }
}
