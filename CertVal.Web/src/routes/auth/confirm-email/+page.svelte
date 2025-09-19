<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	let message = $state(t('auth.confirm.processing', $language));
	let isSuccess = $state(false);

	onMount(async () => {
		const token = page.url.searchParams.get('token');
		if (!token) {
			message = t('auth.confirm.invalidToken', $language);
			return;
		}

		try {
			const response = await api.post('/v1/auth/confirm-email', { token });
			if (response.data) {
				message = t('auth.confirm.success', $language);
				isSuccess = true;
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

<div class="hero">
	<Card class="w-full max-w-md">
		<div class="py-12 text-center">
			<h3 class="text-xl font-semibold">{t('auth.confirm.title', $language)}</h3>
			<p class="mt-4">{message}</p>
			{#if isSuccess}
				<Button class="mt-6" onclick={() => goto('/auth/login')}>
					{t('auth.confirm.login', $language)}
				</Button>
			{/if}
		</div>
	</Card>
</div>
