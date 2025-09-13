<script lang="ts">
	interface Props {
		title?: string;
		children?: any;
		padding?: boolean;
		shadow?: boolean;
		hover?: boolean;
		glass?: boolean;
		class?: string;
	}

	let { 
		title, 
		children, 
		padding = true, 
		shadow = true, 
		hover = false, 
		glass = false,
		class: className = ''
	}: Props = $props();

	const classes = $derived(`
		relative overflow-hidden rounded-2xl transition-all duration-300
		${glass 
			? 'bg-white/80 dark:bg-gray-900/80 backdrop-blur-xl border border-white/20 dark:border-gray-800/50' 
			: 'bg-white dark:bg-gray-900 border border-gray-200/50 dark:border-gray-800/50'
		}
		${shadow ? 'shadow-sm hover:shadow-lg dark:shadow-gray-900/20' : ''} 
		${hover ? 'hover:scale-[1.02] cursor-pointer' : ''}
		${padding ? 'p-6' : ''} 
		${className}
	`.trim().replace(/\s+/g, ' '));
</script>

<div class={classes}>
	<!-- Decorative gradient overlay -->
	{#if glass}
		<div class="absolute inset-0 bg-gradient-to-br from-white/5 to-transparent dark:from-gray-800/5 pointer-events-none"></div>
	{/if}
	
	{#if title}
		<div class="relative mb-6 flex items-center">
			<div class="flex-1">
				<h3 class="text-lg font-semibold text-gray-900 dark:text-white bg-gradient-to-r from-gray-900 to-gray-700 dark:from-white dark:to-gray-300 bg-clip-text text-transparent">
					{title}
				</h3>
			</div>
			<!-- Decorative element -->
			<div class="h-px flex-1 ml-4 bg-gradient-to-r from-gray-200 to-transparent dark:from-gray-700"></div>
		</div>
	{/if}
	
	<div class="relative">
		{@render children?.()}
	</div>
</div>