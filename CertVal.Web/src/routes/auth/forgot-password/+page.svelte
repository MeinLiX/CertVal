<script lang="ts">
	import { goto } from '$app/navigation';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';

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
			const response = await api.post('/v1/auth/forgot-password', { email });
			if (response.data) {
				successMessage = t('auth.forgot.emailSent', $language);
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
	<title>{t('auth.forgot.title', $language)} - CertVal</title>
</svelte:head>

<div class="hero">
	<div class="card w-full max-w-sm shrink-0 bg-base-100 shadow-2xl">
		<form class="card-body" onsubmit={handleSubmit}>
			<h2 class="mb-4 text-center text-2xl font-bold">{t('auth.forgot.title', $language)}</h2>

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

			<p class="text-center text-sm">{t('auth.forgot.subtitle', $language)}</p>

			<Input
				label={t('auth.login.email', $language)}
				type="email"
				bind:value={email}
				required
				placeholder="your@email.com"
			/>

			<div class="form-control mt-6">
				<Button type="submit" variant="primary" loading={isLoading}>
					{t('auth.forgot.submit', $language)}
				</Button>
			</div>

			<div class="mt-4 text-center text-sm">
				<a href="/auth/login" class="link link-primary">
					{t('auth.forgot.backToLogin', $language)}
				</a>
			</div>
		</form>
	</div>
</div>
