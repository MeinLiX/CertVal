<script lang="ts">
	interface Option {
		value: string;
		label: string;
	}

	interface Props {
		value?: string;
		placeholder?: string;
		label?: string;
		error?: string;
		disabled?: boolean;
		required?: boolean;
		id?: string;
		options: Option[];
		bordered?: boolean;
		ghost?: boolean;
		onchange?: (value: string) => void;
	}

	let {
		value = $bindable(''),
		placeholder = '',
		label = '',
		error = '',
		disabled = false,
		required = false,
		id = '',
		options = [],
		bordered = true,
		ghost = false,
		onchange
	}: Props = $props();

	const selectId = $derived(id || `select-${Math.random().toString(36).substr(2, 9)}`);

	function handleChange(event: Event) {
		const target = event.target as HTMLSelectElement;
		const newValue = target.value;
		value = newValue;
		onchange?.(newValue);
	}

	const selectClasses = $derived(`
		select w-full transition-all duration-200
		${bordered ? 'select-bordered' : ''}
		${ghost ? 'select-ghost' : ''}
		${error ? 'select-error' : ''}
		${disabled ? 'select-disabled' : ''}
	`.trim().replace(/\s+/g, ' '));
</script>

<div class="form-control space-y-2">
	{#if label}
		<label for={selectId} class="label">
			<span class="label-text font-medium {error ? 'text-error' : 'text-base-content'}">
				{label}
				{#if required}
					<span class="text-error ml-1">*</span>
				{/if}
			</span>
		</label>
	{/if}

	<div class="relative">
		<select
			{disabled}
			{required}
			id={selectId}
			class={selectClasses}
			bind:value
			onchange={handleChange}
		>
			{#if placeholder}
				<option value="" disabled selected>{placeholder}</option>
			{/if}
			{#each options as option}
				<option value={option.value}>{option.label}</option>
			{/each}
		</select>

		{#if error}
			<div class="absolute inset-y-0 right-8 flex items-center pr-3 pointer-events-none">
				<svg class="h-5 w-5 text-error" viewBox="0 0 20 20" fill="currentColor">
					<path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
				</svg>
			</div>
		{/if}
	</div>

	{#if error}
		<label class="label" for={selectId}>
			<span class="label-text-alt text-error flex items-center">
				<svg class="h-4 w-4 mr-1" viewBox="0 0 20 20" fill="currentColor">
					<path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
				</svg>
				{error}
			</span>
		</label>
	{/if}
</div>