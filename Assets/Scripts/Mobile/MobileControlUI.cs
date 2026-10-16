using UnityEngine;

/// <summary>
/// 根据当前附身的形态，决定屏幕上显示哪几个按钮。
/// 挂在 Canvas 下的一个空物体上，把各个按钮拖进来即可。
///
/// 规则（全部来自 PlayerPossessionController.FixedUpdate 里各个 case 的实际逻辑）：
///   Star       左右 + 跳跃
///   Box        左右 + 变大 / 变小（BoxResizeAbility）
///   Bubble     左右 + 破碎；在水里时额外出现 上下
///   SpikeWall  左右 + 发射
///   Spider     左右 + 脱离
///   Fan        左右 + 脱离
///   BouncyBall 左右
///   gravityBall 全部隐藏（玩家无法控制）
/// </summary>
public class MobileControlsUI : MonoBehaviour
{
    [SerializeField] private PlayerPossessionController controller;

    [Header("方向键")]
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;
    [SerializeField] private GameObject upButton;
    [SerializeField] private GameObject downButton;

    [Header("动作键（每个形态只会亮一个）")]
    [SerializeField] private GameObject jumpButton;   // 星星，Kind = Action
    [SerializeField] private GameObject fireButton;   // 尖刺墙，Kind = Action
    [SerializeField] private GameObject leaveButton;  // 风扇 / 蜘蛛，Kind = Release
    [SerializeField] private GameObject popButton;    // 泡泡，Kind = Release

    [Header("箱子缩放（Kind 同样选 Up / Down）")]
    [Tooltip("变大 = Up，变小 = Down。它们和方向键的上下走同一个 VerticalInput，" +
             "只是摆在右侧、换成放大缩小的图标。")]
    [SerializeField] private GameObject growButton;
    [SerializeField] private GameObject shrinkButton;

    private PossessableBody lastBody;
    private BodyForm lastForm;
    private bool lastInWater;
    private bool lastDead;
    private bool hasCachedState;

    private void Reset()
    {
        controller = FindFirstObjectByType<PlayerPossessionController>();
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        PossessableBody body = controller.CurrentBody;
        bool isDead = controller.IsDead;

        if (body == null)
        {
            if (!hasCachedState || lastBody != null)
            {
                HideAll();
                lastBody = null;
                hasCachedState = true;
            }

            return;
        }

        BodyForm form = body.Form;
        bool inWater = body.IsInWater;

        // 状态没变就什么都不做，避免每帧反复 SetActive。
        if (hasCachedState &&
            lastBody == body &&
            lastForm == form &&
            lastInWater == inWater &&
            lastDead == isDead)
        {
            return;
        }

        lastBody = body;
        lastForm = form;
        lastInWater = inWater;
        lastDead = isDead;
        hasCachedState = true;

        if (isDead)
        {
            HideAll();
            return;
        }

        Apply(form, inWater);
    }

    private void Apply(BodyForm form, bool inWater)
    {
        // 重力球是纯演出，玩家不能操作。
        bool canControl = form != BodyForm.gravityBall;

        // 只有泡泡在水里时才需要上下键：空中泡泡自动上升，verticalInput 不被使用。
        bool needsVertical = canControl &&
            form == BodyForm.Bubble &&
            inWater;

        SetActive(leftButton, canControl);
        SetActive(rightButton, canControl);
        SetActive(upButton, needsVertical);
        SetActive(downButton, needsVertical);

        // 箱子的变大 / 变小。
        bool canResize = form == BodyForm.Box;
        SetActive(growButton, canResize);
        SetActive(shrinkButton, canResize);

        SetActive(jumpButton, form == BodyForm.Star);
        SetActive(fireButton, form == BodyForm.SpikeWall);
        SetActive(leaveButton,
            form == BodyForm.Fan || form == BodyForm.Spider);
        SetActive(popButton, form == BodyForm.Bubble);
    }

    private void HideAll()
    {
        SetActive(leftButton, false);
        SetActive(rightButton, false);
        SetActive(upButton, false);
        SetActive(downButton, false);
        SetActive(growButton, false);
        SetActive(shrinkButton, false);
        SetActive(jumpButton, false);
        SetActive(fireButton, false);
        SetActive(leaveButton, false);
        SetActive(popButton, false);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}