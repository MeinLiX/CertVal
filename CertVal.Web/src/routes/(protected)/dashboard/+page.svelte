<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { withMinDelay } from '$lib/utils/loading';
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
			const [statsRes, certsRes] = await withMinDelay(
				Promise.all([
					api.get<DashboardStats>('/dashboard/stats'),
					api.get<Certificate[]>('/dashboard/expiring-certificates?daysAhead=30')
				])
			);
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
	<title>{t('dashboard.title', language.current)} - CertVal</title>
	<link rel="canonical" href="/dashboard" />
</svelte:head>

<div class="space-y-8">
	<div>
		<h1 class="text-gradient text-3xl font-bold">
			{t('dashboard.welcome', language.current)}{userFirstName ? `, ${userFirstName}` : ''}!
		</h1>
		<p class="text-base-content/70 mt-1">
			{t('dashboard.title', language.current)} • {new Date().toLocaleDateString(
				language.current === 'uk' ? 'uk-UA' : 'en-US',
				{ weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }
			)}
		</p>
	</div>

	{#if isLoading}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each { length: 6 } as _}
				<div class="skeleton h-32 w-full"></div>
			{/each}
		</div>
	{:else if stats}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
			<Card
				class="animate-fade-in-up bg-primary text-primary-content"
				style="animation-delay: 100ms;"
			>
				<h3 class="text-xl font-semibold">
					{t('dashboard.stats.totalWorkspaces', language.current)}
				</h3>
				<p class="text-5xl font-bold">{stats.totalWorkspaces}</p>
			</Card>
			<Card class="animate-fade-in-up" style="animation-delay: 150ms;">
				<h3 class="text-lg font-semibold">
					{t('dashboard.stats.totalCertificates', language.current)}
				</h3>
				<p class="text-4xl font-bold">{stats.totalCertificates}</p>
			</Card>
			<Card class="animate-fade-in-up" style="animation-delay: 200ms;">
				<h3 class="text-lg font-semibold">
					{t('dashboard.stats.validCertificates', language.current)}
				</h3>
				<p class="text-success text-4xl font-bold">{stats.validCertificates}</p>
			</Card>
			<Card class="animate-fade-in-up md:col-span-2 lg:col-span-1" style="animation-delay: 250ms;">
				<h3 class="text-lg font-semibold">
					{t('dashboard.stats.expiringIn30Days', language.current)}
				</h3>
				<p class="text-warning text-4xl font-bold">{stats.expiringIn30Days}</p>
			</Card>
			<Card class="animate-fade-in-up" style="animation-delay: 300ms;">
				<h3 class="text-lg font-semibold">
					{t('dashboard.stats.expiringIn7Days', language.current)}
				</h3>
				<p class="text-warning text-4xl font-bold">{stats.expiringIn7Days}</p>
			</Card>
			<Card class="animate-fade-in-up" style="animation-delay: 350ms;">
				<h3 class="text-lg font-semibold">
					{t('dashboard.stats.expiredCertificates', language.current)}
				</h3>
				<p class="text-error text-4xl font-bold">{stats.expiredCertificates}</p>
			</Card>
		</div>
	{/if}

	<Card
		title={t('dashboard.expiringCertificates', language.current)}
		class="animate-fade-in-up"
		style="animation-delay: 400ms;"
	>
		{#if isLoading}
			<div class="skeleton h-40 w-full"></div>
		{:else if expiringCerts.length > 0}
			<div class="overflow-x-auto">
				<table class="table">
					<thead>
						<tr>
							<th>{t('certificates.subject', language.current)}</th>
							<th>{t('certificates.expires', language.current)}</th>
							<th>{t('common.status', language.current)}</th>
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
										>{t(
											`certificates.${getCertificateStatus(cert.notAfter)}`,
											language.current
										)}</span
									>
								</td>
								<th>
									<Button size="sm" onclick={() => goto(`/certificates/${cert.id}`)}>
										{t('common.details', language.current)}
									</Button>
								</th>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{:else}
			<div class="py-8 text-center">
				<p>{t('certificates.empty', language.current)}</p>
			</div>
		{/if}
	</Card>
</div>
