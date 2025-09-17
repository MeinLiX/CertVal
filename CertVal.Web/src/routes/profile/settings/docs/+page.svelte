<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';

	const VITE_API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
	const scalarUrl = VITE_API_BASE_URL.replace('/api', '') + '/scalar/';
</script>

<svelte:head>
	<title>{t('nav.documentation', $language)}</title>
</svelte:head>

<div class="relative">
	<a
		href={scalarUrl}
		target="_blank"
		rel="noopener noreferrer"
		class="tooltip fixed tooltip-left right-6 bottom-6 z-50"
		data-tip={t('common.openDocsInNewTab', $language)}
	>
		<Button outline variant="primary" class="btn-circle shadow-lg">
			<Icon name="externalLink" class="h-6 w-6" />
		</Button>
	</a>

	<div class="h-[65vh] w-full">
		<iframe
			src={scalarUrl}
			class="h-full w-full rounded-lg border border-base-content/20"
			title="API Documentation"
		></iframe>
	</div>
</div>
