using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Money : MonoBehaviour
{
    public TextMeshProUGUI  money_text;
    public int current_money;

    public void Update()
    {
        money_text.text = current_money.ToString();
    } 

}
