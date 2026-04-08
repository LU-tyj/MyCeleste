using UnityEngine;

namespace Platformer
{
    /// <summary>
    /// 抓墙状态：玩家按住 Grab 键贴墙时进入。
    /// 支持：
    ///   - 静止悬挂（消耗体力）
    ///   - 上爬 / 下爬（垂直轴输入）
    ///   - 体力耗尽后失去控制，下滑阻力逐渐减小
    ///   - 面朝内按跳：原地向上弹跳（消耗额外体力）
    ///   - 面朝外按跳：蹬墙跳（强制解除，附加水平速度）
    /// </summary>
    public class GrabState : BaseState
    {
        public GrabState(PlayerController player, Animator animator) : base(player, animator) { }

        public override void OnEnter()
        {
            animator.CrossFade(WallHash, crossFadeDuration);
            player.ApplyInitialGrabStats();
        }

        public override void FixedUpdate()
        {
            player.HandleGrab();
        }

        public override void OnExit()
        {
            player.ApplyEndOfGrabStats();
        }
    }
}