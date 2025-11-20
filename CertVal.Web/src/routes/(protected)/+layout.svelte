<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';

	let { children } = $props();

	onMount(() => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
		}
	});

	$effect(() => {
		if (!$auth.isAuthenticated && typeof window !== 'undefined') {
			goto('/auth/login');
		}
	});

	const isAuthenticated = $derived($auth.isAuthenticated);
</script>

{#if isAuthenticated}
	{@render children?.()}
{:else}
	<GlobalLoader />
{/if}
