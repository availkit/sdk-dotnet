
namespace AvailKit;
/// <summary>Finite, credential-free diagnostics. Raw requests, URLs, bodies and nested causes are never retained.</summary>
public sealed class AvailKitException : Exception
{
    public string Kind { get; }
    public string Code { get; }
    public int? Status { get; }
    public bool Uncertain { get; }
    internal AvailKitException(string kind, string code, int? status = null, bool uncertain = false)
        : base($"AvailKit {kind}: {code}") { Kind=kind; Code=code; Status=status; Uncertain=uncertain; }
    internal static AvailKitException Input(string code) => new("input",code);
    public IReadOnlyDictionary<string,object?> Diagnostics => new Dictionary<string,object?> { ["kind"]=Kind,["code"]=Code,["status"]=Status,["uncertain"]=Uncertain };
    public override string ToString() => Message;
}
