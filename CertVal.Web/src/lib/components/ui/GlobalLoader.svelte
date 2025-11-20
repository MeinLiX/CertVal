<script lang="ts">
	import { fade } from 'svelte/transition';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';

	interface Props {
		variant?: 'fullscreen' | 'inline' | 'overlay';
		class?: string;
	}

	let { variant = 'fullscreen', class: className = '' }: Props = $props();
</script>

<div
	class={[
		variant === 'fullscreen'
			? 'bg-base-100/95 fixed inset-0 z-[9999] flex items-center justify-center backdrop-blur-xl'
			: variant === 'overlay'
				? 'bg-base-100/95 absolute inset-0 z-50 flex items-center justify-center backdrop-blur-xl'
				: 'flex h-full min-h-[400px] w-full flex-col items-center justify-center py-12',
		className
	].join(' ')}
	transition:fade={{ duration: 500 }}
>
	<div class="relative flex flex-col items-center justify-center gap-8">
		<!-- Shield SVG Animation -->
		<svg class="h-32 w-32 drop-shadow-[0_0_15px_var(--color-primary)]" viewBox="0 0 100 100">
			<defs>
				<linearGradient id="shield-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
					<stop offset="0%" stop-color="var(--color-primary)" />
					<stop offset="50%" stop-color="var(--color-secondary)" />
					<stop offset="100%" stop-color="var(--color-primary)" />
				</linearGradient>
			</defs>
			<!-- Background track -->
			<path
				d="M50 5 L90 20 V50 C90 75 50 95 50 95 C50 95 10 75 10 50 V20 L50 5 Z"
				fill="none"
				stroke="var(--color-base-300)"
				stroke-width="4"
				stroke-linecap="round"
				stroke-linejoin="round"
				class="opacity-30"
			/>
			<!-- Animated path -->
			<path
				d="M50 5 L90 20 V50 C90 75 50 95 50 95 C50 95 10 75 10 50 V20 L50 5 Z"
				fill="none"
				stroke="url(#shield-gradient)"
				stroke-width="4"
				stroke-linecap="round"
				stroke-linejoin="round"
				class="shield-path"
			/>
		</svg>

		<!-- Loading Text -->
		<div class="flex flex-col items-center gap-2">
			<span class="text-primary animate-pulse text-xl font-bold tracking-[0.2em]">
				{t('common.loading', language.current).replace('...', '').toUpperCase()}
			</span>
		</div>
	</div>
</div>

<style>
	.shield-path {
		stroke-dasharray: 300;
		stroke-dashoffset: 300;
		animation: draw 2.5s ease-in-out infinite;
	}

	@keyframes draw {
		0% {
			stroke-dashoffset: 300;
			fill: transparent;
		}
		40% {
			stroke-dashoffset: 0;
			fill: transparent;
		}
		50% {
			fill: color-mix(in srgb, var(--color-primary) 10%, transparent);
		}
		60% {
			fill: transparent;
		}
		100% {
			stroke-dashoffset: -300;
			fill: transparent;
		}
	}
</style>
