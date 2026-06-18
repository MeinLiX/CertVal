<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { workspaces as workspacesStore } from '$lib/stores/workspaces';
	import { language } from '$lib/stores/language.svelte';
	import { CertificateService, type CertificateFilter } from '$lib/services/CertificateService';
	import { WorkspaceService } from '$lib/services/WorkspaceService';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import CertificateCard from '$lib/components/certificates/CertificateCard.svelte';
	import FloatingActionBar from '$lib/components/layout/FloatingActionBar.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { Certificate, Workspace, BulkUploadResultDto } from '$lib/types';

	let certificates = $state<Certificate[]>([]);
	let workspaceList = $state<Workspace[]>([]);
	let totalCount = $state(0);
	let isInitialLoad = $state(true);
	let isRefreshing = $state(false);
	let isMounted = $state(false);

	let showUploadModal = $state(false);
	let showResultsModal = $state(false);
	let isUploading = $state(false);
	let uploadResults = $state<BulkUploadResultDto | null>(null);
	let uploadForm = $state({ workspaceId: '' });
	let uploadFiles = $state<FileList | null>(null);

	let activeResultTab = $state<'success' | 'skipped' | 'failure'>('success');
	let resultSearchQuery = $state('');

	let selectionMode = $state(false);
	let selectedIds = $state<Set<string>>(new Set());
	let bulkTag = $state('');
	let bulkBusy = $state(false);

	const filters = $derived({
		subjectTerm: page.url.searchParams.get('search') || '',
		status: page.url.searchParams.get('status') || 'All',
		workspaceId: page.url.searchParams.get('workspace') || '',
		page: parseInt(page.url.searchParams.get('page') || '1'),
		pageSize: parseInt(page.url.searchParams.get('pageSize') || '9')
	});

	const filteredUploadResults = $derived(
		!uploadResults?.results
			? []
			: uploadResults.results.filter((r) => {
					if (activeResultTab === 'success' && !r.success) return false;
					if (activeResultTab === 'skipped' && (!r.isSkipped || r.success)) return false;
					if (activeResultTab === 'failure' && (r.success || r.isSkipped)) return false;

					if (resultSearchQuery) {
						const q = resultSearchQuery.toLowerCase();
						return (
							r.fileName.toLowerCase().includes(q) ||
							(r.subject && r.subject.toLowerCase().includes(q)) ||
							(r.errorMessage && r.errorMessage.toLowerCase().includes(q))
						);
					}
					return true;
				})
	);

	const totalPages = $derived(Math.ceil(totalCount / filters.pageSize) || 1);

	onMount(async () => {
		if (!userSession.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadWorkspaces();
		isMounted = true;
	});

	$effect(() => {
		if (isMounted) {
			loadCertificates();
		}
	});

	async function loadWorkspaces() {
		try {
			const response = await WorkspaceService.getAll(1, 100);
			if (response.data) {
				workspaceList = response.data.items;
				workspacesStore.set(workspaceList);
			}
		} catch (error) {
			console.error('Failed to load workspaces:', error);
		}
	}

	async function loadCertificates() {
		if (isInitialLoad) {
		} else {
			isRefreshing = true;
		}
		try {
			const filter: CertificateFilter = {
				pageNumber: filters.page,
				pageSize: filters.pageSize,
				workspaceId: filters.workspaceId || undefined,
				subject: filters.subjectTerm || undefined,
				statusFilter: filters.status !== 'All' ? filters.status : undefined,
				sortBy: 'notAfter',
				sortDescending: false
			};

			const response = await CertificateService.getAll(filter);
			if (response.data) {
				certificates = response.data.items;
				totalCount = response.data.totalCount;
			}
		} catch (error) {
			console.error('Failed to load certificates:', error);
		} finally {
			isInitialLoad = false;
			isRefreshing = false;
		}
	}

	function updateParams(newParams: Record<string, string | number>) {
		const url = new URL(window.location.href);
		Object.entries(newParams).forEach(([key, value]) => {
			if (value) {
				url.searchParams.set(key, value.toString());
			} else {
				url.searchParams.delete(key);
			}
		});
		goto(url.toString(), { keepFocus: true, noScroll: true });
	}

	function handleSearch(e: Event) {
		const input = e.target as HTMLInputElement;
		updateParams({ search: input.value, page: 1 });
	}

	function handleWorkspaceChange(e: Event) {
		const select = e.target as HTMLSelectElement;
		updateParams({ workspace: select.value, page: 1 });
	}

	function handleStatusChange(e: Event) {
		const select = e.target as HTMLSelectElement;
		updateParams({ status: select.value, page: 1 });
	}

	function openUploadModal() {
		uploadFiles = null;
		uploadForm.workspaceId = filters.workspaceId || (workspaceList[0]?.id ?? '');
		showUploadModal = true;
	}

	function closeUploadModal() {
		showUploadModal = false;
		uploadFiles = null;
	}

	async function handleUpload() {
		if (!uploadFiles || uploadFiles.length === 0 || !uploadForm.workspaceId) return;

		isUploading = true;
		try {
			const response = await CertificateService.upload(uploadForm.workspaceId, uploadFiles);
			if (response.data) {
				uploadResults = response.data;
				closeUploadModal();
				showResultsModal = true;
				activeResultTab = 'success';
				await loadCertificates();
			}
		} catch (error) {
			console.error('Upload failed:', error);
		} finally {
			isUploading = false;
		}
	}

	function closeResultsModal() {
		showResultsModal = false;
		uploadResults = null;
	}

	function handleFileChange(e: Event) {
		const input = e.target as HTMLInputElement;
		uploadFiles = input.files;
	}

	function goToPage(pageNum: number) {
		updateParams({ page: pageNum });
	}

	function getWorkspaceName(workspaceId: string): string {
		const ws = workspaceList.find((w) => w.id === workspaceId);
		return ws?.name || '';
	}

	function toggleSelectionMode() {
		selectionMode = !selectionMode;
		if (!selectionMode) selectedIds = new Set();
	}

	function toggleSelected(id: string) {
		const next = new Set(selectedIds);
		if (next.has(id)) next.delete(id);
		else next.add(id);
		selectedIds = next;
	}

	function onCardClick(certificate: Certificate) {
		if (selectionMode) toggleSelected(certificate.id);
		else goto(`/certificates/${certificate.id}`);
	}

	async function runBulk(operation: number, tags?: string[]) {
		const selected = certificates.filter((c) => selectedIds.has(c.id));
		if (selected.length === 0) return;

		// The bulk endpoint is workspace-scoped, so group the selection by workspace.
		const byWorkspace = new Map<string, string[]>();
		for (const c of selected) {
			const list = byWorkspace.get(c.workspaceId) ?? [];
			list.push(c.id);
			byWorkspace.set(c.workspaceId, list);
		}

		bulkBusy = true;
		try {
			for (const [workspaceId, certificateIds] of byWorkspace) {
				await api.post('/certificates/bulk', { workspaceId, certificateIds, operation, tags });
			}
			selectedIds = new Set();
			bulkTag = '';
			await loadCertificates();
		} finally {
			bulkBusy = false;
		}
	}

	function bulkAddTag() {
		const tag = bulkTag.trim();
		if (tag) runBulk(4, [tag]);
	}

	function bulkRemoveTag() {
		const tag = bulkTag.trim();
		if (tag) runBulk(5, [tag]);
	}
</script>

<svelte:head>
	<title>{t('nav.certificates', language.current)} - CertVal</title>
</svelte:head>

<div class="page">
	<div class="filters">
		<div class="filters__search">
			<Input
				type="search"
				placeholder={t('certificates.searchPlaceholder', language.current)}
				value={filters.subjectTerm}
				onchange={handleSearch}
			/>
		</div>

		<div class="filters__selects">
			<select class="select" value={filters.workspaceId} onchange={handleWorkspaceChange}>
				<option value="">{t('certificates.allWorkspaces', language.current)}</option>
				{#each workspaceList as workspace}
					<option value={workspace.id}>{workspace.name}</option>
				{/each}
			</select>

			<select class="select" value={filters.status} onchange={handleStatusChange}>
				<option value="All">{t('certificates.allStatuses', language.current)}</option>
				<option value="Valid">{t('certificates.valid', language.current)}</option>
				<option value="Expiring">{t('certificates.expiring', language.current)}</option>
				<option value="Expired">{t('certificates.expired', language.current)}</option>
			</select>

			<Button variant="secondary" onclick={toggleSelectionMode}>
				{selectionMode ? t('certificates.bulkDone', language.current) : t('certificates.bulkSelect', language.current)}
			</Button>
		</div>
	</div>

	{#if selectionMode}
		<div class="bulkbar" data-test-id="bulk-bar">
			<span class="bulkbar__count">{selectedIds.size} {t('certificates.bulkSelected', language.current)}</span>
			<div class="bulkbar__actions">
				<Button variant="secondary" disabled={selectedIds.size === 0 || bulkBusy} onclick={() => runBulk(2)}>
					{t('certificates.bulkSkip', language.current)}
				</Button>
				<Button variant="secondary" disabled={selectedIds.size === 0 || bulkBusy} onclick={() => runBulk(3)}>
					{t('certificates.bulkUnskip', language.current)}
				</Button>
				<input
					class="bulkbar__tag"
					bind:value={bulkTag}
					placeholder={t('certificates.bulkTagPlaceholder', language.current)}
					maxlength="40"
				/>
				<Button variant="secondary" disabled={selectedIds.size === 0 || bulkBusy || !bulkTag.trim()} onclick={bulkAddTag}>
					{t('certificates.bulkAddTag', language.current)}
				</Button>
				<Button variant="secondary" disabled={selectedIds.size === 0 || bulkBusy || !bulkTag.trim()} onclick={bulkRemoveTag}>
					{t('certificates.bulkRemoveTag', language.current)}
				</Button>
				<Button variant="danger" disabled={selectedIds.size === 0 || bulkBusy} onclick={() => runBulk(1)}>
					{t('certificates.bulkDelete', language.current)}
				</Button>
			</div>
		</div>
	{/if}

	{#if isInitialLoad}
		<div class="cert-grid">
			{#each Array(6) as _}
				<div class="skeleton-card">
					<div class="skeleton skeleton--title"></div>
					<div class="skeleton skeleton--text"></div>
					<div class="skeleton skeleton--text skeleton--short"></div>
				</div>
			{/each}
		</div>
	{:else if certificates.length === 0}
		<div class="empty-state">
			<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
				<rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
				<path d="M7 11V7a5 5 0 0 1 10 0v4" />
			</svg>
			<h3 class="empty-state__title">{t('certificates.empty', language.current)}</h3>
			<p class="empty-state__text">
				{#if filters.subjectTerm || filters.workspaceId || filters.status !== 'All'}
					{t('certificates.emptyFiltered', language.current)}
				{:else}
					{t('certificates.emptyDescription', language.current)}
				{/if}
			</p>
			{#if !filters.subjectTerm && !filters.workspaceId && filters.status === 'All'}
				<Button onclick={openUploadModal}>{t('certificates.upload', language.current)}</Button>
			{/if}
		</div>
	{:else}
		<div class="cert-grid" class:cert-grid--refreshing={isRefreshing}>
			{#each certificates as certificate (certificate.id)}
				<CertificateCard
					{certificate}
					{selectionMode}
					selected={selectedIds.has(certificate.id)}
					workspaceName={getWorkspaceName(certificate.workspaceId)}
					onclick={() => onCardClick(certificate)}
				/>
			{/each}
		</div>

		{#if totalPages > 1}
			<div class="pagination">
				<button
					class="pagination__btn"
					disabled={filters.page <= 1 || isRefreshing}
					onclick={() => goToPage(filters.page - 1)}
				>
					←
				</button>
				<span class="pagination__info">
					{filters.page} / {totalPages}
				</span>
				<button
					class="pagination__btn"
					disabled={filters.page >= totalPages || isRefreshing}
					onclick={() => goToPage(filters.page + 1)}
				>
					→
				</button>
			</div>
		{/if}
	{/if}

	<FloatingActionBar label={t('nav.certificates', language.current)}>
		{#snippet trailing()}
			<Button onclick={openUploadModal} data-test-id="certificates-upload-button">
				<Icon name="upload" size="sm" />
				{t('certificates.upload', language.current)}
			</Button>
		{/snippet}
	</FloatingActionBar>
</div>

<Modal bind:isOpen={showUploadModal} title={t('certificates.upload', language.current)} onclose={closeUploadModal}>
	<form class="modal-form" onsubmit={(e) => { e.preventDefault(); handleUpload(); }}>
		<div class="form-group">
			<label class="form-label" for="workspace-select">{t('workspaces.workspace', language.current)}</label>
			<select id="workspace-select" class="select" bind:value={uploadForm.workspaceId} required>
				<option value="" disabled>{t('certificates.selectWorkspace', language.current)}</option>
				{#each workspaceList as workspace}
					<option value={workspace.id}>{workspace.name}</option>
				{/each}
			</select>
		</div>

		<div class="form-group">
			<label class="form-label" for="file-input">{t('certificates.files', language.current)}</label>
			<div class="file-input-wrapper">
				<input
					id="file-input"
					type="file"
					class="file-input"
					accept=".pem,.crt,.cer,.p12,.pfx"
					multiple
					onchange={handleFileChange}
				/>
				<div class="file-input-display">
					{#if uploadFiles && uploadFiles.length > 0}
						<span>{uploadFiles.length} {t('certificates.filesSelected', language.current)}</span>
					{:else}
						<span class="file-input-placeholder">{t('certificates.dropFiles', language.current)}</span>
					{/if}
				</div>
			</div>
			<p class="form-hint">{t('certificates.supportedFormats', language.current)}: .pem, .crt, .cer, .p12, .pfx</p>
		</div>

		<div class="modal-form__actions">
			<Button variant="secondary" onclick={closeUploadModal}>{t('common.cancel', language.current)}</Button>
			<Button
				type="submit"
				disabled={isUploading || !uploadFiles || uploadFiles.length === 0 || !uploadForm.workspaceId}
			>
				{isUploading ? t('common.loading', language.current) : t('common.upload', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<Modal bind:isOpen={showResultsModal} title={t('certificates.uploadResults', language.current)} onclose={closeResultsModal}>
	{#if uploadResults}
		<div class="results">
			<div class="results__tabs">
				<button
					class="results__tab"
					class:results__tab--active={activeResultTab === 'success'}
					onclick={() => (activeResultTab = 'success')}
				>
					<span class="results__tab-badge results__tab-badge--success">
						{uploadResults.results.filter(r => r.success && !r.isSkipped).length}
					</span>
					{t('certificates.success', language.current)}
				</button>
				<button
					class="results__tab"
					class:results__tab--active={activeResultTab === 'skipped'}
					onclick={() => (activeResultTab = 'skipped')}
				>
					<span class="results__tab-badge results__tab-badge--warning">
						{uploadResults.results.filter(r => r.isSkipped).length}
					</span>
					{t('certificates.skipped', language.current)}
				</button>
				<button
					class="results__tab"
					class:results__tab--active={activeResultTab === 'failure'}
					onclick={() => (activeResultTab = 'failure')}
				>
					<span class="results__tab-badge results__tab-badge--error">
						{uploadResults.results.filter(r => !r.success && !r.isSkipped).length}
					</span>
					{t('certificates.failed', language.current)}
				</button>
			</div>

			<div class="results__content">
				{#if filteredUploadResults.length > 0}
					<ul class="results__list">
						{#each filteredUploadResults as item}
							<li class="results__item results__item--{activeResultTab === 'success' ? 'success' : activeResultTab === 'skipped' ? 'warning' : 'error'}">
								<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									{#if activeResultTab === 'success'}
										<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
										<polyline points="22 4 12 14.01 9 11.01" />
									{:else}
										<circle cx="12" cy="12" r="10" />
										<line x1="12" y1="8" x2="12" y2="12" />
										<line x1="12" y1="16" x2="12.01" y2="16" />
									{/if}
								</svg>
								<div>
									<span>{item.fileName}</span>
									{#if item.errorMessage}
										<small class="results__reason">{item.errorMessage}</small>
									{/if}
								</div>
							</li>
						{/each}
					</ul>
				{:else}
					<p class="results__empty">{t('certificates.noResults', language.current)}</p>
				{/if}
			</div>
		</div>

		<div class="modal-form__actions">
			<Button onclick={closeResultsModal}>{t('common.close', language.current)}</Button>
		</div>
	{/if}
</Modal>

<style>
	.page {
		animation: fadeIn 0.5s ease-out;
	}

	@keyframes fadeIn {
		from { opacity: 0; }
		to { opacity: 1; }
	}

	.bulkbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--space-3);
		flex-wrap: wrap;
		padding: var(--space-3) var(--space-4);
		margin-bottom: var(--space-4);
		border: 1px solid var(--color-primary);
		border-radius: var(--radius-md);
		background: var(--color-primary-light);
	}
	.bulkbar__count {
		font-weight: var(--font-semibold);
		color: var(--color-primary);
		white-space: nowrap;
	}
	.bulkbar__actions {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		flex-wrap: wrap;
	}
	.bulkbar__tag {
		padding: var(--space-2) var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface);
		color: var(--color-text);
		font: inherit;
		width: 160px;
	}

	.filters {
		display: flex;
		gap: var(--space-4);
		margin-bottom: var(--space-6);
		flex-wrap: wrap;
	}

	.filters__search {
		flex: 1;
		min-width: 200px;
	}

	.filters__selects {
		display: flex;
		gap: var(--space-3);
	}

	.select {
		padding: var(--space-2) var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface);
		color: var(--color-text);
		font-size: var(--text-sm);
		min-width: 150px;
		cursor: pointer;
		transition: border-color 0.15s ease;
	}

	.select:hover {
		border-color: var(--color-border-hover);
	}

	.select:focus {
		outline: none;
		border-color: var(--color-primary);
		box-shadow: 0 0 0 3px var(--color-primary-alpha);
	}

	.cert-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
		gap: var(--space-4);
		transition: opacity var(--transition-fast);
	}

	.cert-grid--refreshing {
		opacity: 0.55;
		pointer-events: none;
	}

	.skeleton-card {
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		padding: var(--space-5);
	}

	.skeleton {
		background: linear-gradient(90deg, var(--color-surface-elevated) 25%, var(--color-border) 50%, var(--color-surface-elevated) 75%);
		background-size: 200% 100%;
		animation: shimmer 1.5s infinite;
		border-radius: var(--radius-sm);
	}

	.skeleton--title {
		height: 24px;
		width: 70%;
		margin-bottom: var(--space-3);
	}

	.skeleton--text {
		height: 16px;
		width: 100%;
		margin-bottom: var(--space-2);
	}

	.skeleton--short {
		width: 50%;
	}

	@keyframes shimmer {
		0% { background-position: 200% 0; }
		100% { background-position: -200% 0; }
	}

	.empty-state {
		text-align: center;
		padding: var(--space-12) var(--space-6);
		color: var(--color-text-muted);
	}

	.empty-state svg {
		opacity: 0.4;
		margin-bottom: var(--space-4);
	}

	.empty-state__title {
		font-size: var(--text-lg);
		font-weight: 600;
		color: var(--color-text);
		margin: 0 0 var(--space-2);
	}

	.empty-state__text {
		margin: 0 0 var(--space-4);
	}

	.pagination {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: var(--space-3);
		margin-top: var(--space-6);
	}

	.pagination__btn {
		padding: var(--space-2) var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface);
		color: var(--color-text);
		cursor: pointer;
		transition: all 0.15s ease;
	}

	.pagination__btn:hover:not(:disabled) {
		border-color: var(--color-primary);
		color: var(--color-primary);
	}

	.pagination__btn:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.pagination__info {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
	}

	.modal-form {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.form-group {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.form-label {
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text);
	}

	.form-hint {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
		margin: 0;
	}

	.file-input-wrapper {
		position: relative;
	}

	.file-input {
		position: absolute;
		inset: 0;
		opacity: 0;
		cursor: pointer;
	}

	.file-input-display {
		padding: var(--space-4);
		border: 2px dashed var(--color-border);
		border-radius: var(--radius-md);
		text-align: center;
		background: var(--color-surface);
		transition: border-color 0.15s ease, background-color 0.15s ease;
	}

	.file-input-wrapper:hover .file-input-display,
	.file-input:focus + .file-input-display {
		border-color: var(--color-primary);
		background: var(--color-surface-elevated);
	}

	.file-input-placeholder {
		color: var(--color-text-muted);
	}

	.modal-form__actions {
		display: flex;
		justify-content: flex-end;
		gap: var(--space-3);
		padding-top: var(--space-4);
		border-top: 1px solid var(--color-border);
		margin-top: var(--space-2);
	}

	.results {
		margin-bottom: var(--space-4);
	}

	.results__tabs {
		display: flex;
		border-bottom: 1px solid var(--color-border);
		margin-bottom: var(--space-4);
	}

	.results__tab {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-3) var(--space-4);
		background: none;
		border: none;
		border-bottom: 2px solid transparent;
		color: var(--color-text-muted);
		font-size: var(--text-sm);
		cursor: pointer;
		transition: color 0.15s ease, border-color 0.15s ease;
	}

	.results__tab:hover {
		color: var(--color-text);
	}

	.results__tab--active {
		color: var(--color-text);
		border-bottom-color: var(--color-primary);
	}

	.results__tab-badge {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		min-width: 20px;
		height: 20px;
		padding: 0 var(--space-1);
		border-radius: var(--radius-full);
		font-size: var(--text-xs);
		font-weight: 600;
	}

	.results__tab-badge--success {
		background: var(--color-success-bg);
		color: var(--color-success);
	}

	.results__tab-badge--warning {
		background: var(--color-warning-bg);
		color: var(--color-warning);
	}

	.results__tab-badge--error {
		background: var(--color-error-bg);
		color: var(--color-error);
	}

	.results__content {
		max-height: 300px;
		overflow-y: auto;
	}

	.results__list {
		list-style: none;
		padding: 0;
		margin: 0;
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.results__item {
		display: flex;
		align-items: flex-start;
		gap: var(--space-2);
		padding: var(--space-2) var(--space-3);
		border-radius: var(--radius-sm);
		font-size: var(--text-sm);
	}

	.results__item svg {
		flex-shrink: 0;
		margin-top: 2px;
	}

	.results__item--success {
		background: var(--color-success-bg);
		color: var(--color-success);
	}

	.results__item--warning {
		background: var(--color-warning-bg);
		color: var(--color-warning);
	}

	.results__item--error {
		background: var(--color-error-bg);
		color: var(--color-error);
	}

	.results__reason {
		display: block;
		opacity: 0.8;
		font-size: var(--text-xs);
		margin-top: var(--space-1);
	}

	.results__empty {
		text-align: center;
		color: var(--color-text-muted);
		padding: var(--space-6);
	}

	@media (max-width: 768px) {
		.page {
			padding: var(--space-4);
		}

		.filters {
			flex-direction: column;
		}

		.filters__selects {
			flex-direction: column;
		}

		.select {
			width: 100%;
		}

		.cert-grid {
			grid-template-columns: 1fr;
		}
	}
</style>
