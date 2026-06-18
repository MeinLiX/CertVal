export interface User {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    fullName: string;
    isEmailConfirmed: boolean;
    lastLoginAt: string;
    status: string;
    timeZone?: string;
    language?: string;
    emailNotificationsEnabled: boolean;
    createdAt: string;
}

export interface Workspace {
    id: string;
    name: string;
    description?: string;
    ownerId: string;
    owner: User;
    maxCertificates: number;
    isPublic: boolean;
    allowMemberInvites: boolean;
    autoDeleteExpiredCertificates: boolean;
    ocspMonitoringEnabled: boolean;
    certificateCount: number;
    memberCount: number;
    createdAt: string;
    updatedAt: string;
}

export interface UpdateWorkspaceRequest {
    name: string;
    description?: string;
    maxCertificates: number;
    isPublic: boolean;
    allowMemberInvites: boolean;
    autoDeleteExpiredCertificates: boolean;
    ocspMonitoringEnabled: boolean;
}

export interface Certificate {
    id: string;
    workspaceId: string;
    subject: string;
    issuer: string;
    serialNumber?: string;
    thumbprint: string;
    notBefore: string;
    notAfter: string;
    originalFileName: string;
    fileFormat: string;
    fileSize: number;
    isSkipped: boolean;
    tags?: string[];
    previousCertificateId?: string;
    nextCertificateId?: string;
    isBundle: boolean;
    parentCertificateId?: string;
    status: string;
    isExpired: boolean;
    daysUntilExpiry: number;
    ocspStatus: OcspStatus;
    ocspLastCheckedAt?: string;
    ocspResponderUrl?: string;
    ocspRevocationReason?: string;
    ocspRevokedAt?: string;
    childCertificates: Certificate[];
    createdAt: string;
    updatedAt: string;
}

export type OcspStatus =
    | 'NotChecked'
    | 'NotConfigured'
    | 'CheckFailed'
    | 'Good'
    | 'Revoked';

export interface ApiResponse<T> {
    data?: T;
    message?: string;
}

export interface ErrorResponseDto {
    message: string;
    errors?: Record<string, string[]>;
}

export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    instance?: string;
    errors?: Record<string, string[] | string>;
    traceId?: string;
};

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface GoogleLoginRequest {
    idToken: string;
}

export interface LoginResponse {
    token: string;
    expiresAt: string;
    user: User;
}

export interface CreateUserRequest {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    timeZone?: string;
    language?: string;
}

export interface CreateWorkspaceRequest {
    name: string;
    description?: string;
    maxCertificates: number;
    isPublic: boolean;
    allowMemberInvites: boolean;
    autoDeleteExpiredCertificates: boolean;
}

export interface InviteMemberRequest {
    email: string;
    role: WorkspaceRole;
}

export type WorkspaceRole = 'Viewer' | 'Editor' | 'Admin' | 'Owner';
export type WorkspaceMemberStatus = 'Invited' | 'Active' | 'Inactive';
export interface WorkspaceMember {
    id: string;
    userId: string;
    workspaceId: string;
    role: WorkspaceRole;
    status: WorkspaceMemberStatus;
    user: User;
    joinedAt?: string;
    createdAt: string;
    updatedAt: string;
}

export interface DashboardStats {
    totalWorkspaces: number;
    totalCertificates: number;
    expiredCertificates: number;
    expiringIn7Days: number;
    expiringIn30Days: number;
    validCertificates: number;
}

export interface BulkUploadResultDto {
    totalFiles: number;
    successCount: number;
    skippedCount: number;
    failureCount: number;
    results: BulkUploadItemResult[];
    summary: string;
}

export interface BulkUploadItemResult {
    fileName: string;
    success: boolean;
    isSkipped: boolean;
    certificateId?: string;
    subject?: string;
    errorMessage?: string;
}

export interface NotificationRule {
    id: string;
    workspaceId: string;
    name: string;
    isEnabled: boolean;
    daysBeforeExpiry: number;
    frequency: string;
    channelType: string;
    channelConfig: string;
    recipientAggregationMode: 'Individual' | 'SingleEmailToAll';
    createdAt: string;
    updatedAt: string;
}

export interface NotificationHistory {
    id: string;
    notificationRuleId: string;
    certificateId: string;
    status: string;
    channelType: string;
    recipient: string;
    subject: string;
    message: string;
    scheduledAt: string;
    sentAt?: string;
    deliveredAt?: string;
    errorMessage?: string;
    retryCount: number;
    createdAt: string;
}

export interface CreateNotificationRuleRequest {
    name: string;
    daysBeforeExpiry: number;
    channelType: 'Email' | 'Webhook' | 'Slack' | 'Telegram';
    channelConfig: string;
    frequency: 'Once' | 'Daily' | 'Weekly' | 'Monthly';
    recipientAggregationMode?: 'Individual' | 'SingleEmailToAll';
}
export interface UpdateUserRequest {
    firstName: string;
    lastName: string;
    timeZone?: string;
    language?: string;
    emailNotificationsEnabled: boolean;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface TransferOwnershipRequest {
    newOwnerEmail: string;
}

export interface ResetPasswordRequest {
    token: string;
    newPassword: string;
}

export interface ApiToken {
    id: string;
    name: string;
    tokenPrefix: string;
    scope: string;
    isActive: boolean;
    lastUsedAt?: string;
    expiresAt?: string;
    createdAt: string;
}

export interface CreateApiTokenRequest {
    name: string;
    scope: 'ReadOnly' | 'ReadWrite';
    expiresAt?: string;
}

export interface CreateApiTokenResponse {
    id: string;
    name: string;
    token: string;
    tokenPrefix: string;
    scope: string;
    expiresAt?: string;
    createdAt: string;
}

export type Language = 'uk' | 'en';

export type Theme = 'light' | 'dark';

export interface ThemeState {
    theme: Theme;
    resolved: 'light' | 'dark';
}

export interface SslCertInfo {
    subject: string;
    issuer: string;
    serialNumber: string;
    notBefore: string;
    notAfter: string;
    daysRemaining: number;
    isExpired: boolean;
    subjectAltNames: string[];
    sha256Thumbprint: string;
    signatureAlgorithm: string;
    publicKey: string;
    publicKeyAlgorithm: string;
    publicKeyBits: number;
}

export interface TlsFinding {
    severity: 'info' | 'warning' | 'blocking';
    message: string;
}

export interface SslCheckResult {
    host: string;
    port: number;
    reachable: boolean;
    error?: string;
    negotiatedProtocol?: string;
    hostnameMatches?: boolean;
    chainTrusted?: boolean;
    grade?: string;
    findings: TlsFinding[];
    leaf?: SslCertInfo;
    chain: SslCertInfo[];
}

export interface AuditLogEntry {
    id: number;
    eventType: string;
    category: string;
    description: string;
    aggregateId?: string;
    occurredAt: string;
}