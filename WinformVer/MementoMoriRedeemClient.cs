using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MementoMoriCodeRedeemer;

public sealed class MementoMoriRedeemClient : IDisposable
{
    private const string BaseUrl = "https://code-input.mememori-boi.com";
    private const string RefererUrl = "https://mememori-game.com/tw/code/";
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

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

    public async Task<IReadOnlyList<ServerOption>> GetServersAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("/SerialCode/Servers", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var servers = await JsonSerializer.DeserializeAsync<List<ServerOption>>(stream, JsonOptions, cancellationToken);
        return servers ?? [];
    }

    public Task<RedeemConfirmation> ConfirmAsync(
        int serverId,
        string playerId,
        string serialCode,
        CancellationToken cancellationToken)
    {
        return PostFormAsync<RedeemConfirmation>("/SerialCode/Confirm", serverId, playerId, serialCode, cancellationToken);
    }

    public async Task RegisterAsync(
        int serverId,
        string playerId,
        string serialCode,
        CancellationToken cancellationToken)
    {
        using var response = await SendFormAsync("/SerialCode/Register", serverId, playerId, serialCode, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

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
            ?? throw new MementoMoriRedeemException("介面回傳為空。", response.StatusCode);
    }

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

public sealed class ServerOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public override string ToString()
    {
        return Name;
    }
}

public sealed class RedeemConfirmation
{
    public string ServerName { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string World { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string SerialCode { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class MementoMoriRedeemException : Exception
{
    public MementoMoriRedeemException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
