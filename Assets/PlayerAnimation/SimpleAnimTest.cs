using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleAnimTest : MonoBehaviour
{
    public Animator animator;

    // Слой 0 (Ноги)
    private readonly int IdleHash = Animator.StringToHash("Idle");
    private readonly int WalkHash = Animator.StringToHash("Walk");
    private readonly int RunHash = Animator.StringToHash("Run");
    private int currentBaseAnim;

    // Слой 1 (Верх тела / Атаки)
    private readonly string[] AttackNames = { "Attack1", "Attack2" };
    private string currentAttackName;
    private bool isAttacking;
    private float upperLayerWeight;

    void Update()
    {
        UpdateBaseLayer();
        UpdateUpperBodyLayer();
    }

    void UpdateBaseLayer()
    {
        bool w = Keyboard.current != null && Keyboard.current.wKey.isPressed;
        bool shift = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        int targetAnim = w ? (shift ? RunHash : WalkHash) : IdleHash;

        if (currentBaseAnim != targetAnim)
        {
            currentBaseAnim = targetAnim;
            animator.CrossFadeInFixedTime(currentBaseAnim, 0.15f, 0);
        }
    }

    void UpdateUpperBodyLayer()
    {
        if (!isAttacking && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isAttacking = true;
            currentAttackName = AttackNames[Random.Range(0, AttackNames.Length)];
            animator.Play(currentAttackName, 1, 0f);
        }

        if (isAttacking)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(1);
            if (state.IsName(currentAttackName) && state.normalizedTime >= 0.8f)
            {
                isAttacking = false;
            }
        }

        float targetWeight = isAttacking ? 1f : 0f;
        upperLayerWeight = Mathf.MoveTowards(upperLayerWeight, targetWeight, 8f * Time.deltaTime);
        animator.SetLayerWeight(1, upperLayerWeight);
    }
}
