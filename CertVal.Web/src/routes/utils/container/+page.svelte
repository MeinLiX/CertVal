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
		type EUVerifyResult
	} from '$lib/iit/verifyClient';
	import { formatBytes } from '$lib/utils/fileDownload';


	let sigFiles = $state<File[]>([]);
	let dataFiles = $state<File[]>([]); // optional original document for external/detached sigs
	let initializing = $state(false);
	let initialized = $state(false);
	let initError = $state<string | null>(null);
	let busy = $state(false);
	let error = $state<string | null>(null);

	/** Flat list item for tree rendering */
	type FlatItem = {
		id: string;
		depth: number;
		label: string;
		value?: string;
		kind: 'section' | 'leaf' | 'badge-ok' | 'badge-warn';
		hasChildren: boolean;
	};

	let rawJson = $state<string | null>(null);
	let showRaw = $state(false);

	// collapsed section IDs
	let collapsed = $state<Set<string>>(new Set());

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

	type TreeNode = {
		label: string;
		value?: string;
		kind?: 'section' | 'leaf' | 'badge-ok' | 'badge-warn';
		children?: TreeNode[];
	};

	function friendlyLabel(key: string): string {
		const tk = `utils.container.fieldLabels.${key}`;
		const r = t(tk, language.current);
		return r === tk ? key : r;
	}

	/** Recursively convert any value to a TreeNode — shows 100% of available data */
	function anyToNode(val: unknown, key: string): TreeNode {
		const label = friendlyLabel(key);
		if (val === null || val === undefined) {
			return { label, value: '—', kind: 'leaf' };
		}
		if (val instanceof Uint8Array || (ArrayBuffer.isView(val) && !(val instanceof DataView))) {
			return { label, value: `<binary, ${(val as Uint8Array).byteLength} bytes>`, kind: 'leaf' };
		}
		if (val instanceof ArrayBuffer) {
			return { label, value: `<binary, ${val.byteLength} bytes>`, kind: 'leaf' };
		}
		if (Array.isArray(val)) {
			if (val.length === 0) return { label, value: t('utils.container.tree.empty', language.current), kind: 'leaf' };
			return { label, kind: 'section', children: val.map((item, i) => anyToNode(item, `[${i}]`)) };
		}
		if (typeof val === 'boolean') {
			return { label, value: val ? (t('common.yes', language.current)) : (t('common.no', language.current)), kind: val ? 'badge-ok' : 'leaf' };
		}
		if (typeof val === 'object') {
			const entries = Object.entries(val as Record<string, unknown>).filter(([, v]) => v !== null && v !== undefined);
			if (entries.length === 0) return { label, value: '{ }', kind: 'leaf' };
			return { label, kind: 'section', children: entries.map(([k, v]) => anyToNode(v, k)) };
		}
		const s = String(val);
		return { label, value: s === '' ? '—' : s, kind: 'leaf' };
	}

	function buildTree(res: EUVerifyResult, fileName: string, fileSize: number): TreeNode[] {
		const signs = flattenSignsInfos(res.signsInfos);

		const containerChildren: TreeNode[] = [
			{ label: t('utils.container.tree.file', language.current), value: fileName, kind: 'leaf' },
			{ label: t('utils.container.tree.size', language.current), value: formatBytes(fileSize), kind: 'leaf' },
			{ label: t('utils.container.tree.signType', language.current), value: String(res.signType), kind: 'leaf' },
			{ label: t('utils.container.tree.resultCode', language.current), value: String(res.resultCode), kind: 'leaf' },
			{ label: t('utils.container.tree.signerCount', language.current), value: String(signs.length), kind: 'leaf' }
		];

		const noSignersHint = t('utils.container.tree.noSigners', language.current);

		const signerNodes: TreeNode[] = signs.length > 0
			? signs.map((s, i) => ({
				label: `${t('utils.container.tree.signer', language.current)} #${i + 1}`,
				value: (s.signerInfo?.subjFullName ?? s.signerInfo?.subjCN) as string | undefined,
				kind: 'section' as const,
				children: Object.entries(s)
					.filter(([, v]) => v !== null && v !== undefined)
					.map(([k, v]) => anyToNode(v, k))
			}))
			: [{ label: noSignersHint, kind: 'leaf' as const }];

		const dataNodes: TreeNode[] = [];
		if (res.data) {
			const isPDF = res.data[0] === 0x25 && res.data[1] === 0x50;
			dataNodes.push({
				label: t('utils.container.tree.signedData', language.current),
				value: `${formatBytes(res.data.byteLength)}${isPDF ? ' (PDF)' : ''}`,
				kind: 'leaf'
			});
		}
		for (const d of res.datas ?? []) {
			dataNodes.push({ label: d.name, value: formatBytes(d.val.byteLength), kind: 'leaf' });
		}

		const nodes: TreeNode[] = [
			{ label: t('utils.container.tree.container', language.current), kind: 'section', children: containerChildren },
			{ label: t('utils.container.tree.signers', language.current), kind: 'section', children: signerNodes }
		];
		if (dataNodes.length > 0) {
			nodes.push({ label: t('utils.container.tree.embeddedData', language.current), kind: 'section', children: dataNodes });
		}
		return nodes;
	}

	/** Flatten tree into a depth-stamped list, tracking parent IDs for collapse */
	function flatten(nodes: TreeNode[], depth = 0, parentId = ''): FlatItem[] {
		const items: FlatItem[] = [];
		for (let i = 0; i < nodes.length; i++) {
			const n = nodes[i];
			const id = `${parentId}/${i}`;
			const isCollapsed = collapsed.has(id);
			items.push({
				id,
				depth,
				label: n.label,
				value: n.value,
				kind: n.kind ?? 'leaf',
				hasChildren: (n.children?.length ?? 0) > 0
			});
			if (n.children && !isCollapsed) {
				items.push(...flatten(n.children, depth + 1, id));
			}
		}
		return items;
	}

	// Derived flat list (reactively rebuilt whenever collapsed changes)
	let treeData = $state<TreeNode[] | null>(null);
	const visibleItems = $derived(treeData ? flatten(treeData) : []);

	function toggleCollapse(id: string) {
		const s = new Set(collapsed);
		if (s.has(id)) s.delete(id); else s.add(id);
		collapsed = s;
	}

	function collapseAll() {
		if (!treeData) return;
		const ids: string[] = [];
		const collect = (nodes: TreeNode[], prefix: string) => {
			nodes.forEach((n, i) => {
				const id = `${prefix}/${i}`;
				if (n.children?.length) {
					ids.push(id);
					collect(n.children, id);
				}
			});
		};
		collect(treeData, '');
		collapsed = new Set(ids);
	}

	function expandAll() {
		collapsed = new Set();
	}

	async function analyze() {
		if (!initialized || busy || sigFiles.length === 0) return;
		error = null;
		treeData = null;
		rawJson = null;
		collapsed = new Set();
		busy = true;
		const sigFile = sigFiles[0];
		const filesToVerify: File[] = [sigFile, ...(dataFiles.length > 0 ? [dataFiles[0]] : [])];
		try {
			const res = await client.verifyFiles(filesToVerify);
			treeData = buildTree(res, sigFile.name, sigFile.size);
			rawJson = JSON.stringify(res, (_k, v) => {
				if (v instanceof Uint8Array) return `<binary ${v.byteLength} bytes>`;
				if (v instanceof File) return { name: v.name, size: v.size };
				return v;
			}, 2);
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			busy = false;
		}
	}
</script>

<svelte:head>
	<title>{t('utils.container.treeTitle', language.current)} · CertVal</title>
</svelte:head>

<section class="utils-container">
	<UtilsSubnav description={t('utils.container.description', language.current)} />

	<div class="privacy-note"><p>{t('utils.privacyNote', language.current)}</p></div>

	<InitToast loading={initializing} error={initError} />

	<div class="form">
		<FileDropZone
			bind:files={sigFiles}
			multiple={false}
			label={t('utils.container.dropLabel', language.current)}
			hint={t('utils.container.dropHint', language.current)}
		/>
		<details class="data-slot">
			<summary>{t('utils.container.originalDetails', language.current)}</summary>
			<div class="data-slot-inner">
				<FileDropZone
					bind:files={dataFiles}
					multiple={false}
					label={t('utils.container.originalDrop', language.current)}
					hint={t('utils.container.originalHint', language.current)}
				/>
			</div>
		</details>
		<div class="actions">
			<button class="primary" type="button" disabled={!initialized || busy || sigFiles.length === 0} onclick={analyze}>
				{busy ? '…' : (t('utils.container.analyze', language.current))}
			</button>
		</div>
		{#if error}<div class="status error">{error}</div>{/if}
	</div>

	{#if treeData}
		<div class="toolbar">
			<span class="toolbar-title">{t('utils.container.treeTitle', language.current)}</span>
			<div class="toolbar-btns">
				<button type="button" class="btn-sm" onclick={expandAll}>{t('utils.container.expandAll', language.current)}</button>
				<button type="button" class="btn-sm" onclick={collapseAll}>{t('utils.container.collapseAll', language.current)}</button>
				<button type="button" class="btn-sm" class:active={showRaw} onclick={() => (showRaw = !showRaw)}>
					{t('utils.container.rawJson', language.current)}
				</button>
			</div>
		</div>

		{#if showRaw}
			<pre class="raw-json">{rawJson}</pre>
		{:else}
			<div class="tree-root">
				{#each visibleItems as item (item.id)}
					{@const indentPx = item.depth * 20}
					{#if item.hasChildren}
						<button
							type="button"
							class="tree-section"
							onclick={() => toggleCollapse(item.id)}
							style="padding-left: {12 + indentPx}px"
						>
							<span class="chevron">{collapsed.has(item.id) ? '▶' : '▼'}</span>
							<span class="section-label">{item.label}</span>
							{#if item.value}<span class="section-sub">{item.value}</span>{/if}
						</button>
					{:else}
						<div class="tree-leaf" style="padding-left: {28 + indentPx}px">
							<span class="leaf-label">{item.label}</span>
							{#if item.value}
								<span class="leaf-val {item.kind === 'badge-ok' ? 'ok' : item.kind === 'badge-warn' ? 'warn' : ''}">{item.value}</span>
							{/if}
						</div>
					{/if}
				{/each}
			</div>
		{/if}
	{/if}
</section>

<style>
	.utils-container {
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
	.status { padding: 0.5rem 0.75rem; border-radius: 8px; font-size: 0.9rem; }
	.status.info { background: var(--color-primary-light); color: var(--color-primary); }
	.status.error { background: var(--color-error-light); color: var(--color-error); }
	.form { display: flex; flex-direction: column; gap: 0.75rem; }
	.data-slot > summary {
		cursor: pointer;
		font-size: 0.875rem;
		color: var(--color-primary, #4f46ff);
		list-style: none;
		padding: 0.25rem 0;
		user-select: none;
	}
	.data-slot > summary::-webkit-details-marker { display: none; }
	.data-slot-inner { padding-top: 0.5rem; }
	.actions { display: flex; justify-content: center; width: 100%; margin: 0.5rem auto 0; }
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
		align-self: center; /* Centered primary action button */
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
	/* Toolbar */
	.toolbar {
		display: flex;
		justify-content: space-between;
		align-items: center;
		flex-wrap: wrap;
		gap: 0.5rem;
		margin-top: 1rem;
	}
	.toolbar-title { font-weight: 600; font-size: 1rem; }
	.toolbar-btns { display: flex; gap: 0.4rem; flex-wrap: wrap; }
	.btn-sm {
		padding: 0.3rem 0.75rem;
		border-radius: 6px;
		border: 1px solid var(--color-border);
		background: var(--color-surface-inset);
		color: var(--color-text);
		cursor: pointer;
		font-size: 0.82rem;
	}
	.btn-sm:hover { background: var(--color-surface-hover); }
	.btn-sm.active { background: var(--color-primary-light); border-color: var(--color-primary); color: var(--color-primary); }
	/* Tree */
	.tree-root {
		border: 1px solid var(--color-border);
		border-radius: 10px;
		overflow: hidden;
		background: var(--color-surface);
	}
	.tree-section {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		width: 100%;
		text-align: left;
		padding-top: 0.45rem;
		padding-right: 0.75rem;
		padding-bottom: 0.45rem;
		border: 0;
		border-bottom: 1px solid var(--color-border);
		background: var(--color-surface-inset);
		cursor: pointer;
		font-size: 0.9rem;
		font-weight: 600;
		color: var(--color-primary);
		transition: background 0.1s;
	}
	.tree-section:hover { background: var(--color-surface-hover); }
	.chevron { font-size: 0.6rem; color: var(--color-text-muted); flex-shrink: 0; width: 10px; }
	.section-label { flex-shrink: 0; }
	.section-sub {
		font-size: 0.82rem;
		font-weight: 400;
		color: var(--color-text-muted);
		flex: 1;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.tree-leaf {
		display: flex;
		align-items: baseline;
		gap: 0.75rem;
		padding-top: 0.3rem;
		padding-right: 0.75rem;
		padding-bottom: 0.3rem;
		border-bottom: 1px solid var(--color-border);
		font-size: 0.875rem;
	}
	.tree-leaf:last-child { border-bottom: 0; }
	.leaf-label { color: var(--color-text-muted); white-space: nowrap; flex-shrink: 0; min-width: 140px; }
	.leaf-val { flex: 1; word-break: break-word; }
	.leaf-val.ok { color: var(--color-success); font-weight: 600; }
	.leaf-val.warn { color: var(--color-warning); font-weight: 600; }
	/* Raw JSON */
	.raw-json {
		background: var(--color-surface-inset);
		border: 1px solid var(--color-border);
		border-radius: 10px;
		padding: 1rem;
		font-family: monospace;
		font-size: 0.78rem;
		overflow: auto;
		max-height: 600px;
		white-space: pre;
		color: var(--color-text);
	}
</style>

