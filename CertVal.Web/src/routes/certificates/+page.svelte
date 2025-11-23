<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { workspaces as workspacesStore } from '$lib/stores/workspaces';
	import { language } from '$lib/stores/language.svelte';
	import { CertificateService, type CertificateFilter } from '$lib/services/CertificateService';
	import { WorkspaceService } from '$lib/services/WorkspaceService';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import CertificateCard from '$lib/components/certificates/CertificateCard.svelte';
	import Pagination from '$lib/components/ui/Pagination.svelte';
	import type { Certificate, Workspace, BulkUploadResultDto } from '$lib/types';

	let certificates = $state<Certificate[]>([]);
	let workspaceList = $state<Workspace[]>([]);
	let totalCount = $state(0);
	let isLoading = $state(true);
	let isMounted = $state(false);

	let showUploadModal = $state(false);
	let showResultsModal = $state(false);
	let isUploading = $state(false);
	let uploadResults = $state<BulkUploadResultDto | null>(null);
	let errors = $state<Record<string, string>>({});
	let uploadForm = $state({
		workspaceId: ''
	});
	let uploadFiles = $state<FileList | null>(null);

	let activeResultTab = $state<'success' | 'skipped' | 'failure'>('success');
	let resultSearchQuery = $state('');

	const filters = $derived({
		subjectTerm: page.url.searchParams.get('search') || '',
		issuerTerm: page.url.searchParams.get('issuer') || '',
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
	const workspaceOptions = $derived([
		{ value: '', label: t('certificates.allWorkspaces', language.current) },
		...workspaceList.map((w) => ({ value: w.id, label: w.name }))
	]);

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

			const action = page.url.searchParams.get('action');
			if (action === 'upload') {
				if (filters.workspaceId) {
					uploadForm.workspaceId = filters.workspaceId;
				}
				showUploadModal = true;

				const newUrl = new URL(window.location.href);
				newUrl.searchParams.delete('action');
				goto(newUrl.toString(), { replaceState: true, keepFocus: true, noScroll: true });
			}
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
		isLoading = true;
		try {
			const filter: CertificateFilter = {
				pageNumber: filters.page,
				pageSize: filters.pageSize,
				workspaceId: filters.workspaceId || undefined,
				subject: filters.subjectTerm || undefined,
				issuer: filters.issuerTerm || undefined,
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
			isLoading = false;
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

	async function handleUpload(event: Event) {
		event.preventDefault();
		if (!uploadForm.workspaceId || !uploadFiles || uploadFiles.length === 0) {
			errors.general = t('certificates.upload.required', language.current);
			return;
		}

		isUploading = true;
		errors = {};

		try {
			const response = await CertificateService.upload(uploadForm.workspaceId, uploadFiles);
			if (response.data) {
				uploadResults = response.data;
				showUploadModal = false;
				showResultsModal = true;
				activeResultTab = 'success';
				resultSearchQuery = '';

				if (uploadResults.successCount > 0 && uploadForm.workspaceId !== filters.workspaceId) {
					updateParams({ workspace: uploadForm.workspaceId, page: 1 });
				} else {
					loadCertificates();
				}
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', language.current);
		} finally {
			isUploading = false;
		}
	}

	function handleFileChange(event: Event) {
		const input = event.target as HTMLInputElement;
		if (input.files) {
			uploadFiles = input.files;
		}
	}
</script>

<svelte:head>
	<title>{t('certificates.title', language.current)} - CertVal</title>
</svelte:head>

<div
	class="animate-in fade-in slide-in-from-bottom-4 flex h-[calc(100vh-8rem)] min-h-[600px] flex-col space-y-2 duration-500"
	data-test-id="certificates-page"
>
	<div class="flex-none">
		<div class="mb-4 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
			<div>
				<h1
					class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-4xl font-bold tracking-tight text-transparent"
				>
					{t('certificates.title', language.current)}
				</h1>
				<p class="text-base-content/60 mt-2 text-lg font-light">
					{t('certificates.subtitle', language.current)}
				</p>
			</div>
			<Button
				variant="primary"
				size="md"
				class="shadow-primary/20 hover:shadow-primary/40 whitespace-nowrap shadow-lg transition-all"
				onclick={() => {
					if (filters.workspaceId) uploadForm.workspaceId = filters.workspaceId;
					showUploadModal = true;
				}}
				data-test-id="upload-certificate-button"
			>
				<Icon name="upload" class="mr-2 h-5 w-5" />
				{t('certificates.upload', language.current)}
			</Button>
		</div>

		<div
			class="bg-base-100/50 border-base-content/5 rounded-2xl border p-4 shadow-sm backdrop-blur-sm"
		>
			<div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
				<Input
					placeholder={t('certificates.searchPlaceholder', language.current)}
					value={filters.subjectTerm}
					oninput={(e) => updateParams({ search: (e.target as HTMLInputElement).value, page: 1 })}
					icon="search"
					class="bg-base-100"
					data-test-id="search-certificate-input"
				/>
				<Input
					placeholder={t('certificates.issuerPlaceholder', language.current)}
					value={filters.issuerTerm}
					oninput={(e) => updateParams({ issuer: (e.target as HTMLInputElement).value, page: 1 })}
					class="bg-base-100"
					data-test-id="search-issuer-input"
				/>
				<Select
					options={workspaceOptions}
					value={filters.workspaceId}
					onchange={(e) =>
						updateParams({ workspace: (e.target as HTMLSelectElement).value, page: 1 })}
					class="bg-base-100"
					icon="workspaces"
					data-test-id="filter-workspace-select"
				/>
				<Select
					options={[
						{ value: 'All', label: t('common.allStatus', language.current) },
						{ value: 'Valid', label: t('certificates.valid', language.current) },
						{ value: 'Expiring', label: t('certificates.expiring', language.current) },
						{ value: 'Expired', label: t('certificates.expired', language.current) }
					]}
					value={filters.status}
					onchange={(e) => updateParams({ status: (e.target as HTMLSelectElement).value, page: 1 })}
					class="bg-base-100"
					icon="filter"
					data-test-id="filter-status-select"
				/>
			</div>
		</div>
	</div>

	{#if isLoading}
		<div class="grid flex-grow grid-cols-1 gap-6 overflow-y-auto p-1 md:grid-cols-2 lg:grid-cols-3">
			{#each Array(6) as _}
				<div class="card bg-base-100 h-44 animate-pulse shadow-xl">
					<div class="card-body">
						<div class="bg-base-300 mb-4 h-6 w-1/2 rounded"></div>
						<div class="bg-base-300 h-4 w-3/4 rounded"></div>
						<div class="bg-base-300 mt-2 h-4 w-1/2 rounded"></div>
					</div>
				</div>
			{/each}
		</div>
	{:else if certificates.length === 0}
		<div
			class="bg-base-100/50 border-base-200 flex flex-grow flex-col items-center justify-center rounded-3xl border py-20 text-center backdrop-blur-sm"
		>
			<div class="bg-base-200 mb-6 rounded-full p-6">
				<Icon name="certificates" class="text-base-content/30 h-12 w-12" />
			</div>
			<h3 class="mb-2 text-lg font-bold">{t('certificates.empty.title', language.current)}</h3>
			<p class="text-base-content/60 mb-8 max-w-md">
				{t('certificates.empty.description', language.current)}
			</p>
			<Button
				variant="outline"
				onclick={() => {
					if (filters.workspaceId) uploadForm.workspaceId = filters.workspaceId;
					showUploadModal = true;
				}}
				data-test-id="empty-state-upload-button"
			>
				{t('certificates.upload', language.current)}
			</Button>
		</div>
	{:else}
		<div class="min-h-0 flex-grow overflow-y-auto p-1 pb-4">
			<div class="grid grid-cols-1 content-start gap-4 md:grid-cols-2 lg:grid-cols-3">
				{#each certificates as cert (cert.id)}
					<CertificateCard
						certificate={cert}
						workspaceName={workspaceList.find((w) => w.id === cert.workspaceId)?.name}
					/>
				{/each}
			</div>
		</div>
		<div class="flex flex-none justify-center pb-0">
			<Pagination
				currentPage={filters.page}
				{totalPages}
				onPageChange={(p) => updateParams({ page: p })}
				data-test-id="certificates-pagination"
			/>
		</div>
	{/if}
</div>

<Modal
	isOpen={showUploadModal}
	title={t('certificates.upload', language.current)}
	onClose={() => (showUploadModal = false)}
	data-test-id="upload-certificate-modal"
>
	<form onsubmit={handleUpload} class="space-y-6">
		<div class="form-control w-full">
			<label class="label" for="workspace-select">
				<span class="label-text font-medium"
					>{t('certificates.selectWorkspace', language.current)}</span
				>
			</label>
			<select
				id="workspace-select"
				class="select select-bordered focus:select-primary w-full transition-all"
				bind:value={uploadForm.workspaceId}
				required
				data-test-id="upload-workspace-select"
			>
				<option value="" disabled selected>{t('common.select', language.current)}</option>
				{#each workspaceList as workspace}
					<option value={workspace.id}>{workspace.name}</option>
				{/each}
			</select>
		</div>

		<div class="form-control w-full">
			<label class="label" for="file-upload">
				<span class="label-text font-medium">{t('certificates.selectFiles', language.current)}</span
				>
			</label>
			<input
				id="file-upload"
				type="file"
				class="file-input file-input-bordered file-input-primary bg-base-100/50 w-full transition-all"
				multiple
				accept=".cer,.crt,.pem,.pfx,.p12"
				onchange={handleFileChange}
				required
				data-test-id="upload-file-input"
			/>
			<div class="label">
				<span class="label-text-alt text-base-content/60"
					>{t('certificates.supportedFormats', language.current)}: .cer, .crt, .pem, .pfx, .p12</span
				>
			</div>
		</div>

		{#if errors.general}
			<div class="alert alert-error bg-error/10 border-error/20 text-sm">
				<Icon name="error" class="text-error h-5 w-5" />
				<span>{errors.general}</span>
			</div>
		{/if}

		<div class="modal-action">
			<Button
				type="button"
				variant="ghost"
				onclick={() => (showUploadModal = false)}
				disabled={isUploading}
				data-test-id="upload-cancel-button"
			>
				{t('common.cancel', language.current)}
			</Button>
			<Button
				type="submit"
				variant="primary"
				loading={isUploading}
				class="shadow-primary/20 shadow-lg"
				data-test-id="upload-submit-button"
			>
				<Icon name="upload" class="mr-2 h-4 w-4" />
				{t('common.upload', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<Modal
	isOpen={showResultsModal}
	title={t('certificates.uploadResults', language.current)}
	onClose={() => (showResultsModal = false)}
	data-test-id="upload-results-modal"
>
	{#if uploadResults}
		<div class="space-y-4">
			<div class="tabs tabs-boxed bg-base-200 rounded-xl p-1.5">
				<button
					class="tab flex-1 gap-2 rounded-lg transition-all duration-200 {activeResultTab ===
					'success'
						? 'tab-active !bg-success !text-success-content shadow-sm'
						: 'hover:bg-base-300/50'}"
					onclick={() => (activeResultTab = 'success')}
					data-test-id="upload-results-tab-success"
				>
					{t('common.success', language.current)}
					<div
						class="badge badge-sm {activeResultTab === 'success' ? 'badge-ghost' : 'badge-success'}"
					>
						{uploadResults.successCount}
					</div>
				</button>
				<button
					class="tab flex-1 gap-2 rounded-lg transition-all duration-200 {activeResultTab ===
					'skipped'
						? 'tab-active !bg-warning !text-warning-content shadow-sm'
						: 'hover:bg-base-300/50'}"
					onclick={() => (activeResultTab = 'skipped')}
					data-test-id="upload-results-tab-skipped"
				>
					{t('certificates.skipped', language.current)}
					<div
						class="badge badge-sm {activeResultTab === 'skipped' ? 'badge-ghost' : 'badge-warning'}"
					>
						{uploadResults.skippedCount}
					</div>
				</button>
				<button
					class="tab flex-1 gap-2 rounded-lg transition-all duration-200 {activeResultTab ===
					'failure'
						? 'tab-active !bg-error !text-error-content shadow-sm'
						: 'hover:bg-base-300/50'}"
					onclick={() => (activeResultTab = 'failure')}
					data-test-id="upload-results-tab-failure"
				>
					{t('common.failed', language.current)}
					<div
						class="badge badge-sm {activeResultTab === 'failure' ? 'badge-ghost' : 'badge-error'}"
					>
						{uploadResults.failureCount}
					</div>
				</button>
			</div>

			<div class="form-control">
				<Input
					placeholder={t('certificates.searchByFileName', language.current)}
					bind:value={resultSearchQuery}
					icon="search"
					class="bg-base-100 rounded-lg"
					data-test-id="upload-results-search-input"
				/>
			</div>

			<div class="divider my-2 text-sm font-medium opacity-70">
				{t('certificates.details', language.current)}
				{#if activeResultTab === 'success'}
					<span class="text-success">({t('common.success', language.current)})</span>
				{:else if activeResultTab === 'skipped'}
					<span class="text-warning">({t('certificates.skipped', language.current)})</span>
				{:else}
					<span class="text-error">({t('common.failed', language.current)})</span>
				{/if}
			</div>

			<div class="max-h-60 min-h-[100px] space-y-2 overflow-y-auto pr-1">
				{#if filteredUploadResults.length === 0}
					<div
						class="text-base-content/50 bg-base-200/50 border-base-200 rounded-lg border border-dashed py-8 text-center text-sm"
					>
						{t('certificates.noMatches', language.current)}
					</div>
				{:else}
					{#each filteredUploadResults as result}
						<div
							class="alert {result.success
								? 'alert-success/10 border-success/20 text-success-content'
								: result.isSkipped
									? 'alert-warning/10 border-warning/20 text-warning-content'
									: 'alert-error/10 border-error/20 text-error-content'} flex flex-col items-start gap-1 rounded-lg border px-4 py-3 text-xs shadow-sm"
							data-test-id={`upload-result-item-${result.fileName}`}
						>
							<div class="flex w-full items-center gap-2">
								<Icon
									name={result.success ? 'check' : result.isSkipped ? 'warning' : 'error'}
									class="h-4 w-4 shrink-0 {result.success
										? 'text-success'
										: result.isSkipped
											? 'text-warning'
											: 'text-error'}"
								/>
								<span class="text-base-content truncate font-bold">{result.fileName}</span>
							</div>
							{#if result.subject}
								<div
									class="text-base-content w-full truncate pl-6 opacity-70"
									title={result.subject}
								>
									{result.subject}
								</div>
							{/if}
							{#if result.errorMessage}
								<div
									class="pl-6 font-medium opacity-90 {result.isSkipped
										? 'text-warning'
										: 'text-error'}"
								>
									{result.errorMessage}
								</div>
							{/if}
						</div>
					{/each}
				{/if}
			</div>
		</div>
	{/if}
	<div class="modal-action">
		<Button
			variant="primary"
			onclick={() => (showResultsModal = false)}
			data-test-id="upload-results-close-button"
		>
			{t('common.close', language.current)}
		</Button>
	</div>
</Modal>
