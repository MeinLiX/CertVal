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

<div class="min-h-screen bg-base-100 transition-colors duration-300 {mounted ? 'animate-in fade-in duration-500' : ''}">
	{#if !isAuthPage}
		<div class="drawer lg:drawer-open">
			<input id="drawer-toggle" type="checkbox" class="drawer-toggle" />
			<div class="drawer-content flex flex-col">
				<Navbar />
				
				<!-- Main content -->
				<main class="flex-1 pt-16 transition-all duration-300">
					<!-- Mobile drawer button -->
					<div class="lg:hidden p-4">
						<label for="drawer-toggle" class="btn btn-square btn-ghost">
							<svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
							</svg>
						</label>
					</div>
					
					<div class="relative min-h-[calc(100vh-4rem)]">
						<div class="fixed inset-0 overflow-hidden pointer-events-none">
							<div class="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-primary/10 blur-3xl"></div>
							<div class="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-secondary/10 blur-3xl"></div>
						</div>
						
						<div class="relative z-10 p-6 lg:pl-0">
							{@render children?.()}
						</div>
					</div>
				</main>
			</div>
			
			<Sidebar />
		</div>
	{:else}
		<div class="min-h-screen bg-gradient-to-br from-primary/5 via-secondary/5 to-accent/5 relative overflow-hidden">
			<div class="absolute inset-0 overflow-hidden">
				<div class="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-primary/20 blur-3xl animate-pulse"></div>
				<div class="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-secondary/20 blur-3xl animate-pulse" style="animation-delay: 2s;"></div>
				<div class="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 h-96 w-96 rounded-full bg-accent/10 blur-3xl animate-pulse" style="animation-delay: 4s;"></div>
			</div>
			
			<div class="absolute inset-0 opacity-[0.02]">
				<div class="h-full w-full" style="background-image: radial-gradient(circle at 1px 1px, oklch(var(--p)) 1px, transparent 0); background-size: 20px 20px;"></div>
			</div>
			
			<div class="relative z-10">
				{@render children?.()}
			</div>
		</div>
	{/if}
</div>