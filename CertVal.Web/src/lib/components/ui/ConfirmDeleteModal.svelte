<script lang="ts">
	import Modal from './Modal.svelte';
	import Button from './Button.svelte';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	let {
		isOpen = false,
		itemName = '',
		onConfirm,
		onClose,
		'data-test-id': testId
	}: {
		isOpen?: boolean;
		itemName?: string;
		onConfirm?: () => Promise<void> | void;
		onClose?: () => void;
		'data-test-id'?: string;
	} = $props();

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
	<div class="space-y-4">
		<p class="text-base-content/80">
			{t('common.confirmDeleteMessage', language.current, { name: itemName })}
		</p>
		<div class="flex justify-end gap-2">
			<Button
				variant="ghost"
				onclick={onClose}
				disabled={isProcessing}
				data-test-id={testId ? `${testId}-cancel` : undefined}
			>
				{t('common.cancel')}
			</Button>
			<Button
				variant="error"
				onclick={handleConfirm}
				loading={isProcessing}
				data-test-id={testId ? `${testId}-confirm` : undefined}
			>
				{t('common.delete')}
			</Button>
		</div>
	</div>
</Modal>

<style>
	/* small adjustments if needed */
</style>
