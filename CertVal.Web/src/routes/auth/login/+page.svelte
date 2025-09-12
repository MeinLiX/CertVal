<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import type { LoginRequest } from '$lib/types';

	let formData = $state<LoginRequest>({
		email: '',
		password: ''
	});

	let errors = $state<Record<string, string>>({});
	let isLoading = $state(false);

	async function handleSubmit(event: Event) {
		event.preventDefault();
		errors = {};
		isLoading = true;

		try {
			const response = await api.post<any>('/v1/auth/login', formData);

			if (response.data) {
				auth.login(response.data.token, response.data.user);
				goto('/');
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', $language);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('auth.login.title', $language)} - CertVal</title>
</svelte:head>

<div class="flex min-h-screen items-center justify-center px-4 py-12 sm:px-6 lg:px-8">
	<div class="w-full max-w-md space-y-8">
		<div>
			<div class="flex justify-center">
				<svg class="h-12 w-12 text-blue-600" fill="currentColor" viewBox="0 0 24 24">
					<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z" />
				</svg>
			</div>
			<h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
				{t('auth.login.title', $language)}
			</h2>
			<p class="mt-2 text-center text-sm text-gray-600">
				{t('auth.login.noAccount', $language)}
				<a href="/auth/register" class="font-medium text-blue-600 hover:text-blue-500">
					{t('auth.login.registerLink', $language)}
				</a>
			</p>
		</div>

		<form class="mt-8 space-y-6" onsubmit={handleSubmit}>
			<div class="space-y-4">
				{#if errors.general}
					<div class="rounded-md border border-red-200 bg-red-50 p-4">
						<p class="text-sm text-red-600">{errors.general}</p>
					</div>
				{/if}

				<Input
					type="email"
					label={t('auth.login.email', $language)}
					bind:value={formData.email}
					required
					error={errors.email}
				/>

				<Input
					type="password"
					label={t('auth.login.password', $language)}
					bind:value={formData.password}
					required
					error={errors.password}
				/>
			</div>

			<div class="flex items-center justify-between">
				<div class="text-sm">
					<a href="/auth/forgot-password" class="font-medium text-blue-600 hover:text-blue-500">
						{t('auth.login.forgot', $language)}
					</a>
				</div>
			</div>

			<div>
				<Button
					type="submit"
					variant="primary"
					size="lg"
					loading={isLoading}
					disabled={isLoading}
					class="w-full"
				>
					{t('auth.login.submit', $language)}
				</Button>
			</div>
		</form>
	</div>
</div>
