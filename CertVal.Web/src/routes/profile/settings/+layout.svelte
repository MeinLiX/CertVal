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

<div class="space-y-8">
	<div class="relative">
		<div
			bind:this={tabsContainer}
			role="tablist"
			aria-label="Settings sections"
			class="border-base-content/10 relative flex gap-8 overflow-x-auto border-b pb-px"
		>
			{#each tabs as tab}
				<a
					href={tab.href}
					role="tab"
					aria-selected={currentPath.startsWith(tab.href)}
					class="hover:text-primary group relative flex items-center gap-2 whitespace-nowrap py-4 text-sm font-medium transition-colors focus:outline-none {currentPath.startsWith(
						tab.href
					)
						? 'text-primary'
						: 'text-base-content/60'}"
				>
					<Icon
						name={tab.icon}
						class="group-hover:text-primary h-4 w-4 shrink-0 transition-colors {currentPath.startsWith(
							tab.href
						)
							? 'text-primary'
							: 'text-base-content/40'}"
					/>
					<span>{t(tab.label, language.current)}</span>
				</a>
			{/each}

			<div
				class="bg-primary absolute bottom-0 h-0.5 transition-all duration-300 ease-out"
				style="left: {underlineLeft}px; width: {underlineWidth}px; opacity: {isUnderlineVisible
					? 1
					: 0};"
			></div>
		</div>
	</div>

	<div class="min-h-[400px]">
		{@render children?.()}
	</div>
</div>
