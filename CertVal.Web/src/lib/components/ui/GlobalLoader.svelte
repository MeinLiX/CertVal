<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';

	interface Props {
		variant?: 'fullscreen' | 'inline' | 'overlay';
		size?: 'sm' | 'md' | 'lg';
		class?: string;
		'data-test-id'?: string;
	}

	let {
		variant = 'fullscreen',
		size = 'lg',
		class: className = '',
		'data-test-id': testId = 'global-loader'
	}: Props = $props();
</script>

<div
	class="loader loader--{variant} loader--{size} {className}"
	data-test-id={testId}
>
	<div class="loader__content">
		<div class="loader__spinner"></div>
		{#if size !== 'sm'}
			<span class="loader__text">
				{t('common.loading', language.current)}
			</span>
		{/if}
	</div>
</div>

<style>
	.loader {
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.loader--fullscreen {
		position: fixed;
		inset: 0;
		background-color: var(--color-bg);
		z-index: 9999;
	}

	.loader--overlay {
		position: absolute;
		inset: 0;
		background-color: rgba(var(--color-bg), 0.9);
		backdrop-filter: blur(4px);
		z-index: 50;
	}

	.loader--inline {
		padding: var(--space-8);
		min-height: 200px;
	}

	.loader__content {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: var(--space-4);
	}

	.loader__spinner {
		border: 3px solid var(--color-border);
		border-top-color: var(--color-primary);
		border-radius: var(--radius-full);
		animation: spin 0.8s linear infinite;
	}

	.loader--sm .loader__spinner {
		width: 24px;
		height: 24px;
		border-width: 2px;
	}

	.loader--md .loader__spinner {
		width: 40px;
		height: 40px;
	}

	.loader--lg .loader__spinner {
		width: 56px;
		height: 56px;
		border-width: 4px;
	}

	.loader__text {
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text-secondary);
		text-transform: uppercase;
		letter-spacing: 0.1em;
	}

	@keyframes spin {
		to { transform: rotate(360deg); }
	}
</style>
