<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
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
	let googleReady = $state(false);

	const isRegistered = $derived(page.url.searchParams.get('registered') === 'true');
	const redirectUrl = $derived(page.url.searchParams.get('redirect') || '/dashboard');

	onMount(() => {
		if ($auth.isAuthenticated) {
			goto('/dashboard');
		}
		loadGoogleScript($language);
	});

	$effect(() => {
		loadGoogleScript($language);
	});

	function loadGoogleScript(currentLang: 'uk' | 'en') {
		const scriptId = 'google-identity-script';
		const existing = document.getElementById(scriptId) as HTMLScriptElement | null;
		const desiredSrc = `https://accounts.google.com/gsi/client?hl=${currentLang}`;

		if (existing && existing.src === desiredSrc) {
			googleReady = true;
			initGoogleButton();
			return;
		}

		if (existing) {
			existing.remove();
			const container = document.getElementById('googleSignInDiv');
			if (container) container.innerHTML = '';
		}

		const s = document.createElement('script');
		s.src = desiredSrc;
		s.async = true;
		s.defer = true;
		s.id = scriptId;
		s.onload = () => {
			googleReady = true;
			initGoogleButton();
		};
		document.head.appendChild(s);
	}

	async function handleSubmit(event: Event) {
		event.preventDefault();
		errors = {};
		isLoading = true;

		try {
			const response = await api.post<any>('/v1/auth/login', formData);
			if (response.data) {
				auth.login(response.data.token, response.data.user);
				goto(redirectUrl);
			} else {
				errors.general = response.message || 'Invalid credentials.';
			}
		} catch (error) {
			errors.general = t('errors.network', $language);
		} finally {
			isLoading = false;
		}
	}

	function initGoogleButton() {
		errors = {};
		const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string;
		if (!clientId) {
			errors.general = t('errors.googleClientIdMissing', $language);
			return;
		}
		if (!window.google?.accounts?.id) return;

		window.google.accounts.id.initialize({
			client_id: clientId,
			auto_select: false,
			ux_mode: 'popup',
			callback: onGoogleCredential
		});

		const btn = document.getElementById('googleSignInDiv');
		if (btn) {
			window.google.accounts.id.renderButton(btn, {
				type: 'standard',
				theme: 'outline',
				size: 'large',
				shape: 'rectangular',
				text: 'signin_with',
				logo_alignment: 'left',
				width: 320
			});
		}

		window.google.accounts.id.prompt();
	}

	async function onGoogleCredential(resp: any) {
		try {
			const idToken = resp?.credential as string | undefined;
			if (!idToken) {
				errors.general = t('errors.googleLoginFailed', $language);
				return;
			}
			const result = await api.post<any>('/v1/auth/login/google', { idToken });
			if (result.data) {
				auth.login(result.data.token, result.data.user);
				goto(redirectUrl);
			} else {
				errors.general = result.message || t('errors.googleLoginFailed', $language);
			}
		} catch {
			errors.general = t('errors.network', $language);
		}
	}
</script>

<svelte:head>
	<title>{t('auth.login.title', $language)} - CertVal</title>
</svelte:head>

<div class="hero min-h-full">
	<div class="hero-content w-full max-w-4xl flex-col lg:flex-row-reverse">
		<div class="text-center lg:pl-10 lg:text-left">
			<h1 class="text-5xl font-bold">{t('auth.login.welcome', $language)}</h1>
			<p class="py-6 text-lg opacity-80">{t('auth.login.tagline', $language)}</p>
		</div>
		<div
			class="card glass w-full max-w-sm shrink-0 shadow-2xl"
			style="background-color: oklch(from var(--color-base-100) l c h / 0.2);"
		>
			<form class="card-body p-8" onsubmit={handleSubmit}>
				<h2 class="card-title mb-4 justify-center text-2xl font-bold">
					{t('auth.login.title', $language)}
				</h2>

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
					<label class="label cursor-pointer gap-2 p-0">
						<input type="checkbox" class="checkbox checkbox-sm checkbox-primary" />
						<span class="label-text">{t('auth.login.rememberMe', $language)}</span>
					</label>
					<a href="/auth/forgot-password" class="link link-hover text-sm">
						{t('auth.login.forgot', $language)}
					</a>
				</div>
				<div class="form-control mt-6">
					<Button type="submit" variant="primary" loading={isLoading}>
						{t('auth.login.submit', $language)}
					</Button>
				</div>
				<div class="mt-3 flex items-center justify-center">
					<div id="googleSignInDiv" aria-label="Sign in with Google"></div>
				</div>
				<div class="divider text-xs"></div>
				<div class="text-center text-sm">
					<p>
						{t('auth.login.noAccount', $language)}
						<a href="/auth/register" class="link link-primary font-semibold"
							>{t('auth.login.registerLink', $language)}</a
						>
					</p>
				</div>
			</form>
		</div>
	</div>
</div>
