<script lang="ts">
	import type { Snippet } from 'svelte';

	type CardVariant = 'default' | 'bordered' | 'glass' | 'compact' | 'side';
	type CardShadow = 'none' | 'sm' | 'md' | 'lg' | 'xl';

	interface CardProps {
		title?: string;
		subtitle?: string;
		variant?: CardVariant;
		shadow?: CardShadow;
		hover?: boolean;
		clickable?: boolean;
		class?: string;
		onclick?: (event: MouseEvent) => void | Promise<void>;
		children?: Snippet;
		header?: Snippet;
		footer?: Snippet;
		image?: Snippet;
		actions?: Snippet;
		'data-testid'?: string;
		id?: string;
		'aria-label'?: string;
		'aria-labelledby'?: string;
		role?: string;
	}

	let {
		title,
		subtitle,
		variant = 'default',
		shadow = 'md',
		hover = false,
		clickable = false,
		class: className = '',
		onclick,
		children,
		header,
		footer,
		image,
		actions,
		'data-testid': testId,
		id,
		'aria-label': ariaLabel,
		'aria-labelledby': ariaLabelledBy,
		role
	}: CardProps = $props();

	const baseClasses = 'card bg-base-100 transition-all duration-300';

	const variantClasses = $derived(() => {
		const classes = {
			default: '',
			bordered: 'card-bordered border border-base-content/10',
			glass: 'glass backdrop-blur-sm',
			compact: 'card-compact',
			side: 'card-side'
		};
		return classes[variant];
	});

	const shadowClasses = $derived(() => {
		const classes = {
			none: '',
			sm: 'shadow-sm',
			md: 'shadow-md',
			lg: 'shadow-lg',
			xl: 'shadow-xl'
		};
		return classes[shadow];
	});

	const hoverClasses = $derived(() => {
		if (!hover && !clickable) return '';
		return 'hover:shadow-xl hover:-translate-y-1 transform';
	});

	const clickableClasses = $derived(() => {
		if (!clickable && !onclick) return '';
		return 'cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2';
	});

	const computedClasses = $derived(() => {
		return [
			baseClasses,
			variantClasses(),
			shadowClasses(),
			hoverClasses(),
			clickableClasses(),
			className
		]
			.filter(Boolean)
			.join(' ');
	});

	let isProcessing = $state(false);

	async function handleClick(event: MouseEvent) {
		if (!onclick || isProcessing) return;

		try {
			isProcessing = true;
			await onclick(event);
		} catch (error) {
			console.error('Card click handler error:', error);
		} finally {
			isProcessing = false;
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (!clickable && !onclick) return;

		if (event.key === 'Enter' || event.key === ' ') {
			event.preventDefault();
			handleClick(event as any);
		}
	}

	const effectiveRole = $derived(() => {
		if (role) return role;
		if (clickable || onclick) return 'button';
		return undefined;
	});

	const roleValue = $derived(() => effectiveRole());

	const cardId = $derived(id || `card-${Math.random().toString(36).substring(2, 9)}`);
	const titleId = $derived(title ? `${cardId}-title` : undefined);
</script>

{#if roleValue() === 'button'}
	<button
		type="button"
		{id}
		class={computedClasses()}
		aria-label={ariaLabel}
		aria-labelledby={ariaLabelledBy || titleId}
		aria-busy={isProcessing}
		data-testid={testId}
		onclick={handleClick}
		onkeydown={handleKeydown}
	>
		{#if image}
			<figure class="card-image">
				{@render image()}
			</figure>
		{/if}

		{#if header}
			<div class="card-header">
				{@render header()}
			</div>
		{/if}

		<div class="card-body">
			{#if title || subtitle}
				<div class="card-header-content">
					{#if title}
						<h2 id={titleId} class="card-title text-lg font-semibold text-base-content">
							{title}
						</h2>
					{/if}
					{#if subtitle}
						<p class="card-subtitle mt-1 text-sm text-base-content/70">
							{subtitle}
						</p>
					{/if}
				</div>
			{/if}

			{#if children}
				<div class="card-content">
					{@render children()}
				</div>
			{/if}

			{#if actions}
				<div class="mt-4 card-actions">
					{@render actions()}
				</div>
			{/if}
		</div>

		{#if footer}
			<div class="card-footer">
				{@render footer()}
			</div>
		{/if}
	</button>
{:else}
	<div
		{id}
		class={computedClasses()}
		role={roleValue()}
		aria-label={ariaLabel}
		aria-labelledby={ariaLabelledBy || titleId}
		aria-busy={isProcessing}
		data-testid={testId}
		onclick={handleClick}
		onkeydown={handleKeydown}
	>
		{#if image}
			<figure class="card-image">
				{@render image()}
			</figure>
		{/if}

		{#if header}
			<div class="card-header">
				{@render header()}
			</div>
		{/if}

		<div class="card-body">
			{#if title || subtitle}
				<div class="card-header-content">
					{#if title}
						<h2 id={titleId} class="card-title text-lg font-semibold text-base-content">
							{title}
						</h2>
					{/if}
					{#if subtitle}
						<p class="card-subtitle mt-1 text-sm text-base-content/70">
							{subtitle}
						</p>
					{/if}
				</div>
			{/if}

			{#if children}
				<div class="card-content">
					{@render children()}
				</div>
			{/if}

			{#if actions}
				<div class="mt-4 card-actions">
					{@render actions()}
				</div>
			{/if}
		</div>

		{#if footer}
			<div class="card-footer">
				{@render footer()}
			</div>
		{/if}
	</div>
{/if}

<style>
	.card {
		position: relative;
		overflow: hidden;
	}

	.card:hover {
		transform: var(--card-hover-transform, translateY(-2px));
	}

	.card:focus-visible {
		outline: 2px solid oklch(from var(--color-primary) l c h);
		outline-offset: 2px;
	}

	.card.glass {
		background: oklch(from var(--color-base-100) l c h / 0.8);
		border: 1px solid oklch(from var(--color-base-content) l c h / 0.1);
	}

	.card-content {
		flex-grow: 1;
	}

	.card-actions {
		justify-self: end;
		align-self: end;
	}

	@media (max-width: 768px) {
		.card.card-side {
			flex-direction: column;
		}

		.card.card-side .card-body {
			padding-top: 0;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.card {
			--card-hover-transform: none;
			transition-duration: 0.05s;
		}
	}

	@media (prefers-contrast: high) {
		.card {
			border-width: 2px;
		}
	}

	.card[aria-busy='true'] {
		pointer-events: none;
		opacity: 0.7;
	}

	.card[aria-busy='true']::after {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background: linear-gradient(
			90deg,
			transparent 0%,
			oklch(from var(--color-base-content) l c h / 0.1) 50%,
			transparent 100%
		);
		animation: skeleton-loading 1.5s ease-in-out infinite;
	}

	@keyframes skeleton-loading {
		0%,
		100% {
			transform: translateX(-100%);
		}
		50% {
			transform: translateX(100%);
		}
	}
</style>
