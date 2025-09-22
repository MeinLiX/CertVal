<script lang="ts">
	import Button from './Button.svelte';
	import Icon from './Icon.svelte';
	import type { IconName } from '$lib/icons';

	interface Props {
		iconName: IconName;
		variant?:
			| 'primary'
			| 'secondary'
			| 'accent'
			| 'success'
			| 'warning'
			| 'error'
			| 'danger'
			| 'info'
			| 'ghost'
			| 'link'
			| 'outline';
		tooltip?: string;
		tooltipPosition?: 'left' | 'right' | 'top' | 'bottom';
		onclick?: () => void;
		href?: string;
		target?: string;
		rel?: string;
		class?: string;
	}

	let {
		iconName,
		variant = 'outline',
		tooltip,
		tooltipPosition = 'left',
		onclick,
		href,
		target,
		rel,
		class: className = ''
	}: Props = $props();

	const tooltipClass = tooltip ? `tooltip tooltip-${tooltipPosition}` : '';
	const combinedClass = `fab fixed right-6 bottom-6 z-50 ${tooltipClass} ${className}`;
</script>

{#if href}
	<a {href} {target} {rel} class={combinedClass} data-tip={tooltip} role="button">
		<Button {variant} shape="circle" class="shadow-lg">
			<Icon name={iconName} class="h-6 w-6" />
		</Button>
	</a>
{:else}
	<div class={combinedClass} data-tip={tooltip} role="button">
		<Button {variant} shape="circle" class="shadow-lg" {onclick}>
			<Icon name={iconName} class="h-6 w-6" />
		</Button>
	</div>
{/if}

<style>
	.fab {
		transition: transform 0.2s ease-in-out;
	}

	.fab:hover {
		transform: scale(1.05);
	}
</style>
