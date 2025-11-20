<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
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

<div class="space-y-6">
	{#if isLoading}
		<div class="flex h-96 items-center justify-center">
			<span class="loading loading-lg loading-spinner"></span>
		</div>
	{:else if !workspace}
		<Card>
			<div class="py-12 text-center">
				<h3 class="text-xl font-semibold">{t('workspaces.notFound', language.current)}</h3>
				<p class="text-base-content/60 mt-2">
					{errors.load || t('workspaces.loadError', language.current)}
				</p>
				<Button class="mt-6" onclick={() => goto('/workspaces')}
					>{t('workspaces.back', language.current)}</Button
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
								>{t('nav.workspaces', language.current)}</a
							>
						</li>
						<li><span class="font-semibold">{workspace.name}</span></li>
					</ul>
				</div>
				<h1 class="mt-2 text-3xl font-bold">{workspace.name}</h1>
				<p class="text-base-content/70 mt-1">{workspace.description}</p>
			</div>
			<div class="flex gap-2">
				<Button variant="ghost" onclick={() => goto(`/certificates?workspace=${workspaceId}`)}
					>{t('workspaces.viewCertificates', language.current)}</Button
				>
				<Button onclick={() => goto(`/notifications?workspace=${workspaceId}`)}
					>{t('workspaces.manageNotifications', language.current)}</Button
				>
			</div>
		</div>

		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-4">
			<Card
				><div class="stat">
					<div class="stat-title">{t('nav.certificates', language.current)}</div>
					<div class="stat-value">{workspace.certificateCount} / {workspace.maxCertificates}</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">{t('common.members', language.current)}</div>
					<div class="stat-value">{workspace.memberCount}</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">{t('common.owner', language.current)}</div>
					<div class="stat-value truncate text-lg">
						{members.find((m) => m.role === 'Owner')?.user.fullName || 'N/A'}
					</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">{t('common.created', language.current)}</div>
					<div class="stat-value text-lg">{formatDate(workspace.createdAt)}</div>
				</div></Card
			>
		</div>

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<div class="space-y-6 lg:col-span-2">
				<Card title={t('workspaces.expiringSoon', language.current)}>
					{#if certificates.length > 0}
						<div class="overflow-x-auto">
							<table class="table-sm table">
								<tbody>
									{#each certificates as cert}
										<tr>
											<td
												><a
													href="/certificates/{cert.id}"
													class="link link-hover block max-w-md truncate font-semibold"
													>{cert.subject}</a
												></td
											>
											<td
												>{formatDate(cert.notAfter)} ({cert.daysUntilExpiry}
												{t('certificates.days', language.current)})</td
											>
											<td
												><span class="badge badge-sm badge-warning"
													>{t(
														`certificates.${getCertificateStatus(cert.notAfter)}`,
														language.current
													)}</span
												></td
											>
										</tr>
									{/each}
								</tbody>
							</table>
						</div>
					{:else}
						<p class="text-base-content/70 py-4 text-center text-sm">
							{t('workspaces.noExpiringCertificates', language.current)}
						</p>
					{/if}
				</Card>
			</div>

			<div class="space-y-6">
				<Card title={t('common.members', language.current)}>
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
										{t(`workspaces.roles.${member.role.toLowerCase()}`, language.current)}
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
						<div class="card-actions mt-4">
							<Button class="w-full" variant="ghost" onclick={() => (showInviteModal = true)}
								>{t('workspaces.inviteMember', language.current)}</Button
							>
						</div>
					{/if}
				</Card>

				{#if currentUserMember?.role === 'Owner'}
					<Card class="border-error">
						<h3 class="card-title text-error">{t('workspaces.dangerZone', language.current)}</h3>
						<p class="text-base-content/70 text-sm">
							{t('workspaces.dangerZoneWarning', language.current)}
						</p>
						<div class="card-actions mt-4 justify-end gap-2">
							<Button variant="warning" onclick={() => (showTransferModal = true)}
								>{t('workspaces.transferOwnership', language.current)}</Button
							>
							<Button variant="danger" onclick={() => (showDeleteModal = true)}
								>{t('workspaces.deleteWorkspace', language.current)}</Button
							>
						</div>
					</Card>
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
