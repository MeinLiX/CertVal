<script lang="ts">
	import Modal from './Modal.svelte';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	let {
		isOpen = false,
		itemName = '',
		onConfirm,
		onClose
	}: {
		isOpen?: boolean;
		itemName?: string;
		onConfirm?: () => Promise<void> | void;
		onClose?: () => void;
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

<Modal {isOpen} title={t('common.confirmDelete', language.current)} {onClose}>
	<div class="space-y-4">
		<p class="text-base-content/80">
			{t('common.confirmDeleteMessage', language.current, { name: itemName })}
		</p>
		<div class="flex justify-end gap-2">
			<button class="btn btn-ghost" onclick={onClose} disabled={isProcessing}
				>{t('common.cancel')}</button
			>
			<button class="btn btn-error" onclick={handleConfirm} disabled={isProcessing}>
				{#if isProcessing}
					<span class="loading loading-sm loading-spinner mr-2"></span>
				{/if}
				{t('common.delete')}
			</button>
		</div>
	</div>
</Modal>

<style>
	/* small adjustments if needed */
</style>
