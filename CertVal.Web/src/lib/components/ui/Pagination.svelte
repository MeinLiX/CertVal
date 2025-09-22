<script lang="ts">
	import Icon from '$lib/components/ui/Icon.svelte';

	let {
		currentPage,
		totalPages,
		onPageChange
	}: {
		currentPage: number;
		totalPages: number;
		onPageChange: (page: number) => void;
	} = $props();

	const pageNumbers = $derived(() => {
		const pages = [];
		const maxPagesToShow = 5;
		const half = Math.floor(maxPagesToShow / 2);

		if (totalPages <= maxPagesToShow + 2) {
			for (let i = 1; i <= totalPages; i++) {
				pages.push(i);
			}
		} else {
			pages.push(1);
			if (currentPage > half + 2) {
				pages.push(-1);
			}

			let start = Math.max(2, currentPage - half);
			let end = Math.min(totalPages - 1, currentPage + half);

			if (currentPage <= half + 1) {
				end = maxPagesToShow;
			}
			if (currentPage >= totalPages - half) {
				start = totalPages - maxPagesToShow + 1;
			}

			for (let i = start; i <= end; i++) {
				pages.push(i);
			}

			if (currentPage < totalPages - half - 1) {
				pages.push(-1);
			}
			pages.push(totalPages);
		}
		return pages;
	});
</script>

<div class="join">
	<button
		class="btn join-item"
		onclick={() => onPageChange(currentPage - 1)}
		disabled={currentPage <= 1}
		aria-label="Previous Page"
	>
		<Icon name="leftArrow" class="h-4 w-4" />
	</button>

	{#each pageNumbers() as page}
		{#if page === -1}
			<button class="btn join-item btn-disabled">...</button>
		{:else}
			<button
				class="btn join-item {currentPage === page ? 'btn-primary' : ''}"
				onclick={() => onPageChange(page)}
			>
				{page}
			</button>
		{/if}
	{/each}

	<button
		class="btn join-item"
		onclick={() => onPageChange(currentPage + 1)}
		disabled={currentPage >= totalPages}
		aria-label="Next Page"
	>
		<Icon name="rightArrow" class="h-4 w-4" />
	</button>
</div>
