using System.Text.Json;
using System.Text.Json.Serialization;

namespace MementoMoriCodeRedeemer.Wpf.Models;

/// <summary>
/// 伺服器下拉框使用的資料模型。
/// 與原版一致：下拉框只顯示伺服器名稱，編號是介面內部引數，不對使用者暴露。
/// </summary>
public sealed class ServerOption
{
    /// <summary>官方 API裡的伺服器編號，後續確認和兌換都要傳回這個值。</summary>
    public int Id { get; set; }

    /// <summary>伺服器顯示名稱，例如 Asia、World 等。</summary>
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}

/// <summary>
/// Confirm API 回傳的玩家確認資訊。
/// 這些欄位會出現在日誌和表格「最近結果」中，幫助使用者確認獎勵發到了正確賬號。
/// </summary>
public sealed class RedeemConfirmation
{
    /// <summary>API 回傳的伺服器名稱。</summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>玩家 ID。</summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>玩家所在世界或分割槽。</summary>
    public string World { get; set; } = string.Empty;

    /// <summary>玩家暱稱。</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>本次確認的虛寶碼。</summary>
    public string SerialCode { get; set; } = string.Empty;

    /// <summary>
    /// 保留介面額外欄位。
    /// 這樣即使官方臨時增加欄位，反序列化也不會丟失這些資料，後續排查介面變化時還能檢視。
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

/// <summary>
/// 官方兌換介面異常。
/// 除了錯誤訊息外還保留 HTTP 狀態碼，方便以後根據狀態碼做更精細的處理。
/// </summary>
public sealed class MementoMoriRedeemException : Exception
{
    /// <summary>建立介面異常例項。</summary>
    public MementoMoriRedeemException(string message, System.Net.HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>API 回傳的 HTTP 狀態碼。</summary>
    public System.Net.HttpStatusCode StatusCode { get; }
}
