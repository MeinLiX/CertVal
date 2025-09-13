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
	let mounted = $state(false);

	// Check for registration success message
	const isRegistered = $derived($page.url.searchParams.get('registered') === 'true');

	onMount(() => {
		// Redirect if already authenticated
		if ($auth.isAuthenticated) {
			goto('/');
		}
		mounted = true;
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
		if (errors.email) {
			errors.email = '';
		}
	}

	function handlePasswordInput(value: string | number) {
		formData.password = typeof value === 'string' ? value : String(value);
		if (errors.password) {
			errors.password = '';
		}
	}
</script>

<svelte:head>
	<title>{t('auth.login.title', $language)} - CertVal</title>
	<meta name="description" content="Sign in to your account to manage your SSL certificates" />
</svelte:head>

<div class="relative flex min-h-screen items-center justify-center px-4 py-12 sm:px-6 lg:px-8">
	<div class="w-full max-w-md space-y-8 {mounted ? 'animate-in slide-in-from-bottom-8 duration-700' : ''}">
		<!-- Logo and header -->
		<div class="text-center">
			<div class="flex justify-center mb-6">
				<div class="relative group">
					<div class="absolute inset-0 rounded-2xl bg-gradient-to-r from-blue-600 to-indigo-600 opacity-20 blur-xl scale-110 group-hover:opacity-30 transition-opacity duration-300"></div>
					<div class="relative flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-r from-blue-600 to-indigo-600 shadow-lg">
						<svg class="h-8 w-8 text-white" fill="currentColor" viewBox="0 0 24 24">
							<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z" />
						</svg>
					</div>
				</div>
			</div>
			
			<h1 class="text-3xl font-bold bg-gradient-to-r from-gray-900 to-gray-700 dark:from-white dark:to-gray-300 bg-clip-text text-transparent">
				{t('auth.login.title', $language)}
			</h1>
			<p class="mt-3 text-sm text-gray-600 dark:text-gray-400">
				{t('auth.login.noAccount', $language)}
				<a href="/auth/register" class="font-semibold text-blue-600 dark:text-blue-400 hover:text-blue-500 dark:hover:text-blue-300 transition-colors duration-200">
					{t('auth.login.registerLink', $language)}
				</a>
			</p>
		</div>

		<!-- Main form card -->
		<div class="relative overflow-hidden rounded-2xl bg-white/80 dark:bg-gray-900/80 backdrop-blur-xl shadow-xl border border-white/20 dark:border-gray-800/50">
			<div class="absolute inset-0 bg-gradient-to-br from-white/5 to-transparent dark:from-gray-800/5 pointer-events-none"></div>
			
			<form class="relative space-y-6 p-8" onsubmit={handleSubmit}>
				{#if isRegistered}
					<div class="rounded-xl border border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-900/30 p-4 {mounted ? 'animate-in slide-in-from-top-4 duration-500' : ''}">
						<div class="flex">
							<div class="flex-shrink-0">
								<svg class="h-5 w-5 text-green-400 dark:text-green-300" viewBox="0 0 20 20" fill="currentColor">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
								</svg>
							</div>
							<div class="ml-3">
								<p class="text-sm text-green-700 dark:text-green-300">
									Registration successful! Please check your email to confirm your account, then sign in below.
								</p>
							</div>
						</div>
					</div>
				{/if}

				{#if errors.general}
					<div class="rounded-xl border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/30 p-4 {mounted ? 'animate-in slide-in-from-top-4 duration-500' : ''}">
						<div class="flex">
							<div class="flex-shrink-0">
								<svg class="h-5 w-5 text-red-400 dark:text-red-300" viewBox="0 0 20 20" fill="currentColor">
									<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
								</svg>
							</div>
							<div class="ml-3">
								<p class="text-sm text-red-700 dark:text-red-300">{errors.general}</p>
							</div>
						</div>
					</div>
				{/if}

				<div class="space-y-5">
					<Input
						type="email"
						label={t('auth.login.email', $language)}
						value={formData.email}
						oninput={handleEmailInput}
						required
						error={errors.email}
						placeholder="your@email.com"
						icon="M3 8l7.89 7.89a1 1 0 001.415 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
					/>

					<Input
						type="password"
						label={t('auth.login.password', $language)}
						value={formData.password}
						oninput={handlePasswordInput}
						required
						error={errors.password}
						placeholder="Enter your password"
						icon="M12 15v2a6 6 0 11-6-6c0-1.657.672-3.157 1.757-4.243L7.5 6.5a1.5 1.5 0 113 0v2.257A6.958 6.958 0 0112 8a6.958 6.958 0 011.243.757V6.5a1.5 1.5 0 113 0l.257.257A5.98 5.98 0 0118 11a6 6 0 11-6 4z"
					/>
				</div>

				<div class="flex items-center justify-between">
					<div class="text-sm">
						<a href="/auth/forgot-password" class="font-medium text-blue-600 dark:text-blue-400 hover:text-blue-500 dark:hover:text-blue-300 transition-colors duration-200">
							{t('auth.login.forgot', $language)}
						</a>
					</div>
				</div>

				<div class="space-y-4">
					<Button
						type="submit"
						variant="primary"
						size="lg"
						loading={isLoading}
						disabled={isLoading || !formData.email || !formData.password}
						class="w-full"
					>
						<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1" />
						</svg>
						{t('auth.login.submit', $language)}
					</Button>

					<!-- Social login buttons (placeholder) -->
					<div class="relative">
						<div class="absolute inset-0 flex items-center">
							<div class="w-full border-t border-gray-300 dark:border-gray-700"></div>
						</div>
						<div class="relative flex justify-center text-sm">
							<span class="bg-white dark:bg-gray-900 px-2 text-gray-500 dark:text-gray-400">Or continue with</span>
						</div>
					</div>

					<div class="grid grid-cols-2 gap-3">
						<button
							type="button"
							class="flex w-full items-center justify-center rounded-xl border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm font-medium text-gray-700 dark:text-gray-200 shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors duration-200"
						>
							<svg class="mr-2 h-4 w-4" viewBox="0 0 24 24">
								<path fill="currentColor" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
								<path fill="currentColor" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
								<path fill="currentColor" d="M1 12.04C1 5.56 5.56 1 12 1s11 4.56 11 11.04c0 .75-.08 1.48-.22 2.2H12v-4.26h5.92c.26-1.37 1.04-2.53 2.21-3.31V3.86H16.5c-2.08 1.92-3.28 4.74-3.28 8.09 0 .78.07 1.53.2 2.25z"/>
							</svg>
							Google
						</button>
						<button
							type="button"
							class="flex w-full items-center justify-center rounded-xl border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm font-medium text-gray-700 dark:text-gray-200 shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors duration-200"
						>
							<svg class="mr-2 h-4 w-4" fill="currentColor" viewBox="0 0 24 24">
								<path d="M12.017 0C5.396 0 .029 5.367.029 11.987c0 5.079 3.158 9.417 7.618 11.174-.105-.949-.199-2.403.041-3.439.219-.937 1.406-5.957 1.406-5.957s-.359-.72-.359-1.781c0-1.663.967-2.911 2.168-2.911 1.024 0 1.518.769 1.518 1.688 0 1.029-.653 2.567-.992 3.992-.285 1.193.6 2.165 1.775 2.165 2.128 0 3.768-2.245 3.768-5.487 0-2.861-2.063-4.869-5.008-4.869-3.41 0-5.409 2.562-5.409 5.199 0 1.033.394 2.143.889 2.741.099.12.112.225.085.345-.09.375-.293 1.199-.334 1.363-.053.225-.172.271-.402.165-1.495-.69-2.433-2.878-2.433-4.646 0-3.776 2.748-7.252 7.92-7.252 4.158 0 7.392 2.967 7.392 6.923 0 4.135-2.607 7.462-6.233 7.462-1.214 0-2.357-.629-2.750-1.378l-.748 2.853c-.271 1.043-1.002 2.35-1.492 3.146C9.57 23.812 10.763 24.009 12.017 24.009c6.624 0 11.99-5.367 11.99-11.99C24.007 5.369 18.641.001.012.001z"/>
							</svg>
							GitHub
						</button>
					</div>
				</div>
			</form>
		</div>

		<!-- Footer -->
		<div class="text-center">
			<p class="text-xs text-gray-500 dark:text-gray-400">
				By signing in, you agree to our{' '}
				<a href="/terms" class="text-blue-600 dark:text-blue-400 hover:text-blue-500 dark:hover:text-blue-300 transition-colors duration-200">Terms of Service</a>
				{' '}and{' '}
				<a href="/privacy" class="text-blue-600 dark:text-blue-400 hover:text-blue-500 dark:hover:text-blue-300 transition-colors duration-200">Privacy Policy</a>
			</p>
		</div>
	</div>
</div>