
namespace AvailKit;
public enum Environment { Test, Live }
/// <summary>Explicit origin and finite total budget, including response drain. No transport or certificate override.</summary>
public sealed class ClientOptions
{
    public string BaseUrl { get; }
    public TimeSpan Timeout { get; }
    public int MaxResponseBytes { get; }
    public ClientOptions(string baseUrl="https://api.availkit.com", TimeSpan? timeout=null, int maxResponseBytes=1048576)
    {
        if (baseUrl is null || baseUrl.Any(c=>c<=32||c>=127) || !Uri.TryCreate(baseUrl,UriKind.Absolute,out var u) ||
            u.UserInfo.Length!=0 || u.Query.Length!=0 || u.Fragment.Length!=0 || u.AbsolutePath!="/" || u.Port<1 ||
            !(u.Scheme=="https" || u.Scheme=="http" && (u.Host=="localhost"||u.Host=="127.0.0.1"||u.Host=="[::1]"||u.Host=="::1")))
            throw AvailKitException.Input("invalid_base_url");
        var budget=timeout??TimeSpan.FromSeconds(10);
        if(budget.TotalMilliseconds<1 || budget.TotalSeconds>120 || maxResponseBytes<1 || maxResponseBytes>8388608) throw AvailKitException.Input("invalid_limits");
        BaseUrl=u.GetLeftPart(UriPartial.Authority);Timeout=budget;MaxResponseBytes=maxResponseBytes;
    }
    public override string ToString()=>"AvailKit.ClientOptions [redacted]";
}
