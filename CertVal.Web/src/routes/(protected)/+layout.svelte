<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

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
	<div class="flex min-h-screen items-center justify-center">
		<div class="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
	</div>
{/if}