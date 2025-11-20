<script lang="ts">
	import '../app.css';
	import { page } from '$app/state';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { theme } from '$lib/stores/theme.svelte';
	import Navbar from '$lib/components/layout/Navbar.svelte';
	import Sidebar from '$lib/components/layout/Sidebar.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import { onMount } from 'svelte';

	let { children, data } = $props();
	let isAppLoading = $state(true);

	$effect(() => {
		if (data.language) language.set(data.language);
		if (data.theme) theme.set(data.theme);
	});

	onMount(() => {
		setTimeout(() => {
			isAppLoading = false;
		}, 1000);
	});

	const isAuthPage = $derived(page.url.pathname.startsWith('/auth'));
	const isAuthenticated = $derived(!!userSession.token);
</script>

<div
	class="bg-base-200 text-base-content selection:bg-primary selection:text-primary-content min-h-screen font-sans antialiased"
>
	{#if isAppLoading}
		<GlobalLoader />
	{/if}

	<Navbar />
	{#if isAuthPage}
		<main class="relative min-h-screen w-full overflow-hidden">
			{@render children?.()}
		</main>
	{:else if isAuthenticated}
		<div class="drawer lg:drawer-open">
			<input id="drawer-toggle" type="checkbox" class="drawer-toggle" />
			<div
				class="drawer-content bg-base-100/50 flex h-screen flex-col overflow-hidden backdrop-blur-3xl"
			>
				<main
					class="scrollbar-thin scrollbar-track-transparent scrollbar-thumb-base-300 hover:scrollbar-thumb-base-content/20 flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8"
				>
					<div
						class="animate-in fade-in slide-in-from-bottom-4 mx-auto max-w-7xl pt-16 duration-500"
					>
						{@render children?.()}
					</div>
				</main>
			</div>
			<Sidebar />
		</div>
	{:else}
		<main class="relative min-h-screen w-full overflow-hidden">
			{@render children?.()}
		</main>
	{/if}
</div>

<style>
	:global(html.theme-preload, html.theme-preload *) {
		transition: none !important;
	}

	:global(.theme-preload *) {
		transition: none !important;
	}

	:global(::selection) {
		background-color: oklch(from var(--color-primary) l c h / 0.2);
		color: var(--color-primary-content);
	}

	:global(html) {
		scroll-behavior: smooth;
	}

	:global(*:focus:not(:focus-visible)) {
		outline: none;
	}

	:global(*:focus-visible) {
		outline: 2px solid oklch(from var(--color-primary) l c h);
		outline-offset: 2px;
		border-radius: 2px;
	}

	:global(::-webkit-scrollbar) {
		width: 8px;
		height: 8px;
	}

	:global(::-webkit-scrollbar-track) {
		background: oklch(from var(--color-base-200) l c h);
		border-radius: 4px;
	}

	:global(::-webkit-scrollbar-thumb) {
		background: oklch(from var(--color-base-content) l c h / 0.3);
		border-radius: 4px;
		transition: background-color 200ms ease;
	}

	:global(::-webkit-scrollbar-thumb:hover) {
		background: oklch(from var(--color-base-content) l c h / 0.5);
	}

	.drawer-content {
		position: relative;
	}

	@media (max-width: 768px) {
		.mx-auto.max-w-7xl {
			padding-left: 1rem;
			padding-right: 1rem;
		}
	}

	@media (prefers-color-scheme: dark) {
		:global(input[type='search']::-webkit-search-cancel-button) {
			filter: invert(1);
		}
	}

	@media (prefers-reduced-motion: reduce) {
		:global(*) {
			animation-duration: 0.01ms !important;
			animation-iteration-count: 1 !important;
			transition-duration: 0.01ms !important;
		}

		:global(html) {
			scroll-behavior: auto;
		}
	}

	@media (prefers-contrast: high) {
		:global(.drawer-content) {
			border-width: 2px;
		}

		:global(.border) {
			border-width: 2px;
		}
	}

	:global(.loading-overlay) {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background: oklch(from var(--color-base-100) l c h / 0.8);
		backdrop-filter: blur(4px);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 9999;
	}
</style>
