<script lang="ts">
	import { icons, type IconName } from '$lib/icons';

	interface Option {
		value: string | number;
		label: string;
	}

	type SelectSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

	let {
		value = $bindable(''),
		label = '',
		name = '',
		error = '',
		disabled = false,
		required = false,
		id = '',
		options = [],
		size = 'md',
		icon,
		iconPosition = 'left',
		class: className = '',
		onchange
	}: {
		value?: string | number;
		label?: string;
		name?: string;
		error?: string;
		disabled?: boolean;
		required?: boolean;
		id?: string;
		options: Option[];
		size?: SelectSize;
		icon?: IconName;
		iconPosition?: 'left' | 'right';
		class?: string;
		onchange?: (event: Event) => void;
	} = $props();

	const selectId = $derived(id || `select-${Math.random().toString(36).substring(2, 9)}`);

	const sizeClasses = $derived(() => {
		const classes = {
			xs: 'select-xs text-xs h-8 min-h-[2rem]',
			sm: 'select-sm text-sm h-10 min-h-[2.5rem]',
			md: 'select-md text-base h-12 min-h-[3rem]',
			lg: 'select-lg text-lg h-14 min-h-[3.5rem]',
			xl: 'select-xl text-xl h-16 min-h-[4rem]'
		};
		return classes[size];
	});

	const iconPaddingClasses = $derived(() => {
		if (!icon) return '';
		return iconPosition === 'left' ? 'pl-10' : 'pr-10';
	});

	const selectClasses = $derived(
		`
    select select-bordered w-full transition-all duration-300 ease-out
    border-base-300 focus:border-primary focus:ring-2 focus:ring-primary/20
    ${sizeClasses()}
    ${iconPaddingClasses()}
    ${error ? 'select-error focus:ring-error/20' : ''}
    ${disabled ? 'select-disabled' : ''}
    ${className}
  `
			.trim()
			.replace(/\s+/g, ' ')
	);
</script>

<div class="form-control w-full">
	{#if label}
		<label for={selectId} class="label">
			<span class="label-text"
				>{label}{#if required}<span class="text-error ml-1">*</span>{/if}</span
			>
		</label>
	{/if}

	<div class="relative">
		{#if icon && iconPosition === 'left'}
			<span class="pointer-events-none absolute inset-y-0 left-0 z-10 flex items-center pl-3">
				<svg
					class="text-base-content/50 h-5 w-5"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					stroke-width="2"
				>
					<path stroke-linecap="round" stroke-linejoin="round" d={icons[icon]} />
				</svg>
			</span>
		{/if}

		<select
			{name}
			{disabled}
			{required}
			id={selectId}
			class={selectClasses}
			bind:value
			{onchange}
			aria-invalid={!!error}
		>
			{#each options as option}
				<option value={option.value}>{option.label}</option>
			{/each}
		</select>

		{#if icon && iconPosition === 'right'}
			<span class="pointer-events-none absolute inset-y-0 right-0 z-10 flex items-center pr-8">
				<svg
					class="text-base-content/50 h-5 w-5"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					stroke-width="2"
				>
					<path stroke-linecap="round" stroke-linejoin="round" d={icons[icon]} />
				</svg>
			</span>
		{/if}
	</div>

	{#if error}
		<label class="label" for={selectId}>
			<span class="label-text-alt text-error">{error}</span>
		</label>
	{/if}
</div>
