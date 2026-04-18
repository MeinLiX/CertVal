<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { ApiTokenService } from '$lib/services/ApiTokenService';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import FloatingActionBar from '$lib/components/layout/FloatingActionBar.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
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

<div class="page" data-test-id="api-tokens-page">
	{#if isLoading}
		<div class="skeleton-list">
			{#each { length: 3 } as _}
				<div class="skeleton skeleton--card"></div>
			{/each}
		</div>
	{:else if tokens.length === 0}
		<div class="empty-state">
			<div class="empty-state__icon">
				<Icon name="key" />
			</div>
			<h3 class="empty-state__title">{t('common.noTokens', language.current)}</h3>
			<p class="empty-state__description">
				Create a token to access the API programmatically. Tokens allow you to authenticate with the
				API without using your password.
			</p>
			<Button onclick={openCreateModal} variant="outline">
				Create your first token
			</Button>
		</div>
	{:else}
		<div class="tokens-list">
			{#each tokens as token}
				<div class="token-card" data-test-id={`token-item-${token.id}`}>
					<div class="token-card__icon">
						<Icon name="key" />
					</div>
					<div class="token-card__content">
						<div class="token-card__header">
							<h3 class="token-card__name">{token.name}</h3>
							{#if token.isActive}
								<span class="badge badge--success">
									<span class="badge__indicator"></span>
									{t('common.active', language.current)}
								</span>
							{:else}
								<span class="badge badge--error">
									{t('common.revoked', language.current)}
								</span>
							{/if}
						</div>
						<div class="token-card__meta">
							<span class="token-card__prefix">{token.tokenPrefix}...</span>
							<span class="token-card__separator"></span>
							<span class="token-card__scope">
								<Icon name="lock" />
								{t(`common.scopes.${token.scope.charAt(0).toLowerCase() + token.scope.slice(1)}`, language.current)}
							</span>
							<span class="token-card__separator"></span>
							<span>Created {formatDateTime(token.createdAt)}</span>
						</div>
					</div>
					<div class="token-card__actions">
						<Button
							size="sm"
							variant="ghost"
							onclick={() => openRevokeModal(token)}
							disabled={!token.isActive}
							data-test-id={`revoke-token-button-${token.id}`}
						>
							<Icon name="trash" />
							{t('common.revoke', language.current)}
						</Button>
					</div>
				</div>
			{/each}
		</div>
	{/if}

	<FloatingActionBar label={t('nav.apiTokens', language.current)}>
		{#snippet trailing()}
			<Button onclick={openCreateModal} variant="primary" data-test-id="create-token-button">
				<Icon name="plus" />
				{t('common.create', language.current)}
			</Button>
		{/snippet}
	</FloatingActionBar>
</div>

<Modal
	isOpen={showCreateModal}
	title={t('common.createTokenTitle', language.current)}
	onClose={() => (showCreateModal = false)}
	data-test-id="create-token-modal"
>
	<form onsubmit={handleCreateToken} class="modal-form">
		{#if errors.create}
			<div class="alert alert--error">
				<Icon name="error" />
				<span>{errors.create}</span>
			</div>
		{/if}
		<Input
			label={t('common.tokenName', language.current)}
			bind:value={createForm.name}
			required
			placeholder="e.g. CI/CD Pipeline"
			data-test-id="token-name-input"
		/>
		<Select
			label={t('common.scope', language.current)}
			bind:value={createForm.scope}
			options={[
				{ value: 'ReadOnly', label: t('common.scopes.readOnly', language.current) },
				{ value: 'ReadWrite', label: t('common.scopes.readWrite', language.current) }
			]}
			data-test-id="token-scope-select"
		/>
		<div class="modal-form__actions">
			<Button type="button" variant="ghost" onclick={() => { showCreateModal = false; }}>
				{t('common.cancel', language.current)}
			</Button>
			<Button
				type="submit"
				variant="primary"
				loading={isProcessing}
				data-test-id="token-submit-button"
			>
				<Icon name="plus" />
				{t('common.create', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<Modal
	isOpen={showTokenModal}
	title={t('common.newTokenTitle', language.current)}
	onClose={() => (showTokenModal = false)}
	data-test-id="new-token-modal"
>
	{#if newTokenResponse}
		<div class="modal-form">
			<div class="alert alert--warning">
				<Icon name="warning" />
				<span>{t('common.copyTokenWarning', language.current)}</span>
			</div>

			<div class="token-display">
				<label class="form-label" for="new-token">Your API Token</label>
				<div class="token-display__row">
					<input
						id="new-token"
						type="text"
						readonly
						value={newTokenResponse.token}
						class="token-display__input"
						data-test-id="new-token-input"
					/>
					<Button
						variant="primary"
						disabled={isCopied}
						onclick={() => copyToClipboard(newTokenResponse?.token ?? '')}
						data-test-id="copy-token-button"
					>
						{#if isCopied}
							<Icon name="check" />
						{:else}
							<Icon name="document" />
						{/if}
					</Button>
				</div>
			</div>

			<div class="modal-form__actions">
				<Button onclick={() => { showTokenModal = false; }} variant="ghost">
					{t('common.close', language.current)}
				</Button>
			</div>
		</div>
	{/if}
</Modal>

<Modal
	isOpen={showRevokeModal}
	title={t('common.revokeTokenTitle', language.current)}
	onClose={() => { showRevokeModal = false; }}
	data-test-id="revoke-token-modal"
>
	{#if tokenToRevoke}
		<div class="modal-form">
			<div class="warning-box">
				<div class="warning-box__icon">
					<Icon name="alert" />
				</div>
				<div class="warning-box__content">
					<h4 class="warning-box__title">Warning</h4>
					<p class="warning-box__text">
						Are you sure you want to revoke the token <strong>{tokenToRevoke.name}</strong>?
						This action cannot be undone and any applications using this token will stop working immediately.
					</p>
				</div>
			</div>

			<div class="modal-form__actions">
				<Button variant="ghost" onclick={() => { showRevokeModal = false; }}>
					{t('common.cancel', language.current)}
				</Button>
				<Button
					variant="danger"
					loading={isProcessing}
					onclick={handleRevokeToken}
					data-test-id="revoke-token-confirm-button"
				>
					<Icon name="trash" />
					{t('common.revoke', language.current)}
				</Button>
			</div>
		</div>
	{/if}
</Modal>

<style>
	.page {
		display: flex;
		flex-direction: column;
		gap: var(--space-8);
	}

	.skeleton-list {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.skeleton {
		background: linear-gradient(90deg, var(--color-bg-secondary) 25%, var(--color-surface) 50%, var(--color-bg-secondary) 75%);
		background-size: 200% 100%;
		animation: shimmer 1.5s infinite;
		border-radius: var(--radius-lg);
	}

	.skeleton--card {
		height: 96px;
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
		padding: var(--space-16) var(--space-8);
		background: var(--color-bg-secondary);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xl);
		text-align: center;
	}

	.empty-state__icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 80px;
		height: 80px;
		margin-bottom: var(--space-6);
		background: var(--color-surface);
		border-radius: 50%;
		color: var(--color-text-muted);
	}

	.empty-state__title {
		font-family: var(--font-display);
		font-size: var(--text-xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		color: var(--color-text);
		margin-bottom: var(--space-2);
	}

	.empty-state__description {
		font-size: var(--text-base);
		color: var(--color-text-secondary);
		max-width: 400px;
		margin-bottom: var(--space-6);
	}

	.tokens-list {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.token-card {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
		padding: var(--space-6);
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		transition: border-color var(--transition-fast);
	}

	@media (min-width: 640px) {
		.token-card {
			flex-direction: row;
			align-items: center;
		}
	}

	.token-card:hover {
		border-color: var(--color-border-strong);
	}

	.token-card__icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 48px;
		height: 48px;
		background: var(--color-primary-bg);
		color: var(--color-primary);
		border-radius: var(--radius-lg);
		flex-shrink: 0;
	}

	.token-card__content {
		flex: 1;
		min-width: 0;
	}

	.token-card__header {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-3);
		margin-bottom: var(--space-2);
	}

	.token-card__name {
		font-size: var(--text-md);
		font-weight: var(--font-semibold);
		color: var(--color-text);
	}

	.token-card__meta {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-3);
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
	}

	.token-card__prefix {
		padding: var(--space-1) var(--space-2);
		background: var(--color-bg-secondary);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-sm);
		font-family: var(--font-mono);
		font-size: var(--text-xs);
	}

	.token-card__separator {
		width: 4px;
		height: 4px;
		background: var(--color-border);
		border-radius: 50%;
	}

	.token-card__scope {
		display: flex;
		align-items: center;
		gap: var(--space-1);
	}

	.token-card__actions {
		flex-shrink: 0;
	}

	.badge {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-1) var(--space-3);
		border-radius: var(--radius-full);
		font-size: var(--text-xs);
		font-weight: 500;
	}

	.badge--success {
		background: var(--color-success-bg);
		color: var(--color-success);
	}

	.badge--error {
		background: var(--color-error-bg);
		color: var(--color-error);
	}

	.badge__indicator {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: currentColor;
		animation: pulse 2s infinite;
	}

	@keyframes pulse {
		0%, 100% { opacity: 1; }
		50% { opacity: 0.5; }
	}

	.modal-form {
		display: flex;
		flex-direction: column;
		gap: var(--space-6);
	}

	.modal-form__actions {
		display: flex;
		justify-content: flex-end;
		gap: var(--space-3);
		padding-top: var(--space-4);
	}

	.alert {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		padding: var(--space-4);
		border-radius: var(--radius-lg);
		font-size: var(--text-sm);
	}

	.alert--error {
		background: var(--color-error-bg);
		color: var(--color-error);
		border: 1px solid var(--color-error);
	}

	.alert--warning {
		background: var(--color-warning-bg);
		color: var(--color-warning);
		border: 1px solid var(--color-warning);
	}

	.form-label {
		display: block;
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text-primary);
		margin-bottom: var(--space-2);
	}

	.token-display {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.token-display__row {
		display: flex;
		gap: var(--space-2);
	}

	.token-display__input {
		flex: 1;
		padding: var(--space-3) var(--space-4);
		background: var(--color-bg-secondary);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		font-family: var(--font-mono);
		font-size: var(--text-sm);
		color: var(--color-text-primary);
	}

	.warning-box {
		display: flex;
		gap: var(--space-4);
		padding: var(--space-4);
		background: var(--color-error-bg);
		border: 1px solid var(--color-error);
		border-radius: var(--radius-lg);
	}

	.warning-box__icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 40px;
		height: 40px;
		background: var(--color-error);
		color: white;
		border-radius: 50%;
		flex-shrink: 0;
	}

	.warning-box__content {
		flex: 1;
	}

	.warning-box__title {
		font-size: var(--text-base);
		font-weight: var(--font-semibold);
		color: var(--color-error);
		margin-bottom: var(--space-1);
	}

	.warning-box__text {
		font-size: var(--text-sm);
		color: var(--color-text-primary);
		line-height: 1.5;
	}
</style>
