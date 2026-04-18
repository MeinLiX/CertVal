<script lang="ts">
	import Icon from './Icon.svelte';
	import Button from './Button.svelte';

	interface Props {
		currentPage: number;
		totalPages: number;
		onPageChange: (page: number) => void;
		'data-test-id'?: string;
	}

	let {
		currentPage,
		totalPages,
		onPageChange,
		'data-test-id': testId
	}: Props = $props();

	const pageNumbers = $derived(() => {
		if (totalPages <= 7) {
			return Array.from({ length: totalPages }, (_, i) => i + 1);
		}

		const leftSibling = Math.max(currentPage - 1, 1);
		const rightSibling = Math.min(currentPage + 1, totalPages);
		const showLeftDots = leftSibling > 2;
		const showRightDots = rightSibling < totalPages - 1;

		if (!showLeftDots && showRightDots) {
			const leftRange = Array.from({ length: 5 }, (_, i) => i + 1);
			return [...leftRange, -1, totalPages];
		}

		if (showLeftDots && !showRightDots) {
			const rightRange = Array.from({ length: 5 }, (_, i) => totalPages - 4 + i);
			return [1, -1, ...rightRange];
		}

		const middleRange = Array.from(
			{ length: rightSibling - leftSibling + 1 },
			(_, i) => leftSibling + i
		);
		return [1, -1, ...middleRange, -1, totalPages];
	});
</script>

<nav class="pagination" aria-label="Pagination" data-test-id={testId}>
	<Button
		variant="secondary"
		size="sm"
		icon
		disabled={currentPage <= 1}
		onclick={() => onPageChange(currentPage - 1)}
		data-test-id={testId ? `${testId}-prev` : undefined}
	>
		<Icon name="leftArrow" size="sm" />
	</Button>

	<div class="pagination__pages">
		{#each pageNumbers() as page, i (i)}
			{#if page === -1}
				<span class="pagination__dots">...</span>
			{:else}
				<button
					class="pagination__page"
					class:pagination__page--active={currentPage === page}
					onclick={() => onPageChange(page)}
					data-test-id={testId ? `${testId}-page-${page}` : undefined}
				>
					{page}
				</button>
			{/if}
		{/each}
	</div>

	<Button
		variant="secondary"
		size="sm"
		icon
		disabled={currentPage >= totalPages}
		onclick={() => onPageChange(currentPage + 1)}
		data-test-id={testId ? `${testId}-next` : undefined}
	>
		<Icon name="rightArrow" size="sm" />
	</Button>
</nav>

<style>
	.pagination {
		display: flex;
		align-items: center;
		gap: var(--space-1);
	}

	.pagination__pages {
		display: flex;
		align-items: center;
		gap: var(--space-1);
	}

	.pagination__page {
		display: flex;
		align-items: center;
		justify-content: center;
		min-width: 36px;
		height: 36px;
		padding: 0 var(--space-2);
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text-secondary);
		background-color: transparent;
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		cursor: pointer;
		transition: all var(--transition-fast);
	}

	.pagination__page:hover {
		background-color: var(--color-surface-hover);
		border-color: var(--color-border-hover);
	}

	.pagination__page--active {
		background-color: var(--color-primary);
		color: var(--color-primary-text);
		border-color: var(--color-primary);
	}

	.pagination__page--active:hover {
		background-color: var(--color-primary-hover);
	}

	.pagination__dots {
		padding: 0 var(--space-2);
		color: var(--color-text-muted);
	}

	@media (max-width: 640px) {
		.pagination__pages {
			display: none;
		}
	}
</style>
