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
	import Icon from '$lib/components/ui/Icon.svelte';
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
	let uploadForm = $state({
		workspaceId: ''
	});

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

			const response = await api.get<PagedResult<Certificate>>(
				`/v1/search/certificates?${params.toString()}`
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
			const response = await api.upload<BulkUploadResultDto>(
				'/v1/certificates/upload/multiple',
				formData
			);
			if (response.data) {
				uploadResults = response.data;
				showUploadModal = false;
				showResultsModal = true;
				await loadCertificates();
			} else {
				errors.upload = response.message || t('errors.uploadFailed', $language);
			}
		} catch (error) {
			errors.upload = t('errors.network', $language);
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
		<Button onclick={openUploadModal}>
			<Icon name="upload" />
			{t('certificates.upload', $language)}
		</Button>
	</div>

	<Card>
		<div class="grid grid-cols-1 items-end gap-4 md:grid-cols-2 lg:grid-cols-4">
			<Input
				label={t('common.search', $language)}
				value={filters.searchTerm}
				oninput={(e) => updateUrlParams({ search: (e.target as HTMLInputElement).value })}
				placeholder={t('certificates.searchPlaceholder', $language)}
				icon="search"
			/>
			<Select
				label={t('common.workspace', $language)}
				value={filters.workspaceId}
				onchange={(e) => updateUrlParams({ workspace: (e.target as HTMLSelectElement).value })}
				options={workspaceOptions}
			/>
			<Select
				label={t('common.status', $language)}
				value={filters.status}
				onchange={(e) => updateUrlParams({ status: (e.target as HTMLSelectElement).value })}
				options={[
					{ value: 'All', label: t('certificates.all', $language) },
					{ value: 'Valid', label: t('certificates.valid', $language) },
					{ value: 'Expiring', label: t('certificates.expiring', $language) },
					{ value: 'Expired', label: t('certificates.expired', $language) }
				]}
			/>
			<Select
				label={t('certificates.perPage', $language)}
				value={filters.pageSize}
				onchange={(e) => updateUrlParams({ pageSize: (e.target as HTMLSelectElement).value })}
				options={[
					{ value: 10, label: '10' },
					{ value: 20, label: '20' },
					{ value: 50, label: '50' },
					{ value: 100, label: '100' }
				]}
			/>
		</div>
	</Card>

	<Card>
		<div class="overflow-x-auto">
			<table class="table">
				<thead>
					<tr>
						<th>{t('certificates.subject', $language)}</th>
						<th>{t('certificates.expires', $language)}</th>
						<th>{t('common.status', $language)}</th>
						<th>{t('common.workspace', $language)}</th>
						<th></th>
					</tr>
				</thead>
				<tbody>
					{#if isLoading}
						{#each { length: filters.pageSize } as _}
							<tr><td colspan="5"><div class="h-10 w-full skeleton"></div></td></tr>
						{/each}
					{:else if certificates.length === 0}
						<tr
							><td colspan="5" class="py-8 text-center">{t('certificates.empty', $language)}</td
							></tr
						>
					{:else}
						{#each certificates as cert (cert.id)}
							<tr>
								<td>
									<a
										href="/certificates/{cert.id}"
										class="block max-w-sm link truncate font-bold link-hover">{cert.subject}</a
									>
									<div class="text-xs opacity-60">{cert.originalFileName}</div>
								</td>
								<td>
									{formatDate(cert.notAfter)}
									<div class="text-xs opacity-60">
										{cert.daysUntilExpiry}
										{t('certificates.daysLeft', $language)}
									</div>
								</td>
								<td
									><span
										class="badge {getCertificateStatus(cert.notAfter) === 'expired'
											? 'badge-error'
											: getCertificateStatus(cert.notAfter) === 'expiring'
												? 'badge-warning'
												: 'badge-success'} badge-sm"
										>{t(`certificates.${getCertificateStatus(cert.notAfter)}`, $language)}</span
									></td
								>
								<td>{workspaceList.find((w) => w.id === cert.workspaceId)?.name || 'N/A'}</td>
								<td
									><Button
										size="sm"
										variant="ghost"
										onclick={() => goto(`/certificates/${cert.id}`)}
										>{t('common.details', $language)}</Button
									></td
								>
							</tr>
						{/each}
					{/if}
				</tbody>
			</table>
		</div>
		<div class="mt-6 flex flex-col items-center justify-between gap-4 sm:flex-row">
			<div class="text-sm">
				{t('certificates.showing', $language)}
				<strong>{Math.min((filters.page - 1) * filters.pageSize + 1, totalCount)}</strong>
				{t('common.to', $language)}
				<strong>{Math.min(filters.page * filters.pageSize, totalCount)}</strong>
				{t('common.of', $language)} <strong>{totalCount}</strong>
				{t('certificates.results', $language)}
			</div>
			{#if totalPages > 1}
				<div class="join">
					<Button
						class="join-item"
						onclick={() => updateUrlParams({ page: filters.page - 1 })}
						disabled={filters.page <= 1}>«</Button
					>
					<span class="no-animation btn join-item"
						>{t('certificates.page', $language)}
						{filters.page}
						{t('common.of', $language)}
						{totalPages}</span
					>
					<Button
						class="join-item"
						onclick={() => updateUrlParams({ page: filters.page + 1 })}
						disabled={filters.page >= totalPages}>»</Button
					>
				</div>
			{/if}
		</div>
	</Card>
</div>

<Modal
	isOpen={showUploadModal}
	title={t('certificates.upload', $language)}
	onClose={() => (showUploadModal = false)}
>
	<form onsubmit={handleUploadCertificates} class="space-y-4">
		{#if errors.upload}
			<div role="alert" class="alert alert-error text-sm"><span>{errors.upload}</span></div>
		{/if}
		<Select
			label={t('common.workspace', $language)}
			name="workspaceId"
			options={workspaceList.map((w) => ({ value: w.id, label: w.name }))}
			bind:value={uploadForm.workspaceId}
			required
		/>
		<Input
			label={t('certificates.certificateFiles', $language)}
			name="files"
			type="file"
			required
			multiple
			accept=".cer,.crt,.pem,.der,.p7b,.p7c,.pfx,.p12"
		/>
		<div class="modal-action">
			<Button type="button" variant="ghost" onclick={() => (showUploadModal = false)}
				>{t('common.cancel', $language)}</Button
			>
			<Button type="submit" loading={isUploading} variant="primary"
				>{t('common.upload', $language)}</Button
			>
		</div>
	</form>
</Modal>

<Modal
	isOpen={showResultsModal}
	title={t('certificates.uploadResults', $language)}
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
				<Button onclick={() => (showResultsModal = false)}>{t('common.close', $language)}</Button>
			</div>
		</div>
	{/if}
</Modal>
