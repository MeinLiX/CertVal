<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { theme } from '$lib/stores/theme.svelte';
	import { userSession } from '$lib/stores/userSession.svelte';
	import type { Language, Theme } from '$lib/types';
	import Icon from '$lib/components/ui/Icon.svelte';
	import { fade } from 'svelte/transition';

	const languages: { code: Language; label: string; flag: string }[] = [
		{ code: 'uk', label: 'Українська', flag: '🇺🇦' },
		{ code: 'en', label: 'English', flag: '🇺🇸' }
	];
	const themes: { value: Theme; label: string; icon: string }[] = [
		{ value: 'light', label: 'Light', icon: '☀️' },
		{ value: 'dark', label: 'Dark', icon: '🌙' }
	];
	function switchLanguage(lang: Language) {
		language.set(lang);
	}

	function switchTheme(newTheme: Theme) {
		theme.set(newTheme);
	}

	const currentLanguageData = $derived(
		languages.find((l) => l.code === language.current) ?? languages[0]
	);
	const currentThemeData = $derived(themes.find((t) => t.value === theme.current) ?? themes[0]);

	const isAuthenticated = $derived(userSession.isAuthenticated);
</script>

<div
	class="pointer-events-none fixed left-0 right-0 top-0 z-50 flex items-start justify-between p-4"
>
	<div class="pointer-events-auto">
		{#if isAuthenticated}
			<label
				for="drawer-toggle"
				class="btn btn-circle btn-ghost bg-base-100/10 hover:bg-base-100/20 text-base-content border border-white/10 shadow-lg backdrop-blur-md transition-all duration-300 hover:scale-110 lg:hidden"
				transition:fade
				data-test-id="drawer-toggle"
			>
				<Icon name="menu" />
			</label>
		{/if}
	</div>

	<div class="pointer-events-auto flex gap-3">
		<div class="dropdown dropdown-end">
			<div
				tabindex="0"
				role="button"
				class="btn btn-circle btn-ghost bg-base-100/10 hover:bg-base-100/20 border border-white/10 shadow-lg backdrop-blur-md transition-all duration-300 hover:scale-110"
				data-test-id="theme-switcher"
			>
				<span class="indicator text-xl drop-shadow-md filter">
					{currentThemeData.icon}
				</span>
			</div>
			<ul
				class="dropdown-content menu bg-base-100/80 mt-4 w-48 rounded-2xl border border-white/10 p-2 shadow-xl backdrop-blur-xl"
			>
				{#each themes as themeOption}
					<li>
						<button
							class="flex gap-3 rounded-xl py-3 font-medium transition-all hover:bg-white/10 {theme.current ===
							themeOption.value
								? 'bg-primary/10 text-primary'
								: ''}"
							onclick={() => switchTheme(themeOption.value)}
							data-test-id={`theme-option-${themeOption.value}`}
						>
							<span class="text-lg">{themeOption.icon}</span>
							{themeOption.label}
							{#if theme.current === themeOption.value}
								<div
									class="bg-primary ml-auto h-2 w-2 rounded-full shadow-[0_0_8px_rgba(var(--p))]"
								></div>
							{/if}
						</button>
					</li>
				{/each}
			</ul>
		</div>

		<div class="dropdown dropdown-end">
			<div
				tabindex="0"
				role="button"
				class="btn btn-circle btn-ghost bg-base-100/10 hover:bg-base-100/20 border border-white/10 shadow-lg backdrop-blur-md transition-all duration-300 hover:scale-110"
				data-test-id="language-switcher"
			>
				<span class="indicator text-xl drop-shadow-md filter">
					{currentLanguageData.flag}
				</span>
			</div>
			<ul
				class="dropdown-content menu bg-base-100/80 mt-4 w-48 rounded-2xl border border-white/10 p-2 shadow-xl backdrop-blur-xl"
			>
				{#each languages as lang}
					<li>
						<button
							class="flex gap-3 rounded-xl py-3 font-medium transition-all hover:bg-white/10 {language.current ===
							lang.code
								? 'bg-primary/10 text-primary'
								: ''}"
							onclick={() => switchLanguage(lang.code)}
							data-test-id={`language-option-${lang.code}`}
						>
							<span class="text-lg">{lang.flag}</span>
							{lang.label}
							{#if language.current === lang.code}
								<div
									class="bg-primary ml-auto h-2 w-2 rounded-full shadow-[0_0_8px_rgba(var(--p))]"
								></div>
							{/if}
						</button>
					</li>
				{/each}
			</ul>
		</div>
	</div>
</div>
