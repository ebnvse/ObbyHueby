using JetBrains.Annotations;
using UnityEngine;
using YG;

public class DeviceChecker : MonoBehaviour
{
    public GameObject Canvas_Mobile_Input;
    public AttackController attackController;
    public void MyMethod() //пометочка из документации 
    {
        // deviceType может возвращать: desktop, mobile, tablet, tv
        string deviceStr = YG2.envir.deviceType;

        // Есть bool поля: isDesktop, isMobile, isTablet, isTV
        bool deviceIsDesktop = YG2.envir.isDesktop;

        // Поле enum:
        YG2.Device device = YG2.envir.device;
    }
    public void Start()
    {
        string deviceStr = YG2.envir.deviceType;
        switch (deviceStr)
        {
            case "desktop":
                Debug.Log("Комп");
                Canvas_Mobile_Input.SetActive(false);
                attackController.allowMouseClickAttack = true;
                break;
            case "mobile":
                Debug.Log("телефон");
                Canvas_Mobile_Input.SetActive(true);
                attackController.allowMouseClickAttack = false;
                break;
            default:
                Debug.Log("телефон");
                Canvas_Mobile_Input.SetActive(true);
                attackController.allowMouseClickAttack = false;
                break;
        }    
    }
}
