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
		// Allow any value because file inputs will pass FileList or File
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
		onchange,
		oninput
	}: Props = $props();

	function handleInput(event: Event) {
		const target = event.target as HTMLInputElement;
		if (type === 'file') {
			// For file inputs, forward the FileList
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
			// For file inputs, forward the FileList
			onchange?.(target.files);
			return;
		}

		const newValue = type === 'number' ? parseFloat(target.value) : target.value;
		value = newValue;
		onchange?.(newValue);
	}

	const inputClasses = $derived(`block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm ${
		error ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''
	} ${disabled ? 'bg-gray-100 cursor-not-allowed' : ''}`);
</script>

<div class="space-y-1">
	{#if label}
		<label class="block text-sm font-medium text-gray-700">
			{label}
			{#if required}<span class="text-red-500">*</span>{/if}
		</label>
	{/if}

	<input
		{type}
		{placeholder}
		{disabled}
		{required}
		{accept}
		{multiple}
		class={inputClasses}
		bind:value
		oninput={handleInput}
		onchange={handleChange}
	/>

	{#if error}
		<p class="text-sm text-red-600">{error}</p>
	{/if}
</div>