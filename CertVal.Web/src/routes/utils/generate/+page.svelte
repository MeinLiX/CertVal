<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import UtilsSubnav from '$lib/components/layout/UtilsSubnav.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { downloadText, downloadBlob, buildZip } from '$lib/utils/fileDownload';
	import {
		generateCsr,
		generateSelfSigned,
		type KeyAlgo,
		type SubjectFields,
		type GenerateResult
	} from '$lib/utils/x509';

	const lang = $derived(language.current);

	let mode = $state<'csr' | 'selfsigned'>('csr');
	let algo = $state<KeyAlgo>('RSA-2048');
	let validityDays = $state(365);
	let subject = $state<SubjectFields>({ CN: '', O: '', OU: '', C: '' });
	let sansText = $state('');
	let busy = $state(false);
	let error = $state<string | null>(null);
	let result = $state<GenerateResult | null>(null);

	async function run() {
		error = null;
		result = null;
		busy = true;
		try {
			const sans = sansText
				.split(/[\n,]/)
				.map((s) => s.trim())
				.filter(Boolean);
			result =
				mode === 'csr'
					? await generateCsr(algo, subject, sans)
					: await generateSelfSigned(algo, subject, sans, validityDays);
		} catch (e) {
			error = e instanceof Error ? e.message : t('utils.x509.common.error', lang);
		} finally {
			busy = false;
		}
	}

	function downloadBundle() {
		if (!result) return;
		const enc = new TextEncoder();
		const entries = [
			{ name: 'private-key.pem', data: enc.encode(result.privateKeyPem) },
			{ name: 'public-key.pem', data: enc.encode(result.publicKeyPem) }
		];
		if (result.csrPem) entries.push({ name: 'request.csr', data: enc.encode(result.csrPem) });
		if (result.certPem) entries.push({ name: 'certificate.pem', data: enc.encode(result.certPem) });
		downloadBlob(buildZip(entries), 'certval-keys.zip');
	}
</script>

<svelte:head>
	<title>{t('utils.x509.generate.title', lang)} – CertVal</title>
</svelte:head>

<div class="page">
	<UtilsSubnav description={t('utils.x509.generate.description', lang)} />

	<h1>{t('utils.x509.generate.title', lang)}</h1>
	<p class="privacy">{t('utils.x509.common.privacy', lang)}</p>

	<div class="form">
		<div class="row">
			<label>
				<span>Output</span>
				<select bind:value={mode}>
					<option value="csr">CSR (PKCS#10)</option>
					<option value="selfsigned">Self-signed certificate</option>
				</select>
			</label>
			<label>
				<span>Key algorithm</span>
				<select bind:value={algo}>
					<option value="RSA-2048">RSA 2048</option>
					<option value="RSA-3072">RSA 3072</option>
					<option value="RSA-4096">RSA 4096</option>
					<option value="EC-P256">ECDSA P-256</option>
					<option value="EC-P384">ECDSA P-384</option>
				</select>
			</label>
			{#if mode === 'selfsigned'}
				<label>
					<span>Validity (days)</span>
					<input type="number" min="1" max="3650" bind:value={validityDays} />
				</label>
			{/if}
		</div>

		<div class="row">
			<label>
				<span>Common Name (CN)</span>
				<input bind:value={subject.CN} placeholder="example.com" />
			</label>
			<label>
				<span>Organization (O)</span>
				<input bind:value={subject.O} placeholder="Acme Inc." />
			</label>
		</div>
		<div class="row">
			<label>
				<span>Organizational Unit (OU)</span>
				<input bind:value={subject.OU} placeholder="IT" />
			</label>
			<label>
				<span>Country (C)</span>
				<input bind:value={subject.C} placeholder="UA" maxlength="2" />
			</label>
		</div>
		<label class="full">
			<span>Subject Alternative Names (one per line or comma-separated)</span>
			<textarea bind:value={sansText} rows="3" placeholder="example.com&#10;www.example.com"></textarea>
		</label>

		<div class="actions">
			<Button onclick={run} loading={busy}>{t('utils.x509.generate.cardTitle', lang)}</Button>
		</div>
	</div>

	{#if error}<p class="error">{error}</p>{/if}

	{#if result}
		<section class="result">
			<div class="out">
				<header><h2>Private key</h2>
					<Button variant="secondary" onclick={() => downloadText(result!.privateKeyPem, 'private-key.pem')}>{t('utils.x509.common.download', lang)}</Button>
				</header>
				<textarea readonly rows="6">{result.privateKeyPem}</textarea>
			</div>
			{#if result.csrPem}
				<div class="out">
					<header><h2>CSR</h2>
						<Button variant="secondary" onclick={() => downloadText(result!.csrPem!, 'request.csr')}>{t('utils.x509.common.download', lang)}</Button>
					</header>
					<textarea readonly rows="6">{result.csrPem}</textarea>
				</div>
			{/if}
			{#if result.certPem}
				<div class="out">
					<header><h2>Certificate</h2>
						<Button variant="secondary" onclick={() => downloadText(result!.certPem!, 'certificate.pem')}>{t('utils.x509.common.download', lang)}</Button>
					</header>
					<textarea readonly rows="6">{result.certPem}</textarea>
				</div>
			{/if}
			<div class="out">
				<header><h2>OpenSSL equivalent</h2></header>
				<textarea readonly rows="2" class="mono">{result.openssl}</textarea>
			</div>
			<Button onclick={downloadBundle}>{t('utils.x509.common.download', lang)} .zip</Button>
		</section>
	{/if}
</div>

<style>
	.page { display: flex; flex-direction: column; gap: var(--space-4); }
	h1 { font-family: var(--font-display); margin: 0; }
	.privacy { color: var(--color-text-secondary); font-size: var(--text-sm); margin: 0; }
	.form { display: flex; flex-direction: column; gap: var(--space-3); }
	.row { display: flex; gap: var(--space-3); flex-wrap: wrap; }
	.row label, .full { display: flex; flex-direction: column; gap: var(--space-1); flex: 1; min-width: 180px; font-size: var(--text-sm); font-weight: var(--font-semibold); }
	input, select, textarea {
		width: 100%; box-sizing: border-box; padding: var(--space-2) var(--space-3);
		border: 1px solid var(--color-border); border-radius: var(--radius-md);
		background: var(--color-surface-inset); color: var(--color-text); font: inherit;
	}
	textarea { font-family: var(--font-mono); font-size: 0.78rem; resize: vertical; }
	.actions { display: flex; gap: var(--space-2); }
	.error { color: var(--color-error); font-size: var(--text-sm); }
	.result { display: flex; flex-direction: column; gap: var(--space-3); }
	.out { display: flex; flex-direction: column; gap: var(--space-2); }
	.out header { display: flex; align-items: center; justify-content: space-between; }
	.out h2 { margin: 0; font-size: 1rem; }
	.mono { font-family: var(--font-mono); }
</style>
