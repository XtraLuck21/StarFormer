using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把一串 Sprite 当成动画播放（序列帧动画）。
/// WebGL 上比 VideoPlayer 可靠得多：没有编码格式问题，没有浏览器自动播放限制。
///
/// 用法：
///   1. 挂在一个带 Image 组件的 UI 物体上。
///   2. 把录好的 PNG 序列按顺序拖进 Frames 数组。
///   3. 调 Play() 开始，Stop() 停止。HelpBookUI 会自动调用。
///
/// 注意：本脚本用 unscaledDeltaTime 计时，所以游戏暂停（Time.timeScale = 0）时
/// 演示动画依然会播放。帮助面板暂停游戏时这是必须的。
/// </summary>
[RequireComponent(typeof(Image))]
public class SpriteSequencePlayer : MonoBehaviour
{
    [Header("动画帧")]
    [Tooltip("按播放顺序排列的 Sprite。建议用 Unity Recorder 的 Image Sequence 导出。")]
    [SerializeField] private Sprite[] frames;

    [Header("播放设置")]
    [Tooltip("每秒播放多少帧。演示类动画 12 比较合适，太高会浪费包体。")]
    [SerializeField, Range(1f, 30f)] private float framesPerSecond = 12f;

    [Tooltip("播放到最后一帧后回到第一帧继续播。")]
    [SerializeField] private bool loop = true;

    [Tooltip("物体启用时自动开始播放。")]
    [SerializeField] private bool playOnEnable = true;

    private Image targetImage;
    private int currentFrame;
    private float timeSinceLastFrame;
    private bool isPlaying;

    /// <summary>是否有可播放的帧。没有帧时外部可以据此隐藏整个演示区。</summary>
    public bool HasFrames => frames != null && frames.Length > 0;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        isPlaying = false;
    }

    private void Update()
    {
        if (!isPlaying || !HasFrames || frames.Length == 1)
        {
            return;
        }

        // 用 unscaledDeltaTime 而不是 deltaTime：
        // deltaTime 在 Time.timeScale = 0 时恒为 0，动画会卡住不动。
        timeSinceLastFrame += Time.unscaledDeltaTime;

        float secondsPerFrame = 1f / framesPerSecond;

        if (timeSinceLastFrame < secondsPerFrame)
        {
            return;
        }

        // 用 while 而不是 if：一帧卡顿很久时可以一次跳过多帧，避免动画整体变慢。
        while (timeSinceLastFrame >= secondsPerFrame)
        {
            timeSinceLastFrame -= secondsPerFrame;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = frames.Length - 1;
                    isPlaying = false;
                    break;
                }
            }
        }

        targetImage.sprite = frames[currentFrame];
    }

    /// <summary>从第一帧开始播放。</summary>
    public void Play()
    {
        if (!HasFrames)
        {
            return;
        }

        currentFrame = 0;
        timeSinceLastFrame = 0f;
        isPlaying = true;
        targetImage.sprite = frames[0];
    }

    /// <summary>停在当前帧。</summary>
    public void Stop()
    {
        isPlaying = false;
    }

    /// <summary>运行时换一整套帧，比如从桌面版演示切成手机版演示。</summary>
    public void SetFrames(Sprite[] newFrames, bool autoPlay = true)
    {
        frames = newFrames;

        if (autoPlay)
        {
            Play();
        }
    }
}