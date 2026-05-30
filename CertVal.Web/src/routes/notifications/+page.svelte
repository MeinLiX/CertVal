<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { NotificationService } from '$lib/services/NotificationService';
	import { WorkspaceService } from '$lib/services/WorkspaceService';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import ConfirmDeleteModal from '$lib/components/ui/ConfirmDeleteModal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import FloatingActionBar from '$lib/components/layout/FloatingActionBar.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type {
		CreateNotificationRuleRequest,
		NotificationHistory,
		NotificationRule,
		Workspace,
		WorkspaceMember
	} from '$lib/types';

	let workspaces = $state<Workspace[]>([]);
	let selectedWorkspaceId = $state<string>('');
	let workspaceMembers = $state<WorkspaceMember[]>([]);
	let rules = $state<NotificationRule[]>([]);
	let history = $state<NotificationHistory[]>([]);

	let isLoading = $state(true);
	let isProcessing = $state(false);

	let showCreateModal = $state(false);
	let showHistoryModal = $state(false);
	let showConfirmModal = $state(false);
	let pendingDeleteRuleId = $state<string | null>(null);

	let createForm = $state<CreateNotificationRuleRequest & { recipientUserIds: string[] }>({
		name: '',
		daysBeforeExpiry: 30,
		channelType: 'Email',
		channelConfig: '{}',
		frequency: 'Once',
		recipientUserIds: [],
		recipientAggregationMode: 'SingleEmailToAll'
	});
	let webhookUrl = $state('');
	let errors = $state<Record<string, string>>({});

	onMount(async () => {
		if (!userSession.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		const urlWorkspaceId = page.url.searchParams.get('workspace');
		await loadWorkspaces(urlWorkspaceId);
	});

	async function loadWorkspaces(defaultId: string | null) {
		isLoading = true;
		try {
			const response = await WorkspaceService.getAll(1, 100);
			if (response.data) {
				workspaces = response.data.items;
				if (workspaces.length > 0) {
					if (defaultId && workspaces.some((w) => w.id === defaultId)) {
						selectedWorkspaceId = defaultId;
					} else {
						selectedWorkspaceId = workspaces[0].id;
					}
					await loadRulesForWorkspace();
				}
			}
		} catch (error) {
			console.error('Failed to load workspaces:', error);
		} finally {
			isLoading = false;
		}
	}

	async function loadWorkspaceMembers() {
		if (!selectedWorkspaceId) return;
		try {
			const response = await WorkspaceService.getMembers(selectedWorkspaceId);
			if (response.data) {
				workspaceMembers = response.data;
			}
		} catch (error) {
			console.error('Failed to load members:', error);
		}
	}

	async function loadRulesForWorkspace() {
		if (!selectedWorkspaceId) return;
		isLoading = true;
		try {
			await loadWorkspaceMembers();
			const response = await NotificationService.getRules(selectedWorkspaceId);
			if (response.data) {
				rules = response.data;
			}
		} catch (error) {
			console.error('Failed to load rules:', error);
		} finally {
			isLoading = false;
		}
	}

	async function loadHistory() {
		if (!selectedWorkspaceId) return;
		isProcessing = true;
		try {
			const response = await NotificationService.getHistory(selectedWorkspaceId);
			if (response.data) {
				history = response.data;
				showHistoryModal = true;
			}
		} catch (error) {
			console.error('Failed to load history:', error);
		} finally {
			isProcessing = false;
		}
	}

	async function handleCreateRule(event: Event) {
		event.preventDefault();
		if (!selectedWorkspaceId) return;
		isProcessing = true;
		errors = {};

		if (createForm.channelType === 'Webhook') {
			createForm.channelConfig = JSON.stringify({ url: webhookUrl });
		}

		try {
			const response = await NotificationService.createRule(selectedWorkspaceId, createForm);
			if (response.data) {
				rules = [...rules, response.data];
				showCreateModal = false;
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', language.current);
		} finally {
			isProcessing = false;
		}
	}

	async function handleDeleteRule(id: string) {
		pendingDeleteRuleId = id;
		showConfirmModal = true;
	}

	async function confirmDelete() {
		if (!selectedWorkspaceId || !pendingDeleteRuleId) return;
		isProcessing = true;
		try {
			await NotificationService.deleteRule(selectedWorkspaceId, pendingDeleteRuleId);
			rules = rules.filter((r) => r.id !== pendingDeleteRuleId);
			showConfirmModal = false;
			pendingDeleteRuleId = null;
		} catch (error) {
			console.error('Failed to delete rule:', error);
		} finally {
			isProcessing = false;
		}
	}

	function openCreateModal() {
		createForm = {
			name: '',
			daysBeforeExpiry: 30,
			channelType: 'Email',
			channelConfig: '{}',
			frequency: 'Once',
			recipientUserIds: [],
			recipientAggregationMode: 'SingleEmailToAll'
		};
		webhookUrl = '';
		errors = {};
		showCreateModal = true;
	}
</script>

<svelte:head>
	<title>{t('notifications.title', language.current)}</title>
</svelte:head>

<div class="page">
	{#if isLoading}
		<div class="rules-grid">
			{#each Array(6) as _}
				<div class="skeleton-card">
					<div class="skeleton skeleton--icon"></div>
					<div class="skeleton skeleton--title"></div>
					<div class="skeleton skeleton--text"></div>
				</div>
			{/each}
		</div>
	{:else if workspaces.length === 0}
		<div class="empty-state">
			<Icon name="workspaces" size="lg" />
			<h3 class="empty-state__title">{t('notifications.noWorkspaces', language.current)}</h3>
			<p class="empty-state__text">{t('notifications.createFirstWorkspace', language.current)}</p>
			<Button onclick={() => goto('/workspaces')}>{t('nav.workspaces', language.current)}</Button>
		</div>
	{:else if rules.length === 0}
		<div class="empty-state">
			<Icon name="notifications" size="lg" />
			<h3 class="empty-state__title">{t('notifications.empty.title', language.current)}</h3>
			<p class="empty-state__text">{t('notifications.empty.description', language.current)}</p>
			<Button variant="secondary" onclick={openCreateModal}>
				{t('notifications.create', language.current)}
			</Button>
		</div>
	{:else}
		<div class="rules-grid">
			{#each rules as rule (rule.id)}
				<div class="rule-card">
					<div class="rule-card__head">
						<div class="rule-card__icon">
							<Icon name="notifications" size="sm" />
						</div>
						<h3 class="rule-card__title">{rule.name}</h3>
						<button class="rule-card__delete" aria-label="Delete rule" onclick={() => handleDeleteRule(rule.id)}>
							<Icon name="trash" size="sm" />
						</button>
					</div>
					<dl class="rule-card__meta">
						<div class="rule-card__row">
							<dt>{t('notifications.daysBefore', language.current)}</dt>
							<dd><span class="rule-card__value rule-card__value--accent">{rule.daysBeforeExpiry}</span></dd>
						</div>
						<div class="rule-card__row">
							<dt>{t('notifications.frequency', language.current)}</dt>
							<dd>{rule.frequency}</dd>
						</div>
						<div class="rule-card__row">
							<dt>{t('notifications.channel', language.current)}</dt>
							<dd><span class="channel-badge">{rule.channelType}</span></dd>
						</div>
					</dl>
				</div>
			{/each}
		</div>
	{/if}

	<FloatingActionBar label={t('notifications.title', language.current)}>
		{#snippet leading()}
			{#if workspaces.length > 0}
				<label class="toolbar-field">
					<span class="toolbar-field__label">{t('common.workspace', language.current)}</span>
					<select
						class="toolbar-field__select"
						bind:value={selectedWorkspaceId}
						onchange={loadRulesForWorkspace}
					>
						{#each workspaces as workspace}
							<option value={workspace.id}>{workspace.name}</option>
						{/each}
					</select>
				</label>
			{/if}
		{/snippet}
		{#snippet trailing()}
			<Button variant="secondary" onclick={loadHistory} disabled={!selectedWorkspaceId}>
				{t('notifications.history', language.current)}
			</Button>
			<Button onclick={openCreateModal} disabled={!selectedWorkspaceId}>
				+ {t('notifications.create', language.current)}
			</Button>
		{/snippet}
	</FloatingActionBar>
</div>

<Modal bind:isOpen={showCreateModal} title={t('notifications.create', language.current)} onclose={() => (showCreateModal = false)}>
	<form class="modal-form" onsubmit={handleCreateRule}>
		{#if errors.general}
			<div class="alert alert--error">{errors.general}</div>
		{/if}

		<Input
			label={t('common.name', language.current)}
			bind:value={createForm.name}
			required
			placeholder="e.g. 30 Days Warning"
		/>

		<Input
			type="number"
			label={t('notifications.daysBefore', language.current)}
			bind:value={createForm.daysBeforeExpiry}
			required
		/>

		<Select
			label={t('notifications.frequency', language.current)}
			bind:value={createForm.frequency}
			options={[
				{ value: 'Once', label: 'Once' },
				{ value: 'Daily', label: 'Daily' },
				{ value: 'Weekly', label: 'Weekly' }
			]}
		/>

		<div class="form-group">
			<label class="form-label" for="channel">{t('notifications.channel', language.current)}</label>
			<select id="channel" class="select" bind:value={createForm.channelType}>
				<option value="Email">Email</option>
				<option value="Webhook">Webhook</option>
			</select>
		</div>

		{#if createForm.channelType === 'Webhook'}
			<Input
				label="Webhook URL"
				bind:value={webhookUrl}
				required
				placeholder="https://api.example.com/webhook"
			/>
		{/if}

		{#if createForm.channelType === 'Email'}
			<div class="form-group">
				<span class="form-label">{t('notifications.recipients', language.current)}</span>
				<div class="recipients-list" role="group" aria-label="Recipients">
					{#if workspaceMembers.length === 0}
						<p class="recipients-list__empty">No members found.</p>
					{:else}
						{#each workspaceMembers as member}
							<label class="recipient-item">
								<input
									type="checkbox"
									class="checkbox"
									value={member.userId}
									bind:group={createForm.recipientUserIds}
								/>
								<div class="recipient-item__info">
									<span class="recipient-item__name">{member.user?.fullName || 'Unknown User'}</span>
									<span class="recipient-item__email">{member.user?.email || ''}</span>
								</div>
							</label>
						{/each}
					{/if}
				</div>
			</div>

			{#if createForm.recipientUserIds.length > 1}
				<Select
					label={t('notifications.recipientAggregation', language.current)}
					bind:value={createForm.recipientAggregationMode}
					options={[
						{ value: 'SingleEmailToAll', label: t('notifications.aggregationAllOption', language.current) },
						{ value: 'Individual', label: t('notifications.aggregationIndividualOption', language.current) }
					]}
				/>
			{/if}
		{/if}

		<div class="modal-form__actions">
			<Button type="button" variant="secondary" onclick={() => { showCreateModal = false; }} disabled={isProcessing}>
				{t('common.cancel', language.current)}
			</Button>
			<Button type="submit" loading={isProcessing}>
				{t('common.create', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<ConfirmDeleteModal
	isOpen={showConfirmModal}
	itemName={rules.find((r) => r.id === pendingDeleteRuleId)?.name || ''}
	onConfirm={confirmDelete}
	onClose={() => {
		pendingDeleteRuleId = null;
		showConfirmModal = false;
	}}
/>

<Modal bind:isOpen={showHistoryModal} title={t('notifications.history', language.current)} onclose={() => (showHistoryModal = false)}>
	<div class="history-list">
		{#if isProcessing}
			<div class="history-list__loading">
				<div class="spinner"></div>
			</div>
		{:else if history.length === 0}
			<p class="history-list__empty">{t('notifications.noHistory', language.current)}</p>
		{:else}
			{#each history as item}
				<div class="history-item">
					<div class="history-item__header">
						<span class="history-item__subject">{item.subject}</span>
						<span class="history-item__status history-item__status--{item.status.toLowerCase()}">{item.status}</span>
					</div>
					<div class="history-item__meta">
						<span>{formatDateTime(item.createdAt)}</span>
						<span>{t('common.to', language.current)} {item.recipient}</span>
					</div>
				</div>
			{/each}
		{/if}
	</div>
	<div class="modal-form__actions">
		<Button onclick={() => { showHistoryModal = false; }}>{t('common.close', language.current)}</Button>
	</div>
</Modal>

<style>
	.page {
		animation: fadeIn 0.5s ease-out;
	}

	@keyframes fadeIn {
		from { opacity: 0; }
		to { opacity: 1; }
	}

	.toolbar-field {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		min-width: 0;
	}

	.toolbar-field__label {
		font-family: var(--font-mono);
		font-size: var(--text-xs);
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
		color: var(--color-text-muted);
	}

	.toolbar-field__select {
		padding: var(--space-2) var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface);
		color: var(--color-text);
		font-size: var(--text-sm);
		font-family: var(--font-body);
		cursor: pointer;
		min-width: 180px;
		transition: border-color var(--transition-fast);
	}

	.toolbar-field__select:hover {
		border-color: var(--color-border-hover);
	}

	.toolbar-field__select:focus {
		outline: none;
		border-color: var(--color-primary);
		box-shadow: 0 0 0 3px var(--color-primary-alpha);
	}

	.form-group {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.form-label {
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text);
	}

	.select {
		padding: var(--space-2) var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface);
		color: var(--color-text);
		font-size: var(--text-sm);
		cursor: pointer;
		transition: border-color var(--transition-fast);
	}

	.select:hover {
		border-color: var(--color-border-hover);
	}

	.select:focus {
		outline: none;
		border-color: var(--color-primary);
		box-shadow: 0 0 0 3px var(--color-primary-alpha);
	}

	.rules-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
		gap: var(--space-3);
	}

	.skeleton-card {
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		padding: var(--space-5);
	}

	.skeleton {
		background: linear-gradient(90deg, var(--color-surface-elevated) 25%, var(--color-border) 50%, var(--color-surface-elevated) 75%);
		background-size: 200% 100%;
		animation: shimmer 1.5s infinite;
		border-radius: var(--radius-sm);
	}

	.skeleton--icon {
		width: 48px;
		height: 48px;
		border-radius: var(--radius-md);
		margin-bottom: var(--space-4);
	}

	.skeleton--title {
		height: 24px;
		width: 70%;
		margin-bottom: var(--space-3);
	}

	.skeleton--text {
		height: 16px;
		width: 50%;
	}

	@keyframes shimmer {
		0% { background-position: 200% 0; }
		100% { background-position: -200% 0; }
	}

	.empty-state {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
		padding: var(--space-12);
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xl);
		color: var(--color-text-muted);
	}

	.empty-state__title {
		font-family: var(--font-display);
		font-size: var(--text-xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		color: var(--color-text);
		margin: 0 0 var(--space-2);
	}

	.empty-state__text {
		margin: 0 0 var(--space-6);
		max-width: 400px;
	}

	.rule-card {
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		padding: var(--space-4);
		transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
	}

	.rule-card:hover {
		border-color: var(--color-border-hover);
		box-shadow: var(--shadow-sm);
	}

	.rule-card__head {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		margin-bottom: var(--space-3);
	}

	.rule-card__icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 32px;
		height: 32px;
		background: var(--color-primary-light);
		border-radius: var(--radius-md);
		color: var(--color-primary);
		flex-shrink: 0;
	}

	.rule-card__title {
		flex: 1;
		font-family: var(--font-body);
		font-size: var(--text-base);
		font-weight: var(--font-semibold);
		color: var(--color-text);
		margin: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.rule-card__delete {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 28px;
		height: 28px;
		background: none;
		border: none;
		border-radius: var(--radius-sm);
		color: var(--color-text-muted);
		cursor: pointer;
		flex-shrink: 0;
		transition: color var(--transition-fast), background-color var(--transition-fast);
	}

	.rule-card__delete:hover {
		color: var(--color-error);
		background: var(--color-error-bg);
	}

	.rule-card__meta {
		display: grid;
		grid-template-columns: 1fr 1fr 1fr;
		gap: var(--space-2);
		margin: 0;
		padding-top: var(--space-3);
		border-top: 1px solid var(--color-border);
	}

	.rule-card__row {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
		min-width: 0;
	}

	.rule-card__row dt {
		font-family: var(--font-mono);
		font-size: var(--text-xs);
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
		color: var(--color-text-muted);
	}

	.rule-card__row dd {
		margin: 0;
		font-size: var(--text-sm);
		color: var(--color-text);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.rule-card__value--accent {
		font-family: var(--font-display);
		font-size: var(--text-lg);
		font-weight: var(--font-semibold);
		color: var(--color-primary);
	}

	.channel-badge {
		display: inline-flex;
		padding: var(--space-1) var(--space-2);
		background: var(--color-surface-elevated);
		border-radius: var(--radius-sm);
		font-size: var(--text-xs);
		font-weight: 500;
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

	.recipients-list {
		max-height: 200px;
		overflow-y: auto;
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		padding: var(--space-2);
	}

	.recipients-list__empty {
		color: var(--color-text-muted);
		font-size: var(--text-sm);
		padding: var(--space-2);
		margin: 0;
	}

	.recipient-item {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		padding: var(--space-2);
		border-radius: var(--radius-sm);
		cursor: pointer;
		transition: background-color 0.15s ease;
	}

	.recipient-item:hover {
		background: var(--color-surface-elevated);
	}

	.checkbox {
		width: 16px;
		height: 16px;
		accent-color: var(--color-primary);
	}

	.recipient-item__info {
		display: flex;
		flex-direction: column;
	}

	.recipient-item__name {
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text);
	}

	.recipient-item__email {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.history-list {
		max-height: 400px;
		overflow-y: auto;
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.history-list__loading {
		display: flex;
		justify-content: center;
		padding: var(--space-8);
	}

	.spinner {
		width: 32px;
		height: 32px;
		border: 3px solid var(--color-border);
		border-top-color: var(--color-primary);
		border-radius: 50%;
		animation: spin 0.8s linear infinite;
	}

	@keyframes spin {
		to { transform: rotate(360deg); }
	}

	.history-list__empty {
		text-align: center;
		color: var(--color-text-muted);
		padding: var(--space-8);
		margin: 0;
	}

	.history-item {
		padding: var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		transition: background-color 0.15s ease;
	}

	.history-item:hover {
		background: var(--color-surface-elevated);
	}

	.history-item__header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: var(--space-1);
	}

	.history-item__subject {
		font-size: var(--text-sm);
		font-weight: 600;
		color: var(--color-text);
		max-width: 200px;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.history-item__status {
		padding: var(--space-1) var(--space-2);
		border-radius: var(--radius-sm);
		font-size: var(--text-xs);
		font-weight: 500;
	}

	.history-item__status--sent {
		background: var(--color-success-bg);
		color: var(--color-success);
	}

	.history-item__status--failed {
		background: var(--color-error-bg);
		color: var(--color-error);
	}

	.history-item__meta {
		display: flex;
		justify-content: space-between;
		font-size: var(--text-xs);
		color: var(--color-text-muted);
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

		.rules-grid {
			grid-template-columns: 1fr;
		}
	}

	@media (max-width: 640px) {
		/* Compact the workspace selector inside the mobile action dock */
		.toolbar-field__label {
			display: none;
		}

		.toolbar-field__select {
			min-width: 132px;
		}
	}
</style>
