
using System.Globalization;
using System.Text;
using System.Text.Json;
namespace AvailKit;
/// <summary>Immutable exact private intent; importing/exporting never sends. Persist before the initial send.</summary>
public sealed class RequestIntent
{
    internal string Operation { get; } internal string Path { get; } internal string Key { get; } internal string Scope { get; } internal string CreatedAt { get; }
    private readonly byte[] bytes;internal byte[] Body=>bytes.ToArray();internal IReadOnlyDictionary<string,string> Headers { get; }
    internal RequestIntent(string operation,string path,byte[] body,string key,string scope,Dictionary<string,string> headers,string createdAt){Operation=operation;Path=path;bytes=body.ToArray();Key=key;Scope=scope;Headers=new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>(new Dictionary<string,string>(headers));CreatedAt=createdAt;}
    public byte[] ExportPrivate()=>JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string,object>{{"version",1},{"operation",Operation},{"path",Path},{"body",Encoding.UTF8.GetString(bytes)},{"idempotencyKey",Key},{"scope",Scope},{"headers",Headers},{"createdAt",CreatedAt}},Protocol.Json);
    public static RequestIntent ImportPrivate(byte[] raw)
    {
        try
        {
            if(raw is null||raw.Length>131072)throw new Exception();var m=Protocol.Parse(raw);var names=m.EnumerateObject().Select(p=>p.Name).ToHashSet();if(!names.SetEquals(new[]{"version","operation","path","body","idempotencyKey","scope","headers","createdAt"})||!m.GetProperty("version").TryGetInt32(out var v)||v!=1)throw new Exception();
            foreach(var k in new[]{"operation","path","body","idempotencyKey","scope","createdAt"})if(m.GetProperty(k).ValueKind!=JsonValueKind.String)throw new Exception();var body=Encoding.UTF8.GetBytes(Protocol.Str(m,"body"));if(body.Length>16384)throw new Exception();Protocol.Key(Protocol.Str(m,"idempotencyKey"));Protocol.Instant(Protocol.Str(m,"createdAt"));
            var h=new Dictionary<string,string>();foreach(var p in m.GetProperty("headers").EnumerateObject()){if(p.Value.ValueKind!=JsonValueKind.String)throw new Exception();h.Add(p.Name,p.Value.GetString()!);}return new(Protocol.Str(m,"operation"),Protocol.Str(m,"path"),body,Protocol.Str(m,"idempotencyKey"),Protocol.Str(m,"scope"),h,Protocol.Str(m,"createdAt"));
        }
        catch {throw AvailKitException.Input("invalid_intent");}
    }
    public override string ToString()=>"AvailKit.RequestIntent [private]";
}
