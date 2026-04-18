<script lang="ts">
	import { icons, type IconName } from '$lib/icons';

	interface Props {
		name: IconName;
		size?: 'sm' | 'md' | 'lg';
		class?: string;
	}

	let {
		name,
		size = 'md',
		class: className = ''
	}: Props = $props();

	const path = $derived(icons[name]);

	const sizeMap = {
		sm: 16,
		md: 20,
		lg: 24
	};

	const iconSize = $derived(sizeMap[size]);
</script>

<svg
	xmlns="http://www.w3.org/2000/svg"
	width={iconSize}
	height={iconSize}
	viewBox="0 0 24 24"
	fill="none"
	stroke="currentColor"
	stroke-width="2"
	stroke-linecap="round"
	stroke-linejoin="round"
	class="icon icon--{size} {className}"
	aria-hidden="true"
>
	<path d={path} />
</svg>

<style>
	.icon {
		display: inline-block;
		flex-shrink: 0;
		vertical-align: middle;
	}
</style>
