<script lang="ts">
	type Size = 'sm' | 'md' | 'lg';
	let { size = 'md', class: className = '' }: { size?: Size; class?: string } = $props();

	const sizeClasses: Record<Size, string> = {
		sm: 'w-6 h-6',
		md: 'w-12 h-12',
		lg: 'w-24 h-24'
	};
</script>

<div
	class="relative flex items-center justify-center {sizeClasses[size]} {className}"
	aria-label="Loading"
	data-test-id="loader"
>
	<!-- Outer Ring -->
	<div
		class="border-primary/30 border-t-primary absolute inset-0 animate-[spin_3s_linear_infinite] rounded-full border-2"
	></div>
	<!-- Middle Ring -->
	<div
		class="border-secondary/30 border-b-secondary absolute inset-2 animate-[spin_2s_linear_infinite_reverse] rounded-full border-2"
	></div>
	<!-- Inner Core -->
	<div class="bg-accent/20 absolute inset-4 animate-pulse rounded-full backdrop-blur-sm"></div>
</div>
