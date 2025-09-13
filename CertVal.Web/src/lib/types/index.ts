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
    certificateCount: number;
    memberCount: number;
    createdAt: string;
    updatedAt: string;
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
    isBundle: boolean;
    parentCertificateId?: string;
    status: string;
    isExpired: boolean;
    daysUntilExpiry: number;
    childCertificates: Certificate[];
    createdAt: string;
    updatedAt: string;
}

export interface ApiResponse<T> {
    data?: T;
    message?: string;
    errors?: Record<string, string>;
}

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
}

export interface InviteMemberRequest {
    request: {
        email: string;
        role: WorkspaceRole;
    };
}

export interface DirectInviteMemberRequest {
    email: string;
    role: WorkspaceRole;
}

export type WorkspaceRole = 'Viewer' | 'Editor' | 'Administrator';

export interface WorkspaceMember {
    id: string;
    userId: string;
    workspaceId: string;
    role: WorkspaceRole;
    status: 'Pending' | 'Active' | 'Inactive';
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

export type Language = 'uk' | 'en';

export type Theme = 'light' | 'dark' | 'system';

export interface ThemeState {
    theme: Theme;
    resolved: 'light' | 'dark';
}