<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		eyebrow?: string;
		title: string;
		description?: string;
		class?: string;
		actions?: Snippet;
		'data-test-id'?: string;
	}

	let {
		eyebrow,
		title,
		description,
		class: className = '',
		actions,
		'data-test-id': testId
	}: Props = $props();
</script>

<header class="page-header {className}" data-test-id={testId}>
	<div class="page-header__text">
		{#if eyebrow}
			<span class="page-header__eyebrow">{eyebrow}</span>
		{/if}
		<h1 class="page-header__title">{title}</h1>
		{#if description}
			<p class="page-header__description">{description}</p>
		{/if}
	</div>
	{#if actions}
		<div class="page-header__actions">
			{@render actions?.()}
		</div>
	{/if}
</header>

<style>
	.page-header {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		gap: var(--space-6);
		padding-bottom: var(--space-8);
		margin-bottom: var(--space-10);
		border-bottom: 1px solid var(--color-border);
	}

	.page-header__text {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
		min-width: 0;
		flex: 1;
	}

	.page-header__eyebrow {
		font-family: var(--font-family);
		font-size: var(--text-xs);
		font-weight: var(--font-medium);
		letter-spacing: var(--tracking-wide);
		text-transform: uppercase;
		color: var(--color-text-muted);
	}

	.page-header__title {
		font-family: var(--font-display);
		font-size: var(--text-3xl);
		font-weight: var(--font-semibold);
		line-height: var(--leading-tight);
		letter-spacing: var(--tracking-tight);
		color: var(--color-text);
		margin: 0;
	}

	.page-header__description {
		font-size: var(--text-md);
		line-height: var(--leading-snug);
		color: var(--color-text-secondary);
		max-width: 60ch;
		margin: 0;
	}

	.page-header__actions {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		flex-shrink: 0;
	}

	@media (max-width: 720px) {
		.page-header {
			flex-direction: column;
			align-items: stretch;
		}

		.page-header__title {
			font-size: var(--text-2xl);
		}
	}
</style>
