using AvailKit;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

static class Test
{
    const string Id="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",Other="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",Token="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",Key="original-intent-key-0001";
    static JsonElement fixtures,contract;
    static readonly JsonSerializerOptions Json=new(){PropertyNameCaseInsensitive=false};
    [DllImport("libc",EntryPoint="mkfifo",SetLastError=true)] static extern int Mkfifo(string path,uint mode);
    static int checks;
    static void Assert(bool pass,string message="Assertion failed"){checks++;if(!pass)throw new Exception(message);}
    static async Task<AvailKitException> Failed(Task task){try{await task;throw new Exception("Expected SDK failure");}catch(AvailKitException e){Assert(e.InnerException is null);return e;}}
    static void Input(Action action){try{action();throw new Exception("Expected input error");}catch(AvailKitException e){Assert(e.Kind=="input");}}
    static string Str(JsonElement x,string p)=>x.GetProperty(p).GetString()!;
    static JsonElement Fixture(string op)=>fixtures.GetProperty(op);
    static string Name(string op)=>op switch {"getPublicPage"=>"GetPageAsync","listPublicOfferings"=>"ListOfferingsAsync","publicAvailability"=>"AvailabilityAsync","bookPublic"=>"BookAsync","getPublicBooking"=>"GetBookingAsync","cancelPublicBooking"=>"CancelAsync","reschedulePublicBooking"=>"RescheduleAsync","setHours"=>"ReplaceHoursAsync","updateResource"=>"ReplaceResourceAsync","updateOffering"=>"ReplaceOfferingAsync",_=>char.ToUpperInvariant(op[0])+op[1..]+"Async"};
    public static async Task Main()
    {
        using var f=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"fixtures.json")));fixtures=f.RootElement;
        using var c=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"contract.json")));contract=c.RootElement;
        await Matrix();await HoursAcknowledgments();await Errors();await LossReplayIsolation();await Intents();await NoFollowups();await Deadlines();await InvalidResponses();await Inputs();PrivateAndWebhook();
        Console.WriteLine($"PASS .NET: 34 workspace + 7 public HTTP operations, uncertainty/error/isolation/fault checks; {checks} assertions");
    }
    static async Task Matrix()
    {
        await using var s=new Server();using var workspace=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());using var pub=new PublicBookingClient(Id,s.Options());int wc=0,pc=0;
        foreach(var entry in contract.GetProperty("routes").EnumerateObject())
        {
            string op=entry.Name;var spec=entry.Value;var fixture=Fixture(op);bool isPublic=spec.GetProperty("public").GetBoolean();object client=isPublic?pub:workspace;var args=new List<object?>();
            string path=Str(spec,"path").Replace("{workspaceId}","").Replace("{pageId}","");foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(path,"\\{([^}]+)\\}"))args.Add(fixture.GetProperty("ids").TryGetProperty(m.Groups[1].Value,out var id)?id.GetString():Id);
            var input=spec.GetProperty("request");if(input.ValueKind!=JsonValueKind.Null&&input.TryGetProperty("$ref",out var reference))
            {
                var type=reference.GetString()!.Split('/')[^1];if(type is not "Empty" and not "WebhookDisable")args.Add(fixture.GetProperty("request").Deserialize(typeof(WorkspaceClient).Assembly.GetType("AvailKit."+type)!,Json));
            }
            foreach(var p in spec.GetProperty("parameters").EnumerateArray())
            {
                var n=Str(p,"name");var query=fixture.GetProperty("query");var schema=p.GetProperty("schema");
                if(Str(schema,"type")=="integer")args.Add(query.TryGetProperty(n,out var v)?v.GetInt64():schema.TryGetProperty("default",out var fallback)?fallback.GetInt64():0L);
                else args.Add(query.TryGetProperty(n,out var v)?v.GetString():null);
            }
            if(spec.GetProperty("idempotent").GetBoolean())args.Add(Key);
            if(isPublic&&new[]{"bookPublic","getPublicBooking","cancelPublicBooking","reschedulePublicBooking"}.Contains(op)){args.Add(Token);if(op=="bookPublic")args.Add(Token);}
            args.Add(CancellationToken.None);s.Reply(op);var method=client.GetType().GetMethod(Name(op))??throw new Exception("Missing typed operation "+op);var task=(Task)method.Invoke(client,args.ToArray())!;await task;var result=task.GetType().GetProperty("Result")!.GetValue(task)!;Assert(result.ToString()!.Contains("redacted"));
            var call=await s.Take();var expected=System.Text.RegularExpressions.Regex.Replace(Str(spec,"path"),"\\{[^}]+\\}",Id);Assert(call.Path.Split('?')[0]==expected,op+" path");Assert(call.Method==Str(spec,"method"));Assert(!call.Headers.ContainsKey("Cookie"));
            if(isPublic){pc++;Assert(!call.Headers.GetValueOrDefault("Authorization","").StartsWith("Bearer",StringComparison.Ordinal));}else{wc++;Assert(call.Headers["Authorization"]=="Bearer "+Token);}
            if(input.ValueKind==JsonValueKind.Null)Assert(call.Body.Length==0);else{using var body=JsonDocument.Parse(call.Body);Assert(JsonElement.DeepEquals(body.RootElement,fixture.GetProperty("request")),op+" body");}
            Assert(call.Headers.GetValueOrDefault("Idempotency-Key")== (spec.GetProperty("idempotent").GetBoolean()?Key:null));if(op=="bookPublic"){Assert(call.Headers["Authorization"]=="Booking "+Token);Assert(call.Headers["X-AvailKit-Contact-Token"]==Token);}
        }
        Assert(wc==34&&pc==7&&s.Count==41);
    }
    static async Task HoursAcknowledgments()
    {
        await using var s=new Server();using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());
        foreach(string windows in new[]{"[{\"weekday\":1,\"start\":\"25:00\",\"end\":\"26:00\"}]","[{\"weekday\":1,\"start\":\"23:00\",\"end\":\"24:30\"}]","[{\"weekday\":1,\"start\":\"09:00\",\"end\":\"11:00\"},{\"weekday\":1,\"start\":\"10:00\",\"end\":\"12:00\"}]"})
        {s.Replies.Writer.TryWrite(new Reply(200,"{\"windows\":"+windows+"}"));var e=await Failed(c.ReplaceHoursAsync(Id,new Hours(new[]{new HoursWindow(1,"09:00","10:00")})));Assert(e.Kind=="protocol"&&e.Uncertain);}Assert(s.Count==3);
    }
    static async Task Errors()
    {
        await using var s=new Server();using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());
        foreach(int status in new[]{401,403,409,429}){s.Replies.Writer.TryWrite(new Reply(status,"{\"error\":{\"code\":\"scope_denied\",\"message\":\""+Token+"\"}}"));var e=await Failed(c.BookAsync(new BookingInput(Id,"2030-01-01T09:00:00Z"),Key));Assert(e.Kind=="api"&&e.Code=="scope_denied"&&e.Status==status&&!e.Uncertain);Assert(!e.ToString().Contains(Token));}
        s.Replies.Writer.TryWrite(new Reply(403,"{\"error\":{\"code\":\""+Token+"\",\"message\":\"private\"}}"));var unknown=await Failed(c.BookAsync(new BookingInput(Id,"2030-01-01T09:00:00Z"),Secrets.NewIdempotencyKey()));Assert(unknown.Code=="api_error"&&unknown.Uncertain);Assert(!unknown.ToString().Contains(Token));Assert(s.Count==5);
    }
    static async Task LossReplayIsolation()
    {
        await using var s=new Server();using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());var original=c.PrepareBook(new BookingInput(Id,"2030-01-01T09:00:00Z"),Key);var saved=original.ExportPrivate();s.Replies.Writer.TryWrite(new Reply(201,""){Drop=true});Assert((await Failed(c.ExecuteAsync(original))).Uncertain);var first=await s.Take();await Task.Delay(100);Assert(s.Count==1);Assert(saved.SequenceEqual(original.ExportPrivate()));
        var restored=RequestIntent.ImportPrivate(saved);s.Reply("book");Assert((await c.ExecuteAsync(restored)).Id==Id);var second=await s.Take();Assert(first.Body.SequenceEqual(second.Body));Assert(first.Headers["Idempotency-Key"]==second.Headers["Idempotency-Key"]);Assert(first.Headers["Authorization"]==second.Headers["Authorization"]);Assert(!restored.ToString().Contains(Key));Assert(s.Count==2);
        using var live=new WorkspaceClient(Id,AvailKit.Environment.Live,Token,s.Options());using var foreign=new WorkspaceClient(Other,AvailKit.Environment.Test,Token,s.Options());using var changedKey=new WorkspaceClient(Id,AvailKit.Environment.Test,new string('B',42)+"A",s.Options());Input(()=>live.ExecuteAsync(restored));Input(()=>foreign.ExecuteAsync(restored));Input(()=>changedKey.ExecuteAsync(restored));
    }
    static async Task Intents()
    {
        await using var s=new Server();using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());var original=c.PrepareCancel(Id,Key);
        foreach(string kind in new[]{"expired","future","nearFuture","path","operation","headers"})
        {
            var m=System.Text.Json.Nodes.JsonNode.Parse(original.ExportPrivate())!;
            if(kind=="expired")m["createdAt"]=DateTimeOffset.UtcNow.AddDays(-2).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");if(kind=="future")m["createdAt"]=DateTimeOffset.UtcNow.AddMinutes(1).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");if(kind=="nearFuture")m["createdAt"]=DateTimeOffset.UtcNow.AddSeconds(1).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");if(kind=="path")m["path"]="/customers/"+Id+"/erase";if(kind=="operation")m["operation"]="eraseCustomer";if(kind=="headers")m["headers"]=new System.Text.Json.Nodes.JsonObject{["Authorization"]="Bearer "+Token};
            var changed=RequestIntent.ImportPrivate(JsonSerializer.SerializeToUtf8Bytes(m));Input(()=>c.ExecuteAsync(changed));
        }
        Assert(s.Count==0);
    }
    static async Task NoFollowups()
    {
        await using var s=new Server();using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());foreach(int status in new[]{307,308,408,503}){s.Replies.Writer.TryWrite(new Reply(status,"{\"error\":{\"code\":\"unavailable\",\"message\":\"unavailable\"}}"));await Failed(c.BookAsync(new BookingInput(Id,"2030-01-01T09:00:00Z"),Secrets.NewIdempotencyKey()));await s.Take();}await Task.Delay(100);Assert(s.Count==4);
    }
    static async Task Deadlines()
    {
        await using(var s=new Server()){using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options(180));s.Replies.Writer.TryWrite(new Reply(201,Fixture("book").GetProperty("response").GetRawText()){ByteDelay=40});var clock=System.Diagnostics.Stopwatch.StartNew();var e=await Failed(c.BookAsync(new BookingInput(Id,"2030-01-01T09:00:00Z"),Key));Assert(e.Code=="timeout"&&e.Uncertain);Assert(clock.ElapsedMilliseconds<1200);Assert(s.Count==1);}
        await using(var s=new Server()){using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());s.Replies.Writer.TryWrite(new Reply(201,Fixture("book").GetProperty("response").GetRawText()){ByteDelay=40});using var cancel=new CancellationTokenSource();var task=c.BookAsync(new BookingInput(Id,"2030-01-01T09:00:00Z"),Key,cancel.Token);await s.Take();cancel.Cancel();var e=await Failed(task);Assert(e.Code=="cancelled"&&e.Uncertain&&s.Count==1);}
    }
    static async Task InvalidResponses()
    {
        await using(var s=new Server()){using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());foreach(var body in new[]{"{}","null",Fixture("getBooking").GetProperty("response").GetRawText().Replace("\"id\": \""+Id,"\"id\": \""+Other),"{\"id\":\""+Id+"\",\"id\":\""+Id+"\"}"}){s.Replies.Writer.TryWrite(new Reply(200,body));Assert((await Failed(c.GetBookingAsync(Id))).Kind=="protocol");}}
        await using(var escaped=new Server()){using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,escaped.Options());escaped.Replies.Writer.TryWrite(new Reply(200,Fixture("getBooking").GetProperty("response").GetRawText().Replace("\"confirmed\"","\"\\u0063onfirmed\"")));Assert((await c.GetBookingAsync(Id)).Status=="confirmed");var bad=System.Text.Json.Nodes.JsonNode.Parse(Fixture("createWebhook").GetProperty("response").GetRawText())!;bad["signingSecret"]=Token+"\n";escaped.Replies.Writer.TryWrite(new Reply(201,bad.ToJsonString()));var unknown=await Failed(c.CreateWebhookAsync(new WebhookCreate("https://receiver.example.invalid/hook")));Assert(unknown.Kind=="protocol"&&unknown.Uncertain);}
        await using(var s=new Server()){using var c=new PublicBookingClient(Id,s.Options());s.Replies.Writer.TryWrite(new Reply(200,Fixture("getPublicPage").GetProperty("response").GetRawText()){NoStore=false});Assert((await Failed(c.GetPageAsync())).Code=="invalid_response");s.Replies.Writer.TryWrite(new Reply(200,Fixture("getPublicPage").GetProperty("response").GetRawText().Replace(Id,Other)));Assert((await Failed(c.GetPageAsync())).Code=="invalid_response");}
        await using(var s=new Server()){using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options(2000,32));s.Reply("getBooking");Assert((await Failed(c.GetBookingAsync(Id))).Code=="response_too_large");}
    }
    static async Task Inputs()
    {
        await using var s=new Server();using var c=new WorkspaceClient(Id,AvailKit.Environment.Test,Token,s.Options());Input(()=>c.ListResourcesAsync(null,101,null));Input(()=>c.ListResourcesAsync("bad",25,null));Input(()=>c.ListResourcesAsync(null,25," \n"));Input(()=>c.PrepareBook(new BookingInput("bad","2030-01-01T09:00:00Z"),Key));Input(()=>c.AvailabilityAsync(Id,"2030-01-01T09:00:00Z","2030-01-09T09:00:00Z"));Input(()=>c.ReplaceHoursAsync(Id,new Hours(new[]{new HoursWindow(1,"09:00","11:00"),new HoursWindow(1,"10:00","12:00")})));Input(()=>c.PrepareRescheduleBooking(Id,new RescheduleInput("2030-01-01T09:00:00.0001Z","2030-01-01T10:00:00Z"),Key));Assert(s.Count==0);Input(()=>new ClientOptions("https://key@api.availkit.com"));Input(()=>new ClientOptions("http://api.availkit.com"));
        string? publicPageId=new BookingPageSettings(null,"Example",false,false).PageId;string? finalCursor=new PublicOfferingPage(Array.Empty<PublicOffering>(),null).NextAfter;Assert(publicPageId is null&&finalCursor is null);var nullability=new NullabilityInfoContext();Assert(nullability.Create(typeof(BookingPageSettings).GetProperty("PageId")!).ReadState==NullabilityState.Nullable);Assert(nullability.Create(typeof(PublicOfferingPage).GetProperty("NextAfter")!).ReadState==NullabilityState.Nullable);Assert(nullability.Create(typeof(CustomerPage).GetProperty("NextAfter")!).ReadState==NullabilityState.Nullable);var source=new List<Resource>();var page=new ResourcePage(source,null);source.Add(new Resource(Id,"x","UTC",1,false));Assert(page.Items.Count==0);
    }
    static void PrivateAndWebhook()
    {
        byte[] body=Encoding.UTF8.GetBytes("{\"event\":1}"),key=Convert.FromBase64String(Token+"="),message=Encoding.UTF8.GetBytes("1000."+Encoding.UTF8.GetString(body));string signature="t=1000,v1="+Convert.ToHexStringLower(HMACSHA256.HashData(key,message));Assert(Webhooks.Verify(body,signature,Token,1000,0));Assert(!Webhooks.Verify("{}"u8.ToArray(),signature,Token,1000,300));Assert(!Webhooks.Verify(body,signature,Token,1301,300));Assert(!Webhooks.Verify(body,signature+",v1=bad",Token,1000,300));
        if(OperatingSystem.IsLinux()&&RuntimeInformation.OSArchitecture==Architecture.X64)
        {
            string dir=Directory.CreateTempSubdirectory("availkit-dotnet-private-").FullName;File.SetUnixFileMode(dir,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);try{string fifo=Path.Combine(dir,"pipe");Assert(Mkfifo(fifo,0x180)==0);var timer=System.Diagnostics.Stopwatch.StartNew();Input(()=>PrivateFiles.Read(fifo));Assert(timer.ElapsedMilliseconds<500);string path=Path.Combine(dir,"intent.json");PrivateFiles.WriteNew(path,body);Assert(body.SequenceEqual(PrivateFiles.Read(path,1024)));Input(()=>PrivateFiles.WriteNew(path,body));string link=Path.Combine(dir,"link");File.CreateSymbolicLink(link,path);Input(()=>PrivateFiles.Read(link));File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.GroupRead);Input(()=>PrivateFiles.Read(path));}finally{Directory.Delete(dir,true);}
        }
    }
    sealed record Capture(string Path,string Method,Dictionary<string,string> Headers,byte[] Body);
    sealed class Reply(int status,string body){public int Status=status;public string Body=body;public bool Drop,NoStore=true;public int ByteDelay;}
    sealed class Server : IAsyncDisposable
    {
        readonly TcpListener listener=new(IPAddress.Loopback,0);readonly CancellationTokenSource stop=new();readonly Task accept;readonly List<Task> workers=new();int count;
        public readonly Channel<Reply> Replies=Channel.CreateUnbounded<Reply>();readonly Channel<Capture> calls=Channel.CreateUnbounded<Capture>();public int Count=>Volatile.Read(ref count);
        public Server(){listener.Start();accept=Accept();}
        public ClientOptions Options(int ms=2000,int max=1048576)=>new("http://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port,TimeSpan.FromMilliseconds(ms),max);
        public void Reply(string op)=>Replies.Writer.TryWrite(new Reply(contract.GetProperty("routes").GetProperty(op).GetProperty("status").GetInt32(),Fixture(op).GetProperty("response").GetRawText()));
        public async Task<Capture> Take()=>await calls.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3));
        async Task Accept(){try{while(!stop.IsCancellationRequested){var client=await listener.AcceptTcpClientAsync(stop.Token);var task=Serve(client);lock(workers)workers.Add(task);}}catch(OperationCanceledException){}catch(SocketException){} }
        async Task Serve(TcpClient client)
        {
            using(client)try
            {
                var stream=client.GetStream();var head=new List<byte>();var one=new byte[1];while(head.Count<32768){if(await stream.ReadAsync(one,stop.Token)==0)return;head.Add(one[0]);if(head.Count>=4&&head[^4]==13&&head[^3]==10&&head[^2]==13&&head[^1]==10)break;}
                var lines=Encoding.ASCII.GetString(head.ToArray()).Split("\r\n");var first=lines[0].Split(' ');var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);foreach(var line in lines.Skip(1)){int colon=line.IndexOf(':');if(colon>0)headers.Add(line[..colon],line[(colon+1)..].Trim());}int length=int.Parse(headers.GetValueOrDefault("Content-Length","0"));if(length<0||length>16384)return;var body=new byte[length];await stream.ReadExactlyAsync(body,stop.Token);Interlocked.Increment(ref count);calls.Writer.TryWrite(new Capture(first[1],first[0],headers,body));var reply=await Replies.Reader.ReadAsync(stop.Token);if(reply.Drop)return;var bytes=Encoding.UTF8.GetBytes(reply.Body);
                var raw="HTTP/1.1 "+reply.Status+" Response\r\nContent-Type: application/json\r\nContent-Length: "+bytes.Length+"\r\nConnection: close\r\n"+(reply.NoStore?"Cache-Control: no-store\r\n":"")+(reply.Status is 307 or 308?"Location: /redirected\r\n":"")+(reply.Status is 408 or 503?"Retry-After: 0\r\n":"")+"\r\n";await stream.WriteAsync(Encoding.ASCII.GetBytes(raw),stop.Token);
                if(reply.ByteDelay>0){foreach(byte b in bytes){await stream.WriteAsync(new[]{b},stop.Token);await stream.FlushAsync(stop.Token);await Task.Delay(reply.ByteDelay,stop.Token);}}else await stream.WriteAsync(bytes,stop.Token);
            }
            catch(OperationCanceledException){}catch(IOException){}catch(SocketException){}
        }
        public async ValueTask DisposeAsync(){stop.Cancel();listener.Stop();await accept;Task[] tasks;lock(workers)tasks=workers.ToArray();await Task.WhenAll(tasks);stop.Dispose();}
    }
}
