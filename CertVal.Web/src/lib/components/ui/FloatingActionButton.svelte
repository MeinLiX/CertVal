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
		'data-test-id'?: string;
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
		class: className = '',
		'data-test-id': testId
	}: Props = $props();

	const tooltipClass = tooltip ? `tooltip tooltip-${tooltipPosition}` : '';
	const combinedClass = `fab fixed right-6 bottom-6 z-50 ${tooltipClass} ${className}`;
</script>

{#if href}
	<a
		{href}
		{target}
		{rel}
		class={combinedClass}
		data-tip={tooltip}
		role="button"
		data-test-id={testId}
	>
		<Button {variant} shape="circle" class="shadow-lg transition-all duration-300 hover:shadow-xl">
			<Icon name={iconName} class="h-6 w-6" />
		</Button>
	</a>
{:else}
	<div class={combinedClass} data-tip={tooltip} role="button" data-test-id={testId}>
		<Button
			{variant}
			shape="circle"
			class="shadow-lg transition-all duration-300 hover:shadow-xl"
			{onclick}
		>
			<Icon name={iconName} class="h-6 w-6" />
		</Button>
	</div>
{/if}

<style>
	.fab {
		transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
	}

	.fab:hover {
		transform: scale(1.1);
	}
</style>
