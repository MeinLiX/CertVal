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

<div class="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
	{#if isLoading}
		<div class="grid grid-cols-1 gap-4">
			<div class="skeleton h-32 w-full rounded-xl bg-base-200/50"></div>
			<div class="grid grid-cols-1 gap-4 md:grid-cols-4">
				{#each { length: 4 } as _}
					<div class="skeleton h-24 w-full rounded-xl bg-base-200/50"></div>
				{/each}
			</div>
		</div>
	{:else if !workspace}
		<div class="flex flex-col items-center justify-center py-20 text-center bg-base-100/30 rounded-3xl border border-base-content/5 backdrop-blur-sm">
			<div class="bg-base-200/50 p-6 rounded-full mb-6">
				<Icon name="error" class="w-16 h-16 text-base-content/20" />
			</div>
			<h3 class="text-xl font-bold mb-2">{t('workspaces.notFound', language.current)}</h3>
			<p class="text-base-content/60 max-w-md mb-8">
				{errors.load || t('workspaces.loadError', language.current)}
			</p>
			<Button onclick={() => goto('/workspaces')} variant="outline" class="shadow-sm">
				<Icon name="leftArrow" class="w-4 h-4 mr-2" />
				{t('workspaces.back', language.current)}
			</Button>
		</div>
	{:else}
		<div class="flex flex-col gap-6 md:flex-row md:items-start md:justify-between border-b border-base-content/10 pb-8">
			<div class="space-y-4">
				<div class="breadcrumbs text-sm text-base-content/60">
					<ul>
						<li>
							<a href="/workspaces" class="hover:text-primary transition-colors flex items-center gap-1">
								<Icon name="workspaces" class="w-4 h-4" />
								{t('nav.workspaces', language.current)}
							</a>
						</li>
						<li><span class="font-medium text-base-content">{workspace.name}</span></li>
					</ul>
				</div>
				<div>
					<h1 class="text-4xl font-bold tracking-tight bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
						{workspace.name}
					</h1>
					<p class="text-lg text-base-content/60 mt-2 font-light max-w-2xl">{workspace.description}</p>
				</div>
			</div>
			<div class="flex flex-wrap gap-3">
				<Button variant="outline" class="shadow-sm bg-base-100/50 backdrop-blur-sm" onclick={() => goto(`/certificates?workspace=${workspaceId}`)}>
					<Icon name="certificates" class="w-4 h-4 mr-2" />
					{t('workspaces.viewCertificates', language.current)}
				</Button>
				<Button variant="outline" class="shadow-sm bg-base-100/50 backdrop-blur-sm" onclick={() => goto(`/notifications?workspace=${workspaceId}`)}>
					<Icon name="notifications" class="w-4 h-4 mr-2" />
					{t('workspaces.manageNotifications', language.current)}
				</Button>
			</div>
		</div>

		<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-4">
			<div class="group relative overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/50 p-6 transition-all duration-300 hover:bg-base-100 hover:shadow-lg hover:border-primary/20">
				<div class="flex items-center gap-4">
					<div class="p-3 rounded-xl bg-primary/10 text-primary group-hover:bg-primary group-hover:text-primary-content transition-colors">
						<Icon name="certificates" class="w-6 h-6" />
					</div>
					<div>
						<p class="text-sm font-medium text-base-content/60">{t('nav.certificates', language.current)}</p>
						<p class="text-2xl font-bold mt-1">{workspace.certificateCount} <span class="text-sm text-base-content/40 font-normal">/ {workspace.maxCertificates}</span></p>
					</div>
				</div>
			</div>

			<div class="group relative overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/50 p-6 transition-all duration-300 hover:bg-base-100 hover:shadow-lg hover:border-secondary/20">
				<div class="flex items-center gap-4">
					<div class="p-3 rounded-xl bg-secondary/10 text-secondary group-hover:bg-secondary group-hover:text-secondary-content transition-colors">
						<Icon name="members" class="w-6 h-6" />
					</div>
					<div>
						<p class="text-sm font-medium text-base-content/60">{t('common.members', language.current)}</p>
						<p class="text-2xl font-bold mt-1">{workspace.memberCount}</p>
					</div>
				</div>
			</div>

			<div class="group relative overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/50 p-6 transition-all duration-300 hover:bg-base-100 hover:shadow-lg hover:border-accent/20">
				<div class="flex items-center gap-4">
					<div class="p-3 rounded-xl bg-accent/10 text-accent group-hover:bg-accent group-hover:text-accent-content transition-colors">
						<Icon name="user" class="w-6 h-6" />
					</div>
					<div>
						<p class="text-sm font-medium text-base-content/60">{t('common.owner', language.current)}</p>
						<p class="text-lg font-bold mt-1 truncate max-w-[120px]" title={members.find((m) => m.role === 'Owner')?.user.fullName}>
							{members.find((m) => m.role === 'Owner')?.user.fullName || 'N/A'}
						</p>
					</div>
				</div>
			</div>

			<div class="group relative overflow-hidden rounded-2xl border border-base-content/10 bg-base-100/50 p-6 transition-all duration-300 hover:bg-base-100 hover:shadow-lg hover:border-info/20">
				<div class="flex items-center gap-4">
					<div class="p-3 rounded-xl bg-info/10 text-info group-hover:bg-info group-hover:text-info-content transition-colors">
						<Icon name="calendar" class="w-6 h-6" />
					</div>
					<div>
						<p class="text-sm font-medium text-base-content/60">{t('common.created', language.current)}</p>
						<p class="text-lg font-bold mt-1">{formatDate(workspace.createdAt)}</p>
					</div>
				</div>
			</div>
		</div>

		<div class="grid grid-cols-1 gap-8 lg:grid-cols-3">
			<div class="space-y-6 lg:col-span-2">
				<div class="rounded-2xl border border-base-content/10 bg-base-100/50 p-6 backdrop-blur-sm">
					<div class="flex items-center justify-between mb-6">
						<h3 class="text-xl font-bold flex items-center gap-2">
							<div class="p-2 rounded-lg bg-warning/10 text-warning">
								<Icon name="time" class="w-5 h-5" />
							</div>
							{t('workspaces.expiringSoon', language.current)}
						</h3>
						{#if certificates.length > 0}
							<Button size="sm" variant="ghost" onclick={() => goto(`/certificates?workspace=${workspaceId}&status=Expiring`)}>
								{t('common.viewAll', language.current)}
								<Icon name="rightArrow" class="w-4 h-4 ml-1" />
							</Button>
						{/if}
					</div>

					{#if certificates.length > 0}
						<div class="overflow-x-auto">
							<table class="table w-full">
								<thead>
									<tr class="border-b border-base-content/10 text-base-content/60">
										<th class="bg-transparent font-medium">{t('common.name', language.current)}</th>
										<th class="bg-transparent font-medium">{t('certificates.expiresIn', language.current)}</th>
										<th class="bg-transparent font-medium">{t('common.status', language.current)}</th>
									</tr>
								</thead>
								<tbody>
									{#each certificates as cert}
										<tr class="border-b border-base-content/5 hover:bg-base-content/5 transition-colors group cursor-pointer" onclick={() => goto(`/certificates/${cert.id}`)}>
											<td class="font-medium">
												<div class="flex items-center gap-3">
													<div class="p-2 rounded-lg bg-base-200 text-base-content/70 group-hover:bg-primary/10 group-hover:text-primary transition-colors">
														<Icon name="document" class="w-4 h-4" />
													</div>
													<span class="truncate max-w-[200px]">{cert.subject}</span>
												</div>
											</td>
											<td class="text-base-content/70">
												{formatDate(cert.notAfter)} 
												<span class="text-xs opacity-60 ml-1">({cert.daysUntilExpiry} {t('certificates.days', language.current)})</span>
											</td>
											<td>
												<span class="badge badge-sm badge-warning gap-1 shadow-sm">
													<span class="w-1.5 h-1.5 rounded-full bg-white animate-pulse"></span>
													{t(`certificates.${getCertificateStatus(cert.notAfter)}`, language.current)}
												</span>
											</td>
										</tr>
									{/each}
								</tbody>
							</table>
						</div>
					{:else}
						<div class="flex flex-col items-center justify-center py-12 text-center">
							<div class="bg-base-200/50 p-4 rounded-full mb-4">
								<Icon name="checkCircle" class="w-8 h-8 text-success" />
							</div>
							<p class="text-base-content/70 font-medium">
								{t('workspaces.noExpiringCertificates', language.current)}
							</p>
						</div>
					{/if}
				</div>
			</div>

			<div class="space-y-8">
				<div class="rounded-2xl border border-base-content/10 bg-base-100/50 p-6 backdrop-blur-sm">
					<div class="flex items-center justify-between mb-6">
						<h3 class="text-xl font-bold flex items-center gap-2">
							<div class="p-2 rounded-lg bg-secondary/10 text-secondary">
								<Icon name="members" class="w-5 h-5" />
							</div>
							{t('common.members', language.current)}
						</h3>
						<span class="badge badge-ghost">{members.length}</span>
					</div>

					<div class="space-y-4 max-h-[400px] overflow-y-auto pr-2 custom-scrollbar">
						{#each members as member}
							{@const statusInfo = getStatusBadge(member.status)}
							<div class="flex items-center justify-between p-3 rounded-xl bg-base-100/50 border border-base-content/5 hover:border-base-content/20 transition-all">
								<div class="flex items-center gap-3">
									<UserAvatar
										firstName={member.user.firstName}
										lastName={member.user.lastName}
										size="w-10"
										textSize="text-sm"
									/>
									<div class="min-w-0">
										<div class="font-bold text-sm truncate max-w-[120px]">{member.user.fullName}</div>
										<div class="text-xs text-base-content/60 truncate max-w-[120px]">{member.user.email}</div>
									</div>
								</div>
								<div class="flex flex-col items-end gap-1">
									<span class="badge {getRoleBadgeClass(member.role)} badge-xs font-medium">
										{t(`workspaces.roles.${member.role.toLowerCase()}`, language.current)}
									</span>
									{#if canManage && member.role !== 'Owner'}
										<button
											class="text-xs text-error hover:underline flex items-center gap-1 mt-1"
											onclick={() => {
												memberToRemove = member;
												showRemoveMemberModal = true;
											}}
										>
											{t('common.delete', language.current)}
										</button>
									{/if}
								</div>
							</div>
						{/each}
					</div>
					
					{#if canManage && workspace.allowMemberInvites}
						<div class="mt-6 pt-6 border-t border-base-content/10">
							<Button class="w-full shadow-sm" variant="outline" onclick={() => (showInviteModal = true)}>
								<Icon name="plus" class="w-4 h-4 mr-2" />
								{t('workspaces.inviteMember', language.current)}
							</Button>
						</div>
					{/if}
				</div>

				{#if currentUserMember?.role === 'Owner'}
					<div class="rounded-2xl border border-error/20 bg-error/5 p-6 backdrop-blur-sm">
						<h3 class="text-lg font-bold text-error flex items-center gap-2 mb-2">
							<Icon name="warning" class="w-5 h-5" />
							{t('workspaces.dangerZone', language.current)}
						</h3>
						<p class="text-sm text-base-content/70 mb-6">
							{t('workspaces.dangerZoneWarning', language.current)}
						</p>
						<div class="flex flex-col gap-3">
							<Button variant="warning" class="w-full justify-start bg-warning/10 hover:bg-warning/20 border-warning/20 text-warning-content" onclick={() => (showTransferModal = true)}>
								<Icon name="user" class="w-4 h-4 mr-2" />
								{t('workspaces.transferOwnership', language.current)}
							</Button>
							<Button variant="danger" class="w-full justify-start shadow-lg shadow-error/10" onclick={() => (showDeleteModal = true)}>
								<Icon name="trash" class="w-4 h-4 mr-2" />
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
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showInviteModal = false)}
					>{t('common.cancel', language.current)}</Button
				>
				<Button type="submit" loading={isProcessing} variant="primary"
					>{t('workspaces.sendInvitation', language.current)}</Button
				>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showDeleteModal}
		title={t('workspaces.deleteWorkspace', language.current)}
		onClose={() => (showDeleteModal = false)}
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
					>{t('workspaces.deleteWorkspace', language.current)}</Button
				>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showTransferModal}
		title={t('workspaces.transferOwnership', language.current)}
		onClose={() => (showTransferModal = false)}
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
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showTransferModal = false)}
					>{t('common.cancel', language.current)}</Button
				>
				<Button type="submit" variant="warning" loading={isProcessing}
					>{t('workspaces.transfer', language.current)}</Button
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
			<Button variant="danger" loading={isProcessing} onclick={handleRemoveMember}
				>{t('workspaces.remove', language.current)}</Button
			>
		</div>
	</Modal>
</div>
