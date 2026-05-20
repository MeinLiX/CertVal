<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import { utilsBlocks } from '$lib/utils/utilsNav';

	interface Props {
		description?: string;
	}

	let { description }: Props = $props();

	const currentPath = $derived(page.url.pathname);
</script>

<div class="utils-subnav">
	{#each utilsBlocks as block}
		<div class="utils-subnav__row">
			<span class="utils-subnav__label">
				{t(block.titleKey, language.current)}
				{#if block.badge}<span class="utils-subnav__badge">{block.badge}</span>{/if}
			</span>
			<div class="utils-subnav__pills">
				{#each block.tools as tool}
					<button
						class="utils-subnav__pill"
						class:utils-subnav__pill--active={currentPath === tool.href}
						onclick={() => goto(tool.href)}
					>
						{t(tool.labelKey, language.current)}
					</button>
				{/each}
			</div>
		</div>
	{/each}

	{#if description}
		<p class="utils-subnav__desc">{description}</p>
	{/if}
</div>

<style>
	.utils-subnav {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
		padding-bottom: var(--space-8);
		margin-bottom: var(--space-8);
		border-bottom: 1px solid var(--color-border);
	}

	.utils-subnav__row {
		display: flex;
		align-items: center;
		gap: var(--space-4);
		flex-wrap: wrap;
	}

	.utils-subnav__label {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		font-size: var(--text-sm);
		font-weight: var(--font-semibold);
		color: var(--color-text-muted);
		white-space: nowrap;
	}

	.utils-subnav__badge {
		display: inline-flex;
		align-items: center;
		padding: 0.1rem 0.45rem;
		background: var(--color-primary);
		color: #fff;
		border-radius: var(--radius-sm);
		font-family: var(--font-mono);
		font-size: 0.6rem;
		font-weight: 700;
		letter-spacing: 0.06em;
		line-height: 1.5;
	}

	.utils-subnav__pills {
		display: flex;
		align-items: center;
		gap: var(--space-1);
		flex-wrap: wrap;
	}

	.utils-subnav__pill {
		background: none;
		border: 1px solid var(--color-border);
		border-radius: var(--radius-full);
		padding: var(--space-1) var(--space-4);
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
		cursor: pointer;
		transition:
			color var(--transition-fast),
			background-color var(--transition-fast),
			border-color var(--transition-fast);
		white-space: nowrap;
	}

	.utils-subnav__pill:hover {
		color: var(--color-text);
		background-color: var(--color-surface-hover);
		border-color: var(--color-border-hover);
	}

	.utils-subnav__pill--active {
		background-color: var(--color-primary-light);
		border-color: var(--color-primary);
		color: var(--color-primary);
		font-weight: var(--font-semibold);
	}

	.utils-subnav__pill--active:hover {
		background-color: var(--color-primary-light);
		border-color: var(--color-primary);
		color: var(--color-primary);
	}

	.utils-subnav__desc {
		margin: 0;
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
		line-height: 1.6;
	}
</style>
