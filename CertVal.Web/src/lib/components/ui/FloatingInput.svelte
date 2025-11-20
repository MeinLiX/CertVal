<script lang="ts">
	import { slide } from 'svelte/transition';

	interface Props {
		value: string;
		label: string;
		type?: string;
		id: string;
		required?: boolean;
		class?: string;
		error?: string;
		'data-test-id'?: string;
	}

	let {
		value = $bindable(),
		label,
		type = 'text',
		id,
		required = false,
		class: className = '',
		error,
		'data-test-id': testId
	}: Props = $props();
</script>

<div class="relative {className}">
	<input
		{type}
		{id}
		bind:value
		{required}
		class="border-base-content/10 bg-base-100/50 text-base-content focus:border-primary focus:bg-base-100/80 peer block w-full appearance-none rounded-xl border px-4 pb-3 pt-6 text-sm backdrop-blur-md transition-all duration-300 focus:outline-none focus:ring-0 {error
			? 'border-error'
			: ''}"
		placeholder=" "
		data-test-id={testId}
	/>
	<label
		for={id}
		class="text-base-content/60 peer-focus:text-primary absolute left-4 top-4 z-10 origin-[0] -translate-y-3 scale-75 transform cursor-text select-none text-sm duration-300 peer-placeholder-shown:translate-y-0 peer-placeholder-shown:scale-100 peer-focus:-translate-y-3 peer-focus:scale-75 {error
			? 'text-error'
			: ''}"
	>
		{label}
	</label>
	{#if error}
		<div class="text-error mt-1 text-xs" transition:slide>{error}</div>
	{/if}
</div>
