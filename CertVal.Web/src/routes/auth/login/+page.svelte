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

	let formData = $state<LoginRequest>({
		email: '',
		password: ''
	});

	let errors = $state<Record<string, string>>({});
	let isLoading = $state(false);

	// Check for registration success message
	const isRegistered = $derived($page.url.searchParams.get('registered') === 'true');

	onMount(() => {
		// Redirect if already authenticated
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
			} else if (response.message) {
				errors.general = response.message;
			}
		} catch (error) {
			errors.general = t('errors.network', $language);
		} finally {
			isLoading = false;
		}
	}

	function handleEmailInput(value: string | number) {
		formData.email = typeof value === 'string' ? value : String(value);
		// Clear email error when user starts typing
		if (errors.email) {
			errors.email = '';
		}
	}

	function handlePasswordInput(value: string | number) {
		formData.password = typeof value === 'string' ? value : String(value);
		// Clear password error when user starts typing
		if (errors.password) {
			errors.password = '';
		}
	}
</script>

<svelte:head>
	<title>{t('auth.login.title', $language)}</title>
	<meta name="description" content="Sign in to your account to manage your SSL certificates" />
</svelte:head>

<div class="flex min-h-screen items-center justify-center px-4 py-12 sm:px-6 lg:px-8">
	<div class="w-full max-w-md space-y-8">
		<div class="text-center">
			<div class="flex justify-center">
				<svg class="h-12 w-12 text-blue-600" fill="currentColor" viewBox="0 0 24 24">
					<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z" />
				</svg>
			</div>
			<h1 class="mt-6 text-center text-3xl font-bold tracking-tight text-gray-900">
				{t('auth.login.title', $language)}
			</h1>
			<p class="mt-2 text-center text-sm text-gray-600">
				{t('auth.login.noAccount', $language)}
				<a href="/auth/register" class="font-medium text-blue-600 hover:text-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded">
					{t('auth.login.registerLink', $language)}
				</a>
			</p>
		</div>

		<form class="mt-8 space-y-6" onsubmit={handleSubmit}>
			<div class="space-y-4">
				{#if isRegistered}
					<div class="rounded-md border border-green-200 bg-green-50 p-4">
						<div class="flex">
							<svg class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
								<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
							</svg>
							<div class="ml-3">
								<p class="text-sm text-green-600">
									Registration successful! Please check your email to confirm your account, then sign in below.
								</p>
							</div>
						</div>
					</div>
				{/if}

				{#if errors.general}
					<div class="rounded-md border border-red-200 bg-red-50 p-4">
						<div class="flex">
							<svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
								<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
							</svg>
							<div class="ml-3">
								<p class="text-sm text-red-600">{errors.general}</p>
							</div>
						</div>
					</div>
				{/if}

				<Input
					type="email"
					label={t('auth.login.email', $language)}
					value={formData.email}
					oninput={handleEmailInput}
					required
					error={errors.email}
					placeholder="your@email.com"
				/>

				<Input
					type="password"
					label={t('auth.login.password', $language)}
					value={formData.password}
					oninput={handlePasswordInput}
					required
					error={errors.password}
					placeholder="Enter your password"
				/>
			</div>

			<div class="flex items-center justify-between">
				<div class="text-sm">
					<a href="/auth/forgot-password" class="font-medium text-blue-600 hover:text-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded">
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
					disabled={isLoading || !formData.email || !formData.password}
					class="w-full"
				>
					{t('auth.login.submit', $language)}
				</Button>
			</div>
		</form>

		<div class="text-center text-xs text-gray-500">
			<p>
				By signing in, you agree to our 
				<a href="/terms" class="text-blue-600 hover:text-blue-500">Terms of Service</a>
				and 
				<a href="/privacy" class="text-blue-600 hover:text-blue-500">Privacy Policy</a>
			</p>
		</div>
	</div>
</div>