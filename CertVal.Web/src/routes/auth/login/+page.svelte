<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import type { LoginRequest } from '$lib/types';

	let formData = $state<LoginRequest>({ email: '', password: '' });
	let errors = $state<Record<string, string>>({});
	let isLoading = $state(false);

	const isRegistered = $derived($page.url.searchParams.get('registered') === 'true');

	onMount(() => {
		if ($auth.isAuthenticated) {
			goto('/');
		}
	});
	async function handleSubmit(event: Event) {
		event.preventDefault();
		errors = {};
		isLoading = true;

		try {
			const response = await api.post<any>('/v1/auth/login', formData);
			if (response.data) {
				auth.login(response.data.token, response.data.user);
				goto('/');
			} else {
				errors.general = response.message || 'Invalid credentials.';
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

<div class="hero min-h-screen bg-base-200">
	<div class="hero-content w-full max-w-4xl flex-col lg:flex-row-reverse">
		<div class="text-center lg:pl-10 lg:text-left">
			<h1 class="text-5xl font-bold">{t('auth.login.welcome', $language)}</h1>
			<p class="py-6">
				{t('auth.login.tagline', $language)}
			</p>
		</div>
		<div class="card w-full max-w-sm shrink-0 bg-base-100 shadow-2xl">
			<form class="card-body" onsubmit={handleSubmit}>
				<h2 class="mb-4 text-center text-2xl font-bold">{t('auth.login.title', $language)}</h2>

				{#if isRegistered}
					<div role="alert" class="alert alert-success text-sm">
						<span>{t('auth.login.registrationSuccess', $language)}</span>
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
					bind:value={formData.email}
					required
					placeholder="your@email.com"
				/>
				<Input
					label={t('auth.login.password', $language)}
					type="password"
					bind:value={formData.password}
					required
					placeholder={t('auth.login.password', $language)}
				/>
				<div class="mt-2 flex items-center justify-between text-sm">
					<label class="label cursor-pointer gap-2">
						<input type="checkbox" class="checkbox checkbox-sm checkbox-primary" />
						<span class="label-text">{t('auth.login.rememberMe', $language)}</span>
					</label>
					<a href="/auth/forgot-password" class="link text-sm link-hover">
						{t('auth.login.forgot', $language)}
					</a>
				</div>
				<div class="form-control mt-6">
					<Button type="submit" variant="primary" loading={isLoading}>
						{t('auth.login.submit', $language)}
					</Button>
				</div>
				<div class="mt-4 text-center text-sm">
					<p>
						{t('auth.login.noAccount', $language)}
						<a href="/auth/register" class="link link-primary"
							>{t('auth.login.registerLink', $language)}</a
						>
					</p>
				</div>
			</form>
		</div>
	</div>
</div>
