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
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import type {
		Workspace,
		Certificate,
		PagedResult,
		WorkspaceMember,
		InviteMemberRequest,
		TransferOwnershipRequest,
		UpdateWorkspaceRequest
	} from '$lib/types';

	let workspace = $state<Workspace | null>(null);
	let certificates = $state<Certificate[]>([]);
	let members = $state<WorkspaceMember[]>([]);
	let isLoading = $state(true);
	let errors = $state<Record<string, string>>({});

	let showInviteModal = $state(false);
	let showDeleteModal = $state(false);
	let showTransferModal = $state(false);
	let showRemoveMemberModal = $state(false);
	let showEditModal = $state(false);
	let memberToRemove = $state<WorkspaceMember | null>(null);

	let inviteForm = $state<InviteMemberRequest>({ email: '', role: 'Viewer' });
	let transferForm = $state<TransferOwnershipRequest>({ newOwnerEmail: '' });
	let editForm = $state<UpdateWorkspaceRequest>({
		name: '',
		description: '',
		maxCertificates: 1000,
		isPublic: false,
		allowMemberInvites: true,
		autoDeleteExpiredCertificates: false,
		ocspMonitoringEnabled: true
	});
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

	function openEditModal() {
		if (!workspace) return;
		editForm = {
			name: workspace.name,
			description: workspace.description ?? '',
			maxCertificates: workspace.maxCertificates,
			isPublic: workspace.isPublic,
			allowMemberInvites: workspace.allowMemberInvites,
			autoDeleteExpiredCertificates: workspace.autoDeleteExpiredCertificates,
			ocspMonitoringEnabled: workspace.ocspMonitoringEnabled ?? true
		};
		errors = {};
		showEditModal = true;
	}

	async function handleEdit(event: Event) {
		event.preventDefault();
		if (!workspace) return;
		errors = {};
		isProcessing = true;
		try {
			const response = await api.put<Workspace>(`/workspaces/${workspaceId}`, editForm);
			if (response.data) {
				workspace = response.data;
				showEditModal = false;
			} else {
				errors.edit = response.message || t('errors.general', language.current);
			}
		} catch (err) {
			errors.edit = t('errors.network', language.current);
		} finally {
			isProcessing = false;
		}
	}
</script>

<svelte:head>
	<title>
		{workspace
			? `${workspace.name} ${t('common.details', language.current)}`
			: t('common.workspace', language.current)}
	</title>
</svelte:head>

<div class="page">
	{#if isLoading}
		<div class="loading-state">
			<div class="skeleton skeleton--header"></div>
			<div class="stats-grid">
				{#each Array(4) as _}
					<div class="skeleton skeleton--card"></div>
				{/each}
			</div>
		</div>
	{:else if !workspace}
		<div class="error-state">
			<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
				<circle cx="12" cy="12" r="10" />
				<line x1="12" y1="8" x2="12" y2="12" />
				<line x1="12" y1="16" x2="12.01" y2="16" />
			</svg>
			<h3>{t('workspaces.notFound', language.current)}</h3>
			<p>{errors.load || t('workspaces.loadError', language.current)}</p>
			<Button variant="secondary" onclick={() => goto('/workspaces')}>
				← {t('workspaces.back', language.current)}
			</Button>
		</div>
	{:else}
		<header class="page__header">
			<nav class="breadcrumb">
				<a href="/workspaces" class="breadcrumb__link">
					<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<rect x="3" y="3" width="7" height="7" />
						<rect x="14" y="3" width="7" height="7" />
						<rect x="14" y="14" width="7" height="7" />
						<rect x="3" y="14" width="7" height="7" />
					</svg>
					{t('nav.workspaces', language.current)}
				</a>
				<span class="breadcrumb__separator">/</span>
				<span class="breadcrumb__current">{workspace.name}</span>
			</nav>

			<div class="page__title-row">
				<div>
					<h1 class="page__title">{workspace.name}</h1>
					{#if workspace.description}
						<p class="page__subtitle">{workspace.description}</p>
					{/if}
				</div>
				<div class="page__actions">
					<Button variant="secondary" onclick={() => goto(`/certificates?workspace=${workspaceId}`)}>
						{t('workspaces.viewCertificates', language.current)}
					</Button>
					<Button variant="secondary" onclick={() => goto(`/notifications?workspace=${workspaceId}`)}>
						{t('workspaces.manageNotifications', language.current)}
					</Button>
					<Button variant="secondary" onclick={() => goto(`/workspaces/${workspaceId}/audit`)}>
						{t('workspaces.auditLog', language.current)}
					</Button>
					{#if canManage}
						<Button variant="primary" onclick={openEditModal}>
							{t('workspaces.edit', language.current)}
						</Button>
					{/if}
				</div>
			</div>
		</header>

		<div class="stats-grid">
			<div class="stat-card">
				<div class="stat-card__icon">
					<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
						<path d="M7 11V7a5 5 0 0 1 10 0v4" />
					</svg>
				</div>
				<div class="stat-card__content">
					<span class="stat-card__label">{t('nav.certificates', language.current)}</span>
					<span class="stat-card__value">
						{workspace.certificateCount}
						<span class="stat-card__max">/ {workspace.maxCertificates}</span>
					</span>
				</div>
			</div>

			<div class="stat-card">
				<div class="stat-card__icon">
					<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
						<circle cx="9" cy="7" r="4" />
						<path d="M23 21v-2a4 4 0 0 0-3-3.87" />
						<path d="M16 3.13a4 4 0 0 1 0 7.75" />
					</svg>
				</div>
				<div class="stat-card__content">
					<span class="stat-card__label">{t('common.members', language.current)}</span>
					<span class="stat-card__value">{workspace.memberCount}</span>
				</div>
			</div>

			<div class="stat-card">
				<div class="stat-card__icon">
					<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
						<circle cx="12" cy="7" r="4" />
					</svg>
				</div>
				<div class="stat-card__content">
					<span class="stat-card__label">{t('common.owner', language.current)}</span>
					<span class="stat-card__value stat-card__value--text">
						{members.find((m) => m.role === 'Owner')?.user.fullName || 'N/A'}
					</span>
				</div>
			</div>

			<div class="stat-card">
				<div class="stat-card__icon">
					<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
						<line x1="16" y1="2" x2="16" y2="6" />
						<line x1="8" y1="2" x2="8" y2="6" />
						<line x1="3" y1="10" x2="21" y2="10" />
					</svg>
				</div>
				<div class="stat-card__content">
					<span class="stat-card__label">{t('common.created', language.current)}</span>
					<span class="stat-card__value stat-card__value--text">{formatDate(workspace.createdAt)}</span>
				</div>
			</div>
		</div>

		<div class="content-grid">
			<section class="section section--main">
				<div class="section__header">
					<h2 class="section__title">
						<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<circle cx="12" cy="12" r="10" />
							<polyline points="12 6 12 12 16 14" />
						</svg>
						{t('workspaces.expiringSoon', language.current)}
					</h2>
					{#if certificates.length > 0}
						<Button variant="ghost" size="sm" onclick={() => goto(`/certificates?workspace=${workspaceId}&status=Expiring`)}>
							{t('common.viewAll', language.current)} →
						</Button>
					{/if}
				</div>

				{#if certificates.length > 0}
					<div class="table-container">
						<table class="table">
							<thead>
								<tr>
									<th>{t('common.name', language.current)}</th>
									<th>{t('certificates.expiresIn', language.current)}</th>
									<th>{t('common.status', language.current)}</th>
								</tr>
							</thead>
							<tbody>
								{#each certificates as cert}
									<tr class="table__row--clickable" onclick={() => goto(`/certificates/${cert.id}`)}>
										<td>
											<div class="cert-name">
												<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
													<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
													<polyline points="14 2 14 8 20 8" />
												</svg>
												<span>{cert.subject}</span>
											</div>
										</td>
										<td>
											<span class="date-display">
												{formatDate(cert.notAfter)}
												<small>({cert.daysUntilExpiry} {t('certificates.days', language.current)})</small>
											</span>
										</td>
										<td>
											<span class="badge badge--warning">
												{t(`certificates.${getCertificateStatus(cert.notAfter)}`, language.current)}
											</span>
										</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				{:else}
					<div class="empty-section">
						<svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
							<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
							<polyline points="22 4 12 14.01 9 11.01" />
						</svg>
						<p>{t('workspaces.noExpiringCertificates', language.current)}</p>
					</div>
				{/if}
			</section>

			<aside class="sidebar">
				<section class="section">
					<div class="section__header">
						<h2 class="section__title">
							<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
								<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
								<circle cx="9" cy="7" r="4" />
								<path d="M23 21v-2a4 4 0 0 0-3-3.87" />
								<path d="M16 3.13a4 4 0 0 1 0 7.75" />
							</svg>
							{t('common.members', language.current)}
						</h2>
						<span class="count-badge">{members.length}</span>
					</div>

					<div class="members-list">
						{#each members as member}
							<div class="member-item">
								<div class="member-item__info">
									<UserAvatar
										firstName={member.user.firstName}
										lastName={member.user.lastName}
										size="md"
									/>
									<div class="member-item__details">
										<span class="member-item__name">{member.user.fullName}</span>
										<span class="member-item__email">{member.user.email}</span>
									</div>
								</div>
								<div class="member-item__actions">
									<span class="role-badge role-badge--{member.role.toLowerCase()}">
										{t(`workspaces.roles.${member.role.toLowerCase()}`, language.current)}
									</span>
									{#if canManage && member.role !== 'Owner'}
										<button
											class="remove-btn"
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
						<div class="section__footer">
							<Button variant="secondary" class="full-width" onclick={() => { showInviteModal = true; }}>
								+ {t('workspaces.inviteMember', language.current)}
							</Button>
						</div>
					{/if}
				</section>

				{#if currentUserMember?.role === 'Owner'}
					<section class="section section--danger">
						<h3 class="section__title section__title--danger">
							<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
								<path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
								<line x1="12" y1="9" x2="12" y2="13" />
								<line x1="12" y1="17" x2="12.01" y2="17" />
							</svg>
							{t('workspaces.dangerZone', language.current)}
						</h3>
						<p class="section__text">{t('workspaces.dangerZoneWarning', language.current)}</p>
						<div class="danger-actions">
							<Button variant="warning" class="full-width" onclick={() => { showTransferModal = true; }}>
								{t('workspaces.transferOwnership', language.current)}
							</Button>
							<Button variant="danger" class="full-width" onclick={() => { showDeleteModal = true; }}>
								{t('workspaces.deleteWorkspace', language.current)}
							</Button>
						</div>
					</section>
				{/if}
			</aside>
		</div>
	{/if}
</div>

<Modal bind:isOpen={showInviteModal} title={t('workspaces.inviteNewMember', language.current)} onclose={() => (showInviteModal = false)}>
	<form class="modal-form" onsubmit={handleInvite}>
		{#if errors.invite}
			<div class="alert alert--error">{errors.invite}</div>
		{/if}
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
		<div class="modal-form__actions">
			<Button type="button" variant="secondary" onclick={() => { showInviteModal = false; }}>
				{t('common.cancel', language.current)}
			</Button>
			<Button type="submit" loading={isProcessing}>
				{t('workspaces.sendInvitation', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<Modal bind:isOpen={showDeleteModal} title={t('workspaces.deleteWorkspace', language.current)} onclose={() => (showDeleteModal = false)}>
	<form class="modal-form" onsubmit={handleDelete}>
		<p class="modal-text">
			{t('workspaces.irreversibleAction', language.current)}
			{t('workspaces.lossOfDataWarning', language.current)}
		</p>
		<p class="modal-text">
			{t('workspaces.confirmDelete', language.current)}
			<strong class="danger-text">{workspace?.name}</strong>
		</p>
		{#if errors.delete}
			<div class="alert alert--error">{errors.delete}</div>
		{/if}
		<Input
			bind:value={confirmDeleteName}
			placeholder={t('workspaces.workspaceName', language.current)}
		/>
		<div class="modal-form__actions">
			<Button type="button" variant="secondary" onclick={() => { showDeleteModal = false; }}>
				{t('common.cancel', language.current)}
			</Button>
			<Button
				type="submit"
				variant="danger"
				loading={isProcessing}
				disabled={confirmDeleteName !== workspace?.name}
			>
				{t('workspaces.deleteWorkspace', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<Modal bind:isOpen={showTransferModal} title={t('workspaces.transferOwnership', language.current)} onclose={() => (showTransferModal = false)}>
	<form class="modal-form" onsubmit={handleTransferOwnership}>
		<p class="modal-text">{t('workspaces.transferWarning', language.current)}</p>
		{#if errors.transfer}
			<div class="alert alert--error">{errors.transfer}</div>
		{/if}
		<Input
			label={t('workspaces.newOwnerEmail', language.current)}
			type="email"
			bind:value={transferForm.newOwnerEmail}
			required
			placeholder="new.owner@example.com"
		/>
		<div class="modal-form__actions">
			<Button type="button" variant="secondary" onclick={() => { showTransferModal = false; }}>
				{t('common.cancel', language.current)}
			</Button>
			<Button type="submit" variant="warning" loading={isProcessing}>
				{t('workspaces.transfer', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<Modal
	bind:isOpen={showRemoveMemberModal}
	title={t('workspaces.removeMemberTitle', language.current)}
	onclose={() => {
		showRemoveMemberModal = false;
		memberToRemove = null;
	}}
>
	<div class="modal-form">
		<p class="modal-text">
			{t('workspaces.removeMemberWarning', language.current, {
				memberName: memberToRemove?.user.fullName
			})}
		</p>
		{#if errors.removeMember}
			<div class="alert alert--error">{errors.removeMember}</div>
		{/if}
		<div class="modal-form__actions">
			<Button
				type="button"
				variant="secondary"
				onclick={() => {
					showRemoveMemberModal = false;
					memberToRemove = null;
				}}
			>
				{t('common.cancel', language.current)}
			</Button>
			<Button variant="danger" loading={isProcessing} onclick={handleRemoveMember}>
				{t('workspaces.remove', language.current)}
			</Button>
		</div>
	</div>
</Modal>

<Modal
	bind:isOpen={showEditModal}
	title={t('workspaces.edit', language.current)}
	onclose={() => (showEditModal = false)}
>
	<form class="modal-form" onsubmit={handleEdit}>
		{#if errors.edit}
			<div class="alert alert--error">{errors.edit}</div>
		{/if}
		<Input
			label={t('workspaces.name', language.current)}
			bind:value={editForm.name}
			required
			placeholder={t('workspaces.namePlaceholder', language.current)}
		/>
		<Input
			label={t('workspaces.description', language.current)}
			bind:value={editForm.description}
			placeholder={t('workspaces.descriptionPlaceholder', language.current)}
		/>
		<Input
			label={t('workspaces.maxCertificates', language.current)}
			type="number"
			bind:value={editForm.maxCertificates}
		/>
		<label class="checkbox-field">
			<input type="checkbox" bind:checked={editForm.isPublic} />
			<span>{t('workspaces.isPublic', language.current)}</span>
		</label>
		<label class="checkbox-field">
			<input type="checkbox" bind:checked={editForm.allowMemberInvites} />
			<span>{t('workspaces.allowMemberInvites', language.current)}</span>
		</label>
		<label class="checkbox-field">
			<input type="checkbox" bind:checked={editForm.autoDeleteExpiredCertificates} />
			<span>{t('workspaces.autoDeleteExpired', language.current)}</span>
		</label>
		<label class="checkbox-field">
			<input type="checkbox" bind:checked={editForm.ocspMonitoringEnabled} />
			<span>{t('workspaces.ocspMonitoring', language.current)}</span>
		</label>
		<div class="modal-form__actions">
			<Button type="button" variant="secondary" onclick={() => { showEditModal = false; }}>
				{t('common.cancel', language.current)}
			</Button>
			<Button type="submit" loading={isProcessing}>
				{t('common.save', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<style>
	.page {
		padding: var(--space-6);
		max-width: 1400px;
		margin: 0 auto;
	}

	.loading-state {
		display: flex;
		flex-direction: column;
		gap: var(--space-6);
	}

	.skeleton {
		background: linear-gradient(90deg, var(--color-surface-elevated) 25%, var(--color-border) 50%, var(--color-surface-elevated) 75%);
		background-size: 200% 100%;
		animation: shimmer 1.5s infinite;
		border-radius: var(--radius-lg);
	}

	.skeleton--header {
		height: 120px;
	}

	.skeleton--card {
		height: 100px;
	}

	@keyframes shimmer {
		0% { background-position: 200% 0; }
		100% { background-position: -200% 0; }
	}

	.error-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
		padding: var(--space-12);
		color: var(--color-text-muted);
	}

	.error-state svg {
		opacity: 0.4;
		margin-bottom: var(--space-4);
	}

	.error-state h3 {
		font-size: var(--text-xl);
		font-weight: 600;
		color: var(--color-text);
		margin: 0 0 var(--space-2);
	}

	.error-state p {
		margin: 0 0 var(--space-6);
	}

	.page__header {
		margin-bottom: var(--space-6);
	}

	.breadcrumb {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		font-size: var(--text-sm);
		color: var(--color-text-muted);
		margin-bottom: var(--space-4);
	}

	.breadcrumb__link {
		display: flex;
		align-items: center;
		gap: var(--space-1);
		color: inherit;
		text-decoration: none;
		transition: color 0.15s ease;
	}

	.breadcrumb__link:hover {
		color: var(--color-primary);
	}

	.breadcrumb__separator {
		opacity: 0.5;
	}

	.breadcrumb__current {
		color: var(--color-text);
		font-weight: 500;
	}

	.page__title-row {
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		gap: var(--space-4);
		flex-wrap: wrap;
	}

	.page__title {
		font-family: var(--font-display);
		font-size: var(--text-3xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: var(--leading-tight);
		color: var(--color-text);
		margin: 0;
	}

	.page__subtitle {
		color: var(--color-text-secondary);
		margin: var(--space-2) 0 0;
		max-width: 60ch;
	}

	.page__actions {
		display: flex;
		gap: var(--space-3);
		flex-wrap: wrap;
	}

	.stats-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
		gap: var(--space-4);
		margin-bottom: var(--space-8);
	}

	.stat-card {
		display: flex;
		align-items: center;
		gap: var(--space-4);
		padding: var(--space-5);
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		transition: border-color 0.15s ease, box-shadow 0.15s ease;
	}

	.stat-card:hover {
		border-color: var(--color-border-hover);
		box-shadow: var(--shadow-sm);
	}

	.stat-card__icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 48px;
		height: 48px;
		background: var(--color-surface-elevated);
		border-radius: var(--radius-md);
		color: var(--color-text-muted);
	}

	.stat-card__content {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
	}

	.stat-card__label {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
	}

	.stat-card__value {
		font-family: var(--font-display);
		font-size: var(--text-2xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		color: var(--color-text);
	}

	.stat-card__value--text {
		font-size: var(--text-base);
		max-width: 150px;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.stat-card__max {
		font-size: var(--text-sm);
		font-weight: 400;
		color: var(--color-text-muted);
	}

	.content-grid {
		display: grid;
		grid-template-columns: 1fr 380px;
		gap: var(--space-6);
	}

	.section {
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		padding: var(--space-5);
	}

	.section--main {
		min-height: 300px;
	}

	.section--danger {
		background: var(--color-error-bg);
		border-color: color-mix(in srgb, var(--color-error) 30%, transparent);
	}

	.section__header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: var(--space-4);
	}

	.section__title {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		font-family: var(--font-display);
		font-size: var(--text-lg);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		color: var(--color-text);
		margin: 0;
	}

	.section__title svg {
		color: var(--color-text-muted);
	}

	.section__title--danger {
		color: var(--color-error);
	}

	.section__title--danger svg {
		color: var(--color-error);
	}

	.section__text {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
		margin: 0 0 var(--space-4);
	}

	.section__footer {
		margin-top: var(--space-4);
		padding-top: var(--space-4);
		border-top: 1px solid var(--color-border);
	}

	.count-badge {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 24px;
		height: 24px;
		padding: 0 var(--space-2);
		background: var(--color-surface-elevated);
		border-radius: var(--radius-full);
		font-size: var(--text-xs);
		font-weight: 600;
		color: var(--color-text-muted);
	}

	.table-container {
		overflow-x: auto;
	}

	.table {
		width: 100%;
		border-collapse: collapse;
	}

	.table th {
		padding: var(--space-3);
		text-align: left;
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text-muted);
		border-bottom: 1px solid var(--color-border);
	}

	.table td {
		padding: var(--space-3);
		font-size: var(--text-sm);
		border-bottom: 1px solid var(--color-border);
	}

	.table__row--clickable {
		cursor: pointer;
		transition: background-color 0.15s ease;
	}

	.table__row--clickable:hover {
		background: var(--color-surface-elevated);
	}

	.cert-name {
		display: flex;
		align-items: center;
		gap: var(--space-2);
	}

	.cert-name svg {
		color: var(--color-text-muted);
		flex-shrink: 0;
	}

	.cert-name span {
		max-width: 200px;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.date-display {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
	}

	.date-display small {
		color: var(--color-text-muted);
	}

	.badge {
		display: inline-flex;
		align-items: center;
		gap: var(--space-1);
		padding: var(--space-1) var(--space-2);
		border-radius: var(--radius-full);
		font-size: var(--text-xs);
		font-weight: 500;
	}

	.badge--warning {
		background: var(--color-warning-bg);
		color: var(--color-warning);
	}

	.empty-section {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: var(--space-8);
		color: var(--color-text-muted);
		text-align: center;
	}

	.empty-section svg {
		opacity: 0.4;
		margin-bottom: var(--space-3);
		color: var(--color-success);
	}

	.empty-section p {
		margin: 0;
	}

	.sidebar {
		display: flex;
		flex-direction: column;
		gap: var(--space-6);
	}

	.members-list {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
		max-height: 400px;
		overflow-y: auto;
	}

	.member-item {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: var(--space-3);
		background: var(--color-surface-elevated);
		border-radius: var(--radius-md);
		gap: var(--space-3);
	}

	.member-item__info {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		min-width: 0;
	}

	.member-item__details {
		display: flex;
		flex-direction: column;
		min-width: 0;
	}

	.member-item__name {
		font-size: var(--text-sm);
		font-weight: 600;
		color: var(--color-text);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.member-item__email {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.member-item__actions {
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		gap: var(--space-1);
	}

	.role-badge {
		padding: var(--space-1) var(--space-2);
		border-radius: var(--radius-sm);
		font-size: var(--text-xs);
		font-weight: 500;
	}

	.role-badge--owner {
		background: var(--color-error-bg);
		color: var(--color-error);
	}

	.role-badge--admin {
		background: var(--color-primary-alpha);
		color: var(--color-primary);
	}

	.role-badge--editor {
		background: var(--color-warning-bg);
		color: var(--color-warning);
	}

	.role-badge--viewer {
		background: var(--color-surface-elevated);
		color: var(--color-text-muted);
	}

	.remove-btn {
		background: none;
		border: none;
		padding: 0;
		font-size: var(--text-xs);
		color: var(--color-error);
		cursor: pointer;
		text-decoration: none;
	}

	.remove-btn:hover {
		text-decoration: underline;
	}

	.danger-actions {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
	}

	.full-width {
		width: 100%;
	}

	.modal-form {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.modal-form__actions {
		display: flex;
		justify-content: flex-end;
		gap: var(--space-3);
		padding-top: var(--space-4);
		border-top: 1px solid var(--color-border);
		margin-top: var(--space-2);
	}

	.modal-text {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
		margin: 0;
		line-height: 1.6;
	}

	.danger-text {
		color: var(--color-error);
	}

	.alert {
		padding: var(--space-3);
		border-radius: var(--radius-md);
		font-size: var(--text-sm);
	}

	.alert--error {
		background: var(--color-error-bg);
		color: var(--color-error);
		border: 1px solid var(--color-error);
	}

	@media (max-width: 1024px) {
		.content-grid {
			grid-template-columns: 1fr;
		}

		.sidebar {
			order: -1;
		}
	}

	@media (max-width: 768px) {
		.page {
			padding: var(--space-4);
		}

		.page__title-row {
			flex-direction: column;
		}

		.page__actions {
			width: 100%;
		}

		.page__actions :global(button) {
			flex: 1;
		}

		.stats-grid {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	@media (max-width: 480px) {
		.stats-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
