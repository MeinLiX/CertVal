<script lang="ts">
	interface Props {
		width?: string;
		height?: string;
		radius?: string;
		class?: string;
	}

	let {
		width = '100%',
		height = '16px',
		radius = 'var(--radius-sm)',
		class: className = ''
	}: Props = $props();
</script>

<span
	class="skeleton {className}"
	style:width
	style:height
	style:border-radius={radius}
	aria-hidden="true"
></span>

<style>
	.skeleton {
		display: inline-block;
		background: linear-gradient(
			90deg,
			var(--color-surface-inset) 0%,
			var(--color-surface-hover) 50%,
			var(--color-surface-inset) 100%
		);
		background-size: 200% 100%;
		animation: shimmer 1.4s linear infinite;
	}
</style>
