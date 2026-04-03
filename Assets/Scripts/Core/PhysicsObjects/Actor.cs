namespace Core.PhysicsObjects
{
    public class Actor
    {
        /*
         * 可移动对象（玩家/敌人）
         * 在unity周期开始注册到PhysicsWorld
           - 持有 RectInt collider
           - 持有 Position（int）
           - 维护 xRemainder / yRemainder（子像素）
           - 核心函数：
            → MoveX(float amount, Action onCollide)
                - 余数累计
                - Round → 整数移动
                - 逐像素移动（while）
                - 每步用 PhysicsWorld.Overlap 检测
            → MoveY(...)
            → IsRiding(Solid)
                - 默认：是否站在顶部
         */
    }
}