<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { workspaces } from '$lib/stores/workspaces';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import type { Certificate, Workspace, PagedResult } from '$lib/types';

	let certificates = $state<Certificate[]>([]);
	let workspaceList = $state<Workspace[]>([]);
	let isLoading = $state(true);
	let showUploadModal = $state(false);
	let isUploading = $state(false);

	let selectedWorkspaceId = $state<string>('');
	let selectedFile = $state<File | null>(null);
	let errors = $state<Record<string, string>>({});

	// Filters
	let searchTerm = $state<string>('');
	let statusFilter = $state<'All' | 'Valid' | 'Expiring' | 'Expired'>('All');
	let formatFilter = $state<string>('');
	let sortBy = $state<string>('notAfter');
	let sortDescending = $state<boolean>(false);

	// Pagination
	let currentPage = $state<number>(1);
	let pageSize = $state<number>(20);
	let totalCount = $state<number>(0);

	const currentParams = $derived($page.url.searchParams);
	const workspaceFilter = $derived(currentParams.get('workspace'));

	const totalPages = $derived(Math.ceil(totalCount / pageSize));

	$effect(() => {
		const urlParams = currentParams;
		searchTerm = urlParams.get('search') || '';
		const statusParam = urlParams.get('status');
		statusFilter = (statusParam as 'All' | 'Valid' | 'Expiring' | 'Expired') || 'All';
		formatFilter = urlParams.get('format') || '';
		sortBy = urlParams.get('sortBy') || 'notAfter';
		sortDescending = urlParams.get('sortDesc') === 'true';
		currentPage = parseInt(urlParams.get('page') || '1');
	});

	$effect(() => {
		const deps = [
			searchTerm,
			statusFilter,
			formatFilter,
			sortBy,
			sortDescending,
			currentPage,
			workspaceFilter
		];

		if (!isLoading && workspaceList.length > 0) {
			loadCertificates();
		}
	});

	$effect(() => {
		if (typeof window !== 'undefined') {
			const url = new URL(window.location.href);

			// Update search params
			if (searchTerm) url.searchParams.set('search', searchTerm);
			else url.searchParams.delete('search');

			if (statusFilter && statusFilter !== 'All') url.searchParams.set('status', statusFilter);
			else url.searchParams.delete('status');

			if (formatFilter) url.searchParams.set('format', formatFilter);
			else url.searchParams.delete('format');

			if (sortBy !== 'notAfter') url.searchParams.set('sortBy', sortBy);
			else url.searchParams.delete('sortBy');

			if (sortDescending) url.searchParams.set('sortDesc', 'true');
			else url.searchParams.delete('sortDesc');

			if (currentPage > 1) url.searchParams.set('page', currentPage.toString());
			else url.searchParams.delete('page');

			window.history.replaceState({}, '', url.pathname + url.search);
		}
	});

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		await Promise.all([loadWorkspaces(), loadCertificates()]);
		isLoading = false;
	});

	async function loadWorkspaces() {
		try {
			const response = await api.get<PagedResult<Workspace>>('/v1/workspaces');
			if (response.data) {
				workspaceList = response.data.items;
				workspaces.set(workspaceList);
			}
		} catch (error) {
			console.error('Failed to load workspaces:', error);
		}
	}

	async function loadCertificates() {
		try {
			const searchParams = {
				query: searchTerm || undefined,
				workspaceId: workspaceFilter || undefined,
				statusFilter: statusFilter,
				format: formatFilter || undefined,
				pageSize: pageSize,
				pageNumber: currentPage
			};

			const response = await api.searchCertificates<PagedResult<Certificate>>(searchParams);

			if (response.data) {
				certificates = response.data.items;
				totalCount = response.data.totalCount;
			}
		} catch (error) {
			console.error('Failed to load certificates:', error);
		}
	}

	async function handleUploadCertificate(event: Event) {
		event.preventDefault();
		errors = {};

		if (!selectedFile || !selectedWorkspaceId) {
			errors.general = t('errors.required', $language);
			return;
		}

		isUploading = true;

		try {
			const formData = new FormData();
			formData.append('file', selectedFile);
			formData.append('workspaceId', selectedWorkspaceId);

			const response = await api.upload<Certificate>('/v1/certificates/upload', formData);
			if (response.data) {
				showUploadModal = false;
				resetUploadForm();
				await loadCertificates(); // Reload list
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', $language);
		} finally {
			isUploading = false;
		}
	}

	function resetUploadForm() {
		selectedFile = null;
		selectedWorkspaceId = workspaceFilter || '';
		errors = {};
	}

	function handleFileSelect(eventOrFile: Event | File | FileList | null | undefined) {
		if (!eventOrFile) {
			selectedFile = null;
			return;
		}

		if ((eventOrFile as Event).type) {
			const evt = eventOrFile as Event;
			const target = evt.target as HTMLInputElement | null;
			selectedFile = target?.files?.[0] ?? null;
			return;
		}

		if ((eventOrFile as FileList).item !== undefined) {
			const files = eventOrFile as FileList;
			selectedFile = files.item(0) ?? null;
			return;
		}

		selectedFile = eventOrFile as File;
	}

	function handleCloseModal() {
		showUploadModal = false;
		resetUploadForm();
	}

	function getStatusColor(status: 'expired' | 'expiring' | 'valid'): string {
		switch (status) {
			case 'expired':
				return 'text-red-600 bg-red-100';
			case 'expiring':
				return 'text-yellow-600 bg-yellow-100';
			case 'valid':
				return 'text-green-600 bg-green-100';
		}
	}

	function getStatusText(status: 'expired' | 'expiring' | 'valid'): string {
		return t(`certificates.${status}`, $language);
	}

	function getWorkspaceName(workspaceId: string): string {
		const workspace = workspaceList.find((w) => w.id === workspaceId);
		return workspace?.name || t('workspaces.unknownWorkspace', $language);
	}

	function handleWorkspaceFilterChange(event: Event) {
		const target = event.target as HTMLSelectElement;
		const url = new URL(window.location.href);
		if (target.value) {
			url.searchParams.set('workspace', target.value);
		} else {
			url.searchParams.delete('workspace');
		}
		url.searchParams.delete('page');
		goto(url.pathname + url.search);
	}

	function clearFilters() {
		searchTerm = '';
		statusFilter = 'All';
		formatFilter = '';
		sortBy = 'notAfter';
		sortDescending = false;
		currentPage = 1;
		goto('/certificates' + (workspaceFilter ? `?workspace=${workspaceFilter}` : ''));
	}

	function changePage(page: number) {
		currentPage = page;
		// URL and reload will be handled by $effect
	}

	function handleSort(field: string) {
		if (sortBy === field) {
			sortDescending = !sortDescending;
		} else {
			sortBy = field;
			sortDescending = false;
		}
		currentPage = 1;
	}

	function handleFilterChange() {
		currentPage = 1;
	}

	// Format file size
	function formatFileSize(bytes: number): string {
		if (bytes === 0) return '0 B';
		const k = 1024;
		const sizes = ['B', 'KB', 'MB', 'GB'];
		const i = Math.floor(Math.log(bytes) / Math.log(k));
		return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
	}

	// Get unique formats for filter dropdown
	const availableFormats = $derived([...new Set(certificates.map((c) => c.fileFormat))].sort());

	// Show upload modal with pre-selected workspace
	function showUploadModalWithWorkspace() {
		selectedWorkspaceId = workspaceFilter || (workspaceList.length > 0 ? workspaceList[0].id : '');
		showUploadModal = true;
	}

	function handleBackNavigation() {
		window.history.back();
	}

	function getStatusFilterText(filter: 'All' | 'Valid' | 'Expiring' | 'Expired'): string {
		switch (filter) {
			case 'All':
				return t('certificates.allStatuses', $language);
			case 'Valid':
				return t('certificates.valid', $language);
			case 'Expiring':
				return t('certificates.expiring30', $language);
			case 'Expired':
				return t('certificates.expired', $language);
			default:
				return filter;
		}
	}
</script>

<svelte:head>
	<title>{t('certificates.title', $language)}</title>
</svelte:head>

<div class="space-y-6">
	<!-- Header -->
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-2xl font-bold text-gray-900">{t('certificates.title', $language)}</h1>
			<p class="text-gray-600">{t('certificates.subtitle', $language)}</p>
		</div>
		<Button onclick={showUploadModalWithWorkspace}>
			<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
			</svg>
			{t('certificates.upload', $language)}
		</Button>
	</div>

	<!-- Filters Card -->
	<Card>
		<div class="space-y-4">
			<div class="flex items-center justify-between">
				<h3 class="text-lg font-medium text-gray-900">{t('common.filter', $language)}</h3>
				<Button variant="outline" size="sm" onclick={clearFilters}>
					<svg class="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M6 18L18 6M6 6l12 12"
						/>
					</svg>
					{t('certificates.clearAll', $language)}
				</Button>
			</div>

			<div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-5">
				<!-- Workspace Filter -->
				{#if workspaceList.length > 1}
					<div>
						<label for="workspace-filter" class="mb-1 block text-sm font-medium text-gray-700"
							>{t('common.workspace', $language)}</label
						>
						<select
							id="workspace-filter"
							class="block w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none"
							value={workspaceFilter || ''}
							onchange={handleWorkspaceFilterChange}
						>
							<option value="">{t('certificates.allWorkspaces', $language)}</option>
							{#each workspaceList as workspace}
								<option value={workspace.id}>{workspace.name}</option>
							{/each}
						</select>
					</div>
				{/if}

				<!-- Search -->
				<div>
					<Input
						id="search-filter"
						label={t('common.search', $language)}
						type="search"
						bind:value={searchTerm}
						placeholder={t('certificates.searchPlaceholder', $language)}
						oninput={handleFilterChange}
					/>
				</div>

				<!-- Status Filter -->
				<div>
					<label for="status-filter" class="mb-1 block text-sm font-medium text-gray-700"
						>{t('certificates.status', $language)}</label
					>
					<select
						id="status-filter"
						bind:value={statusFilter}
						onchange={handleFilterChange}
						class="block w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none"
					>
						<option value="All">{t('certificates.allStatuses', $language)}</option>
						<option value="Valid">{t('certificates.valid', $language)}</option>
						<option value="Expiring">{t('certificates.expiring30', $language)}</option>
						<option value="Expired">{t('certificates.expired', $language)}</option>
					</select>
				</div>

				<!-- Format Filter -->
				{#if availableFormats.length > 1}
					<div>
						<label for="format-filter" class="mb-1 block text-sm font-medium text-gray-700"
							>{t('certificates.format', $language)}</label
						>
						<select
							id="format-filter"
							bind:value={formatFilter}
							onchange={handleFilterChange}
							class="block w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none"
						>
							<option value="">{t('certificates.allFormats', $language)}</option>
							{#each availableFormats as format}
								<option value={format}>{format}</option>
							{/each}
						</select>
					</div>
				{/if}

				<!-- Sort -->
				<div>
					<label for="sort-filter" class="mb-1 block text-sm font-medium text-gray-700"
						>{t('certificates.sortBy', $language)}</label
					>
					<select
						id="sort-filter"
						bind:value={sortBy}
						onchange={handleFilterChange}
						class="block w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none"
					>
						<option value="notAfter">{t('certificates.expiryDate', $language)}</option>
						<option value="subject">{t('certificates.subject', $language)}</option>
						<option value="issuer">{t('certificates.issuer', $language)}</option>
						<option value="createdAt">{t('certificates.createdDate', $language)}</option>
					</select>
				</div>
			</div>

			<!-- Active Filters Display -->
			{#if searchTerm || statusFilter !== 'All' || formatFilter || workspaceFilter}
				<div class="flex flex-wrap gap-2 pt-2">
					<span class="text-sm font-medium text-gray-700"
						>{t('certificates.activeFilters', $language)}:</span
					>
					{#if searchTerm}
						<span
							class="inline-flex items-center rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-800"
						>
							{t('common.search', $language)}: {searchTerm}
							<button
								onclick={() => {
									searchTerm = '';
									handleFilterChange();
								}}
								class="ml-1 text-blue-600 hover:text-blue-800">×</button
							>
						</span>
					{/if}
					{#if statusFilter !== 'All'}
						<span
							class="inline-flex items-center rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-800"
						>
							{t('certificates.status', $language)}: {getStatusFilterText(statusFilter)}
							<button
								onclick={() => {
									statusFilter = 'All';
									handleFilterChange();
								}}
								class="ml-1 text-blue-600 hover:text-blue-800">×</button
							>
						</span>
					{/if}
					{#if formatFilter}
						<span
							class="inline-flex items-center rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-800"
						>
							{t('certificates.format', $language)}: {formatFilter}
							<button
								onclick={() => {
									formatFilter = '';
									handleFilterChange();
								}}
								class="ml-1 text-blue-600 hover:text-blue-800">×</button
							>
						</span>
					{/if}
					{#if workspaceFilter}
						<span
							class="inline-flex items-center rounded-full bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-800"
						>
							{t('common.workspace', $language)}: {getWorkspaceName(workspaceFilter)}
							<button
								onclick={() => goto('/certificates')}
								class="ml-1 text-blue-600 hover:text-blue-800">×</button
							>
						</span>
					{/if}
				</div>
			{/if}
		</div>
	</Card>

	{#if isLoading}
		<div class="flex h-64 items-center justify-center">
			<div class="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
		</div>
	{:else if certificates.length === 0}
		<Card>
			<div class="py-12 text-center">
				<svg
					class="mx-auto h-12 w-12 text-gray-400"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
				>
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
					/>
				</svg>
				<h3 class="mt-2 text-sm font-medium text-gray-900">
					{searchTerm || statusFilter !== 'All' || formatFilter
						? t('certificates.noMatches', $language)
						: t('certificates.empty', $language)}
				</h3>
				<p class="mt-1 text-sm text-gray-500">
					{searchTerm || statusFilter !== 'All' || formatFilter
						? t('certificates.adjustFilters', $language)
						: t('certificates.uploadFirst', $language)}
				</p>
				<div class="mt-6 flex justify-center space-x-3">
					{#if searchTerm || statusFilter !== 'All' || formatFilter}
						<Button variant="outline" onclick={clearFilters}
							>{t('certificates.clearFilters', $language)}</Button
						>
					{/if}
					<Button onclick={showUploadModalWithWorkspace}>
						{t('certificates.upload', $language)}
					</Button>
				</div>
			</div>
		</Card>
	{:else}
		<!-- Results Summary -->
		<div class="flex items-center justify-between text-sm text-gray-600">
			<span>
				{t('certificates.showing', $language)}
				{certificates.length}
				{t('common.of', $language)}
				{totalCount}
				{t('certificates.title', $language).toLowerCase()}
				{#if statusFilter !== 'All'}
					({getStatusFilterText(statusFilter)})
				{/if}
				{#if currentPage > 1}
					({t('certificates.page', $language)}
					{currentPage}
					{t('common.of', $language)}
					{totalPages}){/if}
			</span>
			<div class="flex items-center space-x-2">
				<span>{t('certificates.sort', $language)}:</span>
				<button
					onclick={() => handleSort(sortBy)}
					class="flex items-center space-x-1 text-blue-600 hover:text-blue-800"
				>
					<span
						>{sortBy === 'notAfter'
							? t('certificates.expiry', $language)
							: sortBy === 'createdAt'
								? t('certificates.created', $language)
								: t(`certificates.${sortBy}`, $language)}</span
					>
					<svg
						class="h-4 w-4 {sortDescending ? 'rotate-180' : ''}"
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M5 15l7-7 7 7"
						/>
					</svg>
				</button>
			</div>
		</div>

		<!-- Certificates Table -->
		<Card padding={false}>
			<div class="overflow-hidden">
				<table class="min-w-full divide-y divide-gray-200">
					<thead class="bg-gray-50">
						<tr>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								<button
									onclick={() => handleSort('subject')}
									class="group flex items-center space-x-1 hover:text-gray-700"
								>
									<span>{t('certificates.subject', $language)}</span>
									<svg
										class="h-3 w-3 opacity-0 group-hover:opacity-100 {sortBy === 'subject'
											? 'opacity-100'
											: ''}"
										fill="none"
										viewBox="0 0 24 24"
										stroke="currentColor"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M8 9l4-4 4 4m0 6l-4 4-4-4"
										/>
									</svg>
								</button>
							</th>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								<button
									onclick={() => handleSort('issuer')}
									class="group flex items-center space-x-1 hover:text-gray-700"
								>
									<span>{t('certificates.issuer', $language)}</span>
									<svg
										class="h-3 w-3 opacity-0 group-hover:opacity-100 {sortBy === 'issuer'
											? 'opacity-100'
											: ''}"
										fill="none"
										viewBox="0 0 24 24"
										stroke="currentColor"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M8 9l4-4 4 4m0 6l-4 4-4-4"
										/>
									</svg>
								</button>
							</th>
							{#if !workspaceFilter}
								<th
									class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
								>
									{t('common.workspace', $language)}
								</th>
							{/if}
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								{t('certificates.format', $language)}
							</th>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								<button
									onclick={() => handleSort('notAfter')}
									class="group flex items-center space-x-1 hover:text-gray-700"
								>
									<span>{t('certificates.expires', $language)}</span>
									<svg
										class="h-3 w-3 opacity-0 group-hover:opacity-100 {sortBy === 'notAfter'
											? 'opacity-100'
											: ''}"
										fill="none"
										viewBox="0 0 24 24"
										stroke="currentColor"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M8 9l4-4 4 4m0 6l-4 4-4-4"
										/>
									</svg>
								</button>
							</th>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								{t('certificates.status', $language)}
							</th>
							<th class="relative px-6 py-3">
								<span class="sr-only">{t('common.actions', $language)}</span>
							</th>
						</tr>
					</thead>
					<tbody class="divide-y divide-gray-200 bg-white">
						{#each certificates as certificate}
							{@const status = getCertificateStatus(certificate.notAfter)}
							<tr class="hover:bg-gray-50">
								<td class="px-6 py-4 whitespace-nowrap">
									<div class="flex items-center">
										{#if certificate.isBundle}
											<svg
												class="mr-2 h-4 w-4 text-blue-500"
												fill="none"
												viewBox="0 0 24 24"
												stroke="currentColor"
											>
												<path
													stroke-linecap="round"
													stroke-linejoin="round"
													stroke-width="2"
													d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6"
												/>
											</svg>
										{/if}
										<div>
											<div
												class="max-w-xs truncate text-sm font-medium text-gray-900"
												title={certificate.subject}
											>
												{certificate.subject}
											</div>
											<div class="text-sm text-gray-500">{certificate.originalFileName}</div>
										</div>
									</div>
								</td>
								<td
									class="max-w-xs truncate px-6 py-4 text-sm whitespace-nowrap text-gray-900"
									title={certificate.issuer}
								>
									{certificate.issuer}
								</td>
								{#if !workspaceFilter}
									<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-500">
										{getWorkspaceName(certificate.workspaceId)}
									</td>
								{/if}
								<td class="px-6 py-4 whitespace-nowrap">
									<span
										class="inline-flex items-center rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-medium text-gray-800"
									>
										{certificate.fileFormat}
									</span>
								</td>
								<td class="px-6 py-4 text-sm whitespace-nowrap">
									<div class="text-gray-900">{formatDate(certificate.notAfter)}</div>
									<div class="text-xs text-gray-500">{formatFileSize(certificate.fileSize)}</div>
								</td>
								<td class="px-6 py-4 whitespace-nowrap">
									<span
										class="inline-flex rounded-full px-2 py-1 text-xs font-semibold {getStatusColor(
											status
										)}"
									>
										{getStatusText(status)}
										{#if certificate.daysUntilExpiry > 0}
											({certificate.daysUntilExpiry} {t('certificates.days', $language)})
										{/if}
									</span>
								</td>
								<td class="px-6 py-4 text-right text-sm font-medium whitespace-nowrap">
									<Button
										variant="outline"
										size="sm"
										onclick={() => goto(`/certificates/${certificate.id}`)}
									>
										{t('certificates.viewDetails', $language)}
									</Button>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</Card>

		<!-- Pagination -->
		{#if totalPages > 1}
			<div class="flex items-center justify-between">
				<div class="flex items-center space-x-2 text-sm text-gray-700">
					<span>
						{t('certificates.showing', $language)}
						{(currentPage - 1) * pageSize + 1}
						{t('common.to', $language)}
						{Math.min(currentPage * pageSize, totalCount)}
						{t('common.of', $language)}
						{totalCount}
						{t('certificates.results', $language)}
					</span>
				</div>

				<div class="flex items-center space-x-1">
					<Button
						variant="outline"
						size="sm"
						disabled={currentPage === 1}
						onclick={() => changePage(currentPage - 1)}
					>
						{t('common.previous', $language)}
					</Button>

					{#if totalPages <= 7}
						{#each Array(totalPages) as _, i}
							<Button
								variant={currentPage === i + 1 ? 'primary' : 'outline'}
								size="sm"
								onclick={() => changePage(i + 1)}
							>
								{i + 1}
							</Button>
						{/each}
					{:else}
						<!-- Show first page -->
						<Button
							variant={currentPage === 1 ? 'primary' : 'outline'}
							size="sm"
							onclick={() => changePage(1)}
						>
							1
						</Button>

						{#if currentPage > 3}
							<span class="px-2 text-gray-500">...</span>
						{/if}

						<!-- Show pages around current -->
						{#each [Math.max(2, currentPage - 1), currentPage, Math.min(totalPages - 1, currentPage + 1)] as page}
							{#if page > 1 && page < totalPages}
								<Button
									variant={currentPage === page ? 'primary' : 'outline'}
									size="sm"
									onclick={() => changePage(page)}
								>
									{page}
								</Button>
							{/if}
						{/each}

						{#if currentPage < totalPages - 2}
							<span class="px-2 text-gray-500">...</span>
						{/if}

						<!-- Show last page -->
						<Button
							variant={currentPage === totalPages ? 'primary' : 'outline'}
							size="sm"
							onclick={() => changePage(totalPages)}
						>
							{totalPages}
						</Button>
					{/if}

					<Button
						variant="outline"
						size="sm"
						disabled={currentPage === totalPages}
						onclick={() => changePage(currentPage + 1)}
					>
						{t('common.next', $language)}
					</Button>
				</div>
			</div>
		{/if}
	{/if}
</div>

<!-- Upload Certificate Modal -->
<Modal
	isOpen={showUploadModal}
	title={t('certificates.upload', $language)}
	onClose={handleCloseModal}
>
	<form onsubmit={handleUploadCertificate} class="space-y-4">
		{#if errors.general}
			<div class="rounded-md border border-red-200 bg-red-50 p-4">
				<div class="flex">
					<svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
						<path
							fill-rule="evenodd"
							d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
							clip-rule="evenodd"
						/>
					</svg>
					<div class="ml-3">
						<p class="text-sm text-red-600">{errors.general}</p>
					</div>
				</div>
			</div>
		{/if}

		<div class="space-y-1">
			<label for="workspace-select" class="block text-sm font-medium text-gray-700">
				{t('common.workspace', $language)} <span class="text-red-500">*</span>
			</label>
			<select
				id="workspace-select"
				bind:value={selectedWorkspaceId}
				required
				class="block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
			>
				<option value="">{t('certificates.selectWorkspace', $language)}</option>
				{#each workspaceList as workspace}
					<option value={workspace.id}>{workspace.name}</option>
				{/each}
			</select>
		</div>

		<Input
			type="file"
			label={t('certificates.certificateFile', $language)}
			accept=".cer,.crt,.pem,.der,.p7b,.p7c,.pfx,.p12"
			required
			onchange={handleFileSelect}
			error={errors.file}
		/>

		{#if selectedFile}
			<div class="rounded-md border border-blue-200 bg-blue-50 p-4">
				<div class="flex items-center">
					<svg
						class="mr-2 h-5 w-5 text-blue-600"
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
						/>
					</svg>
					<div>
						<p class="text-sm font-medium text-blue-800">{selectedFile.name}</p>
						<p class="text-xs text-blue-600">{formatFileSize(selectedFile.size)}</p>
					</div>
				</div>
			</div>
		{/if}

		<div class="rounded-md border border-gray-200 bg-gray-50 p-4">
			<h4 class="mb-2 text-sm font-medium text-gray-900">
				{t('certificates.supportedFormats', $language)}:
			</h4>
			<div class="grid grid-cols-4 gap-2 text-xs text-gray-600">
				<span>• .cer, .crt</span>
				<span>• .pem</span>
				<span>• .der</span>
				<span>• .p7b, .p7c</span>
				<span>• .pfx, .p12</span>
			</div>
		</div>

		<div class="flex justify-end space-x-3 pt-4">
			<Button variant="outline" onclick={handleCloseModal} type="button">
				{t('common.cancel', $language)}
			</Button>
			<Button type="submit" loading={isUploading} disabled={!selectedFile || !selectedWorkspaceId}>
				<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
					<path
						stroke-linecap="round"
						stroke-linejoin="round"
						stroke-width="2"
						d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
					/>
				</svg>
				{t('common.upload', $language)}
			</Button>
		</div>
	</form>
</Modal>
