<script lang="ts">
	import { Canvas } from '@threlte/core';
	import Scene from './Scene.svelte';
	import { authUiState } from '$lib/stores/authUiState.svelte';

	function handleMouseMove(e: MouseEvent) {
		if (typeof window !== 'undefined') {
			authUiState.setMouse(e.clientX / window.innerWidth, e.clientY / window.innerHeight);
		}
	}
</script>

<svelte:window onmousemove={handleMouseMove} />

<div
	class="pointer-events-none fixed inset-0 z-0 h-full w-full overflow-hidden"
	data-test-id="auth-background"
>
	<Canvas>
		<Scene />
	</Canvas>
</div>
