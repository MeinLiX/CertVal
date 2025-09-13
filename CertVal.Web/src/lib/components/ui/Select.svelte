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
		block w-full rounded-xl border-0 bg-white dark:bg-gray-800 px-4 py-3 text-gray-900 dark:text-white shadow-sm ring-1 ring-inset 
		${error 
			? 'ring-red-300 dark:ring-red-700 focus:ring-2 focus:ring-red-500' 
			: 'ring-gray-300 dark:ring-gray-700 focus:ring-2 focus:ring-blue-500'
		}
		${disabled ? 'bg-gray-50 dark:bg-gray-900 cursor-not-allowed opacity-50' : 'cursor-pointer'} 
		focus:ring-inset transition-all duration-200 appearance-none
		bg-[url('data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHZpZXdCb3g9IjAgMCAyMCAyMCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTUuMjMgNy4yMUExLjc1IDEuNzUgMCAwMTYuMjkgN0wxMCAxMC45NEwxMy43MSA3QTEuNzUgMS43NSAwIDAxMTUuNzcgOC4wNkwxMS41MyAxMi4yOUMxMS4yMSAxMi42MSAxMC43OSAxMi43OCAxMC4zNSAxMi43OEMxMC4wNiAxMi43OCA5LjgxIDEyLjY5IDkuNTcgMTIuNTJMMTAuMzUgMTIuNzhMNi4wNiA4LjQ3QzUuODEgOC4yMSA1Ljc4IDcuOCA1Ljk3IDcuNTNMNi4wNiA4LjQ3TDUuMjMgNy4yMVoiIGZpbGw9IiM2QjcyODAiLz4KPC9zdmc+')] bg-no-repeat bg-[right_1rem_center] bg-[length:1rem] pr-10
	`.trim().replace(/\s+/g, ' '));

	const labelClasses = $derived(`
		block text-sm font-medium leading-6 mb-2
		${error ? 'text-red-700 dark:text-red-400' : 'text-gray-900 dark:text-white'}
		transition-colors duration-200
	`.trim().replace(/\s+/g, ' '));
</script>

<div class="space-y-2">
	{#if label}
		<label for={selectId} class={labelClasses}>
			{label}
			{#if required}
				<span class="text-red-500 ml-1">*</span>
			{/if}
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
			<div class="pointer-events-none absolute inset-y-0 right-8 flex items-center pr-3">
				<svg class="h-5 w-5 text-red-500" viewBox="0 0 20 20" fill="currentColor">
					<path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
				</svg>
			</div>
		{/if}
	</div>

	{#if error}
		<p class="text-sm text-red-600 dark:text-red-400 flex items-center mt-2">
			<svg class="h-4 w-4 mr-1" viewBox="0 0 20 20" fill="currentColor">
				<path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1zm0 8a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
			</svg>
			{error}
		</p>
	{/if}
</div>