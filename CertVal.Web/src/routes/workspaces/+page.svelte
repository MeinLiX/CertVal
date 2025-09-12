<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { workspaces } from '$lib/stores/workspaces';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import type { Workspace, CreateWorkspaceRequest, PagedResult } from '$lib/types';

	let workspaceList = $state<Workspace[]>([]);
	let isLoading = $state(true);
	let showCreateModal = $state(false);
	let isCreating = $state(false);

	let createForm = $state<CreateWorkspaceRequest>({
		name: '',
		description: '',
		maxCertificates: 1000,
		isPublic: false,
		allowMemberInvites: true
	});

	let errors = $state<Record<string, string>>({});

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		await loadWorkspaces();
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
		} finally {
			isLoading = false;
		}
	}

	async function handleCreateWorkspace(event: Event) {
		event.preventDefault();
		errors = {};
		isCreating = true;

		try {
			const response = await api.post<Workspace>('/v1/workspaces', createForm);
			if (response.data) {
				workspaceList = [...workspaceList, response.data];
				workspaces.add(response.data);
				showCreateModal = false;
				resetCreateForm();
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', $language);
		} finally {
			isCreating = false;
		}
	}

	function resetCreateForm() {
		createForm = {
			name: '',
			description: '',
			maxCertificates: 1000,
			isPublic: false,
			allowMemberInvites: true
		};
		errors = {};
	}

	function handleCloseModal() {
		showCreateModal = false;
		resetCreateForm();
	}
</script>

<svelte:head>
	<title>{t('workspaces.title', $language)} - CertVal</title>
</svelte:head>

<div class="space-y-6">
	<!-- Header -->
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-2xl font-bold text-gray-900">{t('workspaces.title', $language)}</h1>
			<p class="text-gray-600">Manage your certificate workspaces</p>
		</div>
		<Button onclick={() => (showCreateModal = true)}>
			{t('workspaces.create', $language)}
		</Button>
	</div>

	{#if isLoading}
		<div class="flex h-64 items-center justify-center">
			<div class="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
		</div>
	{:else if workspaceList.length === 0}
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
						d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6"
					/>
				</svg>
				<h3 class="mt-2 text-sm font-medium text-gray-900">{t('workspaces.empty', $language)}</h3>
				<p class="mt-1 text-sm text-gray-500">{t('workspaces.createFirst', $language)}</p>
				<div class="mt-6">
					<Button onclick={() => (showCreateModal = true)}>
						{t('workspaces.create', $language)}
					</Button>
				</div>
			</div>
		</Card>
	{:else}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each workspaceList as workspace}
				<Card>
					<div class="space-y-4">
						<div class="flex items-start justify-between">
							<h3 class="text-lg font-medium text-gray-900">{workspace.name}</h3>
							<span class="text-xs text-gray-500">{workspace.isPublic ? 'Public' : 'Private'}</span>
						</div>

						{#if workspace.description}
							<p class="text-sm text-gray-600">{workspace.description}</p>
						{/if}

						<div class="flex justify-between text-sm text-gray-500">
							<span>{workspace.certificateCount} {t('workspaces.certificates', $language)}</span>
							<span>{workspace.memberCount} {t('workspaces.members', $language)}</span>
						</div>

						<div class="text-xs text-gray-400">
							Created {formatDate(workspace.createdAt)}
						</div>

						<div class="flex items-center justify-between border-t border-gray-200 pt-4">
							<Button
								variant="outline"
								size="sm"
								onclick={() => goto(`/workspaces/${workspace.id}`)}
							>
								View Details
							</Button>
							<Button
								variant="primary"
								size="sm"
								onclick={() => goto(`/certificates?workspace=${workspace.id}`)}
							>
								View Certificates
							</Button>
						</div>
					</div>
				</Card>
			{/each}
		</div>
	{/if}
</div>

<!-- Create Workspace Modal -->
<Modal
	isOpen={showCreateModal}
	title={t('workspaces.create', $language)}
	onClose={handleCloseModal}
>
	<form onsubmit={handleCreateWorkspace} class="space-y-4">
		{#if errors.general}
			<div class="rounded-md border border-red-200 bg-red-50 p-4">
				<p class="text-sm text-red-600">{errors.general}</p>
			</div>
		{/if}

		<Input
			label={t('workspaces.name', $language)}
			bind:value={createForm.name}
			required
			error={errors.name}
		/>

		<div class="space-y-1">
			<label class="block text-sm font-medium text-gray-700">
				{t('workspaces.description', $language)}
			</label>
			<textarea
				bind:value={createForm.description}
				rows="3"
				class="block w-full rounded-md border border-gray-300 px-3 py-2 placeholder-gray-400 shadow-sm focus:border-blue-500 focus:ring-blue-500 focus:outline-none sm:text-sm"
				placeholder="Optional description for this workspace"
			></textarea>
		</div>

		<Input
			type="number"
			label="Max Certificates"
			bind:value={createForm.maxCertificates}
			required
			error={errors.maxCertificates}
		/>

		<div class="flex items-center space-x-4">
			<label class="flex items-center">
				<input
					type="checkbox"
					bind:checked={createForm.isPublic}
					class="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
				/>
				<span class="ml-2 text-sm text-gray-700">Public workspace</span>
			</label>

			<label class="flex items-center">
				<input
					type="checkbox"
					bind:checked={createForm.allowMemberInvites}
					class="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
				/>
				<span class="ml-2 text-sm text-gray-700">Allow member invites</span>
			</label>
		</div>

		<div class="flex justify-end space-x-3 pt-4">
			<Button variant="outline" onclick={handleCloseModal} type="button">
				{t('common.cancel', $language)}
			</Button>
			<Button type="submit" loading={isCreating}>
				{t('common.save', $language)}
			</Button>
		</div>
	</form>
</Modal>
