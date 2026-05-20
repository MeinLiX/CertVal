<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import UtilsSubnav from '$lib/components/layout/UtilsSubnav.svelte';
	import InitToast from '$lib/components/ui/InitToast.svelte';
	import FileDropZone from '$lib/components/utils/FileDropZone.svelte';
	import {
		VerifyClient,
		flattenSignsInfos,
		type EUVerifyResult,
		type EUVerifySignInfo
	} from '$lib/iit/verifyClient';
	import { downloadText, formatBytes } from '$lib/utils/fileDownload';


	let signedFiles = $state<File[]>([]);
	let detached = $state(false);
	let originalFiles = $state<File[]>([]);

	let initializing = $state(false);
	let initialized = $state(false);
	let busy = $state(false);
	let initError = $state<string | null>(null);
	let runError = $state<string | null>(null);

	type RunResult = {
		fileName: string;
		fileSize: number;
		ok: boolean;
		signs: EUVerifySignInfo[];
		raw: EUVerifyResult;
		message?: string;
	};
	let results = $state<RunResult[]>([]);

	const client = new VerifyClient();

	onMount(async () => {
		initializing = true;
		initError = null;
		try {
			await client.initialize({
				language: language.current === 'uk' ? 'ua' : 'en',
				httpProxyServiceURL: '',
				directAccess: true,
				CAs: '/iit/data/CAs.json',
				CACertificates: '/iit/data/CACertificates.p7b',
				verifySettings: {
					signInfoTmpl: '/iit/data/SignInfoTmpl.xml',
					ocspResponseExpireTime: 30,
					supportAdvancedCertificates: true
				}
			});
			initialized = true;
		} catch (e) {
			initError = e instanceof Error ? e.message : String(e);
		} finally {
			initializing = false;
		}
	});

	onDestroy(() => client.terminate());

	function describeError(code: number): string {
		const lang = language.current;
		switch (code) {
			case 1: return t('utils.verify.errors.code1', lang);
			case 2: return t('utils.verify.errors.code2', lang);
			case 3: return t('utils.verify.errors.code3', lang);
			default: return t('utils.verify.errors.codeDefault', lang);
		}
	}

	async function run() {
		if (!initialized || busy) return;
		runError = null;
		results = [];

		if (signedFiles.length === 0) { runError = t('utils.verify.errors.noFiles', language.current); return; }
		if (detached && originalFiles.length === 0) { runError = t('utils.verify.errors.noOriginal', language.current); return; }

		busy = true;
		try {
			const out: RunResult[] = [];
			for (const sig of signedFiles) {
				const files: File[] = detached ? [originalFiles[0], sig] : [sig];
				try {
					const res = await client.verifyFiles(files);
					const signs = flattenSignsInfos(res.signsInfos);
					out.push({ fileName: sig.name, fileSize: sig.size, ok: res.resultCode === 0 && signs.length > 0, signs, raw: res, message: res.resultCode === 0 ? undefined : describeError(res.resultCode) });
				} catch (e) {
					const msg = e instanceof Error ? e.message : String(e);
					out.push({ fileName: sig.name, fileSize: sig.size, ok: false, signs: [], raw: {} as EUVerifyResult, message: msg });
				}
			}
			results = out;
		} finally {
			busy = false;
		}
	}

	function makePdfReportHtml(r: RunResult): string {
		const now = new Date();
		const p2 = (n: number) => String(n).padStart(2, '0');
		const timeStr = `${p2(now.getHours())}:${p2(now.getMinutes())}:${p2(now.getSeconds())}`;
		const dateStr = `${p2(now.getDate())}.${p2(now.getMonth() + 1)}.${now.getFullYear()}`;
		const resultText = r.ok
			? 'Підпис створено та перевірено успішно. Цілісність даних підтверджено'
			: (r.message ?? 'Підпис недійсний');
		const resultColor = r.ok ? '#000' : '#c00';

		const signersHtml = r.signs.map((s) => {
			const info = s.signerInfo as Record<string, unknown>;
			const time = s.signTimeInfo;
			const timeLabel = time?.isTimeStamp
				? 'Час підпису (підтверджено кваліфікованою позначкою часу для підпису від Надавача)'
				: 'Час підпису (не підтверджено кваліфікованою позначкою часу)';
			const keyCarrier = s.qscd?.use ? (s.qscd.name ?? 'Захищений') : 'Незахищений';
			const certValid = certValidAtSign(s);
			const rows: [string, unknown][] = [
				['Підписувач', info.subjCN],
				['П.І.Б.', info.subjFullName],
				['Кваліфікація', s.qscd?.use ? 'Кваліфікований електронний підпис (КЕП)' : 'Удосконалений електронний підпис (УЕП)'],
				['Цифрова печатка', s.isDigitalStamp ? 'Так' : null],
				['Країна', info.subjCountry],
				['РНОКПП', info.subjDRFOCode],
				['Організація (установа)', info.subjOrg],
				['Код ЄДРПОУ', info.subjEDRPOUCode],
				['Посада', info.subjTitle],
				[timeLabel, time?.time],
				['Сертифікат виданий', info.issuerCN],
				['Серійний номер', info.serial],
				['Тип носія особистого ключа', keyCarrier],
				['Серійний номер носія', s.qscd?.sn ?? null],
				['Алгоритм підпису', s.signAlgo],
				['Тип контейнера', s.signContainer],
				['Формат підпису', s.signFormat],
				['Дійсний сертифікат на момент підпису', certValid === null ? null : certValid ? '✓ Так' : '✗ Ні'],
			];
			const rowsHtml = rows
				.filter(([, v]) => v != null && v !== '')
				.map(([k, v]) => `<tr><td class="lbl">${k}</td><td>${String(v)}</td></tr>`)
				.join('');
			return `<table class="info"><tbody>${rowsHtml}</tbody></table>`;
		}).join('<hr class="sep">');

		return `<!DOCTYPE html><html lang="uk"><head><meta charset="UTF-8"><title>Протокол перевірки — ${r.fileName}</title><style>body{font-family:"Times New Roman",serif;max-width:680px;margin:2cm auto;font-size:12pt;color:#000;line-height:1.4}.title1{text-align:center;font-size:12pt;font-weight:normal;margin:0 0 .4em}.title2{text-align:center;font-size:14pt;font-weight:bold;margin:.4em 0 .1em;letter-spacing:.05em}.title3{text-align:center;font-size:12pt;margin:0 0 1.2em}table.info{width:100%;border-collapse:collapse;margin:.25em 0}table.info td{padding:3px 0;vertical-align:top}table.info td.lbl{padding-right:1.5em;width:52%}.result{font-weight:bold;margin:.75em 0}hr.sep{border:none;border-top:1px dashed #aaa;margin:1em 0}.footer{margin-top:2em;font-size:10pt;color:#555}@media print{body{margin:0}@page{margin:1.5cm}}</style></head><body><p class="title1">Онлайн сервіс створення та перевірки кваліфікованого та удосконаленого електронного підпису</p><p class="title2">ПРОТОКОЛ</p><p class="title3">створення та перевірки кваліфікованого та удосконаленого електронного підпису</p><table class="info"><tbody><tr><td class="lbl">Дата та час</td><td>${timeStr} ${dateStr}</td></tr><tr><td class="lbl">Назва файлу з підписом</td><td>${r.fileName}</td></tr><tr><td class="lbl">Розмір файлу з підписом</td><td>${formatBytes(r.fileSize)}</td></tr></tbody></table><p class="result" style="color:${resultColor}">Результат перевірки підпису: ${resultText}</p>${signersHtml}<p class="footer">Версія від: ${now.getFullYear()}.${p2(now.getMonth() + 1)}.${p2(now.getDate())} 13:00</p></body></html>`;
	}

	function openPdfReport(r: RunResult) {
		const html = makePdfReportHtml(r);
		const win = window.open('', '_blank', 'width=860,height=700');
		if (!win) return;
		win.document.open();
		win.document.write(html);
		win.document.close();
		setTimeout(() => { win.focus(); win.print(); }, 400);
	}

	function downloadRaw(r: RunResult) {
		downloadText(
			JSON.stringify(r.raw, (_k, v) => {
				if (v instanceof Uint8Array) return `<binary ${v.byteLength} bytes>`;
				if (v instanceof File) return { name: v.name, size: v.size, type: v.type };
				return v;
			}, 2),
			`${r.fileName}.raw.json`,
			'application/json'
		);
	}

	function signerQual(s: EUVerifySignInfo): string {
		const lang = language.current;
		if (s.qscd?.use) return t('utils.verify.quality.qes', lang);
		return t('utils.verify.quality.ades', lang);
	}

	function certValidAtSign(s: EUVerifySignInfo): boolean | null {
		const signTime = s.signTimeInfo?.time;
		const from = s.signerInfo.certBeginTime;
		const to = s.signerInfo.certEndTime;
		if (!signTime || !from || !to) return null;
		try {
			const p = (d: string) => {
				const m = d.match(/^(\d{2})\.(\d{2})\.(\d{4})\s+(\d{2}:\d{2}:\d{2})$/);
				if (m) return new Date(`${m[3]}-${m[2]}-${m[1]}T${m[4]}`).getTime();
				return new Date(d).getTime();
			};
			const st = p(signTime), ft = p(from), tt = p(to);
			if (isNaN(st) || isNaN(ft) || isNaN(tt)) return null;
			return st >= ft && st <= tt;
		} catch { return null; }
	}

</script>

<svelte:head>
	<title>{t('utils.verify.title', language.current)} · CertVal</title>
</svelte:head>

<section class="utils-verify">
	<UtilsSubnav description={t('utils.verify.description', language.current)} />

	<div class="privacy-note">
		<p>{t('utils.privacyNote', language.current)}</p>
	</div>

	<InitToast loading={initializing} error={initError} />

	<div class="form">
		<label class="checkbox">
			<input type="checkbox" bind:checked={detached} />
			<span>{t('utils.verify.detached', language.current)}</span>
		</label>

		{#if detached}
			<div class="field">
				<div class="field-label">{t('utils.verify.originalFile', language.current)}</div>
				<FileDropZone bind:files={originalFiles} multiple={false} label={t('utils.verify.originalDrop', language.current)} />
			</div>
		{/if}

		<div class="field">
			<div class="field-label">{t('utils.verify.signatureFile', language.current)}</div>
			<FileDropZone bind:files={signedFiles} multiple={true} label={t('utils.verify.signedDrop', language.current)} hint={t('utils.verify.signedHint', language.current)} />
		</div>

		<div class="actions">
			<button class="primary" type="button" disabled={!initialized || busy} onclick={run}>
				{busy ? '…' : t('utils.verify.run', language.current)}
			</button>
		</div>
		{#if runError}<div class="status error">{runError}</div>{/if}
	</div>

	{#if results.length}
		<h2>{t('utils.verify.results', language.current)}</h2>
		<div class="results">
			{#each results as r, i (i)}
				<article class="result" class:ok={r.ok} class:bad={!r.ok}>
					<header>
						<span class="badge">{r.ok ? '✓' : '!'}</span>
						<div>
							<div class="file-name">{r.fileName}</div>
							<div class="file-meta">
								{formatBytes(r.fileSize)} · {r.ok ? t('utils.verify.valid', language.current) : t('utils.verify.invalid', language.current)}
						{#if r.signs.length > 0}· {r.signs.length} {r.signs.length === 1 ? t('utils.verify.signers1', language.current) : t('utils.verify.signersMany', language.current)}{/if}
							</div>
						</div>
					</header>

					{#if r.message}<div class="msg">{r.message}</div>{/if}

					{#if r.signs.length > 0}
						<div class="signers">
							{#each r.signs as s, idx (idx)}
								<div class="signer-card">
									<div class="signer-hdr">
										<span class="signer-num">{idx + 1}</span>
										<div>
											<div class="signer-name">{s.signerInfo.subjFullName ?? s.signerInfo.subjCN ?? '—'}</div>
									<div class="signer-sub">{t('utils.verify.signatureOf', language.current, { idx: String(idx + 1), total: String(r.signs.length) })} · {signerQual(s)}{s.isDigitalStamp ? t('utils.verify.stamp', language.current) : ''}</div>
										</div>
									</div>
									<dl>
										{#if (s.signerInfo as Record<string,unknown>).subjCountry}<dt>{t('utils.verify.signerFields.country', language.current)}</dt><dd>{String((s.signerInfo as Record<string,unknown>).subjCountry)}</dd>{/if}
										{#if s.signerInfo.subjOrg}<dt>{t('utils.verify.signerFields.org', language.current)}</dt><dd>{s.signerInfo.subjOrg}</dd>{/if}
										{#if s.signerInfo.subjTitle}<dt>{t('utils.verify.signerFields.position', language.current)}</dt><dd>{s.signerInfo.subjTitle}</dd>{/if}
										{#if s.signerInfo.subjEMail}<dt>Email</dt><dd>{s.signerInfo.subjEMail}</dd>{/if}
										{#if s.signerInfo.subjDRFOCode}<dt>{t('utils.verify.signerFields.drfo', language.current)}</dt><dd>{s.signerInfo.subjDRFOCode}</dd>{/if}
										{#if s.signerInfo.subjEDRPOUCode}<dt>{t('utils.verify.signerFields.edrpou', language.current)}</dt><dd>{s.signerInfo.subjEDRPOUCode}</dd>{/if}
										<dt>{t('utils.verify.fields.signedAt', language.current)}{s.signTimeInfo?.isTimeStamp ? ' (TSP)' : ''}</dt><dd>{s.signTimeInfo?.time ?? '—'}</dd>
										<dt>{t('utils.verify.fields.issuer', language.current)}</dt><dd>{s.signerInfo.issuerCN ?? '—'}</dd>
										<dt>{t('utils.verify.fields.serial', language.current)}</dt><dd class="mono">{s.signerInfo.serial ?? '—'}</dd>
										<dt>{t('utils.verify.signerFields.keyCarrier', language.current)}</dt><dd>{s.qscd?.use ? (s.qscd.name ?? (t('utils.verify.signerFields.protected', language.current))) : (t('utils.verify.signerFields.unprotected', language.current))}</dd>
										{#if s.qscd?.use && s.qscd?.sn}<dt>{t('utils.verify.signerFields.deviceSn', language.current)}</dt><dd class="mono">{s.qscd.sn}</dd>{/if}
										<dt>{t('utils.verify.signerFields.algorithm', language.current)}</dt><dd>{s.signAlgo ?? '—'}</dd>
										<dt>{t('utils.verify.signerFields.containerType', language.current)}</dt><dd>{s.signContainer}</dd>
										<dt>{t('utils.verify.signerFields.signFormat', language.current)}</dt><dd>{s.signFormat}</dd>
										<dt>{t('utils.verify.fields.validFrom', language.current)}</dt><dd>{s.signerInfo.certBeginTime ?? '—'}</dd>
										<dt>{t('utils.verify.fields.validTo', language.current)}</dt><dd>{s.signerInfo.certEndTime ?? '—'}</dd>
										<dt>{t('utils.verify.signerFields.allContentCovered', language.current)}</dt><dd>{s.isAllContentCovered ? (t('common.yes', language.current)) : (t('common.no', language.current))}</dd>
									</dl>
								</div>
							{/each}
						</div>
					{/if}

					<div class="dl">
						<button type="button" onclick={() => openPdfReport(r)}>
						📄 {t('utils.verify.pdfReport', language.current)}
						</button>
						<button type="button" onclick={() => downloadRaw(r)}>
							⬇️ {t('utils.verify.downloadRaw', language.current)}
						</button>
					</div>
				</article>
			{/each}
		</div>
	{/if}
</section>

<style>
	.utils-verify {
		max-width: 960px;
		margin: 0 auto;
		padding: 2rem 1.5rem 3rem;
		display: flex;
		flex-direction: column;
		gap: 1.25rem;
	}
	.privacy-note {
		padding: 0.75rem 1rem;
		background: var(--color-info-bg, #eef6ff);
		border: 1px solid var(--color-info-border, #b9d6ff);
		color: var(--color-info-text, #1a4480);
		border-radius: 8px;
		font-size: 0.9rem;
	}
	.privacy-note p { margin: 0; }
	.status {
		padding: 0.5rem 0.75rem;
		border-radius: 8px;
		font-size: 0.9rem;
	}
	.status.info { background: #eef6ff; color: #1a4480; }
	.status.error { background: #fee2e2; color: #991b1b; }
	.form { display: flex; flex-direction: column; gap: 1rem; }
	.field { display: flex; flex-direction: column; gap: 0.4rem; }
	.field-label { font-size: 0.85rem; color: var(--color-text-muted, #6a7280); }
	.checkbox { display: inline-flex; gap: 0.5rem; align-items: center; font-size: 0.95rem; }
	.actions { display: flex; gap: 0.75rem; justify-content: center; width: 100%; margin: 0.5rem auto 0; }
	.primary {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 0.5rem;
		padding: 0.75rem 2rem;
		border-radius: 6px;
		border: 1px solid rgba(255, 255, 255, 0.1);
		background: linear-gradient(135deg, #4f46ff 0%, #3e35df 100%);
		color: #fff;
		font-size: 0.95rem;
		font-weight: 600;
		cursor: pointer;
		transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
		box-shadow: 0 4px 15px rgba(79, 70, 255, 0.25);
		align-self: center; /* Center key action button */
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
	h2 { font-size: 1.2rem; margin: 1.5rem 0 0.5rem; text-align: center; } /* Centered results heading */
	.results { display: flex; flex-direction: column; gap: 0.75rem; }
	.result {
		border: 1px solid var(--color-border, #d8dde6);
		border-radius: 12px;
		padding: 1rem;
		background: #fff;
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}
	.result.ok { border-left: 4px solid #16a34a; }
	.result.bad { border-left: 4px solid #b91c1c; }
	.result header { display: flex; gap: 0.75rem; align-items: center; }
	.badge {
		width: 32px;
		height: 32px;
		border-radius: 50%;
		display: inline-flex;
		justify-content: center;
		align-items: center;
		font-weight: 700;
		color: #fff;
		flex-shrink: 0;
	}
	.ok .badge { background: #16a34a; }
	.bad .badge { background: #b91c1c; }
	.file-name { font-weight: 500; }
	.file-meta { font-size: 0.85rem; color: var(--color-text-muted, #6a7280); }
	.msg { font-size: 0.9rem; color: #6a7280; }
	/* Signers */
	.signers { display: flex; flex-direction: column; gap: 0.75rem; }
	.signer-card {
		border: 1px solid var(--color-border, #d8dde6);
		border-radius: 8px;
		padding: 0.75rem;
		background: #fafafa;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}
	.signer-hdr { display: flex; gap: 0.6rem; align-items: flex-start; }
	.signer-num {
		background: #e0e7ff;
		color: #312e81;
		border-radius: 50%;
		width: 24px;
		height: 24px;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 0.75rem;
		font-weight: 700;
		flex-shrink: 0;
	}
	.signer-name { font-weight: 600; font-size: 0.95rem; }
	.signer-sub { font-size: 0.8rem; color: var(--color-text-muted, #6a7280); }
	dl {
		display: grid;
		grid-template-columns: max-content 1fr;
		column-gap: 1rem;
		row-gap: 0.2rem;
		margin: 0;
		font-size: 0.875rem;
	}
	dt { color: var(--color-text-muted, #6a7280); white-space: nowrap; }
	dd { margin: 0; word-break: break-word; }
	.mono { font-family: monospace; font-size: 0.8rem; }
	.ok-text { color: #15803d; font-weight: 500; }
	.err-text { color: #b91c1c; font-weight: 500; }
	/* Download buttons - elegantly centered */
	.dl { display: flex; gap: 0.5rem; flex-wrap: wrap; justify-content: center; margin-top: 0.5rem; }
	.dl button {
		display: inline-flex;
		align-items: center;
		gap: 0.4rem;
		padding: 0.45rem 1rem;
		border-radius: 6px;
		border: 1px solid var(--color-border, #d8dde6);
		background: #f8fafc;
		cursor: pointer;
		font-size: 0.85rem;
		font-weight: 600;
		transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
	}
	.dl button:hover {
		background: #eef2f7;
		border-color: rgba(79, 70, 255, 0.25);
		color: var(--color-primary);
	}
</style>
