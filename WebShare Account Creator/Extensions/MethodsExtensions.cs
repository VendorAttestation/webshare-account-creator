using Bogus;
using System.Net;
using System.Net.Http.Json;
using Zennolab.CapMonsterCloud;
using Zennolab.CapMonsterCloud.Requests;

public static class MethodsExensions
{
    private static readonly object fileWriteLock = new object();
    private static int ProxyIndex = 0;
    private static string[] Proxies = File.ReadAllLines("proxies.txt");
    private static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36";
    public static async Task<string> SolveCaptcha()
    {

        var clientOptions = new ClientOptions
        {
            ClientKey = File.ReadAllText("capmonsterkey.txt"),
        };
        var cmCloudClient = CapMonsterCloudClientFactory.Create(clientOptions);
        var recaptchaV2Request = new RecaptchaV2Request
        {
            WebsiteUrl = "https://proxy2.webshare.io/",
            WebsiteKey = "6LeHZ6UUAAAAAKat_YS--O2tj_by3gv3r_l03j9d",
            UserAgent = UserAgent,
        };

        var recaptchaV2Result = await cmCloudClient.SolveAsync(recaptchaV2Request);
        var solution = recaptchaV2Result.Solution.Value;

        if(solution != null)
        {
            return solution;
        }
        else
        {
            throw new FailedCaptcha();
        }
    }


    private static readonly object ProxiesLock = new object();

    private static string GetNextProxy()
    {
        string proxy;

        lock (ProxiesLock)
        {
            if (ProxyIndex >= Proxies.Count())
            {
                throw new InvalidOperationException("No Proxies");
            }

            proxy = Proxies[ProxyIndex];
        }

        Interlocked.Increment(ref ProxyIndex);

        return proxy;
    }
    private static WebProxy CreateProxyWithCredentials(string[] proxyComponents)
    {
        string host = proxyComponents[0];
        int port = int.Parse(proxyComponents[1]);
        string username = proxyComponents[2];
        string password = proxyComponents[3];

        ICredentials credentials = new NetworkCredential(username, password);
        var proxyUri = new Uri($"http://{host}:{port}");

        return new WebProxy(proxyUri, false, null, credentials);
    }

    public static WebProxy GetProxy()
    {
        try
        {
            string proxy = GetNextProxy();

            string[] proxyComponents = proxy.Split(':');

            WebProxy webProxy;

            switch (proxyComponents.Length)
            {
                case 2:
                    string host = proxyComponents[0];
                    int port = int.Parse(proxyComponents[1]);

                    webProxy = new WebProxy(host, port);
                    break;
                case 4:
                    webProxy = CreateProxyWithCredentials(proxyComponents);
                    break;
                default:
                    throw new ArgumentException("Invalid proxy format.");
            }

            return webProxy;
        }
        catch (Exception)
        {
            throw;
        }
    }


    public static async Task<string> Register(string capKey)
    {
        using var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All, Proxy = GetProxy() });
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Host", "proxy.webshare.io");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://proxy2.webshare.io");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://proxy2.webshare.io/");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "same-site");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Pragma", "no-cache");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");
        client.DefaultRequestHeaders.TryAddWithoutValidation("TE", "trailers");

        var faker = new Faker();
        var payload = new
        {
            email = faker.Internet.Email(),
            password = RandomExtensions.NextString(new Random(), 12),
            tos_accepted = true,
            recaptcha = capKey
        };

        var response = await client.PostAsJsonAsync("https://proxy.webshare.io/api/v2/register/", payload);

        response.EnsureSuccessStatusCode();

        if (response?.RequestMessage?.RequestUri != null)
        {
            var respJson = await response.Content.ReadFromJsonAsync<RegistrationResponse>();

            if (respJson?.Token != null)
            {
                return respJson.Token;
            }
            else
            {
                throw new NullReferenceException("The response object returned null.");
            }
        }
        else
        {
            throw new NullReferenceException("The request URI argument was null.");
        }
    }

    public static async Task GetProxy(string authToken)
    {
        try
        {
            using var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All, Proxy = GetProxy() });
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Token {authToken}");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

            var rotatingResponse = await client.GetAsync("https://proxy.webshare.io/api/v2/proxy/config/");
            rotatingResponse.EnsureSuccessStatusCode();

            var rotatingProxyConfig = await rotatingResponse.Content.ReadFromJsonAsync<ProxyConfig>();
            if (rotatingProxyConfig != null)
            {
                var username = rotatingProxyConfig.Username;
                var password = rotatingProxyConfig.Password;
                if (username == null || password == null)
                {
                    throw new Exception("One or both of the values within the JSON file were missing.");
                }

                var proxy = $"{username}-rotate:{password}@p.webshare.io:80";

                // Use a lock to ensure thread-safe file writing
                await AppendRotatingProxyAsync(proxy);

                Console.WriteLine($"Added rotating proxy {proxy} to rotating_proxies.txt");
            }

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Token {authToken}");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

            var staticResponse = await client.GetAsync("https://proxy.webshare.io/api/v2/proxy/list/?mode=direct&page=1&page_size=10");
            staticResponse.EnsureSuccessStatusCode();

            var staticProxyConfig = await staticResponse.Content.ReadFromJsonAsync<StaticProxyConfig>();
            if (staticProxyConfig != null)
            {
                foreach(Result static_proxy in staticProxyConfig.results)
                {
                    var Username = static_proxy.username;
                    var Password = static_proxy.password;
                    var IP = static_proxy.proxy_address;
                    var Port = static_proxy.port;
                    if (Username == null || Password == null || IP == null || Port == 0)
                    {
                        throw new Exception("One or both of the values within the JSON file were missing.");
                    }

                    var proxy = $"{Username}:{Username}@{IP}:{Port}";

                    // Use a lock to ensure thread-safe file writing
                    await AppendProxysAsync(proxy);

                    Console.WriteLine($"Added static proxy {proxy} to static_proxies.txt");
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while getting the proxy.", ex);
        }
    }


    private static async Task AppendRotatingProxyAsync(string proxy)
    {
        await Task.Yield(); // Ensures the method is asynchronous

        // Use a lock to ensure thread-safe file writing
        lock (fileWriteLock)
        {
            using (var sw = File.AppendText("rotating_proxies.txt"))
            {
                sw.WriteLine(proxy);
            }
        }
    }
    private static async Task AppendProxysAsync(string proxy)
    {
        await Task.Yield(); // Ensures the method is asynchronous

        // Use a lock to ensure thread-safe file writing
        lock (fileWriteLock)
        {
            using (var sw = File.AppendText("static_proxies.txt"))
            {
                sw.WriteLine(proxy);
            }
        }
    }
}
