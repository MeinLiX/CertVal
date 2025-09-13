<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, formatDateTime, getCertificateStatus } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import type { Workspace, Certificate, PagedResult } from '$lib/types';

	interface WorkspaceMember {
		id: string;
		userId: string;
		user: {
			id: string;
			email: string;
			firstName: string;
			lastName: string;
			fullName: string;
		};
		role: string;
		status: string;
		joinedAt?: string;
		createdAt: string;
	}

	interface UpdateWorkspaceRequest {
		name: string;
		description?: string;
		maxCertificates: number;
		isPublic: boolean;
		allowMemberInvites: boolean;
	}

	interface InviteMemberRequest {
		email: string;
		role: 'Viewer' | 'Editor' | 'Administrator'; // Using the correct enum values
	}

	let workspace = $state<Workspace | null>(null);
	let certificates = $state<Certificate[]>([]);
	let members = $state<WorkspaceMember[]>([]);
	let isLoading = $state(true);
	let isLoadingCertificates = $state(false);
	let isLoadingMembers = $state(false);
	let error = $state<string>('');

	// Modals
	let showEditModal = $state(false);
	let showDeleteModal = $state(false);
	let showInviteModal = $state(false);
	let isUpdating = $state(false);
	let isDeleting = $state(false);
	let isInviting = $state(false);
	let confirmName = $state('');

	// Forms
	let updateForm = $state<UpdateWorkspaceRequest>({
		name: '',
		description: '',
		maxCertificates: 1000,
		isPublic: false,
		allowMemberInvites: true
	});

	let inviteForm = $state<InviteMemberRequest>({
		email: '',
		role: 'Viewer'
	});

	let errors = $state<Record<string, string>>({});

	const workspaceId = $derived($page.params.id);
	const canManage = $derived(workspace && $auth.user && workspace.ownerId === $auth.user.id);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		await Promise.all([loadWorkspace(), loadCertificates(), loadMembers()]);
	});

	async function loadWorkspace() {
		try {
			const response = await api.get<Workspace>(`/v1/workspaces/${workspaceId}`);
			if (response.data) {
				workspace = response.data;
				// Initialize update form
				updateForm = {
					name: workspace.name,
					description: workspace.description || '',
					maxCertificates: workspace.maxCertificates,
					isPublic: workspace.isPublic,
					allowMemberInvites: workspace.allowMemberInvites
				};
			} else if (response.message) {
				error = response.message;
			}
		} catch (err) {
			error = 'Failed to load workspace';
			console.error('Failed to load workspace:', err);
		} finally {
			isLoading = false;
		}
	}

	async function loadCertificates() {
		if (!workspaceId) return;

		isLoadingCertificates = true;
		try {
			const response = await api.get<PagedResult<Certificate>>(
				`/v1/certificates?workspaceId=${workspaceId}&pageSize=10&sortBy=notAfter`
			);
			if (response.data) {
				certificates = response.data.items;
			}
		} catch (err) {
			console.error('Failed to load certificates:', err);
		} finally {
			isLoadingCertificates = false;
		}
	}

	async function loadMembers() {
		if (!workspaceId) return;

		isLoadingMembers = true;
		try {
			const response = await api.get<WorkspaceMember[]>(`/v1/workspaces/${workspaceId}/members`);
			if (response.data) {
				members = response.data;
			}
		} catch (err) {
			console.error('Failed to load members:', err);
		} finally {
			isLoadingMembers = false;
		}
	}

	async function handleUpdate(event: Event) {
		event.preventDefault();
		errors = {};
		isUpdating = true;

		try {
			const response = await api.put<Workspace>(`/v1/workspaces/${workspaceId}`, updateForm);
			if (response.data) {
				workspace = response.data;
				showEditModal = false;
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (err) {
			errors.general = t('errors.network', $language);
		} finally {
			isUpdating = false;
		}
	}

	async function handleDelete() {
		isDeleting = true;
		try {
			await api.delete(`/v1/workspaces/${workspaceId}`);
			goto('/workspaces');
		} catch (err) {
			error = 'Failed to delete workspace';
		} finally {
			isDeleting = false;
			showDeleteModal = false;
			confirmName = '';
		}
	}

	async function handleInvite(event: Event) {
		event.preventDefault();
		errors = {};
		isInviting = true;

		try {
			console.log('Inviting member:', inviteForm);

			const response = await api.inviteMember<WorkspaceMember>(
				workspaceId,
				inviteForm.email,
				inviteForm.role
			);

			if (response.data) {
				members = [...members, response.data];
				showInviteModal = false;
				inviteForm = { email: '', role: 'Viewer' };

				console.log('Member invited successfully');
			} else if (response.message) {
				if (
					response.message.includes('already exists') ||
					response.message.includes('already a member')
				) {
					errors.email = 'This user is already a member of this workspace';
				} else if (
					response.message.includes('not found') ||
					response.message.includes('does not exist')
				) {
					errors.email = 'User with this email address was not found';
				} else if (response.message.includes('role')) {
					errors.role = 'Invalid role specified';
				} else {
					errors.general = response.message;
				}
			}
		} catch (err) {
			console.error('Invite error:', err);
			errors.general = 'Failed to send invitation. Please try again.';
		} finally {
			isInviting = false;
		}
	}

	function getStatusColor(status: 'expired' | 'expiring' | 'valid'): string {
		switch (status) {
			case 'expired':
				return 'text-red-600 bg-red-100';
			case 'expiring':
				return 'text-yellow-600 bg-yellow-100';
			case 'valid':
				return 'text-green-600 bg-green-100';
		}
	}

	function getStatusText(status: 'expired' | 'expiring' | 'valid'): string {
		return t(`certificates.${status}`, $language);
	}

	function getRoleColor(role: string): string {
		switch (role.toLowerCase()) {
			case 'admin':
			case 'administrator':
				return 'text-purple-600 bg-purple-100';
			case 'editor':
			case 'manager':
				return 'text-blue-600 bg-blue-100';
			case 'viewer':
			default:
				return 'text-gray-600 bg-gray-100';
		}
	}

	function formatFileSize(bytes: number): string {
		if (bytes === 0) return '0 B';
		const k = 1024;
		const sizes = ['B', 'KB', 'MB', 'GB'];
		const i = Math.floor(Math.log(bytes) / Math.log(k));
		return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
	}

	function handleDescriptionChange(event: Event) {
		const target = event.target as HTMLTextAreaElement;
		updateForm.description = target.value;
	}

	// Stats
	const totalCertificates = $derived(workspace?.certificateCount || 0);
	const expiredCertificates = $derived(
		certificates.filter((c) => getCertificateStatus(c.notAfter) === 'expired').length
	);
	const expiringCertificates = $derived(
		certificates.filter((c) => getCertificateStatus(c.notAfter) === 'expiring').length
	);
	const validCertificates = $derived(
		certificates.filter((c) => getCertificateStatus(c.notAfter) === 'valid').length
	);
</script>

<svelte:head>
	<title>
		{workspace
			? `${workspace.name} - ${t('workspaces.details', $language)}`
			: t('workspaces.details', $language)}
	</title>
</svelte:head>

<div class="space-y-6">
	{#if isLoading}
		<div class="flex h-64 items-center justify-center">
			<div class="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
		</div>
	{:else if error}
		<Card>
			<div class="py-12 text-center">
				<svg
					class="mx-auto h-12 w-12 text-red-400"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
				>
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
					/>
				</svg>
				<h3 class="mt-2 text-sm font-medium text-gray-900">Error Loading Workspace</h3>
				<p class="mt-1 text-sm text-gray-500">{error}</p>
				<div class="mt-6">
					<Button onclick={() => goto('/workspaces')}>
						{t('common.back', $language)} to {t('workspaces.title', $language)}
					</Button>
				</div>
			</div>
		</Card>
	{:else if workspace}
		<!-- Header -->
		<div class="flex items-start justify-between">
			<div class="min-w-0 flex-1">
				<div class="flex items-center space-x-3">
					<Button variant="outline" size="sm" onclick={() => history.back()}>
						<svg class="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M10 19l-7-7m0 0l7-7m-7 7h18"
							/>
						</svg>
						{t('common.back', $language)}
					</Button>
					<div class="flex items-center space-x-2">
						<span
							class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium {workspace.isPublic
								? 'bg-green-100 text-green-800'
								: 'bg-gray-100 text-gray-800'}"
						>
							{workspace.isPublic ? t('common.public', $language) : t('common.private', $language)}
						</span>
						{#if workspace.allowMemberInvites}
							<span
								class="inline-flex items-center rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-800"
							>
								{t('workspaces.invitesAllowed', $language)}
							</span>
						{/if}
					</div>
				</div>
				<h1 class="mt-2 text-2xl font-bold text-gray-900">
					{workspace.name}
				</h1>
				{#if workspace.description}
					<p class="mt-1 text-gray-600">{workspace.description}</p>
				{/if}
				<div class="mt-2 flex items-center space-x-4 text-sm text-gray-500">
					<span>{t('common.owner', $language)}: {workspace.owner.fullName}</span>
					<span>•</span>
					<span>{t('workspaces.created', $language)}: {formatDate(workspace.createdAt)}</span>
					{#if workspace.updatedAt !== workspace.createdAt}
						<span>•</span>
						<span>{t('workspaces.updated', $language)}: {formatDate(workspace.updatedAt)}</span>
					{/if}
				</div>
			</div>
		</div>

		<!-- Actions Bar -->
		<Card>
			<div class="flex items-center justify-between">
				<div class="flex items-center space-x-4 text-sm text-gray-600">
					<div class="flex items-center">
						<svg
							class="mr-1 h-4 w-4 text-gray-400"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z"
							/>
						</svg>
						<span
							>{workspace.memberCount}
							{workspace.memberCount === 1
								? t('common.member', $language)
								: t('common.members', $language)}</span
						>
					</div>
					<div class="flex items-center">
						<svg
							class="mr-1 h-4 w-4 text-gray-400"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
							/>
						</svg>
						<span
							>{workspace.certificateCount}
							{workspace.certificateCount === 1
								? t('workspaces.certificates', $language).slice(0, -1)
								: t('workspaces.certificates', $language)}</span
						>
					</div>
					<div class="flex items-center">
						<svg
							class="mr-1 h-4 w-4 text-gray-400"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M13 10V3L4 14h7v7l9-11h-7z"
							/>
						</svg>
						<span
							>{t('common.limit', $language)}: {workspace.maxCertificates}
							{t('workspaces.certificates', $language)}</span
						>
					</div>
				</div>

				<div class="flex items-center space-x-2">
					<Button variant="outline" onclick={() => goto(`/certificates?workspace=${workspaceId}`)}>
						<svg class="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
							/>
						</svg>
						{t('certificates.title', $language)}
					</Button>

					<Button variant="outline" onclick={() => goto(`/notifications?workspace=${workspaceId}`)}>
						<svg class="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
							/>
						</svg>
						{t('notifications.title', $language)}
					</Button>

					{#if canManage}
						<Modal
							isOpen={showInviteModal}
							title={t('workspaces.inviteMember', $language)}
							onClose={() => (showInviteModal = false)}
						>
							<form onsubmit={handleInvite} class="space-y-4">
								{#if errors.general}
									<div class="rounded-md border border-red-200 bg-red-50 p-4">
										<div class="flex">
											<svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
												<path
													fill-rule="evenodd"
													d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
													clip-rule="evenodd"
												/>
											</svg>
											<div class="ml-3">
												<p class="text-sm text-red-600">{errors.general}</p>
											</div>
										</div>
									</div>
								{/if}

								<Input
                                    id="invite-email"
									type="email"
									label={t('workspaces.emailAddress', $language)}
									bind:value={inviteForm.email}
									required
									error={errors.email}
									placeholder="member@example.com"
								/>

								<div class="space-y-1">
									<label for="invite-role" class="block text-sm font-medium text-gray-700">
										{t('common.role', $language)} <span class="text-red-500">*</span>
									</label>
									<select
										id="invite-role"
										bind:value={inviteForm.role}
										required
										class="block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
									>
										<option value="Viewer">{t('workspaces.roles.viewer', $language)}</option>
										<option value="Editor">{t('workspaces.roles.editor', $language)}</option>
										<option value="Administrator">{t('workspaces.roles.admin', $language)}</option>
									</select>
								</div>

								<div class="rounded-md border border-blue-200 bg-blue-50 p-4">
									<div class="flex">
										<svg class="h-5 w-5 text-blue-400" viewBox="0 0 20 20" fill="currentColor">
											<path
												fill-rule="evenodd"
												d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
												clip-rule="evenodd"
											/>
										</svg>
										<div class="ml-3">
											<p class="text-sm text-blue-700">
												{t('workspaces.invitationNote', $language)}
											</p>
										</div>
									</div>
								</div>

								<div class="flex justify-end space-x-3 pt-4">
									<Button variant="outline" onclick={() => (showInviteModal = false)} type="button">
										{t('common.cancel', $language)}
									</Button>
									<Button type="submit" loading={isInviting}>
										{t('workspaces.sendInvitation', $language)}
									</Button>
								</div>
							</form>
						</Modal>
					{/if}
				</div>
			</div>
		</Card>

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<!-- Main Content -->
			<div class="space-y-6 lg:col-span-2">
				<!-- Certificate Stats -->
				<Card title={t('workspaces.certificateOverview', $language)}>
					<div class="grid grid-cols-2 gap-4 md:grid-cols-4">
						<div class="text-center">
							<div class="text-2xl font-bold text-gray-900">{totalCertificates}</div>
							<div class="text-sm text-gray-500">{t('common.total', $language)}</div>
						</div>
						<div class="text-center">
							<div class="text-2xl font-bold text-green-600">{validCertificates}</div>
							<div class="text-sm text-gray-500">{t('certificates.valid', $language)}</div>
						</div>
						<div class="text-center">
							<div class="text-2xl font-bold text-yellow-600">{expiringCertificates}</div>
							<div class="text-sm text-gray-500">{t('certificates.expiring', $language)}</div>
						</div>
						<div class="text-center">
							<div class="text-2xl font-bold text-red-600">{expiredCertificates}</div>
							<div class="text-sm text-gray-500">{t('certificates.expired', $language)}</div>
						</div>
					</div>
				</Card>

				<!-- Recent Certificates -->
				<Card title={t('workspaces.recentCertificates', $language)}>
					{#if isLoadingCertificates}
						<div class="flex h-32 items-center justify-center">
							<div class="h-6 w-6 animate-spin rounded-full border-b-2 border-blue-600"></div>
						</div>
					{:else if certificates.length === 0}
						<div class="py-8 text-center">
							<svg
								class="mx-auto h-8 w-8 text-gray-400"
								fill="none"
								viewBox="0 0 24 24"
								stroke="currentColor"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
								/>
							</svg>
							<p class="mt-2 text-sm text-gray-500">
								{t('workspaces.noCertificatesInWorkspace', $language)}
							</p>
							<Button
								variant="outline"
								class="mt-4"
								onclick={() => goto(`/certificates?workspace=${workspaceId}`)}
							>
								{t('certificates.upload', $language)}
							</Button>
						</div>
					{:else}
						<div class="space-y-3">
							{#each certificates as certificate}
								{@const status = getCertificateStatus(certificate.notAfter)}
								<div
									class="flex items-center justify-between rounded-lg border border-gray-200 p-4 hover:bg-gray-50"
								>
									<div class="flex items-center space-x-4">
										{#if certificate.isBundle}
											<svg
												class="h-5 w-5 text-blue-500"
												fill="none"
												viewBox="0 0 24 24"
												stroke="currentColor"
											>
												<path
													stroke-linecap="round"
													stroke-linejoin="round"
													stroke-width="2"
													d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6"
												/>
											</svg>
										{:else}
											<svg
												class="h-5 w-5 text-gray-400"
												fill="none"
												viewBox="0 0 24 24"
												stroke="currentColor"
											>
												<path
													stroke-linecap="round"
													stroke-linejoin="round"
													stroke-width="2"
													d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
												/>
											</svg>
										{/if}
										<div class="min-w-0 flex-1">
											<p
												class="truncate text-sm font-medium text-gray-900"
												title={certificate.subject}
											>
												{certificate.subject}
											</p>
											<div class="flex items-center space-x-2 text-xs text-gray-500">
												<span>{certificate.originalFileName}</span>
												<span>•</span>
												<span>{certificate.fileFormat}</span>
												<span>•</span>
												<span>{formatFileSize(certificate.fileSize)}</span>
											</div>
										</div>
									</div>
									<div class="flex items-center space-x-3">
										<div class="text-right">
											<p class="text-sm text-gray-900">{formatDate(certificate.notAfter)}</p>
											<span
												class="inline-flex rounded-full px-2 py-1 text-xs font-semibold {getStatusColor(
													status
												)}"
											>
												{getStatusText(status)}
											</span>
										</div>
										<Button
											variant="outline"
											size="sm"
											onclick={() => goto(`/certificates/${certificate.id}`)}
										>
											{t('common.view', $language)}
										</Button>
									</div>
								</div>
							{/each}
						</div>
						{#if totalCertificates > certificates.length}
							<div class="mt-4 text-center">
								<Button
									variant="outline"
									onclick={() => goto(`/certificates?workspace=${workspaceId}`)}
								>
									{t('dashboard.viewAll', $language)}
									{totalCertificates}
									{t('workspaces.certificates', $language)}
								</Button>
							</div>
						{/if}
					{/if}
				</Card>

				<!-- Workspace Members -->
				<Card title={t('common.members', $language)}>
					{#if isLoadingMembers}
						<div class="flex h-32 items-center justify-center">
							<div class="h-6 w-6 animate-spin rounded-full border-b-2 border-blue-600"></div>
						</div>
					{:else}
						<div class="space-y-3">
							<!-- Owner -->
							<div class="flex items-center justify-between rounded-lg border border-gray-200 p-4">
								<div class="flex items-center space-x-3">
									<div
										class="flex h-8 w-8 items-center justify-center rounded-full bg-purple-100 text-sm font-medium text-purple-600"
									>
										{workspace.owner.firstName.charAt(0)}{workspace.owner.lastName.charAt(0)}
									</div>
									<div>
										<p class="text-sm font-medium text-gray-900">{workspace.owner.fullName}</p>
										<p class="text-xs text-gray-500">{workspace.owner.email}</p>
									</div>
								</div>
								<div class="flex items-center space-x-3">
									<span
										class="inline-flex rounded-full bg-purple-100 px-2 py-1 text-xs font-semibold text-purple-600"
									>
										{t('common.owner', $language)}
									</span>
									<span class="text-xs text-gray-500">
										{t('common.since', $language)}
										{formatDate(workspace.createdAt)}
									</span>
								</div>
							</div>

							<!-- Members -->
							{#each members as member}
								<div
									class="flex items-center justify-between rounded-lg border border-gray-200 p-4"
								>
									<div class="flex items-center space-x-3">
										<div
											class="flex h-8 w-8 items-center justify-center rounded-full bg-gray-100 text-sm font-medium text-gray-600"
										>
											{member.user.firstName.charAt(0)}{member.user.lastName.charAt(0)}
										</div>
										<div>
											<p class="text-sm font-medium text-gray-900">{member.user.fullName}</p>
											<p class="text-xs text-gray-500">{member.user.email}</p>
										</div>
									</div>
									<div class="flex items-center space-x-3">
										<span
											class="inline-flex rounded-full px-2 py-1 text-xs font-semibold {getRoleColor(
												member.role
											)}"
										>
											{member.role}
										</span>
										<span class="text-xs text-gray-500">
											{member.joinedAt
												? `${t('common.joined', $language)} ${formatDate(member.joinedAt)}`
												: `${t('common.invited', $language)} ${formatDate(member.createdAt)}`}
										</span>
									</div>
								</div>
							{/each}

							{#if members.length === 0}
								<div class="py-8 text-center">
									<svg
										class="mx-auto h-8 w-8 text-gray-400"
										fill="none"
										viewBox="0 0 24 24"
										stroke="currentColor"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z"
										/>
									</svg>
									<p class="mt-2 text-sm text-gray-500">
										{t('workspaces.noAdditionalMembers', $language)}
									</p>
									{#if canManage && workspace.allowMemberInvites}
										<Button variant="outline" class="mt-4" onclick={() => (showInviteModal = true)}>
											{t('workspaces.inviteFirstMember', $language)}
										</Button>
									{/if}
								</div>
							{/if}
						</div>
					{/if}
				</Card>
			</div>

			<!-- Sidebar -->
			<div class="space-y-6">
				<!-- Workspace Details -->
				<Card title={t('workspaces.details', $language)}>
					<dl class="space-y-4">
						<div>
							<dt class="text-sm font-medium text-gray-500">{t('common.status', $language)}</dt>
							<dd class="mt-1 text-sm text-gray-900">
								{workspace.isPublic
									? t('common.public', $language)
									: t('common.private', $language)}
							</dd>
						</div>

						<div>
							<dt class="text-sm font-medium text-gray-500">
								{t('workspaces.certificateLimit', $language)}
							</dt>
							<dd class="mt-1 text-sm text-gray-900">
								{workspace.certificateCount} / {workspace.maxCertificates}
								<div class="mt-1 h-2 w-full rounded-full bg-gray-200">
									<div
										class="h-2 rounded-full bg-blue-600"
										style="width: {Math.min(
											(workspace.certificateCount / workspace.maxCertificates) * 100,
											100
										)}%"
									></div>
								</div>
							</dd>
						</div>

						<div>
							<dt class="text-sm font-medium text-gray-500">
								{t('workspaces.memberInvites', $language)}
							</dt>
							<dd class="mt-1 text-sm text-gray-900">
								{workspace.allowMemberInvites
									? t('common.allowed', $language)
									: t('common.disabled', $language)}
							</dd>
						</div>

						<div>
							<dt class="text-sm font-medium text-gray-500">
								{t('workspaces.created', $language)}
							</dt>
							<dd class="mt-1 text-sm text-gray-900">
								{formatDateTime(workspace.createdAt)}
							</dd>
						</div>

						{#if workspace.updatedAt !== workspace.createdAt}
							<div>
								<dt class="text-sm font-medium text-gray-500">{t('common.update', $language)}</dt>
								<dd class="mt-1 text-sm text-gray-900">
									{formatDateTime(workspace.updatedAt)}
								</dd>
							</div>
						{/if}
					</dl>
				</Card>

				<!-- Quick Actions -->
				<Card title={t('common.quickActions', $language)}>
					<div class="space-y-3">
						<Button
							variant="outline"
							class="w-full"
							onclick={() => goto(`/certificates?workspace=${workspaceId}`)}
						>
							<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
								/>
							</svg>
							{t('certificates.upload', $language)}
						</Button>

						<Button
							variant="outline"
							class="w-full"
							onclick={() => goto(`/notifications?workspace=${workspaceId}`)}
						>
							<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
								/>
							</svg>
							{t('workspaces.manageNotifications', $language)}
						</Button>

						<Button
							variant="outline"
							class="w-full"
							onclick={() => alert('Export functionality would be implemented here')}
						>
							<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
								/>
							</svg>
							{t('workspaces.exportData', $language)}
						</Button>
					</div>
				</Card>
			</div>
		</div>
	{/if}
</div>

<!-- Edit Workspace Modal -->
{#if canManage}
	<Modal
		isOpen={showEditModal}
		title={t('workspaces.editWorkspace', $language)}
		onClose={() => (showEditModal = false)}
	>
		<form onsubmit={handleUpdate} class="space-y-4">
			{#if errors.general}
				<div class="rounded-md border border-red-200 bg-red-50 p-4">
					<div class="flex">
						<svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
							<path
								fill-rule="evenodd"
								d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
								clip-rule="evenodd"
							/>
						</svg>
						<div class="ml-3">
							<p class="text-sm text-red-600">{errors.general}</p>
						</div>
					</div>
				</div>
			{/if}

			<Input
				label={t('workspaces.name', $language)}
				bind:value={updateForm.name}
				required
				error={errors.name}
			/>

			<div class="space-y-1">
				<label class="block text-sm font-medium text-gray-700"
					>{t('workspaces.description', $language)}</label
				>
				<textarea
					value={updateForm.description}
					oninput={handleDescriptionChange}
					rows={3}
					class="block w-full rounded-md border border-gray-300 px-3 py-2 placeholder-gray-400 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
					placeholder="Optional workspace description"
				></textarea>
			</div>

			<Input
				type="number"
				label={t('workspaces.maxCertificates', $language)}
				bind:value={updateForm.maxCertificates}
				required
				error={errors.maxCertificates}
			/>

			<div class="flex items-center space-x-4">
				<label class="flex items-center">
					<input
						type="checkbox"
						bind:checked={updateForm.isPublic}
						class="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
					/>
					<span class="ml-2 text-sm text-gray-700">{t('workspaces.isPublic', $language)}</span>
				</label>

				<label class="flex items-center">
					<input
						type="checkbox"
						bind:checked={updateForm.allowMemberInvites}
						class="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
					/>
					<span class="ml-2 text-sm text-gray-700">{t('workspaces.allowInvites', $language)}</span>
				</label>
			</div>

			<div class="flex justify-end space-x-3 pt-4">
				<Button variant="outline" onclick={() => (showEditModal = false)} type="button">
					{t('common.cancel', $language)}
				</Button>
				<Button type="submit" loading={isUpdating}>
					{t('common.save', $language)}
					{t('common.update', $language)}
				</Button>
			</div>
		</form>
	</Modal>

	<!-- Delete Workspace Modal -->
	<Modal
		isOpen={showDeleteModal}
		title={t('workspaces.deleteWorkspace', $language)}
		onClose={() => {
			showDeleteModal = false;
			confirmName = '';
		}}
	>
		<div class="space-y-4">
			<div class="flex items-center">
				<svg
					class="mr-3 h-6 w-6 text-red-600"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
				>
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.865-.833-2.634 0L4.268 18.5c-.77.833.192 2.5 1.732 2.5z"
					/>
				</svg>
				<div>
					<h3 class="text-lg font-medium text-gray-900">
						{t('workspaces.confirmDeletion', $language)}
					</h3>
					<p class="mt-1 text-sm text-gray-500">
						{t('workspaces.cannotBeUndone', $language)}
					</p>
				</div>
			</div>

			{#if workspace}
				<div class="rounded-md border border-gray-200 bg-gray-50 p-3">
					<p class="text-sm font-medium text-gray-900">{workspace.name}</p>
					<p class="text-xs text-gray-500">
						{workspace.certificateCount}
						{workspace.certificateCount === 1
							? t('workspaces.certificates', $language).slice(0, -1)
							: t('workspaces.certificates', $language)} •
						{workspace.memberCount}
						{workspace.memberCount === 1
							? t('common.member', $language)
							: t('common.members', $language)}
					</p>
				</div>
			{/if}

			<div class="rounded-md border border-yellow-200 bg-yellow-50 p-3">
				<div class="flex">
					<svg class="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
						<path
							fill-rule="evenodd"
							d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
							clip-rule="evenodd"
						/>
					</svg>
					<div class="ml-3">
						<p class="text-sm text-yellow-700">
							{t('workspaces.typeWorkspaceName', $language)} <strong>{workspace?.name}</strong>
							{t('workspaces.toConfirmDeletion', $language)}.
						</p>
						<div class="mt-2">
							<Input
								placeholder={t('workspaces.workspaceName', $language)}
								bind:value={confirmName}
							/>
						</div>
					</div>
				</div>
			</div>

			<div class="flex justify-end space-x-3 pt-4">
				<Button
					variant="outline"
					onclick={() => {
						showDeleteModal = false;
						confirmName = '';
					}}
					type="button"
				>
					{t('common.cancel', $language)}
				</Button>
				<Button
					variant="danger"
					loading={isDeleting}
					disabled={confirmName !== workspace?.name}
					onclick={handleDelete}
				>
					{t('workspaces.deleteWorkspace', $language)}
				</Button>
			</div>
		</div>
	</Modal>

	<!-- Invite Member Modal -->
	<Modal
		isOpen={showInviteModal}
		title={t('workspaces.inviteMember', $language)}
		onClose={() => (showInviteModal = false)}
	>
		<form onsubmit={handleInvite} class="space-y-4">
			{#if errors.general}
				<div class="rounded-md border border-red-200 bg-red-50 p-4">
					<div class="flex">
						<svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
							<path
								fill-rule="evenodd"
								d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
								clip-rule="evenodd"
							/>
						</svg>
						<div class="ml-3">
							<p class="text-sm text-red-600">{errors.general}</p>
						</div>
					</div>
				</div>
			{/if}

			<Input
				type="email"
				label={t('workspaces.emailAddress', $language)}
				bind:value={inviteForm.email}
				required
				error={errors.email}
				placeholder="member@example.com"
			/>

			<div class="space-y-1">
				<label class="block text-sm font-medium text-gray-700">
					{t('common.role', $language)} <span class="text-red-500">*</span>
				</label>
				<select
					bind:value={inviteForm.role}
					required
					class="block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
				>
					<option value="Viewer">{t('workspaces.roles.viewer', $language)}</option>
					<option value="Editor">{t('workspaces.roles.editor', $language)}</option>
					<option value="Admin">{t('workspaces.roles.admin', $language)}</option>
				</select>
			</div>

			<div class="rounded-md border border-blue-200 bg-blue-50 p-4">
				<div class="flex">
					<svg class="h-5 w-5 text-blue-400" viewBox="0 0 20 20" fill="currentColor">
						<path
							fill-rule="evenodd"
							d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
							clip-rule="evenodd"
						/>
					</svg>
					<div class="ml-3">
						<p class="text-sm text-blue-700">
							{t('workspaces.invitationNote', $language)}
						</p>
					</div>
				</div>
			</div>

			<div class="flex justify-end space-x-3 pt-4">
				<Button variant="outline" onclick={() => (showInviteModal = false)} type="button">
					{t('common.cancel', $language)}
				</Button>
				<Button type="submit" loading={isInviting}>
					{t('workspaces.sendInvitation', $language)}
				</Button>
			</div>
		</form>
	</Modal>
{/if}
