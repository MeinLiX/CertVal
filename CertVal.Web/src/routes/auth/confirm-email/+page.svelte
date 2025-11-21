<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { auth } from '$lib/stores/auth';
	import { userSession } from '$lib/stores/userSession.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import type { LoginResponse } from '$lib/types';

	let message = $state(t('auth.confirm.processing', language.current));
	let isSuccess = $state(false);
	let isLoggingIn = $state(false);
	let isError = $state(false);

	onMount(async () => {
		const token = page.url.searchParams.get('token');
		if (!token) {
			message = t('auth.confirm.invalidToken', language.current);
			isError = true;
			return;
		}

		try {
			const response = await api.post<LoginResponse>('/auth/confirm-email', { token });
			if (response.data) {
				message = t('auth.confirm.success', language.current);
				isSuccess = true;
				isLoggingIn = true;

				auth.login(response.data.token, response.data.user);
				userSession.login(response.data.token, response.data.user);

				setTimeout(() => {
					goto('/dashboard');
				}, 1500);
			} else {
				message = response.message || t('errors.general', language.current);
				isError = true;
			}
		} catch (error) {
			message = t('errors.network', language.current);
			isError = true;
		}
	});
</script>

<svelte:head>
	<title>{t('auth.confirm.title', language.current)} - CertVal</title>
</svelte:head>

<div
	class="card bg-base-100/20 w-full max-w-md shrink-0 overflow-hidden border border-white/20 shadow-2xl backdrop-blur-xl"
	data-test-id="confirm-email-card"
>
	<div class="card-body items-center gap-6 p-8 text-center">
		<div class="mb-2">
			<h2
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-2xl font-bold text-transparent"
			>
				{t('auth.confirm.title', language.current)}
			</h2>
		</div>

		<p class="text-base-content/80 text-lg">{message}</p>

		{#if isSuccess}
			{#if isLoggingIn}
				<div class="mt-2">
					<span class="loading loading-spinner loading-md text-primary"></span>
					<p class="text-base-content/60 mt-2 text-sm">
						{t('auth.confirm.redirecting', language.current)}
					</p>
				</div>
			{:else}
				<div class="card-actions mt-2 w-full">
					<Button
						variant="primary"
						onclick={() => goto('/dashboard')}
						class="w-full"
						data-test-id="confirm-email-continue-button"
					>
						{t('auth.confirm.continue', language.current)}
					</Button>
				</div>
			{/if}
		{/if}

		{#if isError}
			<div class="card-actions mt-4 w-full">
				<Button
					variant="primary"
					onclick={() => goto('/auth/login')}
					class="w-full"
					data-test-id="confirm-email-login-button"
				>
					{t('auth.confirm.login', language.current)}
				</Button>
			</div>
		{/if}
	</div>
</div>
