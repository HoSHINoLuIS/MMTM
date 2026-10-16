using System.IO;
using System.Text.Json;
using MementoMoriCodeRedeemer.Wpf.Models;

namespace MementoMoriCodeRedeemer.Wpf.Services;

/// <summary>
/// 負責讀寫本地玩家 ID 庫。
/// 兌換程式本身可以放在任意目錄執行，因此玩家資料不儲存在 exe 旁邊，
/// 而是寫入當前 Windows 使用者的 AppData，避免更新或移動程式時誤刪資料。
/// 讀取路徑與舊版 WinForms 完全一致，升級到 WPF 版後原有玩家庫可以直接沿用。
/// </summary>
public sealed class PlayerRepository
{
    // 使用 Web 預設 JSON 配置，並啟用縮排，方便使用者或開發者手動檢視 players.json。
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    /// <summary>
    /// 計算玩家庫檔案路徑。
    /// 實際路徑類似：%AppData%\MementoMoriCodeRedeemer\players.json。
    /// </summary>
    public PlayerRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        FilePath = Path.Combine(appData, "MementoMoriCodeRedeemer", "players.json");
    }

    /// <summary>
    /// 玩家庫 JSON 檔案的完整路徑。介面底部會把它顯示出來，方便使用者備份或排查。
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 從磁碟讀取玩家庫。檔案不存在時回傳空列表，這樣首次執行無需提前建立配置檔案。
    /// </summary>
    public IReadOnlyList<PlayerProfile> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<PlayerProfile>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidOperationException($"玩家庫讀取失敗：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 儲存玩家庫到磁碟。
    /// 先寫入臨時檔案，再用覆蓋移動替換正式檔案，降低寫入中斷導致 JSON 半截損壞的機率。
    /// </summary>
    public void Save(IEnumerable<PlayerProfile> players)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(players, JsonOptions);
            var tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, FilePath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidOperationException($"玩家庫儲存失敗：{ex.Message}", ex);
        }
    }
}
