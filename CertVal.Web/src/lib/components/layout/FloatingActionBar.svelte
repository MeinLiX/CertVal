<script lang="ts">
	import type { Snippet } from 'svelte';
	import { onMount, tick } from 'svelte';
	import { navigating } from '$app/stores';
	import { appState } from '$lib/stores/appState.svelte';

	interface Props {
		/** Left-aligned content (search, filters, workspace selector) */
		leading?: Snippet;
		/** Center content (tabs, breadcrumbs, selection info) */
		center?: Snippet;
		/** Right-aligned content (primary actions, buttons) */
		trailing?: Snippet;
		/** Hide the bar (e.g., when not applicable) */
		hidden?: boolean;
		/** Accessible label (not rendered visually) */
		label?: string;
	}

	let { leading, center, trailing, hidden = false, label }: Props = $props();

	let mounted = $state(false);

	onMount(() => {
		// Wait for DOM paint + a small settle frame so the page layout/animations
		// have committed before we reveal the fixed bar. This avoids the bar
		// briefly rendering inside a transformed ancestor.
		let raf1: number;
		let raf2: number;
		(async () => {
			await tick();
			raf1 = requestAnimationFrame(() => {
				raf2 = requestAnimationFrame(() => {
					mounted = true;
				});
			});
		})();
		return () => {
			if (raf1) cancelAnimationFrame(raf1);
			if (raf2) cancelAnimationFrame(raf2);
		};
	});

	const isRouteChange = $derived(
		$navigating !== null &&
			$navigating.from?.url.pathname !== $navigating.to?.url.pathname
	);

	const shouldHide = $derived(
		hidden || appState.isLoading || !mounted || isRouteChange
	);
</script>

{#if !shouldHide}
	<div
		class="action-bar"
		role="toolbar"
		aria-label={label ?? 'Page actions'}
		data-test-id="floating-action-bar"
	>
		<div class="action-bar__inner">
			<div class="action-bar__track">
				{#if leading}
					<div class="action-bar__slot action-bar__slot--leading">
						{@render leading()}
					</div>
				{/if}
				{#if center}
					<div class="action-bar__slot action-bar__slot--center">
						{@render center()}
					</div>
				{/if}
				{#if trailing}
					<div class="action-bar__slot action-bar__slot--trailing">
						{@render trailing()}
					</div>
				{/if}
			</div>
		</div>
	</div>
{/if}

<style>
	.action-bar {
		position: fixed;
		left: 50%;
		bottom: var(--space-5);
		transform: translateX(-50%);
		z-index: var(--z-sticky, 40);
		pointer-events: none;
		max-width: calc(100vw - var(--space-8));
		animation: bar-in 320ms cubic-bezier(0.22, 1, 0.36, 1) both;
		animation-delay: 120ms;
	}

	.action-bar__inner {
		/* Compact fit-content pill instead of full-width bar */
		width: max-content;
		max-width: 100%;
		margin: 0 auto;
		background:
			linear-gradient(
				180deg,
				color-mix(in srgb, var(--color-surface) 68%, transparent),
				color-mix(in srgb, var(--color-surface) 58%, transparent)
			),
			color-mix(in srgb, var(--color-text) 5%, transparent);
		backdrop-filter: saturate(170%) blur(20px);
		-webkit-backdrop-filter: saturate(170%) blur(20px);
		border: 1px solid color-mix(in srgb, var(--color-border) 65%, transparent);
		border-radius: var(--radius-lg);
		/* Symmetric halo — same spread on all sides */
		box-shadow:
			0 0 0 1px color-mix(in srgb, var(--color-text) 5%, transparent),
			0 0 20px -4px color-mix(in srgb, var(--color-text) 16%, transparent),
			0 0 48px -12px color-mix(in srgb, var(--color-text) 22%, transparent);
		padding: var(--space-2) var(--space-3);
		display: flex;
		align-items: center;
		justify-content: center;
		pointer-events: auto;
		transition:
			border-color var(--transition-fast),
			box-shadow var(--transition-fast),
			transform var(--transition-fast);
	}

	.action-bar__track {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: var(--space-2);
		flex-wrap: nowrap;
		min-width: 0;
	}

	.action-bar__inner:hover {
		border-color: color-mix(in srgb, var(--color-border-hover) 85%, transparent);
		transform: translateY(-1px);
		box-shadow:
			0 0 0 1px color-mix(in srgb, var(--color-text) 6%, transparent),
			0 0 24px -4px color-mix(in srgb, var(--color-text) 20%, transparent),
			0 0 60px -12px color-mix(in srgb, var(--color-text) 28%, transparent);
	}

	.action-bar__slot {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		min-width: 0;
	}

	.action-bar__slot--leading,
	.action-bar__slot--center,
	.action-bar__slot--trailing {
		flex: 0 0 auto;
	}

	/* Subtle divider between slots when multiple slots are present */
	.action-bar__slot + .action-bar__slot::before {
		content: '';
		display: block;
		width: 1px;
		height: 20px;
		background: color-mix(in srgb, var(--color-border) 55%, transparent);
		margin: 0 var(--space-1);
	}

	@keyframes bar-in {
		from {
			opacity: 0;
			transform: translate(-50%, 16px) scale(0.96);
		}
		to {
			opacity: 1;
			transform: translate(-50%, 0) scale(1);
		}
	}

	@media (max-width: 640px) {
		.action-bar {
			bottom: 0;
			left: 0;
			right: 0;
			transform: none;
			max-width: none;
			padding: 0 var(--space-3)
				max(var(--space-3), env(safe-area-inset-bottom, 0px));
			animation-name: bar-in-mobile;
		}

		.action-bar__inner {
			width: 100%;
			border-radius: var(--radius-xl);
			padding: var(--space-2);
			/* Stronger liquid-glass blur for the full-width mobile dock */
			backdrop-filter: saturate(180%) blur(24px);
			-webkit-backdrop-filter: saturate(180%) blur(24px);
			/* Allow the action row to scroll horizontally instead of wrapping
			   so buttons + selectors never overflow the viewport. */
			overflow-x: auto;
			overflow-y: hidden;
			scrollbar-width: none;
			-webkit-overflow-scrolling: touch;
			justify-content: flex-start;
		}

		.action-bar__inner::-webkit-scrollbar {
			display: none;
		}

		.action-bar__track {
			/* Centre when the actions fit, scroll from the start when they don't */
			margin: 0 auto;
			flex-wrap: nowrap;
		}

		.action-bar__slot {
			flex: 0 0 auto;
		}

		.action-bar__slot + .action-bar__slot::before {
			display: none;
		}
	}

	@keyframes bar-in-mobile {
		from {
			opacity: 0;
			transform: translateY(16px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.action-bar {
			animation: none;
		}
		.action-bar__inner:hover {
			transform: none;
		}
	}
</style>
