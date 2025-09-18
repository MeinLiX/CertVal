<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import ConfirmDeleteModal from '$lib/components/ui/ConfirmDeleteModal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import type {
		CreateNotificationRuleRequest,
		NotificationHistory,
		NotificationRule,
		Workspace,
		PagedResult,
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
		recipientUserIds: []
	});
	let webhookUrl = $state('');
	let errors = $state<Record<string, string>>({});

	let openRuleId = $state<string | null>(null);

	const selectedWorkspace = $derived(workspaces.find((w) => w.id === selectedWorkspaceId));

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		const urlWorkspaceId = page.url.searchParams.get('workspace');
		await loadWorkspaces(urlWorkspaceId);
	});

	async function loadWorkspaces(defaultId: string | null) {
		isLoading = true;
		try {
			const response = await api.get<PagedResult<Workspace>>('/v1/workspaces');
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
			const response = await api.get<WorkspaceMember[]>(
				`/v1/workspaces/${selectedWorkspaceId}/members`
			);
			if (response.data) {
				const owner = workspaces.find((w) => w.id === selectedWorkspaceId)?.owner;

				let allUsers = [...response.data];
				if (owner && !allUsers.some((m) => m.userId === owner.id)) {
					allUsers.unshift({
						id: owner.id,
						userId: owner.id,
						workspaceId: selectedWorkspaceId,
						role: 'Owner',
						user: owner,
						status: 'Active'
					} as any);
				}
				workspaceMembers = allUsers;
			}
		} catch (error) {
			console.error('Failed to load workspace members:', error);
			workspaceMembers = [];
		}
	}

	async function loadRulesForWorkspace() {
		if (!selectedWorkspaceId) return;
		isLoading = true;
		rules = [];
		await loadWorkspaceMembers();
		try {
			const response = await api.get<NotificationRule[]>(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/rules`
			);
			if (response.data) {
				rules = response.data;
			}
		} catch (error) {
			console.error('Failed to load rules:', error);
		} finally {
			isLoading = false;
		}
	}

	async function loadHistoryForWorkspace() {
		if (!selectedWorkspaceId) return;
		isProcessing = true;
		history = [];
		try {
			const response = await api.get<NotificationHistory[]>(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/history`
			);
			if (response.data) {
				history = response.data;
			}
		} catch (error) {
			console.error('Failed to load history:', error);
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
			recipientUserIds: []
		};
		webhookUrl = '';
		errors = {};
		showCreateModal = true;
	}

	async function handleCreateRule(event: Event) {
		event.preventDefault();
		isProcessing = true;
		errors = {};

		const { recipientUserIds, ...restOfForm } = createForm;

		let payload: any = { ...restOfForm };

		if (createForm.channelType === 'Email') {
			if (recipientUserIds.length === 0) {
				errors.general = 'Please select at least one recipient.';
				isProcessing = false;
				return;
			}
			payload.recipientUserIds = recipientUserIds;
		} else if (createForm.channelType === 'Webhook') {
			try {
				new URL(webhookUrl);
				payload.channelConfig = JSON.stringify({ url: webhookUrl });
			} catch (_) {
				errors.general = t('errors.invalidUrl', $language);
				isProcessing = false;
				return;
			}
		}

		try {
			const response = await api.post<NotificationRule>(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/rules`,
				payload
			);
			if (response.data) {
				rules = [...rules, response.data];
				showCreateModal = false;
			} else {
				errors.general = response.message || 'Failed to create rule.';
			}
		} catch (err) {
			const error = err as Error;
			errors.general = error.message || 'A network error occurred.';
		} finally {
			isProcessing = false;
		}
	}

	function handleDeleteRule(ruleId: string) {
		pendingDeleteRuleId = ruleId;
		showConfirmModal = true;
	}

	async function confirmDelete() {
		if (!pendingDeleteRuleId) return;
		try {
			await api.delete(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/rules/${pendingDeleteRuleId}`
			);
			rules = rules.filter((r) => r.id !== pendingDeleteRuleId);
		} catch (err) {
			console.error('Failed to delete rule', err);
		} finally {
			pendingDeleteRuleId = null;
			showConfirmModal = false;
		}
	}

	function isRuleDisabledForCurrentUser(rule: NotificationRule): boolean {
		if (!$auth.user || $auth.user.emailNotificationsEnabled) {
			return false;
		}

		if (rule.channelType !== 'Email') {
			return false;
		}

		try {
			const config = JSON.parse(rule.channelConfig);
			return config.email && config.email === $auth.user.email;
		} catch (e) {
			return false;
		}
	}

	function getRecipientsForRule(rule: NotificationRule) {
		if (rule.channelType !== 'Email') return [];
		try {
			const config = JSON.parse(rule.channelConfig);
			const userIds: string[] = config.userIds || [];
			return userIds
				.map((id) => workspaceMembers.find((m) => m.userId === id))
				.filter(Boolean) as WorkspaceMember[];
		} catch {
			return [];
		}
	}

	function toggleRule(ruleId: string) {
		openRuleId = openRuleId === ruleId ? null : ruleId;
	}
</script>

<svelte:head>
	<title>{t('notifications.title', $language)}</title>
</svelte:head>

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-3xl font-bold">{t('notifications.title', $language)}</h1>
			<p class="mt-1 text-base-content/70">{t('notifications.subtitle', $language)}</p>
		</div>
		<div>
			<Button
				onclick={() => {
					showHistoryModal = true;
					loadHistoryForWorkspace();
				}}
				variant="ghost"
				class="mr-2"
				disabled={workspaces.length === 0}
			>
				{t('notifications.viewHistory', $language)}
			</Button>
			<Button onclick={openCreateModal} disabled={!selectedWorkspaceId}>
				{t('notifications.createRule', $language)}
			</Button>
		</div>
	</div>

	{#if isLoading}
		<div class="flex justify-center py-16">
			<span class="loading loading-lg loading-spinner"></span>
		</div>
	{:else if workspaces.length === 0}
		<div class="py-16 text-center">
			<div class="placeholder avatar">
				<div class="w-24 rounded-full bg-base-200 text-base-content/50">
					<Icon name="workspaces" class="h-12 w-12" />
				</div>
			</div>
			<h3 class="mt-4 text-xl font-semibold">{t('notifications.noWorkspaces', $language)}</h3>
			<p class="mt-2 text-base-content/60">{t('notifications.createFirstWorkspace', $language)}</p>
			<div class="mt-6 flex justify-center">
				<Button onclick={() => goto('/workspaces')}>
					{t('workspaces.create', $language)}
				</Button>
			</div>
		</div>
	{:else}
		<Card>
			<Select
				label={t('common.workspace', $language)}
				bind:value={selectedWorkspaceId}
				onchange={loadRulesForWorkspace}
				options={workspaces.map((w) => ({ value: w.id, label: w.name }))}
			/>
		</Card>

		<div class="space-y-4">
			{#if rules.length === 0}
				<div class="py-16 text-center">
					<h3 class="text-xl font-semibold">{t('notifications.noRules', $language)}</h3>
					<p class="mt-2 text-base-content/60">{t('notifications.createFirstRule', $language)}</p>
					<div class="mt-6 flex justify-center">
						<Button onclick={openCreateModal}>
							{t('notifications.createRule', $language)}
						</Button>
					</div>
				</div>
			{:else}
				{#each rules as rule (rule.id)}
					{@const isDisabled = isRuleDisabledForCurrentUser(rule)}
					{@const recipients = getRecipientsForRule(rule)}
					{@const isOpen = openRuleId === rule.id}
					<div
						class="rounded-xl border border-base-content/10 bg-base-100 shadow-sm transition-colors"
					>
						<div
							role="button"
							class="group flex w-full items-start gap-4 px-5 py-4 text-left focus:outline-none focus-visible:ring focus-visible:ring-primary/40"
							aria-expanded={isOpen}
							tabindex="0"
							onclick={() => toggleRule(rule.id)}
							onkeydown={(e) => {
								if (e.key === 'Enter' || e.key === ' ') {
									e.preventDefault();
									toggleRule(rule.id);
								}
							}}
						>
							<div class="flex-grow space-y-1">
								<div class="flex items-start justify-between gap-4">
									<p class="leading-tight font-semibold">{rule.name}</p>
									<div class="flex items-center gap-3">
										<div class="flex items-center gap-2">
											<span
												class="badge badge-xs {rule.isEnabled ? 'badge-success' : 'badge-ghost'}"
											>
												{rule.isEnabled
													? t('notifications.enabled', $language)
													: t('notifications.disabled', $language)}
											</span>
											<button
												type="button"
												class="inline-flex cursor-pointer items-center gap-2 rounded-md px-2 py-1 hover:bg-base-200 focus:outline-none focus-visible:ring focus-visible:ring-primary/40"
												onclick={(e) => e.stopPropagation()}
												onkeydown={(e) => {
													if (e.key === 'Enter' || e.key === ' ') {
														e.preventDefault();
														e.stopPropagation();
													}
												}}
												aria-label={rule.isEnabled
													? t('notifications.enabled', $language)
													: t('notifications.disabled', $language)}
											>
												<input
													type="checkbox"
													class="toggle toggle-success toggle-xs"
													checked={rule.isEnabled}
													disabled={isDisabled}
													tabindex="-1"
												/>
											</button>
											<Button
												size="sm"
												variant="ghost"
												onclick={(e) => {
													e.stopPropagation();
													handleDeleteRule(rule.id);
												}}
											>
												{t('common.delete', $language)}
											</Button>
										</div>
									</div>
								</div>
								<p class="text-xs opacity-70 sm:text-sm">
									{t('notifications.triggers', $language)} <strong>{rule.daysBeforeExpiry}</strong>
									{t('notifications.daysBeforeExpirySuffix', $language)}.
									{t('notifications.channel', $language)} <strong>{rule.channelType}</strong>.
									{t('notifications.frequency', $language)}: <strong>{rule.frequency}</strong>.
								</p>
								{#if isDisabled}
									<div role="alert" class="mt-2 alert alert-warning p-2 text-xs">
										<span>{t('notifications.disabledInProfileWarning', $language)}</span>
									</div>
								{/if}
							</div>
							<div
								class="mt-1 shrink-0 self-center transition-transform group-aria-expanded:rotate-180"
								aria-hidden="true"
							>
								<svg
									class="h-4 w-4 text-base-content/60 transition-transform"
									viewBox="0 0 20 20"
									fill="none"
									stroke="currentColor"
									stroke-width="2"
									stroke-linecap="round"
									stroke-linejoin="round"
								>
									<path d="M6 8l4 4 4-4" />
								</svg>
							</div>
						</div>
						{#if isOpen && rule.channelType === 'Email' && recipients.length > 0}
							<div class="border-t border-base-content/10 px-5 py-3">
								<p class="mb-2 text-sm font-medium">
									{t('notifications.recipients', $language)} ({recipients.length})
								</p>
								<div class="max-h-48 space-y-2 overflow-y-auto pr-1">
									{#each recipients as member (member.userId)}
										<div class="flex items-center gap-3 rounded-lg bg-base-200/60 p-2">
											<UserAvatar
												firstName={member.user.firstName}
												lastName={member.user.lastName}
												size="w-8"
												textSize="text-sm"
											/>
											<div class="flex flex-col">
												<span class="label-text">{member.user.fullName}</span>
												{#if selectedWorkspace && member.userId === selectedWorkspace.ownerId}
													<span class="mt-1 badge badge-xs badge-primary"
														>{t('common.owner', $language)}</span
													>
												{/if}
											</div>
										</div>
									{/each}
								</div>
							</div>
						{/if}
					</div>
				{/each}
			{/if}
		</div>
	{/if}
</div>

<Modal
	isOpen={showCreateModal}
	title={t('notifications.createRule', $language)}
	onClose={() => (showCreateModal = false)}
>
	<form onsubmit={handleCreateRule} class="space-y-4">
		{#if errors.general}
			<div role="alert" class="alert alert-error text-sm"><span>{errors.general}</span></div>
		{/if}
		<Input label={t('notifications.ruleName', $language)} bind:value={createForm.name} required />
		<Input
			label={t('notifications.daysBeforeExpiry', $language)}
			type="number"
			bind:value={createForm.daysBeforeExpiry}
			required
		/>
		<Select
			label={t('notifications.channel', $language)}
			bind:value={createForm.channelType}
			options={[
				{ value: 'Email', label: t('notifications.email', $language) },
				{ value: 'Webhook', label: t('notifications.webhook', $language) }
			]}
		/>

		{#if createForm.channelType === 'Email'}
			<fieldset class="form-control">
				<legend class="label">
					<span class="label-text font-medium">{t('notifications.recipients', $language)}</span>
				</legend>
				<div
					class="max-h-48 space-y-1 overflow-y-auto rounded-lg border border-base-content/20 p-2"
				>
					{#each workspaceMembers as member (member.userId)}
						<label
							class="flex cursor-pointer items-center justify-between rounded-lg p-2 transition-colors hover:bg-base-200"
						>
							<div class="flex items-center gap-3">
								<UserAvatar
									firstName={member.user.firstName}
									lastName={member.user.lastName}
									size="w-8"
									textSize="text-sm"
								/>
								<div class="flex flex-col">
									<span class="label-text">{member.user.fullName}</span>
									{#if selectedWorkspace && member.userId === selectedWorkspace.ownerId}
										<span class="mt-1 badge badge-xs badge-primary"
											>{t('common.owner', $language)}</span
										>
									{/if}
								</div>
							</div>
							<input
								type="checkbox"
								bind:group={createForm.recipientUserIds}
								value={member.userId}
								class="checkbox checkbox-primary"
							/>
						</label>
					{/each}
				</div>
			</fieldset>
		{:else if createForm.channelType === 'Webhook'}
			<Input
				label={t('notifications.webhookUrl', $language)}
				type="url"
				bind:value={webhookUrl}
				required
				placeholder="https://api.example.com/webhook"
			/>
		{/if}

		<Select
			label={t('notifications.frequency', $language)}
			bind:value={createForm.frequency}
			options={[
				{ value: 'Once', label: t('notifications.once', $language) },
				{ value: 'Daily', label: t('notifications.daily', $language) },
				{ value: 'Weekly', label: t('notifications.weekly', $language) },
				{ value: 'Monthly', label: t('notifications.monthly', $language) }
			]}
		/>
		<div class="modal-action">
			<Button type="button" variant="ghost" onclick={() => (showCreateModal = false)}
				>{t('common.cancel', $language)}</Button
			>
			<Button type="submit" loading={isProcessing} variant="primary"
				>{t('common.create', $language)}</Button
			>
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
	title={t('notifications.history', $language)}
	onClose={() => (showHistoryModal = false)}
>
	<div class="max-h-96 space-y-2 overflow-y-auto">
		{#if isProcessing}
			<div class="flex justify-center p-8"><span class="loading loading-spinner"></span></div>
		{:else if history.length === 0}
			<p class="py-8 text-center">{t('notifications.noHistory', $language)}</p>
		{:else}
			{#each history as item}
				<div class="rounded-lg border border-base-content/10 p-2 text-sm">
					<div class="flex justify-between">
						<span class="max-w-xs truncate font-semibold">{item.subject}</span>
						<span class="badge badge-sm {item.status === 'Sent' ? 'badge-success' : 'badge-error'}"
							>{item.status}</span
						>
					</div>
					<p class="text-xs opacity-60">
						{formatDateTime(item.createdAt)}
						{t('common.to', $language)}
						{item.recipient}
					</p>
				</div>
			{/each}
		{/if}
	</div>
	<div class="modal-action">
		<Button onclick={() => (showHistoryModal = false)}>{t('common.close', $language)}</Button>
	</div>
</Modal>
