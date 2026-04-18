<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { userSession } from '$lib/stores/userSession.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';

	let { children } = $props();

	const isAuthenticated = $derived(userSession.isAuthenticated);

	onMount(() => {
		if (!userSession.isAuthenticated) {
			goto('/auth/login');
		}
	});

	$effect(() => {
		if (!userSession.isAuthenticated && typeof window !== 'undefined') {
			goto('/auth/login');
		}
	});
</script>

{#if isAuthenticated}
	{@render children?.()}
{:else}
	<GlobalLoader variant="fullscreen" />
{/if}
