<script lang="ts">
	import { tick } from 'svelte';

	let {
		isOpen = false,
		title = '',
		onClose,
		children,
		class: className = '',
		allowClickOutsideToClose = false
	}: {
		isOpen?: boolean;
		title?: string;
		onClose?: () => void;
		children?: any;
		class?: string;
		allowClickOutsideToClose?: boolean;
	} = $props();

	let dialog: HTMLDialogElement | undefined = $state();
	let isVisible = $state(false);
	$effect(() => {
		if (isOpen) {
			isVisible = true;
			tick().then(() => {
				dialog?.showModal();
			});
		} else {
			if (dialog) {
				dialog.classList.add('modal-closing');
				dialog.addEventListener(
					'animationend',
					() => {
						dialog?.classList.remove('modal-closing');
						dialog?.close();
						isVisible = false;
					},
					{ once: true }
				);
			}
		}
	});
</script>

{#if isVisible}
	<dialog
		bind:this={dialog}
		class="modal {className}"
		oncancel={(e) => {
			e.preventDefault();
			if (allowClickOutsideToClose) {
				onClose?.();
			}
		}}
		onclick={(e) => {
			if (allowClickOutsideToClose && e.currentTarget === e.target) {
				onClose?.();
			}
		}}
	>
		<div class="modal-box">
			{#if title}
				<h3 class="text-lg font-bold">{title}</h3>
			{/if}
			<button class="btn absolute top-2 right-2 btn-circle btn-ghost btn-sm" onclick={onClose}>
				✕
			</button>
			<div>
				{@render children?.()}
			</div>
		</div>
	</dialog>
{/if}

<style>
	dialog.modal {
		position: fixed;
		inset: 0;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: 1rem;
		margin: 0;
		z-index: 60;
	}

	dialog.modal::backdrop {
		background-color: rgba(0, 0, 0, 0.45);
		backdrop-filter: none;
	}

	.modal-box {
		max-width: min(720px, 95vw);
		width: 100%;
		margin: 0 auto;
		border-radius: 0.5rem;
		box-shadow: 0 10px 30px rgba(2, 6, 23, 0.2);
	}

	.modal {
		animation: fade-in 0.2s ease-out forwards;
	}
	.modal-box {
		animation: slide-up 0.2s ease-out forwards;
	}
	.modal-closing {
		animation: fade-out 0.2s ease-in forwards;
	}
	.modal-closing .modal-box {
		animation: slide-down 0.2s ease-in forwards;
	}
	@keyframes fade-in {
		from {
			opacity: 0;
		}
		to {
			opacity: 1;
		}
	}
	@keyframes fade-out {
		from {
			opacity: 1;
		}
		to {
			opacity: 0;
		}
	}
	@keyframes slide-up {
		from {
			transform: translateY(20px);
			opacity: 0;
		}
		to {
			transform: translateY(0);
			opacity: 1;
		}
	}
	@keyframes slide-down {
		from {
			transform: translateY(0);
			opacity: 1;
		}
		to {
			transform: translateY(20px);
			opacity: 0;
		}
	}
</style>
