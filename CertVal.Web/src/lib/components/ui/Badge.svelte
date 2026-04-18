<script lang="ts">
	import type { Snippet } from 'svelte';

	type Variant = 'neutral' | 'primary' | 'success' | 'warning' | 'danger';

	interface Props {
		variant?: Variant;
		class?: string;
		children?: Snippet;
		'data-test-id'?: string;
	}

	let {
		variant = 'neutral',
		class: className = '',
		children,
		'data-test-id': testId
	}: Props = $props();
</script>

<span class="badge badge--{variant} {className}" data-test-id={testId}>
	{@render children?.()}
</span>

<style>
	.badge {
		display: inline-flex;
		align-items: center;
		gap: var(--space-1);
		padding: 2px var(--space-2);
		font-size: var(--text-xs);
		font-weight: var(--font-medium);
		line-height: 1.4;
		border-radius: var(--radius-full);
		white-space: nowrap;
		border: 1px solid transparent;
	}

	.badge--neutral {
		background-color: var(--color-surface-inset);
		color: var(--color-text-secondary);
		border-color: var(--color-border);
	}

	.badge--primary {
		background-color: var(--color-primary-light);
		color: var(--color-primary);
	}

	.badge--success {
		background-color: var(--color-success-light);
		color: var(--color-success);
	}

	.badge--warning {
		background-color: var(--color-warning-light);
		color: var(--color-warning);
	}

	.badge--danger {
		background-color: var(--color-error-light);
		color: var(--color-error);
	}
</style>
