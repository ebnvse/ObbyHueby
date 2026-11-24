using UnityEngine;
using YG;

public class RewarBuf : MonoBehaviour
{
    [Header("Ссылки")]
    public Sharpening sharpening;

    [Header("Настройки награды")]
    public string rewardID;
    public int coins;
    [Tooltip("Новая длительность заточки после просмотра рекламы")]
    public float boostedSharpenDuration = 0.5f;

    // Вызов рекламы за вознаграждение
    public void MyRewardAdvShow()
    {
        // Если в момент нажатия на рекламу шла заточка — аккуратно отменяем её, чтобы не зависла при паузе рекламы
        if (sharpening != null)
        {
            sharpening.CancelSharpening();
        }

        YG2.RewardedAdvShow(rewardID, () =>
        {
            if (rewardID == "UpSpeedSharpening" && sharpening != null)
            {
                sharpening.SetSharpenDuration(boostedSharpenDuration);
            }
        });
    }
}
