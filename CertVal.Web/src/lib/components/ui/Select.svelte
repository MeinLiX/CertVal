<script lang="ts">
	import type { IconName } from '$lib/icons';
	import Icon from './Icon.svelte';

	interface Option {
		value: string | number;
		label: string;
	}

	interface Props {
		value?: string | number;
		label?: string;
		name?: string;
		id?: string;
		error?: string;
		disabled?: boolean;
		required?: boolean;
		options?: Option[];
		icon?: IconName;
		class?: string;
		onchange?: (event: Event) => void;
		'data-test-id'?: string;
	}

	let {
		value = $bindable(''),
		label = '',
		name = '',
		id = '',
		error = '',
		disabled = false,
		required = false,
		options = [],
		icon,
		class: className = '',
		onchange,
		'data-test-id': testId
	}: Props = $props();

	const selectId = $derived(id || `select-${Math.random().toString(36).substring(2, 9)}`);
</script>

<div class="select-group {className}">
	{#if label}
		<label class="select-label" class:select-label--required={required} for={selectId}>
			{label}
		</label>
	{/if}

	<div class="select-wrapper" class:select-wrapper--icon={icon}>
		{#if icon}
			<span class="select-icon">
				<Icon name={icon} />
			</span>
		{/if}

		<select
			{name}
			{disabled}
			{required}
			id={selectId}
			class="select"
			class:select--error={error}
			bind:value
			{onchange}
			aria-invalid={!!error}
			data-test-id={testId}
		>
			{#each options as option}
				<option value={option.value}>{option.label}</option>
			{/each}
		</select>
	</div>

	{#if error}
		<span class="select-error">{error}</span>
	{/if}
</div>

<style>
	.select-group {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.select-label {
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text);
	}

	.select-label--required::after {
		content: ' *';
		color: var(--color-error);
	}

	.select-wrapper {
		position: relative;
		display: flex;
	}

	.select-wrapper--icon .select {
		padding-left: var(--space-10);
	}

	.select-icon {
		position: absolute;
		left: var(--space-3);
		top: 50%;
		transform: translateY(-50%);
		color: var(--color-text-muted);
		pointer-events: none;
		display: flex;
	}

	.select {
		width: 100%;
		padding: 0 var(--space-10) 0 var(--space-3);
		height: 38px;
		font-size: var(--text-sm);
		font-family: var(--font-family);
		color: var(--color-text);
		background-color: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		cursor: pointer;
		transition: all var(--transition-fast);
		appearance: none;
		background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
		background-position: right var(--space-3) center;
		background-repeat: no-repeat;
		background-size: 1.25em;
	}

	.select:hover:not(:disabled) {
		border-color: var(--color-border-hover);
	}

	.select:focus {
		outline: none;
		border-color: var(--color-primary);
		box-shadow: 0 0 0 3px var(--color-primary-light);
	}

	.select:disabled {
		background-color: var(--color-bg-alt);
		cursor: not-allowed;
		opacity: 0.6;
	}

	.select--error {
		border-color: var(--color-error);
	}

	.select--error:focus {
		box-shadow: 0 0 0 3px var(--color-error-light);
	}

	.select-error {
		font-size: var(--text-xs);
		color: var(--color-error);
	}
</style>
