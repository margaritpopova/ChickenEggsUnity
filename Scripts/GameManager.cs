using System.Collections;
using UnityEngine;

/// <summary>
/// 管理游戏逻辑并控制UI
/// </summary>
public class GameManager : MonoBehaviour
{
    [Tooltip("游戏在代理孵化出10个蛋时结束")]
    public int maxEggsCount = 10;

    [Tooltip("游戏在经过这么多秒后结束")]
    public float timerAmount = 45f;

    [Tooltip("The UI Controller")]
    public UIController uiController;

    [Tooltip("The player chichen")]
    public ChickenAgent player;

    [Tooltip("The ML-Agent NPC")]
    public ChickenAgent opponent;

    [Tooltip("树木区域")]
    public TreesArea treesArea;

    [Tooltip("场景的主摄像头")]
    public Camera mainCamera;

    public AudioSource audioSource;

    // 游戏计时器开始时
    private float gameTimerStartTime;

    /// <summary>
    /// 所有可能的游戏状态
    /// </summary>
    public enum GameState
    {
        Default,
        MainMenu,
        Preparing,
        Playing,
        Gameover
    }

    /// <summary>
    /// 当前游戏状态
    /// </summary>
    public GameState State { get; private set; } = GameState.Default;

    /// <summary>
    /// 获取游戏剩余时间
    /// </summary>
    public float TimeRemaining
    {
        get
        {
            if (State == GameState.Playing)
            {
                float timeRemaining = timerAmount - (Time.time - gameTimerStartTime);
                return Mathf.Max(0f, timeRemaining);
            }
            else
            {
                return 0f;
            }
        }
    }

    /// <summary>
    /// 处理在不同状态下的按钮点击
    /// </summary>
    public void ButtonClicked()
    {
        if (State == GameState.Gameover)
        {
            // 在Gameover状态下，按钮点击应返回主菜单
            MainMenu();
        }
        else if (State == GameState.MainMenu)
        {
            // 在MainMenu单状态下，按钮点击应开始游戏
            StartCoroutine(StartGame());
        }
        else
        {
            Debug.LogWarning("Button clicked in unexpected state: " + State.ToString());
        }
    }

    /// <summary>
    /// 游戏开始时调用
    /// </summary>
    private void Start()
    {
        // 注视用户界面的按钮点击事件
        uiController.OnButtonClicked += ButtonClicked;

        // 启动主菜单
        MainMenu();
    }

    /// <summary>
    /// 销毁时调用
    /// </summary>
    private void OnDestroy()
    {
        // 不监视用户界面的按钮点击事件
        uiController.OnButtonClicked -= ButtonClicked;
    }

    /// <summary>
    /// 显示主菜单
    /// </summary>
    private void MainMenu()
    {
        // 将状态设置为"MainMenu"
        State = GameState.MainMenu;

        // 重新UI
        uiController.ShowBanner("");
        uiController.ShowCtrlAnotation();
        uiController.ShowButton("Start");

        // 使用主摄像头，禁用角色摄像头
        mainCamera.gameObject.SetActive(true);
        player.agentCamera.gameObject.SetActive(false);
        opponent.agentCamera.gameObject.SetActive(false); // 永远不再用

        // 重置巢穴
        treesArea.ResetTrees();

        // 重置代理
        player.OnEpisodeBegin();
        opponent.OnEpisodeBegin();

        // 冻结代理
        player.FreezeAgent();
        opponent.FreezeAgent();
    }

    /// <summary>
    /// 用倒计时开始游戏
    /// </summary>
    /// <returns>IEnumerator</returns>
    private IEnumerator StartGame()
    {
        // 将状态设置为 "preparing"
        State = GameState.Preparing;

        // 更新 UI 
        uiController.ShowBanner("");
        uiController.HideButton();
        uiController.HideCtrlAnotation();

        // 使用玩家摄像头，禁用主摄像头
        mainCamera.gameObject.SetActive(false);
        player.agentCamera.gameObject.SetActive(true);

        audioSource.Play();

        // 显示倒计时
        uiController.ShowBanner("3");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("2");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("1");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("Go!");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("");

        // 将状态设置为 "playing"
        State = GameState.Playing;

        // 启动游戏计时器
        gameTimerStartTime = Time.time;

        // 解冻角色
        player.UnfreezeAgent();
        opponent.UnfreezeAgent();

        
    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    private void EndGame()
    {
        // 将状态设置为 "game over"
        State = GameState.Gameover;

        // 冻结角色
        player.FreezeAgent();
        opponent.FreezeAgent();

        // 根据胜利/失败更新文本
        if (player.eggObtained >= opponent.eggObtained )
        {
            uiController.ShowBanner("You win!");
        }
        else
        {
            uiController.ShowBanner("ML-Agent win!");
        }
        audioSource.Stop();

        // 更新按钮文本
        uiController.ShowButton("Main Menu");
    }

    /// <summary>
    /// 每帧调用
    /// </summary>
    private void Update()
    {
        if (State == GameState.Playing)
        {
            // 检查时间是否用尽或任一代理达到最大蛋数
            if (TimeRemaining <= 0f ||
                player.eggObtained >= maxEggsCount ||
                opponent.eggObtained >= maxEggsCount)
            {
                EndGame();
            }

            // 更新计时器和蛋计数器
            uiController.SetTimer(TimeRemaining);
            uiController.SetPlayerEggsAmount(player.eggObtained);
            uiController.SetOpponentEggsAmount(opponent.eggObtained);

            if (Input.GetKey("escape"))
            {
            Application.Quit();
            }
        }
        else if (State == GameState.Preparing || State == GameState.Gameover)
        {
            // 更新计时器
            uiController.SetTimer(TimeRemaining);
            if (Input.GetKey("escape"))
            {
            Application.Quit();
            }
        }
        else
        {
            // 隐藏计时器
            uiController.SetTimer(-1f);

            // 更新进度计数器
            uiController.SetPlayerEggsAmount(0);
            uiController.SetOpponentEggsAmount(0);
        }

        

    }
}
