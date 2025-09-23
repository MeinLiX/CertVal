<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';

	let { children } = $props();

	onMount(() => {
		// Redirect to dashboard if already authenticated
		if ($auth.isAuthenticated) {
			goto('/dashboard');
		}
	});
</script>

{@render children?.()}
