<script lang="ts">
	import type { IconName } from '$lib/icons';
	import { icons } from '$lib/icons';
	import Icon from './Icon.svelte';

	type InputType = 'text' | 'email' | 'password' | 'number' | 'search' | 'url' | 'tel' | 'date';

	interface Props {
		type?: InputType;
		value?: string | number;
		label?: string;
		placeholder?: string;
		name?: string;
		id?: string;
		error?: string;
		hint?: string;
		disabled?: boolean;
		readonly?: boolean;
		required?: boolean;
		icon?: IconName;
		form?: string;
		class?: string;
		oninput?: (event: Event) => void;
		onchange?: (event: Event) => void;
		'data-test-id'?: string;
	}

	let {
		type = 'text',
		value = $bindable(''),
		label = '',
		placeholder = '',
		name = '',
		id = '',
		error = '',
		hint = '',
		disabled = false,
		readonly = false,
		required = false,
		icon,
		form,
		class: className = '',
		oninput,
		onchange,
		'data-test-id': testId
	}: Props = $props();

	const inputId = $derived(id || `input-${Math.random().toString(36).substring(2, 9)}`);
</script>

<div class="input-group {className}">
	{#if label}
		<label class="input-label" class:input-label--required={required} for={inputId}>
			{label}
		</label>
	{/if}

	<div class="input-wrapper" class:input-wrapper--icon={icon}>
		{#if icon}
			<span class="input-icon">
				<Icon name={icon} />
			</span>
		{/if}

		<input
			{type}
			{name}
			{placeholder}
			{disabled}
			{readonly}
			{required}
			{form}
			id={inputId}
			class="input"
			class:input--error={error}
			bind:value
			{oninput}
			{onchange}
			aria-invalid={!!error}
			data-test-id={testId}
		/>
	</div>

	{#if error}
		<span class="input-error">{error}</span>
	{:else if hint}
		<span class="input-hint">{hint}</span>
	{/if}
</div>

<style>
	.input-group {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.input-label {
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text);
	}

	.input-label--required::after {
		content: ' *';
		color: var(--color-error);
	}

	.input-wrapper {
		position: relative;
		display: flex;
	}

	.input-wrapper--icon .input {
		padding-left: var(--space-10);
	}

	.input-icon {
		position: absolute;
		left: var(--space-3);
		top: 50%;
		transform: translateY(-50%);
		color: var(--color-text-muted);
		pointer-events: none;
		display: flex;
	}

	.input {
		width: 100%;
		padding: 0 var(--space-3);
		height: 38px;
		font-size: var(--text-sm);
		font-family: var(--font-family);
		color: var(--color-text);
		background-color: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
	}

	.input:focus {
		outline: none;
		border-color: var(--color-primary);
		box-shadow: 0 0 0 3px var(--color-primary-light);
	}

	.input:hover:not(:disabled):not(:focus) {
		border-color: var(--color-border-hover);
	}

	.input::placeholder {
		color: var(--color-text-muted);
	}

	.input:disabled {
		background-color: var(--color-bg-alt);
		cursor: not-allowed;
		opacity: 0.6;
	}

	.input--error {
		border-color: var(--color-error);
	}

	.input--error:focus {
		box-shadow: 0 0 0 3px var(--color-error-light);
	}

	.input-hint {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.input-error {
		font-size: var(--text-xs);
		color: var(--color-error);
	}
</style>
