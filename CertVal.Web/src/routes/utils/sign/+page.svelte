<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import UtilsSubnav from '$lib/components/layout/UtilsSubnav.svelte';
	import InitToast from '$lib/components/ui/InitToast.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import FileDropZone from '$lib/components/utils/FileDropZone.svelte';
	import {
		SignClient,
		EU_SIGN_TYPE_PARAMETER,
		EU_CTX_SIGN_UNKNOWN,
		EU_CTX_HASH_ALGO_UNKNOWN,
		EU_CTX_HASH_ALGO_GOST34311,
		EU_CTX_HASH_ALGO_SHA256,
		EU_CTX_HASH_ALGO_DSTU7564_256,
		EU_CTX_HASH_ALGO_DSTU7564_384,
		EU_CTX_HASH_ALGO_DSTU7564_512,
		EU_SIGN_TYPE_CADES_BES,
		EU_SIGN_TYPE_CADES_T,
		EU_SIGN_TYPE_CADES_X_LONG,
		EU_ASIC_TYPE_E,
		EU_ASIC_TYPE_S,
		EU_ASIC_SIGN_TYPE_CADES,
		EU_ASIC_SIGN_TYPE_XADES,
		EU_ASIC_SIGN_LEVEL_BES,
		EU_ASIC_SIGN_LEVEL_T,
		EU_XADES_TYPE_DETACHED,
		EU_XADES_TYPE_ENVELOPING,
		EU_XADES_TYPE_ENVELOPED,
		EU_XADES_SIGN_LEVEL_B_B,
		EU_XADES_SIGN_LEVEL_B_T,
		EU_XADES_SIGN_LEVEL_B_LT,
		EU_CHECK_CERT_CHAIN_ON_SIGN_TIME_PARAMETER,
		type JKSPrivateKey
	} from '$lib/iit/signClient';
	import { buildZip, downloadBlob, formatBytes, readFileAsUint8Array } from '$lib/utils/fileDownload';


	type SignMethod = 'cades' | 'xades' | 'pades' | 'asic-e' | 'asic-s';

	interface SignFormat {
		id: string;
		label: string;
		method: SignMethod;
		level: number;
		ext: string;
		needsTSP?: boolean;
		needsOCSP?: boolean;
		pdfOnly?: boolean;
	}

	const FORMATS: SignFormat[] = [
		{ id: 'cades-bes', label: 'CAdES-BES',      method: 'cades',  level: EU_SIGN_TYPE_CADES_BES,    ext: 'p7s' },
		{ id: 'cades-t',   label: 'CAdES-T',         method: 'cades',  level: EU_SIGN_TYPE_CADES_T,      ext: 't.p7s',      needsTSP: true },
		{ id: 'cades-xl',  label: 'CAdES-X Long',    method: 'cades',  level: EU_SIGN_TYPE_CADES_X_LONG, ext: 'xl.p7s',     needsTSP: true, needsOCSP: true },
		{ id: 'xades-b',   label: 'XAdES-B',         method: 'xades',  level: EU_XADES_SIGN_LEVEL_B_B,   ext: 'xades.xml' },
		{ id: 'xades-t',   label: 'XAdES-T',         method: 'xades',  level: EU_XADES_SIGN_LEVEL_B_T,   ext: 'xades-t.xml', needsTSP: true },
		{ id: 'pades-b',   label: 'PAdES-B',         method: 'pades',  level: EU_SIGN_TYPE_CADES_BES,    ext: 'pdf',        pdfOnly: true },
		{ id: 'pades-t',   label: 'PAdES-T',         method: 'pades',  level: EU_SIGN_TYPE_CADES_T,      ext: 'pdf',        pdfOnly: true, needsTSP: true },
		{ id: 'asic-e',    label: 'ASiC-E (CAdES)',  method: 'asic-e', level: EU_ASIC_SIGN_LEVEL_BES,    ext: 'asice' },
		{ id: 'asic-s',    label: 'ASiC-S (CAdES)',  method: 'asic-s', level: EU_ASIC_SIGN_LEVEL_BES,    ext: 'asics' }
	];

	const FORMAT_GROUPS: [string, string[]][] = [
		['CAdES', ['cades-bes', 'cades-t', 'cades-xl']],
		['XAdES', ['xades-b', 'xades-t']],
		['PAdES',  ['pades-b', 'pades-t']],
		['ASiC',   ['asic-e', 'asic-s']]
	];

	let keyFiles = $state<File[]>([]);
	let password = $state('');
	let initializing = $state(false);
	let initialized = $state(false);
	let initError = $state<string | null>(null);
	let keyLoading = $state(false);
	let keyLoaded = $state(false);
	let keyError = $state<string | null>(null);
	let jksKeys = $state<JKSPrivateKey[]>([]);
	let selectedJksIndex = $state(0);
	// Signing algorithm + expected hash size resolved from the loaded key certificate.
	// EU_CTX_SIGN_DSTU4145_WITH_GOST34311=1, RSA_WITH_SHA=2, ECDSA_WITH_SHA=3, DSTU4145_WITH_DSTU7564=4
	let keySignAlgo = $state(0);
	let keyHashSize = $state(0); // expected hash input length in bytes for SignHash

	function resolveSignAlgo(cert: unknown): number {
		const c = cert as Record<string, number>;
		const pubKeyType = c.publicKeyType ?? 0;
		const hashType = c.certHashType ?? 0;
		if (pubKeyType === 0x01) return hashType === 0x01 ? 1 : 4;
		if (pubKeyType === 0x02) return 2;
		if (pubKeyType === 0x04) return 3;
		return 1;
	}

	function resolveHashSize(cert: unknown): number {
		const c = cert as Record<string, number>;
		const pubKeyType = c.publicKeyType ?? 0;
		const hashType = c.certHashType ?? 0;
		if (pubKeyType === 0x01) {
			if (hashType === 0x01) return 32; // GOST 34-311 ? 256 bit
			if (hashType === 0x07) return 32; // DSTU 7564-256
			if (hashType === 0x08) return 48; // DSTU 7564-384
			if (hashType === 0x09) return 64; // DSTU 7564-512
		}
		if (pubKeyType === 0x02) return 32; // RSA + SHA-256
		if (pubKeyType === 0x04) return 32; // ECDSA + SHA-256
		return 32;
	}

	function getDataHashAlgo(): number {
		if (keySignAlgo === 1) return EU_CTX_HASH_ALGO_GOST34311;
		if (keySignAlgo === 4) {
			if (keyHashSize === 64) return EU_CTX_HASH_ALGO_DSTU7564_512;
			if (keyHashSize === 48) return EU_CTX_HASH_ALGO_DSTU7564_384;
			return EU_CTX_HASH_ALGO_DSTU7564_256;
		}
		return EU_CTX_HASH_ALGO_SHA256; // RSA (2), ECDSA (3)
	}

	function signAlgoLabel(algo: number): string {
		const lang = language.current;
		const map: Record<number, string> = {
			1: t('utils.sign.algo.dstu4145_gost', lang),
			2: t('utils.sign.algo.rsa_sha', lang),
			3: t('utils.sign.algo.ecdsa_sha', lang),
			4: t('utils.sign.algo.dstu4145_dstu7564', lang),
		};
		return map[algo] ?? t('utils.sign.algo.unknown', lang);
	}

	let mode = $state<'advanced' | 'multi' | 'hash'>('advanced');

	const client = new SignClient();

	onMount(async () => {
		initializing = true;
		initError = null;
		try {
			await client.initialize({
				language: language.current === 'uk' ? 'ua' : 'en',
				encoding: 'UTF-8',
				httpProxyServiceURL: '',
				directAccess: true,
				CAs: '/iit/data/CAs.json',
				CACertificates: '/iit/data/CACertificates.p7b'
			});
			await client.setRuntimeParameter(EU_CHECK_CERT_CHAIN_ON_SIGN_TIME_PARAMETER, false);
			initialized = true;
		} catch (e) {
			initError = e instanceof Error ? e.message : String(e);
		} finally {
			initializing = false;
		}
	});

	onDestroy(() => client.terminate());

	async function unlockKey() {
		if (!initialized || keyFiles.length === 0 || keyLoading) return;
		keyLoading = true;
		keyError = null;
		jksKeys = [];
		try {
			const file = keyFiles[0];
			const bytes = await readFileAsUint8Array(file);
			const isJks = file.name.toLowerCase().endsWith('.jks');
			if (isJks) {
				const keys = await client.getJKSPrivateKeys(bytes);
				if (keys.length === 0) throw new Error('No private keys found in JKS container');
				jksKeys = keys;
				const key = keys[selectedJksIndex] ?? keys[0];
				const certBytes = key.certificates.map((c) => (c as { data: Uint8Array }).data);
				await client.readPrivateKeyBinary(key.privateKey, password, certBytes);
			} else {
				await client.readPrivateKeyBinary(bytes, password);
			}
			keyLoaded = true;
			try {
				const certs = await client.getOwnCertificates();
				if (certs.length > 0) {
					keySignAlgo = resolveSignAlgo(certs[0]);
					keyHashSize = resolveHashSize(certs[0]);
				}
			} catch { /* non-critical */ }
		} catch (e) {
			keyError = e instanceof Error ? e.message : String(e);
			keyLoaded = false;
		} finally {
			keyLoading = false;
		}
	}

	async function unloadKey() {
		try { await client.resetPrivateKey(); } catch { /* ignore */ }
		keyLoaded = false;
		keySignAlgo = 0;
		keyHashSize = 0;
		password = '';
	}

	let multiDocs = $state<File[]>([]);
	let multiSelectedFormats = $state<Record<string, boolean>>(
		Object.fromEntries(FORMATS.map((f, i) => [f.id, i === 0]))
	);
	let multiDetached = $state(true);
	let multiSigning = $state(false);

	type RunResult = {
		docName: string;
		formatLabel: string;
		ok: boolean;
		fileName?: string;
		data?: Uint8Array;
		error?: string;
	};
	let multiResults = $state<RunResult[]>([]);

	function selectedFormatList(): SignFormat[] {
		return FORMATS.filter((f) => multiSelectedFormats[f.id]);
	}

	async function signDoc(doc: File, data: Uint8Array, fmt: SignFormat): Promise<RunResult> {
		const isPdf = doc.name.toLowerCase().endsWith('.pdf');
		if (fmt.pdfOnly && !isPdf) {
			return { docName: doc.name, formatLabel: fmt.label, ok: false, error: `${fmt.label} requires a PDF file` };
		}
		try {
			if (fmt.method === 'cades') {
				await client.setRuntimeParameter(EU_SIGN_TYPE_PARAMETER, fmt.level);
				const signed = (await client.signDataEx(EU_CTX_SIGN_UNKNOWN, data, multiDetached, true, false)) as Uint8Array | string;
				const bytes = signed instanceof Uint8Array ? signed : new TextEncoder().encode(signed as string);
				return { docName: doc.name, formatLabel: fmt.label, ok: true, fileName: `${doc.name}.${fmt.ext}`, data: bytes };
			} else if (fmt.method === 'xades') {
			const res = await client.xadesSignData(keySignAlgo, EU_XADES_TYPE_DETACHED, fmt.level, doc.name, data, false);
			return { docName: doc.name, formatLabel: fmt.label, ok: true, fileName: res.name, data: res.val };
		} else if (fmt.method === 'pades') {
			const res = await client.pdfSignData(keySignAlgo, doc.name, data, fmt.level, false);
			return { docName: doc.name, formatLabel: fmt.label, ok: true, fileName: res.name, data: res.val };
		} else {
			const asicType = fmt.method === 'asic-e' ? EU_ASIC_TYPE_E : EU_ASIC_TYPE_S;
			const res = await client.asicSignData(keySignAlgo, asicType, EU_ASIC_SIGN_TYPE_CADES, fmt.level, doc.name, data, false);
			return { docName: doc.name, formatLabel: fmt.label, ok: true, fileName: res.name, data: res.val };
		}
		} catch (e) {
			return { docName: doc.name, formatLabel: fmt.label, ok: false, error: e instanceof Error ? e.message : String(e) };
		}
	}

	async function runMulti() {
		if (!keyLoaded || multiSigning) return;
		multiResults = [];
		const formats = selectedFormatList();
		if (formats.length === 0) {
			multiResults = [{ docName: '', formatLabel: '', ok: false, error: t('utils.sign.errors.noFormat', language.current) }];
			return;
		}
		if (multiDocs.length === 0) {
			multiResults = [{ docName: '', formatLabel: '', ok: false, error: t('utils.sign.errors.noDoc', language.current) }];
			return;
		}
		multiSigning = true;
		try {
			const out: RunResult[] = [];
			for (const doc of multiDocs) {
				const data = await readFileAsUint8Array(doc);
				for (const fmt of formats) {
					out.push(await signDoc(doc, data, fmt));
				}
			}
			multiResults = out;
		} finally {
			multiSigning = false;
		}
	}

	function downloadOne(r: RunResult) {
		if (!r.data || !r.fileName) return;
		downloadBlob(new Blob([new Uint8Array(r.data)], { type: 'application/octet-stream' }), r.fileName);
	}

	function downloadAll() {
		const items = multiResults.filter((r) => r.ok && r.data && r.fileName);
		const zip = buildZip(items.map((r) => ({ name: r.fileName!, data: r.data! })));
		downloadBlob(zip, 'signatures.zip');
	}

	const multiSuccessCount = $derived(multiResults.filter((r) => r.ok).length);

	let advDocs = $state<File[]>([]);
	let advFamily = $state<'cades' | 'xades' | 'pades' | 'asic'>('cades');
	let advCadesLevel = $state(EU_SIGN_TYPE_CADES_BES);
	let advDetached = $state(true);
	let advAppendCert = $state(true);
	let advXadesType = $state(EU_XADES_TYPE_DETACHED);
	let advXadesLevel = $state(EU_XADES_SIGN_LEVEL_B_B);
	let advAsicContainer = $state<'e' | 's'>('e');
	let advAsicSignType = $state(EU_ASIC_SIGN_TYPE_CADES);
	let advAsicLevel = $state(EU_ASIC_SIGN_LEVEL_BES);
	// PAdES options
	let advPadesLevel = $state(EU_SIGN_TYPE_CADES_BES);

	let advSigning = $state(false);
	let advResult = $state<{ ok: boolean; fileName?: string; data?: Uint8Array; error?: string } | null>(null);

	async function runAdvanced() {
		if (!keyLoaded || advSigning || advDocs.length === 0) return;
		advResult = null;
		advSigning = true;
		const doc = advDocs[0];
		const isPdf = doc.name.toLowerCase().endsWith('.pdf');
		try {
			const data = await readFileAsUint8Array(doc);
			if (advFamily === 'cades') {
				await client.setRuntimeParameter(EU_SIGN_TYPE_PARAMETER, advCadesLevel);
				const extMap: Record<number, string> = {
					[EU_SIGN_TYPE_CADES_BES]: 'p7s',
					[EU_SIGN_TYPE_CADES_T]: 't.p7s',
					[EU_SIGN_TYPE_CADES_X_LONG]: 'xl.p7s'
				};
				const signed = (await client.signDataEx(EU_CTX_SIGN_UNKNOWN, data, advDetached, advAppendCert, false)) as Uint8Array;
				advResult = { ok: true, fileName: `${doc.name}.${extMap[advCadesLevel] ?? 'p7s'}`, data: signed instanceof Uint8Array ? signed : new TextEncoder().encode(signed) };
			} else if (advFamily === 'xades') {
				const res = await client.xadesSignData(keySignAlgo, advXadesType, advXadesLevel, doc.name, data, false);
				advResult = { ok: true, fileName: res.name, data: res.val };
			} else if (advFamily === 'pades') {
				if (!isPdf) throw new Error(t('utils.sign.format.padesRequiresPdf', language.current));
				const res = await client.pdfSignData(keySignAlgo, doc.name, data, advPadesLevel, false);
				advResult = { ok: true, fileName: res.name, data: res.val };
			} else {
				const asicType = advAsicContainer === 'e' ? EU_ASIC_TYPE_E : EU_ASIC_TYPE_S;
				const ext = advAsicContainer === 'e' ? 'asice' : 'asics';
				const res = await client.asicSignData(keySignAlgo, asicType, advAsicSignType, advAsicLevel, doc.name, data, false);
				advResult = { ok: true, fileName: res.name ?? `${doc.name}.${ext}`, data: res.val };
			}
		} catch (e) {
			advResult = { ok: false, error: e instanceof Error ? e.message : String(e) };
		} finally {
			advSigning = false;
		}
	}

	// ---------------------------------------------------------------------------
	// Mode 2.3  Hash signing (SignHash only)
	// Input: pre-computed hash bytes (hex or base64).
	// Output: raw signature bytes  no CAdES wrapper.
	// Note: SignHash does NOT auto-resolve signAlgo=0; keySignAlgo is used directly.
	// ---------------------------------------------------------------------------

	let hashInput = $state('');
	let hashEncoding = $state<'hex' | 'base64'>('hex');
	let hashAppendCert = $state(false);
	let hashOutputBase64 = $state(false);
	let hashSigning = $state(false);
	let hashResult = $state<{ ok: boolean; data?: Uint8Array | string; fileName?: string; error?: string } | null>(null);

	// Derived: approximate byte count of the current hash input (for length hint).
	const hashByteCount = $derived((() => {
		const trim = hashInput.trim();
		if (!trim) return 0;
		try {
			if (hashEncoding === 'hex') return Math.floor(trim.replace(/\s/g, '').length / 2);
			return Math.floor(trim.replace(/[\s=]+/g, '').length * 3 / 4);
		} catch { return 0; }
	})());

	function hexToBytes(hex: string): Uint8Array {
		const clean = hex.replace(/\s/g, '');
		if (clean.length % 2 !== 0) throw new Error(t('utils.sign.errors.hexOdd', language.current));
		const bytes = new Uint8Array(clean.length / 2);
		for (let i = 0; i < bytes.length; i++) {
			const b = parseInt(clean.slice(i * 2, i * 2 + 2), 16);
			if (isNaN(b)) throw new Error(t('utils.sign.errors.hexInvalid', language.current));
			bytes[i] = b;
		}
		return bytes;
	}

	function base64ToBytes(b64: string): Uint8Array {
		const bin = atob(b64.trim());
		const bytes = new Uint8Array(bin.length);
		for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
		return bytes;
	}

	async function runHash() {
		if (!keyLoaded || hashSigning || !hashInput.trim()) return;
		hashResult = null;
		hashSigning = true;
		try {
			const hashBytes: Uint8Array = hashEncoding === 'hex'
				? hexToBytes(hashInput)
				: base64ToBytes(hashInput);
			// SignHash: signs the pre-computed hash directly.
			// signAlgo must be explicit (SignHash does not auto-resolve 0).
			const signed = await client.signHash(keySignAlgo, hashBytes, hashAppendCert, hashOutputBase64);
			const timestamp = Date.now();
			if (hashOutputBase64) {
				hashResult = { ok: true, data: signed as string, fileName: `hash_signature_${timestamp}.sig.b64` };
			} else {
				hashResult = { ok: true, data: signed as Uint8Array, fileName: `hash_signature_${timestamp}.sig` };
			}
		} catch (e) {
			hashResult = { ok: false, error: e instanceof Error ? e.message : String(e) };
		} finally {
			hashSigning = false;
		}
	}

	function downloadHashResult() {
		if (!hashResult?.data || !hashResult.fileName) return;
		const data = hashResult.data;
		if (typeof data === 'string') {
			downloadBlob(new Blob([data], { type: 'text/plain' }), hashResult.fileName);
		} else {
			downloadBlob(new Blob([data.buffer as ArrayBuffer], { type: 'application/octet-stream' }), hashResult.fileName);
		}
	}

	function bytesToHex(bytes: Uint8Array): string {
		return Array.from(bytes).map(b => b.toString(16).padStart(2, '0')).join('');
	}

	// ---------------------------------------------------------------------------
	// Mode 2.3 sub-mode: sign arbitrary text or Base64-encoded binary data
	// Uses signDataEx (CAdES-BES), returns a .p7s container.
	// ---------------------------------------------------------------------------
	let hashSubMode = $state<'hash' | 'data'>('data');
	let dataInput = $state('');
	let dataEncoding = $state<'text' | 'base64'>('text');
	let dataOutputFmt = $state<'cades' | 'xades' | 'asice' | 'raw'>('cades');
	let dataEmbedData = $state(true); // CAdES only: true = internal (data embedded), false = detached
	let dataSigning = $state(false);
	let dataResult = $state<{ ok: boolean; data?: Uint8Array; fileName?: string; error?: string } | null>(null);

	function dataFileName(ext: string): string {
		const base = dataEncoding === 'text' ? 'data.txt' : 'data.bin';
		return `${base}_signature_${Date.now()}.${ext}`;
	}

	async function runData() {
		if (!keyLoaded || dataSigning || !dataInput.trim()) return;
		dataResult = null;
		dataSigning = true;
		try {
			const bytes: Uint8Array = dataEncoding === 'text'
				? new TextEncoder().encode(dataInput)
				: base64ToBytes(dataInput);
			const syntheticName = dataEncoding === 'text' ? 'data.txt' : 'data.bin';
			let signed: Uint8Array;
			let ext: string;
			if (dataOutputFmt === 'cades') {
				await client.setRuntimeParameter(EU_SIGN_TYPE_PARAMETER, EU_SIGN_TYPE_CADES_BES);
				// external=false → internal (data embedded); external=true → detached
				signed = await client.signDataEx(EU_CTX_SIGN_UNKNOWN, bytes, !dataEmbedData, false, false) as Uint8Array;
				ext = 'p7s';
			} else if (dataOutputFmt === 'xades') {
				const r = await client.xadesSignData(keySignAlgo, EU_XADES_TYPE_DETACHED, EU_XADES_SIGN_LEVEL_B_B, syntheticName, bytes, false);
				signed = r.val;
				ext = 'xades.xml';
			} else if (dataOutputFmt === 'asice') {
				const r = await client.asicSignData(keySignAlgo, EU_ASIC_TYPE_E, EU_ASIC_SIGN_TYPE_CADES, EU_ASIC_SIGN_LEVEL_BES, syntheticName, bytes, false);
				signed = r.val;
				ext = 'asice';
			} else {
				// raw: hash data → signHash → raw signature bytes
				const hash = await client.hashData(getDataHashAlgo(), bytes, false) as Uint8Array;
				signed = await client.signHash(keySignAlgo, hash, false, false) as Uint8Array;
				ext = 'sig';
			}
			dataResult = { ok: true, data: signed, fileName: `${syntheticName}_signed_${Date.now()}.${ext}` };
		} catch (e) {
			dataResult = { ok: false, error: e instanceof Error ? e.message : String(e) };
		} finally {
			dataSigning = false;
		}
	}

	function downloadDataResult() {
		if (!dataResult?.data || !dataResult.fileName) return;
		downloadBlob(new Blob([dataResult.data.buffer as ArrayBuffer], { type: 'application/octet-stream' }), dataResult.fileName);
	}
</script>

<svelte:head>
	<title>{t('utils.sign.title', language.current)}  CertVal</title>
</svelte:head>

<section class="utils-sign">
	<UtilsSubnav description={t('utils.sign.description', language.current)} />

	<div class="privacy-note"><p>{t('utils.privacyNote', language.current)}</p></div>

	<InitToast loading={initializing} error={initError} />

	<section class="step">
		<h3>{t('utils.sign.step1.title', language.current)}</h3>
		{#if !keyLoaded}
			<FileDropZone
				bind:files={keyFiles}
				multiple={false}
				label={t('utils.sign.step1.keyDrop', language.current)}
				hint={t('utils.sign.step1.keyHint', language.current)}
			/>
			<label class="field">
				<span>{t('utils.sign.step1.password', language.current)}</span>
				<input
					type="password"
					bind:value={password}
					placeholder={t('utils.sign.step1.passwordPlaceholder', language.current)}
				/>
			</label>
			{#if jksKeys.length > 1}
				<label class="field">
					<span>{t('utils.sign.jksKey', language.current)}</span>
					<select bind:value={selectedJksIndex}>
						{#each jksKeys as k, i (k.alias)}
							<option value={i}>{k.alias}{k.digitalStamp ? t('utils.sign.jksStamp', language.current) : ''}</option>
						{/each}
					</select>
				</label>
			{/if}
			<button
				class="primary"
				type="button"
				disabled={!initialized || keyFiles.length === 0 || keyLoading}
				onclick={unlockKey}
			>
				{keyLoading ? '' : t('utils.sign.step1.unlock', language.current)}
			</button>
			{#if keyError}<div class="status error">{keyError}</div>{/if}
		{:else}
			<div class="status info">
				{t('utils.sign.step1.loaded', language.current)}  {keyFiles[0]?.name}
				<button class="link" type="button" onclick={unloadKey}>
					({t('utils.sign.step1.unload', language.current)})
				</button>
			</div>
		{/if}
	</section>

	{#if keyLoaded}
		<section class="step">
<h3>{t('utils.sign.mode.title', language.current)}</h3>
		<div class="mode-tabs">
			<button
				type="button"
				class="mode-tab"
				class:active={mode === 'advanced'}
				onclick={() => (mode = 'advanced')}
			>
				<span class="tab-num">2.1</span>
				{t('utils.sign.mode.advanced.tab', language.current)}
			</button>
			<button
				type="button"
				class="mode-tab"
				class:active={mode === 'multi'}
				onclick={() => (mode = 'multi')}
			>
				<span class="tab-num">2.2</span>
				{t('utils.sign.mode.multi.tab', language.current)}
			</button>
			<button
				type="button"
				class="mode-tab"
				class:active={mode === 'hash'}
				onclick={() => (mode = 'hash')}
			>
				<span class="tab-num">2.3</span>
				{t('utils.sign.mode.hash.tab', language.current)}
				</button>
			</div>

			{#if mode === 'multi'}
				<p class="mode-desc">
					{t('utils.sign.mode.multi.desc', language.current)}
				</p>

				<FileDropZone
					bind:files={multiDocs}
					multiple={true}
					label={t('utils.sign.step2.docDrop', language.current)}
					hint={t('utils.sign.step2.docHint', language.current)}
				/>

				{#each FORMAT_GROUPS as [group, ids] (group)}
					<div class="format-group">
						<span class="format-group-label">{group}</span>
						<div class="format-row">
							{#each FORMATS.filter((f) => ids.includes(f.id)) as f (f.id)}
								<label class="format">
									<input type="checkbox" bind:checked={multiSelectedFormats[f.id]} />
									<span>{f.label}</span>
									{#if f.needsTSP && f.needsOCSP}
										<span class="badge net">TSP+OCSP</span>
									{:else if f.needsTSP}
										<span class="badge net">TSP</span>
									{/if}
									{#if f.pdfOnly}
										<span class="badge pdf">PDF</span>
									{/if}
								</label>
							{/each}
						</div>
					</div>
				{/each}

				<div class="row">
					<button type="button" class="link" onclick={() => { for (const f of FORMATS) multiSelectedFormats[f.id] = true; }}>
						{t('utils.sign.step3.selectAll', language.current)}
					</button>
					<button type="button" class="link" onclick={() => { for (const f of FORMATS) multiSelectedFormats[f.id] = false; }}>
						{t('utils.sign.step3.clear', language.current)}
					</button>
				</div>
				<label class="checkbox">
					<input type="checkbox" bind:checked={multiDetached} />
					<span>{t('utils.sign.step3.detached', language.current)}</span>
				</label>

				<div class="actions">
					<button class="primary" type="button" disabled={multiSigning} onclick={runMulti}>
						{multiSigning ? '' : t('utils.sign.run', language.current)}
					</button>
				</div>

				{#if multiResults.length > 0}
					<ul class="outputs">
						{#each multiResults as r, i (i)}
							{#if r.docName || r.formatLabel}
								<li class={r.ok ? 'ok' : 'fail'}>
									<span class="name" title={r.ok ? r.fileName : r.error}>
										{#if r.ok && r.fileName}{r.fileName}{:else}{r.docName}{r.formatLabel ? '  ' + r.formatLabel : ''}{/if}
									</span>
									{#if r.ok && r.data}
										<span class="size">{formatBytes(r.data.byteLength)}</span>
										<span class="tag">{r.formatLabel}</span>
										<button type="button" onclick={() => downloadOne(r)} title="Download"><Icon name="download" size="sm" /></button>
									{:else if r.error}
										<span class="err-msg" title={r.error}>{r.error}</span>
									{/if}
								</li>
							{:else if r.error}
								<li class="fail global-err">{r.error}</li>
							{/if}
						{/each}
					</ul>
					{#if multiSuccessCount > 1}
						<button class="primary" type="button" onclick={downloadAll}>
							{t('utils.sign.downloadAll', language.current)}
						</button>
					{/if}
				{/if}

			{:else if mode === 'advanced'}
				<p class="mode-desc">
					{t('utils.sign.mode.advanced.desc', language.current)}
				</p>

				<FileDropZone
					bind:files={advDocs}
					multiple={false}
					label={t('utils.sign.step2.docDrop', language.current)}
					hint={t('utils.sign.step2.docHintSingle', language.current)}
				/>

				<div class="field-group">
					<div class="field-label">{t('utils.sign.format.family', language.current)}</div>
					<div class="radio-row">
						{#each (['cades','xades','pades','asic'] as const) as fam}
							<label class="radio-pill" class:active={advFamily === fam}>
								<input type="radio" name="advFamily" value={fam} bind:group={advFamily} />
								{fam.toUpperCase()}
							</label>
						{/each}
					</div>
				</div>

				{#if advFamily === 'cades'}
					<div class="field-group">
						<div class="field-label">{t('utils.sign.format.level', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={advCadesLevel === EU_SIGN_TYPE_CADES_BES}><input type="radio" name="cadesLvl" value={EU_SIGN_TYPE_CADES_BES} bind:group={advCadesLevel} />BES</label>
							<label class="radio-pill" class:active={advCadesLevel === EU_SIGN_TYPE_CADES_T}><input type="radio" name="cadesLvl" value={EU_SIGN_TYPE_CADES_T} bind:group={advCadesLevel} />T <span class="badge net">TSP</span></label>
							<label class="radio-pill" class:active={advCadesLevel === EU_SIGN_TYPE_CADES_X_LONG}><input type="radio" name="cadesLvl" value={EU_SIGN_TYPE_CADES_X_LONG} bind:group={advCadesLevel} />X-Long <span class="badge net">TSP+OCSP</span></label>
						</div>
					</div>
					<div class="checks-row">
						<label class="checkbox"><input type="checkbox" bind:checked={advDetached} /><span>{t('utils.sign.format.detachedSign', language.current)}</span></label>
						<label class="checkbox"><input type="checkbox" bind:checked={advAppendCert} /><span>{t('utils.sign.format.includeCert', language.current)}</span></label>
					</div>
				{/if}

				{#if advFamily === 'xades'}
					<div class="field-group">
						<div class="field-label">{t('utils.sign.format.xadesType', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={advXadesType === EU_XADES_TYPE_DETACHED}><input type="radio" name="xadesType" value={EU_XADES_TYPE_DETACHED} bind:group={advXadesType} />{t('utils.sign.format.xadesTypeDetached', language.current)}</label>
							<label class="radio-pill" class:active={advXadesType === EU_XADES_TYPE_ENVELOPING}><input type="radio" name="xadesType" value={EU_XADES_TYPE_ENVELOPING} bind:group={advXadesType} />Enveloping</label>
							<label class="radio-pill" class:active={advXadesType === EU_XADES_TYPE_ENVELOPED}><input type="radio" name="xadesType" value={EU_XADES_TYPE_ENVELOPED} bind:group={advXadesType} />Enveloped</label>
						</div>
					</div>
					<div class="field-group">
						<div class="field-label">{t('utils.sign.format.xadesLevel', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={advXadesLevel === EU_XADES_SIGN_LEVEL_B_B}><input type="radio" name="xadesLvl" value={EU_XADES_SIGN_LEVEL_B_B} bind:group={advXadesLevel} />B-B</label>
							<label class="radio-pill" class:active={advXadesLevel === EU_XADES_SIGN_LEVEL_B_T}><input type="radio" name="xadesLvl" value={EU_XADES_SIGN_LEVEL_B_T} bind:group={advXadesLevel} />B-T <span class="badge net">TSP</span></label>
							<label class="radio-pill" class:active={advXadesLevel === EU_XADES_SIGN_LEVEL_B_LT}><input type="radio" name="xadesLvl" value={EU_XADES_SIGN_LEVEL_B_LT} bind:group={advXadesLevel} />B-LT <span class="badge net">TSP+OCSP</span></label>
						</div>
					</div>
				{/if}

				{#if advFamily === 'pades'}
					<div class="status info" style="font-size:0.85rem;">{t('utils.sign.format.padesOnly', language.current)}</div>
					<div class="field-group">
						<div class="field-label">{t('utils.sign.format.padesLevel', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={advPadesLevel === EU_SIGN_TYPE_CADES_BES}><input type="radio" name="padesLvl" value={EU_SIGN_TYPE_CADES_BES} bind:group={advPadesLevel} />B</label>
							<label class="radio-pill" class:active={advPadesLevel === EU_SIGN_TYPE_CADES_T}><input type="radio" name="padesLvl" value={EU_SIGN_TYPE_CADES_T} bind:group={advPadesLevel} />T <span class="badge net">TSP</span></label>
						</div>
					</div>
				{/if}

				{#if advFamily === 'asic'}
					<div class="field-group">
						<div class="field-label">{t('utils.sign.format.asicContainer', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={advAsicContainer === 'e'}><input type="radio" name="asicCont" value="e" bind:group={advAsicContainer} />ASiC-E</label>
							<label class="radio-pill" class:active={advAsicContainer === 's'}><input type="radio" name="asicCont" value="s" bind:group={advAsicContainer} />ASiC-S</label>
						</div>
					</div>
					<div class="field-group">
						<div class="field-label">{t('utils.sign.format.asicSignType', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={advAsicSignType === EU_ASIC_SIGN_TYPE_CADES}><input type="radio" name="asicST" value={EU_ASIC_SIGN_TYPE_CADES} bind:group={advAsicSignType} />CAdES</label>
							<label class="radio-pill" class:active={advAsicSignType === EU_ASIC_SIGN_TYPE_XADES}><input type="radio" name="asicST" value={EU_ASIC_SIGN_TYPE_XADES} bind:group={advAsicSignType} />XAdES</label>
						</div>
					</div>
					<div class="field-group">
						<div class="field-label">{t('utils.sign.format.level', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={advAsicLevel === EU_ASIC_SIGN_LEVEL_BES}><input type="radio" name="asicLvl" value={EU_ASIC_SIGN_LEVEL_BES} bind:group={advAsicLevel} />BES</label>
							<label class="radio-pill" class:active={advAsicLevel === EU_ASIC_SIGN_LEVEL_T}><input type="radio" name="asicLvl" value={EU_ASIC_SIGN_LEVEL_T} bind:group={advAsicLevel} />T <span class="badge net">TSP</span></label>
						</div>
					</div>
				{/if}

				<div class="actions">
					<button class="primary" type="button" disabled={advSigning || advDocs.length === 0} onclick={runAdvanced}>
						{advSigning ? '' : t('utils.sign.run', language.current)}
					</button>
				</div>

				{#if advResult}
					{#if advResult.ok && advResult.data && advResult.fileName}
						<div class="result-row ok">
							<span class="name">{advResult.fileName}</span>
							<span class="size">{formatBytes(advResult.data.byteLength)}</span>
							<button type="button" onclick={() => { if (advResult?.data && advResult?.fileName) downloadBlob(new Blob([advResult.data.buffer as ArrayBuffer], { type: 'application/octet-stream' }), advResult.fileName); }}>
								<Icon name="download" size="sm" /> {t('utils.sign.download', language.current)}
							</button>
						</div>
					{:else if advResult.error}
						<div class="status error">{advResult.error}</div>
					{/if}
				{/if}

			{:else}
				<!-- Sub-mode toggle: sign arbitrary text/data vs. sign pre-computed hash -->
				<div class="mode-tabs mode-tabs--sm">
					<button type="button" class="mode-tab" class:active={hashSubMode === 'data'} onclick={() => hashSubMode = 'data'}>
						{t('utils.sign.hash.subModeData', language.current)}
					</button>
					<button type="button" class="mode-tab" class:active={hashSubMode === 'hash'} onclick={() => hashSubMode = 'hash'}>
						{t('utils.sign.hash.subModeHash', language.current)}
					</button>
				</div>

				{#if hashSubMode === 'hash'}
					<p class="mode-desc">
						{t('utils.sign.mode.hash.desc', language.current)}
					</p>

					{#if keyLoaded && keySignAlgo > 0}
						<div class="hash-algo-card">
							<span>{t('utils.sign.hash.algoLabel', language.current)}</span>
							<strong>{signAlgoLabel(keySignAlgo)}</strong>
							<span class="hash-algo-sep"></span>
							<span>{t('utils.sign.hash.expectedLen', language.current)} <strong>{keyHashSize} {t('utils.sign.hash.bytes', language.current)}</strong></span>
						</div>
					{/if}

					<div class="field-group">
						<div class="field-label">{t('utils.sign.hash.inputLabel', language.current)}</div>
						<div class="radio-row" style="margin-bottom:0.4rem;">
							<label class="radio-pill" class:active={hashEncoding === 'hex'}><input type="radio" name="hashEnc" value="hex" bind:group={hashEncoding} />Hex</label>
							<label class="radio-pill" class:active={hashEncoding === 'base64'}><input type="radio" name="hashEnc" value="base64" bind:group={hashEncoding} />Base64</label>
						</div>
						<textarea
							class="hash-input"
							bind:value={hashInput}
							rows={4}
							placeholder={hashEncoding === 'hex'
								? t('utils.sign.hash.hexPlaceholder', language.current)
								: t('utils.sign.hash.base64Placeholder', language.current)}
						></textarea>
						{#if hashInput.trim() && keyHashSize > 0}
							<p class="hash-len-hint" class:len-warn={hashByteCount !== keyHashSize}>
								{t('utils.sign.hash.entered', language.current, { count: String(hashByteCount) })}
								{hashByteCount !== keyHashSize ? t('utils.sign.hash.expectedCount', language.current, { count: String(keyHashSize) }) : ''}
							</p>
						{/if}
					</div>

					<div class="checks-row">
						<label class="checkbox"><input type="checkbox" bind:checked={hashAppendCert} /><span>{t('utils.sign.hash.includeCert', language.current)}</span></label>
						<label class="checkbox"><input type="checkbox" bind:checked={hashOutputBase64} /><span>{t('utils.sign.hash.outputBase64', language.current)}</span></label>
					</div>

					<div class="actions">
						<button class="primary" type="button" disabled={hashSigning || !hashInput.trim()} onclick={runHash}>
							{hashSigning ? '' : t('utils.sign.hash.signBtn', language.current)}
						</button>
					</div>

					{#if hashResult}
						{#if hashResult.ok && hashResult.data}
							<div class="result-row ok">
								<span class="name">{hashResult.fileName}</span>
								{#if hashResult.data instanceof Uint8Array}
									<span class="size">{formatBytes(hashResult.data.byteLength)}</span>
								{/if}
								<button type="button" onclick={downloadHashResult}>
									↓ {t('utils.sign.download', language.current)}
								</button>
							</div>
							{#if typeof hashResult.data === 'string'}
								<pre class="hash-out">{hashResult.data}</pre>
							{:else if hashResult.data instanceof Uint8Array}
								<pre class="hash-out">{bytesToHex(hashResult.data)}</pre>
							{/if}
						{:else if hashResult.error}
							<div class="status error">{hashResult.error}</div>
						{/if}
					{/if}
				{:else}
					<p class="mode-desc">{t('utils.sign.hash.dataDesc', language.current)}</p>

					<div class="field-group">
						<div class="field-label">{t('utils.sign.hash.dataEncLabel', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={dataEncoding === 'text'}><input type="radio" name="dataEnc" value="text" bind:group={dataEncoding} />{t('utils.sign.hash.dataText', language.current)}</label>
							<label class="radio-pill" class:active={dataEncoding === 'base64'}><input type="radio" name="dataEnc" value="base64" bind:group={dataEncoding} />{t('utils.sign.hash.dataBase64', language.current)}</label>
						</div>
					</div>

					<div class="field-group">
						<div class="field-label">{t('utils.sign.hash.dataOutFmtLabel', language.current)}</div>
						<div class="radio-row">
							<label class="radio-pill" class:active={dataOutputFmt === 'cades'}><input type="radio" name="dataFmt" value="cades" bind:group={dataOutputFmt} />CAdES (.p7s)</label>
							<label class="radio-pill" class:active={dataOutputFmt === 'xades'}><input type="radio" name="dataFmt" value="xades" bind:group={dataOutputFmt} />XAdES (.xml)</label>
							<label class="radio-pill" class:active={dataOutputFmt === 'asice'}><input type="radio" name="dataFmt" value="asice" bind:group={dataOutputFmt} />ASiC-E (.asice)</label>
							<label class="radio-pill" class:active={dataOutputFmt === 'raw'}><input type="radio" name="dataFmt" value="raw" bind:group={dataOutputFmt} />{t('utils.sign.hash.dataOutFmtRaw', language.current)}</label>
						</div>
					</div>

					{#if dataOutputFmt === 'cades'}
						<div class="field-group">
							<label class="checkbox">
								<input type="checkbox" bind:checked={dataEmbedData} />
								<span>{t('utils.sign.hash.dataEmbedData', language.current)}</span>
							</label>
							<p class="mode-desc" style="margin-top:0.35rem">{dataEmbedData ? t('utils.sign.hash.dataEmbedDataHintOn', language.current) : t('utils.sign.hash.dataEmbedDataHintOff', language.current)}</p>
						</div>
					{/if}

					<div class="field-group">
						<div class="field-label">{t('utils.sign.hash.dataInputLabel', language.current)}</div>
						<textarea
							class="hash-input"
							bind:value={dataInput}
							rows={6}
							placeholder={dataEncoding === 'text'
								? t('utils.sign.hash.dataTextPlaceholder', language.current)
								: t('utils.sign.hash.dataBase64Placeholder', language.current)}
						></textarea>
					</div>

					<div class="actions">
						<button class="primary" type="button" disabled={dataSigning || !dataInput.trim()} onclick={runData}>
							{dataSigning ? '' : t('utils.sign.hash.dataSignBtn', language.current)}
						</button>
					</div>

					{#if dataResult}
						{#if dataResult.ok && dataResult.data && dataResult.fileName}
							<div class="result-row ok">
								<span class="name">{dataResult.fileName}</span>
								<span class="size">{formatBytes(dataResult.data.byteLength)}</span>
								<button type="button" onclick={downloadDataResult}>
									<Icon name="download" size="sm" /> {t('utils.sign.download', language.current)}
								</button>
							</div>
							<pre class="hash-out">{bytesToHex(dataResult.data)}</pre>
						{:else if dataResult.error}
							<div class="status error">{dataResult.error}</div>
						{/if}
					{/if}
				{/if}
			{/if}
		</section>
	{/if}
</section>

<style>
	.utils-sign {
		max-width: 960px;
		margin: 0 auto;
		padding: 2rem 1.5rem 3rem;
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
	}
	.privacy-note {
		padding: 0.75rem 1rem;
		background: var(--color-primary-light);
		border: 1px solid var(--color-border);
		color: var(--color-primary);
		border-radius: 8px;
		font-size: 0.9rem;
	}
	.privacy-note p { margin: 0; }
	.step {
		padding: 1rem 1.25rem;
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: 12px;
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}
	.step h3 { margin: 0; font-size: 1.05rem; }
	.status {
		padding: 0.5rem 0.75rem;
		border-radius: 8px;
		font-size: 0.9rem;
	}
	.status.info { background: var(--color-primary-light); color: var(--color-primary); }
	.status.error { background: var(--color-error-light); color: var(--color-error); }
	.field {
		display: flex;
		flex-direction: column;
		gap: 0.3rem;
		font-size: 0.9rem;
	}
	.field input, .field select {
		padding: 0.5rem 0.75rem;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		font-size: 1rem;
		background: var(--color-surface-inset);
		color: var(--color-text);
	}
	/* Mode tabs */
	.mode-tabs {
		display: flex;
		gap: 0.5rem;
		flex-wrap: wrap;
	}
	.mode-tab {
		display: flex;
		align-items: center;
		gap: 0.4rem;
		padding: 0.5rem 1rem;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		background: var(--color-surface-inset);
		color: var(--color-text);
		cursor: pointer;
		font-size: 0.9rem;
		transition: background 0.12s, border-color 0.12s;
	}
	.mode-tab:hover { background: var(--color-surface-hover); }
	.mode-tab.active {
		background: var(--color-text);
		color: var(--color-bg);
		border-color: var(--color-text);
	}
	.tab-num {
		font-size: 0.75rem;
		font-weight: 600;
		opacity: 0.75;
	}
	.mode-desc {
		margin: 0;
		font-size: 0.88rem;
		color: var(--color-text-muted);
	}
	.hash-algo-card {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.4rem;
		padding: 0.5rem 0.75rem;
		background: var(--color-primary-light);
		border: 1px solid var(--color-border);
		border-radius: 8px;
		font-size: 0.85rem;
		color: var(--color-primary);
	}
	.hash-algo-sep { color: var(--color-text-muted); }
	.hash-len-hint {
		margin: 0.2rem 0 0;
		font-size: 0.8rem;
		color: var(--color-text-muted, #6a7280);
	}
	.hash-len-hint.len-warn { color: var(--color-warning); font-weight: 500; }
	/* Field groups */
	.field-group { display: flex; flex-direction: column; gap: 0.3rem; }
	.field-label { font-size: 0.82rem; font-weight: 600; color: var(--color-text-muted); }
	.radio-row { display: flex; flex-wrap: wrap; gap: 0.4rem; }
	.radio-pill {
		display: inline-flex;
		align-items: center;
		gap: 0.3rem;
		padding: 0.35rem 0.7rem;
		border: 1px solid var(--color-border);
		border-radius: 999px;
		cursor: pointer;
		font-size: 0.85rem;
		transition: background 0.1s;
	}
	.radio-pill.active { background: var(--color-primary-light); border-color: var(--color-primary); color: var(--color-primary); }
	.radio-pill input[type="radio"] { display: none; }
	.checks-row { display: flex; flex-wrap: wrap; gap: 1rem; }
	/* Primary button styling: elegant high-tech rounded rectangle, nicely balanced */
	.primary {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 0.5rem;
		padding: 0.75rem 2rem;
		border-radius: 6px;
		border: 1px solid rgba(255, 255, 255, 0.1);
		background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-hover) 100%);
		color: var(--color-primary-text);
		font-size: 0.95rem;
		font-weight: 600;
		cursor: pointer;
		transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
		box-shadow: 0 4px 15px rgba(79, 70, 255, 0.25);
		align-self: center; /* No longer left-aligned! Centered action */
		margin: 0.5rem auto 0;
	}
	.primary:hover:not(:disabled) {
		transform: translateY(-2px);
		box-shadow: 0 6px 20px rgba(79, 70, 255, 0.4);
		border-color: rgba(255, 255, 255, 0.2);
	}
	.primary:disabled {
		opacity: 0.45;
		cursor: not-allowed;
		transform: none;
		box-shadow: none;
	}
	.link {
		background: transparent;
		border: 0;
		color: var(--color-primary);
		text-decoration: underline;
		cursor: pointer;
		font-size: 0.9rem;
	}
	/* Format groups */
	.format-group { display: flex; flex-direction: column; gap: 0.35rem; }
	.format-group-label {
		font-size: 0.78rem;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.06em;
		color: var(--color-text-muted, #6a7280);
	}
	.format-row { display: flex; flex-wrap: wrap; gap: 0.45rem; }
	.format {
		display: inline-flex;
		gap: 0.4rem;
		align-items: center;
		padding: 0.4rem 0.65rem;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		cursor: pointer;
		font-size: 0.875rem;
		background: var(--color-surface-inset);
		color: var(--color-text);
	}
	.badge {
		font-size: 0.65rem;
		padding: 0.1rem 0.4rem;
		border-radius: 999px;
		font-weight: 600;
	}
	.badge.net { background: var(--color-warning-light); color: var(--color-warning); }
	.badge.pdf { background: #fce7f3; color: #831843; }
	.row { display: flex; gap: 1rem; justify-content: center; } /* Centered link-action options */
	.checkbox { display: inline-flex; gap: 0.5rem; align-items: center; font-size: 0.9rem; }
	.actions { display: flex; gap: 0.75rem; justify-content: center; width: 100%; } /* Centered actions block! */
	/* Results */
	.outputs {
		list-style: none;
		margin: 0;
		padding: 0;
		display: flex;
		flex-direction: column;
		gap: 0.4rem;
	}
	.outputs li {
		display: flex;
		gap: 0.6rem;
		align-items: center;
		padding: 0.45rem 0.75rem;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		font-size: 0.875rem;
	}
	.outputs li.ok { background: var(--color-success-light); border-color: var(--color-success); }
	.outputs li.fail { background: var(--color-error-light); border-color: var(--color-error); }
	.outputs li.global-err { color: var(--color-error); }
	.outputs .name {
		flex: 1;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.outputs .size {
		font-size: 0.8rem;
		color: var(--color-text-muted, #6a7280);
		font-variant-numeric: tabular-nums;
		white-space: nowrap;
	}
	.outputs .tag {
		font-size: 0.72rem;
		padding: 0.15rem 0.5rem;
		border-radius: 999px;
		background: var(--color-primary-light);
		color: var(--color-primary);
		white-space: nowrap;
	}
	.outputs .err-msg {
		flex: 1;
		color: var(--color-error);
		font-size: 0.82rem;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.outputs button {
		border: 0;
		background: var(--color-text);
		color: var(--color-bg);
		width: 28px;
		height: 28px;
		border-radius: 50%;
		cursor: pointer;
		flex-shrink: 0;
	}
	/* Advanced / hash result row */
	.result-row {
		display: flex;
		gap: 0.6rem;
		align-items: center;
		padding: 0.55rem 0.75rem;
		border-radius: 8px;
		font-size: 0.9rem;
	}
	.result-row.ok { background: var(--color-success-light); border: 1px solid var(--color-success); }
	.result-row .name { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.result-row .size { font-size: 0.8rem; color: var(--color-text-muted); white-space: nowrap; }
	.result-row button {
		padding: 0.35rem 0.8rem;
		border: 0;
		background: var(--color-text);
		color: var(--color-bg);
		border-radius: 999px;
		cursor: pointer;
		font-size: 0.85rem;
		flex-shrink: 0;
	}
	/* Hash textarea and output */
	.hash-input {
		width: 100%;
		padding: 0.6rem 0.75rem;
		border: 1px solid var(--color-border);
		border-radius: 8px;
		font-family: monospace;
		font-size: 0.85rem;
		resize: vertical;
		box-sizing: border-box;
		background: var(--color-surface-inset);
		color: var(--color-text);
	}
	.hash-out {
		background: var(--color-surface-inset);
		border: 1px solid var(--color-border);
		border-radius: 8px;
		padding: 0.75rem;
		font-family: monospace;
		font-size: 0.78rem;
		overflow-x: auto;
		white-space: pre-wrap;
		word-break: break-all;
		max-height: 200px;
		overflow-y: auto;
		margin: 0;
		color: var(--color-text);
	}
</style>
