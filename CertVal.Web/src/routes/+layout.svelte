<script lang="ts">
	import '../app.css';
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import Navbar from '$lib/components/layout/Navbar.svelte';
	import Sidebar from '$lib/components/layout/Sidebar.svelte';

	let { children } = $props();

	onMount(() => {
		auth.initialize();
		language.initialize();
	});

	$: isAuthPage = $page.url.pathname.startsWith('/auth');
</script>

{#if !isAuthPage}
	<div class="min-h-screen bg-gray-50">
		<Navbar />
		<div class="flex">
			<Sidebar />
			<main class="ml-64 flex-1 p-6">
				{@render children?.()}
			</main>
		</div>
	</div>
{:else}
	<div class="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
		{@render children?.()}
	</div>
{/if}
