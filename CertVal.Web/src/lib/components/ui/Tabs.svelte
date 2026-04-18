<script lang="ts">
	import type { Snippet } from 'svelte';

	interface TabItem {
		id: string;
		label: string;
		count?: number;
		disabled?: boolean;
	}

	interface Props {
		items: TabItem[];
		active: string;
		onchange?: (id: string) => void;
		class?: string;
		'data-test-id'?: string;
	}

	let {
		items,
		active = $bindable(),
		onchange,
		class: className = '',
		'data-test-id': testId
	}: Props = $props();

	function select(id: string, disabled?: boolean) {
		if (disabled) return;
		active = id;
		onchange?.(id);
	}
</script>

<div class="tabs {className}" role="tablist" data-test-id={testId}>
	{#each items as item}
		{@const isActive = item.id === active}
		<button
			type="button"
			role="tab"
			aria-selected={isActive}
			aria-controls={`panel-${item.id}`}
			id={`tab-${item.id}`}
			class="tabs__item"
			class:tabs__item--active={isActive}
			disabled={item.disabled}
			onclick={() => select(item.id, item.disabled)}
			data-test-id={testId ? `${testId}-tab-${item.id}` : undefined}
		>
			<span>{item.label}</span>
			{#if item.count !== undefined}
				<span class="tabs__count">{item.count}</span>
			{/if}
		</button>
	{/each}
</div>

<style>
	.tabs {
		display: flex;
		align-items: center;
		gap: var(--space-1);
		border-bottom: 1px solid var(--color-border);
		overflow-x: auto;
		scrollbar-width: none;
	}

	.tabs::-webkit-scrollbar {
		display: none;
	}

	.tabs__item {
		position: relative;
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-3) var(--space-4);
		margin-bottom: -1px;
		background: none;
		border: 0;
		border-bottom: 2px solid transparent;
		font-family: var(--font-family);
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text-muted);
		white-space: nowrap;
		cursor: pointer;
		transition: color var(--transition-fast), border-color var(--transition-fast);
	}

	.tabs__item:hover:not(:disabled):not(.tabs__item--active) {
		color: var(--color-text);
	}

	.tabs__item:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.tabs__item--active {
		color: var(--color-text);
		border-bottom-color: var(--color-text);
	}

	.tabs__count {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 20px;
		height: 20px;
		padding: 0 var(--space-2);
		font-size: var(--text-xs);
		font-weight: var(--font-medium);
		color: var(--color-text-secondary);
		background-color: var(--color-surface-inset);
		border-radius: var(--radius-full);
	}

	.tabs__item--active .tabs__count {
		background-color: var(--color-primary-light);
		color: var(--color-primary);
	}
</style>
