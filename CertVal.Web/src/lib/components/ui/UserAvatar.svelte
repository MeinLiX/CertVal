<script lang="ts">
	let {
		firstName,
		lastName,
		size = 'w-24'
	}: {
		firstName: string | undefined;
		lastName: string | undefined;
		size?: string;
	} = $props();

	const initials = $derived(
		`${firstName?.charAt(0) ?? ''}${lastName?.charAt(0) ?? ''}`.toUpperCase()
	);
</script>

<div class="placeholder avatar">
	<div class="rounded-full bg-primary text-primary-content {size}">
		<span class="text-3xl">{initials}</span>
	</div>
</div>
