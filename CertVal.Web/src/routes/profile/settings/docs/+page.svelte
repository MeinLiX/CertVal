<script lang="ts">
	import FloatingActionButton from '$lib/components/ui/FloatingActionButton.svelte';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';

	const VITE_API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
	const scalarUrl = VITE_API_BASE_URL.replace('/api', '') + '/scalar/';
</script>

<svelte:head>
	<title>{t('nav.documentation', $language)}</title>
</svelte:head>

<div class="relative">
	<FloatingActionButton
		iconName="externalLink"
		variant="primary"
		href={scalarUrl}
		target="_blank"
		rel="noopener noreferrer"
		tooltip={t('common.openDocsInNewTab', $language)}
		tooltipPosition="left"
	/>

	<div class="h-[65vh] w-full">
		<iframe
			src={scalarUrl}
			class="border-base-content/20 h-full w-full rounded-lg border"
			title="API Documentation"
		></iframe>
	</div>
</div>
