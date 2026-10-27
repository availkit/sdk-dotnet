
using System.Security.Cryptography;
namespace AvailKit;
public static class Secrets
{
    public static string NewIdempotencyKey()=>Random(24);
    public static string NewBookingToken()=>Random(32);
    public static string NewContactToken()=>Random(32);
    private static string Random(int n)=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(n)).TrimEnd('=').Replace('+','-').Replace('/','_');
}
