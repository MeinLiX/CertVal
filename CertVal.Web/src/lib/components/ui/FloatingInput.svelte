<script lang="ts">
	interface Props {
		value: string;
		label: string;
		type?: 'text' | 'email' | 'password';
		id: string;
		required?: boolean;
		error?: string;
		class?: string;
		'data-test-id'?: string;
	}

	let {
		value = $bindable(''),
		label,
		type = 'text',
		id,
		required = false,
		error = '',
		class: className = '',
		'data-test-id': testId
	}: Props = $props();

	let isFocused = $state(false);
	const hasValue = $derived(value && value.length > 0);
</script>

<div class="floating-input {className}">
	<input
		{type}
		{id}
		{required}
		class="floating-input__field"
		class:floating-input__field--error={error}
		class:floating-input__field--filled={hasValue || isFocused}
		bind:value
		onfocus={() => isFocused = true}
		onblur={() => isFocused = false}
		placeholder=" "
		data-test-id={testId}
	/>
	<label
		for={id}
		class="floating-input__label"
		class:floating-input__label--active={hasValue || isFocused}
		class:floating-input__label--error={error}
	>
		{label}
	</label>
	{#if error}
		<span class="floating-input__error">{error}</span>
	{/if}
</div>

<style>
	.floating-input {
		position: relative;
	}

	.floating-input__field {
		width: 100%;
		height: 52px;
		padding: 1.125rem var(--space-4) 0.375rem;
		font-size: var(--text-sm);
		font-family: var(--font-family);
		color: var(--color-text);
		background-color: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
	}

	.floating-input__field:hover:not(:focus) {
		border-color: var(--color-border-hover);
	}

	.floating-input__field:focus {
		outline: none;
		border-color: var(--color-primary);
		box-shadow: 0 0 0 3px var(--color-primary-light);
	}

	.floating-input__field--error {
		border-color: var(--color-error);
	}

	.floating-input__field--error:focus {
		box-shadow: 0 0 0 3px var(--color-error-light);
	}

	.floating-input__label {
		position: absolute;
		left: var(--space-4);
		top: 50%;
		transform: translateY(-50%);
		font-size: var(--text-sm);
		color: var(--color-text-muted);
		pointer-events: none;
		transition: top var(--transition-fast), font-size var(--transition-fast), color var(--transition-fast);
		background: transparent;
	}

	.floating-input__label--active {
		top: 0.5rem;
		transform: translateY(0);
		font-size: var(--text-xs);
		color: var(--color-text-secondary);
	}

	.floating-input__label--error {
		color: var(--color-error);
	}

	.floating-input__field:not(:placeholder-shown) + .floating-input__label {
		top: 0.5rem;
		transform: translateY(0);
		font-size: var(--text-xs);
	}

	.floating-input__error {
		display: block;
		margin-top: var(--space-1);
		font-size: var(--text-xs);
		color: var(--color-error);
	}
</style>
