<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { theme } from '$lib/stores/theme';
	import type { Language, Theme } from '$lib/types';

	const languages: { code: Language; label: string; flag: string }[] = [
		{ code: 'uk', label: 'Українська', flag: '🇺🇦' },
		{ code: 'en', label: 'English', flag: '🇺🇸' }
	];

	const themes: { value: Theme; label: string; icon: string }[] = [
		{ value: 'light', label: 'Light', icon: '☀️' },
		{ value: 'dark', label: 'Dark', icon: '🌙' }
	];

	onMount(() => {
		language.initialize?.();
		auth.initialize?.();
		theme.initialize?.();
	});

	function switchLanguage(lang: Language) {
		language.setLanguage(lang);
	}

	function switchTheme(newTheme: Theme) {
		theme.setTheme(newTheme);
	}

	const currentLanguageData = $derived(languages.find((l) => l.code === $language) ?? languages[0]);

	const currentThemeData = $derived(themes.find((t) => t.value === $theme.theme) ?? themes[0]);
</script>

<!-- Navbar -->
<div class="z-50 navbar border-b border-base-200 bg-base-100 shadow-sm">
	<div class="navbar-start"></div>

	<div class="navbar-center"></div>

	<div class="navbar-end">
		<div class="flex items-center gap-2">
			<!-- Theme Switcher -->
			<div class="dropdown dropdown-end">
				<div tabindex="0" role="button" class="btn btn-circle btn-ghost">
					<span class="text-lg">{currentThemeData.icon}</span>
				</div>
				<ul
					class="dropdown-content menu z-[1] w-52 rounded-box border border-base-200 bg-base-100 p-2 shadow-lg"
				>
					{#each themes as themeOption}
						<li>
							<button
								class="flex items-center gap-3 {$theme.theme === themeOption.value ? 'active' : ''}"
								onclick={() => switchTheme(themeOption.value)}
							>
								<span class="text-lg">{themeOption.icon}</span>
								<span>{themeOption.label}</span>
								{#if $theme.theme === themeOption.value}
									<svg class="ml-auto h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
										<path
											fill-rule="evenodd"
											d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
											clip-rule="evenodd"
										/>
									</svg>
								{/if}
							</button>
						</li>
					{/each}
				</ul>
			</div>

			<!-- Language Switcher -->
			<div class="dropdown dropdown-end">
				<div tabindex="0" role="button" class="btn btn-circle btn-ghost">
					<span class="text-lg">{currentLanguageData.flag}</span>
				</div>
				<ul
					class="dropdown-content menu z-[1] w-52 rounded-box border border-base-200 bg-base-100 p-2 shadow-lg"
				>
					{#each languages as lang}
						<li>
							<button
								class="flex items-center gap-3 {$language === lang.code ? 'active' : ''}"
								onclick={() => switchLanguage(lang.code)}
							>
								<span class="text-lg">{lang.flag}</span>
								<span>{lang.label}</span>
								{#if $language === lang.code}
									<svg class="ml-auto h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
										<path
											fill-rule="evenodd"
											d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
											clip-rule="evenodd"
										/>
									</svg>
								{/if}
							</button>
						</li>
					{/each}
				</ul>
			</div>
		</div>
	</div>
</div>
