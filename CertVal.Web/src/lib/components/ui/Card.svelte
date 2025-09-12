<script lang="ts">
	interface Props {
		title?: string;
		children?: any;
		padding?: boolean;
		shadow?: boolean;
	}

	let { title, children, padding = true, shadow = true }: Props = $props();

	$: classes = `bg-white rounded-lg ${shadow ? 'shadow' : ''} ${padding ? 'p-6' : ''}`;
</script>

<div class={classes}>
	{#if title}
		<h3 class="mb-4 text-lg font-medium text-gray-900">{title}</h3>
	{/if}
	{@render children?.()}
</div>
