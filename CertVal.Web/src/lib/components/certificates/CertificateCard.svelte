<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import type { Certificate } from '$lib/types';
	import Card from '$lib/components/ui/Card.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import { goto } from '$app/navigation';

	let {
		certificate,
		workspaceName = ''
	}: {
		certificate: Certificate;
		workspaceName?: string;
	} = $props();

	const status = $derived(getCertificateStatus(certificate.notAfter));
	const statusInfo = $derived(() => {
		switch (status) {
			case 'expired':
				return {
					text: t('certificates.expired', language.current),
					textColor: 'text-error',
					barColor: 'bg-error'
				};
			case 'expiring':
				return {
					text: t('certificates.expiring', language.current),
					textColor: 'text-warning',
					barColor: 'bg-warning'
				};
			default:
				return {
					text: t('certificates.valid', language.current),
					textColor: 'text-success',
					barColor: 'bg-success'
				};
		}
	});
</script>

<Card
	class="group/card certificate-card relative h-full transition-all duration-300 hover:-translate-y-1 hover:shadow-md"
	clickable={true}
	onclick={() => goto(`/certificates/${certificate.id}`)}
	data-test-id={`certificate-card-${certificate.id}`}
>
	<div class="absolute right-2 top-2 z-20">
		{#if certificate.isSkipped}
			<div
				class="badge badge-ghost badge-sm text-base-content/50 gap-1"
				title={t('certificates.ignored', language.current)}
			>
				<Icon name="eye-off" class="h-3 w-3" />
				<span class="text-[10px] uppercase">{t('common.skipped', language.current)}</span>
			</div>
		{/if}
	</div>

	<div
		class="absolute left-0 top-0 h-full w-1 {statusInfo()
			.barColor} transition-all duration-300 group-hover/card:w-1.5"
	></div>

	<div class="flex h-full flex-col gap-2 p-3 pl-4">
		<div class="flex items-center justify-between text-xs">
			<div class="text-base-content/60 flex max-w-[70%] items-center gap-1.5 truncate">
				<Icon name="workspaces" class="h-3.5 w-3.5 flex-shrink-0" />
				<span class="truncate font-medium" title={workspaceName}>{workspaceName || '...'}</span>
			</div>
			<span class="text-[10px] font-bold uppercase tracking-wider {statusInfo().textColor}">
				{statusInfo().text}
			</span>
		</div>

		<div class="flex min-h-0 flex-grow flex-col justify-center py-0.5">
			<h3 class="line-clamp-2 text-sm font-bold leading-snug" title={certificate.subject}>
				{certificate.subject}
			</h3>
			{#if certificate.issuer}
				<p class="text-base-content/50 mt-0.5 truncate text-xs" title={certificate.issuer}>
					{certificate.issuer}
				</p>
			{/if}
		</div>

		<div class="border-base-content/5 mt-1 flex items-end justify-between border-t pt-2">
			<div>
				<div class="text-base-content/50 text-[10px] uppercase tracking-wide">
					{t('certificates.expires', language.current)}
				</div>
				<div class="font-mono text-xs font-medium">{formatDate(certificate.notAfter)}</div>
			</div>
			<div class="text-right">
				<div class="text-lg font-bold leading-none {statusInfo().textColor}">
					{certificate.daysUntilExpiry}
				</div>
				<div class="text-base-content/50 text-[10px]">
					{t('certificates.days', language.current)}
				</div>
			</div>
		</div>
	</div>
</Card>

<style>
	:global(.certificate-card .card-body) {
		padding: var(--card-p, 0.2rem) !important;
	}
</style>
