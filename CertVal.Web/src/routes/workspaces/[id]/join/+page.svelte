<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';
	import { auth } from '$lib/stores/auth';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	let message = $state(t('workspaces.join.processing', $language));
	let error = $state('');
	let isLoading = $state(true);
	let token = $state('');
	let workspaceId = $state('');

	onMount(async () => {
		token = page.url.searchParams.get('token') || '';
		workspaceId = page.params.id;

		if (!token) {
			error = t('workspaces.join.invalidToken', $language);
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
			const response = await api.get<any>(`/v1/invitations/${token}`);
			if (response.data) {
				message = t('workspaces.join.inviteDetails', $language, { workspaceName: response.data.workspaceName });
			} else {
				error = response.message || t('workspaces.join.invalidToken', $language);
			}
		} catch (e) {
			error = t('workspaces.join.invalidToken', $language);
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
			const response = await api.post(`/v1/invitations/${token}/accept`);
			if (!response.message) {
				goto(`/workspaces/${workspaceId}`);
			} else {
				error = response.message;
			}
		} catch (e) {
			error = t('workspaces.join.acceptError', $language);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('workspaces.join.title', $language)}</title>
</svelte:head>

<div class="hero min-h-screen bg-base-200">
	<Card class="w-full max-w-md">
		<div class="py-12 text-center">
			<h3 class="text-xl font-semibold">{t('workspaces.join.title', $language)}</h3>
			
			{#if isLoading}
				<div class="flex justify-center p-8"><span class="loading loading-spinner"></span></div>
			{:else if error}
				<p class="mt-4 text-error">{error}</p>
				<Button class="mt-6" onclick={() => goto('/auth/login')}>
					{t('auth.login.title', $language)}
				</Button>
			{:else}
				<p class="mt-4">{message}</p>
				<Button class="mt-6" onclick={handleAccept} loading={isLoading}>
					{#if $auth.isAuthenticated}
						{t('workspaces.join.acceptButton', $language)}
					{:else}
						{t('workspaces.join.loginToAccept', $language)}
					{/if}
				</Button>
			{/if}
		</div>
	</Card>
</div>