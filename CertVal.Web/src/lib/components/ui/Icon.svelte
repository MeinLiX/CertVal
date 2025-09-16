<script lang="ts">
	import { icons, type IconName } from '$lib/icons';

	let {
		name,
		class: className = '',
		...rest
	}: {
		name: IconName;
		class?: string;
		[key: string]: any;
	} = $props();

	const path = $derived(icons[name]);
</script>

<svg
	xmlns="http://www.w3.org/2000/svg"
	class="h-5 w-5 {className}"
	fill="none"
	viewBox="0 0 24 24"
	stroke="currentColor"
	stroke-width="2"
	{...rest}
>
	<path stroke-linecap="round" stroke-linejoin="round" d={path} />
</svg>
