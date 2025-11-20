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
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import ConfirmDeleteModal from '$lib/components/ui/ConfirmDeleteModal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import Loader from '$lib/components/ui/Loader.svelte';
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

	const selectedWorkspace = $derived(workspaces.find((w) => w.id === selectedWorkspaceId));

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

<div class="animate-in fade-in slide-in-from-bottom-4 space-y-8 duration-500">
	<div class="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
		<div>
			<h1
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-4xl font-bold tracking-tight text-transparent"
			>
				{t('notifications.title', language.current)}
			</h1>
			<p class="text-base-content/60 mt-2 text-lg font-light">
				{t('notifications.subtitle', language.current)}
			</p>
		</div>
		<div class="flex gap-2">
			<Button variant="outline" onclick={loadHistory}>
				<Icon name="document" class="mr-2 h-5 w-5" />
				{t('notifications.history', language.current)}
			</Button>
			<Button variant="primary" onclick={openCreateModal}>
				<Icon name="plus" class="mr-2 h-5 w-5" />
				{t('notifications.create', language.current)}
			</Button>
		</div>
	</div>

	<div
		class="bg-base-100/50 border-base-content/5 rounded-2xl border p-6 shadow-sm backdrop-blur-sm"
	>
		<div class="form-control w-full max-w-md">
			<label class="label" for="workspace-select">
				<span class="label-text font-medium">{t('common.workspace', language.current)}</span>
			</label>
			<select
				id="workspace-select"
				class="select select-bordered focus:select-primary w-full transition-all"
				bind:value={selectedWorkspaceId}
				onchange={loadRulesForWorkspace}
			>
				{#each workspaces as workspace}
					<option value={workspace.id}>{workspace.name}</option>
				{/each}
			</select>
		</div>
	</div>

	{#if isLoading}
		<div class="flex h-64 items-center justify-center">
			<Loader size="lg" />
		</div>
	{:else if rules.length === 0}
		<div
			class="bg-base-100/50 border-base-200 flex flex-col items-center justify-center rounded-3xl border py-20 text-center backdrop-blur-sm"
		>
			<div class="bg-base-200 mb-6 rounded-full p-6">
				<Icon name="notifications" class="text-base-content/30 h-12 w-12" />
			</div>
			<h3 class="mb-2 text-xl font-semibold">{t('notifications.empty.title', language.current)}</h3>
			<p class="text-base-content/60 mb-8 max-w-md">
				{t('notifications.empty.description', language.current)}
			</p>
			<Button variant="outline" onclick={openCreateModal}>
				{t('notifications.create', language.current)}
			</Button>
		</div>
	{:else}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each rules as rule (rule.id)}
				<Card
					variant="glass"
					class="border-base-content/5 hover:border-primary/20 group border transition-all duration-300"
				>
					<div class="mb-4 flex items-start justify-between">
						<div
							class="bg-primary/10 text-primary group-hover:bg-primary group-hover:text-primary-content rounded-xl p-3 transition-colors duration-300"
						>
							<Icon name="notifications" class="h-6 w-6" />
						</div>
						<div class="dropdown dropdown-end">
							<div tabindex="0" role="button" class="btn btn-ghost btn-sm btn-circle">
								<Icon name="settings" class="h-4 w-4" />
							</div>
							<ul
								tabindex="0"
								role="menu"
								class="dropdown-content menu bg-base-100 rounded-box z-[1] w-52 p-2 shadow"
							>
								<li>
									<button class="text-error" onclick={() => handleDeleteRule(rule.id)}
										>{t('common.delete', language.current)}</button
									>
								</li>
							</ul>
						</div>
					</div>

					<h3 class="mb-2 text-xl font-bold">{rule.name}</h3>
					<div class="text-base-content/70 space-y-2 text-sm">
						<div class="flex items-center gap-2">
							<span class="font-medium">{t('notifications.daysBefore', language.current)}:</span>
							<span>{rule.daysBeforeExpiry}</span>
						</div>
						<div class="flex items-center gap-2">
							<span class="font-medium">{t('notifications.frequency', language.current)}:</span>
							<span>{rule.frequency}</span>
						</div>
						<div class="flex items-center gap-2">
							<span class="font-medium">{t('notifications.channel', language.current)}:</span>
							<span class="badge badge-sm badge-outline">{rule.channelType}</span>
						</div>
					</div>
				</Card>
			{/each}
		</div>
	{/if}
</div>

<Modal
	isOpen={showCreateModal}
	title={t('notifications.create', language.current)}
	onClose={() => (showCreateModal = false)}
>
	<form onsubmit={handleCreateRule} class="space-y-4">
		{#if errors.general}
			<div role="alert" class="alert alert-error text-sm"><span>{errors.general}</span></div>
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

		<div class="form-control">
			<label class="label" for="channel">
				<span class="label-text font-medium">{t('notifications.channel', language.current)}</span>
			</label>
			<select
				id="channel"
				class="select select-bordered focus:select-primary w-full transition-all"
				bind:value={createForm.channelType}
			>
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
			<div class="form-control">
				<label class="label" for="recipients">
					<span class="label-text font-medium"
						>{t('notifications.recipients', language.current)}</span
					>
				</label>
				<div class="border-base-300 bg-base-100/50 max-h-40 overflow-y-auto rounded-lg border p-2">
					{#if workspaceMembers.length === 0}
						<p class="text-base-content/50 p-2 text-sm">No members found.</p>
					{:else}
						{#each workspaceMembers as member}
							<label
								class="label hover:bg-base-200/50 cursor-pointer justify-start gap-3 rounded p-2 transition-colors"
							>
								<input
									type="checkbox"
									class="checkbox checkbox-sm checkbox-primary"
									value={member.userId}
									bind:group={createForm.recipientUserIds}
								/>
								<div class="flex flex-col">
									<span class="label-text font-medium"
										>{member.user?.fullName || 'Unknown User'}</span
									>
									<span class="text-base-content/60 text-xs">{member.user?.email || ''}</span>
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
						{
							value: 'SingleEmailToAll',
							label: t('notifications.aggregationAllOption', language.current)
						},
						{
							value: 'Individual',
							label: t('notifications.aggregationIndividualOption', language.current)
						}
					]}
				/>
			{/if}
		{/if}

		<div class="modal-action">
			<Button
				type="button"
				variant="ghost"
				onclick={() => (showCreateModal = false)}
				disabled={isProcessing}
			>
				{t('common.cancel', language.current)}
			</Button>
			<Button type="submit" variant="primary" loading={isProcessing}>
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

<Modal
	isOpen={showHistoryModal}
	title={t('notifications.history', language.current)}
	onClose={() => (showHistoryModal = false)}
>
	<div class="max-h-96 space-y-2 overflow-y-auto">
		{#if isProcessing}
			<div class="flex justify-center p-8"><span class="loading loading-spinner"></span></div>
		{:else if history.length === 0}
			<p class="py-8 text-center">{t('notifications.noHistory', language.current)}</p>
		{:else}
			{#each history as item}
				<div
					class="border-base-content/10 hover:bg-base-200/50 rounded-lg border p-3 text-sm transition-colors"
				>
					<div class="mb-1 flex items-center justify-between">
						<span class="max-w-xs truncate font-semibold">{item.subject}</span>
						<span class="badge badge-sm {item.status === 'Sent' ? 'badge-success' : 'badge-error'}"
							>{item.status}</span
						>
					</div>
					<p class="flex justify-between text-xs opacity-60">
						<span>{formatDateTime(item.createdAt)}</span>
						<span>{t('common.to', language.current)} {item.recipient}</span>
					</p>
				</div>
			{/each}
		{/if}
	</div>
	<div class="modal-action">
		<Button onclick={() => (showHistoryModal = false)}>{t('common.close', language.current)}</Button
		>
	</div>
</Modal>
