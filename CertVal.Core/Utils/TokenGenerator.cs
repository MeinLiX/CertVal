namespace CertVal.Core.Utils;

public static class TokenGenerator
{
    public static string GenerateUrlSafeToken(int length = 32)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var token = new char[length];
        
        for (int i = 0; i < token.Length; i++)
        {
            token[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(token);
    }
}