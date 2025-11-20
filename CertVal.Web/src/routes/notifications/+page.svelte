<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { withMinDelay } from '$lib/utils/loading';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
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
		recipientUserIds: [],
		recipientAggregationMode: 'SingleEmailToAll'
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
			const response = await withMinDelay(api.get<PagedResult<Workspace>>('/workspaces'));
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
				`/workspaces/${selectedWorkspaceId}/members`
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
			const response = await withMinDelay(
				api.get<NotificationRule[]>(`/workspaces/${selectedWorkspaceId}/notifications/rules`)
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
				`/workspaces/${selectedWorkspaceId}/notifications/history`
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
			recipientUserIds: [],
			recipientAggregationMode: 'SingleEmailToAll'
		};
		webhookUrl = '';
		errors = {};
		showCreateModal = true;
	}

	async function handleCreateRule(event: Event) {
		event.preventDefault();
		isProcessing = true;
		errors = {};

		const { recipientUserIds, recipientAggregationMode, ...restOfForm } = createForm;

		let payload: any = { ...restOfForm };

		if (createForm.channelType === 'Email') {
			if (recipientUserIds.length === 0) {
				errors.general = 'Please select at least one recipient.';
				isProcessing = false;
				return;
			}
			payload.recipientUserIds = recipientUserIds;
			payload.recipientAggregationMode = recipientAggregationMode;
		} else if (createForm.channelType === 'Webhook') {
			try {
				new URL(webhookUrl);
				payload.channelConfig = JSON.stringify({ url: webhookUrl });
			} catch (_) {
				errors.general = t('errors.invalidUrl', language.current);
				isProcessing = false;
				return;
			}
		}

		try {
			const response = await api.post<NotificationRule>(
				`/workspaces/${selectedWorkspaceId}/notifications/rules`,
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
				`/workspaces/${selectedWorkspaceId}/notifications/rules/${pendingDeleteRuleId}`
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
	<title>{t('notifications.title', language.current)}</title>
</svelte:head>

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-3xl font-bold">{t('notifications.title', language.current)}</h1>
			<p class="text-base-content/70 mt-1">{t('notifications.subtitle', language.current)}</p>
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
				{t('notifications.viewHistory', language.current)}
			</Button>
			<Button onclick={openCreateModal} disabled={!selectedWorkspaceId}>
				{t('notifications.createRule', language.current)}
			</Button>
		</div>
	</div>

	<div class="relative min-h-[400px]">
		{#if isLoading}
			<GlobalLoader variant="overlay" class="bg-transparent backdrop-blur-none" />
		{:else if workspaces.length === 0}
			<div class="py-16 text-center">
				<div
					class="bg-base-100 text-base-content/50 mx-auto flex h-24 w-24 items-center justify-center rounded-full shadow-inner"
				>
					<Icon name="workspaces" class="h-12 w-12" />
				</div>
				<h3 class="mt-4 text-xl font-semibold">
					{t('notifications.noWorkspaces', language.current)}
				</h3>
				<p class="text-base-content/60 mt-2">
					{t('notifications.createFirstWorkspace', language.current)}
				</p>
				<div class="mt-6 flex justify-center">
					<Button onclick={() => goto('/workspaces')}>
						{t('workspaces.create', language.current)}
					</Button>
				</div>
			</div>
		{:else}
			<Card>
				<Select
					label={t('common.workspace', language.current)}
					bind:value={selectedWorkspaceId}
					onchange={loadRulesForWorkspace}
					options={workspaces.map((w) => ({ value: w.id, label: w.name }))}
				/>
			</Card>

			<div class="space-y-4">
				{#if rules.length === 0}
					<div class="py-16 text-center">
						<h3 class="text-xl font-semibold">{t('notifications.noRules', language.current)}</h3>
						<p class="text-base-content/60 mt-2">
							{t('notifications.createFirstRule', language.current)}
						</p>
						<div class="mt-6 flex justify-center">
							<Button onclick={openCreateModal}>
								{t('notifications.createRule', language.current)}
							</Button>
						</div>
					</div>
				{:else}
					{#each rules as rule (rule.id)}
						{@const isDisabled = isRuleDisabledForCurrentUser(rule)}
						{@const recipients = getRecipientsForRule(rule)}
						{@const isOpen = openRuleId === rule.id}
						<div
							class="border-base-content/10 bg-base-100 rounded-xl border shadow-sm transition-colors"
						>
							<div
								role="button"
								class="focus-visible:ring-primary/40 group flex w-full items-start gap-4 px-5 py-4 text-left focus:outline-none focus-visible:ring"
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
										<p class="font-semibold leading-tight">{rule.name}</p>
										<div class="flex items-center gap-3">
											<div class="flex items-center gap-2">
												<span
													class="badge badge-xs {rule.isEnabled ? 'badge-success' : 'badge-ghost'}"
												>
													{rule.isEnabled
														? t('notifications.enabled', language.current)
														: t('notifications.disabled', language.current)}
												</span>
												<button
													type="button"
													class="hover:bg-base-200 focus-visible:ring-primary/40 inline-flex cursor-pointer items-center gap-2 rounded-md px-2 py-1 focus:outline-none focus-visible:ring"
													onclick={(e) => e.stopPropagation()}
													onkeydown={(e) => {
														if (e.key === 'Enter' || e.key === ' ') {
															e.preventDefault();
															e.stopPropagation();
														}
													}}
													aria-label={rule.isEnabled
														? t('notifications.enabled', language.current)
														: t('notifications.disabled', language.current)}
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
													{t('common.delete', language.current)}
												</Button>
											</div>
										</div>
									</div>
									<p class="text-xs opacity-70 sm:text-sm">
										{t('notifications.triggers', language.current)}
										<strong>{rule.daysBeforeExpiry}</strong>
										{t('notifications.daysBeforeExpirySuffix', language.current)}.
										{t('notifications.channel', language.current)}
										<strong>{rule.channelType}</strong>.
										{t('notifications.frequency', language.current)}:
										<strong>{rule.frequency}</strong>.
										{#if rule.channelType === 'Email'}
											<span class="ml-1">
												{#if rule.recipientAggregationMode === 'SingleEmailToAll'}
													<strong>[{t('notifications.aggregatedBadge', language.current)}]</strong>
												{:else}
													<strong>[{t('notifications.individualBadge', language.current)}]</strong>
												{/if}
											</span>
										{/if}
									</p>
									{#if isDisabled}
										<div role="alert" class="alert alert-warning mt-2 p-2 text-xs">
											<span>{t('notifications.disabledInProfileWarning', language.current)}</span>
										</div>
									{/if}
								</div>
								<div
									class="mt-1 shrink-0 self-center transition-transform group-aria-expanded:rotate-180"
									aria-hidden="true"
								>
									<svg
										class="text-base-content/60 h-4 w-4 transition-transform"
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
								<div class="border-base-content/10 border-t px-5 py-3">
									<p class="mb-2 text-sm font-medium">
										{t('notifications.recipients', language.current)} ({recipients.length})
									</p>
									<div class="max-h-48 space-y-2 overflow-y-auto pr-1">
										{#each recipients as member (member.userId)}
											<div class="bg-base-200/60 flex items-center gap-3 rounded-lg p-2">
												<UserAvatar
													firstName={member.user.firstName}
													lastName={member.user.lastName}
													size="w-8"
													textSize="text-sm"
												/>
												<div class="flex flex-col">
													<span class="label-text">{member.user.fullName}</span>
													{#if selectedWorkspace && member.userId === selectedWorkspace.ownerId}
														<span class="badge badge-xs badge-primary mt-1"
															>{t('common.owner', language.current)}</span
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
</div>

<Modal
	isOpen={showCreateModal}
	title={t('notifications.createRule', language.current)}
	onClose={() => (showCreateModal = false)}
>
	<form onsubmit={handleCreateRule} class="space-y-4">
		{#if errors.general}
			<div role="alert" class="alert alert-error text-sm"><span>{errors.general}</span></div>
		{/if}
		<Input
			label={t('notifications.ruleName', language.current)}
			bind:value={createForm.name}
			required
		/>
		<Input
			label={t('notifications.daysBeforeExpiry', language.current)}
			type="number"
			bind:value={createForm.daysBeforeExpiry}
			required
		/>
		<Select
			label={t('notifications.channel', language.current)}
			bind:value={createForm.channelType}
			options={[
				{ value: 'Email', label: t('notifications.email', language.current) },
				{ value: 'Webhook', label: t('notifications.webhook', language.current) }
			]}
		/>

		{#if createForm.channelType === 'Email'}
			<fieldset class="form-control">
				<label class="label" for="recipient-aggregation-mode">
					<span class="label-text">{t('notifications.recipientAggregation', language.current)}</span
					>
				</label>
				<div
					id="recipient-aggregation-mode"
					class="border-base-content/20 max-h-48 space-y-1 overflow-y-auto rounded-lg border p-2"
				>
					{#each workspaceMembers as member (member.userId)}
						<div class="bg-base-200/60 flex items-center justify-between gap-3 rounded-lg p-2">
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
										<span class="badge badge-xs badge-primary mt-1"
											>{t('common.owner', language.current)}</span
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
						</div>
					{/each}
				</div>

				{#if createForm.recipientUserIds.length > 1}
					<div class="mt-3">
						<label class="label" for="recipient-aggregation-select">
							<span class="label-text"
								>{t('notifications.recipientAggregation', language.current)}</span
							>
						</label>
						<select
							id="recipient-aggregation-select"
							bind:value={createForm.recipientAggregationMode}
							class="select select-bordered w-full"
						>
							<option value="SingleEmailToAll">
								{t('notifications.aggregationAllOption', language.current)}
							</option>
							<option value="Individual">
								{t('notifications.aggregationIndividualOption', language.current)}
							</option>
						</select>
						<p class="mt-1 text-xs opacity-70">
							{t('notifications.recipientAggregationHelp', language.current)}
						</p>
					</div>
				{/if}
			</fieldset>
		{:else if createForm.channelType === 'Webhook'}
			<Input
				label={t('notifications.webhookUrl', language.current)}
				type="url"
				bind:value={webhookUrl}
				required
				placeholder="https://api.example.com/webhook"
			/>
		{/if}

		<Select
			label={t('notifications.frequency', language.current)}
			bind:value={createForm.frequency}
			options={[
				{ value: 'Once', label: t('notifications.once', language.current) },
				{ value: 'Daily', label: t('notifications.daily', language.current) },
				{ value: 'Weekly', label: t('notifications.weekly', language.current) },
				{ value: 'Monthly', label: t('notifications.monthly', language.current) }
			]}
		/>
		<div class="modal-action">
			<Button type="button" variant="ghost" onclick={() => (showCreateModal = false)}
				>{t('common.cancel', language.current)}</Button
			>
			<Button type="submit" loading={isProcessing} variant="primary"
				>{t('common.create', language.current)}</Button
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
				<div class="border-base-content/10 rounded-lg border p-2 text-sm">
					<div class="flex justify-between">
						<span class="max-w-xs truncate font-semibold">{item.subject}</span>
						<span class="badge badge-sm {item.status === 'Sent' ? 'badge-success' : 'badge-error'}"
							>{item.status}</span
						>
					</div>
					<p class="text-xs opacity-60">
						{formatDateTime(item.createdAt)}
						{t('common.to', language.current)}
						{item.recipient}
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
