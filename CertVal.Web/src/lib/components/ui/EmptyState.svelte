<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { IconName } from '$lib/icons';
	import Icon from './Icon.svelte';

	interface Props {
		icon?: IconName;
		title: string;
		description?: string;
		action?: Snippet;
		class?: string;
		'data-test-id'?: string;
	}

	let {
		icon,
		title,
		description,
		action,
		class: className = '',
		'data-test-id': testId
	}: Props = $props();
</script>

<div class="empty {className}" data-test-id={testId}>
	{#if icon}
		<div class="empty__icon" aria-hidden="true">
			<Icon name={icon} size="lg" />
		</div>
	{/if}
	<h3 class="empty__title">{title}</h3>
	{#if description}
		<p class="empty__description">{description}</p>
	{/if}
	{#if action}
		<div class="empty__action">
			{@render action?.()}
		</div>
	{/if}
</div>

<style>
	.empty {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
		padding: var(--space-16) var(--space-6);
		gap: var(--space-3);
	}

	.empty__icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 56px;
		height: 56px;
		border-radius: var(--radius-full);
		background-color: var(--color-surface-inset);
		color: var(--color-text-muted);
		margin-bottom: var(--space-3);
	}

	.empty__title {
		font-family: var(--font-display);
		font-size: var(--text-xl);
		font-weight: var(--font-semibold);
		color: var(--color-text);
		margin: 0;
	}

	.empty__description {
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
		max-width: 40ch;
		margin: 0;
	}

	.empty__action {
		margin-top: var(--space-4);
	}
</style>
