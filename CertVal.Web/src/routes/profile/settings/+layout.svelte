<script lang="ts">
	import { page } from '$app/state';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { IconName } from '$lib/icons';

	let { children } = $props();

	const tabs: { href: string; label: string; icon: IconName }[] = [
		{ href: '/profile/settings/personal', label: 'profile.personalInfo', icon: 'profile' },
		{ href: '/profile/settings/security', label: 'profile.security', icon: 'security' },
		{ href: '/profile/settings/api-tokens', label: 'nav.apiTokens', icon: 'key' },
		{ href: '/profile/settings/docs', label: 'nav.documentation', icon: 'document' }
	];

	const currentPath = $derived(page.url.pathname);
</script>

<div class="space-y-6">
	<div>
		<h1 class="text-3xl font-bold">{t('nav.settings', $language)}</h1>
		<p class="mt-1 text-base-content/70">{t('profile.subtitle', $language)}</p>
	</div>

	<div role="tablist" class="tabs-boxed tabs bg-base-200">
		{#each tabs as tab}
			<a
				href={tab.href}
				role="tab"
				class="tab flex-1 {currentPath.startsWith(tab.href)
					? 'tab-active !bg-primary text-primary-content'
					: ''}"
			>
				<Icon name={tab.icon} class="mr-2 h-4 w-4" />
				{t(tab.label, $language)}
			</a>
		{/each}
	</div>

	<div>
		{@render children?.()}
	</div>
</div>
