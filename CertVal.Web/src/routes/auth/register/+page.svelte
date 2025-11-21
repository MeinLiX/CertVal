<script lang="ts">
	import { authUiState } from '$lib/stores/authUiState.svelte';
	import { AuthService } from '$lib/services/AuthService';
	import FloatingInput from '$lib/components/ui/FloatingInput.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { goto } from '$app/navigation';
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';

	let firstName = $state('');
	let lastName = $state('');
	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let loading = $state(false);
	let error = $state<string | null>(null);

	$effect(() => {
		authUiState.isTyping =
			firstName.length > 0 || lastName.length > 0 || email.length > 0 || password.length > 0;
		authUiState.isValid =
			email.includes('@') &&
			password.length >= 6 &&
			password === confirmPassword &&
			firstName.length > 0 &&
			lastName.length > 0;
	});

	async function handleRegister(e: Event) {
		e.preventDefault();
		loading = true;
		error = null;

		if (password !== confirmPassword) {
			error = 'Passwords do not match';
			loading = false;
			return;
		}

		try {
			const result = await AuthService.register({ email, firstName, lastName, password });
			if (result.data) {
				goto('/auth/login?registered=true');
			} else {
				error = result.error || 'Registration failed';
			}
		} catch (err) {
			error = 'An unexpected error occurred';
		} finally {
			loading = false;
		}
	}
</script>

<div class="w-full max-w-lg" data-test-id="register-page">
	<div
		class="card bg-base-100/20 overflow-hidden border border-white/20 shadow-2xl backdrop-blur-xl"
	>
		<div class="card-body gap-6 p-8">
			<div class="mb-2 text-center">
				<h1
					class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-3xl font-bold text-transparent"
				>
					{t('auth.register.title', language.current)}
				</h1>
				<p class="text-base-content/60 mt-2">{t('auth.register.tagline', language.current)}</p>
			</div>

			<form onsubmit={handleRegister} class="flex flex-col gap-4">
				<div class="grid grid-cols-2 gap-4">
					<FloatingInput
						id="firstName"
						label={t('auth.register.firstName', language.current)}
						bind:value={firstName}
						required
						data-test-id="register-firstname"
					/>
					<FloatingInput
						id="lastName"
						label={t('auth.register.lastName', language.current)}
						bind:value={lastName}
						required
						data-test-id="register-lastname"
					/>
				</div>

				<FloatingInput
					id="email"
					label={t('auth.register.email', language.current)}
					type="email"
					bind:value={email}
					required
					data-test-id="register-email"
				/>

				<FloatingInput
					id="password"
					label={t('auth.register.password', language.current)}
					type="password"
					bind:value={password}
					required
					data-test-id="register-password"
				/>

				<FloatingInput
					id="confirmPassword"
					label={t('auth.register.confirmPassword', language.current)}
					type="password"
					bind:value={confirmPassword}
					required
					data-test-id="register-confirm-password"
				/>

				{#if error}
					<div class="alert alert-error rounded-lg py-2 text-sm">
						<span>{error}</span>
					</div>
				{/if}

				<Button
					type="submit"
					variant="primary"
					class="mt-2 w-full"
					{loading}
					disabled={!authUiState.isValid}
					data-test-id="register-submit-button"
				>
					{t('auth.register.submit', language.current)}
				</Button>
			</form>

			<div class="mt-4 text-center text-sm">
				<span class="text-base-content/60">{t('auth.register.haveAccount', language.current)}</span>
				<button
					type="button"
					onclick={() => goto('/auth/login')}
					class="link link-primary ml-1 font-medium no-underline hover:underline"
					data-test-id="register-login-link"
				>
					{t('auth.register.loginLink', language.current)}
				</button>
			</div>
		</div>
	</div>
</div>
