
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
namespace AvailKit;
internal static class Protocol
{
    internal static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull, MaxDepth=24, PropertyNameCaseInsensitive=false };
    internal static readonly JsonElement Contract;
    static Protocol() { using var s=typeof(Protocol).Assembly.GetManifestResourceStream("AvailKit.contract.json")??throw new InvalidOperationException("AvailKit contract unavailable");using var d=JsonDocument.Parse(s);Contract=d.RootElement.Clone(); }
    internal static JsonElement Route(string operation) { if(!Contract.GetProperty("routes").TryGetProperty(operation,out var r))throw AvailKitException.Input("invalid_operation");return r; }
    internal static string Id(string value) { if(value is null||!Regex.IsMatch(value,"\\A[0-9a-f]{32}\\z"))throw AvailKitException.Input("invalid_id");return value; }
    internal static string Token(string value) { if(value is null||!Regex.IsMatch(value,"\\A[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]\\z"))throw AvailKitException.Input("invalid_credential");return value; }
    internal static string Key(string value) { if(value is null||!Regex.IsMatch(value,"\\A[\\x20-\\x7e]{16,128}\\z")||value.Trim()!=value)throw AvailKitException.Input("invalid_idempotency_key");return value; }
    internal static string Hash(string value)=>Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    // Decimal seconds retain up to nine fractional digits; comparisons never round to DateTime's seven digits.
    internal static decimal Instant(string value)
    {
        if(value is null||!Regex.IsMatch(value,"\\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\\.[0-9]{1,9})?Z\\z"))throw AvailKitException.Input("invalid_timestamp");
        if(!DateTimeOffset.TryParseExact(value[..19]+"Z","yyyy-MM-dd'T'HH:mm:ss'Z'",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var t))throw AvailKitException.Input("invalid_timestamp");
        return t.ToUnixTimeSeconds()+(value.Length>20?decimal.Parse("0"+value[19..^1],CultureInfo.InvariantCulture):0);
    }
    internal static bool Text(string value,int max)=>value is not null&&!string.IsNullOrWhiteSpace(value)&&value.EnumerateRunes().Count()<=max&&!value.Any(char.IsControl);
    internal static JsonElement Parse(byte[] raw)
    {
        try { using var d=JsonDocument.Parse(raw,new JsonDocumentOptions{MaxDepth=24});CheckUnique(d.RootElement);return d.RootElement.Clone(); }
        catch { throw new AvailKitException("protocol","invalid_json"); }
    }
    private static void CheckUnique(JsonElement v) { if(v.ValueKind==JsonValueKind.Object){var seen=new HashSet<string>();foreach(var p in v.EnumerateObject()){if(!seen.Add(p.Name))throw new JsonException();CheckUnique(p.Value);}}else if(v.ValueKind==JsonValueKind.Array)foreach(var x in v.EnumerateArray())CheckUnique(x); }
    internal static bool Valid(JsonElement v,JsonElement s,int depth=0)
    {
        if(depth>20||s.ValueKind!=JsonValueKind.Object)return false;
        if(s.TryGetProperty("$ref",out var rf))return Valid(v,Contract.GetProperty("schemas").GetProperty(rf.GetString()!.Split('/')[^1]),depth+1);
        if(s.TryGetProperty("allOf",out var all)&&!all.EnumerateArray().All(x=>Valid(v,x,depth+1)))return false;
        if(s.TryGetProperty("anyOf",out var any)&&!any.EnumerateArray().Any(x=>Valid(v,x,depth+1)))return false;
        if(s.TryGetProperty("oneOf",out var one)&&one.EnumerateArray().Count(x=>Valid(v,x,depth+1))!=1)return false;
        if(s.TryGetProperty("const",out var constant)&&!Equal(v,constant))return false;
        if(s.TryGetProperty("enum",out var en)&&!en.EnumerateArray().Any(x=>Equal(v,x)))return false;
        if(s.TryGetProperty("type",out var type)&&!(type.ValueKind==JsonValueKind.String?Kind(v,type.GetString()!):type.EnumerateArray().Any(t=>Kind(v,t.GetString()!))))return false;
        if(v.ValueKind==JsonValueKind.Object)
        {
            s.TryGetProperty("properties",out var props);
            if(s.TryGetProperty("required",out var req)&&req.EnumerateArray().Any(r=>!v.TryGetProperty(r.GetString()!,out _)))return false;
            foreach(var p in v.EnumerateObject())
            {
                if(props.ValueKind==JsonValueKind.Object&&props.TryGetProperty(p.Name,out var sub)){if(!Valid(p.Value,sub,depth+1))return false;}
                else if(s.TryGetProperty("additionalProperties",out var more)&&more.ValueKind==JsonValueKind.False)return false;
                if(p.Name=="signingSecret"){try{Token(p.Value.GetString()!);}catch{return false;}}
                if((p.Name=="id"||p.Name.EndsWith("Id",StringComparison.Ordinal)||p.Name=="nextAfter")&&p.Value.ValueKind==JsonValueKind.String&&!Regex.IsMatch(p.Value.GetString()!,"\\A[0-9a-f]{32}\\z"))return false;
            }
        }
        else if(v.ValueKind==JsonValueKind.Array)
        {
            if(v.GetArrayLength()<Num(s,"minItems",0)||v.GetArrayLength()>Num(s,"maxItems",10000))return false;
            if(s.TryGetProperty("items",out var items)&&!v.EnumerateArray().All(x=>Valid(x,items,depth+1)))return false;
        }
        else if(v.ValueKind==JsonValueKind.String)
        {
            var str=v.GetString()!;var length=str.EnumerateRunes().Count();
            if(length<Num(s,"minLength",0)||length>Num(s,"maxLength",8192))return false;
            if(s.TryGetProperty("pattern",out var pattern)&&!Regex.IsMatch(str,pattern.GetString()!))return false;
            if(s.TryGetProperty("format",out var format)&&format.GetString()=="date-time")try{Instant(str);}catch{return false;}
        }
        else if(v.ValueKind==JsonValueKind.Number)
        {
            if(!v.TryGetInt64(out var n)||n<Num(s,"minimum",-9007199254740991L)||n>Num(s,"maximum",9007199254740991L))return false;
        }
        return true;
    }
    private static bool Equal(JsonElement a,JsonElement b)=>JsonElement.DeepEquals(a,b);
    private static bool Kind(JsonElement v,string kind)=>kind switch {"object"=>v.ValueKind==JsonValueKind.Object,"array"=>v.ValueKind==JsonValueKind.Array,"string"=>v.ValueKind==JsonValueKind.String,"boolean"=>v.ValueKind is JsonValueKind.True or JsonValueKind.False,"null"=>v.ValueKind==JsonValueKind.Null,"integer"=>v.ValueKind==JsonValueKind.Number&&v.TryGetInt64(out _),"number"=>v.ValueKind==JsonValueKind.Number,_=>false};
    private static long Num(JsonElement v,string name,long fallback)=>v.TryGetProperty(name,out var n)?n.GetInt64():fallback;
    internal static byte[]? Body(object? body,JsonElement schema)
    {
        if(schema.ValueKind==JsonValueKind.Null){if(body is not null)throw AvailKitException.Input("invalid_input");return null;}
        try { var raw=JsonSerializer.SerializeToUtf8Bytes(body,Json);if(raw.Length>16384)throw new Exception();var v=Parse(raw);if(!Valid(v,schema))throw new Exception();Semantic(v,schema.TryGetProperty("$ref",out var r)?r.GetString()!.Split('/')[^1]:"");return raw; }
        catch { throw AvailKitException.Input("invalid_input"); }
    }
    internal static void Semantic(JsonElement v,string name)
    {
        if(name is "ResourceInput" or "OfferingInput"&&!Text(Str(v,"name"),160))throw AvailKitException.Input("invalid_input");
        if(name=="ResourceInput"&&!Text(Str(v,"timeZone"),100))throw AvailKitException.Input("invalid_time_zone");
        if(name=="CustomerInput")
        {
            if(!Text(Str(v,"displayName"),160))throw AvailKitException.Input("invalid_customer");
            if(v.TryGetProperty("email",out var e)&&e.ValueKind!=JsonValueKind.Null&&(!Text(e.GetString()!,254)||!Regex.IsMatch(e.GetString()!,"\\A[^@\\s]+@[^@\\s]+\\z")))throw AvailKitException.Input("invalid_customer");
            if(v.TryGetProperty("phone",out var p)&&p.ValueKind!=JsonValueKind.Null&&!Text(p.GetString()!,32))throw AvailKitException.Input("invalid_customer");
        }
        if(name=="PublicBookingInput")Semantic(v.GetProperty("contact"),"CustomerInput");
        if(name=="BookingPageInput"&&!Text(Str(v,"title"),160))throw AvailKitException.Input("invalid_input");
        if(name=="RescheduleInput"&&(Instant(Str(v,"start"))*1000%1!=0||Instant(Str(v,"expectedStart"))*1000%1!=0))throw AvailKitException.Input("invalid_timestamp");
        if(name=="WebhookCreate") { if(!Uri.TryCreate(Str(v,"url"),UriKind.Absolute,out var u)||u.Scheme!="https"||u.Port!=443||u.HostNameType!=UriHostNameType.Dns||!u.Host.Contains('.')||u.UserInfo!=""||u.Query!=""||u.Fragment!="")throw AvailKitException.Input("invalid_webhook_url"); }
        if(name=="Hours")
        {
            var seen=new List<JsonElement>();foreach(var w in v.GetProperty("windows").EnumerateArray())
            {
                var a=Str(w,"start");var b=Str(w,"end");
                if(!Regex.IsMatch(a,"\\A(?:[01][0-9]|2[0-3]):[0-5][0-9]\\z")||!Regex.IsMatch(b,"\\A(?:(?:[01][0-9]|2[0-3]):[0-5][0-9]|24:00)\\z")||string.CompareOrdinal(a,b)>=0||seen.Any(p=>p.GetProperty("weekday").GetInt64()==w.GetProperty("weekday").GetInt64()&&string.CompareOrdinal(Str(p,"start"),b)<0&&string.CompareOrdinal(a,Str(p,"end"))<0))throw AvailKitException.Input("invalid_hours");seen.Add(w);
            }
        }
    }
    internal static string Str(JsonElement v,string p)=>v.GetProperty(p).GetString()!;
    internal static void Query(JsonElement route,Dictionary<string,string> q)
    {
        var allowed=new HashSet<string>();foreach(var p in route.GetProperty("parameters").EnumerateArray())
        {
            var n=Str(p,"name");allowed.Add(n);if(!q.TryGetValue(n,out var value)){if(p.TryGetProperty("required",out var req)&&req.ValueKind==JsonValueKind.True)throw AvailKitException.Input("invalid_query");continue;}
            var schema=p.GetProperty("schema");JsonElement val;
            try { val=Str(schema,"type")=="integer"?JsonSerializer.SerializeToElement(long.Parse(value,CultureInfo.InvariantCulture)):JsonSerializer.SerializeToElement(value); }
            catch { throw AvailKitException.Input("invalid_query"); }
            if(!Valid(val,schema))throw AvailKitException.Input("invalid_query");
            if(n=="after"&&!Str(route,"path").EndsWith("/events",StringComparison.Ordinal)||n=="offeringId")Id(value);
            if(n=="q"&&!Text(value,160))throw AvailKitException.Input("invalid_query");
        }
        if(q.Keys.Any(k=>!allowed.Contains(k)))throw AvailKitException.Input("invalid_query");
        if(q.TryGetValue("from",out var from)){var range=Instant(q["to"])-Instant(from);if(range<=0||range>604800)throw AvailKitException.Input("invalid_range");}
    }
    internal static void Relations(string op,string path,Dictionary<string,string> q,byte[]? bytes,JsonElement d,string scopeId)
    {
        var b=bytes is null?default:Parse(bytes);bool ok=true;
        if(op is "setHours" or "getHours"){try{Semantic(d,"Hours");}catch{throw new AvailKitException("protocol","invalid_response");}}
        if(d.TryGetProperty("items",out var items))
        {
            int limit=int.Parse(q.GetValueOrDefault("limit","25"),CultureInfo.InvariantCulture);string after=q.GetValueOrDefault("after","");ok=items.GetArrayLength()<=limit;
            foreach(var item in items.EnumerateArray()){var next=Str(item,"id");ok&=string.CompareOrdinal(next,after)>0;after=next;}
            var nextAfter=d.GetProperty("nextAfter");ok&=nextAfter.ValueKind==JsonValueKind.Null||items.GetArrayLength()==limit&&nextAfter.GetString()==after;
            if(op=="listWebhookDeliveries")foreach(var item in items.EnumerateArray())ok&=Str(item,"subscriptionId")==path.Split('/')[2];
        }
        if(new[]{"getResource","updateResource","archiveResource","getOffering","updateOffering","archiveOffering","getBooking","cancel","rescheduleBooking","getCustomer","replaceCustomer","archiveCustomer","eraseCustomer","disableWebhook","getPublicBooking","cancelPublicBooking","reschedulePublicBooking"}.Contains(op))ok&=Str(d,"id")==path.Split('/')[2];
        if(d.TryGetProperty("status",out _)&&d.TryGetProperty("start",out _))ok&=Instant(Str(d,"end"))>Instant(Str(d,"start"));
        if(op is "cancel" or "cancelPublicBooking")ok&=Str(d,"status")=="cancelled";
        if(op is "book" or "bookPublic" or "rescheduleBooking" or "reschedulePublicBooking")ok&=Str(d,"status")=="confirmed"&&Instant(Str(d,"start"))==Instant(Str(b,"start"));
        if(op=="book")ok&=Str(d,"offeringId")==Str(b,"offeringId")&&(d.TryGetProperty("customerId",out var dc)?dc.GetString():null)==(b.TryGetProperty("customerId",out var bc)?bc.GetString():null);
        if(op=="bookPublic")ok&=Str(d.GetProperty("offering"),"id")==Str(b,"offeringId");
        if(op.StartsWith("archive",StringComparison.Ordinal))ok&=d.GetProperty("archived").GetBoolean();
        if(op=="disableWebhook")ok&=!d.GetProperty("enabled").GetBoolean();
        if(op=="getPublicPage")ok&=Str(d,"id")==scopeId;
        if(d.TryGetProperty("page",out var page))ok&=Str(page,"id")==scopeId;
        if(op is "availability" or "publicAvailability")
        {
            decimal lo=Instant(q["from"]),hi=Instant(q["to"]),last=decimal.MinValue;foreach(var s in d.GetProperty("slots").EnumerateArray()){decimal a=Instant(Str(s,"start")),z=Instant(Str(s,"end"));ok&=a>=lo&&z<=hi&&a<z&&a>last;last=a;}
        }
        if(op=="getUsage")
        {
            var dt=DateTimeOffset.ParseExact(Str(d,"asOf")[..19]+"Z","yyyy-MM-dd'T'HH:mm:ss'Z'",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal);var start=new DateTimeOffset(dt.Year,dt.Month,1,0,0,0,TimeSpan.Zero);var period=d.GetProperty("period");
            ok&=Instant(Str(period,"start"))==start.ToUnixTimeSeconds()&&Instant(Str(period,"end"))==start.AddMonths(1).ToUnixTimeSeconds();foreach(var p in d.GetProperty("counters").EnumerateObject()){var c=p.Value;ok&=c.GetProperty("remaining").GetInt64()==Math.Max(0,c.GetProperty("limit").GetInt64()-c.GetProperty("used").GetInt64());}
        }
        if(op is "pruneHistory" or "pruneExpiredReplays") {long n=d.GetProperty("deleted").GetInt64(),limit=b.GetProperty("limit").GetInt64();ok&=n<=limit&&(!d.GetProperty("hasMore").GetBoolean()||n==limit);if(op=="pruneHistory")ok&=Instant(Str(d,"asOf"))-Instant(Str(d,"cutoff"))==2592000;}
        if(op=="events")
        {
            long after=long.Parse(q.GetValueOrDefault("after","0"),CultureInfo.InvariantCulture),floor=d.GetProperty("retentionFloor").GetInt64(),high=d.GetProperty("highWater").GetInt64(),prev=Math.Max(after,floor);var evs=d.GetProperty("events");ok&=floor<=high&&evs.GetArrayLength()<=int.Parse(q.GetValueOrDefault("limit","100"),CultureInfo.InvariantCulture);
            foreach(var e in evs.EnumerateArray()){long n=e.GetProperty("sequence").GetInt64();ok&=n>prev&&n<=high;prev=n;}ok&=d.GetProperty("nextAfter").GetInt64()==prev&&prev<=Math.Max(after,high);
        }
        if(!ok)throw new AvailKitException("protocol","invalid_response");
    }
}
