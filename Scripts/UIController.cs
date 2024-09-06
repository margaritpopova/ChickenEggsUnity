using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理 UI
/// </summary>
public class UIController : MonoBehaviour
{
    [Tooltip("玩家的蛋计数器")]
    public TextMeshProUGUI playerEggCounter;

    [Tooltip("NPC的蛋计数器")]
    public TextMeshProUGUI opponentEggCounter;

    [Tooltip("计时器文本")]
    public TextMeshProUGUI timerText;

    [Tooltip("横幅文本")]
    public TextMeshProUGUI bannerText;

    [Tooltip("按钮")]
    public Button button;

    [Tooltip("按钮文本")]
    public TextMeshProUGUI buttonText;

    [Tooltip("控制注释图像")]
    public RawImage CtrlAnnotation;

    /// <summary>
    /// 按钮点击的委托
    /// </summary>
    public delegate void ButtonClick();

    /// <summary>
    /// 在按钮被点击时调用
    /// </summary>
    public ButtonClick OnButtonClicked;

    /// <summary>
    /// 响应按钮点击
    /// </summary>
    public void ButtonClicked()
    {
        if (OnButtonClicked != null) OnButtonClicked();
    }

    /// <summary>
    /// 显示按钮
    /// </summary>
    /// <param name="text">按钮上的文本字符串</param>
    public void ShowButton(string text)
    {
        buttonText.text = text;
        button.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏按钮
    /// </summary>
    public void HideButton()
    {
        button.gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示横幅文本
    /// </summary>
    /// <param name="text">要显示的文本字符串</param>
    public void ShowBanner(string text)
    {
        bannerText.text = text;
        bannerText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 显示控制注释图像
    /// </summary>
    public void ShowCtrlAnotation()
    {
        CtrlAnnotation.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏控制注释图像
    /// </summary>
    public void HideCtrlAnotation()
    {
        CtrlAnnotation.gameObject.SetActive(false);
    }

    /// <summary>
    /// 隐藏横幅文本
    /// </summary>
    public void HideBanner()
    {
        bannerText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置计时器，如果 timeRemaining 为负数，则隐藏文本
    /// </summary>
    /// <param name="timeRemaining">剩余时间（秒）</param>
    public void SetTimer(float timeRemaining)
    {
        if (timeRemaining > 0f)
            timerText.text = timeRemaining.ToString("00");
        else
            timerText.text = "";
    }

    /// <summary>
    /// 设置玩家的蛋数量
    /// </summary>
    /// <param name="EggsAmount">蛋数量</param>
    public void SetPlayerEggsAmount(int EggsAmount)
    {
        playerEggCounter.text = EggsAmount.ToString();
    }

    /// <summary>
    /// 设置NPC的蛋数量
    /// </summary>
    /// <param name="nectarAmount">蛋数量</param>
    public void SetOpponentEggsAmount(float EggsAmount)
    {
        opponentEggCounter.text = EggsAmount.ToString();
    }
}
