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

	let modalRef = $state<HTMLDivElement>();

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
	<dialog
		class="modal modal-open"
		onclick={handleBackdropClick}
		onkeydown={(event) => {
			if (event.key === 'Escape') {
				onClose?.();
			}
		}}
		tabindex="0"
		aria-modal="true"
		aria-labelledby={title ? "modal-title" : undefined}
	>
		<div
			bind:this={modalRef}
			class="modal-box relative animate-in slide-in-from-bottom-8 duration-300"
			role="document"
		>
			<div class="flex justify-between items-center mb-4">
				{#if title}
					<h3 id="modal-title" class="font-bold text-lg text-base-content">{title}</h3>
				{/if}
				<button
					class="btn btn-sm btn-circle btn-ghost absolute right-2 top-2"
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
	</dialog>
{/if}