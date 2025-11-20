<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth';
	import { workspaces as workspacesStore } from '$lib/stores/workspaces';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { withMinDelay } from '$lib/utils/loading';
	import { t } from '$lib/i18n';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { Certificate, Workspace, PagedResult, BulkUploadResultDto } from '$lib/types';
	import CertificateCard from '$lib/components/certificates/CertificateCard.svelte';
	import Pagination from '$lib/components/ui/Pagination.svelte';

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

	const filters = $derived({
		subjectTerm: page.url.searchParams.get('search') || '',
		issuerTerm: page.url.searchParams.get('issuer') || '',
		status: page.url.searchParams.get('status') || 'All',
		workspaceId: page.url.searchParams.get('workspace') || '',
		page: parseInt(page.url.searchParams.get('page') || '1'),
		pageSize: parseInt(page.url.searchParams.get('pageSize') || '12')
	});

	const totalPages = $derived(Math.ceil(totalCount / filters.pageSize) || 1);
	const workspaceOptions = $derived([
		{ value: '', label: t('certificates.allWorkspaces', language.current) },
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
			const response = await api.get<PagedResult<Workspace>>('/workspaces');
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
			if (filters.subjectTerm) params.set('subject', filters.subjectTerm);
			if (filters.issuerTerm) params.set('issuer', filters.issuerTerm);
			if (filters.status !== 'All') params.set('statusFilter', filters.status);

			const response = await withMinDelay(
				api.get<PagedResult<Certificate>>(`/certificates?${params.toString()}`)
			);
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
		const params = new URLSearchParams(page.url.searchParams);
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

	function openUploadModal() {
		const defaultWorkspace =
			filters.workspaceId || (workspaceList.length > 0 ? workspaceList[0].id : '');
		uploadForm.workspaceId = defaultWorkspace;
		errors = {};
		showUploadModal = true;
	}

	async function handleUploadCertificates(event: Event) {
		event.preventDefault();
		const form = event.target as HTMLFormElement;
		const formData = new FormData(form);
		isUploading = true;
		errors = {};
		try {
			const response = await api.upload<BulkUploadResultDto>('/certificates/upload', formData);
			if (response.data) {
				uploadResults = response.data;
				showUploadModal = false;
				showResultsModal = true;
				await loadCertificates();
			} else {
				errors.upload = response.message || t('errors.uploadFailed', language.current);
			}
		} catch (error) {
			errors.upload = t('errors.network', language.current);
		} finally {
			isUploading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('certificates.title', language.current)}</title>
</svelte:head>

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-3xl font-bold">{t('certificates.title', language.current)}</h1>
			<p class="text-base-content/70 mt-1">{t('certificates.subtitle', language.current)}</p>
		</div>
		<Button onclick={openUploadModal}>
			<Icon name="upload" />
			{t('certificates.upload', language.current)}
		</Button>
	</div>

	<Card>
		<div class="grid grid-cols-1 items-end gap-4 md:grid-cols-2 lg:grid-cols-4">
			<Input
				label={t('certificates.subject', language.current)}
				value={filters.subjectTerm}
				oninput={(e) => updateUrlParams({ search: (e.target as HTMLInputElement).value })}
				placeholder={t('certificates.searchSubjectPlaceholder', language.current)}
				icon="search"
			/>
			<Select
				label={t('common.workspace', language.current)}
				value={filters.workspaceId}
				onchange={(e) => updateUrlParams({ workspace: (e.target as HTMLSelectElement).value })}
				options={workspaceOptions}
			/>
			<Select
				label={t('common.status', language.current)}
				value={filters.status}
				onchange={(e) => updateUrlParams({ status: (e.target as HTMLSelectElement).value })}
				options={[
					{ value: 'All', label: t('certificates.all', language.current) },
					{ value: 'Valid', label: t('certificates.valid', language.current) },
					{ value: 'Expiring', label: t('certificates.expiring', language.current) },
					{ value: 'Expired', label: t('certificates.expired', language.current) }
				]}
			/>
			<Select
				label={t('certificates.perPage', language.current)}
				value={filters.pageSize}
				onchange={(e) => updateUrlParams({ pageSize: (e.target as HTMLSelectElement).value })}
				options={[
					{ value: 12, label: '12' },
					{ value: 24, label: '24' },
					{ value: 48, label: '48' }
				]}
			/>
		</div>
	</Card>

	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
			{#each { length: filters.pageSize } as _}
				<div class="skeleton h-64 w-full"></div>
			{/each}
		</div>
	{:else if certificates.length === 0}
		<div class="py-16 text-center">
			<h3 class="text-xl font-semibold">{t('certificates.empty', language.current)}</h3>
			<p class="text-base-content/60 mt-2">{t('certificates.uploadFirst', language.current)}</p>
		</div>
	{:else}
		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
			{#each certificates as cert (cert.id)}
				<CertificateCard
					certificate={cert}
					workspaceName={workspaceList.find((w) => w.id === cert.workspaceId)?.name}
				/>
			{/each}
		</div>
	{/if}

	<div class="mt-6 flex flex-col items-center justify-between gap-4 sm:flex-row">
		<div class="text-sm">
			{t('certificates.showing', language.current)}
			<strong>{Math.min((filters.page - 1) * filters.pageSize + 1, totalCount)}</strong>
			{t('common.to', language.current)}
			<strong>{Math.min(filters.page * filters.pageSize, totalCount)}</strong>
			{t('common.of', language.current)} <strong>{totalCount}</strong>
			{t('certificates.results', language.current)}
		</div>
		{#if totalPages > 1}
			<Pagination
				currentPage={filters.page}
				{totalPages}
				onPageChange={(p) => updateUrlParams({ page: p })}
			/>
		{/if}
	</div>
</div>

<Modal
	isOpen={showUploadModal}
	title={t('certificates.upload', language.current)}
	onClose={() => (showUploadModal = false)}
>
	<form onsubmit={handleUploadCertificates} class="space-y-4">
		{#if errors.upload}
			<div role="alert" class="alert alert-error text-sm"><span>{errors.upload}</span></div>
		{/if}
		<Select
			label={t('common.workspace', language.current)}
			name="workspaceId"
			options={workspaceList.map((w) => ({ value: w.id, label: w.name }))}
			bind:value={uploadForm.workspaceId}
			required
		/>
		<Input
			label={t('certificates.certificateFiles', language.current)}
			name="files"
			type="file"
			required
			multiple
			accept=".cer,.crt,.pem,.der,.p7b,.p7c,.pfx,.p12"
		/>
		<div class="modal-action">
			<Button type="button" variant="ghost" onclick={() => (showUploadModal = false)}
				>{t('common.cancel', language.current)}</Button
			>
			<Button type="submit" loading={isUploading} variant="primary"
				>{t('common.upload', language.current)}</Button
			>
		</div>
	</form>
</Modal>

<Modal
	isOpen={showResultsModal}
	title={t('certificates.uploadResults', language.current)}
	onClose={() => (showResultsModal = false)}
>
	{#if uploadResults}
		<div class="space-y-4">
			<p>{uploadResults.summary}</p>
			<div class="max-h-60 space-y-2 overflow-y-auto">
				{#each uploadResults.results as result}
					<div
						class="rounded-lg p-2 {result.success
							? 'bg-success/20'
							: result.isSkipped
								? 'bg-warning/20'
								: 'bg-error/20'}"
					>
						<p class="text-sm font-semibold">{result.fileName}</p>
						{#if !result.success}
							<p class="text-xs opacity-70">{result.errorMessage}</p>
						{/if}
					</div>
				{/each}
			</div>
			<div class="modal-action">
				<Button onclick={() => (showResultsModal = false)}
					>{t('common.close', language.current)}</Button
				>
			</div>
		</div>
	{/if}
</Modal>
