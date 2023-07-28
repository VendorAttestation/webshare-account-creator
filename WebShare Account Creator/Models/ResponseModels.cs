public class RegistrationResponse
{
    public string Token { get; set; }
}
public class ProxyConfig
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class Result
{
    public string id { get; set; }
    public string username { get; set; }
    public string password { get; set; }
    public string proxy_address { get; set; }
    public int port { get; set; }
    public bool valid { get; set; }
    public DateTime last_verification { get; set; }
    public string country_code { get; set; }
    public string city_name { get; set; }
    public string asn_name { get; set; }
    public int asn_number { get; set; }
    public bool high_country_confidence { get; set; }
    public DateTime created_at { get; set; }
}

public class StaticProxyConfig
{
    public int count { get; set; }
    public object next { get; set; }
    public object previous { get; set; }
    public List<Result> results { get; set; }
}
