<script lang="ts">
	interface Props {
		isOpen?: boolean;
		title?: string;
		onClose?: () => void;
		children?: any;
	}

	let {
		isOpen = false,
		title = '',
		onClose,
		children
	}: Props = $props();

	function handleBackdropClick(event: MouseEvent) {
		if (event.target === event.currentTarget) {
			onClose?.();
		}
	}

	function handleKeyDown(event: KeyboardEvent) {
		if (event.key === 'Escape' && isOpen) {
			onClose?.();
		}
	}
</script>

<svelte:window onkeydown={handleKeyDown} />

{#if isOpen}
	<div
		class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50"
		onclick={handleBackdropClick}
	>
		<div class="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
			<div class="flex justify-between items-center mb-4">
				{#if title}
					<h3 class="text-lg font-medium text-gray-900">{title}</h3>
				{/if}
				<button
					class="text-gray-400 hover:text-gray-600"
					onclick={onClose}
				>
					<svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
					</svg>
				</button>
			</div>
			
			{@render children?.()}
		</div>
	</div>
{/if}