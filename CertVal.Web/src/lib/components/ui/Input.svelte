<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';
	import { icons, type IconName } from '$lib/icons';

	interface Props {
		type?: 'text' | 'email' | 'password' | 'number' | 'search' | 'file';
		value?: string | number;
		placeholder?: string;
		label?: string;
		name?: string;
		error?: string;
		disabled?: boolean;
		required?: boolean;
		multiple?: boolean;
		id?: string;
		icon?: IconName;
		accept?: string;
		oninput?: (event: Event) => void;
	}

	let {
		type = 'text',
		value = $bindable(''),
		placeholder = '',
		label = '',
		name = '',
		error = '',
		disabled = false,
		required = false,
		multiple = false,
		id = '',
		icon,
		accept = '',
		oninput
	}: Props = $props();

	const inputId = $derived(id || `input-${Math.random().toString(36).substring(2, 9)}`);

	let isDragging = $state(false);
	let inputElement = $state<HTMLInputElement | undefined>(undefined);
	let selectedFiles = $state<File[]>([]);

	function handleDragOver(e: DragEvent) {
		e.preventDefault();
		if (disabled) return;
		isDragging = true;
	}

	function handleDragLeave() {
		isDragging = false;
	}

	function handleDrop(e: DragEvent) {
		e.preventDefault();
		if (disabled || !inputElement) return;
		isDragging = false;
		if (e.dataTransfer?.files) {
			inputElement.files = e.dataTransfer.files;
			const event = new Event('input', { bubbles: true });
			inputElement.dispatchEvent(event);
		}
	}

	function handleInputChange(e: Event) {
		const target = e.target as HTMLInputElement;
		if (type === 'file') {
			selectedFiles = target.files ? Array.from(target.files) : [];
		} else {
			const newValue = type === 'number' ? parseFloat(target.value) : target.value;
			(value as any) = newValue;
		}

		if (oninput) {
			oninput(e);
		}
	}

	const supportedFormats = $derived(
		accept
			?.split(',')
			.map((f) => f.trim().toUpperCase())
			.join(', ')
	);

	const inputClasses = $derived(
		`
    input input-bordered w-full transition-all duration-200
    focus:input-primary focus:border-primary
    ${error ? 'input-error' : ''}
    ${disabled ? 'input-disabled' : ''}
    ${icon && type !== 'file' ? 'pl-10' : ''}
  `
			.trim()
			.replace(/\s+/g, ' ')
	);
</script>

<div class="form-control w-full">
	{#if label}
		<label for={inputId} class="label">
			<span class="label-text"
				>{label}{#if required}<span class="ml-1 text-error">*</span>{/if}</span
			>
		</label>
	{/if}

	{#if type === 'file'}
		<div class="relative">
			<label
				for={inputId}
				class="relative flex h-32 w-full flex-col items-center justify-center rounded-lg border-2 border-dashed border-base-content/20 bg-base-200/50 transition-colors {isDragging
					? 'border-primary bg-primary/10'
					: ''} {disabled ? 'cursor-not-allowed bg-base-200' : 'cursor-pointer hover:bg-base-200'}"
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
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d={icons['upload']}
						/>
					</svg>
					<p class="mb-2 text-sm text-base-content/70">
						<span class="font-semibold">{t('common.clickToUpload', $language)}</span>
						{t('common.orDragAndDrop', $language)}
					</p>
					{#if supportedFormats}
						<p class="text-xs text-base-content/50">{supportedFormats}</p>
					{/if}
				</div>
				<input
					bind:this={inputElement}
					id={inputId}
					{name}
					{type}
					{disabled}
					{required}
					{multiple}
					{accept}
					class="hidden"
					oninput={handleInputChange}
				/>
			</label>
		</div>

		{#if selectedFiles.length > 0}
			<div class="mt-4">
				<p class="mb-2 text-sm font-semibold">{t('common.selectedFiles', $language)}</p>
				<ul class="list-inside list-disc space-y-1 text-sm text-base-content/80">
					{#each selectedFiles as file}
						<li>
							{file.name}
							<span class="text-xs opacity-60">({(file.size / 1024).toFixed(2)} KB)</span>
						</li>
					{/each}
				</ul>
			</div>
		{/if}
	{:else}
		<div class="relative">
			{#if icon}
				<span class="pointer-events-none absolute inset-y-0 left-0 z-10 flex items-center pl-3">
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
				{name}
				{type}
				{placeholder}
				{disabled}
				{required}
				{multiple}
				{accept}
				id={inputId}
				class={inputClasses}
				bind:value
				oninput={handleInputChange}
				aria-invalid={!!error}
				aria-describedby={error ? `${inputId}-error` : undefined}
			/>
		</div>
	{/if}

	{#if error}
		<label class="label" for={inputId}>
			<span id="{inputId}-error" class="label-text-alt text-error">{error}</span>
		</label>
	{/if}
</div>
