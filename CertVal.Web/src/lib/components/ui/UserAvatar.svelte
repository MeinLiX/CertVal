<script lang="ts">
	interface Props {
		firstName?: string;
		lastName?: string;
		size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl';
		class?: string;
		'data-test-id'?: string;
	}

	let {
		firstName = '',
		lastName = '',
		size = 'md',
		class: className = '',
		'data-test-id': testId
	}: Props = $props();

	const initials = $derived(
		`${firstName?.charAt(0) ?? ''}${lastName?.charAt(0) ?? ''}`.toUpperCase()
	);
</script>

<div class="avatar avatar--{size} {className}" data-test-id={testId}>
	<span class="avatar__initials">{initials || '?'}</span>
</div>

<style>
	.avatar {
		display: flex;
		align-items: center;
		justify-content: center;
		background-color: var(--color-primary);
		color: var(--color-primary-text);
		border-radius: var(--radius-full);
		font-weight: var(--font-semibold);
		flex-shrink: 0;
	}

	.avatar--sm {
		width: 32px;
		height: 32px;
		font-size: var(--text-xs);
	}

	.avatar--md {
		width: 40px;
		height: 40px;
		font-size: var(--text-sm);
	}

	.avatar--lg {
		width: 64px;
		height: 64px;
		font-size: var(--text-xl);
	}

	.avatar--xl {
		width: 96px;
		height: 96px;
		font-size: var(--text-3xl);
	}

	.avatar--2xl {
		width: 128px;
		height: 128px;
		font-size: var(--text-5xl);
	}

	.avatar__initials {
		line-height: 1;
	}
</style>
