<script lang="ts">
	type ButtonVariant = 'primary' | 'secondary' | 'success' | 'danger' | 'outline' | 'ghost' | 'warning' | 'info' | 'neutral';
	type ButtonSize = 'xs' | 'sm' | 'md' | 'lg';

	interface Props {
		variant?: ButtonVariant;
		size?: ButtonSize;
		disabled?: boolean;
		loading?: boolean;
		type?: 'button' | 'submit' | 'reset';
		wide?: boolean;
		glass?: boolean;
		class?: string;
		onclick?: () => void;
		children?: any;
	}

	let {
		variant = 'primary',
		size = 'md',
		disabled = false,
		loading = false,
		type = 'button',
		wide = false,
		glass = false,
		class: className = '',
		onclick,
		children
	}: Props = $props();

	const baseClasses = 'btn btn-enhance transition-all duration-200';

	const variantMap = {
		primary: 'btn-primary',
		secondary: 'btn-secondary',
		success: 'btn-success',
		danger: 'btn-error',
		outline: 'btn-outline',
		ghost: 'btn-ghost',
		warning: 'btn-warning',
		info: 'btn-info',
		neutral: 'btn-neutral'
	};

	const sizeMap = {
		xs: 'btn-xs',
		sm: 'btn-sm',
		md: 'btn-md',
		lg: 'btn-lg'
	};

	const classes = $derived(`${baseClasses} ${variantMap[variant]} ${sizeMap[size]} ${wide ? 'btn-wide' : ''} ${glass ? 'glass' : ''} ${className}`);
</script>

<button 
	{type} 
	{disabled} 
	class={classes} 
	{onclick}
	class:loading
>
	{#if loading}
		<span class="loading loading-spinner loading-sm"></span>
	{/if}
	{@render children?.()}
</button>