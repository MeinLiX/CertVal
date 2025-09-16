<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
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
		InviteMemberRequest
	} from '$lib/types';

	let workspace = $state<Workspace | null>(null);
	let certificates = $state<Certificate[]>([]);
	let members = $state<WorkspaceMember[]>([]);
	let isLoading = $state(true);
	let errors = $state<Record<string, string>>({});

	let showInviteModal = $state(false);
	let showDeleteModal = $state(false);
	let inviteForm = $state<InviteMemberRequest>({ email: '', role: 'Viewer' });
	let confirmDeleteName = $state('');
	let isProcessing = $state(false);
	const workspaceId = $derived($page.params.id);
	const canManage = $derived(workspace && $auth.user && workspace.ownerId === $auth.user.id);
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

	async function handleDelete(event: Event) {
		event.preventDefault();
		if (confirmDeleteName !== workspace?.name) return;
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
			case 'admin':
				return 'badge-primary';
			case 'editor':
				return 'badge-secondary';
			default:
				return 'badge-ghost';
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
					<div class="stat-value truncate text-lg">{workspace.owner.fullName}</div>
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
											<td>
												<a
													href="/certificates/{cert.id}"
													class="block max-w-md link truncate font-semibold link-hover"
													>{cert.subject}</a
												>
											</td>
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
						<div class="flex items-center gap-3">
							<div class="placeholder avatar">
								<div class="w-10 rounded-full bg-primary text-primary-content">
									<span
										>{workspace.owner.firstName.charAt(0)}{workspace.owner.lastName.charAt(0)}</span
									>
								</div>
							</div>
							<div>
								<div class="font-bold">{workspace.owner.fullName}</div>
								<div class="text-xs opacity-50">{t('common.owner', $language)}</div>
							</div>
						</div>
						{#each members as member}
							<div class="flex items-center justify-between">
								<div class="flex items-center gap-3">
									<div class="placeholder avatar">
										<div class="w-10 rounded-full bg-neutral text-neutral-content">
											<span>{member.user.firstName.charAt(0)}{member.user.lastName.charAt(0)}</span>
										</div>
									</div>
									<div>
										<div class="font-bold">{member.user.fullName}</div>
										<div class="text-xs opacity-50">{member.user.email}</div>
									</div>
								</div>
								<span class="badge {getRoleBadgeClass(member.role)}"
									>{t(`workspaces.roles.${member.role.toLowerCase()}`, $language)}</span
								>
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

				{#if canManage}
					<Card class="border-error">
						<h3 class="card-title text-error">{t('workspaces.dangerZone', $language)}</h3>
						<p class="text-sm text-base-content/70">
							{t('workspaces.dangerZoneWarning', $language)}
						</p>
						<div class="mt-4 card-actions justify-end">
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
			{#if errors.invite}
				<div role="alert" class="alert alert-error text-sm"><span>{errors.invite}</span></div>
			{/if}
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
					{ value: 'Admin', label: t('workspaces.roles.admin', $language) }
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
			{#if errors.delete}
				<div role="alert" class="alert alert-error text-sm"><span>{errors.delete}</span></div>
			{/if}
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
</div>
