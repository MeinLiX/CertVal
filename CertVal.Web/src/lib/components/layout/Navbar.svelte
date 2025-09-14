<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { theme } from '$lib/stores/theme';
	import logoUrl from '$lib/assets/favicon.svg?url';
	import { t } from '$lib/i18n';
	import type { Language, Theme } from '$lib/types';
	import { goto } from '$app/navigation';

	const languages: { code: Language; label: string; flag: string }[] = [
		{ code: 'uk', label: 'Українська', flag: '🇺🇦' },
		{ code: 'en', label: 'English', flag: '🇺🇸' }
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

	function handleLogout() {
		auth.logout();
		goto('/auth/login');
	}

	const currentLanguageData = $derived(
		languages.find(l => l.code === $language) ?? languages[0]
	);

	const userInitials = $derived(
		($auth.user?.firstName ?? '').charAt(0) + ($auth.user?.lastName ?? '').charAt(0)
	);
</script>

<!-- Navbar -->
<div class="navbar bg-base-100 border-b border-base-200 fixed top-0 z-50 backdrop-blur-md bg-base-100/95">
	<div class="navbar-start">
		<a href="/" class="btn btn-ghost text-xl normal-case">
			<div class="avatar">
				<div class="w-8 rounded-full overflow-hidden">
					<img src="{logoUrl}" alt="CertVal logo" class="w-8 h-8 object-cover" />
				</div>
			</div>
			<span class="font-bold text-base-content ml-2">CertVal</span>
		</a>
	</div>

	<!-- Navbar Center - Hidden on mobile, could add navigation items here -->
	<div class="navbar-center hidden lg:flex">

	</div>

</div>