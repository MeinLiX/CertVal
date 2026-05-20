<script lang="ts">
	import { formatBytes } from '$lib/utils/fileDownload';

	interface Props {
		files?: File[];
		accept?: string;
		multiple?: boolean;
		label: string;
		hint?: string;
		disabled?: boolean;
		onchange?: (files: File[]) => void;
	}

	let {
		files = $bindable<File[]>([]),
		accept,
		multiple = false,
		label,
		hint,
		disabled = false,
		onchange
	}: Props = $props();

	let dragOver = $state(false);
	let inputEl: HTMLInputElement;

	function emit(next: File[]) {
		files = next;
		onchange?.(next);
	}

	function add(list: FileList | File[]) {
		const incoming = Array.from(list);
		if (incoming.length === 0) return;
		if (multiple) {
			const merged = [...files];
			for (const f of incoming) {
				if (!merged.some((m) => m.name === f.name && m.size === f.size)) merged.push(f);
			}
			emit(merged);
		} else {
			emit([incoming[0]]);
		}
	}

	function onDrop(e: DragEvent) {
		e.preventDefault();
		dragOver = false;
		if (disabled || !e.dataTransfer) return;
		add(e.dataTransfer.files);
	}

	function onDragOver(e: DragEvent) {
		e.preventDefault();
		if (!disabled) dragOver = true;
	}

	function onDragLeave() {
		dragOver = false;
	}

	function onInput(e: Event) {
		const el = e.currentTarget as HTMLInputElement;
		if (el.files) add(el.files);
		el.value = '';
	}

	function remove(index: number) {
		emit(files.filter((_, i) => i !== index));
	}

	function clearAll() {
		emit([]);
	}
</script>

<div
	class="drop-zone"
	class:drag-over={dragOver}
	class:disabled
	role="button"
	tabindex={disabled ? -1 : 0}
	aria-disabled={disabled}
	onclick={() => !disabled && inputEl?.click()}
	onkeydown={(e) => {
		if (!disabled && (e.key === 'Enter' || e.key === ' ')) {
			e.preventDefault();
			inputEl?.click();
		}
	}}
	ondrop={onDrop}
	ondragover={onDragOver}
	ondragleave={onDragLeave}
>
	<div class="primary">{label}</div>
	{#if hint}<div class="hint">{hint}</div>{/if}
	<input
		type="file"
		bind:this={inputEl}
		{accept}
		{multiple}
		{disabled}
		hidden
		onchange={onInput}
	/>
</div>

{#if files.length}
	<ul class="file-list">
		{#each files as f, i (i)}
			<li>
				<span class="name" title={f.name}>{f.name}</span>
				<span class="size">{formatBytes(f.size)}</span>
				<button type="button" class="remove" onclick={() => remove(i)} aria-label="Remove">×</button>
			</li>
		{/each}
		{#if multiple && files.length > 1}
			<li class="clear-row">
				<button type="button" class="clear" onclick={clearAll}>Clear all</button>
			</li>
		{/if}
	</ul>
{/if}

<style>
	.drop-zone {
		border: 2px dashed var(--color-border, #c7cfd9);
		border-radius: 12px;
		padding: 1.5rem 1rem;
		text-align: center;
		background: var(--color-surface-alt, #f8fafc);
		cursor: pointer;
		transition: background 0.15s, border-color 0.15s;
		outline: none;
	}
	.drop-zone:focus-visible {
		border-color: var(--color-primary, #1a4480);
		box-shadow: 0 0 0 3px rgba(26, 68, 128, 0.15);
	}
	.drop-zone.drag-over {
		border-color: var(--color-primary, #1a4480);
		background: rgba(26, 68, 128, 0.05);
	}
	.drop-zone.disabled {
		opacity: 0.55;
		cursor: not-allowed;
	}
	.primary {
		font-weight: 500;
	}
	.hint {
		margin-top: 0.35rem;
		font-size: 0.85rem;
		color: var(--color-text-muted, #6a7280);
	}
	.file-list {
		list-style: none;
		margin: 0.75rem 0 0;
		padding: 0;
		display: flex;
		flex-direction: column;
		gap: 0.35rem;
	}
	.file-list li {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		padding: 0.5rem 0.75rem;
		background: var(--color-surface, #fff);
		border: 1px solid var(--color-border, #d8dde6);
		border-radius: 8px;
		font-size: 0.9rem;
	}
	.name {
		flex: 1;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.size {
		color: var(--color-text-muted, #6a7280);
		font-variant-numeric: tabular-nums;
	}
	.remove {
		border: 0;
		background: transparent;
		color: var(--color-text-muted, #6a7280);
		font-size: 1.2rem;
		cursor: pointer;
		line-height: 1;
		padding: 0 0.25rem;
	}
	.remove:hover {
		color: var(--color-danger, #b91c1c);
	}
	.clear-row {
		justify-content: flex-end;
		background: transparent;
		border: 0;
		padding: 0;
	}
	.clear {
		background: transparent;
		border: 0;
		color: var(--color-text-muted, #6a7280);
		font-size: 0.85rem;
		cursor: pointer;
		text-decoration: underline;
	}
</style>
