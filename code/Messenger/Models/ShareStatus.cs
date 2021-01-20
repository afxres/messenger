namespace Messenger.Models
{
    /// <summary>
    /// 传输状态
    /// </summary>
    public enum ShareStatus : int
    {
        默认 = 0,

        等待 = 1,

        连接 = 2,

        运行 = 4,

        暂停 = 8,

        中断 = 16 | 终止,

        取消 = 32 | 终止,

        成功 = 64 | 终止,

        失败 = 128 | 终止,

        终止 = 1 << 31,
    }
}
