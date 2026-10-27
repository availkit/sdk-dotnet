
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
namespace AvailKit;
/// <summary>Owned HTTP engine. Clients support disposal and per-call cancellation; no caller-supplied auth middleware.</summary>
public abstract class ClientCore : IDisposable
{
    private readonly ClientOptions options;
    private readonly string root,scope,scopeId;
    private readonly bool publicClient;
    private readonly Dictionary<string,string> headers;
    private readonly HttpClient http;
    private readonly SemaphoreSlim active=new(32,32);
    private bool closed;
    private static readonly HashSet<string> SafeCodes=new(("booking_page_paused booking_access_in_use contact_changed validation_failed synthetic_email_required unauthorized forbidden scope_denied not_found invalid_input invalid_query invalid_cursor invalid_limit quota_exceeded unavailable credential_invalid capability_denied customer_archived idempotency_conflict capacity_conflict capacity_exceeded version_conflict catalog_in_use catalog_archived event_cursor_expired invalid_id invalid_range identity_unavailable recovery_unavailable csrf_invalid recent_auth_required secret_already_revealed idempotency_mismatch membership_changed membership_denied last_credential identity_limit identity_rate_limited membership_conflict request_key_invalid").Split(' '));
    protected ClientCore(string pageId,ClientOptions options) : this(Protocol.Id(pageId),options,true,new(),options.BaseUrl+":public:"+pageId) {}
    protected ClientCore(string workspaceId,Environment environment,string credential,ClientOptions options) : this(Protocol.Id(workspaceId),options,false,new(){["Authorization"]="Bearer "+Protocol.Token(credential)},WorkspaceScope(workspaceId,environment,credential,options)) {}
    private static string WorkspaceScope(string id,Environment environment,string credential,ClientOptions options) {if(!Enum.IsDefined(environment))throw AvailKitException.Input("invalid_environment");return options.BaseUrl+":"+Protocol.Id(id)+":"+environment.ToString().ToLowerInvariant()+":"+Protocol.Hash(Protocol.Token(credential));}
    private ClientCore(string id,ClientOptions options,bool isPublic,Dictionary<string,string> headers,string scope)
    {
        this.options=options;this.scopeId=id;this.scope=scope;this.publicClient=isPublic;this.headers=headers;root=(isPublic?"/v1/public/pages/":"/v1/workspaces/")+id;
        var handler=new SocketsHttpHandler { AllowAutoRedirect=false,UseCookies=false,UseProxy=false,AutomaticDecompression=DecompressionMethods.None,Credentials=null,PreAuthenticate=false,MaxConnectionsPerServer=4,MaxResponseHeadersLength=32,ConnectTimeout=options.Timeout,PooledConnectionLifetime=TimeSpan.FromMinutes(2) };
        http=new HttpClient(handler){Timeout=Timeout.InfiniteTimeSpan,DefaultRequestVersion=HttpVersion.Version11,DefaultVersionPolicy=HttpVersionPolicy.RequestVersionExact};
    }
    public void Dispose(){closed=true;http.Dispose();GC.SuppressFinalize(this);}
    public override string ToString()=>"AvailKit client [credentials redacted]";
    protected static Dictionary<string,string> Query(params object?[] pairs) {var q=new Dictionary<string,string>();for(int i=0;i<pairs.Length;i+=2)if(pairs[i+1] is not null)q[(string)pairs[i]!]=Convert.ToString(pairs[i+1],CultureInfo.InvariantCulture)!;return q;}
    protected static Dictionary<string,string> Access(string bookingToken,string? contactToken) {var h=new Dictionary<string,string>{["Authorization"]="Booking "+Protocol.Token(bookingToken)};if(contactToken is not null)h["X-AvailKit-Contact-Token"]=Protocol.Token(contactToken);return h;}
    private string Path(string op,string[] ids)
    {
        var s=Protocol.Route(op);if(s.GetProperty("public").GetBoolean()!=publicClient)throw AvailKitException.Input("invalid_operation");var path=Protocol.Str(s,"path").Replace(publicClient?"/v1/public/pages/{pageId}":"/v1/workspaces/{workspaceId}","",StringComparison.Ordinal);
        foreach(var id in ids)path=new System.Text.RegularExpressions.Regex("\\{[^}]+\\}").Replace(path,Protocol.Id(id),1);if(path.Contains('{'))throw AvailKitException.Input("invalid_id");return path;
    }
    protected RequestIntent Prepare(string op,string[] ids,object body,string key,Dictionary<string,string> extra)
    {
        var s=Protocol.Route(op);if(!s.GetProperty("idempotent").GetBoolean())throw AvailKitException.Input("unsafe_replay");return new(op,Path(op,ids),Protocol.Body(body,s.GetProperty("request"))!,Protocol.Key(key),scope,extra,DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'",CultureInfo.InvariantCulture));
    }
    protected Task<T> ExecuteAsync<T>(RequestIntent intent,CancellationToken cancellationToken)
    {
        if(intent is null||intent.Scope!=scope)throw AvailKitException.Input("intent_scope_mismatch");var s=Protocol.Route(intent.Operation);if(!s.GetProperty("idempotent").GetBoolean()||s.GetProperty("public").GetBoolean()!=publicClient)throw AvailKitException.Input("unsafe_replay");
        var age=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000m-Protocol.Instant(intent.CreatedAt);if(age>=86400)throw AvailKitException.Input("intent_expired");if(age<0)throw AvailKitException.Input("invalid_intent_timestamp");
        var ids=System.Text.RegularExpressions.Regex.Matches(intent.Path,"[0-9a-f]{32}").Select(m=>m.Value).ToArray();if(Path(intent.Operation,ids)!=intent.Path)throw AvailKitException.Input("invalid_intent");
        var bytes=intent.Body;var body=Protocol.Parse(bytes);if(!Protocol.Valid(body,s.GetProperty("request")))throw AvailKitException.Input("invalid_intent");Protocol.Body(body,s.GetProperty("request"));Protocol.Key(intent.Key);
        Dictionary<string,string> expected=new();if(publicClient){var auth=intent.Headers.GetValueOrDefault("Authorization","");if(!auth.StartsWith("Booking ",StringComparison.Ordinal))throw AvailKitException.Input("invalid_intent");expected=Access(auth[8..],intent.Operation=="bookPublic"?intent.Headers.GetValueOrDefault("X-AvailKit-Contact-Token"):null);if(intent.Operation=="bookPublic"&&!expected.ContainsKey("X-AvailKit-Contact-Token"))throw AvailKitException.Input("invalid_intent");}
        if(intent.Headers.Count!=expected.Count||expected.Any(p=>intent.Headers.GetValueOrDefault(p.Key)!=p.Value))throw AvailKitException.Input("invalid_intent");
        return SendAsync<T>(intent.Operation,intent.Path,new(),bytes,intent.Key,new(intent.Headers),cancellationToken);
    }
    protected Task<T> CallAsync<T>(string op,string[] ids,object? body,Dictionary<string,string> q,string? key,Dictionary<string,string> extra,CancellationToken cancellationToken)
    {
        var s=Protocol.Route(op);Protocol.Query(s,q);return SendAsync<T>(op,Path(op,ids),q,Protocol.Body(body,s.GetProperty("request")),key,extra,cancellationToken);
    }
    private async Task<T> SendAsync<T>(string op,string path,Dictionary<string,string> q,byte[]? body,string? key,Dictionary<string,string> extra,CancellationToken cancellationToken)
    {
        if(closed)throw AvailKitException.Input("client_closed");if(cancellationToken.IsCancellationRequested)throw new AvailKitException("transport","cancelled");if(!active.Wait(0))throw new AvailKitException("transport","client_busy");
        var s=Protocol.Route(op);bool mutation=Protocol.Str(s,"method")!="GET",admitted=false;
        using var budget=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);budget.CancelAfter(options.Timeout);
        try
        {
            string suffix=q.Count==0?"":"?"+string.Join("&",q.Select(p=>Uri.EscapeDataString(p.Key)+"="+Uri.EscapeDataString(p.Value)));
            using var req=new HttpRequestMessage(new HttpMethod(Protocol.Str(s,"method")),options.BaseUrl+root+path+suffix){Version=HttpVersion.Version11,VersionPolicy=HttpVersionPolicy.RequestVersionExact};
            req.Headers.Accept.ParseAdd("application/json");req.Headers.AcceptEncoding.ParseAdd("identity");req.Headers.CacheControl=new CacheControlHeaderValue{NoStore=true};req.Headers.UserAgent.ParseAdd("availkit-dotnet/0.1.1");
            foreach(var p in headers)req.Headers.Add(p.Key,p.Value);foreach(var p in extra)req.Headers.Add(p.Key,p.Value);if(key is not null)req.Headers.Add("Idempotency-Key",Protocol.Key(key));if(body is not null)req.Content=new OneShotContent(body);
            admitted=true;using var response=await http.SendAsync(req,HttpCompletionOption.ResponseHeadersRead,budget.Token).ConfigureAwait(false);int status=(int)response.StatusCode;
            string media=response.Content.Headers.ContentType?.MediaType??"";
            if(status is >=300 and <400||media!="application/json"&&media!="application/problem+json"||status is >=200 and <300&&media!="application/json"||response.Content.Headers.ContentEncoding.Any(x=>x!="identity"))throw new AvailKitException("protocol","invalid_response",status,mutation);
            long? length=response.Content.Headers.ContentLength;if(length>options.MaxResponseBytes)throw new AvailKitException("protocol","response_too_large",status,mutation);
            await using var stream=await response.Content.ReadAsStreamAsync(budget.Token).ConfigureAwait(false);using var buffer=new MemoryStream();var chunk=new byte[8192];int count;
            while((count=await stream.ReadAsync(chunk.AsMemory(0,Math.Min(chunk.Length,options.MaxResponseBytes+1-(int)buffer.Length)),budget.Token).ConfigureAwait(false))>0){buffer.Write(chunk,0,count);if(buffer.Length>options.MaxResponseBytes)throw new AvailKitException("protocol","response_too_large",status,mutation);}
            if(length.HasValue&&buffer.Length!=length)throw new AvailKitException("protocol","incomplete_response",status,mutation);budget.Token.ThrowIfCancellationRequested();var data=Protocol.Parse(buffer.ToArray());
            if(status is <200 or >=300)
            {
                string? code=null;if(data.ValueKind==JsonValueKind.Object&&data.TryGetProperty("error",out var err)&&err.ValueKind==JsonValueKind.Object&&err.TryGetProperty("code",out var c)&&c.ValueKind==JsonValueKind.String&&err.TryGetProperty("message",out var m)&&m.ValueKind==JsonValueKind.String)code=c.GetString();
                else if(data.ValueKind==JsonValueKind.Object&&data.TryGetProperty("type",out var ty)&&ty.ValueKind==JsonValueKind.String&&ty.GetString()=="urn:availkit:problem"&&data.TryGetProperty("status",out var st)&&st.TryGetInt32(out var sn)&&sn==status&&data.TryGetProperty("code",out var pc)&&pc.ValueKind==JsonValueKind.String)code=pc.GetString();
                if(code is null)throw new AvailKitException("protocol","invalid_error",status,mutation);throw new AvailKitException("api",SafeCodes.Contains(code)?code:"api_error",status,mutation&&(status>=500||!SafeCodes.Contains(code)));
            }
            var noStore=response.Headers.CacheControl?.ToString()=="no-store";
            if(status!=s.GetProperty("status").GetInt32()||!Protocol.Valid(data,s.GetProperty("response"))||((publicClient||path.StartsWith("/customers",StringComparison.Ordinal)||path.StartsWith("/usage",StringComparison.Ordinal)||path.StartsWith("/maintenance",StringComparison.Ordinal))&&!noStore))throw new AvailKitException("protocol","invalid_response",status,mutation);
            Protocol.Relations(op,path,q,body,data,scopeId);var result=data.Deserialize<T>(Protocol.Json);budget.Token.ThrowIfCancellationRequested();return result??throw new AvailKitException("protocol","invalid_response",status,mutation);
        }
        catch(AvailKitException e) {throw new AvailKitException(e.Kind,e.Code,e.Status,e.Uncertain||mutation&&admitted&&e.Kind=="protocol");}
        catch(OperationCanceledException) {throw new AvailKitException("transport",cancellationToken.IsCancellationRequested?"cancelled":"timeout",null,mutation&&admitted);}
        catch(HttpRequestException) {throw new AvailKitException("transport","request_failed",null,mutation&&admitted);}
        catch {throw new AvailKitException("protocol","invalid_response",null,mutation&&admitted);}
        finally {active.Release();}
    }
    // A runtime cannot serialize the body a second time, including an internal transport followup.
    private sealed class OneShotContent : HttpContent
    {
        private readonly byte[] body;private int used;
        public OneShotContent(byte[] body){this.body=body;Headers.ContentType=new MediaTypeHeaderValue("application/json");}
        protected override bool TryComputeLength(out long length){length=body.Length;return true;}
        protected override Task SerializeToStreamAsync(Stream stream,TransportContext? context)=>Write(stream,CancellationToken.None);
        protected override Task SerializeToStreamAsync(Stream stream,TransportContext? context,CancellationToken token)=>Write(stream,token);
        private Task Write(Stream stream,CancellationToken token){if(Interlocked.Exchange(ref used,1)!=0)throw new IOException("AvailKit request already sent");return stream.WriteAsync(body,token).AsTask();}
    }
}
