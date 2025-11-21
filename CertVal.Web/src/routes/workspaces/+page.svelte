<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { WorkspaceService } from '$lib/services/WorkspaceService';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { Workspace, CreateWorkspaceRequest } from '$lib/types';

	let workspaces = $state<Workspace[]>([]);
	let isLoading = $state(true);
	let showCreateModal = $state(false);
	let isCreating = $state(false);
	let searchQuery = $state('');

	let createForm = $state<CreateWorkspaceRequest>({
		name: '',
		description: '',
		maxCertificates: 1000,
		isPublic: false,
		allowMemberInvites: true
	});
	let errors = $state<Record<string, string>>({});

	const filteredWorkspaces = $derived(
		workspaces.filter(
			(w) =>
				w.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
				w.description?.toLowerCase().includes(searchQuery.toLowerCase())
		)
	);

	onMount(async () => {
		if (!userSession.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadWorkspaces();
	});

	$effect(() => {
		const action = page.url.searchParams.get('action');
		if (action === 'create') {
			openCreateModal();
			
			const newUrl = new URL(window.location.href);
			newUrl.searchParams.delete('action');
			goto(newUrl.toString(), { replaceState: true, keepFocus: true, noScroll: true });
		}
	});

	async function loadWorkspaces() {
		isLoading = true;
		try {
			const response = await WorkspaceService.getAll();
			if (response.data) {
				workspaces = response.data.items;
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
			const response = await WorkspaceService.create(createForm);
			if (response.data) {
				workspaces = [...workspaces, response.data];
				showCreateModal = false;
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', language.current);
		} finally {
			isCreating = false;
		}
	}

	function openCreateModal() {
		createForm = {
			name: '',
			description: '',
			maxCertificates: 1000,
			isPublic: false,
			allowMemberInvites: true
		};
		errors = {};
		showCreateModal = true;
	}
</script>

<svelte:head>
	<title>{t('workspaces.title', language.current)}</title>
</svelte:head>

<div class="animate-in fade-in slide-in-from-bottom-4 min-h-[80vh] space-y-8 duration-500">
	<div class="flex flex-col items-start justify-between gap-6 md:flex-row md:items-center">
		<div class="space-y-2">
			<h1
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-4xl font-bold tracking-tight text-transparent"
			>
				{t('workspaces.title', language.current)}
			</h1>
			<p class="text-base-content/60 max-w-2xl text-lg font-light">
				{t('workspaces.subtitle', language.current)}
			</p>
		</div>
		<div class="flex w-full flex-col items-center gap-4 sm:flex-row md:w-auto">
			<div class="w-full sm:w-64">
				<Input
					type="search"
					placeholder={t('common.search', language.current)}
					bind:value={searchQuery}
					icon="search"
					variant="bordered"
					class="bg-base-100/50"
				/>
			</div>
			<Button
				variant="primary"
				size="md"
				class="shadow-primary/20 hover:shadow-primary/40 whitespace-nowrap shadow-lg transition-all"
				onclick={openCreateModal}
				data-testid="create-workspace-btn"
			>
				<Icon name="plus" class="mr-2 h-5 w-5" />
				{t('workspaces.create', language.current)}
			</Button>
		</div>
	</div>

	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each Array(3) as _}
				<div class="card bg-base-100 h-48 animate-pulse shadow-xl">
					<div class="card-body">
						<div class="bg-base-300 mb-4 h-6 w-1/2 rounded"></div>
						<div class="bg-base-300 h-4 w-3/4 rounded"></div>
						<div class="bg-base-300 mt-2 h-4 w-1/2 rounded"></div>
					</div>
				</div>
			{/each}
		</div>
	{:else if filteredWorkspaces.length > 0}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each filteredWorkspaces as workspace (workspace.id)}
				<Card
					variant="glass"
					class="hover:border-primary/30 group flex h-full flex-col transition-all duration-300"
				>
					<div class="mb-4 flex items-start justify-between">
						<h3
							class="group-hover:text-primary line-clamp-1 text-xl font-bold transition-colors"
							title={workspace.name}
						>
							{workspace.name}
						</h3>
						<div class="flex gap-2">
							{#if workspace.isPublic}
								<span
									class="badge badge-sm badge-info gap-1"
									title={t('common.public', language.current)}
								>
									<Icon name="users" class="h-3 w-3" />
									{t('common.public', language.current)}
								</span>
							{:else}
								<span
									class="badge badge-sm badge-ghost gap-1"
									title={t('common.private', language.current)}
								>
									<Icon name="lock" class="h-3 w-3" />
									{t('common.private', language.current)}
								</span>
							{/if}
						</div>
					</div>

					<p class="text-base-content/60 mb-6 line-clamp-2 flex-grow text-sm">
						{workspace.description || t('common.noDescription', language.current)}
					</p>

					<div class="mb-4 grid grid-cols-2 gap-4">
						<div class="bg-base-200/30 flex flex-col gap-1 rounded-lg p-2">
							<div class="text-base-content/60 flex items-center gap-2 text-xs">
								<Icon name="certificates" class="h-3 w-3" />
								{t('workspaces.certificates', language.current)}
							</div>
							<div class="font-semibold">
								{workspace.certificateCount}
								<span class="text-base-content/40 text-xs">/ {workspace.maxCertificates}</span>
							</div>
						</div>

						<div class="bg-base-200/30 flex flex-col gap-1 rounded-lg p-2">
							<div class="text-base-content/60 flex items-center gap-2 text-xs">
								<Icon name="users" class="h-3 w-3" />
								{t('workspaces.members', language.current)}
							</div>
							<div class="font-semibold">
								{workspace.memberCount}
							</div>
						</div>
					</div>

					<div class="border-base-content/5 flex items-center justify-between border-t pt-4">
						<div
							class="text-base-content/60 flex items-center gap-2 text-xs"
							title={t('workspaces.invitesAllowed', language.current)}
						>
							<div
								class={`h-2 w-2 rounded-full ${workspace.allowMemberInvites ? 'bg-success' : 'bg-base-300'}`}
							></div>
							{workspace.allowMemberInvites
								? t('workspaces.invitesAllowed', language.current)
								: t('workspaces.invitesDisabled', language.current)}
						</div>

						<Button
							variant="ghost"
							size="sm"
							class="group-hover:bg-primary group-hover:text-primary-content"
							onclick={() => goto(`/workspaces/${workspace.id}`)}
						>
							{t('common.view', language.current)}
							<Icon name="rightArrow" class="ml-2 h-4 w-4" />
						</Button>
					</div>
				</Card>
			{/each}
		</div>
	{:else}
		<div
			class="bg-base-100/30 border-base-content/5 flex flex-col items-center justify-center rounded-3xl border py-20 text-center backdrop-blur-sm"
		>
			<div class="bg-base-200/50 mb-6 rounded-full p-6">
				<Icon name="workspaces" class="text-base-content/20 h-16 w-16" />
			</div>
			<h3 class="mb-2 text-xl font-bold">{t('workspaces.empty', language.current)}</h3>
			<p class="text-base-content/60 mb-8 max-w-md">
				{searchQuery
					? t('common.noResults', language.current)
					: t('workspaces.emptyDescription', language.current)}
			</p>
			{#if !searchQuery}
				<Button variant="primary" onclick={openCreateModal}>
					{t('workspaces.createFirst', language.current)}
				</Button>
			{/if}
		</div>
	{/if}
</div>

<Modal
	isOpen={showCreateModal}
	title={t('workspaces.create', language.current)}
	onClose={() => (showCreateModal = false)}
>
	<form onsubmit={handleCreateWorkspace} class="space-y-6">
		<Input
			label={t('workspaces.name', language.current)}
			bind:value={createForm.name}
			error={errors.name}
			required
			placeholder={t('workspaces.namePlaceholder', language.current)}
		/>

		<Input
			label={t('workspaces.description', language.current)}
			bind:value={createForm.description}
			error={errors.description}
			placeholder={t('workspaces.descriptionPlaceholder', language.current)}
		/>

		<Input
			label={t('workspaces.maxCertificates', language.current)}
			type="number"
			bind:value={createForm.maxCertificates}
			error={errors.maxCertificates}
			min="1"
			max="10000"
		/>

		<div class="form-control">
			<label class="label cursor-pointer justify-start gap-4">
				<input
					type="checkbox"
					class="checkbox checkbox-primary"
					bind:checked={createForm.isPublic}
				/>
				<span class="label-text font-medium">{t('workspaces.isPublic', language.current)}</span>
			</label>
		</div>

		<div class="form-control">
			<label class="label cursor-pointer justify-start gap-4">
				<input
					type="checkbox"
					class="checkbox checkbox-primary"
					bind:checked={createForm.allowMemberInvites}
				/>
				<span class="label-text font-medium"
					>{t('workspaces.allowMemberInvites', language.current)}</span
				>
			</label>
		</div>

		{#if errors.general}
			<div class="alert alert-error text-sm shadow-lg">
				<Icon name="alert" class="h-5 w-5" />
				<span>{errors.general}</span>
			</div>
		{/if}

		<div class="flex justify-end gap-3 pt-4">
			<Button variant="ghost" onclick={() => (showCreateModal = false)} disabled={isCreating}>
				{t('common.cancel', language.current)}
			</Button>
			<Button type="submit" variant="primary" loading={isCreating}>
				{t('common.create', language.current)}
			</Button>
		</div>
	</form>
</Modal>
