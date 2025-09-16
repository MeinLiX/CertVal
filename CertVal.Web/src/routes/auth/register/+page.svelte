<script lang="ts">
	import { goto } from '$app/navigation';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import type { CreateUserRequest } from '$lib/types';

	let formData = $state<CreateUserRequest & { confirmPassword: string }>({
		firstName: '',
		lastName: '',
		email: '',
		password: '',
		confirmPassword: '',
		language: $language,
		timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone
	});
	let errors = $state<Record<string, string>>({});
	let isLoading = $state(false);

	async function handleSubmit(event: Event) {
		event.preventDefault();
		errors = {};
		if (formData.password !== formData.confirmPassword) {
			errors.confirmPassword = t('errors.passwordsNotMatch', $language);
			return;
		}

		isLoading = true;
		try {
			const { confirmPassword, ...userData } = formData;
			const response = await api.post<any>('/v1/auth/register', userData);
			if (response.data) {
				goto('/auth/login?registered=true');
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
	<title>{t('auth.register.title', $language)} - CertVal</title>
</svelte:head>

<div class="hero min-h-screen bg-base-200">
	<div class="hero-content w-full max-w-4xl flex-col lg:flex-row-reverse">
		<div class="text-center lg:pl-10 lg:text-left">
			<h1 class="text-5xl font-bold">{t('auth.register.welcome', $language)}</h1>
			<p class="py-6">{t('auth.register.tagline', $language)}</p>
		</div>
		<div class="card w-full max-w-sm shrink-0 bg-base-100 shadow-2xl">
			<form class="card-body" onsubmit={handleSubmit}>
				<h2 class="mb-4 text-center text-2xl font-bold">{t('auth.register.title', $language)}</h2>

				{#if errors.general}
					<div role="alert" class="alert alert-error text-sm">
						<span>{errors.general}</span>
					</div>
				{/if}

				<div class="grid grid-cols-2 gap-4">
					<Input
						label={t('auth.register.firstName', $language)}
						bind:value={formData.firstName}
						required
					/>
					<Input
						label={t('auth.register.lastName', $language)}
						bind:value={formData.lastName}
						required
					/>
				</div>
				<Input
					label={t('auth.register.email', $language)}
					type="email"
					bind:value={formData.email}
					required
				/>
				<Input
					label={t('auth.register.password', $language)}
					type="password"
					bind:value={formData.password}
					required
				/>
				<Input
					label={t('auth.register.confirmPassword', $language)}
					type="password"
					bind:value={formData.confirmPassword}
					required
					error={errors.confirmPassword}
				/>

				<div class="form-control mt-6">
					<Button type="submit" variant="primary" loading={isLoading}>
						{t('auth.register.submit', $language)}
					</Button>
				</div>
				<div class="mt-4 text-center text-sm">
					<p>
						{t('auth.register.hasAccount', $language)}
						<a href="/auth/login" class="link link-primary"
							>{t('auth.register.loginLink', $language)}</a
						>
					</p>
				</div>
			</form>
		</div>
	</div>
</div>
