<script lang="ts">
	interface Option {
		value: string | number;
		label: string;
	}

	let {
		value = $bindable(''),
		label = '',
		name = '',
		error = '',
		disabled = false,
		required = false,
		id = '',
		options = [],
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
		onchange?: (event: Event) => void;
	} = $props();

	const selectId = $derived(id || `select-${Math.random().toString(36).substring(2, 9)}`);

	const selectClasses = $derived(
		`
    select select-bordered w-full transition-all duration-200
    focus:select-primary focus:border-primary
    ${error ? 'select-error' : ''}
    ${disabled ? 'select-disabled' : ''}
  `
			.trim()
			.replace(/\s+/g, ' ')
	);
</script>

<div class="form-control w-full">
	{#if label}
		<label for={selectId} class="label">
			<span class="label-text"
				>{label}{#if required}<span class="ml-1 text-error">*</span>{/if}</span
			>
		</label>
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
	{#if error}
		<label class="label" for={selectId}>
			<span class="label-text-alt text-error">{error}</span>
		</label>
	{/if}
</div>
