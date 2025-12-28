using System.Net;
using System.Text.Json;
using TONX.Modules;

namespace TONX;

// 来源：https://github.com/Slok7565/FinalSuspect/blob/FinalSus/FinalSuspect/Helpers/RemoteHelper.cs
public static class RemoteHelper
{
    public static async Task<(string, bool)> GetRemoteStringAsync(string url, bool json = true,
        bool removeLineBreaks = true)
    {
        string result;
        bool isValid;

        if (url.StartsWith("https://tonx.leever.cn/api") && !url.EndsWith("/api/stats/visitor"))
            url += $"?token={ApiTokenProvider.BuildTokenAsync().Result}";

        if (url.StartsWith("file:///"))
        {
            result = await File.ReadAllTextAsync(url[8..]);
        }
        else
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
#if Windows
                | SecurityProtocolType.Tls13
#endif
            ;

            var handler = CreateOptimizedHttpClientHandler();

            using HttpClient client = new(handler);

            client.Timeout = TimeSpan.FromSeconds(45);

            ConfigureHttpClientHeaders(client);

            try
            {
                using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Failed [{url}]: {response.StatusCode}", "Get Json Failed");
                    return ("", false);
                }

                result = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"HTTP请求失败: {ex.Message}", "Get Remote");
                if (ex.InnerException != null)
                {
                    Logger.Error($"内部异常: {ex.InnerException.Message}", "Get Remote");
                }

                return ("", false);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Logger.Error($"请求超时: {url}", "Get Remote");
                return ("", false);
            }
            catch (Exception ex)
            {
                Logger.Error($"请求异常: {ex.Message}", "Get Remote");
                return ("", false);
            }

            if (removeLineBreaks)
                result = result.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
        }

        if (string.IsNullOrWhiteSpace(result)) return (result, false);
        var hasInvalidChars = HasInvalidControlCharacters(result);

        if (json)
        {
            isValid = !hasInvalidChars && IsValidJson(result);
        }
        else
        {
            isValid = !hasInvalidChars;
        }

        return (result, isValid);
    }

    private static HttpClientHandler CreateOptimizedHttpClientHandler()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = false,
            CheckCertificateRevocationList = false // Android 上关闭证书吊销检查
        };

        // Android SSL 配置
        ConfigureAndroidSSL(handler);

        return handler;
    }

    private static void ConfigureAndroidSSL(HttpClientHandler handler)
    {
        handler.ServerCertificateCustomValidationCallback = (_, cert, _, sslPolicyErrors) =>
        {
            Logger.Info($"SSL 验证结果: {sslPolicyErrors}", "Get Remote");

            if (cert == null) return true;
            Logger.Info($"证书主题: {cert.Subject}", "Get Remote");
            Logger.Info($"证书颁发者: {cert.Issuer}", "Get Remote");
            return true;
        };
    }

    private static void ConfigureHttpClientHeaders(HttpClient client)
    {
        // 清除默认头
        client.DefaultRequestHeaders.Clear();

        // 设置 User-Agent - 使用移动设备标识
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Linux; Android 15; Mobile) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/120.0.0.0 Mobile Safari/537.36");

        // 添加请求头
        client.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        client.DefaultRequestHeaders.Add("Referer", "https://gitee.com");

        // 添加更多现代浏览器头以提高兼容性
        client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\"");
        client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?1");
        client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Android\"");
    }

    private static bool HasInvalidControlCharacters(string input)
    {
        var allowedWhitespace = new[] { ' ', '\t', '\n', '\r' };

        return input.Any(c => char.IsControl(c) && !allowedWhitespace.Contains(c));
    }

    private static bool IsValidJson(string str)
    {
        try
        {
            using (JsonDocument.Parse(str))
            {
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }
    }
}