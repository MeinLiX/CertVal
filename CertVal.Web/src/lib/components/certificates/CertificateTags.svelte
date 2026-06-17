<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import { api } from '$lib/utils/api';
	import type { Certificate } from '$lib/types';

	interface Props {
		certificate: Certificate;
		onUpdated?: (tags: string[]) => void;
	}

	let { certificate, onUpdated }: Props = $props();

	let tags = $state<string[]>([]);
	let input = $state('');
	let saving = $state(false);
	let error = $state<string | null>(null);
	let lastId = $state('');

	$effect(() => {
		if (certificate.id !== lastId) {
			lastId = certificate.id;
			tags = [...(certificate.tags ?? [])];
		}
	});

	async function persist(next: string[]) {
		saving = true;
		error = null;
		const res = await api.put<string[]>(`/certificates/${certificate.id}/tags`, {
			workspaceId: certificate.workspaceId,
			tags: next
		});
		saving = false;
		if (res.data) {
			tags = res.data;
			onUpdated?.(res.data);
		} else {
			error = res.message ?? 'Failed to update tags';
		}
	}

	function addTag() {
		const value = input.trim();
		input = '';
		if (!value) return;
		if (tags.some((x) => x.toLowerCase() === value.toLowerCase())) return;
		persist([...tags, value]);
	}

	function removeTag(tag: string) {
		persist(tags.filter((x) => x !== tag));
	}
</script>

<section class="card">
	<h2 class="card__title">{t('certificates.tags', language.current)}</h2>
	<div class="tags">
		{#if tags.length === 0}
			<span class="tags__empty">{t('certificates.noTags', language.current)}</span>
		{/if}
		{#each tags as tag (tag)}
			<span class="tag">
				{tag}
				<button type="button" class="tag__remove" aria-label="Remove tag" disabled={saving} onclick={() => removeTag(tag)}>×</button>
			</span>
		{/each}
	</div>
	<form
		class="tags__add"
		onsubmit={(e) => {
			e.preventDefault();
			addTag();
		}}
	>
		<input
			bind:value={input}
			placeholder={t('certificates.tagPlaceholder', language.current)}
			maxlength="40"
			disabled={saving}
		/>
		<button type="submit" disabled={saving || !input.trim()}>+</button>
	</form>
	{#if error}<p class="tags__error">{error}</p>{/if}
</section>

<style>
	.tags {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2);
		margin-bottom: var(--space-3);
	}
	.tags__empty {
		color: var(--color-text-muted);
		font-size: var(--text-sm);
	}
	.tag {
		display: inline-flex;
		align-items: center;
		gap: var(--space-1);
		padding: 0.15rem 0.5rem;
		background: var(--color-primary-light);
		color: var(--color-primary);
		border-radius: var(--radius-full);
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
	}
	.tag__remove {
		border: 0;
		background: transparent;
		color: inherit;
		cursor: pointer;
		font-size: 1rem;
		line-height: 1;
		padding: 0;
	}
	.tag__remove:disabled {
		opacity: 0.5;
		cursor: default;
	}
	.tags__add {
		display: flex;
		gap: var(--space-2);
	}
	.tags__add input {
		flex: 1;
		padding: var(--space-2) var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface-inset);
		color: var(--color-text);
		font: inherit;
	}
	.tags__add button {
		padding: 0 var(--space-4);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface);
		color: var(--color-text);
		cursor: pointer;
		font-size: 1.1rem;
	}
	.tags__add button:disabled {
		opacity: 0.5;
		cursor: default;
	}
	.tags__error {
		color: var(--color-error);
		font-size: var(--text-sm);
		margin: var(--space-2) 0 0;
	}
</style>
