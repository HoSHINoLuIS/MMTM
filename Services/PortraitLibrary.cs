using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace MementoMoriCodeRedeemer.Wpf.Services;

/// <summary>
/// 日誌旁裝飾立繪的輪換池。
///
/// 池子由兩部分合成：
///   1. 內建立繪 —— Assets\Portraits 下的 192×192 PNG 以 EmbeddedResource 方式隨程式編譯進 exe，
///      下載即用、不依賴任何外部檔案；
///   2. 擴充套件口 —— 資料目錄下的 portraits\ 資料夾（與 players.json 同在
///      %APPDATA%\MementoMoriCodeRedeemer\），使用者把圖片丟進去就會併入輪換池。
///
/// 選擇策略按需求保持簡單：每次隨機抽一張，不保證一輪內不重複。
/// </summary>
public sealed class PortraitLibrary
{
    // 資料目錄下的擴充套件口資料夾名與支援的圖片副檔名。
    private const string UserPortraitFolderName = "portraits";

    private static readonly string[] UserPortraitExtensions =
        [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    private readonly Assembly _assembly = typeof(PortraitLibrary).Assembly;
    private readonly List<string> _builtInNames = [];
    private readonly List<string> _userFiles = [];

    public PortraitLibrary(string? userPortraitDirectory = null)
    {
        var directory = userPortraitDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MementoMoriCodeRedeemer",
                UserPortraitFolderName);

        // 收集內建立繪的資源名，形如
        // MementoMoriCodeRedeemer.Wpf.Assets.Portraits.CHR_000001.png
        _builtInNames.AddRange(_assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Assets.Portraits.", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal));

        // 擴充套件口目錄不存在時先建出來（空目錄不影響使用），方便使用者按名字找到它。
        try
        {
            Directory.CreateDirectory(directory);
            _userFiles.AddRange(Directory.EnumerateFiles(directory)
                .Where(file => UserPortraitExtensions.Contains(
                    Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderBy(file => file, StringComparer.Ordinal));
        }
        catch (Exception)
        {
            // 目錄建不出來（許可權等）時靜默退回到只用內建立繪，不影響主流程。
        }
    }

    /// <summary>當前輪換池裡一共有多少張立繪（內建 + 擴充套件口）。</summary>
    public int Count => _builtInNames.Count + _userFiles.Count;

    /// <summary>
    /// 隨機取一張立繪。內建立繪必然可讀；擴充套件口裡無法解碼的圖片會被剔除出池子後再抽，
    /// 不會因為一張壞圖讓整個功能失效。池子為空（理論上不會）時回傳 null。
    /// </summary>
    public BitmapSource? CreateRandom()
    {
        while (Count > 0)
        {
            var index = Random.Shared.Next(Count);
            if (index < _builtInNames.Count)
            {
                return LoadBuiltIn(_builtInNames[index]);
            }

            var fileIndex = index - _builtInNames.Count;
            if (LoadUserFile(_userFiles[fileIndex]) is { } image)
            {
                return image;
            }

            _userFiles.RemoveAt(fileIndex);
        }

        return null;
    }

    private BitmapSource LoadBuiltIn(string resourceName)
    {
        using var stream = _assembly.GetManifestResourceStream(resourceName)!;
        return Decode(stream, decodePixelWidth: 0);
    }

    private static BitmapSource? LoadUserFile(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            // 使用者圖片可能很大，解碼階段就縮到內建立繪同檔，避免佔用過多記憶體。
            return Decode(stream, decodePixelWidth: 192);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 把流解碼成可以立即脫離流的點陣圖。OnLoad 會在 EndInit 時把整張圖讀進記憶體，
    /// 因此 using 釋放流之後圖片依然可用。
    /// </summary>
    private static BitmapSource Decode(Stream stream, int decodePixelWidth)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        if (decodePixelWidth > 0)
        {
            image.DecodePixelWidth = decodePixelWidth;
        }

        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
