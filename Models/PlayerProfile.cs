namespace MementoMoriCodeRedeemer.Wpf.Models;

/// <summary>
/// 持久化用的玩家資料。
/// 這裡只儲存需要跨程式啟動保留的資料；執行中的選擇狀態和領取結果由介面行模型維護。
/// </summary>
public sealed class PlayerProfile
{
    /// <summary>是否鎖定該玩家。鎖定後介面不允許修改或刪除，避免誤操作。</summary>
    public bool Locked { get; set; }

    /// <summary>玩家 ID，官方 API要求數字字串。</summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>使用者自定義備註，比如賬號名、區服說明或用途。</summary>
    public string Note { get; set; } = string.Empty;
}
