<script lang="ts">
	import { tick } from 'svelte';

	let {
		isOpen = false,
		title = '',
		onClose,
		children,
		class: className = '',
		allowClickOutsideToClose = false,
		'data-test-id': testId
	}: {
		isOpen?: boolean;
		title?: string;
		onClose?: () => void;
		children?: any;
		class?: string;
		allowClickOutsideToClose?: boolean;
		'data-test-id'?: string;
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

				const closeDialog = () => {
					dialog?.classList.remove('modal-closing');
					dialog?.close();
					isVisible = false;
				};

				dialog.addEventListener('animationend', closeDialog, { once: true });

				setTimeout(closeDialog, 250);
			}
		}
	});
</script>

{#if isVisible}
	<dialog
		bind:this={dialog}
		class="modal {className}"
		data-test-id={testId}
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
			<button class="btn btn-circle btn-ghost btn-sm absolute right-2 top-2" onclick={onClose}>
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
		background-color: rgba(0, 0, 0, 0.4);
		backdrop-filter: blur(8px);
	}

	.modal-box {
		max-width: min(720px, 95vw);
		width: 100%;
		margin: 0 auto;
		border-radius: 1rem;
		box-shadow:
			0 20px 25px -5px rgb(0 0 0 / 0.1),
			0 8px 10px -6px rgb(0 0 0 / 0.1);
		border: 1px solid rgba(255, 255, 255, 0.1);
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
