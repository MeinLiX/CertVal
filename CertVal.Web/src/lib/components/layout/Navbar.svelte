<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { t } from '$lib/i18n';
	import type { Language } from '$lib/types';
	import { goto } from '$app/navigation';

	let showUserMenu = $state(false);
	let showLangMenu = $state(false);

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

	// Close menus when clicking outside
	function handleDocumentClick() {
		showUserMenu = false;
		showLangMenu = false;
	}

	// Derived values
	const currentLanguageLabel = $derived(
		$language === 'uk' ? 'Українська' : 'English'
	);

	const userInitials = $derived(
		($auth.user?.firstName ?? '').charAt(0) + ($auth.user?.lastName ?? '').charAt(0)
	);

	const userDisplayName = $derived(
		$auth.user?.fullName ?? $auth.user?.email ?? ''
	);

	onMount(() => {
		document.addEventListener('click', handleDocumentClick);
		return () => {
			document.removeEventListener('click', handleDocumentClick);
		};
	});
</script>

<nav class="fixed top-0 z-50 w-full border-b border-gray-200 bg-white shadow-sm">
	<div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
		<div class="flex h-16 items-center justify-between">
			<div class="flex items-center">
				<div class="flex flex-shrink-0 items-center">
					<a href="/" class="flex items-center">
						<svg
							class="h-8 w-8 text-blue-600"
							fill="currentColor"
							viewBox="0 0 24 24"
							aria-hidden="true"
						>
							<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z" />
						</svg>
						<span class="ml-2 text-xl font-bold text-gray-900">CertVal</span>
					</a>
				</div>
			</div>

			<div class="flex items-center space-x-4">
				<!-- Language selector -->
				<div class="relative">
					<button
						class="flex items-center space-x-2 rounded-md px-3 py-2 text-sm font-medium text-gray-600 hover:text-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
						onclick={(e) => {
							e.stopPropagation();
							showLangMenu = !showLangMenu;
						}}
						aria-haspopup="true"
						aria-expanded={showLangMenu}
					>
						<span class="text-sm">{currentLanguageLabel}</span>
						<svg
							class="h-4 w-4 text-gray-500 transition-transform {showLangMenu ? 'rotate-180' : ''}"
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
						<div class="ring-opacity-5 absolute right-0 z-50 mt-2 w-40 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black">
							<div class="py-1">
								{#each languages as lang}
									<button
										class="w-full px-4 py-2 text-left text-sm text-gray-700 hover:bg-gray-100 focus:bg-gray-100 focus:outline-none"
										onclick={(e) => {
											e.stopPropagation();
											switchLanguage(lang.code);
										}}
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
							class="flex items-center space-x-2 rounded-md px-3 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
							onclick={(e) => {
								e.stopPropagation();
								showUserMenu = !showUserMenu;
							}}
							aria-haspopup="true"
							aria-expanded={showUserMenu}
						>
							<div
								class="flex h-8 w-8 items-center justify-center rounded-full bg-gray-200 text-sm font-medium text-gray-700"
							>
								{userInitials}
							</div>
							<span class="hidden sm:inline">{userDisplayName}</span>
							<svg
								class="h-4 w-4 text-gray-500 transition-transform {showUserMenu ? 'rotate-180' : ''}"
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

						{#if showUserMenu}
							<div class="ring-opacity-5 absolute right-0 z-50 mt-2 w-48 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-black">
								<div class="py-1">
									<a 
										href="/profile" 
										class="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 focus:bg-gray-100 focus:outline-none"
									>
										{t('nav.profile', $language)}
									</a>
									<button
										class="w-full px-4 py-2 text-left text-sm text-gray-700 hover:bg-gray-100 focus:bg-gray-100 focus:outline-none"
										onclick={(e) => {
											e.stopPropagation();
											handleLogout();
										}}
									>
										{t('nav.logout', $language)}
									</button>
								</div>
							</div>
						{/if}
					{:else}
						<div class="flex items-center space-x-2">
							<a class="text-sm text-gray-600 hover:text-gray-800 focus:text-gray-800 focus:outline-none" href="/auth/login">
								{t('nav.login', $language)}
							</a>
							<span class="text-gray-300">|</span>
							<a class="text-sm text-gray-600 hover:text-gray-800 focus:text-gray-800 focus:outline-none" href="/auth/register">
								{t('nav.register', $language)}
							</a>
						</div>
					{/if}
				</div>
			</div>
		</div>
	</div>
</nav>