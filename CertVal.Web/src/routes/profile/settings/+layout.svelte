<script lang="ts">
	import { page } from '$app/state';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { IconName } from '$lib/icons';
	import { onMount, onDestroy, tick } from 'svelte';

	const tabs: { href: string; label: string; icon: IconName }[] = [
		{ href: '/profile/settings/personal', label: 'profile.personalInfo', icon: 'profile' },
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
			underlineLeft = rect.left - parentRect.left + container.scrollLeft;
			underlineWidth = rect.width;
			isUnderlineVisible = true;
		} else {
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

<div class="settings-layout">
	<div class="tabs-nav" bind:this={tabsContainer} role="tablist" aria-label="Settings sections">
		{#each tabs as tab}
			<a
				href={tab.href}
				role="tab"
				aria-selected={currentPath.startsWith(tab.href)}
				class="tab"
				class:tab--active={currentPath.startsWith(tab.href)}
			>
				<Icon name={tab.icon} />
				<span>{t(tab.label, language.current)}</span>
			</a>
		{/each}
		
		<div
			class="tabs-underline"
			style="left: {underlineLeft}px; width: {underlineWidth}px; opacity: {isUnderlineVisible ? 1 : 0};"
		></div>
	</div>

	<div class="settings-content">
		{@render children?.()}
	</div>
</div>

<style>
	.settings-layout {
		display: flex;
		flex-direction: column;
		gap: var(--space-8);
	}

	.tabs-nav {
		position: relative;
		display: flex;
		gap: var(--space-6);
		overflow-x: auto;
		border-bottom: 1px solid var(--color-border);
		padding-bottom: 1px;
	}

	.tab {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-4) 0;
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text-secondary);
		text-decoration: none;
		white-space: nowrap;
		transition: color 0.2s;
	}

	.tab:hover {
		color: var(--color-primary);
	}

	.tab--active {
		color: var(--color-primary);
	}

	.tabs-underline {
		position: absolute;
		bottom: 0;
		height: 2px;
		background: var(--color-primary);
		transition: all 0.3s ease-out;
	}

	.settings-content {
		min-height: 400px;
	}
</style>
