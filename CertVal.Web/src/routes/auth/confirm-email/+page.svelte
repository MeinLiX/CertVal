<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';
	import { auth } from '$lib/stores/auth';
	import Button from '$lib/components/ui/Button.svelte';
	import type { LoginResponse } from '$lib/types';

	let message = $state(t('auth.confirm.processing', $language));
	let isSuccess = $state(false);
	let isLoggingIn = $state(false);
	
	onMount(async () => {
		const token = page.url.searchParams.get('token');
		if (!token) {
			message = t('auth.confirm.invalidToken', $language);
			return;
		}

		try {
			const response = await api.post<LoginResponse>('/v1/auth/confirm-email', { token });
			if (response.data) {
				message = t('auth.confirm.success', $language);
				isSuccess = true;
				isLoggingIn = true;
				
				auth.login(response.data.token, response.data.user);
				
				setTimeout(() => {
					goto('/dashboard');
				}, 1500);
			} else {
				message = response.message || t('errors.general', $language);
			}
		} catch (error) {
			message = t('errors.network', $language);
		}
	});
</script>

<svelte:head>
	<title>{t('auth.confirm.title', $language)} - CertVal</title>
</svelte:head>

<div class="hero min-h-full">
	<div
		class="card w-full max-w-md glass shadow-2xl"
		style="background-color: oklch(from var(--color-base-100) l c h / 0.2);"
	>
		<div class="card-body items-center p-8 text-center">
			<h2 class="card-title text-2xl font-bold">{t('auth.confirm.title', $language)}</h2>
			<p class="mt-4">{message}</p>
			{#if isSuccess}
				{#if isLoggingIn}
					<div class="mt-6">
						<p class="text-sm text-gray-600">{t('auth.confirm.redirecting', $language)}</p>
					</div>
				{:else}
					<div class="mt-6 card-actions">
						<Button variant="primary" onclick={() => goto('/dashboard')}>
							{t('auth.confirm.continue', $language)}
						</Button>
					</div>
				{/if}
			{/if}
		</div>
	</div>
</div>
