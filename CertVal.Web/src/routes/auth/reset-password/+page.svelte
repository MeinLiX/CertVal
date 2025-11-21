<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import FloatingInput from '$lib/components/ui/FloatingInput.svelte';

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
			errors.confirmPassword = t('errors.passwordsNotMatch', language.current);
			isLoading = false;
			return;
		}

		try {
			const response = await api.post('/auth/reset-password', { token, newPassword: password });
			if (response.data) {
				successMessage = t('success.passwordChanged', language.current);
				setTimeout(() => goto('/auth/login'), 2000);
			} else {
				errors.general = response.message || t('errors.general', language.current);
			}
		} catch (error) {
			errors.general = t('errors.network', language.current);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('auth.reset.title', language.current)} - CertVal</title>
</svelte:head>

<div
	class="card bg-base-100/20 w-full max-w-md shrink-0 overflow-hidden border border-white/20 shadow-2xl backdrop-blur-xl"
	data-test-id="reset-password-card"
>
	<form class="card-body gap-6 p-8" onsubmit={handleSubmit}>
		<div class="mb-2 text-center">
			<h2
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-2xl font-bold text-transparent"
			>
				{t('auth.reset.title', language.current)}
			</h2>
		</div>

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

		<FloatingInput
			id="password"
			label={t('auth.reset.newPassword', language.current)}
			type="password"
			bind:value={password}
			required
			data-test-id="reset-password-input"
		/>
		<FloatingInput
			id="confirmPassword"
			label={t('auth.reset.confirmPassword', language.current)}
			type="password"
			bind:value={confirmPassword}
			required
			error={errors.confirmPassword}
			data-test-id="reset-confirm-password-input"
		/>

		<div class="form-control mt-2">
			<Button
				type="submit"
				variant="primary"
				loading={isLoading}
				class="w-full"
				data-test-id="reset-password-submit-button"
			>
				{t('auth.reset.submit', language.current)}
			</Button>
		</div>
	</form>
</div>
