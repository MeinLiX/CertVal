<script lang="ts">
	interface Props {
		type?: 'text' | 'email' | 'password' | 'number' | 'search' | 'file';
		value?: string | number;
		placeholder?: string;
		label?: string;
		error?: string;
		disabled?: boolean;
		required?: boolean;
		accept?: string;
		multiple?: boolean;
		id?: string;
		icon?: string;
		bordered?: boolean;
		ghost?: boolean;
		onchange?: (value: any) => void;
		oninput?: (value: any) => void;
	}

	let {
		type = 'text',
		value = $bindable(''),
		placeholder = '',
		label = '',
		error = '',
		disabled = false,
		required = false,
		accept = '',
		multiple = false,
		id = '',
		icon = '',
		bordered = true,
		ghost = false,
		onchange,
		oninput
	}: Props = $props();

	const inputId = $derived(id || `input-${Math.random().toString(36).substr(2, 9)}`);

	function handleInput(event: Event) {
		const target = event.target as HTMLInputElement;
		if (type === 'file') {
			oninput?.(target.files);
			return;
		}

		const newValue = type === 'number' ? parseFloat(target.value) : target.value;
		value = newValue;
		oninput?.(newValue);
	}

	function handleChange(event: Event) {
		const target = event.target as HTMLInputElement;
		if (type === 'file') {
			onchange?.(target.files);
			return;
		}

		const newValue = type === 'number' ? parseFloat(target.value) : target.value;
		value = newValue;
		onchange?.(newValue);
	}

	const inputClasses = $derived(`
		input w-full transition-all duration-200
		${bordered ? 'input-bordered' : ''}
		${ghost ? 'input-ghost' : ''}
		${error ? 'input-error' : ''}
		${disabled ? 'input-disabled' : ''}
	`.trim().replace(/\s+/g, ' '));
</script>

<div class="form-control space-y-2">
	{#if label}
		<label for={inputId} class="label">
			<span class="label-text font-medium {error ? 'text-error' : 'text-base-content'}">
				{label}
				{#if required}
					<span class="text-error ml-1">*</span>
				{/if}
			</span>
		</label>
	{/if}

	<div class="relative">
		{#if icon}
			<div class="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
				<svg class="h-5 w-5 text-base-content/40" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					<path stroke-linecap="round" stroke-linejoin="round" d={icon} />
				</svg>
			</div>
		{/if}
		
		<input
			{type}
			{placeholder}
			{disabled}
			{required}
			{accept}
			{multiple}
			id={inputId}
			class="{inputClasses} {icon ? 'pl-10' : ''}"
			bind:value
			oninput={handleInput}
			onchange={handleChange}
			aria-describedby={error ? `${inputId}-error` : undefined}
		/>

		{#if error}
			<div class="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
				<svg class="h-5 w-5 text-error" viewBox="0 0 20 20" fill="currentColor">
					<path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
				</svg>
			</div>
		{/if}
	</div>

	{#if error}
		<span id="{inputId}-error" class="label label-text-alt text-error flex items-center">
			<svg class="h-4 w-4 mr-1" viewBox="0 0 20 20" fill="currentColor">
				<path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
			</svg>
			{error}
		</span>
	{/if}
</div>