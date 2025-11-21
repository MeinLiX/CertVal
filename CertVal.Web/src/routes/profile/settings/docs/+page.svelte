<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { theme } from '$lib/stores/theme.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';

	const VITE_API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
	const scalarBaseUrl = VITE_API_BASE_URL.replace('/api', '/scalar');

	const scalarUrl = $derived(`${scalarBaseUrl}?theme=${theme.current}`);
</script>

<svelte:head>
	<title>{t('nav.documentation', language.current)}</title>
</svelte:head>

<div
	class="animate-in fade-in slide-in-from-bottom-4 space-y-6 duration-500"
	data-test-id="docs-page"
>
	<div class="border-base-content/10 flex items-center justify-between border-b pb-6">
		<div>
			<h1
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-3xl font-bold text-transparent"
			>
				{t('nav.documentation', language.current)}
			</h1>
			<p class="text-base-content/60 mt-1 font-light">
				{t('profile.documentationSubtitle', language.current)}
			</p>
		</div>
		<Button
			href={scalarUrl}
			target="_blank"
			rel="noopener noreferrer"
			variant="outline"
			class="gap-2"
			data-test-id="open-docs-button"
		>
			<Icon name="externalLink" class="h-4 w-4" />
			{t('common.openDocsInNewTab', language.current)}
		</Button>
	</div>

	<div
		class="border-base-content/10 bg-base-100/50 h-[65vh] overflow-hidden rounded-2xl border shadow-sm backdrop-blur-sm"
	>
		<iframe
			src={scalarUrl}
			class="h-full w-full bg-transparent"
			title="API Documentation"
			data-test-id="docs-iframe"
		></iframe>
	</div>
</div>
