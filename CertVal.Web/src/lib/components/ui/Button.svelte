<script lang="ts">
	type ButtonVariant =
		| 'primary'
		| 'secondary'
		| 'accent'
		| 'success'
		| 'danger'
		| 'warning'
		| 'info'
		| 'ghost'
		| 'link';
	type ButtonSize = 'xs' | 'sm' | 'md' | 'lg';

	let {
		variant = 'primary',
		size = 'md',
		disabled = false,
		loading = false,
		type = 'button',
		wide = false,
		outline = false,
		class: className = '',
		onclick,
		children
	}: {
		variant?: ButtonVariant;
		size?: ButtonSize;
		disabled?: boolean;
		loading?: boolean;
		type?: 'button' | 'submit' | 'reset';
		wide?: boolean;
		outline?: boolean;
		class?: string;
		onclick?: (event: MouseEvent) => void;
		children?: any;
	} = $props();

	const baseClasses = 'btn transition-all duration-200 ease-in-out transform hover:scale-105';

	const variantMap = {
		primary: 'btn-primary',
		secondary: 'btn-secondary',
		accent: 'btn-accent',
		success: 'btn-success',
		danger: 'btn-error',
		warning: 'btn-warning',
		info: 'btn-info',
		ghost: 'btn-ghost',
		link: 'btn-link'
	};

	const sizeMap = {
		xs: 'btn-xs',
		sm: 'btn-sm',
		md: 'btn-md',
		lg: 'btn-lg'
	};

	const classes = $derived(
		`${baseClasses} ${variantMap[variant]} ${sizeMap[size]} ${wide ? 'btn-wide' : ''} ${outline ? 'btn-outline' : ''} ${className}`
	);
</script>

<button {type} {disabled} class={classes} {onclick} class:loading>
	{#if loading}
		<span class="loading loading-spinner"></span>
	{/if}
	{@render children?.()}
</button>
