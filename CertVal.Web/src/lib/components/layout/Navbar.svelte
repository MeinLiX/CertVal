<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { theme } from '$lib/stores/theme';
	import { t } from '$lib/i18n';
	import type { Language, Theme } from '$lib/types';
	import { goto } from '$app/navigation';

	let showUserMenu = $state(false);
	let showLangMenu = $state(false);
	let showThemeMenu = $state(false);

	const languages: { code: Language; label: string; flag: string }[] = [
		{ code: 'uk', label: 'Українська', flag: '🇺🇦' },
		{ code: 'en', label: 'English', flag: '🇺🇸' }
	];

	const themes: { value: Theme; label: string; icon: string }[] = [
		{ value: 'light', label: 'Light', icon: '☀️' },
		{ value: 'dark', label: 'Dark', icon: '🌙' },
		{ value: 'system', label: 'System', icon: '💻' }
	];

	onMount(() => {
		language.initialize?.();
		auth.initialize?.();
		theme.initialize?.();
	});

	function switchLanguage(lang: Language) {
		language.setLanguage(lang);
		showLangMenu = false;
	}

	function switchTheme(newTheme: Theme) {
		theme.setTheme(newTheme);
		showThemeMenu = false;
	}

	function handleDocumentClick() {
		showUserMenu = false;
		showLangMenu = false;
		showThemeMenu = false;
	}

	const currentLanguageLabel = $derived(
		$language === 'uk' ? 'Українська' : 'English'
	);

	const currentLanguageFlag = $derived(
		$language === 'uk' ? '🇺🇦' : '🇺🇸'
	);

	const currentTheme = $derived($theme);

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

<nav class="fixed top-0 z-50 w-full border-b border-gray-200/50 bg-white/80 backdrop-blur-xl supports-[backdrop-filter]:bg-white/80 dark:border-gray-800/50 dark:bg-gray-950/80 dark:supports-[backdrop-filter]:bg-gray-950/80 transition-all duration-200">
	<div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
		<div class="flex h-16 items-center justify-between">
			<div class="flex items-center">
				<div class="flex flex-shrink-0 items-center">
					<a href="/" class="flex items-center group">
						<div class="relative">
							<svg
								class="h-8 w-8 text-blue-600 dark:text-blue-400 transition-all duration-300 group-hover:scale-110"
								fill="currentColor"
								viewBox="0 0 24 24"
								aria-hidden="true"
							>
								<path d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4z" />
							</svg>
							<div class="absolute inset-0 rounded-full bg-blue-400 opacity-20 scale-0 group-hover:scale-150 transition-all duration-500 blur-md"></div>
						</div>
						<span class="ml-3 text-xl font-bold bg-gradient-to-r from-gray-900 to-gray-700 dark:from-white dark:to-gray-300 bg-clip-text text-transparent transition-all duration-300">
							CertVal
						</span>
					</a>
				</div>
			</div>

			<div class="flex items-center space-x-2">
				<!-- Theme selector -->
				<div class="relative">
					<button
						class="flex items-center space-x-2 rounded-xl px-3 py-2 text-sm font-medium text-gray-600 hover:text-gray-800 hover:bg-gray-100 dark:text-gray-300 dark:hover:text-white dark:hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900 transition-all duration-200"
						onclick={(e) => {
							e.stopPropagation();
							showThemeMenu = !showThemeMenu;
						}}
						aria-haspopup="true"
						aria-expanded={showThemeMenu}
					>
						<span class="text-lg">{themes.find(t => t.value === currentTheme.theme)?.icon}</span>
						<svg
							class="h-4 w-4 text-gray-400 transition-all duration-200 {showThemeMenu ? 'rotate-180' : ''}"
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

					{#if showThemeMenu}
						<div class="absolute right-0 z-50 mt-2 w-48 origin-top-right rounded-xl bg-white dark:bg-gray-900 shadow-lg ring-1 ring-black ring-opacity-5 dark:ring-gray-700 border-0 dark:border dark:border-gray-800 animate-in slide-in-from-top-2 duration-200">
							<div class="py-2">
								{#each themes as themeOption}
									<button
										class="w-full flex items-center px-4 py-2.5 text-left text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 focus:bg-gray-50 dark:focus:bg-gray-800 focus:outline-none transition-colors duration-150 {currentTheme.theme === themeOption.value ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400' : ''}"
										onclick={(e) => {
											e.stopPropagation();
											switchTheme(themeOption.value);
										}}
									>
										<span class="mr-3 text-lg">{themeOption.icon}</span>
										<span class="flex-1">{themeOption.label}</span>
										{#if currentTheme.theme === themeOption.value}
											<svg class="h-4 w-4 text-blue-600 dark:text-blue-400" fill="currentColor" viewBox="0 0 20 20">
												<path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
											</svg>
										{/if}
									</button>
								{/each}
							</div>
						</div>
					{/if}
				</div>

				<!-- Language selector -->
				<div class="relative">
					<button
						class="flex items-center space-x-2 rounded-xl px-3 py-2 text-sm font-medium text-gray-600 hover:text-gray-800 hover:bg-gray-100 dark:text-gray-300 dark:hover:text-white dark:hover:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900 transition-all duration-200"
						onclick={(e) => {
							e.stopPropagation();
							showLangMenu = !showLangMenu;
						}}
						aria-haspopup="true"
						aria-expanded={showLangMenu}
					>
						<span class="text-lg">{currentLanguageFlag}</span>
						<span class="text-sm font-medium">{currentLanguageLabel}</span>
						<svg
							class="h-4 w-4 text-gray-400 transition-all duration-200 {showLangMenu ? 'rotate-180' : ''}"
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
						<div class="absolute right-0 z-50 mt-2 w-48 origin-top-right rounded-xl bg-white dark:bg-gray-900 shadow-lg ring-1 ring-black ring-opacity-5 dark:ring-gray-700 border-0 dark:border dark:border-gray-800 animate-in slide-in-from-top-2 duration-200">
							<div class="py-2">
								{#each languages as lang}
									<button
										class="w-full flex items-center px-4 py-2.5 text-left text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800 focus:bg-gray-50 dark:focus:bg-gray-800 focus:outline-none transition-colors duration-150 {$language === lang.code ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400' : ''}"
										onclick={(e) => {
											e.stopPropagation();
											switchLanguage(lang.code);
										}}
									>
										<span class="mr-3 text-lg">{lang.flag}</span>
										<span class="flex-1">{lang.label}</span>
										{#if $language === lang.code}
											<svg class="h-4 w-4 text-blue-600 dark:text-blue-400" fill="currentColor" viewBox="0 0 20 20">
												<path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
											</svg>
										{/if}
									</button>
								{/each}
							</div>
						</div>
					{/if}
				</div>

			</div>
		</div>
	</div>
</nav>