using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using MementoMoriCodeRedeemer.Wpf.Models;

namespace MementoMoriCodeRedeemer.Wpf.Services;

/// <summary>
/// 封裝 MementoMori 官方兌換頁面背後的介面呼叫。
/// 介面層只關心「讀取伺服器、確認玩家、提交兌換」這幾個動作，
/// 具體的 URL、請求頭、表單欄位和錯誤解析都集中在這裡維護。
/// </summary>
public sealed class MementoMoriRedeemClient : IDisposable
{
    // 官方兌換介面域名。下面的 Confirm/Register/Servers 都基於這個地址傳送請求。
    private const string BaseUrl = "https://code-input.mememori-boi.com";

    // 官方頁面地址會作為 Referer 傳送，保持請求形態接近瀏覽器訪問。
    private const string RefererUrl = "https://mememori-game.com/tw/code/";

    // 模擬普通桌面瀏覽器的 User-Agent，避免服務端把請求當成未知客戶端直接拒絕。
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36";

    // API 回傳的是 JSON；使用 Web 預設命名規則，同時允許大小寫不敏感，降低API 欄位大小寫變化的影響。
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    // 一個客戶端例項複用同一個 HttpClient，避免每次請求都重新建立連線。
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 初始化介面客戶端，並統一配置所有請求都會用到的公共請求頭。
    /// </summary>
    public MementoMoriRedeemClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(45),
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/javascript"));
        _httpClient.DefaultRequestHeaders.Referrer = new Uri(RefererUrl);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://mememori-game.com");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
    }

    /// <summary>
    /// 讀取官方當前開放的伺服器列表。主視窗啟動後會呼叫它來填充伺服器下拉框。
    /// </summary>
    public async Task<IReadOnlyList<ServerOption>> GetServersAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/SerialCode/Servers", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var servers = await JsonSerializer.DeserializeAsync<List<ServerOption>>(stream, JsonOptions, cancellationToken);
        return servers ?? [];
    }

    /// <summary>
    /// 向官方 API確認「伺服器 + 玩家 ID + 虛寶碼」是否能匹配到玩家。
    /// 這個 API只做確認，不真正發放獎勵；回傳值裡通常包含玩家名、世界等展示資訊。
    /// </summary>
    public Task<RedeemConfirmation> ConfirmAsync(
        int serverId,
        string playerId,
        string serialCode,
        CancellationToken cancellationToken)
    {
        return PostFormAsync<RedeemConfirmation>("/SerialCode/Confirm", serverId, playerId, serialCode, cancellationToken);
    }

    /// <summary>
    /// 正式提交兌換請求。
    /// 呼叫前通常先執行 <see cref="ConfirmAsync"/>，這樣日誌裡能顯示確認到的玩家資訊。
    /// </summary>
    public async Task RegisterAsync(
        int serverId,
        string playerId,
        string serialCode,
        CancellationToken cancellationToken)
    {
        using var response = await SendFormAsync("/SerialCode/Register", serverId, playerId, serialCode, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    /// <summary>
    /// 釋放底層 HttpClient。視窗關閉時呼叫，避免連線和定時器資源殘留。
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    /// 傳送表單 POST 並把成功響應反序列化為指定型別。
    /// </summary>
    private async Task<T> PostFormAsync<T>(
        string path,
        int serverId,
        string playerId,
        string serialCode,
        CancellationToken cancellationToken)
    {
        using var response = await SendFormAsync(path, serverId, playerId, serialCode, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new MementoMoriRedeemException("API 回傳為空。", response.StatusCode);
    }

    /// <summary>
    /// 構造並傳送官方 API要求的表單資料。
    /// 欄位名保持和官方頁面一致：ServerId、PlayerId、SerialCode。
    /// </summary>
    private Task<HttpResponseMessage> SendFormAsync(
        string path,
        int serverId,
        string playerId,
        string serialCode,
        CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["ServerId"] = serverId.ToString(),
            ["PlayerId"] = playerId,
            ["SerialCode"] = serialCode,
        });

        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = content,
        };

        request.Headers.Referrer = new Uri(RefererUrl);
        request.Headers.TryAddWithoutValidation("Origin", "https://mememori-game.com");
        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

        return _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
    }

    /// <summary>
    /// 統一處理非 2xx 響應。
    /// 官方錯誤體有時是多語言 JSON，有時是純文字；這裡先儘量提取可讀訊息，再拋給介面層顯示。
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = ExtractErrorMessage(body);
        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
        }

        throw new MementoMoriRedeemException(message, response.StatusCode);
    }

    /// <summary>
    /// 從介面錯誤響應中提取最適合使用者閱讀的文字。
    /// 優先使用繁中/中文，其次英文、日文、韓文；如果無法解析 JSON，就直接回傳原始文字。
    /// </summary>
    private static string ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            var messages = JsonSerializer.Deserialize<Dictionary<string, string>>(body, JsonOptions);
            if (messages is null || messages.Count == 0)
            {
                return body.Trim();
            }

            foreach (var key in new[] { "zh-cmn-Hant", "zh-Hant", "zh", "en", "ja", "ko" })
            {
                if (messages.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return messages.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? body.Trim();
        }
        catch (JsonException)
        {
            return body.Trim();
        }
    }
}
