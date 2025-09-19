<script lang="ts">
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
			successMessage = t('auth.forgot.emailSent', $language);
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

<div class="hero min-h-full">
	<div
		class="card w-full max-w-sm shrink-0 glass shadow-2xl"
		style="background-color: oklch(from var(--color-base-100) l c h / 0.2);"
	>
		<form class="card-body p-8" onsubmit={handleSubmit}>
			<h2 class="card-title justify-center text-2xl font-bold">
				{t('auth.forgot.title', $language)}
			</h2>
			<p class="mb-4 text-center text-sm opacity-70">{t('auth.forgot.subtitle', $language)}</p>

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

			<div class="divider text-xs"></div>

			<div class="text-center text-sm">
				<a href="/auth/login" class="link font-semibold link-primary">
					{t('auth.forgot.backToLogin', $language)}
				</a>
			</div>
		</form>
	</div>
</div>
