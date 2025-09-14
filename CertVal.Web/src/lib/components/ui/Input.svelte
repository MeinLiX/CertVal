<script lang="ts">
	interface Props {
		type?: 'text' | 'email' | 'password' | 'number' | 'search' | 'file';
		value?: string | number;
		placeholder?: string;
		label?: string;
		name?: string;

		error?: string;
		disabled?: boolean;
		required?: boolean;
		multiple?: boolean;
		id?: string;
		icon?: string;
		accept?: string;
		oninput?: (value: any) => void;
	}

	let {
		type = 'text',
		value = $bindable(''),
		placeholder = '',
		label = '',
		name = '',
		error = '',
		disabled = false,
		required = false,
		multiple = false,
		id = '',
		icon = '',
		accept = '',
		oninput
	}: Props = $props();

	const inputId = $derived(id || `input-${Math.random().toString(36).substring(2, 9)}`);

	function handleInput(event: Event) {
		const target = event.target as HTMLInputElement;
		const newValue = type === 'number' ? parseFloat(target.value) : target.value;
		value = newValue;
		oninput?.(newValue);
	}

	const inputClasses = $derived(
		`
    input input-bordered w-full transition-all duration-200
    focus:input-primary focus:border-primary
    ${error ? 'input-error' : ''}
    ${disabled ? 'input-disabled' : ''}
    ${icon ? 'pl-10' : ''}
  `
			.trim()
			.replace(/\s+/g, ' ')
	);
</script>

<div class="form-control w-full">
	{#if label}
		<label for={inputId} class="label">
			<span class="label-text"
				>{label}{#if required}<span class="ml-1 text-error">*</span>{/if}</span
			>
		</label>
	{/if}
	<div class="relative">
		{#if icon}
			<span class="absolute inset-y-0 left-0 flex items-center pl-3">
				<svg
					class="h-5 w-5 text-base-content/40"
					fill="none"
					viewBox="0 0 24 24"
					stroke="currentColor"
					stroke-width="2"
				>
					<path stroke-linecap="round" stroke-linejoin="round" d={icon} />
				</svg>
			</span>
		{/if}
		<input
			{name}
			{type}
			{placeholder}
			{disabled}
			{required}
			{multiple}
			{accept}
			id={inputId}
			class={inputClasses}
			bind:value
			oninput={handleInput}
			aria-invalid={!!error}
			aria-describedby={error ? `${inputId}-error` : undefined}
		/>
	</div>
	{#if error}
		<label class="label" for={inputId}>
			<span id="{inputId}-error" class="label-text-alt text-error">{error}</span>
		</label>
	{/if}
</div>
