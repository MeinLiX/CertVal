<script lang="ts">
	let {
		title = '',
		children,
		class: className = '',
		compact = false,
		glass = false,
		bordered = true,
		shadow = true,
		hover = false,
		onclick
	}: {
		title?: string;
		children?: any;
		class?: string;
		compact?: boolean;
		glass?: boolean;
		bordered?: boolean;
		shadow?: boolean;
		hover?: boolean;
		onclick?: (event: MouseEvent) => void;
	} = $props();

	const cardClasses = $derived(
		`
		card bg-base-100/80 transition-all duration-300
		${glass ? 'glass' : ''}
		${bordered ? 'border border-base-content/10' : ''}
		${shadow ? 'shadow-lg' : ''}
		${hover ? 'hover:shadow-xl hover:-translate-y-1' : ''}
		${compact ? 'card-compact' : ''}
		${className}
	`
			.trim()
			.replace(/\s+/g, ' ')
	);
</script>

<div class={cardClasses} {onclick} {...onclick ? { role: 'button', tabindex: 0 } : {}}>
	<div class="card-body">
		{#if title}
			<h2 class="mb-4 card-title text-lg font-semibold text-base-content">{title}</h2>
		{/if}
		{@render children?.()}
	</div>
</div>
