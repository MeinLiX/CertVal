<script lang="ts">
	import '../app.css';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { theme } from '$lib/stores/theme';
	import Navbar from '$lib/components/layout/Navbar.svelte';
	import Sidebar from '$lib/components/layout/Sidebar.svelte';

	let { children, data } = $props();

	language.set(data.language);
	theme.set(data.theme);

	$effect(() => {
		auth.initialize();
	});

	const isAuthPage = $derived(page.url.pathname.startsWith('/auth'));
</script>

<div class="min-h-screen bg-base-200 text-base-content">
	{#if isAuthPage}
		<div class="flex min-h-screen flex-col">
			<Navbar />
			<main class="relative flex grow flex-col items-center justify-center overflow-hidden p-4">
				<div
					class="absolute inset-0 bg-base-100 [mask-image:radial-gradient(100%_100%_at_top_right,white,transparent)]"
				></div>
				<div
					class="absolute inset-0 bg-base-200 [mask-image:radial-gradient(100%_100%_at_bottom_left,white,transparent)]"
				></div>
				{@render children?.()}
			</main>
		</div>
	{:else}
		<div class="drawer lg:drawer-open">
			<input id="drawer-toggle" type="checkbox" class="drawer-toggle" />
			<div class="drawer-content flex flex-col">
				<Navbar />
				<main class="flex-1 p-4 sm:p-6 lg:p-8">
					<div class="mx-auto max-w-7xl">
						{@render children?.()}
					</div>
				</main>
			</div>
			<Sidebar />
		</div>
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

	main .absolute.inset-0 {
		background:
			radial-gradient(
				circle at 20% 80%,
				oklch(from var(--color-primary) l c h / 0.1) 0%,
				transparent 50%
			),
			radial-gradient(
				circle at 80% 20%,
				oklch(from var(--color-secondary) l c h / 0.1) 0%,
				transparent 50%
			);
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
