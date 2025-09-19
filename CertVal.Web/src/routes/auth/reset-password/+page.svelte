<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';

	let password = $state('');
	let confirmPassword = $state('');
	let errors = $state<Record<string, string>>({});
	let isLoading = $state(false);
	let successMessage = $state('');
	let token = $state('');
	onMount(() => {
		token = page.url.searchParams.get('token') || '';
		if (!token) {
			goto('/auth/login');
		}
	});

	async function handleSubmit(event: Event) {
		event.preventDefault();
		errors = {};
		isLoading = true;
		if (password !== confirmPassword) {
			errors.confirmPassword = t('errors.passwordsNotMatch', $language);
			isLoading = false;
			return;
		}

		try {
			const response = await api.post('/v1/auth/reset-password', { token, newPassword: password });
			if (response.data) {
				successMessage = t('success.passwordChanged', $language);
				setTimeout(() => goto('/auth/login'), 2000);
			} else {
				errors.general = response.message || t('errors.general', $language);
			}
		} catch (error) {
			errors.general = t('errors.network', $language);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('auth.reset.title', $language)} - CertVal</title>
</svelte:head>

<div class="hero min-h-full">
	<div
		class="card w-full max-w-sm shrink-0 glass shadow-2xl"
		style="background-color: oklch(from var(--color-base-100) l c h / 0.2);"
	>
		<form class="card-body p-8" onsubmit={handleSubmit}>
			<h2 class="mb-4 card-title justify-center text-2xl font-bold">
				{t('auth.reset.title', $language)}
			</h2>

			{#if successMessage}
				<div role="alert" class="alert alert-success text-sm">
					<span>{successMessage}</span>
				</div>
			{/if}

			{#if errors.general}
				<div role="alert" class="alert alert-error text-sm">
					<span>{errors.general}</span>
				</div>
			{/if}

			<Input
				label={t('auth.reset.newPassword', $language)}
				type="password"
				bind:value={password}
				required
			/>
			<Input
				label={t('auth.reset.confirmPassword', $language)}
				type="password"
				bind:value={confirmPassword}
				required
				error={errors.confirmPassword}
			/>

			<div class="form-control mt-6">
				<Button type="submit" variant="primary" loading={isLoading}>
					{t('auth.reset.submit', $language)}
				</Button>
			</div>
		</form>
	</div>
</div>
