<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import UtilsSubnav from '$lib/components/layout/UtilsSubnav.svelte';
	import FileDropZone from '$lib/components/utils/FileDropZone.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { readFileAsUint8Array } from '$lib/utils/fileDownload';
	import { bytesToHex } from '$lib/utils/x509';

	const lang = $derived(language.current);

	const ALGOS = ['SHA-1', 'SHA-256', 'SHA-384', 'SHA-512'] as const;
	type Algo = (typeof ALGOS)[number];

	let text = $state('');
	let files = $state<File[]>([]);
	let busy = $state(false);
	let error = $state<string | null>(null);
	let results = $state<Array<{ algo: Algo; hex: string }>>([]);
	let copied = $state<string | null>(null);

	async function compute() {
		error = null;
		results = [];
		copied = null;
		let bytes: Uint8Array | null = null;
		if (files.length) bytes = await readFileAsUint8Array(files[0]);
		else if (text.length) bytes = new TextEncoder().encode(text);
		if (!bytes) {
			error = t('utils.hash.noInput', lang);
			return;
		}
		busy = true;
		try {
			const out: Array<{ algo: Algo; hex: string }> = [];
			for (const algo of ALGOS) {
				const digest = await globalThis.crypto.subtle.digest(algo, bytes.slice());
				out.push({ algo, hex: bytesToHex(new Uint8Array(digest), '') });
			}
			results = out;
		} catch (e) {
			error = e instanceof Error ? e.message : t('utils.hash.error', lang);
		} finally {
			busy = false;
		}
	}

	function clearAll() {
		text = '';
		files = [];
		results = [];
		error = null;
		copied = null;
	}

	async function copy(hex: string, algo: string) {
		await navigator.clipboard.writeText(hex);
		copied = algo;
		setTimeout(() => (copied = null), 1500);
	}
</script>

<svelte:head>
	<title>{t('utils.hash.title', lang)} – CertVal</title>
</svelte:head>

<div class="page">
	<UtilsSubnav description={t('utils.hash.description', lang)} />

	<h1>{t('utils.hash.title', lang)}</h1>
	<p class="privacy">{t('utils.hash.privacy', lang)}</p>

	<div class="input-grid">
		<label class="field">
			<span>{t('utils.hash.textLabel', lang)}</span>
			<textarea bind:value={text} rows="6" spellcheck="false"></textarea>
		</label>
		<FileDropZone
			bind:files
			label={t('utils.hash.fileLabel', lang)}
			hint={t('utils.hash.fileHint', lang)}
		/>
	</div>

	<div class="actions">
		<Button onclick={compute} loading={busy}>{t('utils.hash.compute', lang)}</Button>
		<Button variant="secondary" onclick={clearAll}>{t('utils.hash.clear', lang)}</Button>
	</div>

	{#if error}<p class="error">{error}</p>{/if}

	{#if results.length}
		<section class="results">
			{#each results as r (r.algo)}
				<div class="hash-row">
					<span class="hash-row__algo">{r.algo}</span>
					<code class="hash-row__value">{r.hex}</code>
					<button type="button" class="hash-row__copy" onclick={() => copy(r.hex, r.algo)}>
						{copied === r.algo ? t('utils.hash.copied', lang) : t('utils.hash.copy', lang)}
					</button>
				</div>
			{/each}
		</section>
	{/if}
</div>

<style>
	.page { display: flex; flex-direction: column; gap: var(--space-4); }
	h1 { font-family: var(--font-display); margin: 0; }
	.privacy { color: var(--color-text-secondary); font-size: var(--text-sm); margin: 0; }
	.input-grid { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-4); }
	@media (max-width: 720px) { .input-grid { grid-template-columns: 1fr; } }
	.field { display: flex; flex-direction: column; gap: var(--space-2); font-size: var(--text-sm); font-weight: var(--font-semibold); }
	textarea {
		width: 100%; box-sizing: border-box; font-family: var(--font-mono); font-size: 0.8rem;
		padding: var(--space-3); border: 1px solid var(--color-border); border-radius: var(--radius-md);
		background: var(--color-surface-inset); color: var(--color-text); resize: vertical;
	}
	.actions { display: flex; gap: var(--space-2); flex-wrap: wrap; }
	.error { color: var(--color-error); font-size: var(--text-sm); }
	.results { display: flex; flex-direction: column; gap: var(--space-2); }
	.hash-row {
		display: grid; grid-template-columns: max-content 1fr max-content; gap: var(--space-3);
		align-items: center; padding: var(--space-3); border: 1px solid var(--color-border);
		border-radius: var(--radius-md); background: var(--color-surface);
	}
	.hash-row__algo { font-weight: var(--font-semibold); font-size: var(--text-sm); }
	.hash-row__value { font-family: var(--font-mono); font-size: 0.72rem; word-break: break-all; }
	.hash-row__copy {
		border: 1px solid var(--color-border); background: var(--color-surface); color: var(--color-text);
		border-radius: var(--radius-sm); padding: 0.2rem 0.6rem; font-size: var(--text-xs); cursor: pointer; white-space: nowrap;
	}
	@media (max-width: 560px) {
		.hash-row { grid-template-columns: 1fr; }
	}
</style>
