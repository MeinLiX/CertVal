<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import type { DashboardStats, Certificate } from '$lib/types';

	let stats = $state<DashboardStats | null>(null);
	let expiringCerts = $state<Certificate[]>([]);
	let isLoading = $state(true);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadDashboardData();
	});

	async function loadDashboardData() {
		isLoading = true;
		try {
			const [statsRes, certsRes] = await Promise.all([
				api.get<DashboardStats>('/v1/dashboard/stats'),
				api.get<Certificate[]>('/v1/dashboard/expiring-certificates?daysAhead=30')
			]);
			if (statsRes.data) stats = statsRes.data;
			if (certsRes.data) expiringCerts = certsRes.data;
		} catch (error) {
			console.error('Failed to load dashboard data:', error);
		} finally {
			isLoading = false;
		}
	}

	const userFirstName = $derived($auth.user?.firstName || '');
</script>

<svelte:head>
	<title>{t('dashboard.title', $language)} - CertVal</title>
</svelte:head>

<div class="space-y-8">
	<div>
		<h1 class="text-gradient text-3xl font-bold">
			{t('dashboard.welcome', $language)}{userFirstName ? `, ${userFirstName}` : ''}!
		</h1>
		<p class="mt-1 text-base-content/70">
			{t('dashboard.title', $language)} • {new Date().toLocaleDateString(
				$language === 'uk' ? 'uk-UA' : 'en-US',
				{ weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }
			)}
		</p>
	</div>

	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each { length: 6 } as _}
				<div class="h-32 w-full skeleton"></div>
			{/each}
		</div>
	{:else if stats}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			<Card
				class="animate-fade-in-up bg-primary text-primary-content"
				style="animation-delay: 100ms;"
			>
				<h3 class="text-xl font-semibold">{t('dashboard.stats.totalWorkspaces', $language)}</h3>
				<p class="text-5xl font-bold">{stats.totalWorkspaces}</p>
			</Card>
			<Card class="animate-fade-in-up" style="animation-delay: 150ms;">
				<h3 class="text-lg font-semibold">{t('dashboard.stats.totalCertificates', $language)}</h3>
				<p class="text-4xl font-bold">{stats.totalCertificates}</p>
			</Card>
			<Card class="animate-fade-in-up" style="animation-delay: 200ms;">
				<h3 class="text-lg font-semibold">{t('dashboard.stats.validCertificates', $language)}</h3>
				<p class="text-4xl font-bold text-success">{stats.validCertificates}</p>
			</Card>
			<Card class="animate-fade-in-up md:col-span-2 lg:col-span-1" style="animation-delay: 250ms;">
				<h3 class="text-lg font-semibold">{t('dashboard.stats.expiringIn30Days', $language)}</h3>
				<p class="text-4xl font-bold text-warning">{stats.expiringIn30Days}</p>
			</Card>
			<Card class="animate-fade-in-up" style="animation-delay: 300ms;">
				<h3 class="text-lg font-semibold">{t('dashboard.stats.expiringIn7Days', $language)}</h3>
				<p class="text-4xl font-bold text-warning">{stats.expiringIn7Days}</p>
			</Card>
			<Card class="animate-fade-in-up" style="animation-delay: 350ms;">
				<h3 class="text-lg font-semibold">{t('dashboard.stats.expiredCertificates', $language)}</h3>
				<p class="text-4xl font-bold text-error">{stats.expiredCertificates}</p>
			</Card>
		</div>
	{/if}

	<Card
		title={t('dashboard.expiringCertificates', $language)}
		class="animate-fade-in-up"
		style="animation-delay: 400ms;"
	>
		{#if isLoading}
			<div class="h-40 w-full skeleton"></div>
		{:else if expiringCerts.length > 0}
			<div class="overflow-x-auto">
				<table class="table">
					<thead>
						<tr>
							<th>{t('certificates.subject', $language)}</th>
							<th>{t('certificates.expires', $language)}</th>
							<th>{t('common.status', $language)}</th>
							<th></th>
						</tr>
					</thead>
					<tbody>
						{#each expiringCerts.slice(0, 5) as cert}
							<tr>
								<td>
									<div class="max-w-xs truncate font-bold">{cert.subject}</div>
									<div class="text-sm opacity-50">{cert.originalFileName}</div>
								</td>
								<td>{formatDate(cert.notAfter)}</td>
								<td>
									<span class="badge badge-sm badge-warning"
										>{t(`certificates.${getCertificateStatus(cert.notAfter)}`, $language)}</span
									>
								</td>
								<th>
									<Button size="sm" onclick={() => goto(`/certificates/${cert.id}`)}>
										{t('common.details', $language)}
									</Button>
								</th>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{:else}
			<div class="py-8 text-center">
				<p>{t('certificates.empty', $language)}</p>
			</div>
		{/if}
	</Card>
</div>
