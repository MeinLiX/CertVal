<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { auth } from '$lib/stores/auth';
	import Button from '$lib/components/ui/Button.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';

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

<div
	class="bg-base-200/30 flex min-h-[calc(100vh-10rem)] items-center justify-center p-4"
	data-test-id="join-workspace-page"
>
	<div class="w-full max-w-md space-y-8">
		<div
			class="border-base-content/10 bg-base-100/50 rounded-2xl border p-8 shadow-xl backdrop-blur-sm"
		>
			<div class="mb-8 text-center">
				<h1 class="text-2xl font-bold">{t('workspaces.join.title', language.current)}</h1>
			</div>

			{#if isLoading}
				<div class="flex flex-col items-center justify-center space-y-4 py-8">
					<span class="loading loading-spinner loading-lg text-primary"></span>
					<p class="text-base-content/60 animate-pulse">{message}</p>
				</div>
			{:else if error}
				<div class="flex flex-col items-center justify-center space-y-4 text-center">
					<div class="bg-error/10 text-error mb-2 rounded-full p-4">
						<Icon name="error" class="h-8 w-8" />
					</div>
					<p class="text-error font-medium">{error}</p>
					<Button
						class="mt-4 w-full"
						variant="outline"
						onclick={() => goto('/auth/login')}
						data-test-id="join-back-to-login-button"
					>
						<Icon name="leftArrow" class="mr-2 h-4 w-4" />
						{t('auth.login.title', language.current)}
					</Button>
				</div>
			{:else}
				<div class="flex flex-col items-center justify-center space-y-6 text-center">
					<div
						class="from-primary/10 to-secondary/10 text-primary mb-2 rounded-3xl bg-gradient-to-br p-6"
					>
						<Icon name="workspaces" class="h-12 w-12" />
					</div>

					<p class="text-lg font-medium">
						{message}
					</p>

					<div class="w-full pt-4">
						<Button
							class="shadow-primary/20 w-full shadow-lg"
							variant="primary"
							size="lg"
							onclick={handleAccept}
							loading={isLoading}
							data-test-id="join-workspace-button"
						>
							{#if $auth.isAuthenticated}
								<Icon name="check" class="mr-2 h-5 w-5" />
								{t('workspaces.join.acceptButton', language.current)}
							{:else}
								<Icon name="user" class="mr-2 h-5 w-5" />
								{t('workspaces.join.loginToAccept', language.current)}
							{/if}
						</Button>
					</div>
				</div>
			{/if}
		</div>
	</div>
</div>
