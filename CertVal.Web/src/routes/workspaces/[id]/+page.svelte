<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
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
	const currentUserMember = $derived(members.find(m => m.user.id === $auth.user?.id));
	const canManage = $derived(currentUserMember?.role === 'Owner' || currentUserMember?.role === 'Administrator');

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
				api.get<Workspace>(`/v1/workspaces/${workspaceId}`),
				api.get<PagedResult<Certificate>>(
					`/v1/certificates?workspaceId=${workspaceId}&pageSize=5&statusFilter=Expiring`
				),
				api.get<WorkspaceMember[]>(`/v1/workspaces/${workspaceId}/members`)
			]);

			if (wsRes.data) workspace = wsRes.data;
			if (certsRes.data) certificates = certsRes.data.items;
			if (membersRes.data) members = membersRes.data;
		} catch (err) {
			console.error('Failed to load workspace data:', err);
			errors.load = t('workspaces.loadError', $language);
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
				`/v1/workspaces/${workspaceId}/members/invite`,
				inviteForm
			);
			if (response.data) {
				members.push(response.data);
				showInviteModal = false;
				inviteForm = { email: '', role: 'Viewer' };
			} else {
				errors.invite = response.message || t('errors.general', $language);
			}
		} catch (err) {
			errors.invite = t('errors.network', $language);
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
				`/v1/workspaces/${workspaceId}/members/${memberToRemove.id}`
			);
			if (response.message) {
				errors.removeMember = response.message;
			} else {
				members = members.filter((m) => m.id !== memberToRemove!.id);
				showRemoveMemberModal = false;
				memberToRemove = null;
			}
		} catch (err) {
			errors.removeMember = t('errors.general', $language);
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
				`/v1/workspaces/${workspaceId}/members/transfer-ownership`,
				transferForm
			);
			if (response.data) {
				workspace = response.data;
				showTransferModal = false;
				transferForm.newOwnerEmail = '';
				await loadData();
			} else {
				errors.transfer = response.message || t('errors.general', $language);
			}
		} catch (err) {
			errors.transfer = err instanceof Error ? err.message : t('errors.general', $language);
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
			await api.delete(`/v1/workspaces/${workspaceId}`);
			goto('/workspaces');
		} catch (err) {
			errors.delete = t('errors.general', $language);
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
			case 'administrator':
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
			? `${workspace.name} ${t('common.details', $language)}`
			: t('common.workspace', $language)}</title
	>
</svelte:head>

<div class="space-y-6">
	{#if isLoading}
		<div class="flex h-96 items-center justify-center">
			<span class="loading loading-lg loading-spinner"></span>
		</div>
	{:else if !workspace}
		<Card>
			<div class="py-12 text-center">
				<h3 class="text-xl font-semibold">{t('workspaces.notFound', $language)}</h3>
				<p class="mt-2 text-base-content/60">
					{errors.load || t('workspaces.loadError', $language)}
				</p>
				<Button class="mt-6" onclick={() => goto('/workspaces')}
					>{t('workspaces.back', $language)}</Button
				>
			</div>
		</Card>
	{:else}
		<div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
			<div>
				<div class="breadcrumbs text-sm">
					<ul>
						<li>
							<a href="/workspaces" class="link hover:link-primary"
								>{t('nav.workspaces', $language)}</a
							>
						</li>
						<li><span class="font-semibold">{workspace.name}</span></li>
					</ul>
				</div>
				<h1 class="mt-2 text-3xl font-bold">{workspace.name}</h1>
				<p class="mt-1 text-base-content/70">{workspace.description}</p>
			</div>
			<div class="flex gap-2">
				<Button variant="ghost" onclick={() => goto(`/certificates?workspace=${workspaceId}`)}
					>{t('workspaces.viewCertificates', $language)}</Button
				>
				<Button onclick={() => goto(`/notifications?workspace=${workspaceId}`)}
					>{t('workspaces.manageNotifications', $language)}</Button
				>
			</div>
		</div>

		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-4">
			<Card
				><div class="stat">
					<div class="stat-title">{t('nav.certificates', $language)}</div>
					<div class="stat-value">{workspace.certificateCount} / {workspace.maxCertificates}</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">{t('common.members', $language)}</div>
					<div class="stat-value">{workspace.memberCount}</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">{t('common.owner', $language)}</div>
					<div class="stat-value truncate text-lg">{members.find(m => m.role === 'Owner')?.user.fullName || 'N/A'}</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">{t('common.created', $language)}</div>
					<div class="stat-value text-lg">{formatDate(workspace.createdAt)}</div>
				</div></Card
			>
		</div>

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<div class="space-y-6 lg:col-span-2">
				<Card title={t('workspaces.expiringSoon', $language)}>
					{#if certificates.length > 0}
						<div class="overflow-x-auto">
							<table class="table table-sm">
								<tbody>
									{#each certificates as cert}
										<tr>
											<td
												><a
													href="/certificates/{cert.id}"
													class="block max-w-md link truncate font-semibold link-hover"
													>{cert.subject}</a
												></td
											>
											<td
												>{formatDate(cert.notAfter)} ({cert.daysUntilExpiry}
												{t('certificates.days', $language)})</td
											>
											<td
												><span class="badge badge-sm badge-warning"
													>{t(
														`certificates.${getCertificateStatus(cert.notAfter)}`,
														$language
													)}</span
												></td
											>
										</tr>
									{/each}
								</tbody>
							</table>
						</div>
					{:else}
						<p class="py-4 text-center text-sm text-base-content/70">
							{t('workspaces.noExpiringCertificates', $language)}
						</p>
					{/if}
				</Card>
			</div>

			<div class="space-y-6">
				<Card title={t('common.members', $language)}>
					<div class="space-y-2">
						{#each members as member}
							{@const statusInfo = getStatusBadge(member.status)}
							<div class="flex items-center justify-between">
								<div class="flex items-center gap-3">
									<UserAvatar
										firstName={member.user.firstName}
										lastName={member.user.lastName}
										size="w-10"
										textSize="text-1xl"
									/>
									<div>
										<div class="font-bold">{member.user.fullName}</div>
										<div class="text-xs opacity-50">{member.user.email}</div>
									</div>
								</div>
								<div class="flex items-center gap-2">
									<span class="badge {statusInfo.class} badge-sm">{statusInfo.text}</span>
									<span class="badge {getRoleBadgeClass(member.role)} badge-sm">
										{t(`workspaces.roles.${member.role.toLowerCase()}`, $language)}
									</span>
									{#if canManage && member.role !== 'Owner'}
										<Button
											size="xs"
											variant="ghost"
											onclick={() => {
												memberToRemove = member;
												showRemoveMemberModal = true;
											}}>✕</Button
										>
									{/if}
								</div>
							</div>
						{/each}
					</div>
					{#if canManage && workspace.allowMemberInvites}
						<div class="mt-4 card-actions">
							<Button class="w-full" variant="ghost" onclick={() => (showInviteModal = true)}
								>{t('workspaces.inviteMember', $language)}</Button
							>
						</div>
					{/if}
				</Card>

				{#if currentUserMember?.role === 'Owner'}
					<Card class="border-error">
						<h3 class="card-title text-error">{t('workspaces.dangerZone', $language)}</h3>
						<p class="text-sm text-base-content/70">
							{t('workspaces.dangerZoneWarning', $language)}
						</p>
						<div class="mt-4 card-actions justify-end gap-2">
							<Button variant="warning" onclick={() => (showTransferModal = true)}
								>{t('workspaces.transferOwnership', $language)}</Button
							>
							<Button variant="danger" onclick={() => (showDeleteModal = true)}
								>{t('workspaces.deleteWorkspace', $language)}</Button
							>
						</div>
					</Card>
				{/if}
			</div>
		</div>
	{/if}

	<Modal
		isOpen={showInviteModal}
		title={t('workspaces.inviteNewMember', $language)}
		onClose={() => (showInviteModal = false)}
	>
		<form onsubmit={handleInvite} class="space-y-4">
			{#if errors.invite}<div role="alert" class="alert alert-error text-sm">
					<span>{errors.invite}</span>
				</div>{/if}
			<Input
				label={t('auth.login.email', $language)}
				type="email"
				bind:value={inviteForm.email}
				required
				placeholder="member@example.com"
			/>
			<Select
				label={t('common.role', $language)}
				bind:value={inviteForm.role}
				options={[
					{ value: 'Viewer', label: t('workspaces.roles.viewer', $language) },
					{ value: 'Editor', label: t('workspaces.roles.editor', $language) },
					...(currentUserMember?.role === 'Owner' ? [
						{ value: 'Administrator', label: t('workspaces.roles.administrator', $language) }
					] : [])
				]}
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showInviteModal = false)}
					>{t('common.cancel', $language)}</Button
				>
				<Button type="submit" loading={isProcessing} variant="primary"
					>{t('workspaces.sendInvitation', $language)}</Button
				>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showDeleteModal}
		title={t('workspaces.deleteWorkspace', $language)}
		onClose={() => (showDeleteModal = false)}
	>
		<form onsubmit={handleDelete} class="space-y-4">
			<p>
				{t('workspaces.irreversibleAction', $language)}
				{t('workspaces.lossOfDataWarning', $language)}
			</p>
			<p>
				{t('workspaces.confirmDelete', $language)}
				<strong class="text-error">{workspace?.name}</strong>
			</p>
			{#if errors.delete}<div role="alert" class="alert alert-error text-sm">
					<span>{errors.delete}</span>
				</div>{/if}
			<Input
				bind:value={confirmDeleteName}
				placeholder={t('workspaces.workspaceName', $language)}
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showDeleteModal = false)}
					>{t('common.cancel', $language)}</Button
				>
				<Button
					type="submit"
					variant="danger"
					loading={isProcessing}
					disabled={confirmDeleteName !== workspace?.name}
					>{t('workspaces.deleteWorkspace', $language)}</Button
				>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showTransferModal}
		title={t('workspaces.transferOwnership', $language)}
		onClose={() => (showTransferModal = false)}
	>
		<form onsubmit={handleTransferOwnership} class="space-y-4">
			<p>{t('workspaces.transferWarning', $language)}</p>
			{#if errors.transfer}<div role="alert" class="alert alert-error text-sm">
					<span>{errors.transfer}</span>
				</div>{/if}
			<Input
				label={t('workspaces.newOwnerEmail', $language)}
				type="email"
				bind:value={transferForm.newOwnerEmail}
				required
				placeholder="new.owner@example.com"
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showTransferModal = false)}
					>{t('common.cancel', $language)}</Button
				>
				<Button type="submit" variant="warning" loading={isProcessing}
					>{t('workspaces.transfer', $language)}</Button
				>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showRemoveMemberModal}
		title={t('workspaces.removeMemberTitle', $language)}
		onClose={() => {
			showRemoveMemberModal = false;
			memberToRemove = null;
		}}
	>
		<p>
			{t('workspaces.removeMemberWarning', $language, {
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
				}}>{t('common.cancel', $language)}</Button
			>
			<Button variant="danger" loading={isProcessing} onclick={handleRemoveMember}
				>{t('workspaces.remove', $language)}</Button
			>
		</div>
	</Modal>
</div>
