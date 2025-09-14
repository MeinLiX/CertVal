<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import type {
		Workspace,
		Certificate,
		PagedResult,
		WorkspaceMember,
		InviteMemberRequest
	} from '$lib/types';

	let workspace = $state<Workspace | null>(null);
	let certificates = $state<Certificate[]>([]);
	let members = $state<WorkspaceMember[]>([]);
	let isLoading = $state(true);
	let errors = $state<Record<string, string>>({});

	let showInviteModal = $state(false);
	let showDeleteModal = $state(false);

	let inviteForm = $state<InviteMemberRequest>({ email: '', role: 'Viewer' });
	let confirmDeleteName = $state('');
	let isProcessing = $state(false);

	const workspaceId = $derived($page.params.id);
	const canManage = $derived(workspace && $auth.user && workspace.ownerId === $auth.user.id);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadData();
	});

	async function loadData() {
		isLoading = true;
		try {
			const [wsRes, certsRes, membersRes] = await Promise.all([
				api.get<Workspace>(`/v1/workspaces/${workspaceId}`),
				api.get<PagedResult<Certificate>>(
					`/v1/certificates?workspaceId=${workspaceId}&pageSize=1&statusFilter=expiring`
				),
				api.get<WorkspaceMember[]>(`/v1/workspaces/${workspaceId}/members`)
			]);

			if (wsRes.data) workspace = wsRes.data;
			if (certsRes.data) certificates = certsRes.data.items;
			if (membersRes.data) members = membersRes.data;
		} catch (err) {
			console.error('Failed to load workspace data:', err);
			errors.load = 'Failed to load workspace data.';
		} finally {
			isLoading = false;
		}
	}

	async function handleInvite(event: Event) {
		event.preventDefault();
		errors = {};
		isProcessing = true;
		try {
			const response = await api.post<WorkspaceMember>(
				`/v1/workspaces/${workspaceId}/members/invite`,
				inviteForm
			);
			if (response.data) {
				members.push(response.data);
				showInviteModal = false;
				inviteForm = { email: '', role: 'Viewer' };
			} else {
				errors.invite = response.message || 'Failed to send invitation.';
			}
		} catch (err) {
			errors.invite = 'A network error occurred.';
		} finally {
			isProcessing = false;
		}
	}

	async function handleDelete(event: Event) {
		event.preventDefault();
		if (confirmDeleteName !== workspace?.name) return;
		isProcessing = true;
		try {
			await api.delete(`/v1/workspaces/${workspaceId}`);
			goto('/workspaces');
		} catch (err) {
			errors.delete = 'Failed to delete workspace.';
		} finally {
			isProcessing = false;
			showDeleteModal = false;
			confirmDeleteName = '';
		}
	}

	function getRoleBadgeClass(role: string): string {
		switch (role.toLowerCase()) {
			case 'administrator':
				return 'badge-primary';
			case 'editor':
				return 'badge-secondary';
			default:
				return 'badge-ghost';
		}
	}
</script>

<svelte:head>
	<title>{workspace ? `${workspace.name} Details` : 'Workspace'}</title>
</svelte:head>

<div class="space-y-6">
	{#if isLoading}
		<div class="flex h-96 items-center justify-center">
			<span class="loading loading-lg loading-spinner"></span>
		</div>
	{:else if !workspace}
		<Card>
			<div class="py-12 text-center">
				<h3 class="text-xl font-semibold">Workspace not found</h3>
				<p class="mt-2 text-base-content/60">
					{errors.load || 'The requested workspace could not be loaded.'}
				</p>
				<Button class="mt-6" onclick={() => goto('/workspaces')}>Back to Workspaces</Button>
			</div>
		</Card>
	{:else}
		<div class="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
			<div>
				<div class="breadcrumbs text-sm">
					<ul>
						<li><a href="/workspaces" class="link hover:link-primary">Workspaces</a></li>
						<li><span class="font-semibold">{workspace.name}</span></li>
					</ul>
				</div>
				<h1 class="mt-2 text-3xl font-bold">{workspace.name}</h1>
				<p class="mt-1 text-base-content/70">{workspace.description}</p>
			</div>
			<div class="flex gap-2">
				<Button variant="ghost" onclick={() => goto(`/certificates?workspace=${workspaceId}`)}
					>View Certificates</Button
				>
				<Button onclick={() => goto(`/notifications?workspace=${workspaceId}`)}
					>Manage Notifications</Button
				>
			</div>
		</div>

		<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-4">
			<Card
				><div class="stat">
					<div class="stat-title">Certificates</div>
					<div class="stat-value">{workspace.certificateCount} / {workspace.maxCertificates}</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">Members</div>
					<div class="stat-value">{workspace.memberCount}</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">Owner</div>
					<div class="stat-value truncate text-lg">{workspace.owner.fullName}</div>
				</div></Card
			>
			<Card
				><div class="stat">
					<div class="stat-title">Created</div>
					<div class="stat-value text-lg">{formatDate(workspace.createdAt)}</div>
				</div></Card
			>
		</div>

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<div class="space-y-6 lg:col-span-2">
				<Card title="Expiring Soon">
					{#if certificates.length > 0}
						<div class="overflow-x-auto">
							<table class="table table-sm">
								<tbody>
									{#each certificates as cert}
										<tr>
											<td>
												<a
													href="/certificates/{cert.id}"
													class="block max-w-md link truncate font-semibold link-hover"
													>{cert.subject}</a
												>
											</td>
											<td>{formatDate(cert.notAfter)} ({cert.daysUntilExpiry} days)</td>
											<td
												><span class="badge badge-sm badge-warning"
													>{getCertificateStatus(cert.notAfter)}</span
												></td
											>
										</tr>
									{/each}
								</tbody>
							</table>
						</div>
					{:else}
						<p class="py-4 text-center text-sm text-base-content/70">
							No certificates are expiring soon.
						</p>
					{/if}
				</Card>
			</div>

			<div class="space-y-6">
				<Card title="Members">
					<div class="space-y-2">
						<div class="flex items-center gap-3">
							<div class="placeholder avatar">
								<div class="w-10 rounded-full bg-primary text-primary-content">
									<span
										>{workspace.owner.firstName.charAt(0)}{workspace.owner.lastName.charAt(0)}</span
									>
								</div>
							</div>
							<div>
								<div class="font-bold">{workspace.owner.fullName}</div>
								<div class="text-xs opacity-50">Owner</div>
							</div>
						</div>
						{#each members as member}
							<div class="flex items-center justify-between">
								<div class="flex items-center gap-3">
									<div class="placeholder avatar">
										<div class="w-10 rounded-full bg-neutral text-neutral-content">
											<span>{member.user.firstName.charAt(0)}{member.user.lastName.charAt(0)}</span>
										</div>
									</div>
									<div>
										<div class="font-bold">{member.user.fullName}</div>
										<div class="text-xs opacity-50">{member.user.email}</div>
									</div>
								</div>
								<span class="badge {getRoleBadgeClass(member.role)}">{member.role}</span>
							</div>
						{/each}
					</div>
					{#if canManage && workspace.allowMemberInvites}
						<div class="mt-4 card-actions">
							<Button class="w-full" variant="ghost" onclick={() => (showInviteModal = true)}
								>Invite Member</Button
							>
						</div>
					{/if}
				</Card>

				{#if canManage}
					<Card class="border-error">
						<h3 class="card-title text-error">Danger Zone</h3>
						<p class="text-sm text-base-content/70">
							These actions are permanent and cannot be undone.
						</p>
						<div class="mt-4 card-actions justify-end">
							<Button variant="danger" onclick={() => (showDeleteModal = true)}
								>Delete Workspace</Button
							>
						</div>
					</Card>
				{/if}
			</div>
		</div>
	{/if}

	<Modal
		isOpen={showInviteModal}
		title="Invite New Member"
		onClose={() => (showInviteModal = false)}
	>
		<form onsubmit={handleInvite} class="space-y-4">
			{#if errors.invite}
				<div role="alert" class="alert alert-error text-sm"><span>{errors.invite}</span></div>
			{/if}
			<Input
				label="Email Address"
				type="email"
				bind:value={inviteForm.email}
				required
				placeholder="member@example.com"
			/>
			<Select
				label="Role"
				bind:value={inviteForm.role}
				options={[
					{ value: 'Viewer', label: 'Viewer' },
					{ value: 'Editor', label: 'Editor' },
					{ value: 'Administrator', label: 'Administrator' }
				]}
			/>
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showInviteModal = false)}
					>Cancel</Button
				>
				<Button type="submit" loading={isProcessing} variant="primary">Send Invitation</Button>
			</div>
		</form>
	</Modal>

	<Modal
		isOpen={showDeleteModal}
		title="Delete Workspace"
		onClose={() => (showDeleteModal = false)}
	>
		<form onsubmit={handleDelete} class="space-y-4">
			<p>
				This action is irreversible. You will lose all certificates and configuration associated
				with this workspace.
			</p>
			<p>
				To confirm, please type the name of the workspace: <strong class="text-error"
					>{workspace?.name}</strong
				>
			</p>
			{#if errors.delete}
				<div role="alert" class="alert alert-error text-sm"><span>{errors.delete}</span></div>
			{/if}
			<Input bind:value={confirmDeleteName} placeholder="Workspace name" />
			<div class="modal-action">
				<Button type="button" variant="ghost" onclick={() => (showDeleteModal = false)}
					>Cancel</Button
				>
				<Button
					type="submit"
					variant="danger"
					loading={isProcessing}
					disabled={confirmDeleteName !== workspace?.name}>Delete Workspace</Button
				>
			</div>
		</form>
	</Modal>
</div>
