<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import type { Certificate } from '$lib/types';
	import Card from '$lib/components/ui/Card.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import { goto } from '$app/navigation';

	let { certificate, workspaceName = '' }: { certificate: Certificate; workspaceName?: string } =
		$props();

	const status = $derived(getCertificateStatus(certificate.notAfter));
	const statusInfo = $derived(() => {
		switch (status) {
			case 'expired':
				return {
					text: t('certificates.expired', $language),
					textColor: 'text-error',
					barColor: 'bg-error'
				};
			case 'expiring':
				return {
					text: t('certificates.expiring', $language),
					textColor: 'text-warning',
					barColor: 'bg-warning'
				};
			default:
				return {
					text: t('certificates.valid', $language),
					textColor: 'text-success',
					barColor: 'bg-success'
				};
		}
	});
</script>

<Card
	class="group/card relative h-full !p-0 transition-all duration-300 hover:-translate-y-1 hover:shadow-lg"
	clickable={true}
	onclick={() => goto(`/certificates/${certificate.id}`)}
>
	<div
		class="absolute left-0 top-0 h-full w-1.5 {statusInfo()
			.barColor} transition-all duration-300 group-hover/card:w-2"
	></div>

	<div class="flex h-full flex-col p-5 pl-6">
		<div class="flex items-start justify-between">
			<div class="text-base-content/60 flex items-center gap-2 text-xs">
				<Icon name="workspaces" class="h-4 w-4" />
				<span class="truncate" title={workspaceName}>{workspaceName || '...'}</span>
			</div>
			<span class="text-xs font-bold uppercase {statusInfo().textColor}">
				{statusInfo().text}
			</span>
		</div>

		<div class="my-4 flex-grow">
			<h3 class="line-clamp-3 text-lg font-semibold leading-tight" title={certificate.subject}>
				{certificate.subject}
			</h3>
			<p class="text-base-content/60 mt-1 truncate text-sm" title={certificate.issuer}>
				{certificate.issuer}
			</p>
		</div>

		<div class="border-base-content/10 mt-auto flex items-end justify-between border-t pt-4">
			<div>
				<div class="text-base-content/60 text-xs">{t('certificates.expires', $language)}</div>
				<div class="font-semibold">{formatDate(certificate.notAfter)}</div>
			</div>
			<div class="text-right">
				<div class="text-2xl font-bold {statusInfo().textColor}">
					{certificate.daysUntilExpiry}
				</div>
				<div class="text-base-content/60 -mt-1 text-xs">{t('certificates.days', $language)}</div>
			</div>
		</div>
	</div>
</Card>
