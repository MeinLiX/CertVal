<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import FloatingActionButton from '$lib/components/ui/FloatingActionButton.svelte';
	import type { ApiToken, CreateApiTokenRequest, CreateApiTokenResponse } from '$lib/types';

	let tokens = $state<ApiToken[]>([]);
	let isLoading = $state(true);
	let isProcessing = $state(false);

	let showCreateModal = $state(false);
	let showTokenModal = $state(false);
	let showRevokeModal = $state(false);

	let newTokenResponse = $state<CreateApiTokenResponse | null>(null);
	let tokenToRevoke = $state<ApiToken | null>(null);
	let isCopied = $state(false);

	let createForm = $state<CreateApiTokenRequest>({
		name: '',
		scope: 'ReadOnly',
		expiresAt: undefined
	});

	let errors = $state<Record<string, string>>({});

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadTokens();
	});

	async function loadTokens() {
		isLoading = true;
		try {
			const response = await api.get<ApiToken[]>('/v1/apitokens');
			if (response.data) {
				tokens = response.data;
			}
		} catch (error) {
			console.error('Failed to load API tokens:', error);
		} finally {
			isLoading = false;
		}
	}

	function openCreateModal() {
		createForm = { name: '', scope: 'ReadOnly', expiresAt: undefined };
		errors = {};
		isCopied = false;
		showCreateModal = true;
	}

	async function handleCreateToken(event: Event) {
		event.preventDefault();
		isProcessing = true;
		errors = {};
		try {
			const response = await api.post<CreateApiTokenResponse>('/v1/apitokens', createForm);
			if (response.data) {
				newTokenResponse = response.data;
				showCreateModal = false;
				showTokenModal = true;
				await loadTokens();
			} else {
				errors.create = response.message || 'Failed to create token.';
			}
		} catch (err) {
			errors.create = 'A network error occurred.';
		} finally {
			isProcessing = false;
		}
	}

	function copyToClipboard(text: string) {
		navigator.clipboard.writeText(text).then(() => {
			isCopied = true;
			setTimeout(() => {
				isCopied = false;
			}, 2000);
		});
	}

	function openRevokeModal(token: ApiToken) {
		tokenToRevoke = token;
		showRevokeModal = true;
	}

	async function handleRevokeToken() {
		if (!tokenToRevoke) return;
		isProcessing = true;
		try {
			await api.delete(`/v1/apitokens/${tokenToRevoke.id}`);
			showRevokeModal = false;
			await loadTokens();
		} catch (err) {
			console.error('Failed to revoke token:', err);
		} finally {
			isProcessing = false;
		}
	}
</script>

<svelte:head>
	<title>{t('nav.apiTokens', $language)}</title>
</svelte:head>

<div class="space-y-6">
	<FloatingActionButton iconName="plus" variant="primary" onclick={openCreateModal} />

	{#if isLoading}
		<div class="flex justify-center py-16">
			<span class="loading loading-lg loading-spinner"></span>
		</div>
	{:else if tokens.length === 0}
		<div class="py-16 text-center">
			<p>{t('common.noTokens', $language)}</p>
		</div>
	{:else}
		<div class="overflow-x-auto">
			<table class="table">
				<thead>
					<tr>
						<th>{t('common.name', $language)}</th>
						<th>{t('common.prefix', $language)}</th>
						<th>{t('common.scope', $language)}</th>
						<th>{t('common.status', $language)}</th>
						<th>{t('common.created', $language)}</th>
						<th>{t('common.lastUsed', $language)}</th>
						<th></th>
					</tr>
				</thead>
				<tbody>
					{#each tokens as token}
						<tr>
							<td class="font-semibold">{token.name}</td>
							<td><code>{token.tokenPrefix}...</code></td>
							<td><span class="badge badge-primary">{token.scope}</span></td>
							<td>
								{#if token.isActive}
									<span class="badge badge-success">{t('common.active', $language)}</span>
								{:else}
									<span class="badge badge-error">{t('common.revoked', $language)}</span>
								{/if}
							</td>
							<td>{formatDateTime(token.createdAt)}</td>
							<td
								>{token.lastUsedAt
									? formatDateTime(token.lastUsedAt)
									: t('common.never', $language)}</td
							>
							<td>
								<Button
									size="sm"
									variant="danger"
									onclick={() => openRevokeModal(token)}
									disabled={!token.isActive}
								>
									{t('common.revoke', $language)}
								</Button>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</div>

<Modal
	isOpen={showCreateModal}
	title={t('common.createTokenTitle', $language)}
	onClose={() => (showCreateModal = false)}
>
	<form onsubmit={handleCreateToken} class="space-y-4">
		{#if errors.create}
			<div role="alert" class="alert alert-error text-sm"><span>{errors.create}</span></div>
		{/if}
		<Input label={t('common.tokenName', $language)} bind:value={createForm.name} required />
		<Select
			label="Scope"
			bind:value={createForm.scope}
			options={[
				{ value: 'ReadOnly', label: 'Read Only' },
				{ value: 'ReadWrite', label: 'Read/Write' }
			]}
		/>
		<div class="modal-action">
			<Button type="button" variant="ghost" onclick={() => (showCreateModal = false)}
				>{t('common.cancel', $language)}</Button
			>
			<Button type="submit" variant="primary" loading={isProcessing}
				>{t('common.create', $language)}</Button
			>
		</div>
	</form>
</Modal>

<Modal
	isOpen={showTokenModal}
	title={t('common.newTokenTitle', $language)}
	onClose={() => (showTokenModal = false)}
>
	{#if newTokenResponse}
		<div class="space-y-4">
			<p>{t('common.copyTokenWarning', $language)}</p>
			<div class="space-y-2">
				<Input readonly value={newTokenResponse.token} />
				<Button
					size="sm"
					variant="primary"
					class="w-full"
					disabled={isCopied}
					onclick={() => copyToClipboard(newTokenResponse?.token ?? '')}
				>
					{isCopied ? t('common.copied', $language) : t('common.copy', $language)}
				</Button>
			</div>
			<div class="modal-action">
				<Button onclick={() => (showTokenModal = false)}>{t('common.close', $language)}</Button>
			</div>
		</div>
	{/if}
</Modal>

<Modal
	isOpen={showRevokeModal}
	title={t('common.revokeTokenTitle', $language)}
	onClose={() => (showRevokeModal = false)}
>
	{#if tokenToRevoke}
		<p>{t('common.revokeTokenWarning', $language, { tokenName: tokenToRevoke.name })}</p>
		<div class="modal-action">
			<Button variant="ghost" onclick={() => (showRevokeModal = false)}
				>{t('common.cancel', $language)}</Button
			>
			<Button variant="danger" loading={isProcessing} onclick={handleRevokeToken}>
				{t('common.revoke', $language)}
			</Button>
		</div>
	{/if}
</Modal>
