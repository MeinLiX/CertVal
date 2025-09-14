<script lang="ts">
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

	function switchLanguage(lang: Language) {
		language.setLanguage(lang);
	}

	function switchTheme(newTheme: Theme) {
		theme.setTheme(newTheme);
	}

	const currentLanguageData = $derived(languages.find((l) => l.code === $language) ?? languages[0]);
	const currentThemeData = $derived(themes.find((t) => t.value === $theme.theme) ?? themes[0]);
</script>

<div
	class="sticky top-0 z-30 navbar border-b border-base-content/10 bg-base-100/80 backdrop-blur-lg"
>
	<div class="navbar-start">
		<label for="drawer-toggle" class="btn btn-circle btn-ghost lg:hidden">
			<svg
				xmlns="http://www.w3.org/2000/svg"
				class="h-5 w-5"
				fill="none"
				viewBox="0 0 24 24"
				stroke="currentColor"
				><path
					stroke-linecap="round"
					stroke-linejoin="round"
					stroke-width="2"
					d="M4 6h16M4 12h16M4 18h7"
				/></svg
			>
		</label>
	</div>
	<div class="navbar-center"></div>
	<div class="navbar-end gap-2">
		<div class="dropdown dropdown-end">
			<div tabindex="0" role="button" class="btn btn-circle btn-ghost">
				<span class="indicator text-lg">
					{currentThemeData.icon}
				</span>
			</div>
			<ul
				class="dropdown-content menu w-40 rounded-box border border-base-content/10 bg-base-100 p-2 shadow-lg"
			>
				{#each themes as themeOption}
					<li>
						<button
							class:active={$theme.theme === themeOption.value}
							onclick={() => switchTheme(themeOption.value)}
						>
							{themeOption.icon}
							{themeOption.label}
						</button>
					</li>
				{/each}
			</ul>
		</div>

		<div class="dropdown dropdown-end">
			<div tabindex="0" role="button" class="btn btn-circle btn-ghost">
				<span class="indicator text-lg">
					{currentLanguageData.flag}
				</span>
			</div>
			<ul
				class="dropdown-content menu w-40 rounded-box border border-base-content/10 bg-base-100 p-2 shadow-lg"
			>
				{#each languages as lang}
					<li>
						<button
							class:active={$language === lang.code}
							onclick={() => switchLanguage(lang.code)}
						>
							{lang.flag}
							{lang.label}
						</button>
					</li>
				{/each}
			</ul>
		</div>
	</div>
</div>
