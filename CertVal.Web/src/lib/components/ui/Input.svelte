<script lang="ts">
	import type { Snippet } from 'svelte';
	import { icons, type IconName } from '$lib/icons';
	import { language } from '$lib/stores/language';
	import { t } from '$lib/i18n';

	type InputType =
		| 'text'
		| 'email'
		| 'password'
		| 'number'
		| 'search'
		| 'url'
		| 'tel'
		| 'file'
		| 'hidden'
		| 'date'
		| 'datetime-local'
		| 'time';

	type InputSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';
	type InputVariant = 'default' | 'bordered' | 'ghost';

	interface InputProps {
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
		multiple?: boolean;
		size?: InputSize;
		variant?: InputVariant;
		icon?: IconName;
		iconPosition?: 'left' | 'right';
		accept?: string;
		min?: string | number;
		max?: string | number;
		step?: string | number;
		pattern?: string;
		spellcheck?: boolean;
		class?: string;
		inputClass?: string;
		labelClass?: string;
		oninput?: (event: Event) => void;
		onchange?: (event: Event) => void;
		onfocus?: (event: FocusEvent) => void;
		onblur?: (event: FocusEvent) => void;
		children?: Snippet;
		'data-testid'?: string;
		'aria-describedby'?: string;
		'aria-invalid'?: boolean;
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
		multiple = false,
		size = 'md',
		variant = 'bordered',
		icon,
		iconPosition = 'left',
		accept = '',
		min,
		max,
		step,
		pattern,
		spellcheck,
		class: className = '',
		inputClass = '',
		labelClass = '',
		oninput,
		onchange,
		onfocus,
		onblur,
		children,
		'data-testid': testId,
		'aria-describedby': ariaDescribedBy,
		'aria-invalid': ariaInvalid
	}: InputProps = $props();

	let isDragging = $state(false);
	let selectedFiles = $state<File[]>([]);
	let inputElement = $state<HTMLInputElement>();

	const inputId = $derived(id || `input-${Math.random().toString(36).substring(2, 9)}`);
	const errorId = $derived(`${inputId}-error`);
	const hintId = $derived(`${inputId}-hint`);

	const baseClasses = 'input w-full transition-all duration-200';

	const variantClasses = $derived(() => {
		const classes = {
			default: '',
			bordered: 'input-bordered',
			ghost: 'input-ghost'
		};
		return classes[variant];
	});

	const sizeClasses = $derived(() => {
		const classes = {
			xs: 'input-xs',
			sm: 'input-sm',
			md: 'input-md',
			lg: 'input-lg',
			xl: 'input-xl'
		};
		return classes[size];
	});

	const stateClasses = $derived(() => {
		if (error) return 'input-error focus:input-error';
		return 'focus:input-primary focus:border-primary';
	});

	const iconPaddingClasses = $derived(() => {
		if (!icon || type === 'file') return '';
		return iconPosition === 'left' ? 'pl-10' : 'pr-10';
	});

	const computedInputClasses = $derived(() => {
		return [
			baseClasses,
			variantClasses(),
			sizeClasses(),
			stateClasses(),
			iconPaddingClasses(),
			inputClass
		]
			.filter(Boolean)
			.join(' ');
	});

	const ariaAttributes = $derived(() => {
		const attrs: Record<string, any> = {};

		if (error) {
			attrs['aria-invalid'] = true;
			attrs['aria-describedby'] = errorId;
		} else if (hint) {
			attrs['aria-describedby'] = hintId;
		}

		if (ariaDescribedBy) {
			attrs['aria-describedby'] = ariaDescribedBy;
		}

		if (ariaInvalid !== undefined) {
			attrs['aria-invalid'] = ariaInvalid;
		}

		return attrs;
	});

	function handleInput(event: Event) {
		const target = event.target as HTMLInputElement;

		if (type === 'file') {
			selectedFiles = target.files ? Array.from(target.files) : [];
		} else {
			const newValue =
				type === 'number' ? (target.value === '' ? '' : Number(target.value)) : target.value;
			value = newValue;
		}

		oninput?.(event);
	}

	function handleChange(event: Event) {
		onchange?.(event);
	}

	function handleFocus(event: FocusEvent) {
		onfocus?.(event);
	}

	function handleBlur(event: FocusEvent) {
		onblur?.(event);
	}

	function handleDragOver(event: DragEvent) {
		event.preventDefault();
		if (disabled || type !== 'file') return;
		isDragging = true;
	}

	function handleDragLeave() {
		isDragging = false;
	}

	function handleDrop(event: DragEvent) {
		event.preventDefault();
		if (disabled || !inputElement || type !== 'file') return;

		isDragging = false;

		if (event.dataTransfer?.files) {
			inputElement.files = event.dataTransfer.files;
			const syntheticEvent = new Event('input', { bubbles: true });
			inputElement.dispatchEvent(syntheticEvent);
		}
	}

	const supportedFormats = $derived(() => {
		if (type !== 'file' || !accept) return '';
		return accept
			.split(',')
			.map((format) => format.trim().toUpperCase())
			.join(', ');
	});
</script>

<div class="form-control w-full {className}">
	{#if label}
		<label for={inputId} class="label {labelClass}">
			<span class="label-text font-medium">
				{label}
				{#if required}
					<span class="ml-1 text-error" aria-label="Обов'язкове поле">*</span>
				{/if}
			</span>
		</label>
	{/if}

	{#if type === 'file'}
		<div class="relative">
			<label
				for={inputId}
				class="flex h-32 w-full flex-col items-center justify-center rounded-lg border-2 border-dashed transition-all duration-200
				{isDragging
					? 'border-primary bg-primary/10'
					: error
						? 'border-error bg-error/5'
						: 'border-base-content/20 bg-base-200/50'}
				{disabled
					? 'cursor-not-allowed opacity-50'
					: 'cursor-pointer hover:border-primary hover:bg-primary/5'}"
				ondragover={handleDragOver}
				ondragleave={handleDragLeave}
				ondrop={handleDrop}
			>
				<div class="flex flex-col items-center justify-center pt-5 pb-6 text-center">
					<svg
						xmlns="http://www.w3.org/2000/svg"
						class="mb-4 h-8 w-8 text-base-content/60"
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
						stroke-width="2"
					>
						<path stroke-linecap="round" stroke-linejoin="round" d={icons.upload} />
					</svg>

					<p class="mb-2 text-sm text-base-content/70">
						<span class="font-semibold">{t('common.clickToUpload', $language)}</span>
						{t('common.orDragAndDrop', $language)}
					</p>

					{#if supportedFormats()}
						<p class="text-xs text-base-content/50">{supportedFormats()}</p>
					{/if}
				</div>

				<input
					bind:this={inputElement}
					{id}
					{name}
					{type}
					{disabled}
					{readonly}
					{required}
					{multiple}
					{accept}
					class="hidden"
					data-testid={testId}
					{...ariaAttributes()}
					oninput={handleInput}
					onchange={handleChange}
					onfocus={handleFocus}
					onblur={handleBlur}
				/>
			</label>
		</div>

		{#if selectedFiles.length > 0}
			<div class="mt-4">
				<p class="mb-2 text-sm font-semibold">{t('common.selectedFiles', $language)}</p>
				<ul class="space-y-1 text-sm text-base-content/80">
					{#each selectedFiles as file}
						<li class="flex items-center justify-between rounded bg-base-200 px-3 py-2">
							<span class="truncate">{file.name}</span>
							<span class="ml-2 text-xs opacity-60">
								{(file.size / 1024).toFixed(2)} KB
							</span>
						</li>
					{/each}
				</ul>
			</div>
		{/if}
	{:else}
		<div class="relative">
			{#if icon && iconPosition === 'left'}
				<span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
					<svg
						class="h-5 w-5 text-base-content/50"
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
						stroke-width="2"
					>
						<path stroke-linecap="round" stroke-linejoin="round" d={icons[icon]} />
					</svg>
				</span>
			{/if}

			<input
				bind:this={inputElement}
				bind:value
				{id}
				{name}
				{type}
				{placeholder}
				{disabled}
				{readonly}
				{required}
				{multiple}
				{accept}
				{min}
				{max}
				{step}
				{pattern}
				{spellcheck}
				class={computedInputClasses()}
				data-testid={testId}
				{...ariaAttributes()}
				oninput={handleInput}
				onchange={handleChange}
				onfocus={handleFocus}
				onblur={handleBlur}
			/>

			{#if icon && iconPosition === 'right'}
				<span class="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3">
					<svg
						class="h-5 w-5 text-base-content/50"
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
						stroke-width="2"
					>
						<path stroke-linecap="round" stroke-linejoin="round" d={icons[icon]} />
					</svg>
				</span>
			{/if}
		</div>
	{/if}

	{#if children}
		<div class="mt-2">
			{@render children()}
		</div>
	{/if}

	{#if hint && !error}
		<label class="label" for={inputId}>
			<span id={hintId} class="label-text-alt text-base-content/60">{hint}</span>
		</label>
	{/if}

	{#if error}
		<label class="label" for={inputId}>
			<span id={errorId} class="label-text-alt flex items-center gap-1 text-error">
				<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
					<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d={icons.error} />
				</svg>
				{error}
			</span>
		</label>
	{/if}
</div>

<style>
	.input:focus {
		box-shadow:
			0 0 0 2px oklch(from var(--color-primary) l c h / 0.2),
			0 1px 3px 0 rgb(0 0 0 / 0.1);
	}

	.input-error:focus {
		box-shadow:
			0 0 0 2px oklch(from var(--color-error) l c h / 0.2),
			0 1px 3px 0 rgb(0 0 0 / 0.1);
	}

	label[for] {
		transition: all 0.2s ease;
	}

	@media (prefers-contrast: high) {
		.input {
			border-width: 2px;
		}

		.input:focus {
			outline: 2px solid currentColor;
			outline-offset: 2px;
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.input,
		label[for] {
			transition-duration: 0.05s;
		}
	}

	.input[aria-busy='true'] {
		background-image: url("data:image/svg+xml,%3csvg width='100' height='100' xmlns='http://www.w3.org/2000/svg'%3e%3cpath d='m6.758 4.757 6.849 6.849m0-6.849-6.849 6.849' stroke='%23000' stroke-width='2' fill='none'/%3e%3c/svg%3e");
		background-repeat: no-repeat;
		background-position: right 0.5rem center;
		background-size: 1rem;
		animation: spin 1s linear infinite;
	}

	@keyframes spin {
		from {
			transform: rotate(0deg);
		}
		to {
			transform: rotate(360deg);
		}
	}
</style>
