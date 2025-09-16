<script lang="ts">
	import '../app.css';
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { theme } from '$lib/stores/theme';
	import Navbar from '$lib/components/layout/Navbar.svelte';
	import Sidebar from '$lib/components/layout/Sidebar.svelte';

	let { children } = $props();

	$effect(() => {
		auth.initialize();
		language.initialize();
		theme.initialize();
	});
	const isAuthPage = $derived($page.url.pathname.startsWith('/auth'));
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
