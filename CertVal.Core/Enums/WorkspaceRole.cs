namespace CertVal.Core.Enums;

public enum WorkspaceRole
{
    Viewer = 1,    // Can only view certificates
    Editor = 2,    // Can manage certificates but not workspace settings
    Admin = 3      // Full access to workspace
}
