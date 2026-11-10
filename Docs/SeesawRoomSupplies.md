# 跷跷板房间：蓝绿、黄色通道与四次撞击终局

本次按确认方案保留手动摆放的四个左侧齿轮、向右调整的 Gate、长板、地面和黄紫按钮最终位置。镜头始终固定为完整房间构图，解锁及动画过程中不平移、不缩放。

## 初始状态

- 蓝绿按钮、齿轮和小球玩法保持。
- 上下固定的蓝色、绿色按钮均绘制锁图案，跷跷板上蓝色、绿色接触块均绘制钥匙图案，表示随板移动的钥匙需要接触同色的锁。黄色按钮显示黄色六边形标志，与当前 Spider 的占位外形一致，并使用深色描边增强辨识度。`SeesawButtonSymbol` 使用高对比深色线条和浅色按钮边框，在编辑器和运行时均可见，不依赖 emoji 字体；图案跟随按钮姿态及原有显隐规则，不改变碰撞或触发条件。
- 黄色按钮、紫色按钮在运行时隐藏，碰撞触发器关闭；编辑器中的位置仍是最终出现位置，便于调整。
- 右侧延伸地板初始隐藏且没有实体碰撞。
- 初始活动区左右各有淡红色半透明挡板（世界 X=25、X=31，颜色 RGB=0.9/0.22/0.22、Alpha=0.28，与普通小球同色）。右侧碰撞位置沿用手动调整结果，图像在水平方向与碰撞体对齐，可见高度收在地板和天花板之间。
- 两侧挡板阻止玩家形态通过，普通小球在生成时分别登记放行。两侧物理与机关持续运行，风可把球吹过挡板触发对应倾斜机关。
- 两侧开局挡板加宽至 0.6 世界单位（原 0.22），图像和碰撞宽度一致；可见高度继续贯通地面与天花板，保留原位置和淡红色半透明外观。
- 四齿轮右侧（世界 X=14.4）有独立的永久能量墙，将装饰齿轮区与玩家路线隔开。墙体、亮边和流动纹平时完全透明，实体延伸至天花板上方和悬崖清理区下方，不能绕过端点。能量墙保留玩家实体碰撞，使用 Spider 贴面查询排除的 `Ignore Raycast` 层，因此 Spider 不能把它作为可攀爬表面；新生和已有普通小球放行。蓝绿解锁只移除原开局屏障，齿轮区能量墙保留。
- `SeesawGearBarrier` 复用能量墙亮边、流动和碰撞闪光。玩家 Star／Spider 实际撞到墙时，墙面与亮边以半透明蓝色闪现（墙面最大 Alpha=0.38），在约 0.9 秒内淡回透明；齿轮墙不生成或显示文字提示。原开局两道能量墙继续使用常显红色及各自原有提示。
- `SeesawGateFeedback` 在两道屏障边缘绘制亮红描边，内部能量纹缓慢上移。受控 Star 或 Fan 实际撞上屏障时闪亮约 0.35 秒；本次房间首次撞击显示 `ONLY BALLS / CAN PASS` 约 2.8 秒，两侧、两种形态共用一次提示。持续抵住墙不会反复闪光，离开后再次撞击会重新闪亮；已贴墙的 Fan 被附身时也可通过持续接触回调触发反馈。开始解锁时这些提示隐藏，原碎裂动画继续播放。
- 悬空小球生成点由 `SeesawBallSpawnMarker` 仅绘制虚线圆环，不显示文字；轮廓跟随生成位置并保持世界空间圆形。球掉落后轮廓仍保留，蓝绿完成、风扇阶段结束时隐藏。
- 开局控制本房间 Fan 后，第一次按左右方向以及之后每次左右转向都会启动吹风，无需反复按 F。供球遵循单球规则：优先释放生成器内唯一的待机球；场上已有普通球时，转向只调整风向，不生成第二颗。球在移动、晃动或停住时都计入，直到离场／清理后才允许补下一颗；每次转向即时刷新数量，避免定时扫描尚未更新导致重复生成。持续按住或松开后重按同方向不会重复触发。球先悬空晃两次（约 0.6 秒），再恢复重力下落；暂停／死亡时不推进。原 F／触屏开风操作仍可选用，后续黄色三球批次沿用原逻辑。

## 蓝绿完成后的约 2 秒动画

蓝绿两个齿轮都开始转动时，`SeesawRoomStage` 一次性记录完成进度并播放联动：

- 蓝绿解谜期间保留左右倾斜；完成蓝绿后长板回平并保持，只有 Spider 按黄色按钮才开始左低右高的三球受压阶段。
- 黄色按钮从天花板内向下伸到原位置。
- 紫色按钮从地面内向上升到原位置。
- `Purple Button Extending Floor` 从紫色按钮下方朝右伸展，跨过原右侧缺口并接到右墙附近；顶面与中央地面相齐。
- 按钮在出现动画结束前不可触发。按钮绘制层级在动画期间位于原地面／天花板之后，使埋入部分被原环境遮住。
- 蓝绿完成时若玩家仍在控制房间 Fan，先原地退回 Star 并回收 Fan；已是其他形态则保持。随后清空输入并暂停控制，将保留下来的身体固定为 Kinematic／FreezeAll。动画结束后恢复身体类型、约束和操作，不缓冲动画期间按下的移动／跳跃／换形／开风输入。
- 两侧挡板在同一段动画内逐渐出现裂痕，随后散成淡出的碎片。`SeesawGateShatter` 使用房间联动进度驱动，碎片没有碰撞体；恢复已完成的检查点时直接隐藏，不重复创建碎片。
- 动画结束后两侧挡板的碰撞同时关闭，按钮触发器开启。黄色仍需附身指定 Spider、贴天花板接触才触发，沿用原黄色齿轮机关事件。紫色按钮等待下述四次撞击断板联动。

蓝绿完成时立即停用 Fan 生成点和补给，并回收房间 Fan（包括退回 Star 后的原受控 Fan）。同时清理本补给生成的已释放／晃动中的小球，以及房间内手动摆放的普通游荡球；已离开房间的本补给小球也会回收，其他房间的小球及可附身形态保留。生成器中只保留一颗尚未释放的悬空待机球，若没有则立即补一颗，供黄色机关复用。清理只执行一次，后续黄色三球批次不会被再次清理；死亡恢复蓝绿完成进度时仍保持无 Fan、带待机球的状态。

## 黄色机关：三球左吹与 Spider 通道

- 黄色按钮仍必须由指定 Spider 贴天花板触发；黄色齿轮启动后，`SeesawOverloadSequence` 接续联动，蓝、绿、黄三处齿轮及板上显示向左的风线。
- 先复用悬停球，再补两个球，共释放三个；风先出现约 0.6 秒，小球间隔约 0.75 秒依次落下。该阶段停用普通球的无限补给，风只驱动这三个普通球，不锁玩家或推动 Spider。
- 小球保留实际重力和板面碰撞。三个不同的球都接触长板后，长板以左低右高姿态进一步受压，并逐渐出现贯穿板厚的裂纹；板面仍能承托，不整块坍塌。
- 同时，原 Spider 出生点附近（世界 X=34）的地面显示金色裂痕。仅由存活受控的 Spider 靠近后，入口和左出口盖板播放约 0.7 秒碎裂，无碰撞碎片淡出，然后关闭盖板实体碰撞。
- 地台原外侧边界与两个悬崖不动。地台内局部拆为支撑块和两个盖板，下面接短通道；右入口 X=32.9..35.1，左出口 X=24.5..28.4。底部走面 Y=-26.5；左出口斜坡接回原地台，使 Spider 向左爬回现有 BouncyBall（仍在 X=24.42、Y=-22.5）区域。

## 神秘通道与一次性 Spider

- `SeesawUnderpassCover` 的上下两层开局均为不透明黑色，连成完整地台，遮住地下通道的空白与结构；从 Awake 起应用遮罩颜色，下层不再使用镜头背景色。入口打开后，只有存活、受控的 Spider 实际进入地下范围才在约 0.45 秒内淡出；Star／Fan 入地、单纯换成 Spider、按黄色按钮或打开入口均不会提前揭开。遮罩没有碰撞，死亡重载后恢复全黑。
- 只有这只 Spider 的 `ReturnToSpawnOnLeave.consumeOnLeave` 开启；同一次附身可完成黄色按钮和下方通道，Q 离开或附身 BouncyBall 后消耗，不再回位或补生。死亡重载后恢复一只。原本挂在其下、属于后续区域的形态在 Awake 时保留世界姿态独立挂回父级，避免一起移动或消耗。
- 左出口世界 X=25.1 设 BouncyBall 专用实体挡板，完整跨过通道和房间可跳跃高度。只与本机关的 BouncyBall 碰撞；Star、Spider、Fan 和普通球放行。Spider 的贴面查询也不会把它当成墙，BouncyBall 不能回到地下通道。

## BouncyBall 四次撞击与最后一个齿轮

- `SeesawBouncyFinale` 只记录这只球受控、通道已打开、位于左侧机关范围时实际撞到板底的事件。落地起跳或撞到其他天花板不计数；其余关卡的跳跃参数与节奏不变。
- `requiredImpacts` 设为 4。每次撞板将染色推进 1/4，逐步过渡到 RGB=(154,120,220)。中间锯齿裂纹比旧版更宽，并随每次撞击继续增宽；前三次长板保持完整。
- 第四次真实撞板时，长板完全染成紫色，中心留下约 0.35 世界单位的断口（`fractureGap` 可调）。左半板立即脱离，在约 1.1 秒内移向左侧悬崖并转平，落定后启用实体碰撞，作为保留整段长度的安全平台跨过左侧缺口，并与两端地台重叠衔接。
- 原长板碰撞体缩到右半段，左侧不残留隐形支撑；右半板仍由原中心支点驱动，平滑右下倾约 14 度。断裂后隐藏中间裂纹线。
- 第四次撞击才启用右侧紫色接触区。右半板下压、实际碰到原紫色按钮后，通过 `SeesawColorContact` 事件启动最后的紫色齿轮，并隐藏按钮／接触区。

## 四齿轮完成与 Smile Face 电梯

- `SeesawRoomExit` 逐一检查蓝、绿、黄、紫四个齿轮，全部运行才开始一次性完成动画。通过 `level2Camera` 加入约 2 秒、逐渐减弱的全房间镜头震动，房间实体位置保持稳定。震动期间中央平台保持隐藏，震动完全结束后才显示并开始升起。
- 震动后，从中央支点上方升起宽 2.8、高 0.35 的紫色小平台（约 0.9 秒），显示向上箭头与 `SMILE FACE` 目的地标注。平台升起且左侧断板落定后，才开启平台碰撞与乘坐，并关闭 BouncyBall 回程挡板，允许玩家到达中央。
- 玩家落到平台顶面后，约 0.35 秒内站到中央，平台再用约 7 秒缓缓上升，起步和停靠均平滑减速。玩家随平台连续移动，镜头平滑收近后全程跟随；不再淡出或瞬移。乘坐时临时暂停操作和自动弹跳，暂停游戏会同步暂停电梯。
- 若登台时是 BouncyBall，平台升出 Seesaw 天花板后，球壳先用约 0.3 秒显出锯齿裂纹，再向左右裂开、下落淡出，总计约 1.1 秒。裂开时实际交回原始 Star 身体并回收旧球体，镜头转而跟随 Star，电梯继续上升，以 Star 进入 Smile Face；其他登台形态保持原样。球壳仅为无碰撞视觉碎片，结束或中断乘坐时清理。
- 电梯在开动时打开 Seesaw 天花板中央的通道，两侧天花板保留实体碰撞。最终停靠 Smile Face 左下方阶梯起点下的现有底座（`wall (33)`，世界 X=28.03、底座顶面约 Y=-0.30），平台顶面与底座对齐。停稳后清空输入、速度并恢复控制和身体物理。
- 抵达时保存 `SMILE_FACE_ENTRY` 检查点。之后死亡在左下方底座恢复 Star 和跟随镜头，无需重玩跷跷板房间；重新开始关卡仍按原规则清除检查点。乘坐前死亡仍沿用原跷跷板检查点重置规则。

## 检查点

保留 Level 1 样式：蓝色星形轮廓，进入后变金色并短暂显示 `CHECKPOINT SAVED`。

- 蓝绿未全部完成时死亡：恢复初始按钮、关闭的两侧挡板 和隐藏的黄紫按钮／平台；镜头仍为固定全景。
- 蓝绿全部完成后死亡：蓝绿按钮保持已完成、齿轮继续转，两侧挡板开放，黄紫按钮与延伸平台直接恢复到最终展开位置，不重复动画或锁定输入，Fan 不再生成。
- 黄色三球风、板面裂纹、通道开启、四次撞击染色断裂与最后齿轮不在本阶段存档范围；死亡后重置，恢复遮罩和一只 Spider、长板回平，需重新触发黄色按钮。重新开始关卡清除检查点和阶段完成记录。

## 既有规则

长板维持约 23.71 宽；两端传感器、地面缺口与下方清理区沿用上一版的对齐结果。普通球掉入下方清理区后消失，玩家受控形态掉入则死亡。Star 上下坡跳跃修复保留。

## 房间边缘整理

- 地台下方左右支撑和地下走面的底边统一至世界 Y=-27.15，与黑色遮罩底边齐平；地下走面顶面仍为 Y=-26.5，左右支撑的上沿和面向通道的边界保留。地下走面两端略伸入墙体，消除底角细缝。
- 天花板两端对齐左右外墙，保留原来的内侧高度；两处墙脚与墙体统一宽度、颜色，减少黑色凸角和断开的边缘。
- `SeesawRampGroundVisual` 为出口斜坡下方补上连续实心地形轮廓，消除斜坡与地台之间的小三角空隙。该补面只绘制图像，不增加碰撞；原斜坡表面及 Spider 路线保留。

## 验证

已通过隔离 Unity Play Mode 验证：左右实际风吹球触发蓝绿机关、固定镜头、初始隐藏与触发关闭、动画中间位置与平台中间宽度、动画期间移动／跳跃／换形／开风无效、结束后操作恢复、Star 经平台取得 Spider、Spider 沿右墙和天花板接触黄色按钮并启动黄色齿轮，以及实际死亡后直接恢复展开状态。日志：`Logs/SeesawRevealChecks.log`。

暂停时不启动联动；蓝绿完成与死亡出现在同一步时，仅记录完成进度，保留正常死亡流程，复活直接恢复最终状态。对应实测日志：`Logs/SeesawRevealDeathChecks.log`。

两侧挡板已通过隔离 Unity Play Mode 和截图验证：Star、Fan 分别从中间向左右移动均被实际碰撞阻挡；两侧真实风吹小球仍能触发蓝绿机关；裂纹和无碰撞碎片随现有两秒动画播放，结束后两个碰撞体关闭；Star／Spider 后续路线及实际死亡重生恢复开放状态通过。日志：`Logs/PairedGateChecks.log`，截图：`Logs/PairedGatesInitial.png`、`Logs/PairedGatesCracks.png`、`Logs/PairedGatesShatter.png`。

新增流程已通过隔离 Unity Play Mode 的完整路线验证：实际风吹球完成蓝绿按钮，Fan 停止补给、Q 退出后清理而悬停球保留；Spider 爬天花板按黄色按钮，复用悬停球并依次释放三个球，真实板面碰撞触发左低右高的受压裂纹；Spider 返回地面破开入口，进入下方通道并沿斜坡抵达、附身原位置的 BouncyBall。实际死亡重生后蓝绿进度与无 Fan 状态保留，黄色机关、板面裂纹和通道恢复初始等待状态。日志：`Logs/SeesawOverloadChecks.log`；截图：`Logs/SeesawOverloadCracked.png`、`Logs/SeesawOverloadPassage.png`。

本轮已通过隔离 Unity 完整路线验证与截图检查：蓝绿后回平、黄色后才左低右高、仅实际进入地下后揭开遮罩、Spider 换形及 Q 离开两种消耗路径、后续区域 Spider 保留、真实自动弹跳逐次计数至三、完全染色与板底真实撞击、右侧接触块实际压中紫色按钮并启动最后齿轮，以及持续向右移动／弹跳的 BouncyBall 无法回到通道。实际死亡重生重置遮罩、Spider、跳跃计数、板色和最后机关，保留蓝绿完成状态。日志：`Logs/SeesawFinaleChecks.log`；截图：`Logs/SeesawFinalePassage.png`、`Logs/SeesawFinaleComplete.png`。

四次撞击断板版已通过隔离 Unity Play Mode 完整流程和截图验证：前三次真实撞板只推进染色、增宽裂纹，第四次左半板脱离下落、左侧支撑碰撞移除，右半板实际压中紫色按钮并启动最后齿轮；BouncyBall 返回通道的挡板继续有效。死亡重生清除断板碎片、恢复完整板面和零次计数，保留蓝绿进度。日志：`Logs/SeesawFourImpactsChecks.log`；截图：`Logs/SeesawFourImpactsCrack.png`、`Logs/SeesawFourImpactsBreaking.png`、`Logs/SeesawFourImpactsComplete.png`。

开局屏障加宽至 0.6 后已通过隔离 Unity Play Mode 验证：图像与碰撞宽度一致，Star／Fan 在左右两侧均被挡住，实际风吹小球仍可穿过两侧触发机关；裂纹、碎片及解锁动画正常。日志：`Logs/WiderGatesChecks.log`；预览：`Logs/WiderGatesInitial.png`。

屏障与生成器提示版已通过隔离 Unity Play Mode 完整流程验证：Star 实际碰撞闪光、两侧共用首次提示；虚线生成点显示与停用清理；两轮晃动期间保持悬空、暂停冻结、短暂开风指令锁存、结束后真实重力落球；左右风吹球、屏障碎裂解锁、黄色三球通道、四次撞板断裂及死亡重置均通过。日志：`Logs/SeesawCuesChecks.log`；预览：`Logs/SeesawCuesInitial.png`、`Logs/SeesawBarrierFirstHit.png`。

左侧断板平台与 Smile Face 传送版已通过隔离 Unity 完整路线验证：左板转平后实体覆盖悬崖缺口，Star 实际落地站稳并走回地台；前三个齿轮不触发出口，四个齿轮齐全后实际观察到镜头震动、平台升起中间态、回程挡板解除；BouncyBall 实际移动到中央平台后淡出传送到 Smile Face 左侧入口，镜头与控制恢复，死亡后在新入口检查点重生。日志：`Logs/SeesawExitChecks.log`；截图：`Logs/SeesawBridgeAndPortal.png`、`Logs/SmileFaceArrivalOverview.png`。

Fan 碰撞反馈与缓升电梯版已通过隔离 Unity 完整路线验证：首次以 Fan 碰撞两侧能量墙时实际阻挡、闪光与一次性提示正常，普通球继续放行；玩家实际到达电梯后，完整观察到约 7 秒的连续上升、玩家与平台位置保持一致、镜头跟随、无淡出瞬移、暂停／恢复正常。最终停靠 Smile Face 左下方阶梯底座，死亡后恢复到该新落点。日志：`Logs/SeesawElevatorChecks.log`；截图：`Logs/FanBarrierFirstHit.png`、`Logs/ElevatorMidRide.png`、`Logs/SmileLowerLeftDock.png`。

蓝锁绿钥匙按钮版已通过隔离 Unity 编译、全景／近景截图与真实风吹球触发验证：四处按钮／接触块均有清晰图案，蓝色完成后仅隐藏蓝色一对，绿色随后仍可正常触发；两色均完成后图案随一次性按钮清理，原齿轮和房间解锁动画正常。日志：`Logs/SeesawButtonSymbolChecks.log`；截图：`Logs/SeesawButtonSymbols.png`、`Logs/ButtonSymbolsCloseup.png`。

按位置区分图案的修正版已通过隔离 Unity 编译及截图检查：上下固定按钮均为锁，板上两处接触块均为钥匙，黄色按钮展开后显示蜘蛛轮廓；绘制层级随黄色按钮从遮挡层恢复到前景层，标志不会被按钮遮住。实际风吹球仍正常触发蓝绿机关与黄色按钮展开，完成的图案随原按钮隐藏。日志：`Logs/SeesawButtonRoleChecks.log`；截图：`Logs/ButtonRolesCloseup.png`、`Logs/YellowSpiderButtonCloseup.png`。

黄色标志现改为与当前 Spider 占位形态一致的黄色六边形，带深色描边。已通过隔离 Unity 显示及原按钮流程验证；预览：`Logs/YellowHexagonButtonCloseup.png`，日志：`Logs/SeesawYellowHexagonChecks.log`。

蓝绿完成自动回收版已通过隔离 Unity 验证：受控 Fan 原地退回 Star，房间 Fan 立即清理，展开期间冻结并恢复的是 Star；玩家已是 Star 时不换形。刻意保留的已释放球、晃动中的球、室内手动普通球均被清理，其他房间球保留，现有待机球不被替换；没有待机球时立即补一颗，死亡恢复后仍有待机球且不生成 Fan。后续 Spider 黄色三球、通道、四次撞板和缓升电梯完整流程通过，重复停止补给不会误删黄色三球。日志：`Logs/SeesawCompletionCleanupChecks.log`、`Logs/SeesawCompletionStarChecks.log`、`Logs/SeesawCompletionNoWaitingChecks.log`。

全黑地下遮罩版已通过隔离 Unity 完整入地路线与截图验证：开局上下遮罩均为不透明黑色，切换镜头背景色不会露出灰色空洞；只开黄色入口不会揭示，受控 Star 入地及暂停中的 Spider 不会揭示。实际 Spider 沿通道进入后遮罩淡出，死亡重生恢复全黑，蓝绿完成进度继续保留。日志：`Logs/SeesawBlackCoverChecks.log`；截图：`Logs/SeesawBlackCoverInitial.png`、`Logs/SeesawBlackCoverRevealed.png`。

边缘整理版已通过隔离 Unity 完整路线与截图检查：地台／遮罩底边齐平、墙角对齐、斜坡下方补面不新增碰撞，接缝没有露出的彩色三角细边；实际风吹球、Fan 回收、Spider 墙面／天花板与地下斜坡、四次撞板、左侧安全平台、2 秒地震后升台、电梯和死亡重生均通过。日志：`Logs/SeesawEdgePolishChecks.log`；截图：`Logs/SeesawPolishedInitial.png`、`Logs/SeesawPolishedUnderpass.png`、`Logs/SeesawPolishedFinale.png`。

齿轮区永久能量墙已通过隔离 Unity 实测：Spider 沿天花板持续爬向墙面会被挡住，不能穿到齿轮区，也不会将能量墙作为贴面；墙体上下实体封住可达的绕行端点，靠近时显示独立的禁止攀爬提示。新生普通小球正常放行，蓝绿解锁后墙保留，后续黄色通道、四次撞板、左侧平台、电梯及死亡重生均通过。日志：`Logs/SeesawGearWallChecks.log`；预览：`Logs/SeesawGearWallInitial.png`、`Logs/SeesawGearWallSpiderBlocked.png`。

齿轮墙透明碰撞显现版已通过隔离 Unity 实测及截图检查：开局墙面、描边、能量纹均不可见；Star 实际撞墙后显示墙面与提示，约 0.9 秒后恢复完全透明；Spider 沿天花板实际撞墙同样闪现，并持续被挡在齿轮区外。普通球放行、后续地下通道、断板平台、电梯及重生仍通过。日志：`Logs/SeesawHiddenGearWallChecks.log`；截图：`Logs/GearWallHiddenIdle.png`、`Logs/GearWallStarFlash.png`、`Logs/GearWallSpiderFlash.png`。

齿轮墙无文字蓝色版已通过隔离 Unity 实测和截图检查：Star／Spider 碰撞时显示半透明蓝色墙面，完全没有文字对象；墙面与能量纹随后恢复透明，阻挡／禁止贴面及普通球放行保持。日志：`Logs/SeesawBlueGearWallChecks.log`；预览：`Logs/BlueGearWallStarFlash.png`、`Logs/BlueGearWallSpiderFlash.png`。

电梯球壳离场版已通过隔离 Unity 完整路线验证及截图检查：真实四次撞板、四齿轮解锁后以 BouncyBall 登台，出房间时依次出现裂纹／两半分离，实际归还原始 Star 控制权并停用旧球体；分裂中暂停会冻结碎片和电梯，恢复后继续上升，Star 在 Smile Face 左下入口停靠并正常重生。日志：`Logs/SeesawBallShellExitChecks.log`；预览：`Logs/ElevatorBallCrack.png`、`Logs/ElevatorBallSplit.png`。

## Smile Face 与 Level2addon 的机关连接

`NewLevel02` 的 Smile Face 区域沿用 `Level2addon` 的三牙齿机关规则：每次受控 BouncyBall 从下方撞击抬升 0.5，最高抬升 4；三个对应的 `TeethArrivalTriggerZone` 全部记录到位后，`TeethTriggeredDoor` 才停用右侧 `Door (1)` 及其碰撞。开门组件放在 `Smile face part` 根对象上，三个到位事件均连接该组件。迁移时丢失的 9 个 `SpikeHazard.playerController` 引用已统一接回本场景 `PlayerControl`，碰刺执行正常死亡流程。

隔离 Unity 已验证：三颗牙齿分别经过 8 次真实受控小球撞击升到 4 单位，并实际进入对应触发区；第一／第二颗到位门仍关闭，第三颗到位后门及其碰撞退出场景。BouncyBall 和 Star 分别实际碰刺死亡，均在 Smile Face 入口重生，牙齿机关与门恢复初始状态。日志：`Logs/SmileFaceWiringChecks.log`。

## Smile Face 完成动画与无文字存档点

三颗牙齿全部到位、右门开启后，`SmileFaceCompletion` 开始约 2.6 秒的表情转换：三个活动牙齿的图像逐渐融入完整的笑容。嘴部只连接原图四颗灰色牙齿的包络范围，保留周围原有的厚黑嘴线，并直接采样原牙齿的浅灰纹理；不再向黑色嘴部边缘扩大填充。眼睛保持圆形、黑色瞳孔和双高光，比第一版小圆眼再放大约 15%。完成机关后先保持闭眼片刻，再在约 0.95～2.45 秒期间逐渐抬起眼皮，固定圆形的瞳孔由眼皮遮罩渐渐露出，之后平滑注视当前受控身体。原始图片保留，通过 `SmileFaceAwakening` 材质覆盖眼部、独立无碰撞网格连接嘴部；活动牙齿的实体支撑保持。

Smile 镜头分三段：未完成牙齿时，受控 BouncyBall 进入嘴部房间后用约 1 秒切入固定构图，覆盖水边小球、三颗牙齿和右门，不跟着小球左右移动。三颗牙齿实际全部到位后，用约 1.8 秒拉远展示整个小孩儿，配合 2.6 秒的补全嘴巴／睁眼动画，再停留约 1.4 秒后恢复跟随；恢复已完成的存档不重播。脑袋／额头阶段仍按当前位置选择全景，Bubble 退出该区域用约 0.8 秒恢复跟随，重新进入可再展示；进入脑袋会结束尚未播完的牙齿展示计时，避免退回时重新锁定。额头 checkpoint 重生时直接恢复全景。所有构图适配宽高比，暂停冻结镜头、嘴部转换和眼睛追踪。

Smile Face 入口虚拟存档点与 `CP03 / Check_Mickey` 不显示保存文字或头顶标记；玩家在该房间范围内时，统一隐藏 checkpoint HUD 文本，离开后恢复其他区域的正常提示。触发范围、存档位置和原本的重生事件继续保留。

隔离 Unity 已通过真实三牙齿撞击后的动画验证：过渡帧／最终连续嘴部、睁眼、瞳孔左右追踪、全房间构图、暂停恢复、离场恢复跟随及文字提示均通过；`CP03` 实际触发仍正常存档，不打断全景且没有文字。BouncyBall／Star 碰刺后正常重生，表情与镜头回到初始状态。日志：`Logs/SmileFaceAwakeningChecks.log`；画面：`Logs/SmileFaceTransition.png`、`Logs/SmileFaceAwake.png`、`Logs/SmileFaceLookLeft.png`、`Logs/SmileFaceLookRight.png`。

自然表情调整已通过隔离 Unity 渲染与原流程验证，嘴部边缘、原牙齿灰色纹理和小圆眼已检查；动画、瞳孔追踪、暂停、无文字存档点、离场镜头与死亡重生继续通过。日志：`Logs/SmileFaceNaturalStyleChecks.log`；预览：`Logs/SmileFaceAwake.png`。

保留黑嘴线与逐步睁眼版已通过隔离 Unity 验证，并检查最终及半睁眼画面：原黑色嘴部边缘保持，灰色连接限于牙齿范围；逐步睁眼、追踪、暂停、checkpoint、离场和死亡重生均通过。日志：`Logs/SmileFaceOriginalOutlineChecks.log`；预览：`Logs/SmileFaceAwake.png`、`Logs/SmileFaceEyesOpening.png`。

## Smile Face 左侧泡泡水道

左侧泡泡房间右边增加 L 形水道：竖直水区世界范围 X=31.952～35.3、Y=5.81～15.5；右侧较低水池 X=35.3～39.2、Y=5.81～7.1。水体位于笑脸原图与地形后方，原有墙体遮住相交部分，并用浅色细线标明水面。泡泡从左侧房间右上的现有开口进入，随后可在水中上下左右游动，沿低处水池接近 BouncyBall。

下方 `BouncyBall (1)` 保持原来的 X，调整到世界 Y=7.58，刚好在右侧水面上；受控后沿用原有浮水逻辑，停在水面、不持续弹跳。水面低于第一颗牙齿底部，玩家能向右移出水池，然后恢复陆地弹跳和顶牙齿流程。

隔离 Unity 已实测完整水路：原泡泡不瞬移地从原位置右移入水，向下游过竖直通道和底部水池，以真实接触接管原 BouncyBall；小球停在水面且竖直速度为零，向右离水后实际弹起第一颗牙齿。日志：`Logs/SmileWaterwayChecks.log`；预览：`Logs/SmileWaterwayInitial.png`、`Logs/SmileBubbleEntersWater.png`、`Logs/SmileBouncyOnWater.png`、`Logs/SmileWaterwayFirstTooth.png`。

## Smile Face 右侧攀爬、额头陷阱与升旗终点

- `Smile Final Route` 管理四个末段 checkpoint：门后接触 Spider 的 `SMILE_SPIDER_BASE`、右耳攀爬口的 `SMILE_RIGHT_HEAD`、右侧 Bubble 入口的 `SMILE_FOREHEAD_ENTRY`、左手 Box 的 `SMILE_HAND_BOX`。均只有纯色星星标记，没有文字。前两个重生为 Spider；泡泡侧存档后在安全起点 `(53.4,27.25)` 以 Star 重生；实际取得终点 Box 后保存箱子检查点，之后死亡直接在箱子原始位置以完整大小的 Box 重生，继续缩小通道。所有存档都保留已完成牙齿、开门和笑脸状态，并防止被更早的存档覆盖。右耳存档记录实际攀爬位置，额头与箱子存档使用安全落点。
- 右侧 `Square (6)` 收窄形成上行开口。额头入口原来的上方 BouncyBall 停用，换成可补充的 Bubble；Spider 经过后切入泡泡，破泡回到 Star。泡泡破裂沿用 E／Pop／Release 的现有操作。
- 额头放置 3 个错峰跳跃的尖刺，约 3.1 秒一轮，先用约 0.45 秒变色预警，再起跳约 0.85 秒。陷阱之间保留落脚处，玩家可观察落地节奏后跳过。
- Box 位于当前场景左手原有黑色落脚线，世界位置约 `(32.31,27.38)`。其左侧出口上方新增三颗朝下的静态尖刺，保留约 0.7–0.8 的净空：正常大小的 Box 会碰刺，按 J 缩小到一半后可从下方通过。然后沿原本左侧轮廓自然下落到屋顶按钮，全程保留操作与刚体物理；没有新增平台或搬运。`Square (7)`／`Square (9)`／`Square (11)` 仍只保留原画面，不用额外碰撞挡住路线。
- 屋顶按钮位于约 `(25.1,11.58)`，仅实际的终点 Box 落地压住才能触发。按钮下压、变绿后，约 `(21.8,11.47)` 的旗子用约 1.25 秒升起；升旗状态在死亡后保留，清除存档重开时重置。
- 按钮和旗子是两个步骤：只有受控身体实际碰到已经升起的旗子，才显示 `Congratulations,` / `you are now a super star`，并清空输入、固定获胜身体。尚未升起的旗子不能提前触发胜利。

同时修复 `HexagonSurfaceMovement` 的表面查询：只接受实体 Collider，忽略 checkpoint、水区和危险触发框，避免 Spider 攀附触发区导致绕路。

隔离 Unity 的自然下落版完整路线已通过：底部和右侧 checkpoint 分别真实死亡重生、Spider 实际攀爬入泡泡、破泡回 Star、按节奏跳过三组尖刺、走到左手附身 Box、沿原地形自然下落并落地压按钮、升旗后死亡保留进度、实际碰旗显示准确祝词。箱子下行时控制器和刚体模拟保持开启，没有乘台锁定或搬运。日志：`Logs/SmileNaturalBoxRouteChecks.log`；预览：`Logs/NaturalSmileHandBox.png`、`Logs/NaturalSmileFlagRising.png`、`Logs/NaturalSmileCongratulations.png`。实体表面查询修复还通过原 Seesaw 全流程、电梯换回 Star 和 Smile 入口重生回归，日志：`Logs/SeesawFinalRouteRegression.log`。

泡泡侧存档与分阶段镜头版已通过隔离 Unity 完整路线验证：实际进入新触发区自动存档，Star 实际碰跳刺死亡后在安全起点重生，保留牙齿和完整小孩儿全景；进入额头全景能容纳全部关卡边界，切入左手 Box 后恢复大小为 6 的跟随构图，并继续自然下落压按钮、升旗和触旗通关。日志：`Logs/SmileForeheadCheckpointCameraChecks.log`；镜头预览：`Logs/ForeheadCameraForeheadOverviewView.png`、`Logs/ForeheadCameraBoxFollowView.png`。

入口全景漏触发修复已通过隔离 Unity 定向验证：牙齿未完成、眼睛仍闭着时，Star 位于右侧接近区即可展示完整小孩儿；实际 Bubble 进入 checkpoint 触发区也能独立请求全景，破泡切回 Star 后保持。镜头切换不自动完成牙齿机关，后续切入左手 Box 仍恢复跟随。日志：`Logs/SmileCameraEntryFixChecks.log`；预览：`Logs/SmileUnsolvedForeheadOverview.png`。

## Smile 房间紧凑化

- 移除原来底部需要 BouncyBall 跳刺接触 Spider 的房间、底板及三组悬挂刺。主地板向右连续延伸，右侧颈部下端缩短形成水平通道，上方原有攀爬轮廓保留。
- 三颗牙齿仍负责开门。门后保留可见的 Spider，玩家实际接触它后按普通附身流程换形。`SmileDoorSpiderGate` 只监听成功换形事件，收回原 BouncyBall，保存门后 `(58.5,6.4)` 的 Spider 检查点，并用约 0.8 秒从地下升起返回墙。墙的实体阻挡在换形完成时立即生效，顶端连接原有嘴部边界，封住回牙齿房间的路线。死亡恢复末段检查点时墙保持升起，完整重开后恢复开放。原 `CP03 / Check_Mickey` 已移除。
- 左下入口地板上移 3.5，室内高度由约 11.1 缩到 7.6；删除底下两级，剩下四级原有台阶重新排列，并使用单向平台碰撞，避免缩短后跳跃撞到台阶底面。入口原有尖刺、泡泡和通向水区的上方出口保留。Seesaw 电梯继续读取入口地板碰撞体计算停靠高度。

隔离 Unity 实测已通过：Star 从抬高后的入口真实跳过台阶接触泡泡、游入原水道接管水面 BouncyBall、未解锁的门保持阻挡、开门后原地换 Spider 并清理旧球、门后／右侧／额头检查点真实死亡重生，以及后续攀爬、额头镜头切换、三组跳刺、自然落箱、按钮升旗和触旗通关。日志：`Logs/SmileCompactRoomChecks.log`；全景：`Logs/CompactSmileCompactInitial.png`、`Logs/CompactSmileCompactDoorSpider.png`。

Seesaw 全流程回归也已通过，包括四次撞击、四齿轮后地震、电梯连续上升／暂停恢复、出房间裂球回 Star、停靠抬高后的左下入口及真实死亡后在该落点重生。日志：`Logs/SeesawCompactEntryChecks.log`。

接触 Spider 与返回墙版本已通过隔离 Unity 实测：小球穿门不换形，移到可见 Spider 上方落下后正常附身，收回旧球并保存检查点；观察到返回墙升起中间态，暂停／恢复正常，Spider 实际向左移动被挡住。真实死亡后门后 Spider 与关闭的返回墙正确恢复，后续右侧攀爬、额头 checkpoint、镜头、跳刺、箱子按钮与旗子通关全部通过。日志：`Logs/SmileContactSpiderChecks.log`；截图：`Logs/ContactSpiderWallRising.png`、`Logs/ContactSpiderReturnWallClosed.png`。上面的紧凑化初版自动换形测试记录属于旧版本。

转向出球版本已通过隔离 Unity 实测：第一次方向输入无需 F 即开风并晃球，持续按住／松开重按同方向不重复生成，旧球滞留时转向仍补下一颗，晃动期间快速转向不堆球，暂停／恢复正确。方向输入生成的小球实际被风吹到左右两侧触发蓝绿机关，完成后的 Fan 与游荡球清理、黄色三球、四次撞击、地震电梯和 Smile 入口死亡恢复回归全部通过。日志：`Logs/SeesawTurnSupplyChecks.log`。

单球限制版已通过隔离 Unity 实测：唯一待机球正常晃动释放，移动／停止／晃动中的球及手动摆放的普通球都能阻止转向生成第二颗，风向仍正常改变；球停用后即使定时扫描尚未到期也能立即正确识别并生成下一颗，暂停／恢复不重复出球。左右蓝绿机关、完成清理、黄色三球批次、四次撞击及地震电梯／入口重生回归通过。此版本替代上面转向出球初版的“旧球仍在场上也补下一颗”规则。日志：`Logs/SeesawSingleBallChecks.log`。

脑袋镜头区域版已通过隔离 Unity 实测：Bubble 在脑袋内展示完整小孩儿，往右退回区域外后恢复大小为 6 的跟随，区域外残留的 checkpoint 回调不会重新锁定全景；再次进入可重新展示，Bubble 破裂回 Star 时在区域内保持全景，向下或向左离开均恢复跟随。表情动画播放期间区域外仍保持跟随，暂停／恢复正常。此规则替代旧版本“直到切入 Box 才解除”的镜头锁定。日志：`Logs/SmileRegionCameraChecks.log`；截图：`Logs/SmileBubbleInsideBrain.png`、`Logs/SmileBubbleBacktrackFollow.png`。

脑袋入口新增回落保护：仅在 `SMILE_FOREHEAD_ENTRY` 已保存后生效，以右侧旧检查点碰撞框为基准向左右各扩 2、上下各扩 0.6；受控 Star／Bubble／Spider 落入后，通过 `TryReturnToStarCheckpoint` 直接回到已保存的 Star 安全入口，清空动量与输入并立即恢复脑袋全景。无需 R，不记死亡或手动重试，不重载场景、不重置牙齿／门／机关进度。第一次攀爬、暂停时、左侧箱子路线均不触发。隔离 Unity 已验证真实 Star 下落、Bubble 返回、首次攀爬保护、暂停恢复和后续跳刺／箱子／旗子完整通关。日志：`Logs/SmileForeheadRecoveryChecks.log`。

Smile Face 的电梯落点、门后 Spider、右耳攀爬口、额头入口和左手 Box 共五处使用 Level 1 的十点星星轮廓：未保存为纯蓝色 `(47,155,236)`，到达后为纯黄色 `(255,211,77)`。深色外沿已移除，不带碰撞或常驻文字，原三条细线停用。电梯落点由 `SeesawRoomExit` 同步进度，其余由 `SmileRouteCheckpoint.RefreshMarker` 同步，后续存档与死亡恢复保留已到达标记。

隔离 Unity 已通过四处标记的初始显示、蓝金色保存进度、真实死亡恢复、右侧回落保护、镜头及后续箱子／升旗通关验证，并检查全景截图中的描边可读性。日志：`Logs/SmileCheckpointVisualChecks.log`；截图：`Logs/CheckpointStarsAllUnsavedStarCheckpoints.png`、`Logs/CheckpointStarsForeheadOverviewView.png`。

牙齿固定镜头与完整笑脸展示版已通过隔离 Unity 实测：BouncyBall 左右移动及撞牙过程中镜头保持固定，水边小球／三颗牙齿／右门在 16:9、4:3 构图中均可见；24 次真实撞击完成三颗牙齿的到位触发，只有三处到位后才切入完整角色展示，补全嘴巴和睁眼正常，暂停／恢复冻结转场。展示结束自动跟随，之后脑袋进入与 Bubble 退出仍正确切换，恢复已完成表情不会重播。此版本在脑袋区域镜头之外，补回独立且有限时长的牙齿完成展示。日志：`Logs/SmileTeethCameraSequenceChecks.log`；截图：`Logs/SmileTeethRoomFixed.png`、`Logs/SmileTeethCompletedFullCharacter.png`。

脑袋阶段现在使用独立的较近头部构图（世界中心约 `(45.2,21.7)`），覆盖左右耳朵、额头三组跳刺、右侧 Bubble／存档与左手 Box，裁掉大部分下方房间。牙齿完成时仍短暂展示整个角色；进入脑袋、额头存档重生及右侧回落自动返回统一使用 `ForeheadBounds`，退出仍恢复跟随。16:9 下头部视角约为正交大小 13.4，其他宽高比自动适配。

较近脑袋构图已通过隔离 Unity 验证：左右关键落点与 Bubble 同时可见，退出恢复跟随，真实死亡重生和自动回落返回后构图一致；固定牙齿房间和三牙齿到位后的完整笑脸展示仍通过。日志：`Logs/SmileForeheadFramingChecks.log`；截图：`Logs/SmileForeheadCloserFraming.png`、`Logs/SmileForeheadRespawnFraming.png`。

箱子缩小出口版已通过隔离 Unity 实测：实际接触 Box 保存新检查点、纯蓝星星变纯黄；完整大小的 Box 实际碰刺死亡，重生为安全落点的 Box，原检查点不回退且保持跟随镜头。实际缩小到一半后通过三颗顶刺，沿原轮廓下落压按钮；升旗后再死亡仍保留按钮／旗子进度，以 Box 重生、再次缩小走完全程并真实触旗通关。五处检查点均无黑色描边。日志：`Logs/SmileBoxShrinkChecks.log`；截图：`Logs/SmileBoxShrinkUnsaved.png`、`Logs/SmileBoxShrunkClearance.png`。

后续任何更改，均先向用户说明具体细节，获得确认后再执行。
