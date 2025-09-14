<script lang="ts">
	interface Props {
		title?: string;
		children?: any;
		compact?: boolean;
		glass?: boolean;
		bordered?: boolean;
		shadow?: boolean;
		hover?: boolean;
		class?: string;
	}

	let { 
		title, 
		children, 
		compact = false,
		glass = false,
		bordered = true,
		shadow = true,
		hover = false,
		class: className = ''
	}: Props = $props();

	const classes = $derived(`
		card bg-base-100 transition-all duration-300
		${glass ? 'glass' : ''} 
		${bordered ? 'border border-base-300' : ''}
		${shadow ? 'shadow-lg' : ''} 
		${hover ? 'hover:scale-[1.02] hover:shadow-xl cursor-pointer' : ''}
		${compact ? 'card-compact' : ''} 
		${className}
	`.trim().replace(/\s+/g, ' '));
</script>

<div class={classes}>
	{#if title}
		<div class="card-body">
			<h3 class="card-title text-base-content mb-4">
				{title}
				<!-- Decorative element -->
				<div class="flex-1 h-px bg-gradient-to-r from-base-300 to-transparent ml-4"></div>
			</h3>
			<div class="relative">
				{@render children?.()}
			</div>
		</div>
	{:else}
		<div class="card-body">
			{@render children?.()}
		</div>
	{/if}
</div>