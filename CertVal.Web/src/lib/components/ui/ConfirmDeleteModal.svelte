<script lang="ts">
	import Modal from './Modal.svelte';
	import Button from './Button.svelte';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';

	interface Props {
		isOpen?: boolean;
		itemName?: string;
		onConfirm?: () => Promise<void> | void;
		onClose?: () => void;
		'data-test-id'?: string;
	}

	let {
		isOpen = false,
		itemName = '',
		onConfirm,
		onClose,
		'data-test-id': testId
	}: Props = $props();

	let isProcessing = $state(false);

	async function handleConfirm() {
		if (!onConfirm || isProcessing) return;
		try {
			isProcessing = true;
			await onConfirm();
			onClose?.();
		} catch (err) {
			console.error('Confirm delete error', err);
		} finally {
			isProcessing = false;
		}
	}
</script>

<Modal {isOpen} title={t('common.confirmDelete', language.current)} {onClose} data-test-id={testId}>
	<div class="confirm-delete">
		<p class="confirm-delete__message">
			{t('common.confirmDeleteMessage', language.current, { name: itemName })}
		</p>
		<div class="confirm-delete__actions">
			<Button
				variant="secondary"
				onclick={onClose}
				disabled={isProcessing}
				data-test-id={testId ? `${testId}-cancel` : undefined}
			>
				{t('common.cancel', language.current)}
			</Button>
			<Button
				variant="danger"
				onclick={handleConfirm}
				loading={isProcessing}
				data-test-id={testId ? `${testId}-confirm` : undefined}
			>
				{t('common.delete', language.current)}
			</Button>
		</div>
	</div>
</Modal>

<style>
	.confirm-delete {
		display: flex;
		flex-direction: column;
		gap: var(--space-6);
	}

	.confirm-delete__message {
		color: var(--color-text-secondary);
	}

	.confirm-delete__actions {
		display: flex;
		justify-content: flex-end;
		gap: var(--space-3);
	}
</style>
