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

	// Focus management
	let modalRef: HTMLDivElement;
	
	$effect(() => {
		if (isOpen && modalRef) {
			const focusableElements = modalRef.querySelectorAll(
				'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
			);
			const firstElement = focusableElements[0] as HTMLElement;
			if (firstElement) {
				firstElement.focus();
			}
		}
	});
</script>

<svelte:window onkeydown={handleKeyDown} />

{#if isOpen}
	<div
		class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50 flex items-center justify-center p-4"
		onclick={handleBackdropClick}
		role="dialog"
		aria-modal="true"
		aria-labelledby={title ? "modal-title" : undefined}
	>
		<div
			bind:this={modalRef}
			class="relative bg-white rounded-lg shadow-xl max-w-md w-full max-h-full overflow-y-auto"
			onclick={(e) => e.stopPropagation()}
		>
			<div class="p-6">
				<div class="flex justify-between items-center mb-4">
					{#if title}
						<h3 id="modal-title" class="text-lg font-medium text-gray-900">{title}</h3>
					{/if}
					<button
						class="text-gray-400 hover:text-gray-600 focus:outline-none focus:ring-2 focus:ring-blue-500 rounded"
						onclick={onClose}
						aria-label="Close modal"
					>
						<svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
						</svg>
					</button>
				</div>
				
				{@render children?.()}
			</div>
		</div>
	</div>
{/if}