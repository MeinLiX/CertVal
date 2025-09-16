<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import type {
		CreateNotificationRuleRequest,
		NotificationHistory,
		NotificationRule,
		Workspace,
		PagedResult
	} from '$lib/types';

	let workspaces = $state<Workspace[]>([]);
	let selectedWorkspaceId = $state<string>('');
	let rules = $state<NotificationRule[]>([]);
	let history = $state<NotificationHistory[]>([]);

	let isLoading = $state(true);
	let isProcessing = $state(false);

	let showCreateModal = $state(false);
	let showHistoryModal = $state(false);
	let createForm = $state<CreateNotificationRuleRequest>({
		name: '',
		daysBeforeExpiry: 30,
		channelType: 'Email',
		channelConfig: '{}',
		frequency: 'Once'
	});
	let webhookUrl = $state('');
	let errors = $state<Record<string, string>>({});

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		const urlWorkspaceId = $page.url.searchParams.get('workspace');
		await loadWorkspaces(urlWorkspaceId);
	});

	async function loadWorkspaces(defaultId: string | null) {
		isLoading = true;
		try {
			const response = await api.get<PagedResult<Workspace>>('/v1/workspaces');
			if (response.data) {
				workspaces = response.data.items;
				if (defaultId && workspaces.some((w) => w.id === defaultId)) {
					selectedWorkspaceId = defaultId;
				} else if (workspaces.length > 0) {
					selectedWorkspaceId = workspaces[0].id;
				}
				await loadRulesForWorkspace();
			}
		} catch (error) {
			console.error('Failed to load workspaces:', error);
		} finally {
			isLoading = false;
		}
	}

	async function loadRulesForWorkspace() {
		if (!selectedWorkspaceId) return;
		isLoading = true;
		rules = [];
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
			frequency: 'Once'
		};
		webhookUrl = '';
		errors = {};
		showCreateModal = true;
	}

	async function handleCreateRule(event: Event) {
		event.preventDefault();
		isProcessing = true;
		errors = {};

		let channelConfig = '{}';
		if (createForm.channelType === 'Email') {
			channelConfig = JSON.stringify({ email: $auth.user?.email || '' });
		} else if (createForm.channelType === 'Webhook') {
			try {
				new URL(webhookUrl); // Validate URL format
				channelConfig = JSON.stringify({ url: webhookUrl });
			} catch (_) {
				errors.general = t('errors.invalidUrl', $language);
				isProcessing = false;
				return;
			}
		}

		try {
			const response = await api.post<NotificationRule>(
				`/v1/workspaces/${selectedWorkspaceId}/notifications/rules`,
				{ ...createForm, channelConfig }
			);
			if (response.data) {
				rules = [...rules, response.data];
				showCreateModal = false;
			} else {
				errors.general = response.message || 'Failed to create rule.';
			}
		} catch (err) {
			errors.general = 'A network error occurred.';
		} finally {
			isProcessing = false;
		}
	}

	async function handleDeleteRule(ruleId: string) {
		if (!confirm(t('notifications.confirmDelete', $language))) return;
		try {
			await api.delete(`/v1/workspaces/${selectedWorkspaceId}/notifications/rules/${ruleId}`);
			rules = rules.filter((r) => r.id !== ruleId);
		} catch (err) {
			console.error('Failed to delete rule', err);
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
				class="mr-2">{t('notifications.viewHistory', $language)}</Button
			>
			<Button onclick={openCreateModal} disabled={!selectedWorkspaceId}
				>{t('notifications.createRule', $language)}</Button
			>
		</div>
	</div>

	<Card>
		<Select
			label={t('common.workspace', $language)}
			bind:value={selectedWorkspaceId}
			onchange={loadRulesForWorkspace}
			options={workspaces.map((w) => ({ value: w.id, label: w.name }))}
		/>
	</Card>

	<div class="space-y-4">
		{#if isLoading}
			{#each { length: 3 } as _}
				<div class="h-20 w-full skeleton"></div>
			{/each}
		{:else if rules.length === 0}
			<div class="py-16 text-center">
				<h3 class="text-xl font-semibold">{t('notifications.noRules', $language)}</h3>
				<p class="mt-2 text-base-content/60">{t('notifications.createFirstRule', $language)}</p>
				<Button class="mt-6" onclick={openCreateModal}
					>{t('notifications.createRule', $language)}</Button
				>
			</div>
		{:else}
			{#each rules as rule (rule.id)}
				{@const isDisabled = isRuleDisabledForCurrentUser(rule)}
				<Card>
					<div class="flex items-center justify-between">
						<div>
							<p class="font-bold">{rule.name}</p>
							<p class="text-sm opacity-70">
								{t('notifications.triggers', $language)} <strong>{rule.daysBeforeExpiry}</strong>
								{t('notifications.daysBeforeExpirySuffix', $language)}. {t(
									'notifications.channel',
									$language
								)}
								<strong>{rule.channelType}</strong>. {t('notifications.frequency', $language)}:
								<strong>{rule.frequency}</strong>.
							</p>
							{#if isDisabled}
								<div role="alert" class="mt-2 alert alert-warning p-2 text-xs">
									<span>{t('notifications.disabledInProfileWarning', $language)}</span>
								</div>
							{/if}
						</div>
						<div class="flex items-center gap-2">
							<div class="form-control">
								<label class="label cursor-pointer gap-2">
									<span class="label-text text-xs"
										>{rule.isEnabled
											? t('notifications.enabled', $language)
											: t('notifications.disabled', $language)}</span
									>
									<input
										type="checkbox"
										checked={rule.isEnabled}
										class="toggle toggle-sm toggle-success"
										disabled={isDisabled}
									/>
								</label>
							</div>
							<Button size="sm" variant="ghost" onclick={() => handleDeleteRule(rule.id)}
								>{t('common.delete', $language)}</Button
							>
						</div>
					</div>
				</Card>
			{/each}
		{/if}
	</div>
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

		{#if createForm.channelType === 'Webhook'}
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
					<div
						class="flex
justify-between"
					>
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
