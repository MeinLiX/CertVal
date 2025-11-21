<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import FloatingInput from '$lib/components/ui/FloatingInput.svelte';
	import { goto } from '$app/navigation';

	let email = $state('');
	let errors = $state<Record<string, string>>({});
	let isLoading = $state(false);
	let successMessage = $state('');

	async function handleSubmit(event: Event) {
		event.preventDefault();
		errors = {};
		isLoading = true;
		successMessage = '';

		try {
			const response = await api.post('/auth/forgot-password', { email });
			successMessage = t('auth.forgot.emailSent', language.current);
		} catch (error) {
			errors.general = t('errors.network', language.current);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('auth.forgot.title', language.current)} - CertVal</title>
</svelte:head>

<div
	class="card bg-base-100/20 w-full max-w-md shrink-0 overflow-hidden border border-white/20 shadow-2xl backdrop-blur-xl"
	data-test-id="forgot-password-card"
>
	<form class="card-body gap-6 p-8" onsubmit={handleSubmit}>
		<div class="mb-2 text-center">
			<h2
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-2xl font-bold text-transparent"
			>
				{t('auth.forgot.title', language.current)}
			</h2>
			<p class="text-base-content/60 mt-2 text-sm">
				{t('auth.forgot.subtitle', language.current)}
			</p>
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
			id="email"
			label={t('auth.login.email', language.current)}
			type="email"
			bind:value={email}
			required
			data-test-id="forgot-password-email-input"
		/>

		<div class="form-control mt-2">
			<Button
				type="submit"
				variant="primary"
				loading={isLoading}
				class="w-full"
				data-test-id="forgot-password-submit-button"
			>
				{t('auth.forgot.submit', language.current)}
			</Button>
		</div>

		<div class="divider text-base-content/40 text-xs"></div>

		<div class="text-center text-sm">
			<button
				type="button"
				onclick={() => goto('/auth/login')}
				class="link link-primary font-semibold no-underline hover:underline"
				data-test-id="forgot-password-back-button"
			>
				{t('auth.forgot.backToLogin', language.current)}
			</button>
		</div>
	</form>
</div>
