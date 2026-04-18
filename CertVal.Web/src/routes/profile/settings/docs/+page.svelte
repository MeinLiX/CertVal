<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { theme } from '$lib/stores/theme.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import FloatingActionBar from '$lib/components/layout/FloatingActionBar.svelte';

	const VITE_API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
	const scalarBaseUrl = VITE_API_BASE_URL.replace('/api', '/scalar');

	const scalarUrl = $derived(
		`${scalarBaseUrl}?darkMode=${theme.current === 'dark'}&_t=${theme.current}`
	);
</script>

<svelte:head>
	<title>{t('nav.documentation', language.current)}</title>
</svelte:head>

<div class="page" data-test-id="docs-page">
	<div class="docs-frame">
		<iframe
			src={scalarUrl}
			class="docs-frame__iframe"
			title="API Documentation"
			data-test-id="docs-iframe"
		></iframe>
	</div>

	<FloatingActionBar label={t('nav.documentation', language.current)}>
		{#snippet trailing()}
			<Button
				href={scalarUrl}
				target="_blank"
				rel="noopener noreferrer"
				variant="outline"
				data-test-id="open-docs-button"
			>
				<Icon name="externalLink" />
				{t('common.openDocsInNewTab', language.current)}
			</Button>
		{/snippet}
	</FloatingActionBar>
</div>

<style>
	.page {
		display: flex;
		flex-direction: column;
		gap: var(--space-6);
	}

	.docs-frame {
		height: 75vh;
		overflow: hidden;
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xl);
	}

	.docs-frame__iframe {
		width: 100%;
		height: 100%;
		border: none;
		background: transparent;
	}
</style>
