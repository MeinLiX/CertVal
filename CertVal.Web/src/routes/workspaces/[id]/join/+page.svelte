<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { auth } from '$lib/stores/auth';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	let message = $state(t('workspaces.join.processing', language.current));
	let error = $state('');
	let isLoading = $state(true);
	let token = $state('');
	let workspaceId = $state('');

	onMount(async () => {
		token = page.url.searchParams.get('token') || '';
		workspaceId = page.params.id;

		if (!token) {
			error = t('workspaces.join.invalidToken', language.current);
			isLoading = false;
			return;
		}

		const postLoginToken = sessionStorage.getItem('invitation_token');
		if (postLoginToken === token && $auth.isAuthenticated) {
			sessionStorage.removeItem('invitation_token');
			await handleAccept();
			return;
		}

		try {
			const response = await api.get<any>(`/invitations/${token}`);
			if (response.data) {
				message = t('workspaces.join.inviteDetails', language.current, {
					workspaceName: response.data.workspaceName
				});
			} else {
				error = response.message || t('workspaces.join.invalidToken', language.current);
			}
		} catch (e) {
			error = t('workspaces.join.invalidToken', language.current);
		} finally {
			isLoading = false;
		}
	});

	async function handleAccept() {
		if (!$auth.isAuthenticated) {
			sessionStorage.setItem('invitation_token', token);
			goto(`/auth/login?redirect=${page.url.pathname}${page.url.search}`);
			return;
		}

		isLoading = true;
		error = '';
		try {
			const response = await api.post(`/invitations/${token}/accept`);
			if (!response.message) {
				goto(`/workspaces/${workspaceId}`);
			} else {
				error = response.message;
			}
		} catch (e) {
			error = t('workspaces.join.acceptError', language.current);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('workspaces.join.title', language.current)}</title>
</svelte:head>

<div class="hero bg-base-200 min-h-screen">
	<Card class="w-full max-w-md">
		<div class="py-12 text-center">
			<h3 class="text-xl font-semibold">{t('workspaces.join.title', language.current)}</h3>

			{#if isLoading}
				<div class="flex justify-center p-8"><span class="loading loading-spinner"></span></div>
			{:else if error}
				<p class="text-error mt-4">{error}</p>
				<Button class="mt-6" onclick={() => goto('/auth/login')}>
					{t('auth.login.title', language.current)}
				</Button>
			{:else}
				<p class="mt-4">{message}</p>
				<Button class="mt-6" onclick={handleAccept} loading={isLoading}>
					{#if $auth.isAuthenticated}
						{t('workspaces.join.acceptButton', language.current)}
					{:else}
						{t('workspaces.join.loginToAccept', language.current)}
					{/if}
				</Button>
			{/if}
		</div>
	</Card>
</div>
