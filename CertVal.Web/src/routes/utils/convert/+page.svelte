<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import UtilsSubnav from '$lib/components/layout/UtilsSubnav.svelte';
	import FileDropZone from '$lib/components/utils/FileDropZone.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { readFileAsUint8Array, downloadText, downloadBlob, buildZip } from '$lib/utils/fileDownload';
	import { toCertificateDers, derToPem, describeCertificate } from '$lib/utils/x509';

	const lang = $derived(language.current);

	interface Item {
		index: number;
		subject: string;
		der: Uint8Array;
		pem: string;
	}

	let text = $state('');
	let files = $state<File[]>([]);
	let items = $state<Item[]>([]);
	let error = $state<string | null>(null);
	let busy = $state(false);

	async function convert() {
		error = null;
		items = [];
		let bytes: Uint8Array | null = null;
		if (files.length) bytes = await readFileAsUint8Array(files[0]);
		else if (text.trim()) bytes = new TextEncoder().encode(text);
		if (!bytes) {
			error = t('utils.x509.common.noInput', lang);
			return;
		}
		busy = true;
		try {
			const ders = toCertificateDers(bytes);
			const out: Item[] = [];
			for (let i = 0; i < ders.length; i++) {
				let subject = `Certificate ${i + 1}`;
				try {
					subject = (await describeCertificate(ders[i])).subject || subject;
				} catch {
					/* keep default label */
				}
				out.push({ index: i, subject, der: ders[i], pem: derToPem(ders[i], 'CERTIFICATE') });
			}
			if (out.length === 0) error = t('utils.x509.common.error', lang);
			items = out;
		} catch (e) {
			error = e instanceof Error ? e.message : t('utils.x509.common.error', lang);
		} finally {
			busy = false;
		}
	}

	function clearAll() {
		text = '';
		files = [];
		items = [];
		error = null;
	}

	function downloadDer(it: Item) {
		downloadBlob(new Blob([it.der.slice().buffer], { type: 'application/x-x509-ca-cert' }), `certificate-${it.index + 1}.der`);
	}

	function downloadZip() {
		const enc = new TextEncoder();
		const entries = items.flatMap((it) => [
			{ name: `certificate-${it.index + 1}.pem`, data: enc.encode(it.pem) },
			{ name: `certificate-${it.index + 1}.der`, data: it.der.slice() }
		]);
		downloadBlob(buildZip(entries), 'certificates.zip');
	}
</script>

<svelte:head>
	<title>{t('utils.x509.convert.title', lang)} – CertVal</title>
</svelte:head>

<div class="page">
	<UtilsSubnav description={t('utils.x509.convert.description', lang)} />

	<h1>{t('utils.x509.convert.title', lang)}</h1>
	<p class="privacy">{t('utils.x509.common.privacy', lang)}</p>

	<div class="input-grid">
		<label class="field">
			<span>{t('utils.x509.common.pasteLabel', lang)}</span>
			<textarea bind:value={text} rows="7" spellcheck="false" placeholder="-----BEGIN CERTIFICATE-----"></textarea>
		</label>
		<FileDropZone
			bind:files
			accept=".pem,.crt,.cer,.der,.p7b,.txt"
			label={t('utils.x509.common.pasteLabel', lang)}
			hint={t('utils.x509.common.pasteHint', lang)}
		/>
	</div>

	<div class="actions">
		<Button onclick={convert} loading={busy}>{t('utils.x509.common.parse', lang)}</Button>
		<Button variant="secondary" onclick={clearAll}>{t('utils.x509.common.clear', lang)}</Button>
		{#if items.length > 1}
			<Button variant="secondary" onclick={downloadZip}>{t('utils.x509.common.download', lang)} .zip</Button>
		{/if}
	</div>

	{#if error}<p class="error">{error}</p>{/if}

	{#each items as it (it.index)}
		<section class="item">
			<header>
				<h2>#{it.index + 1}</h2>
				<span class="subject mono">{it.subject}</span>
			</header>
			<textarea readonly rows="5" class="mono">{it.pem}</textarea>
			<div class="actions">
				<Button variant="secondary" onclick={() => downloadText(it.pem, `certificate-${it.index + 1}.pem`)}>PEM</Button>
				<Button variant="secondary" onclick={() => downloadDer(it)}>DER</Button>
			</div>
		</section>
	{/each}
</div>

<style>
	.page { display: flex; flex-direction: column; gap: var(--space-4); }
	h1 { font-family: var(--font-display); margin: 0; }
	.privacy { color: var(--color-text-secondary); font-size: var(--text-sm); margin: 0; }
	.input-grid { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-4); }
	@media (max-width: 720px) { .input-grid { grid-template-columns: 1fr; } }
	.field { display: flex; flex-direction: column; gap: var(--space-2); font-size: var(--text-sm); font-weight: var(--font-semibold); }
	textarea {
		width: 100%; box-sizing: border-box; font-family: var(--font-mono); font-size: 0.78rem;
		padding: var(--space-3); border: 1px solid var(--color-border); border-radius: var(--radius-md);
		background: var(--color-surface-inset); color: var(--color-text); resize: vertical;
	}
	.actions { display: flex; gap: var(--space-2); flex-wrap: wrap; }
	.error { color: var(--color-error); font-size: var(--text-sm); }
	.item { border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: var(--space-4); display: flex; flex-direction: column; gap: var(--space-2); background: var(--color-surface); }
	.item header { display: flex; align-items: baseline; gap: var(--space-3); }
	.item h2 { margin: 0; font-size: 1rem; }
	.subject { color: var(--color-text-secondary); font-size: 0.78rem; word-break: break-all; }
	.mono { font-family: var(--font-mono); }
</style>
