<script lang="ts">
	import type { Snippet } from 'svelte';

	export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'warning' | 'outline' | 'link';
	type ButtonSize = 'sm' | 'md' | 'lg';

	interface Props {
		variant?: ButtonVariant;
		size?: ButtonSize;
		disabled?: boolean;
		loading?: boolean;
		type?: 'button' | 'submit' | 'reset';
		href?: string;
		icon?: boolean;
		fullWidth?: boolean;
		class?: string;
		title?: string;
		form?: string;
		target?: '_blank' | '_self' | '_parent' | '_top';
		rel?: string;
		onclick?: (event: MouseEvent) => void | Promise<void>;
		children?: Snippet;
		'data-test-id'?: string;
	}

	let {
		variant = 'primary',
		size = 'md',
		disabled = false,
		loading = false,
		type = 'button',
		href,
		icon = false,
		fullWidth = false,
		class: className = '',
		title,
		form,
		target,
		rel,
		onclick,
		children,
		'data-test-id': testId
	}: Props = $props();

	async function handleClick(event: MouseEvent) {
		if (disabled || loading || !onclick) return;
		await onclick(event);
	}
</script>

{#if href && !disabled}
	<a
		{href}
		{title}
		{target}
		{rel}
		class="btn btn--{variant} btn--{size} {icon ? 'btn--icon' : ''} {fullWidth ? 'btn--full' : ''} {className}"
		class:btn--loading={loading}
		data-test-id={testId}
	>
		{#if loading}
			<span class="btn__spinner"></span>
		{/if}
		{@render children?.()}
	</a>
{:else}
	<button
		{type}
		{title}
		{form}
		class="btn btn--{variant} btn--{size} {icon ? 'btn--icon' : ''} {fullWidth ? 'btn--full' : ''} {className}"
		class:btn--loading={loading}
		disabled={disabled || loading}
		onclick={handleClick}
		data-test-id={testId}
	>
		{#if loading}
			<span class="btn__spinner"></span>
		{/if}
		{@render children?.()}
	</button>
{/if}

<style>
	.btn {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: var(--space-2);
		font-family: var(--font-family);
		font-weight: var(--font-medium);
		line-height: 1;
		border: 1px solid transparent;
		border-radius: var(--radius-md);
		cursor: pointer;
		transition: background-color var(--transition-fast), border-color var(--transition-fast), color var(--transition-fast);
		white-space: nowrap;
		user-select: none;
		text-decoration: none;
	}

	.btn:disabled {
		opacity: 0.45;
		cursor: not-allowed;
	}

	/* Sizes */
	.btn--sm {
		padding: 0 var(--space-3);
		font-size: var(--text-xs);
		height: 32px;
	}

	.btn--md {
		padding: 0 var(--space-4);
		font-size: var(--text-sm);
		height: 38px;
	}

	.btn--lg {
		padding: 0 var(--space-6);
		font-size: var(--text-base);
		height: 46px;
	}

	/* Full Width */
	.btn--full {
		width: 100%;
	}

	/* Icon Button */
	.btn--icon {
		padding: 0;
		aspect-ratio: 1;
	}
	.btn--icon.btn--sm { width: 32px; }
	.btn--icon.btn--md { width: 38px; }
	.btn--icon.btn--lg { width: 46px; }

	/* Variants */
	.btn--primary {
		background-color: var(--color-primary);
		color: var(--color-primary-text);
	}
	.btn--primary:hover:not(:disabled) {
		background-color: var(--color-primary-hover);
	}

	.btn--secondary {
		background-color: var(--color-surface);
		color: var(--color-text);
		border-color: var(--color-border);
	}
	.btn--secondary:hover:not(:disabled) {
		background-color: var(--color-surface-hover);
		border-color: var(--color-border-hover);
	}

	.btn--ghost {
		background-color: transparent;
		color: var(--color-text-secondary);
	}
	.btn--ghost:hover:not(:disabled) {
		background-color: var(--color-surface-hover);
		color: var(--color-text);
	}

	.btn--danger {
		background-color: var(--color-error);
		color: white;
	}
	.btn--danger:hover:not(:disabled) {
		background-color: #b91c1c;
	}

	.btn--warning {
		background-color: var(--color-warning);
		color: white;
	}
	.btn--warning:hover:not(:disabled) {
		background-color: #d97706;
	}

	.btn--outline {
		background-color: transparent;
		color: var(--color-text);
		border-color: var(--color-border);
	}
	.btn--outline:hover:not(:disabled) {
		background-color: var(--color-surface-hover);
		border-color: var(--color-border-hover);
	}

	.btn--link {
		background: none;
		color: var(--color-primary);
		padding: 0;
		height: auto;
	}
	.btn--link:hover:not(:disabled) {
		text-decoration: underline;
	}

	/* Loading */
	.btn--loading {
		position: relative;
		color: transparent !important;
	}

	.btn__spinner {
		position: absolute;
		width: 16px;
		height: 16px;
		border: 2px solid currentColor;
		border-top-color: transparent;
		border-radius: var(--radius-full);
		animation: spin 0.6s linear infinite;
	}

	.btn--primary .btn__spinner,
	.btn--danger .btn__spinner {
		border-color: rgba(255,255,255,0.3);
		border-top-color: white;
	}

	@keyframes spin {
		to { transform: rotate(360deg); }
	}
</style>
