<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		variant?: 'default' | 'bordered';
		hover?: boolean;
		clickable?: boolean;
		class?: string;
		onclick?: (event: MouseEvent) => void;
		children?: Snippet;
		'data-test-id'?: string;
	}

	let {
		variant = 'default',
		hover = false,
		clickable = false,
		class: className = '',
		onclick,
		children,
		'data-test-id': testId
	}: Props = $props();

	function handleClick(event: MouseEvent) {
		if (onclick) onclick(event);
	}

	function handleKeydown(event: KeyboardEvent) {
		if ((event.key === 'Enter' || event.key === ' ') && onclick) {
			event.preventDefault();
			onclick(event as unknown as MouseEvent);
		}
	}
</script>

{#if clickable}
<button
	class="card card--{variant} card--clickable {className}"
	onclick={handleClick}
	data-test-id={testId}
	type="button"
>
	{@render children?.()}
</button>
{:else}
<div
	class="card card--{variant} {className}"
	class:card--hover={hover}
	data-test-id={testId}
>
	{@render children?.()}
</div>
{/if}

<style>
	.card {
		background-color: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xl);
		padding: var(--space-6);
		transition: border-color var(--transition-fast), background-color var(--transition-fast);
		text-align: left;
		font-family: inherit;
		font-size: inherit;
		width: 100%;
	}

	.card--bordered {
		border-color: var(--color-border-strong);
	}

	.card--hover:hover {
		border-color: var(--color-border-hover);
	}

	.card--clickable {
		cursor: pointer;
	}

	.card--clickable:hover {
		border-color: var(--color-border-strong);
	}
</style>
