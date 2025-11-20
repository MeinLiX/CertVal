<script lang="ts">
	import { page } from '$app/state';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { IconName } from '$lib/icons';
	import { onMount, onDestroy, tick } from 'svelte';

	const tabs: { href: string; label: string; icon: IconName }[] = [
		{ href: '/profile/settings/personal', label: 'profile.personalInfo', icon: 'profile' },
		{ href: '/profile/settings/security', label: 'profile.security', icon: 'security' },
		{ href: '/profile/settings/api-tokens', label: 'nav.apiTokens', icon: 'key' },
		{ href: '/profile/settings/docs', label: 'nav.documentation', icon: 'document' }
	];

	let { children } = $props();

	const currentPath = $derived(page.url.pathname);

	let tabsContainer: HTMLElement | null = null;
	let underlineLeft = $state(0);
	let underlineWidth = $state(0);
	let isUnderlineVisible = $state(false);

	async function updateUnderline() {
		await tick();
		const container = tabsContainer;
		if (!container) {
			underlineLeft = 0;
			underlineWidth = 0;
			isUnderlineVisible = false;
			return;
		}

		const tabEls = Array.from(container.querySelectorAll<HTMLElement>('a[role="tab"]'));
		const activeIndex = tabs.findIndex((t) => currentPath.startsWith(t.href));
		const el = tabEls[activeIndex];

		if (el) {
			const rect = el.getBoundingClientRect();
			const parentRect = container.getBoundingClientRect();
			const inset = Math.min(16, Math.round(rect.width * 0.12));
			underlineLeft = rect.left - parentRect.left + container.scrollLeft + Math.round(inset / 2);
			underlineWidth = Math.max(0, rect.width - inset);
			isUnderlineVisible = underlineWidth > 0;
		} else {
			underlineLeft = 0;
			underlineWidth = 0;
			isUnderlineVisible = false;
		}
	}

	let resizeHandler: () => void;

	onMount(() => {
		updateUnderline();

		resizeHandler = () => updateUnderline();
		window.addEventListener('resize', resizeHandler);
	});

	onDestroy(() => {
		if (resizeHandler) window.removeEventListener('resize', resizeHandler);
	});

	$effect(() => {
		if (currentPath) {
			updateUnderline();
		}
	});
</script>

<div class="space-y-6">
	<div>
		<h1 class="text-3xl font-bold">{t('nav.settings', language.current)}</h1>
		<p class="text-base-content/70 mt-1">{t('profile.subtitle', language.current)}</p>
	</div>

	<div
		bind:this={tabsContainer}
		role="tablist"
		aria-label="Settings sections"
		class="border-base-300 relative flex gap-0 overflow-x-auto border-b"
	>
		{#each tabs as tab, i}
			<a
				href={tab.href}
				role="tab"
				aria-selected={currentPath.startsWith(tab.href)}
				tabindex={currentPath.startsWith(tab.href) ? 0 : -1}
				class="focus:ring-primary/30 flex items-center gap-2 whitespace-nowrap px-4 py-3 transition-all duration-100 focus:outline-none focus:ring-2 {currentPath.startsWith(
					tab.href
				)
					? 'text-primary font-medium'
					: 'text-base-content/70'}"
			>
				<Icon name={tab.icon} class="h-4 w-4 shrink-0" />
				<span>{t(tab.label, language.current)}</span>
			</a>
		{/each}

		<div
			class="bg-primary pointer-events-none absolute bottom-0 h-2 rounded-full transition-all duration-300"
			style="transform: translateX({underlineLeft}px); width: {underlineWidth}px; opacity: {isUnderlineVisible
				? 1
				: 0};"
			aria-hidden="true"
		></div>
	</div>

	<div>
		{@render children?.()}
	</div>
</div>
