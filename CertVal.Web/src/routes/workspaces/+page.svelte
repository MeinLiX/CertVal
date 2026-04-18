<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { WorkspaceService } from '$lib/services/WorkspaceService';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import FloatingActionBar from '$lib/components/layout/FloatingActionBar.svelte';
	import type { Workspace, CreateWorkspaceRequest } from '$lib/types';

	let workspaces = $state<Workspace[]>([]);
	let isLoading = $state(true);
	let showCreateModal = $state(false);
	let isCreating = $state(false);
	let searchQuery = $state('');

	let createForm = $state<CreateWorkspaceRequest>({
		name: '',
		description: '',
		maxCertificates: 1000,
		isPublic: false,
		allowMemberInvites: true
	});
	let errors = $state<Record<string, string>>({});

	const filteredWorkspaces = $derived(
		workspaces.filter(
			(w) =>
				w.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
				w.description?.toLowerCase().includes(searchQuery.toLowerCase())
		)
	);

	onMount(async () => {
		if (!userSession.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadWorkspaces();
	});

	$effect(() => {
		const action = page.url.searchParams.get('action');
		if (action === 'create') {
			openCreateModal();
			const newUrl = new URL(window.location.href);
			newUrl.searchParams.delete('action');
			goto(newUrl.toString(), { replaceState: true, keepFocus: true, noScroll: true });
		}
	});

	async function loadWorkspaces() {
		isLoading = true;
		try {
			const response = await WorkspaceService.getAll();
			if (response.data) {
				workspaces = response.data.items;
			}
		} catch (error) {
			console.error('Failed to load workspaces:', error);
		} finally {
			isLoading = false;
		}
	}

	async function handleCreateWorkspace(event: Event) {
		event.preventDefault();
		errors = {};
		isCreating = true;
		try {
			const response = await WorkspaceService.create(createForm);
			if (response.data) {
				workspaces = [...workspaces, response.data];
				showCreateModal = false;
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', language.current);
		} finally {
			isCreating = false;
		}
	}

	function openCreateModal() {
		createForm = {
			name: '',
			description: '',
			maxCertificates: 1000,
			isPublic: false,
			allowMemberInvites: true
		};
		errors = {};
		showCreateModal = true;
	}
</script>

<svelte:head>
	<title>{t('workspaces.title', language.current)}</title>
</svelte:head>

<div class="page" data-test-id="workspaces-page">
	<!-- Content -->
	{#if isLoading}
		<div class="page__loading">
			<GlobalLoader variant="inline" />
		</div>
	{:else if filteredWorkspaces.length > 0}
		<div class="workspace-grid">
			{#each filteredWorkspaces as workspace (workspace.id)}
				<Card
					clickable
					onclick={() => goto(`/workspaces/${workspace.id}`)}
					data-test-id={`workspace-card-${workspace.id}`}
				>
					<div class="workspace-card">
						<div class="workspace-card__header">
							<h3 class="workspace-card__title" title={workspace.name}>{workspace.name}</h3>
							<span class="workspace-card__badge workspace-card__badge--{workspace.isPublic ? 'public' : 'private'}">
								<Icon name={workspace.isPublic ? 'users' : 'lock'} size="sm" />
								{workspace.isPublic ? t('common.public', language.current) : t('common.private', language.current)}
							</span>
						</div>

						<p class="workspace-card__desc">
							{workspace.description || t('common.noDescription', language.current)}
						</p>

						<div class="workspace-card__stats">
							<div class="workspace-stat">
								<span class="workspace-stat__label">
									<Icon name="certificates" size="sm" />
									{t('workspaces.certificates', language.current)}
								</span>
								<span class="workspace-stat__value">
									{workspace.certificateCount}
									<span class="workspace-stat__max">/ {workspace.maxCertificates}</span>
								</span>
							</div>
							<div class="workspace-stat">
								<span class="workspace-stat__label">
									<Icon name="users" size="sm" />
									{t('workspaces.members', language.current)}
								</span>
								<span class="workspace-stat__value">{workspace.memberCount}</span>
							</div>
						</div>

						<div class="workspace-card__footer">
							<div class="workspace-card__status">
								<span class="status-dot status-dot--{workspace.allowMemberInvites ? 'active' : 'inactive'}"></span>
								{workspace.allowMemberInvites
									? t('workspaces.invitesAllowed', language.current)
									: t('workspaces.invitesDisabled', language.current)}
							</div>
							<Button
								variant="ghost"
								size="sm"
								onclick={() => goto(`/workspaces/${workspace.id}`)}
								data-test-id={`view-workspace-${workspace.id}`}
							>
								{t('common.view', language.current)}
								<Icon name="rightArrow" size="sm" />
							</Button>
						</div>
					</div>
				</Card>
			{/each}
		</div>
	{:else}
		<div class="empty-state">
			<div class="empty-state__icon">
				<Icon name="workspaces" />
			</div>
			<h3 class="empty-state__title">{t('workspaces.empty', language.current)}</h3>
			<p class="empty-state__text">
				{searchQuery ? t('common.noResults', language.current) : ''}
			</p>
			{#if !searchQuery}
				<Button
					variant="primary"
					onclick={openCreateModal}
					data-test-id="empty-state-create-workspace-button"
				>
					{t('workspaces.createFirst', language.current)}
				</Button>
			{/if}
		</div>
	{/if}

	<FloatingActionBar label={t('workspaces.title', language.current)}>
		{#snippet leading()}
			<Input
				type="search"
				placeholder={t('common.search', language.current)}
				bind:value={searchQuery}
				icon="search"
				data-test-id="search-workspace-input"
			/>
		{/snippet}
		{#snippet trailing()}
			<Button
				variant="primary"
				onclick={openCreateModal}
				data-test-id="create-workspace-button"
			>
				<Icon name="plus" size="sm" />
				{t('workspaces.create', language.current)}
			</Button>
		{/snippet}
	</FloatingActionBar>
</div>

<!-- Create Modal -->
<Modal
	isOpen={showCreateModal}
	title={t('workspaces.create', language.current)}
	onClose={() => (showCreateModal = false)}
	data-test-id="create-workspace-modal"
>
	<form onsubmit={handleCreateWorkspace} class="modal-form">
		<Input
			label={t('workspaces.name', language.current)}
			bind:value={createForm.name}
			error={errors.name}
			required
			placeholder={t('workspaces.namePlaceholder', language.current)}
			data-test-id="create-workspace-name-input"
		/>

		<Input
			label={t('workspaces.description', language.current)}
			bind:value={createForm.description}
			error={errors.description}
			placeholder={t('workspaces.descriptionPlaceholder', language.current)}
			data-test-id="create-workspace-description-input"
		/>

		<Input
			label={t('workspaces.maxCertificates', language.current)}
			type="number"
			bind:value={createForm.maxCertificates}
			error={errors.maxCertificates}
			data-test-id="create-workspace-max-certs-input"
		/>

		<label class="checkbox-field">
			<input
				type="checkbox"
				bind:checked={createForm.isPublic}
				data-test-id="create-workspace-public-checkbox"
			/>
			<span>{t('workspaces.isPublic', language.current)}</span>
		</label>

		<label class="checkbox-field">
			<input
				type="checkbox"
				bind:checked={createForm.allowMemberInvites}
				data-test-id="create-workspace-invites-checkbox"
			/>
			<span>{t('workspaces.allowMemberInvites', language.current)}</span>
		</label>

		{#if errors.general}
			<div class="form-error">
				<Icon name="alert" size="sm" />
				<span>{errors.general}</span>
			</div>
		{/if}

		<div class="modal-form__actions">
			<Button
				variant="ghost"
				onclick={() => { showCreateModal = false; }}
				disabled={isCreating}
				data-test-id="create-workspace-cancel-button"
			>
				{t('common.cancel', language.current)}
			</Button>
			<Button
				type="submit"
				variant="primary"
				loading={isCreating}
				data-test-id="create-workspace-submit-button"
			>
				{t('common.create', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<style>
	.page {
		animation: fadeIn 0.5s ease-out;
	}

	@keyframes fadeIn {
		from { opacity: 0; }
		to { opacity: 1; }
	}

	.page__loading {
		display: flex;
		justify-content: center;
		padding: var(--space-12);
	}

	/* Workspace Grid */
	.workspace-grid {
		display: grid;
		grid-template-columns: 1fr;
		gap: var(--space-6);
	}

	@media (min-width: 768px) {
		.workspace-grid { grid-template-columns: repeat(2, 1fr); }
	}

	@media (min-width: 1024px) {
		.workspace-grid { grid-template-columns: repeat(3, 1fr); }
	}

	/* Workspace Card */
	.workspace-card {
		display: flex;
		flex-direction: column;
		height: 100%;
	}

	.workspace-card__header {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: var(--space-3);
		margin-bottom: var(--space-4);
	}

	.workspace-card__title {
		font-family: var(--font-display);
		font-size: var(--text-lg);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		color: var(--color-text);
		margin: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.workspace-card__badge {
		display: inline-flex;
		align-items: center;
		gap: var(--space-1);
		padding: var(--space-1) var(--space-2);
		font-size: var(--text-xs);
		border-radius: var(--radius-full);
		white-space: nowrap;
	}

	.workspace-card__badge--public {
		background-color: var(--color-primary-light);
		color: var(--color-primary);
	}

	.workspace-card__badge--private {
		background-color: var(--color-surface-hover);
		color: var(--color-text-muted);
	}

	.workspace-card__desc {
		color: var(--color-text-secondary);
		font-size: var(--text-sm);
		margin: 0 0 var(--space-6) 0;
		flex-grow: 1;
		display: -webkit-box;
		line-clamp: 2;
		-webkit-line-clamp: 2;
		-webkit-box-orient: vertical;
		overflow: hidden;
	}

	.workspace-card__stats {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: var(--space-4);
		margin-bottom: var(--space-4);
	}

	.workspace-stat {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
		padding: var(--space-2);
		background-color: var(--color-surface-hover);
		border-radius: var(--radius-lg);
	}

	.workspace-stat__label {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.workspace-stat__value {
		font-weight: var(--font-semibold);
		color: var(--color-text);
	}

	.workspace-stat__max {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.workspace-card__footer {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding-top: var(--space-4);
		border-top: 1px solid var(--color-border);
	}

	.workspace-card__status {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.status-dot {
		width: 8px;
		height: 8px;
		border-radius: var(--radius-full);
	}

	.status-dot--active { background-color: var(--color-success); }
	.status-dot--inactive { background-color: var(--color-border); }

	/* Empty State */
	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
		padding: var(--space-16) var(--space-6);
		background-color: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
	}

	.empty-state__icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 56px;
		height: 56px;
		border-radius: var(--radius-full);
		background-color: var(--color-surface-inset);
		color: var(--color-text-secondary);
		margin-bottom: var(--space-5);
	}

	.empty-state__title {
		font-family: var(--font-display);
		font-size: var(--text-xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		color: var(--color-text);
		margin: 0 0 var(--space-2) 0;
	}

	.empty-state__text {
		color: var(--color-text-secondary);
		margin: 0 0 var(--space-6) 0;
		max-width: 48ch;
	}

	/* Modal Form */
	.modal-form {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.checkbox-field {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		cursor: pointer;
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text);
	}

	.checkbox-field input {
		width: 18px;
		height: 18px;
		accent-color: var(--color-primary);
	}

	.form-error {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-3) var(--space-4);
		background-color: var(--color-error-light);
		border: 1px solid var(--color-error);
		border-radius: var(--radius-md);
		color: var(--color-error);
		font-size: var(--text-sm);
	}

	.modal-form__actions {
		display: flex;
		justify-content: flex-end;
		gap: var(--space-3);
		padding-top: var(--space-4);
		border-top: 1px solid var(--color-border);
		margin-top: var(--space-2);
	}
</style>
