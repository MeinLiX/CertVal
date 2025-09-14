<script lang="ts">
	import { onMount, tick } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { workspaces as workspacesStore } from '$lib/stores/workspaces';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import type { Certificate, Workspace, PagedResult, BulkUploadResultDto } from '$lib/types';

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

	const filters = $derived({
		searchTerm: $page.url.searchParams.get('search') || '',
		status: $page.url.searchParams.get('status') || 'All',
		workspaceId: $page.url.searchParams.get('workspace') || '',
		page: parseInt($page.url.searchParams.get('page') || '1'),
		pageSize: parseInt($page.url.searchParams.get('pageSize') || '10')
	});
	
	const totalPages = $derived(Math.ceil(totalCount / filters.pageSize) || 1);
	const workspaceOptions = $derived([
		{ value: '', label: t('certificates.allWorkspaces', $language) },
		...workspaceList.map((w) => ({ value: w.id, label: w.name }))
	]);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
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
			const response = await api.get<PagedResult<Workspace>>('/v1/workspaces');
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
			const params = new URLSearchParams({
				pageNumber: filters.page.toString(),
				pageSize: filters.pageSize.toString(),
				sortBy: 'notAfter',
				sortDescending: 'false'
			});
			if (filters.workspaceId) params.set('workspaceId', filters.workspaceId);
			if (filters.searchTerm) params.set('query', filters.searchTerm);
			if (filters.status !== 'All') params.set('statusFilter', filters.status);
			
			const response = await api.get<PagedResult<Certificate>>(`/v1/search/certificates?${params.toString()}`);
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
	
	function updateUrlParams(newParams: Record<string, string | number>) {
		const params = new URLSearchParams($page.url.searchParams);
		for (const [key, value] of Object.entries(newParams)) {
			if (value) {
				params.set(key, String(value));
			} else {
				params.delete(key);
			}
		}
		if (!('page' in newParams)) {
			params.delete('page');
		}

		goto(`?${params.toString()}`, { keepFocus: true, noScroll: true, replaceState: true });
	}

	async function handleUploadCertificates(event: Event) {
		event.preventDefault();
		const form = event.target as HTMLFormElement;
		const formData = new FormData(form);
		isUploading = true;
		errors = {};
		try {
			const response = await api.upload<BulkUploadResultDto>('/v1/certificates/upload/multiple', formData);
			if (response.data) {
				uploadResults = response.data;
				showUploadModal = false;
				showResultsModal = true;
				await loadCertificates();
			} else {
				errors.upload = response.message || 'Upload failed.';
			}
		} catch (error) {
			errors.upload = 'A network error occurred during upload.';
		} finally {
			isUploading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('certificates.title', $language)}</title>
</svelte:head>

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-3xl font-bold">{t('certificates.title', $language)}</h1>
			<p class="mt-1 text-base-content/70">{t('certificates.subtitle', $language)}</p>
		</div>
		<Button onclick={() => showUploadModal = true}>
			<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" /></svg>
			{t('certificates.upload', $language)}
		</Button>
	</div>

	<Card>
		<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 items-end">
			<Input label="Search" value={filters.searchTerm} oninput={(e) => updateUrlParams({ search: (e.target as HTMLInputElement).value })} placeholder="Subject, issuer..." icon="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
			<Select label="Workspace" value={filters.workspaceId} onchange={(e) => updateUrlParams({ workspace: (e.target as HTMLSelectElement).value })} options={workspaceOptions} />
			<Select label="Status" value={filters.status} onchange={(e) => updateUrlParams({ status: (e.target as HTMLSelectElement).value })} options={[ {value: 'All', label: 'All'}, {value: 'Valid', label: 'Valid'}, {value: 'Expiring', label: 'Expiring'}, {value: 'Expired', label: 'Expired'} ]} />
			<Select label="Per Page" value={filters.pageSize} onchange={(e) => updateUrlParams({ pageSize: (e.target as HTMLSelectElement).value })} options={[ {value: 10, label: '10'}, {value: 20, label: '20'}, {value: 50, label: '50'}, {value: 100, label: '100'} ]} />
		</div>
	</Card>

	<Card>
		<div class="overflow-x-auto">
			<table class="table">
				<thead>
					<tr>
						<th>{t('certificates.subject', $language)}</th>
						<th>{t('certificates.expires', $language)}</th>
						<th>{t('certificates.status', $language)}</th>
						<th>{t('common.workspace', $language)}</th>
						<th></th>
					</tr>
				</thead>
				<tbody>
					{#if isLoading}
						{#each { length: filters.pageSize } as _}
							<tr><td colspan="5"><div class="skeleton h-10 w-full"></div></td></tr>
						{/each}
					{:else if certificates.length === 0}
						<tr><td colspan="5" class="text-center py-8">No certificates found.</td></tr>
					{:else}
						{#each certificates as cert (cert.id)}
							<tr>
								<td>
									<a href="/certificates/{cert.id}" class="link link-hover font-bold truncate block max-w-sm">{cert.subject}</a>
									<div class="text-xs opacity-60">{cert.originalFileName}</div>
								</td>
								<td>
									{formatDate(cert.notAfter)}
									<div class="text-xs opacity-60">{cert.daysUntilExpiry} days left</div>
								</td>
								<td><span class="badge {getCertificateStatus(cert.notAfter) === 'expired' ? 'badge-error' : getCertificateStatus(cert.notAfter) === 'expiring' ? 'badge-warning' : 'badge-success'} badge-sm">{getCertificateStatus(cert.notAfter)}</span></td>
								<td>{workspaceList.find(w => w.id === cert.workspaceId)?.name || 'N/A'}</td>
								<td><Button size="sm" variant="ghost" onclick={() => goto(`/certificates/${cert.id}`)}>Details</Button></td>
							</tr>
						{/each}
					{/if}
				</tbody>
			</table>
		</div>
		<div class="flex flex-col sm:flex-row justify-between items-center gap-4 mt-6">
			<div class="text-sm">
				Showing <strong>{Math.min((filters.page - 1) * filters.pageSize + 1, totalCount)}</strong>
				to <strong>{Math.min(filters.page * filters.pageSize, totalCount)}</strong>
				of <strong>{totalCount}</strong> results
			</div>
			{#if totalPages > 1}
			<div class="join">
				<Button class="join-item" onclick={() => updateUrlParams({ page: filters.page - 1 })} disabled={filters.page <= 1}>«</Button>
				<span class="join-item btn no-animation">Page {filters.page} of {totalPages}</span>
				<Button class="join-item" onclick={() => updateUrlParams({ page: filters.page + 1 })} disabled={filters.page >= totalPages}>»</Button>
			</div>
			{/if}
		</div>
	</Card>
</div>

<Modal isOpen={showUploadModal} title="Upload Certificates" onClose={() => showUploadModal = false}>
	<form onsubmit={handleUploadCertificates} class="space-y-4">
		{#if errors.upload}
			<div role="alert" class="alert alert-error text-sm"><span>{errors.upload}</span></div>
		{/if}
		<Select label="Workspace" name="workspaceId" options={workspaceList.map(w => ({value: w.id, label: w.name}))} required />
		<Input label="Certificate Files" name="files" type="file" required multiple accept=".cer,.crt,.pem,.der,.p7b,.p7c,.pfx,.p12" />
		<div class="modal-action">
			<Button type="button" variant="ghost" onclick={() => showUploadModal = false}>Cancel</Button>
			<Button type="submit" loading={isUploading} variant="primary">Upload</Button>
		</div>
	</form>
</Modal>

<Modal isOpen={showResultsModal} title="Upload Results" onClose={() => showResultsModal = false}>
	{#if uploadResults}
		<div class="space-y-4">
			<p>{uploadResults.summary}</p>
			<div class="max-h-60 overflow-y-auto space-y-2">
				{#each uploadResults.results as result}
					<div class="p-2 rounded-lg {result.success ? 'bg-success/20' : result.isSkipped ? 'bg-warning/20' : 'bg-error/20'}">
						<p class="font-semibold text-sm">{result.fileName}</p>
						{#if !result.success}
							<p class="text-xs opacity-70">{result.errorMessage}</p>
						{/if}
					</div>
				{/each}
			</div>
			<div class="modal-action">
				<Button onclick={() => showResultsModal = false}>Close</Button>
			</div>
		</div>
	{/if}
</Modal>