<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { t } from '$lib/i18n';
	import type { Language } from '$lib/types';
	import { goto } from '$app/navigation';

	let showUserMenu = false;
	let showLangMenu = false;

	const languages: { code: Language; label: string }[] = [
		{ code: 'uk', label: 'Українська' },
		{ code: 'en', label: 'English' }
	];

	onMount(() => {
		// initialize stores from localStorage if needed
		language.initialize?.();
		auth.initialize?.();
	});

	function handleLogout() {
		auth.logout();
		goto('/auth/login');
	}

	function switchLanguage(lang: Language) {
		language.setLanguage(lang);
		showLangMenu = false;
	}
</script>

<nav class="fixed top-0 z-50 w-full border-b border-gray-200 bg-white shadow-sm">
	<div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
		<div class="flex h-16 items-center justify-between">
			<div class="flex items-center">
				<div class="flex flex-shrink-0 items-center">
					<svg
						class="h-8 w-8 text-blue-600"
						fill="currentColor"
						viewBox="0 0 24 24"
						aria-hidden="true"
					>
						<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z" />
					</svg>
					<span class="ml-2 text-xl font-bold text-gray-900">CertVal</span>
				</div>
			</div>

			<div class="flex items-center space-x-4">
				<!-- Language selector -->
				<div class="relative">
					<button
						class="flex items-center space-x-2 rounded-md px-3 py-2 text-sm font-medium text-gray-600 hover:text-gray-800"
						on:click={() => (showLangMenu = !showLangMenu)}
						aria-haspopup="true"
						aria-expanded={showLangMenu}
					>
						<span class="text-sm">
							{#if $language === 'uk'}Українська{/if}
							{#if $language === 'en'}English{/if}
						</span>
						<svg
							class="h-4 w-4 text-gray-500"
							viewBox="0 0 20 20"
							fill="currentColor"
							aria-hidden="true"
						>
							<path
								fill-rule="evenodd"
								d="M5.23 7.21a.75.75 0 011.06.02L10 10.94l3.71-3.71a.75.75 0 111.06 1.06l-4.24 4.24a.75.75 0 01-1.06 0L5.21 8.29a.75.75 0 01.02-1.08z"
								clip-rule="evenodd"
							/>
						</svg>
					</button>

					{#if showLangMenu}
						<div
							class="ring-opacity-5 absolute right-0 z-50 mt-2 w-40 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black"
						>
							<div class="py-1">
								{#each languages as lang}
									<button
										class="w-full px-4 py-2 text-left text-sm text-gray-700 hover:bg-gray-100"
										on:click={() => switchLanguage(lang.code)}
									>
										{lang.label}
									</button>
								{/each}
							</div>
						</div>
					{/if}
				</div>

				<!-- User menu -->
				<div class="relative">
					{#if $auth.isAuthenticated}
						<button
							class="flex items-center space-x-2 rounded-md px-3 py-2 text-sm font-medium text-gray-700 hover:text-gray-900"
							on:click={() => (showUserMenu = !showUserMenu)}
							aria-haspopup="true"
							aria-expanded={showUserMenu}
						>
							<div
								class="flex h-8 w-8 items-center justify-center rounded-full bg-gray-200 text-sm font-medium text-gray-700"
							>
								{($auth.user?.firstName ?? '').charAt(0) + ($auth.user?.lastName ?? '').charAt(0)}
							</div>
							<span class="hidden sm:inline">{$auth.user?.fullName ?? $auth.user?.email}</span>
							<svg
								class="h-4 w-4 text-gray-500"
								viewBox="0 0 20 20"
								fill="currentColor"
								aria-hidden="true"
							>
								+ <path
									fill-rule="evenodd"
									d="M5.23 7.21a.75.75 0 011.06.02L10 10.94l3.71-3.71a.75.75 0 111.06 1.06l-4.24 4.24a.75.75 0 01-1.06 0L5.21 8.29a.75.75 0 01.02-1.08z"
									clip-rule="evenodd"
								/>
							</svg>
						</button>

						{#if showUserMenu}
							<div
								class="ring-opacity-5 absolute right-0 z-50 mt-2 w-48 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black"
							>
								<div class="py-1">
									<a href="/" class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
										>{t('nav.profile', $language)}</a
									>
									<button
										class="w-full px-4 py-2 text-left text-sm text-gray-700 hover:bg-gray-100"
										on:click={handleLogout}>{t('nav.logout', $language)}</button
									>
								</div>
							</div>
						{/if}
					{:else}
						<div class="flex items-center space-x-2">
							+ <a class="text-sm text-gray-600 hover:text-gray-800" href="/auth/login"
								>{t('nav.login', $language)}</a
							>
							<a class="text-sm text-gray-600 hover:text-gray-800" href="/auth/register"
								>{t('nav.register', $language)}</a
							>
						</div>
					{/if}
				</div>
			</div>
		</div>
	</div>
</nav>
