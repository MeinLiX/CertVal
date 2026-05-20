<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';

	interface Props {
		loading?: boolean;
		error?: string | null;
	}

	let { loading = false, error = null }: Props = $props();

	const visible = $derived(loading || !!error);
</script>

{#if visible}
	<div class="init-toast init-toast--{error ? 'error' : 'loading'}" role="status" aria-live="polite">
		{#if !error}
			<span class="init-toast__spinner" aria-hidden="true"></span>
		{:else}
			<span class="init-toast__icon" aria-hidden="true">✕</span>
		{/if}
		<span class="init-toast__text">
			{#if error}
				{t('utils.initError', language.current)} {error}
			{:else}
				{t('utils.iitInit', language.current)}
			{/if}
		</span>
	</div>
{/if}

<style>
	.init-toast {
		position: fixed;
		bottom: 1.5rem;
		left: 1.5rem;
		z-index: 200;
		display: flex;
		align-items: center;
		gap: 0.625rem;
		padding: 0.625rem 1rem;
		border-radius: 0.625rem;
		border: 1px solid var(--color-border);
		background: var(--color-surface, #fff);
		box-shadow: var(--shadow-pop);
		font-size: 0.8125rem;
		color: var(--color-text);
		max-width: 28rem;
		animation: toast-in 0.2s ease;
	}

	.init-toast--error {
		border-color: #f87171;
		background: #fef2f2;
		color: #b91c1c;
	}

	:global([data-theme='dark']) .init-toast--error {
		background: #2d1515;
		border-color: #7f1d1d;
		color: #fca5a5;
	}

	.init-toast__spinner {
		flex-shrink: 0;
		width: 1rem;
		height: 1rem;
		border: 2px solid var(--color-border);
		border-top-color: var(--color-primary, #4f46ff);
		border-radius: 50%;
		animation: spin 0.7s linear infinite;
	}

	.init-toast__icon {
		flex-shrink: 0;
		font-size: 0.75rem;
		font-weight: 700;
	}

	.init-toast__text {
		line-height: 1.4;
		word-break: break-word;
	}

	@keyframes spin {
		to { transform: rotate(360deg); }
	}

	@keyframes toast-in {
		from { opacity: 0; transform: translateY(0.5rem); }
		to   { opacity: 1; transform: translateY(0); }
	}
</style>
