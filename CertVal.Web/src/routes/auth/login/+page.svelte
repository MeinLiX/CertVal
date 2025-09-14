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

<div class="hero min-h-screen">
	<div class="hero-content flex-col lg:flex-row-reverse max-w-6xl w-full">
		<!-- Hero Text Section -->
		<div class="text-center lg:text-left lg:w-1/2">
			<h1 class="text-5xl font-bold bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
				Welcome Back!
			</h1>
			<p class="py-6 text-base-content/70 text-lg">
				Secure certificate management made simple. Monitor your SSL/TLS certificates with ease and never miss an expiration again.
			</p>
			<div class="hidden lg:block">
				<div class="stats shadow border border-base-300">
					<div class="stat">
						<div class="stat-figure text-primary">
							<svg class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
							</svg>
						</div>
						<div class="stat-title">Certificates</div>
						<div class="stat-value text-primary">Monitored</div>
					</div>
					<div class="stat">
						<div class="stat-figure text-secondary">
							<svg class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
							</svg>
						</div>
						<div class="stat-title">Alerts</div>
						<div class="stat-value text-secondary">Real-time</div>
					</div>
				</div>
			</div>
		</div>

		<!-- Login Form Section -->
		<div class="card flex-shrink-0 w-full max-w-md shadow-2xl bg-base-100 lg:w-1/2 {mounted ? 'animate-in slide-in-from-bottom-8 duration-700' : ''}">
			<div class="card-body">
				<!-- Logo and header -->
				<div class="text-center mb-6">
					<div class="avatar placeholder mb-4">
						<div class="bg-primary text-primary-content rounded-2xl w-16">
							<svg class="h-8 w-8" fill="currentColor" viewBox="0 0 24 24">
								<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z" />
							</svg>
						</div>
					</div>
					
					<h2 class="text-2xl font-bold text-base-content">
						{t('auth.login.title', $language)}
					</h2>
					<p class="text-base-content/60 mt-2">
						{t('auth.login.noAccount', $language)}
						<a href="/auth/register" class="link link-primary font-semibold">
							{t('auth.login.registerLink', $language)}
						</a>
					</p>
				</div>

				<form onsubmit={handleSubmit} class="space-y-4">
					{#if isRegistered}
						<div class="alert alert-success {mounted ? 'animate-in slide-in-from-top-4 duration-500' : ''}">
							<svg class="h-6 w-6 stroke-current shrink-0" fill="none" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
							</svg>
							<span>Registration successful! Please check your email to confirm your account, then sign in below.</span>
						</div>
					{/if}

					{#if errors.general}
						<div class="alert alert-error {mounted ? 'animate-in slide-in-from-top-4 duration-500' : ''}">
							<svg class="h-6 w-6 stroke-current shrink-0" fill="none" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"></path>
							</svg>
							<span>{errors.general}</span>
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

					<div class="flex items-center justify-between">
						<div class="form-control">
							<label class="label cursor-pointer">
								<input type="checkbox" class="checkbox checkbox-primary checkbox-sm" />
								<span class="label-text ml-2">{t('auth.login.rememberMe', $language)}</span>
							</label>
						</div>
						<div>
							<a href="/auth/forgot-password" class="link link-primary text-sm">
								{t('auth.login.forgot', $language)}
							</a>
						</div>
					</div>

					<Button
						type="submit"
						variant="primary"
						size="lg"
						loading={isLoading}
						disabled={isLoading || !formData.email || !formData.password}
						wide={true}
					>
						<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 16l-4-4m0 0l4-4m-4 4h14m-5 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h7a3 3 0 013 3v1" />
						</svg>
						{t('auth.login.submit', $language)}
					</Button>

					<!-- Social login buttons -->
					<div class="divider">Or continue with</div>

					<div class="grid grid-cols-2 gap-3">
						<Button variant="outline" class="btn-block">
							<svg class="h-5 w-5" viewBox="0 0 24 24">
								<path fill="currentColor" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
								<path fill="currentColor" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
								<path fill="currentColor" d="M1 12.04C1 5.56 5.56 1 12 1s11 4.56 11 11.04c0 .75-.08 1.48-.22 2.2H12v-4.26h5.92c.26-1.37 1.04-2.53 2.21-3.31V3.86H16.5c-2.08 1.92-3.28 4.74-3.28 8.09 0 .78.07 1.53.2 2.25z"/>
							</svg>
							Google
						</Button>
						<Button variant="outline" class="btn-block">
							<svg class="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
								<path d="M12.017 0C5.396 0 .029 5.367.029 11.987c0 5.079 3.158 9.417 7.618 11.174-.105-.949-.199-2.403.041-3.439.219-.937 1.406-5.957 1.406-5.957s-.359-.72-.359-1.781c0-1.663.967-2.911 2.168-2.911 1.024 0 1.518.769 1.518 1.688 0 1.029-.653 2.567-.992 3.992-.285 1.193.6 2.165 1.775 2.165 2.128 0 3.768-2.245 3.768-5.487 0-2.861-2.063-4.869-5.008-4.869-3.41 0-5.409 2.562-5.409 5.199 0 1.033.394 2.143.889 2.741.099.12.112.225.085.345-.09.375-.293 1.199-.334 1.363-.053.225-.172.271-.402.165-1.495-.69-2.433-2.878-2.433-4.646 0-3.776 2.748-7.252 7.92-7.252 4.158 0 7.392 2.967 7.392 6.923 0 4.135-2.607 7.462-6.233 7.462-1.214 0-2.357-.629-2.750-1.378l-.748 2.853c-.271 1.043-1.002 2.35-1.492 3.146C9.57 23.812 10.763 24.009 12.017 24.009c6.624 0 11.99-5.367 11.99-11.99C24.007 5.369 18.641.001.012.001z"/>
							</svg>
							GitHub
						</Button>
					</div>
				</form>

				<!-- Footer -->
				<div class="text-center mt-6">
					<p class="text-xs text-base-content/60">
						By signing in, you agree to our{' '}
						<a href="/terms" class="link link-primary">Terms of Service</a>
						{' '}and{' '}
						<a href="/privacy" class="link link-primary">Privacy Policy</a>
					</p>
				</div>
			</div>
		</div>
	</div>
</div>