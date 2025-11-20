<script lang="ts">
	let {
		firstName,
		lastName,
		size = 'w-24',
		textSize = 'text-3xl',
		class: className = ''
	}: {
		firstName: string | undefined;
		lastName: string | undefined;
		size?: string;
		textSize?: string;
		class?: string;
	} = $props();

	const initials = $derived(
		`${firstName?.charAt(0) ?? ''}${lastName?.charAt(0) ?? ''}`.toUpperCase()
	);
</script>

<div class="avatar avatar-placeholder {className}">
	<div class="bg-neutral text-neutral-content {size} rounded-full">
		<span class={textSize}>{initials}</span>
	</div>
</div>
