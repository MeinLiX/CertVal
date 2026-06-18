<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import UtilsSubnav from '$lib/components/layout/UtilsSubnav.svelte';
	import FileDropZone from '$lib/components/utils/FileDropZone.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { readFileAsUint8Array } from '$lib/utils/fileDownload';
	import {
		spkiFromCertificate,
		spkiFromCsr,
		spkiFromPrivateKey,
		publicKeysEqual,
		bytesToHex
	} from '$lib/utils/x509';

	const lang = $derived(language.current);

	let keyText = $state('');
	let keyFiles = $state<File[]>([]);
	let certText = $state('');
	let certFiles = $state<File[]>([]);
	let csrText = $state('');
	let csrFiles = $state<File[]>([]);

	let busy = $state(false);
	let error = $state<string | null>(null);

	type Triple = { keyVsCert: boolean | null; keyVsCsr: boolean | null; csrVsCert: boolean | null };
	let result = $state<Triple | null>(null);
	let fingerprint = $state<string | null>(null);
	let notes = $state<string[]>([]);

	async function bytesFrom(files: File[], text: string): Promise<Uint8Array | null> {
		if (files.length) return await readFileAsUint8Array(files[0]);
		if (text.trim()) return new TextEncoder().encode(text);
		return null;
	}

	async function check() {
		error = null;
		result = null;
		fingerprint = null;
		notes = [];
		busy = true;
		try {
			const keyBytes = await bytesFrom(keyFiles, keyText);
			const certBytes = await bytesFrom(certFiles, certText);
			const csrBytes = await bytesFrom(csrFiles, csrText);

			let keySpki: Uint8Array | null = null;
			let certSpki: Uint8Array | null = null;
			let csrSpki: Uint8Array | null = null;

			if (keyBytes) {
				try {
					keySpki = await spkiFromPrivateKey(keyBytes);
				} catch (e) {
					notes.push(`Key: ${e instanceof Error ? e.message : 'parse error'}`);
				}
			}
			if (certBytes) {
				try {
					certSpki = spkiFromCertificate(certBytes);
				} catch (e) {
					notes.push(`Certificate: ${e instanceof Error ? e.message : 'parse error'}`);
				}
			}
			if (csrBytes) {
				try {
					csrSpki = spkiFromCsr(csrBytes);
				} catch (e) {
					notes.push(`CSR: ${e instanceof Error ? e.message : 'parse error'}`);
				}
			}

			const provided = [keySpki, certSpki, csrSpki].filter(Boolean).length;
			if (provided < 2) {
				error = 'Provide at least two of: private key, certificate, CSR.';
				return;
			}

			result = {
				keyVsCert: keySpki && certSpki ? publicKeysEqual(keySpki, certSpki) : null,
				keyVsCsr: keySpki && csrSpki ? publicKeysEqual(keySpki, csrSpki) : null,
				csrVsCert: csrSpki && certSpki ? publicKeysEqual(csrSpki, certSpki) : null
			};

			const ref = keySpki ?? certSpki ?? csrSpki;
			if (ref) {
				const digest = await globalThis.crypto.subtle.digest('SHA-256', ref.slice());
				fingerprint = bytesToHex(new Uint8Array(digest));
			}
		} catch (e) {
			error = e instanceof Error ? e.message : t('utils.x509.common.error', lang);
		} finally {
			busy = false;
		}
	}

	function label(v: boolean | null): { text: string; cls: string } {
		if (v === null) return { text: '—', cls: 'na' };
		return v ? { text: '✓ Match', cls: 'ok' } : { text: '✗ Mismatch', cls: 'err' };
	}
</script>

<svelte:head>
	<title>{t('utils.x509.match.title', lang)} – CertVal</title>
</svelte:head>

<div class="page">
	<UtilsSubnav description={t('utils.x509.match.description', lang)} />

	<h1>{t('utils.x509.match.title', lang)}</h1>
	<p class="privacy">{t('utils.x509.common.privacy', lang)}</p>

	<div class="inputs">
		<div class="col">
			<h2>Private key (PKCS#8)</h2>
			<textarea bind:value={keyText} rows="4" spellcheck="false" placeholder="-----BEGIN PRIVATE KEY-----"></textarea>
			<FileDropZone bind:files={keyFiles} accept=".pem,.key,.txt" label="Drop private key" />
		</div>
		<div class="col">
			<h2>Certificate</h2>
			<textarea bind:value={certText} rows="4" spellcheck="false" placeholder="-----BEGIN CERTIFICATE-----"></textarea>
			<FileDropZone bind:files={certFiles} accept=".pem,.crt,.cer,.der,.txt" label="Drop certificate" />
		</div>
		<div class="col">
			<h2>CSR (optional)</h2>
			<textarea bind:value={csrText} rows="4" spellcheck="false" placeholder="-----BEGIN CERTIFICATE REQUEST-----"></textarea>
			<FileDropZone bind:files={csrFiles} accept=".pem,.csr,.req,.txt" label="Drop CSR" />
		</div>
	</div>

	<div class="actions">
		<Button onclick={check} loading={busy}>{t('utils.x509.match.cardTitle', lang)}</Button>
	</div>

	{#if error}<p class="error">{error}</p>{/if}

	{#if result}
		<section class="results">
			<div class="pair"><span>Key ↔ Certificate</span><span class={label(result.keyVsCert).cls}>{label(result.keyVsCert).text}</span></div>
			<div class="pair"><span>Key ↔ CSR</span><span class={label(result.keyVsCsr).cls}>{label(result.keyVsCsr).text}</span></div>
			<div class="pair"><span>CSR ↔ Certificate</span><span class={label(result.csrVsCert).cls}>{label(result.csrVsCert).text}</span></div>
			{#if fingerprint}
				<div class="fp"><span>Public key SHA-256</span><code>{fingerprint}</code></div>
			{/if}
		</section>
	{/if}

	{#if notes.length}
		<ul class="notes">
			{#each notes as n (n)}<li>{n}</li>{/each}
		</ul>
	{/if}
</div>

<style>
	.page { display: flex; flex-direction: column; gap: var(--space-4); }
	h1 { font-family: var(--font-display); margin: 0; }
	.privacy { color: var(--color-text-secondary); font-size: var(--text-sm); margin: 0; }
	.inputs { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--space-4); }
	@media (max-width: 900px) { .inputs { grid-template-columns: 1fr; } }
	.col { display: flex; flex-direction: column; gap: var(--space-2); }
	.col h2 { margin: 0; font-size: 0.95rem; }
	textarea {
		width: 100%; box-sizing: border-box; font-family: var(--font-mono); font-size: 0.72rem;
		padding: var(--space-2); border: 1px solid var(--color-border); border-radius: var(--radius-md);
		background: var(--color-surface-inset); color: var(--color-text); resize: vertical;
	}
	.actions { display: flex; gap: var(--space-2); }
	.error { color: var(--color-error); font-size: var(--text-sm); }
	.results { display: flex; flex-direction: column; gap: var(--space-2); border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: var(--space-4); background: var(--color-surface); }
	.pair { display: flex; justify-content: space-between; align-items: center; }
	.pair span:first-child { color: var(--color-text-secondary); }
	.ok { color: var(--color-success); font-weight: 700; }
	.err { color: var(--color-error); font-weight: 700; }
	.na { color: var(--color-text-muted); }
	.fp { display: flex; flex-direction: column; gap: var(--space-1); padding-top: var(--space-2); border-top: 1px solid var(--color-border); }
	.fp code { font-family: var(--font-mono); font-size: 0.72rem; word-break: break-all; }
	.notes { color: var(--color-text-secondary); font-size: var(--text-sm); margin: 0; padding-left: var(--space-4); }
</style>
