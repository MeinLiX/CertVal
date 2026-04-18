<script lang="ts">
	import { tick } from 'svelte';
	import type { Snippet } from 'svelte';
	import Button from './Button.svelte';
	import Icon from './Icon.svelte';

	interface Props {
		isOpen?: boolean;
		title?: string;
		size?: 'sm' | 'md' | 'lg';
		onClose?: () => void;
		onclose?: () => void;
		children?: Snippet;
		footer?: Snippet;
		class?: string;
		allowClickOutsideToClose?: boolean;
		'data-test-id'?: string;
	}

	let {
		isOpen = $bindable(false),
		title = '',
		size = 'md',
		onClose,
		onclose,
		children,
		footer,
		class: className = '',
		allowClickOutsideToClose = false,
		'data-test-id': testId
	}: Props = $props();

	// Support both onClose and onclose props
	const handleClose = $derived(onClose ?? onclose);

	let dialog: HTMLDialogElement | undefined = $state();
	let isVisible = $state(false);

	$effect(() => {
		if (isOpen) {
			isVisible = true;
			tick().then(() => {
				dialog?.showModal();
			});
		} else if (dialog) {
			dialog.classList.add('modal--closing');
			const timer = setTimeout(() => {
				dialog?.classList.remove('modal--closing');
				dialog?.close();
				isVisible = false;
			}, 200);
			return () => clearTimeout(timer);
		}
	});

	function handleBackdropClick(e: MouseEvent) {
		if (allowClickOutsideToClose && e.target === dialog) {
			handleClose?.();
		}
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			e.preventDefault();
			handleClose?.();
		}
	}
</script>

{#if isVisible}
	<dialog
		bind:this={dialog}
		class="modal modal--{size} {className}"
		onclick={handleBackdropClick}
		onkeydown={handleKeydown}
		oncancel={(e) => { e.preventDefault(); handleClose?.(); }}
		data-test-id={testId}
	>
		<div class="modal__container">
			{#if title}
				<header class="modal__header">
					<h3 class="modal__title">{title}</h3>
					<Button variant="ghost" icon onclick={() => handleClose?.()} data-test-id="modal-close-btn">
						<Icon name="close" />
					</Button>
				</header>
			{/if}

			<div class="modal__body">
				{@render children?.()}
			</div>

			{#if footer}
				<footer class="modal__footer">
					{@render footer?.()}
				</footer>
			{/if}
		</div>
	</dialog>
{/if}

<style>
	.modal {
		position: fixed;
		inset: 0;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: var(--space-4);
		margin: 0;
		max-width: 100%;
		max-height: 100%;
		width: 100%;
		height: 100%;
		border: none;
		background: transparent;
		z-index: var(--z-modal);
	}

	.modal::backdrop {
		background-color: rgba(0, 0, 0, 0.5);
		animation: fadeIn var(--transition-fast) ease;
	}

	.modal__container {
		background-color: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xl);
		box-shadow: var(--shadow-pop);
		width: 100%;
		max-height: 90vh;
		overflow: hidden;
		display: flex;
		flex-direction: column;
		animation: slideUp var(--transition-base) ease;
	}

	.modal--sm .modal__container { max-width: 400px; }
	.modal--md .modal__container { max-width: 560px; }
	.modal--lg .modal__container { max-width: 720px; }

	.modal--closing .modal__container {
		animation: slideDown var(--transition-fast) ease forwards;
	}

	.modal--closing::backdrop {
		animation: fadeOut var(--transition-fast) ease forwards;
	}

	.modal__header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: var(--space-5) var(--space-6);
		border-bottom: 1px solid var(--color-border);
	}

	.modal__title {
		font-size: var(--text-lg);
		font-weight: var(--font-semibold);
		margin: 0;
	}

	.modal__body {
		padding: var(--space-6);
		overflow-y: auto;
		flex: 1;
	}

	.modal__footer {
		display: flex;
		justify-content: flex-end;
		gap: var(--space-3);
		padding: var(--space-4) var(--space-6);
		border-top: 1px solid var(--color-border);
	}

	@keyframes fadeIn {
		from { opacity: 0; }
		to { opacity: 1; }
	}

	@keyframes fadeOut {
		from { opacity: 1; }
		to { opacity: 0; }
	}

	@keyframes slideUp {
		from { opacity: 0; transform: translateY(16px); }
		to { opacity: 1; transform: translateY(0); }
	}

	@keyframes slideDown {
		from { opacity: 1; transform: translateY(0); }
		to { opacity: 0; transform: translateY(16px); }
	}
</style>
