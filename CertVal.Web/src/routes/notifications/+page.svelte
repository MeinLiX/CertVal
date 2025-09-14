<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import type { CreateNotificationRuleRequest, NotificationHistory, NotificationRule, Workspace } from '$lib/types';

	let workspaces = $state<Workspace[]>([]);
	let selectedWorkspaceId = $state<string>('');
	let notificationRules = $state<NotificationRule[]>([]);
	let notificationHistory = $state<NotificationHistory[]>([]);
	let isLoading = $state(true);
	let isLoadingRules = $state(false);
	let isLoadingHistory = $state(false);

	// Modal states
	let showCreateRuleModal = $state(false);
	let showHistoryModal = $state(false);
	let isCreatingRule = $state(false);

	let createRuleForm = $state<CreateNotificationRuleRequest>({
		name: '',
		daysBeforeExpiry: 30,
		channelType: 'Email',
		channelConfig: '{}',
		frequency: 'Once'
	});

	let errors = $state<Record<string, string>>({});

	const workspaceSelectId = 'workspace-select-' + Math.random().toString(36).substr(2, 9);
	const channelTypeSelectId = 'channel-type-select-' + Math.random().toString(36).substr(2, 9);
	const frequencySelectId = 'frequency-select-' + Math.random().toString(36).substr(2, 9);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		await loadWorkspaces();
	});

	async function loadWorkspaces() {
		try {
			const response = await api.get<any>('/v1/workspaces');
			if (response.data) {
				workspaces = response.data.items;
				if (workspaces.length > 0 && !selectedWorkspaceId) {
					selectedWorkspaceId = workspaces[0].id;
					await loadNotificationRules();
				}
			}
		} catch (error) {
			console.error('Failed to load workspaces:', error);
		} finally {
			isLoading = false;
		}
	}

	async function loadNotificationRules() {
		if (!selectedWorkspaceId) return;

		isLoadingRules = true;
		try {
			const response = await api.get<NotificationRule[]>(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/rules`
			);
			if (response.data) {
				notificationRules = response.data;
			}
		} catch (error) {
			console.error('Failed to load notification rules:', error);
			notificationRules = [];
		} finally {
			isLoadingRules = false;
		}
	}

	async function loadNotificationHistory() {
		if (!selectedWorkspaceId) return;

		isLoadingHistory = true;
		showHistoryModal = true;

		try {
			const response = await api.get<NotificationHistory[]>(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/history`
			);
			if (response.data) {
				notificationHistory = response.data;
			}
		} catch (error) {
			console.error('Failed to load notification history:', error);
			notificationHistory = [];
		} finally {
			isLoadingHistory = false;
		}
	}

	async function handleWorkspaceChange() {
		if (selectedWorkspaceId) {
			await loadNotificationRules();
		} else {
			notificationRules = [];
		}
	}

	async function handleCreateRule(event: Event) {
		event.preventDefault();
		errors = {};
		isCreatingRule = true;

		try {
			let channelConfig = '{}';
			if (createRuleForm.channelType === 'Email') {
				channelConfig = JSON.stringify({
					email: $auth.user?.email || '',
					priority: 'normal'
				});
			} else if (createRuleForm.channelType === 'Webhook') {
				channelConfig = JSON.stringify({
					url: '',
					headers: {}
				});
			}

			const requestData = {
				...createRuleForm,
				channelConfig
			};

			const response = await api.post<NotificationRule>(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/rules`,
				requestData
			);

			if (response.data) {
				notificationRules = [...notificationRules, response.data];
				showCreateRuleModal = false;
				resetCreateForm();
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', $language);
		} finally {
			isCreatingRule = false;
		}
	}

	async function toggleRule(ruleId: string) {
		try {
			const response = await api.post<NotificationRule>(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/rules/${ruleId}/toggle`
			);

			if (response.data) {
				notificationRules = notificationRules.map((rule) =>
					rule.id === ruleId ? response.data! : rule
				);
			}
		} catch (error) {
			console.error('Failed to toggle rule:', error);
		}
	}

	async function deleteRule(ruleId: string) {
		if (!confirm('Are you sure you want to delete this notification rule?')) {
			return;
		}

		try {
			await api.delete(`/v1/workspaces/${selectedWorkspaceId}/notifications/rules/${ruleId}`);
			notificationRules = notificationRules.filter((rule) => rule.id !== ruleId);
		} catch (error) {
			console.error('Failed to delete rule:', error);
		}
	}

	function resetCreateForm() {
		createRuleForm = {
			name: '',
			daysBeforeExpiry: 30,
			channelType: 'Email',
			channelConfig: '{}',
			frequency: 'Once'
		};
		errors = {};
	}

	function handleCloseCreateModal() {
		showCreateRuleModal = false;
		resetCreateForm();
	}

	function getStatusColor(status: string): string {
		switch (status.toLowerCase()) {
			case 'sent':
			case 'delivered':
				return 'text-green-600 bg-green-100';
			case 'failed':
				return 'text-red-600 bg-red-100';
			case 'pending':
				return 'text-yellow-600 bg-yellow-100';
			default:
				return 'text-gray-600 bg-gray-100';
		}
	}

	function getChannelIcon(channelType: string): string {
		switch (channelType.toLowerCase()) {
			case 'email':
				return 'M3 8l7.89 7.89a1 1 0 001.415 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z';
			case 'webhook':
				return 'M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1';
			case 'slack':
				return 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z';
			case 'telegram':
				return 'M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z';
			default:
				return 'M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9';
		}
	}

	const selectedWorkspace = $derived(workspaces.find((w) => w.id === selectedWorkspaceId));
</script>

<svelte:head>
	<title>{t('nav.notifications', $language)}</title>
</svelte:head>

<div class="space-y-6">
	<!-- Header -->
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-2xl font-bold text-gray-900">{t('nav.notifications', $language)}</h1>
			<p class="text-gray-600">Configure notification rules for certificate expiry alerts</p>
		</div>

		{#if selectedWorkspaceId}
			<div class="flex space-x-3">
				<Button variant="outline" onclick={() => loadNotificationHistory()}>
					<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
						/>
					</svg>
					View History
				</Button>
				<Button onclick={() => (showCreateRuleModal = true)}>
					<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M12 4v16m8-8H4"
						/>
					</svg>
					Create Rule
				</Button>
			</div>
		{/if}
	</div>

	{#if isLoading}
		<div class="flex h-64 items-center justify-center">
			<div class="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
		</div>
	{:else if workspaces.length === 0}
		<Card>
			<div class="py-12 text-center">
				<svg
					class="mx-auto h-12 w-12 text-gray-400"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
				>
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6"
					/>
				</svg>
				<h3 class="mt-2 text-sm font-medium text-gray-900">No workspaces found</h3>
				<p class="mt-1 text-sm text-gray-500">
					Create a workspace first to set up notification rules.
				</p>
				<div class="mt-6">
					<Button onclick={() => goto('/workspaces')}>Create Workspace</Button>
				</div>
			</div>
		</Card>
	{:else}
		<!-- Workspace Selector -->
		<Card>
			<div class="flex items-center justify-between">
				<div>
					<label for={workspaceSelectId} class="mb-2 block text-sm font-medium text-gray-700"> Select Workspace </label>
					<select
						id={workspaceSelectId}
						bind:value={selectedWorkspaceId}
						onchange={handleWorkspaceChange}
						class="block w-64 rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
					>
						<option value="">Choose a workspace...</option>
						{#each workspaces as workspace}
							<option value={workspace.id}>{workspace.name}</option>
						{/each}
					</select>
				</div>

				{#if selectedWorkspace}
					<div class="text-right">
						<p class="text-sm text-gray-600">{selectedWorkspace.certificateCount} certificates</p>
						<p class="text-xs text-gray-500">{selectedWorkspace.memberCount} members</p>
					</div>
				{/if}
			</div>
		</Card>

		<!-- Notification Rules -->
		{#if selectedWorkspaceId}
			<Card title="Notification Rules">
				{#if isLoadingRules}
					<div class="flex h-32 items-center justify-center">
						<div class="h-6 w-6 animate-spin rounded-full border-b-2 border-blue-600"></div>
					</div>
				{:else if notificationRules.length === 0}
					<div class="py-8 text-center">
						<svg
							class="mx-auto h-12 w-12 text-gray-400"
							fill="none"
							viewBox="0 0 24 24"
							stroke="currentColor"
						>
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
							/>
						</svg>
						<h3 class="mt-2 text-sm font-medium text-gray-900">No notification rules</h3>
						<p class="mt-1 text-sm text-gray-500">
							Create your first notification rule to get alerts before certificates expire.
						</p>
						<div class="mt-6">
							<Button onclick={() => (showCreateRuleModal = true)}>Create First Rule</Button>
						</div>
					</div>
				{:else}
					<div class="space-y-4">
						{#each notificationRules as rule}
							<div class="rounded-lg border border-gray-200 p-4 transition-colors hover:bg-gray-50">
								<div class="flex items-center justify-between">
									<div class="flex items-center space-x-4">
										<!-- Channel Icon -->
										<div class="flex-shrink-0">
											<div
												class="flex h-10 w-10 items-center justify-center rounded-full bg-blue-100"
											>
												<svg
													class="h-5 w-5 text-blue-600"
													fill="none"
													viewBox="0 0 24 24"
													stroke="currentColor"
												>
													<path
														stroke-linecap="round"
														stroke-linejoin="round"
														stroke-width="2"
														d={getChannelIcon(rule.channelType)}
													/>
												</svg>
											</div>
										</div>

										<!-- Rule Info -->
										<div class="min-w-0 flex-1">
											<div class="flex items-center space-x-3">
												<h3 class="text-sm font-medium text-gray-900">{rule.name}</h3>
												<span
													class="inline-flex rounded-full px-2 py-1 text-xs font-semibold {rule.isEnabled
														? 'bg-green-100 text-green-600'
														: 'bg-gray-100 text-gray-600'}"
												>
													{rule.isEnabled ? 'Enabled' : 'Disabled'}
												</span>
											</div>
											<div class="mt-1 flex items-center space-x-4 text-sm text-gray-500">
												<span>{rule.daysBeforeExpiry} days before expiry</span>
												<span>•</span>
												<span>{rule.frequency} frequency</span>
												<span>•</span>
												<span>{rule.channelType}</span>
											</div>
											<p class="mt-1 text-xs text-gray-400">
												Created {formatDateTime(rule.createdAt)}
											</p>
										</div>
									</div>

									<!-- Actions -->
									<div class="flex items-center space-x-2">
										<button
											onclick={() => toggleRule(rule.id)}
											class="text-sm font-medium {rule.isEnabled
												? 'text-yellow-600 hover:text-yellow-800'
												: 'text-green-600 hover:text-green-800'}"
										>
											{rule.isEnabled ? 'Disable' : 'Enable'}
										</button>
										<span class="text-gray-300">|</span>
										<button
											onclick={() => deleteRule(rule.id)}
											class="text-sm font-medium text-red-600 hover:text-red-800"
										>
											Delete
										</button>
									</div>
								</div>
							</div>
						{/each}
					</div>
				{/if}
			</Card>
		{/if}
	{/if}
</div>

<!-- Create Rule Modal -->
<Modal
	isOpen={showCreateRuleModal}
	title="Create Notification Rule"
	onClose={handleCloseCreateModal}
>
	<form onsubmit={handleCreateRule} class="space-y-4">
		{#if errors.general}
			<div class="rounded-md border border-red-200 bg-red-50 p-4">
				<p class="text-sm text-red-600">{errors.general}</p>
			</div>
		{/if}

		<Input
			label="Rule Name"
			bind:value={createRuleForm.name}
			placeholder="e.g., Critical Alert - 7 days"
			required
			error={errors.name}
		/>

		<Input
			type="number"
			label="Days Before Expiry"
			bind:value={createRuleForm.daysBeforeExpiry}
			required
			error={errors.daysBeforeExpiry}
		/>

		<div class="space-y-1">
			<label for={channelTypeSelectId} class="block text-sm font-medium text-gray-700">
				Channel Type <span class="text-red-500">*</span>
			</label>
			<select
				id={channelTypeSelectId}
				bind:value={createRuleForm.channelType}
				required
				class="block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
			>
				<option value="Email">📧 Email</option>
				<option value="Webhook">🔗 Webhook</option>
				<option value="Slack">💬 Slack</option>
				<option value="Telegram">📱 Telegram</option>
			</select>
		</div>

		<div class="space-y-1">
			<label for={frequencySelectId} class="block text-sm font-medium text-gray-700">
				Frequency <span class="text-red-500">*</span>
			</label>
			<select
				id={frequencySelectId}
				bind:value={createRuleForm.frequency}
				required
				class="block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
			>
				<option value="Once">Once</option>
				<option value="Daily">Daily</option>
				<option value="Weekly">Weekly</option>
				<option value="Monthly">Monthly</option>
			</select>
		</div>

		<div class="rounded-md border border-blue-200 bg-blue-50 p-4">
			<div class="flex">
				<svg class="h-5 w-5 text-blue-400" viewBox="0 0 20 20" fill="currentColor">
					<path
						fill-rule="evenodd"
						d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
						clip-rule="evenodd"
					/>
				</svg>
				<div class="ml-3">
					<p class="text-sm text-blue-700">
						{#if createRuleForm.channelType === 'Email'}
							Email notifications will be sent to your registered email address.
						{:else if createRuleForm.channelType === 'Webhook'}
							Configure the webhook URL after creating the rule.
						{:else if createRuleForm.channelType === 'Slack'}
							You'll need to configure the Slack webhook URL after creating the rule.
						{:else if createRuleForm.channelType === 'Telegram'}
							You'll need to configure the Telegram bot token and chat ID after creating the rule.
						{/if}
					</p>
				</div>
			</div>
		</div>

		<div class="flex justify-end space-x-3 pt-4">
			<Button variant="outline" onclick={handleCloseCreateModal} type="button">
				{t('common.cancel', $language)}
			</Button>
			<Button type="submit" loading={isCreatingRule}>Create Rule</Button>
		</div>
	</form>
</Modal>

<!-- Notification History Modal -->
<Modal
	isOpen={showHistoryModal}
	title="Notification History"
	onClose={() => (showHistoryModal = false)}
>
	<div class="max-h-96 space-y-4 overflow-y-auto">
		{#if isLoadingHistory}
			<div class="flex h-32 items-center justify-center">
				<div class="h-6 w-6 animate-spin rounded-full border-b-2 border-blue-600"></div>
			</div>
		{:else if notificationHistory.length === 0}
			<div class="py-8 text-center">
				<svg
					class="mx-auto h-8 w-8 text-gray-400"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
				>
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
					/>
				</svg>
				<p class="mt-2 text-sm text-gray-500">No notification history found.</p>
			</div>
		{:else}
			{#each notificationHistory as notification}
				<div class="rounded-lg border border-gray-200 p-3">
					<div class="flex items-center justify-between">
						<div class="flex items-center space-x-3">
							<span
								class="inline-flex rounded-full px-2 py-1 text-xs font-semibold {getStatusColor(
									notification.status
								)}"
							>
								{notification.status}
							</span>
							<span class="text-sm text-gray-600">{notification.channelType}</span>
						</div>
						<span class="text-xs text-gray-500">
							{formatDateTime(notification.createdAt)}
						</span>
					</div>

					<div class="mt-2">
						<p class="text-sm font-medium text-gray-900">{notification.subject}</p>
						<p class="mt-1 text-xs text-gray-500">To: {notification.recipient}</p>
						{#if notification.errorMessage}
							<p class="mt-1 text-xs text-red-600">Error: {notification.errorMessage}</p>
						{/if}
					</div>
				</div>
			{/each}
		{/if}
	</div>
</Modal>