<script lang="ts">
	import Icon from '$lib/components/ui/Icon.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	let {
		currentPage,
		totalPages,
		onPageChange,
		'data-test-id': testId
	}: {
		currentPage: number;
		totalPages: number;
		onPageChange: (page: number) => void;
		'data-test-id'?: string;
	} = $props();

	const pageNumbers = $derived(() => {
		const totalNumbers = 5;
		const siblingCount = 1;

		if (totalPages <= 7) {
			return Array.from({ length: totalPages }, (_, i) => i + 1);
		}

		const leftSiblingIndex = Math.max(currentPage - siblingCount, 1);
		const rightSiblingIndex = Math.min(currentPage + siblingCount, totalPages);

		const shouldShowLeftDots = leftSiblingIndex > 2;
		const shouldShowRightDots = rightSiblingIndex < totalPages - 1;

		const firstPageIndex = 1;
		const lastPageIndex = totalPages;

		if (!shouldShowLeftDots && shouldShowRightDots) {
			let leftItemCount = 3 + 2 * siblingCount;
			let leftRange = Array.from({ length: leftItemCount }, (_, i) => i + 1);
			return [...leftRange, -1, totalPages];
		}

		if (shouldShowLeftDots && !shouldShowRightDots) {
			let rightItemCount = 3 + 2 * siblingCount;
			let rightRange = Array.from(
				{ length: rightItemCount },
				(_, i) => totalPages - rightItemCount + i + 1
			);
			return [firstPageIndex, -1, ...rightRange];
		}

		if (shouldShowLeftDots && shouldShowRightDots) {
			let middleRange = Array.from(
				{ length: rightSiblingIndex - leftSiblingIndex + 1 },
				(_, i) => leftSiblingIndex + i
			);
			return [firstPageIndex, -1, ...middleRange, -1, lastPageIndex];
		}

		return [];
	});
</script>

<div class="join shadow-sm" data-test-id={testId}>
	<Button
		class="join-item {currentPage <= 1 ? 'btn-disabled opacity-50' : ''}"
		onclick={() => onPageChange(currentPage - 1)}
		disabled={currentPage <= 1}
		aria-label="Previous Page"
		data-test-id={testId ? `${testId}-prev` : undefined}
	>
		<Icon name="leftArrow" class="h-4 w-4" />
	</Button>

	{#each pageNumbers() as page}
		{#if page === -1}
			<Button class="join-item btn-disabled opacity-50" disabled>...</Button>
		{:else}
			<Button
				class="join-item"
				variant={currentPage === page ? 'primary' : 'ghost'}
				onclick={() => onPageChange(page)}
				data-test-id={testId ? `${testId}-page-${page}` : undefined}
			>
				{page}
			</Button>
		{/if}
	{/each}

	<Button
		class="join-item {currentPage >= totalPages ? 'btn-disabled opacity-50' : ''}"
		onclick={() => onPageChange(currentPage + 1)}
		disabled={currentPage >= totalPages}
		aria-label="Next Page"
		data-test-id={testId ? `${testId}-next` : undefined}
	>
		<Icon name="rightArrow" class="h-4 w-4" />
	</Button>
</div>
