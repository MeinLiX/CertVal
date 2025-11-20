<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
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
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { Workspace, CreateWorkspaceRequest, PagedResult } from '$lib/types';

	let workspaces = $state<Workspace[]>([]);
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
		isLoading = true;
		try {
			const response = await withMinDelay(api.get<PagedResult<Workspace>>('/workspaces'));
			if (response.data) {
				workspaces = response.data.items;
				workspacesStore.set(workspaces);
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
			const response = await api.post<Workspace>('/workspaces', createForm);
			if (response.data) {
				workspacesStore.add(response.data);
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

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-3xl font-bold">{t('workspaces.title', language.current)}</h1>
			<p class="text-base-content/70 mt-1">{t('workspaces.subtitle', language.current)}</p>
		</div>
		<Button onclick={openCreateModal}>
			<Icon name="plus" />
			{t('workspaces.create', language.current)}
		</Button>
	</div>

	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each { length: 3 } as _}
				<div class="skeleton h-48 w-full"></div>
			{/each}
		</div>
	{:else if workspaces.length === 0}
		<div class="py-16 text-center">
			<div
				class="bg-base-100 text-base-content/50 mx-auto flex h-24 w-24 items-center justify-center rounded-full shadow-inner"
			>
				<Icon name="workspaces" class="h-12 w-12" />
			</div>
			<h3 class="mt-4 text-xl font-semibold">{t('workspaces.empty', language.current)}</h3>
			<p class="text-base-content/60 mt-2">{t('workspaces.createFirst', language.current)}</p>
			<div class="mt-6">
				<Button onclick={openCreateModal}>
					{t('workspaces.create', language.current)}
				</Button>
			</div>
		</div>
	{:else}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each workspaces as workspace (workspace.id)}
				<Card
					hover={true}
					class="cursor-pointer"
					onclick={() => goto(`/workspaces/${workspace.id}`)}
				>
					<div class="mb-4 flex items-center justify-between">
						<h3 class="card-title text-lg">{workspace.name}</h3>
						<span class="badge {workspace.isPublic ? 'badge-accent' : 'badge-ghost'} badge-sm">
							{workspace.isPublic
								? t('common.public', language.current)
								: t('common.private', language.current)}
						</span>
					</div>

					<p class="text-base-content/70 line-clamp-2 h-10 text-sm">
						{workspace.description || t('workspaces.noDescription', language.current)}
					</p>

					<div class="mt-4 flex justify-between text-sm">
						<div class="flex items-center gap-2">
							<Icon name="certificates" class="h-4 w-4 opacity-50" />
							<span
								>{workspace.certificateCount} {t('workspaces.certificates', language.current)}</span
							>
						</div>
						<div class="flex items-center gap-2">
							<Icon name="members" class="h-4 w-4 opacity-50" />
							<span>{workspace.memberCount} {t('workspaces.members', language.current)}</span>
						</div>
					</div>

					<div class="card-actions mt-4 justify-end">
						<Button
							size="sm"
							variant="primary"
							onclick={(e) => {
								e.stopPropagation();
								goto(`/certificates?workspace=${workspace.id}`);
							}}
						>
							{t('workspaces.viewCertificates', language.current)}
						</Button>
					</div>
				</Card>
			{/each}
		</div>
	{/if}
</div>

<Modal
	isOpen={showCreateModal}
	title={t('workspaces.create', language.current)}
	onClose={() => (showCreateModal = false)}
>
	<form onsubmit={handleCreateWorkspace} class="space-y-4">
		{#if errors.general}
			<div role="alert" class="alert alert-error text-sm">
				<Icon name="error" class="h-6 w-6 shrink-0 stroke-current" />
				<span>{errors.general}</span>
			</div>
		{/if}

		<Input
			label={t('workspaces.name', language.current)}
			bind:value={createForm.name}
			required
			error={errors.name}
			placeholder="e.g. Production Environment"
		/>

		<div>
			<label for="description" class="label">
				<span class="label-text">{t('workspaces.description', language.current)}</span>
			</label>
			<textarea
				id="description"
				bind:value={createForm.description}
				rows={3}
				class="textarea-bordered textarea w-full"
				placeholder={t('workspaces.description', language.current)}
			></textarea>
		</div>

		<Input
			type="number"
			label={t('workspaces.maxCertificates', language.current)}
			bind:value={createForm.maxCertificates}
			required
			error={errors.maxCertificates}
		/>

		<div class="flex gap-4">
			<div class="form-control">
				<label class="label cursor-pointer gap-2">
					<span class="label-text">{t('common.public', language.current)}</span>
					<input type="checkbox" bind:checked={createForm.isPublic} class="toggle toggle-primary" />
				</label>
			</div>
			<div class="form-control">
				<label class="label cursor-pointer gap-2">
					<span class="label-text">{t('workspaces.allowInvites', language.current)}</span>
					<input
						type="checkbox"
						bind:checked={createForm.allowMemberInvites}
						class="toggle toggle-primary"
					/>
				</label>
			</div>
		</div>

		<div class="modal-action">
			<Button type="button" variant="ghost" onclick={() => (showCreateModal = false)}>
				{t('common.cancel', language.current)}
			</Button>
			<Button type="submit" loading={isCreating} variant="primary">
				{t('common.save', language.current)}
			</Button>
		</div>
	</form>
</Modal>
