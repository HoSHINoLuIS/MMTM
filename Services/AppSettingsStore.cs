using System.IO;
using System.Text.Json;

namespace MementoMoriCodeRedeemer.Wpf.Services;

/// <summary>
/// 視窗在「正常」狀態下的尺寸，以及關閉時是否處於最大化。
/// </summary>
public sealed record WindowPlacement(double Width, double Height, bool Maximized);

/// <summary>
/// 負責讀寫程式設定檔案。
/// 存放位置與玩家庫相同（%APPDATA%\MementoMoriCodeRedeemer\settings.json），方便使用者一起備份。
/// 目前只儲存視窗尺寸：使用者調整過視窗大小後，下次啟動會恢復到同樣的縮放大小。
/// 視窗尺寸屬於錦上添花的資訊，因此讀寫失敗一律靜默處理，不彈窗打斷使用。
/// </summary>
public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public AppSettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        FilePath = Path.Combine(appData, "MementoMoriCodeRedeemer", "settings.json");
    }

    /// <summary>設定檔案完整路徑。</summary>
    public string FilePath { get; }

    /// <summary>
    /// 讀取上次儲存的視窗尺寸。
    /// 檔案不存在、內容損壞或數值無效時回傳 null，由呼叫方回退到預設尺寸。
    /// </summary>
    public WindowPlacement? Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var placement = JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(FilePath), JsonOptions);
            return placement is { Width: > 0, Height: > 0 } ? placement : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 儲存視窗尺寸。同樣先寫臨時檔案再替換，避免寫入中斷留下半截 JSON。
    /// </summary>
    public void Save(WindowPlacement placement)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(placement, JsonOptions);
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FilePath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // 儲存失敗隻影響「下次記住尺寸」，不影響本次退出。
        }
    }
}
