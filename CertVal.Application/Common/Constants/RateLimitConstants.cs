namespace CertVal.Application.Common.Constants;

public static class RateLimitConstants
{
    public static class ConfigurationKeys
    {
        public const string PasswordResetMinutes = "RateLimits:PasswordResetMinutes";
        public const string InviteMemberMinutes = "RateLimits:InviteMemberMinutes";
    }

    public static class CacheKeys
    {
        public const string PasswordResetPrefix = "password_reset";
        public const string InviteMemberPrefix = "invite_member";
    }
}
