using UnityEngine;
using YG;

public class RewarBuf : MonoBehaviour
{
    public Sharpening sharpening;
    public string rewardID;
    public int coins;

	// Вызов рекламы за вознаграждение
    public void MyRewardAdvShow()
    {
        YG2.RewardedAdvShow(rewardID, () =>
        {

			// По желанию, воспользуйтесь ID вознаграждения
			if (rewardID == "UpSpeedSharpening")
				sharpening.sharpenDuration = 1.3f;
        });
    }
}
