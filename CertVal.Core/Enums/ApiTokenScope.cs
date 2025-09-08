namespace CertVal.Core.Enums;

public enum ApiTokenScope
{
    ReadOnly = 1,      // Can only read data
    ReadWrite = 2,     // Can read and modify data
    Admin = 3          // Full access including workspace management
}