
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace AvailKit;
public static class Webhooks
{
    /// <summary>Authenticate exact raw bytes first. Transactionally deduplicate AvailKit-Delivery-ID after verification.</summary>
    public static bool Verify(byte[] body,string signature,string secret,long? nowEpochSeconds=null,int toleranceSeconds=300)
    {
        long now=nowEpochSeconds??DateTimeOffset.UtcNow.ToUnixTimeSeconds();if(now<0||toleranceSeconds<0||toleranceSeconds>3600)throw AvailKitException.Input("invalid_webhook_tolerance");
        try
        {
            if(body is null||body.Length>16384||signature is null||!Regex.IsMatch(signature,"\\At=(0|[1-9][0-9]{0,19}),v1=[0-9a-f]{64}\\z"))return false;Protocol.Token(secret);var stamp=signature[2..signature.IndexOf(',')];if(System.Numerics.BigInteger.Abs(now-System.Numerics.BigInteger.Parse(stamp,CultureInfo.InvariantCulture))>toleranceSeconds)return false;
            var key=Convert.FromBase64String(secret.Replace('-','+').Replace('_','/')+"=");var prefix=Encoding.ASCII.GetBytes(stamp+".");var message=new byte[prefix.Length+body.Length];prefix.CopyTo(message,0);body.CopyTo(message,prefix.Length);var expected=HMACSHA256.HashData(key,message);var claimed=Convert.FromHexString(signature[(signature.IndexOf("v1=",StringComparison.Ordinal)+3)..]);return CryptographicOperations.FixedTimeEquals(expected,claimed);
        }
        catch {return false;}
    }
}
