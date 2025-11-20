<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { ApiTokenService } from '$lib/services/ApiTokenService';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import Card from '$lib/components/ui/Card.svelte';
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
		if (!userSession.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadTokens();
	});

	async function loadTokens() {
		isLoading = true;
		try {
			const response = await ApiTokenService.getTokens();
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
			const response = await ApiTokenService.createToken(createForm);
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
			await ApiTokenService.revokeToken(tokenToRevoke.id);
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
	<title>{t('nav.apiTokens', language.current)} - CertVal</title>
</svelte:head>

<div class="animate-in fade-in slide-in-from-bottom-4 space-y-8 duration-500">
	<div
		class="border-base-content/10 flex flex-col items-start justify-between gap-4 border-b pb-6 md:flex-row md:items-center"
	>
		<div>
			<h1
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-3xl font-bold text-transparent"
			>
				{t('nav.apiTokens', language.current)}
			</h1>
			<p class="text-base-content/60 mt-2 text-lg font-light">
				{t('profile.apiTokensSubtitle', language.current)}
			</p>
		</div>
		<Button onclick={openCreateModal} variant="primary" class="shadow-primary/20 shadow-lg">
			<Icon name="plus" class="mr-2 h-5 w-5" />
			{t('common.create', language.current)}
		</Button>
	</div>

	{#if isLoading}
		<div class="grid grid-cols-1 gap-4">
			{#each { length: 3 } as _}
				<div class="skeleton bg-base-200/50 h-24 w-full rounded-xl"></div>
			{/each}
		</div>
	{:else if tokens.length === 0}
		<div
			class="bg-base-100/30 border-base-content/5 flex flex-col items-center justify-center rounded-3xl border py-20 text-center backdrop-blur-sm"
		>
			<div class="bg-base-200/50 mb-6 rounded-full p-6">
				<Icon name="key" class="text-base-content/20 h-16 w-16" />
			</div>
			<h3 class="mb-2 text-xl font-bold">{t('common.noTokens', language.current)}</h3>
			<p class="text-base-content/60 mb-8 max-w-md">
				Create a token to access the API programmatically. Tokens allow you to authenticate with the
				API without using your password.
			</p>
			<Button onclick={openCreateModal} variant="outline" class="shadow-sm">
				Create your first token
			</Button>
		</div>
	{:else}
		<div class="grid grid-cols-1 gap-4">
			{#each tokens as token}
				<div
					class="border-base-content/10 bg-base-100/50 hover:bg-base-100 hover:border-primary/20 group relative overflow-hidden rounded-2xl border p-6 transition-all duration-300 hover:scale-[1.01] hover:shadow-lg"
				>
					<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
						<div class="flex items-start gap-4">
							<div
								class="bg-primary/10 text-primary group-hover:bg-primary group-hover:text-primary-content rounded-xl p-3 transition-colors"
							>
								<Icon name="key" class="h-6 w-6" />
							</div>
							<div>
								<div class="flex flex-wrap items-center gap-3">
									<h3 class="text-lg font-bold">{token.name}</h3>
									{#if token.isActive}
										<span class="badge badge-success gap-1.5 shadow-sm">
											<span class="h-1.5 w-1.5 animate-pulse rounded-full bg-white"></span>
											{t('common.active', language.current)}
										</span>
									{:else}
										<span class="badge badge-error gap-1.5 shadow-sm">
											<span class="h-1.5 w-1.5 rounded-full bg-white"></span>
											{t('common.revoked', language.current)}
										</span>
									{/if}
								</div>
								<div
									class="text-base-content/60 mt-2 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm"
								>
									<span
										class="bg-base-200/80 border-base-content/5 rounded-md border px-2 py-1 font-mono text-xs"
										>{token.tokenPrefix}...</span
									>
									<span class="bg-base-content/30 h-1 w-1 rounded-full"></span>
									<span class="flex items-center gap-1">
										<Icon name="lock" class="h-3 w-3" />
										{t(
											`common.scopes.${token.scope.charAt(0).toLowerCase() + token.scope.slice(1)}`,
											language.current
										)}
									</span>
									<span class="bg-base-content/30 h-1 w-1 rounded-full"></span>
									<span>Created {formatDateTime(token.createdAt)}</span>
								</div>
							</div>
						</div>

						<div class="flex items-center gap-2 self-end sm:self-center">
							<Button
								size="sm"
								variant="ghost"
								class="text-error hover:bg-error/10 hover:text-error"
								onclick={() => openRevokeModal(token)}
								disabled={!token.isActive}
							>
								<Icon name="trash" class="mr-2 h-4 w-4" />
								{t('common.revoke', language.current)}
							</Button>
						</div>
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>

<Modal
	isOpen={showCreateModal}
	title={t('common.createTokenTitle', language.current)}
	onClose={() => (showCreateModal = false)}
>
	<form onsubmit={handleCreateToken} class="space-y-6">
		{#if errors.create}
			<div role="alert" class="alert alert-error bg-error/10 border-error/20 text-sm shadow-sm">
				<Icon name="error" class="text-error h-5 w-5" />
				<span>{errors.create}</span>
			</div>
		{/if}
		<Input
			label={t('common.tokenName', language.current)}
			bind:value={createForm.name}
			required
			placeholder="e.g. CI/CD Pipeline"
			class="bg-base-100/50"
		/>
		<Select
			label={t('common.scope', language.current)}
			bind:value={createForm.scope}
			options={[
				{ value: 'ReadOnly', label: t('common.scopes.readOnly', language.current) },
				{ value: 'ReadWrite', label: t('common.scopes.readWrite', language.current) }
			]}
			class="bg-base-100"
		/>
		<div class="modal-action pt-4">
			<Button type="button" variant="ghost" onclick={() => (showCreateModal = false)}
				>{t('common.cancel', language.current)}</Button
			>
			<Button
				type="submit"
				variant="primary"
				loading={isProcessing}
				class="shadow-primary/20 shadow-lg"
			>
				<Icon name="plus" class="mr-2 h-4 w-4" />
				{t('common.create', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<Modal
	isOpen={showTokenModal}
	title={t('common.newTokenTitle', language.current)}
	onClose={() => (showTokenModal = false)}
>
	{#if newTokenResponse}
		<div class="space-y-6">
			<div class="alert alert-warning bg-warning/10 border-warning/20 text-sm shadow-sm">
				<Icon name="warning" class="text-warning h-5 w-5" />
				<span>{t('common.copyTokenWarning', language.current)}</span>
			</div>

			<div class="form-control">
				<label class="label" for="new-token">
					<span class="label-text font-medium">Your API Token</span>
				</label>
				<div class="join w-full shadow-sm">
					<input
						id="new-token"
						type="text"
						readonly
						value={newTokenResponse.token}
						class="input input-bordered join-item bg-base-200/50 w-full font-mono text-sm focus:outline-none"
					/>
					<button
						class="btn btn-primary join-item"
						disabled={isCopied}
						onclick={() => copyToClipboard(newTokenResponse?.token ?? '')}
					>
						{#if isCopied}
							<Icon name="check" class="h-5 w-5" />
						{:else}
							<Icon name="document" class="h-5 w-5" />
						{/if}
					</button>
				</div>
			</div>

			<div class="modal-action">
				<Button onclick={() => (showTokenModal = false)} variant="ghost"
					>{t('common.close', language.current)}</Button
				>
			</div>
		</div>
	{/if}
</Modal>

<Modal
	isOpen={showRevokeModal}
	title={t('common.revokeTokenTitle', language.current)}
	onClose={() => (showRevokeModal = false)}
>
	{#if tokenToRevoke}
		<div class="space-y-6">
			<div class="bg-error/10 border-error/20 flex items-start gap-4 rounded-xl border p-4">
				<div class="bg-error/20 text-error rounded-full p-2">
					<Icon name="alert" class="h-6 w-6" />
				</div>
				<div>
					<h4 class="text-error mb-1 font-bold">Warning</h4>
					<p class="text-base-content/80 text-sm">
						Are you sure you want to revoke the token <span class="text-base-content font-bold"
							>{tokenToRevoke.name}</span
						>? This action cannot be undone and any applications using this token will stop working
						immediately.
					</p>
				</div>
			</div>

			<div class="modal-action">
				<Button variant="ghost" onclick={() => (showRevokeModal = false)}
					>{t('common.cancel', language.current)}</Button
				>
				<Button
					variant="danger"
					loading={isProcessing}
					onclick={handleRevokeToken}
					class="shadow-error/20 shadow-lg"
				>
					<Icon name="trash" class="mr-2 h-4 w-4" />
					{t('common.revoke', language.current)}
				</Button>
			</div>
		</div>
	{/if}
</Modal>
