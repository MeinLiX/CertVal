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

	// Get workspace filter from URL
	const workspaceFilter = $derived($page.url.searchParams.get('workspace'));

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		await Promise.all([loadWorkspaces(), loadCertificates()]);
	});

	// Reload certificates when workspace filter changes
	$effect(() => {
		if (!isLoading) {
			loadCertificates();
		}
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
			let endpoint = '/v1/certificates';
			if (workspaceFilter) {
				endpoint += `?workspaceId=${workspaceFilter}`;
			}

			const response = await api.get<PagedResult<Certificate>>(endpoint);
			if (response.data) {
				certificates = response.data.items;
			}
		} catch (error) {
			console.error('Failed to load certificates:', error);
		} finally {
			isLoading = false;
		}
	}

	async function handleUploadCertificate(event: Event) {
		event.preventDefault();
		errors = {};

		if (!selectedFile || !selectedWorkspaceId) {
			errors.general = 'Please select a file and workspace';
			return;
		}

		isUploading = true;

		try {
			const formData = new FormData();
			formData.append('file', selectedFile);
			formData.append('workspaceId', selectedWorkspaceId);

			const response = await api.upload<Certificate>('/v1/certificates/upload', formData);
			if (response.data) {
				certificates = [response.data, ...certificates];
				showUploadModal = false;
				resetUploadForm();
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
		selectedWorkspaceId = '';
		errors = {};
	}

	function handleFileSelect(eventOrFile: Event | File | FileList | null | undefined) {
		// Support three cases:
		// 1. Native input change Event -> extract from event.target.files
		// 2. A FileList (e.g., from a custom component) -> take first file
		// 3. A single File directly
		if (!eventOrFile) {
			selectedFile = null;
			return;
		}

		// If it's an Event, try to extract files from the target
		if ((eventOrFile as Event).type) {
			const evt = eventOrFile as Event;
			const target = evt.target as HTMLInputElement | null;
			selectedFile = target?.files?.[0] ?? null;
			return;
		}

		// If it's a FileList, use first file
		if ((eventOrFile as FileList).item !== undefined) {
			const files = eventOrFile as FileList;
			selectedFile = files.item(0) ?? null;
			return;
		}

		// Otherwise assume it's a File
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
		return workspace?.name || 'Unknown Workspace';
	}

	function handleWorkspaceFilterChange(event: Event) {
		const target = event.target as HTMLSelectElement;
		const url = new URL(window.location.href);
		if (target.value) {
			url.searchParams.set('workspace', target.value);
		} else {
			url.searchParams.delete('workspace');
		}
		goto(url.pathname + url.search);
	}
</script>

<svelte:head>
	<title>{t('certificates.title', $language)} - CertVal</title>
</svelte:head>

<div class="space-y-6">
	<!-- Header -->
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-2xl font-bold text-gray-900">{t('certificates.title', $language)}</h1>
			<p class="text-gray-600">Monitor and manage your SSL/TLS certificates</p>
		</div>
		<Button onclick={() => (showUploadModal = true)}>
			{t('certificates.upload', $language)}
		</Button>
	</div>

	<!-- Filters -->
	{#if workspaceList.length > 1}
		<Card padding={false}>
			<div class="p-4">
				<div class="flex items-center space-x-4">
					<select
						class="block w-48 rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
						value={workspaceFilter || ''}
						onchange={handleWorkspaceFilterChange}
					>
						<option value="">All Workspaces</option>
						{#each workspaceList as workspace}
							<option value={workspace.id}>
								{workspace.name}
							</option>
						{/each}
					</select>

					{#if workspaceFilter}
						<Button variant="outline" size="sm" onclick={() => goto('/certificates')}>
							Clear Filter
						</Button>
					{/if}
				</div>
			</div>
		</Card>
	{/if}

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
				<h3 class="mt-2 text-sm font-medium text-gray-900">{t('certificates.empty', $language)}</h3>
				<p class="mt-1 text-sm text-gray-500">{t('certificates.uploadFirst', $language)}</p>
				<div class="mt-6">
					<Button onclick={() => (showUploadModal = true)}>
						{t('certificates.upload', $language)}
					</Button>
				</div>
			</div>
		</Card>
	{:else}
		<Card>
			<div class="overflow-hidden">
				<table class="min-w-full divide-y divide-gray-200">
					<thead class="bg-gray-50">
						<tr>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								{t('certificates.subject', $language)}
							</th>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								{t('certificates.issuer', $language)}
							</th>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								Workspace
							</th>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								{t('certificates.expires', $language)}
							</th>
							<th
								class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
							>
								{t('certificates.status', $language)}
							</th>
							<th class="relative px-6 py-3">
								<span class="sr-only">Actions</span>
							</th>
						</tr>
					</thead>
					<tbody class="divide-y divide-gray-200 bg-white">
						{#each certificates as certificate}
							{@const status = getCertificateStatus(certificate.notAfter)}
							<tr class="hover:bg-gray-50">
								<td class="px-6 py-4 whitespace-nowrap">
									<div class="text-sm font-medium text-gray-900">
										{certificate.subject}
									</div>
									<div class="text-sm text-gray-500">
										{certificate.originalFileName}
									</div>
								</td>
								<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-900">
									{certificate.issuer}
								</td>
								<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-500">
									{getWorkspaceName(certificate.workspaceId)}
								</td>
								<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-500">
									{formatDate(certificate.notAfter)}
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
									<button
										class="text-blue-600 hover:text-blue-900"
										onclick={() => goto(`/certificates/${certificate.id}`)}
									>
										View Details
									</button>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</Card>
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
				<p class="text-sm text-red-600">{errors.general}</p>
			</div>
		{/if}

		<div class="space-y-1">
			<label for="workspace-select" class="block text-sm font-medium text-gray-700">
				Workspace <span class="text-red-500">*</span>
			</label>
			<select
				id="workspace-select"
				bind:value={selectedWorkspaceId}
				required
				class="block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
			>
				<option value="">Select a workspace</option>
				{#each workspaceList as workspace}
					<option value={workspace.id}>{workspace.name}</option>
				{/each}
			</select>
		</div>

		<Input
			type="file"
			label="Certificate File"
			accept=".cer,.crt,.pem,.der,.p7b,.p7c,.pfx,.p12"
			required
			onchange={handleFileSelect}
			error={errors.file}
		/>

		{#if selectedFile}
			<div class="rounded-md border border-blue-200 bg-blue-50 p-4">
				<p class="text-sm text-blue-600">
					Selected: {selectedFile.name} ({(selectedFile.size / 1024).toFixed(1)} KB)
				</p>
			</div>
		{/if}

		<div class="flex justify-end space-x-3 pt-4">
			<Button variant="outline" onclick={handleCloseModal} type="button">
				{t('common.cancel', $language)}
			</Button>
			<Button type="submit" loading={isUploading}>
				{t('common.upload', $language)}
			</Button>
		</div>
	</form>
</Modal>