<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import type {
		Workspace,
		Certificate,
		PagedResult,
		WorkspaceMember,
		InviteMemberRequest,
		TransferOwnershipRequest
	} from '$lib/types';
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';

	let workspace = $state<Workspace | null>(null);
	let certificates = $state<Certificate[]>([]);
	let members = $state<WorkspaceMember[]>([]);
	let isLoading = $state(true);
	let errors = $state<Record<string, string>>({});

	let showInviteModal = $state(false);
	let showDeleteModal = $state(false);
	let showTransferModal = $state(false);
	let showRemoveMemberModal = $state(false);
	let memberToRemove = $state<WorkspaceMember | null>(null);

	let inviteForm = $state<InviteMemberRequest>({ email: '', role: 'Viewer' });
	let transferForm = $state<TransferOwnershipRequest>({ newOwnerEmail: '' });
	let confirmDeleteName = $state('');
	let isProcessing = $state(false);

	const workspaceId = $derived(page.params.id);
	const currentUserMember = $derived(members.find((m) => m.user.id === $auth.user?.id));
	const canManage = $derived(
		currentUserMember?.role === 'Owner' || currentUserMember?.role === 'Admin'
	);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadData();
	});

	async function loadData() {
		isLoading = true;
		try {
			const [wsRes, certsRes, membersRes] = await Promise.all([
				api.get<Workspace>(`/workspaces/${workspaceId}`),
				api.get<PagedResult<Certificate>>(
					`/certificates?workspaceId=${workspaceId}&pageSize=5&statusFilter=Expiring`
				),
				api.get<WorkspaceMember[]>(`/workspaces/${workspaceId}/members`)
			]);

			if (wsRes.data) workspace = wsRes.data;
			if (certsRes.data) certificates = certsRes.data.items;
			if (membersRes.data) members = membersRes.data;
		} catch (err) {
			console.error('Failed to load workspace data:', err);
			errors.load = t('workspaces.loadError', language.current);
		} finally {
			isLoading = false;
		}
	}

	async function handleInvite(event: Event) {
		event.preventDefault();
		errors = {};
		isProcessing = true;
		try {
			const response = await api.post<WorkspaceMember>(
				`/workspaces/${workspaceId}/members/invite`,
				inviteForm
			);
			if (response.data) {
				members.push(response.data);
				showInviteModal = false;
				inviteForm = { email: '', role: 'Viewer' };
			} else {
				errors.invite = response.message || t('errors.general', language.current);
			}
		} catch (err) {
			errors.invite = t('errors.network', language.current);
		} finally {
			isProcessing = false;
		}
	}

	async function handleRemoveMember() {
		if (!memberToRemove) return;
		errors = {};
		isProcessing = true;
		try {
			const response = await api.delete(
				`/workspaces/${workspaceId}/members/${memberToRemove.userId}`
			);
			if (response.message) {
				errors.removeMember = response.message;
			} else {
				members = members.filter((m) => m.id !== memberToRemove!.id);
				showRemoveMemberModal = false;
				memberToRemove = null;
			}
		} catch (err) {
			errors.removeMember = t('errors.general', language.current);
		} finally {
			isProcessing = false;
		}
	}

	async function handleTransferOwnership(event: Event) {
		event.preventDefault();
		errors = {};
		isProcessing = true;
		try {
			const response = await api.post<Workspace>(
				`/workspaces/${workspaceId}/members/transfer-ownership`,
				transferForm
			);
			if (response.data) {
				workspace = response.data;
				showTransferModal = false;
				transferForm.newOwnerEmail = '';
				await loadData();
			} else {
				errors.transfer = response.message || t('errors.general', language.current);
			}
		} catch (err) {
			errors.transfer = err instanceof Error ? err.message : t('errors.general', language.current);
		} finally {
			isProcessing = false;
		}
	}

	async function handleDelete(event: Event) {
		event.preventDefault();
		if (confirmDeleteName !== workspace?.name) return;
		errors = {};
		isProcessing = true;
		try {
			await api.delete(`/workspaces/${workspaceId}`);
			goto('/workspaces');
		} catch (err) {
			errors.delete = t('errors.general', language.current);
		} finally {
			isProcessing = false;
			showDeleteModal = false;
			confirmDeleteName = '';
		}
	}

	function getRoleBadgeClass(role: string): string {
		switch (role.toLowerCase()) {
			case 'owner':
				return 'badge-error';
			case 'admin':
				return 'badge-primary';
			case 'editor':
				return 'badge-secondary';
			default:
				return 'badge-ghost';
		}
	}

	function getStatusBadge(status: string): { class: string; text: string } {
		switch (status.toLowerCase()) {
			case 'active':
				return { class: 'badge-success', text: 'Active' };
			case 'invited':
				return { class: 'badge-warning', text: 'Invited' };
			default:
				return { class: 'badge-ghost', text: status };
		}
	}
</script>

<svelte:head>
	<title
		>{workspace
			? `${workspace.name} ${t('common.details', language.current)}`
			: t('common.workspace', language.current)}</title
	>
</svelte:head>

<div
	class="animate-in fade-in slide-in-from-bottom-4 space-y-8 duration-500"
	data-test-id="workspace-details-page"
>
	{#if isLoading}
		<div class="grid grid-cols-1 gap-4">
			<div class="skeleton bg-base-200/50 h-32 w-full rounded-xl"></div>
			<div class="grid grid-cols-1 gap-4 md:grid-cols-4">
				{#each { length: 4 } as _}
					<div class="skeleton bg-base-200/50 h-24 w-full rounded-xl"></div>
				{/each}
			</div>
		</div>
	{:else if !workspace}
		<div
			class="bg-base-100/30 border-base-content/5 flex flex-col items-center justify-center rounded-3xl border py-20 text-center backdrop-blur-sm"
		>
			<div class="bg-base-200/50 mb-6 rounded-full p-6">
				<Icon name="error" class="text-base-content/20 h-16 w-16" />
			</div>
			<h3 class="mb-2 text-xl font-bold">{t('workspaces.notFound', language.current)}</h3>
			<p class="text-base-content/60 mb-8 max-w-md">
				{errors.load || t('workspaces.loadError', language.current)}
			</p>
			<Button
				onclick={() => goto('/workspaces')}
				variant="outline"
				class="shadow-sm"
				data-test-id="workspace-error-back-button"
			>
				<Icon name="leftArrow" class="mr-2 h-4 w-4" />
				{t('workspaces.back', language.current)}
			</Button>
		</div>
	{:else}
		<div
			class="border-base-content/10 flex flex-col gap-6 border-b pb-8 md:flex-row md:items-start md:justify-between"
		>
			<div class="space-y-4">
				<div class="breadcrumbs text-base-content/60 text-sm">
					<ul>
						<li>
							<a
								href="/workspaces"
								class="hover:text-primary flex items-center gap-1 transition-colors"
								data-test-id="workspace-breadcrumb-link"
							>
								<Icon name="workspaces" class="h-4 w-4" />
								{t('nav.workspaces', language.current)}
							</a>
						</li>
						<li><span class="text-base-content font-medium">{workspace.name}</span></li>
					</ul>
				</div>
				<div>
					<h1
						class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-4xl font-bold tracking-tight text-transparent"
					>
						{workspace.name}
					</h1>
					<p class="text-base-content/60 mt-2 max-w-2xl text-lg font-light">
						{workspace.description}
					</p>
				</div>
			</div>
			<div class="flex flex-wrap gap-3">
				<Button
					variant="outline"
					class="bg-base-100/50 shadow-sm backdrop-blur-sm"
					onclick={() => goto(`/certificates?workspace=${workspaceId}`)}
					data-test-id="workspace-view-certificates-button"
				>
					<Icon name="certificates" class="mr-2 h-4 w-4" />
					{t('workspaces.viewCertificates', language.current)}
				</Button>
				<Button
					variant="outline"
					class="bg-base-100/50 shadow-sm backdrop-blur-sm"
					onclick={() => goto(`/notifications?workspace=${workspaceId}`)}
					data-test-id="workspace-manage-notifications-button"
				>
					<Icon name="notifications" class="mr-2 h-4 w-4" />
					{t('workspaces.manageNotifications', language.current)}
				</Button>
			</div>
		</div>

		<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-4">
			<div
				class="border-base-content/10 bg-base-100/50 hover:bg-base-100 hover:border-primary/20 group relative overflow-hidden rounded-2xl border p-6 transition-all duration-300 hover:shadow-lg"
			>
				<div class="flex items-center gap-4">
					<div
						class="bg-primary/10 text-primary group-hover:bg-primary group-hover:text-primary-content rounded-xl p-3 transition-colors"
					>
						<Icon name="certificates" class="h-6 w-6" />
					</div>
					<div>
						<p class="text-base-content/60 text-sm font-medium">
							{t('nav.certificates', language.current)}
						</p>
						<p class="mt-1 text-2xl font-bold">
							{workspace.certificateCount}
							<span class="text-base-content/40 text-sm font-normal"
								>/ {workspace.maxCertificates}</span
							>
						</p>
					</div>
				</div>
			</div>

			<div
				class="border-base-content/10 bg-base-100/50 hover:bg-base-100 hover:border-secondary/20 group relative overflow-hidden rounded-2xl border p-6 transition-all duration-300 hover:shadow-lg"
			>
				<div class="flex items-center gap-4">
					<div
						class="bg-secondary/10 text-secondary group-hover:bg-secondary group-hover:text-secondary-content rounded-xl p-3 transition-colors"
					>
						<Icon name="members" class="h-6 w-6" />
					</div>
					<div>
						<p class="text-base-content/60 text-sm font-medium">
							{t('common.members', language.current)}
						</p>
						<p class="mt-1 text-2xl font-bold">{workspace.memberCount}</p>
					</div>
				</div>
			</div>

			<div
				class="border-base-content/10 bg-base-100/50 hover:bg-base-100 hover:border-accent/20 group relative overflow-hidden rounded-2xl border p-6 transition-all duration-300 hover:shadow-lg"
			>
				<div class="flex items-center gap-4">
					<div
						class="bg-accent/10 text-accent group-hover:bg-accent group-hover:text-accent-content rounded-xl p-3 transition-colors"
					>
						<Icon name="user" class="h-6 w-6" />
					</div>
					<div>
						<p class="text-base-content/60 text-sm font-medium">
							{t('common.owner', language.current)}
						</p>
						<p
							class="mt-1 max-w-[120px] truncate text-lg font-bold"
							title={members.find((m) => m.role === 'Owner')?.user.fullName}
						>
							{members.find((m) => m.role === 'Owner')?.user.fullName || 'N/A'}
						</p>
					</div>
				</div>
			</div>

			<div
				class="border-base-content/10 bg-base-100/50 hover:bg-base-100 hover:border-info/20 group relative overflow-hidden rounded-2xl border p-6 transition-all duration-300 hover:shadow-lg"
			>
				<div class="flex items-center gap-4">
					<div
						class="bg-info/10 text-info group-hover:bg-info group-hover:text-info-content rounded-xl p-3 transition-colors"
					>
						<Icon name="calendar" class="h-6 w-6" />
					</div>
					<div>
						<p class="text-base-content/60 text-sm font-medium">
							{t('common.created', language.current)}
						</p>
						<p class="mt-1 text-lg font-bold">{formatDate(workspace.createdAt)}</p>
					</div>
				</div>
			</div>
		</div>

		<div class="grid grid-cols-1 gap-8 lg:grid-cols-3">
			<div class="space-y-6 lg:col-span-2">
				<div class="border-base-content/10 bg-base-100/50 rounded-2xl border p-6 backdrop-blur-sm">
					<div class="mb-6 flex items-center justify-between">
						<h3 class="flex items-center gap-2 text-xl font-bold">
							<div class="bg-warning/10 text-warning rounded-lg p-2">
								<Icon name="time" class="h-5 w-5" />
							</div>
							{t('workspaces.expiringSoon', language.current)}
						</h3>
						{#if certificates.length > 0}
							<Button
								size="sm"
								variant="ghost"
								onclick={() => goto(`/certificates?workspace=${workspaceId}&status=Expiring`)}
								data-test-id="workspace-view-all-expiring-button"
							>
								{t('common.viewAll', language.current)}
								<Icon name="rightArrow" class="ml-1 h-4 w-4" />
							</Button>
						{/if}
					</div>

					{#if certificates.length > 0}
						<div class="overflow-x-auto">
							<table class="table w-full">
								<thead>
									<tr class="border-base-content/10 text-base-content/60 border-b">
										<th class="bg-transparent font-medium">{t('common.name', language.current)}</th>
										<th class="bg-transparent font-medium"
											>{t('certificates.expiresIn', language.current)}</th
										>
										<th class="bg-transparent font-medium"
											>{t('common.status', language.current)}</th
										>
									</tr>
								</thead>
								<tbody>
									{#each certificates as cert}
										<tr
											class="border-base-content/5 hover:bg-base-content/5 group cursor-pointer border-b transition-colors"
											onclick={() => goto(`/certificates/${cert.id}`)}
											data-test-id={`workspace-expiring-cert-row-${cert.id}`}
										>
											<td class="font-medium">
												<div class="flex items-center gap-3">
													<div
														class="bg-base-200 text-base-content/70 group-hover:bg-primary/10 group-hover:text-primary rounded-lg p-2 transition-colors"
													>
														<Icon name="document" class="h-4 w-4" />
													</div>
													<span class="max-w-[200px] truncate">{cert.subject}</span>
												</div>
											</td>
											<td class="text-base-content/70">
												{formatDate(cert.notAfter)}
												<span class="ml-1 text-xs opacity-60"
													>({cert.daysUntilExpiry} {t('certificates.days', language.current)})</span
												>
											</td>
											<td>
												<span class="badge badge-sm badge-warning gap-1 shadow-sm">
													<span class="h-1.5 w-1.5 animate-pulse rounded-full bg-white"></span>
													{t(
														`certificates.${getCertificateStatus(cert.notAfter)}`,
														language.current
													)}
												</span>
											</td>
										</tr>
									{/each}
								</tbody>
							</table>
						</div>
					{:else}
						<div class="flex flex-col items-center justify-center py-12 text-center">
							<div class="bg-base-200/50 mb-4 rounded-full p-4">
								<Icon name="checkCircle" class="text-success h-8 w-8" />
							</div>
							<p class="text-base-content/70 font-medium">
								{t('workspaces.noExpiringCertificates', language.current)}
							</p>
						</div>
					{/if}
				</div>
			</div>

			<div class="space-y-8">
				<div class="border-base-content/10 bg-base-100/50 rounded-2xl border p-6 backdrop-blur-sm">
					<div class="mb-6 flex items-center justify-between">
						<h3 class="flex items-center gap-2 text-xl font-bold">
							<div class="bg-secondary/10 text-secondary rounded-lg p-2">
								<Icon name="members" class="h-5 w-5" />
							</div>
							{t('common.members', language.current)}
						</h3>
						<span class="badge badge-ghost">{members.length}</span>
					</div>

					<div class="custom-scrollbar max-h-[400px] space-y-4 overflow-y-auto pr-2">
						{#each members as member}
							{@const statusInfo = getStatusBadge(member.status)}
							<div
								class="bg-base-100/50 border-base-content/5 hover:border-base-content/20 flex items-center justify-between rounded-xl border p-3 transition-all"
								data-test-id={`workspace-member-item-${member.id}`}
							>
								<div class="flex items-center gap-3">
									<UserAvatar
										firstName={member.user.firstName}
										lastName={member.user.lastName}
										size="w-10"
										textSize="text-sm"
									/>
									<div class="min-w-0">
										<div class="max-w-[120px] truncate text-sm font-bold">
											{member.user.fullName}
										</div>
										<div class="text-base-content/60 max-w-[120px] truncate text-xs">
											{member.user.email}
										</div>
									</div>
								</div>
								<div class="flex flex-col items-end gap-1">
									<span class="badge {getRoleBadgeClass(member.role)} badge-xs font-medium">
										{t(`workspaces.roles.${member.role.toLowerCase()}`, language.current)}
									</span>
									{#if canManage && member.role !== 'Owner'}
										<button
											class="text-error mt-1 flex items-center gap-1 text-xs hover:underline"
											onclick={() => {
												memberToRemove = member;
												showRemoveMemberModal = true;
											}}
											data-test-id={`workspace-remove-member-button-${member.id}`}
										>
											{t('common.delete', language.current)}
										</button>
									{/if}
								</div>
							</div>
						{/each}
					</div>

					{#if canManage && workspace.allowMemberInvites}
						<div class="border-base-content/10 mt-6 border-t pt-6">
							<Button
								class="w-full shadow-sm"
								variant="outline"
								onclick={() => (showInviteModal = true)}
								data-test-id="workspace-invite-member-button"
							>
								<Icon name="plus" class="mr-2 h-4 w-4" />
								{t('workspaces.inviteMember', language.current)}
							</Button>
						</div>
					{/if}
				</div>

				{#if currentUserMember?.role === 'Owner'}
					<div class="border-error/20 bg-error/5 rounded-2xl border p-6 backdrop-blur-sm">
						<h3 class="text-error mb-2 flex items-center gap-2 text-lg font-bold">
							<Icon name="warning" class="h-5 w-5" />
							{t('workspaces.dangerZone', language.current)}
						</h3>
						<p class="text-base-content/70 mb-6 text-sm">
							{t('workspaces.dangerZoneWarning', language.current)}
						</p>
						<div class="flex flex-col gap-3">
							<Button
								variant="warning"
								class="bg-warning/10 hover:bg-warning/20 border-warning/20 text-warning-content w-full justify-start"
								onclick={() => (showTransferModal = true)}
								data-test-id="workspace-transfer-ownership-button"
							>
								<Icon name="user" class="mr-2 h-4 w-4" />
								{t('workspaces.transferOwnership', language.current)}
							</Button>
							<Button
								variant="danger"
								class="shadow-error/10 w-full justify-start shadow-lg"
								onclick={() => (showDeleteModal = true)}
								data-test-id="workspace-delete-button"
							>
								<Icon name="trash" class="mr-2 h-4 w-4" />
								{t('workspaces.deleteWorkspace', language.current)}
							</Button>
						</div>
					</div>
				{/if}
			</div>
		</div>
	{/if}

	<Modal
		isOpen={showInviteModal}
		title={t('workspaces.inviteNewMember', language.current)}
		onClose={() => (showInviteModal = false)}
		data-test-id="workspace-invite-modal"
	>
		<form onsubmit={handleInvite} class="space-y-4">
			{#if errors.invite}<div role="alert" class="alert alert-error text-sm">
					<span>{errors.invite}</span>
				</div>{/if}
			<Input
				label={t('auth.login.email', language.current)}
				type="email"
				bind:value={inviteForm.email}
				required
				placeholder="member@example.com"
				data-test-id="invite-email-input"
			/>
			<Select
				label={t('common.role', language.current)}
				bind:value={inviteForm.role}
				options={[
					{ value: 'Viewer', label: t('workspaces.roles.viewer', language.current) },
					{ value: 'Editor', label: t('workspaces.roles.editor', language.current) },
					...(currentUserMember?.role === 'Owner'
						? [{ value: 'Admin', label: t('workspaces.roles.admin', language.current) }]
						: [])
				]}
				data-test-id="invite-role-select"
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showInviteModal = false)}
					>{t('common.cancel', language.current)}</Button
				>
				<Button
					type="submit"
					loading={isProcessing}
					variant="primary"
					data-test-id="invite-submit-button"
					>{t('workspaces.sendInvitation', language.current)}</Button
				>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showDeleteModal}
		title={t('workspaces.deleteWorkspace', language.current)}
		onClose={() => (showDeleteModal = false)}
		data-test-id="workspace-delete-modal"
	>
		<form onsubmit={handleDelete} class="space-y-4">
			<p>
				{t('workspaces.irreversibleAction', language.current)}
				{t('workspaces.lossOfDataWarning', language.current)}
			</p>
			<p>
				{t('workspaces.confirmDelete', language.current)}
				<strong class="text-error">{workspace?.name}</strong>
			</p>
			{#if errors.delete}<div role="alert" class="alert alert-error text-sm">
					<span>{errors.delete}</span>
				</div>{/if}
			<Input
				bind:value={confirmDeleteName}
				placeholder={t('workspaces.workspaceName', language.current)}
				data-test-id="delete-workspace-name-input"
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showDeleteModal = false)}
					>{t('common.cancel', language.current)}</Button
				>
				<Button
					type="submit"
					variant="danger"
					loading={isProcessing}
					disabled={confirmDeleteName !== workspace?.name}
					data-test-id="delete-workspace-confirm-button"
					>{t('workspaces.deleteWorkspace', language.current)}</Button
				>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showTransferModal}
		title={t('workspaces.transferOwnership', language.current)}
		onClose={() => (showTransferModal = false)}
		data-test-id="workspace-transfer-modal"
	>
		<form onsubmit={handleTransferOwnership} class="space-y-4">
			<p>{t('workspaces.transferWarning', language.current)}</p>
			{#if errors.transfer}<div role="alert" class="alert alert-error text-sm">
					<span>{errors.transfer}</span>
				</div>{/if}
			<Input
				label={t('workspaces.newOwnerEmail', language.current)}
				type="email"
				bind:value={transferForm.newOwnerEmail}
				required
				placeholder="new.owner@example.com"
				data-test-id="transfer-email-input"
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showTransferModal = false)}
					>{t('common.cancel', language.current)}</Button
				>
				<Button
					type="submit"
					variant="warning"
					loading={isProcessing}
					data-test-id="transfer-submit-button">{t('workspaces.transfer', language.current)}</Button
				>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showRemoveMemberModal}
		title={t('workspaces.removeMemberTitle', language.current)}
		onClose={() => {
			showRemoveMemberModal = false;
			memberToRemove = null;
		}}
		data-test-id="workspace-remove-member-modal"
	>
		<p>
			{t('workspaces.removeMemberWarning', language.current, {
				memberName: memberToRemove?.user.fullName
			})}
		</p>
		{#if errors.removeMember}<div role="alert" class="alert alert-error text-sm">
				<span>{errors.removeMember}</span>
			</div>{/if}
		<div class="modal-action">
			<Button
				type="button"
				variant="ghost"
				onclick={() => {
					showRemoveMemberModal = false;
					memberToRemove = null;
				}}>{t('common.cancel', language.current)}</Button
			>
			<Button
				variant="danger"
				loading={isProcessing}
				onclick={handleRemoveMember}
				data-test-id="remove-member-confirm-button"
				>{t('workspaces.remove', language.current)}</Button
			>
		</div>
	</Modal>
</div>
