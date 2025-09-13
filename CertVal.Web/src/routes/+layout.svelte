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
	let mounted = $state(false);

	onMount(() => {
		auth.initialize();
		language.initialize();
		theme.initialize();
		mounted = true;
	});

	const isAuthPage = $derived($page.url.pathname.startsWith('/auth'));
</script>

<div class="min-h-screen transition-colors duration-300 {mounted ? 'animate-in fade-in duration-500' : ''}">
	{#if !isAuthPage}
		<div class="min-h-screen bg-gradient-to-br from-gray-50 via-white to-blue-50/30 dark:from-gray-950 dark:via-gray-900 dark:to-blue-950/30">
			<Navbar />
			<div class="flex">
				<Sidebar />
				<main class="ml-64 flex-1 pt-16 transition-all duration-300">
					<div class="relative">
						<!-- Background decorative elements -->
						<div class="fixed inset-0 overflow-hidden pointer-events-none">
							<div class="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-gradient-to-br from-blue-400/10 to-indigo-400/10 blur-3xl"></div>
							<div class="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-gradient-to-br from-purple-400/10 to-pink-400/10 blur-3xl"></div>
						</div>
						
						<div class="relative z-10">
							{@render children?.()}
						</div>
					</div>
				</main>
			</div>
		</div>
	{:else}
		<div class="min-h-screen bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 dark:from-gray-950 dark:via-blue-950/30 dark:to-indigo-950/30 relative overflow-hidden">
			<!-- Enhanced background decorations for auth pages -->
			<div class="absolute inset-0 overflow-hidden">
				<div class="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-gradient-to-br from-blue-400/20 to-indigo-400/20 blur-3xl animate-pulse"></div>
				<div class="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-gradient-to-br from-purple-400/20 to-pink-400/20 blur-3xl animate-pulse" style="animation-delay: 2s;"></div>
				<div class="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 h-96 w-96 rounded-full bg-gradient-to-br from-cyan-400/10 to-blue-400/10 blur-3xl animate-pulse" style="animation-delay: 4s;"></div>
			</div>
			
			<!-- Geometric pattern overlay -->
			<div class="absolute inset-0 opacity-[0.02] dark:opacity-[0.05]">
				<div class="h-full w-full" style="background-image: radial-gradient(circle at 1px 1px, rgba(59, 130, 246, 0.3) 1px, transparent 0); background-size: 20px 20px;"></div>
			</div>
			
			<div class="relative z-10">
				{@render children?.()}
			</div>
		</div>
	{/if}
</div>