<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { auth } from '$lib/stores/auth';
	import AuthBackground from '$lib/components/three/AuthBackground.svelte';

	let { children } = $props();

	$effect(() => {
		if ($auth.isAuthenticated && !page.url.pathname.includes('/auth/confirm-email')) {
			goto('/dashboard');
		}
	});
</script>

<div class="text-base-content relative min-h-screen w-full overflow-hidden font-sans">
	<AuthBackground />

	<div class="relative z-10 flex min-h-screen items-center justify-center p-4">
		{@render children()}
	</div>
</div>
