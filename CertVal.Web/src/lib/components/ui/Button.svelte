<script lang="ts">
	import type { Snippet } from 'svelte';

	type ButtonVariant =
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
		| 'outline'
		| 'glass';

	type ButtonSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';
	type ButtonShape = 'square' | 'circle' | 'wide' | 'block';

	interface ButtonProps {
		variant?: ButtonVariant;
		size?: ButtonSize;
		shape?: ButtonShape;
		disabled?: boolean;
		loading?: boolean;
		type?: 'button' | 'submit' | 'reset';
		href?: string;
		target?: string;
		rel?: string;
		class?: string;
		onclick?: (event: MouseEvent) => void | Promise<void> | any;
		children?: Snippet;
		'data-testid'?: string;
		'data-test-id'?: string;
		'aria-label'?: string;
		'aria-describedby'?: string;
		id?: string;
		name?: string;
		value?: string;
		form?: string;
		formaction?: string;
		formnovalidate?: boolean;
		formtarget?: string;
		title?: string;
	}

	let {
		variant = 'primary',
		size = 'md',
		shape,
		disabled = false,
		loading = false,
		type = 'button',
		href,
		target,
		rel,
		class: className = '',
		onclick,
		children,
		'data-testid': testId,
		'data-test-id': testIdAlt,
		'aria-label': ariaLabel,
		'aria-describedby': ariaDescribedBy,
		id,
		name,
		value,
		form,
		formaction,
		formnovalidate,
		formtarget,
		title
	}: ButtonProps = $props();

	const baseClasses =
		'btn relative overflow-hidden transition-colors transition-shadow duration-300 hover:shadow-lg';

	const variantClasses = $derived(() => {
		const classes = {
			primary:
				'btn-primary bg-gradient-to-r from-primary to-secondary border-none text-primary-content shadow-lg shadow-primary/20 hover:shadow-primary/40',
			secondary:
				'btn-secondary bg-gradient-to-r from-secondary to-accent border-none text-secondary-content shadow-lg shadow-secondary/20 hover:shadow-secondary/40',
			accent:
				'btn-accent bg-gradient-to-r from-accent to-accent-focus border-none text-accent-content shadow-lg shadow-accent/20 hover:shadow-accent/40',
			success:
				'btn-success text-success-content shadow-lg shadow-success/20 hover:shadow-success/40',
			warning:
				'btn-warning text-warning-content shadow-lg shadow-warning/20 hover:shadow-warning/40',
			error: 'btn-error text-error-content shadow-lg shadow-error/20 hover:shadow-error/40',
			danger: 'btn-error text-error-content shadow-lg shadow-error/20 hover:shadow-error/40',
			info: 'btn-info text-info-content shadow-lg shadow-info/20 hover:shadow-info/40',
			ghost: 'btn-ghost hover:bg-base-content/10 hover:text-base-content',
			link: 'btn-link no-underline hover:underline text-primary',
			outline:
				'btn-outline border-2 hover:bg-primary hover:text-primary-content hover:border-primary',
			glass: 'glass hover:bg-white/20 text-base-content border-white/20'
		};
		return classes[variant];
	});

	const sizeClasses = $derived(() => {
		const classes = {
			xs: 'btn-xs',
			sm: 'btn-sm',
			md: 'btn-md',
			lg: 'btn-lg',
			xl: 'btn-xl'
		};
		return classes[size];
	});

	const shapeClasses = $derived(() => {
		if (!shape) return '';

		const classes = {
			square: 'btn-square',
			circle: 'btn-circle',
			wide: 'btn-wide',
			block: 'btn-block'
		};
		return classes[shape];
	});

	const computedClasses = $derived(() => {
		return [baseClasses, variantClasses(), sizeClasses(), shapeClasses(), className]
			.filter(Boolean)
			.join(' ');
	});

	const isInteractive = $derived(!disabled && !loading);

	let isProcessing = $state(false);

	async function handleClick(event: MouseEvent) {
		if (!onclick || !isInteractive || isProcessing) return;

		try {
			isProcessing = true;
			await onclick(event);
		} catch (error) {
			console.error('Button click handler error:', error);
		} finally {
			isProcessing = false;
		}
	}

	const effectiveAriaLabel = $derived(() => {
		if (ariaLabel) return ariaLabel;
		if (loading) return 'Завантаження...';
		return undefined;
	});

	const effectiveDisabled = $derived(disabled || loading || isProcessing);
</script>

{#if href}
	<a
		{id}
		{href}
		{target}
		{rel}
		{title}
		class={computedClasses()}
		aria-label={effectiveAriaLabel()}
		aria-describedby={ariaDescribedBy}
		aria-busy={loading || isProcessing}
		data-testid={testId}
		data-test-id={testIdAlt ?? testId}
		onclick={handleClick}
		role="button"
		tabindex={effectiveDisabled ? -1 : 0}
	>
		{#if loading || isProcessing}
			<span
				class="loading loading-sm loading-spinner absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2"
				aria-hidden="true"
			></span>
		{/if}

		<span class:invisible={loading || isProcessing} class="contents">
			{#if children}
				{@render children()}
			{/if}
		</span>
	</a>
{:else}
	<button
		{id}
		{name}
		{value}
		{type}
		{form}
		{formaction}
		{formnovalidate}
		{formtarget}
		{title}
		class={computedClasses()}
		disabled={effectiveDisabled}
		aria-label={effectiveAriaLabel()}
		aria-describedby={ariaDescribedBy}
		aria-busy={loading || isProcessing}
		data-testid={testId}
		data-test-id={testIdAlt ?? testId}
		onclick={handleClick}
		onkeydown={(e) => {
			if ((e.key === 'Enter' || e.key === ' ') && isInteractive) {
				e.preventDefault();
				handleClick(e as any);
			}
		}}
	>
		{#if loading || isProcessing}
			<span
				class="loading loading-sm loading-spinner absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2"
				aria-hidden="true"
			></span>
		{/if}

		<span class:invisible={loading || isProcessing} class="contents">
			{#if children}
				{@render children()}
			{/if}
		</span>
	</button>
{/if}

<style>
	.btn {
		position: relative;
		overflow: hidden;
	}

	.btn:focus-visible {
		outline: 2px solid oklch(from var(--color-primary) l c h);
		outline-offset: 2px;
	}

	.btn::after {
		content: '';
		position: absolute;
		top: 50%;
		left: 50%;
		width: 0;
		height: 0;
		background: rgba(255, 255, 255, 0.3);
		border-radius: 50%;
		transform: translate(-50%, -50%);
		transition:
			width 0.3s ease,
			height 0.3s ease;
		pointer-events: none;
	}

	.btn:active {
		animation: none;
	}

	.btn:active::after {
		width: 0;
		height: 0;
		display: none;
	}

	.btn.loading {
		pointer-events: none;
	}

	@media (prefers-contrast: high) {
		.btn {
			border-width: 2px;
		}

		.btn:focus-visible {
			outline-width: 3px;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.btn {
			transition-duration: 0.05s;
		}

		.btn::after {
			display: none;
		}
	}
</style>
