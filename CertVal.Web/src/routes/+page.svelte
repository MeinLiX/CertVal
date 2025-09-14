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

	let dashboardStats = $state<DashboardStats | null>(null);
	let expiringCertificates = $state<Certificate[]>([]);
	let isLoading = $state(true);
	let mounted = $state(false);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		await loadDashboardData();
		mounted = true;
	});

	async function loadDashboardData() {
		try {
			// Load dashboard stats
			const statsResponse = await api.get<DashboardStats>('/v1/dashboard/stats');
			if (statsResponse.data) {
				dashboardStats = statsResponse.data;
			}

			// Load expiring certificates
			const certsResponse = await api.get<Certificate[]>(
				'/v1/dashboard/expiring-certificates?daysAhead=30'
			);
			if (certsResponse.data) {
				expiringCertificates = certsResponse.data;
			}
		} catch (error) {
			console.error('Failed to load dashboard data:', error);
		} finally {
			isLoading = false;
		}
	}

	function getStatusBadgeClass(status: 'expired' | 'expiring' | 'valid'): string {
		switch (status) {
			case 'expired':
				return 'badge badge-error';
			case 'expiring':
				return 'badge badge-warning';
			case 'valid':
				return 'badge badge-success';
		}
	}

	function getStatusText(status: 'expired' | 'expiring' | 'valid'): string {
		return t(`certificates.${status}`, $language);
	}

	const userFirstName = $derived($auth.user?.firstName || '');

	const statCards = $derived([
		{
			title: t('dashboard.stats.totalWorkspaces', $language),
			value: dashboardStats?.totalWorkspaces || 0,
			icon: 'M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6',
			color: 'text-primary',
			bgColor: 'bg-primary/10'
		},
		{
			title: t('dashboard.stats.totalCertificates', $language),
			value: dashboardStats?.totalCertificates || 0,
			icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
			color: 'text-success',
			bgColor: 'bg-success/10'
		},
		{
			title: t('dashboard.stats.expiredCertificates', $language),
			value: dashboardStats?.expiredCertificates || 0,
			icon: 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
			color: 'text-error',
			bgColor: 'bg-error/10'
		},
		{
			title: t('dashboard.stats.expiringIn7Days', $language),
			value: dashboardStats?.expiringIn7Days || 0,
			icon: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.865-.833-2.634 0L4.268 18.5c-.77.833.192 2.5 1.732 2.5z',
			color: 'text-warning',
			bgColor: 'bg-warning/10'
		},
		{
			title: t('dashboard.stats.expiringIn30Days', $language),
			value: dashboardStats?.expiringIn30Days || 0,
			icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
			color: 'text-info',
			bgColor: 'bg-info/10'
		},
		{
			title: t('dashboard.stats.validCertificates', $language),
			value: dashboardStats?.validCertificates || 0,
			icon: 'M5 13l4 4L19 7',
			color: 'text-success',
			bgColor: 'bg-success/10'
		}
	]);
</script>

<svelte:head>
	<title>{t('dashboard.title', $language)} - CertVal</title>
</svelte:head>

<div class="space-y-8">
	<!-- Header Section -->
	<div class="hero min-h-[200px] relative overflow-hidden {mounted ? 'animate-in slide-in-from-top-4 duration-500' : ''}">
		<div class="hero-content text-center">
			<div class="max-w-lg">
				<h1 class="text-5xl font-bold bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
					{t('dashboard.welcome', $language)}{userFirstName ? `, ${userFirstName}` : ''}!
				</h1>
				<p class="py-6 text-base-content/70 text-lg">
					{t('dashboard.title', $language)} • {new Date().toLocaleDateString(
						$language === 'uk' ? 'uk-UA' : 'en-US',
						{
							weekday: 'long',
							year: 'numeric',
							month: 'long',
							day: 'numeric'
						}
					)}
				</p>
				
				<!-- Quick Actions -->
				<div class="flex flex-col sm:flex-row gap-4 justify-center">
					<Button size="lg" onclick={() => goto('/certificates')} class="btn-wide">
						<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
						</svg>
						Upload Certificate
					</Button>
					<Button variant="outline" size="lg" onclick={() => goto('/workspaces')} class="btn-wide">
						<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
							<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6" />
						</svg>
						New Workspace
					</Button>
				</div>
			</div>
		</div>
	</div>

	{#if isLoading}
		<div class="flex h-64 items-center justify-center">
			<span class="loading loading-spinner loading-lg text-primary"></span>
		</div>
	{:else}
		<!-- Stats Cards Grid -->
		{#if dashboardStats}
			<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
				{#each statCards as card, index}
					<div 
						class="stats shadow-lg border border-base-300 transition-all duration-300 hover:shadow-xl hover:scale-[1.02] {mounted ? 'animate-in slide-in-from-bottom-8 duration-500' : ''}"
						style="animation-delay: {mounted ? index * 100 : 0}ms"
					>
						<div class="stat">
							<div class="stat-figure">
								<div class="avatar">
									<div class="mask mask-circle w-16 h-16 {card.bgColor}">
										<svg class="h-8 w-8 {card.color}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
											<path stroke-linecap="round" stroke-linejoin="round" d={card.icon} />
										</svg>
									</div>
								</div>
							</div>
							<div class="stat-title text-base-content/60">{card.title}</div>
							<div class="stat-value {card.color}">{card.value.toLocaleString()}</div>
							
							<!-- Progress indicator for some cards -->
							{#if card.title.includes('Expired') && dashboardStats}
								{@const percentage = dashboardStats.totalCertificates > 0 ? (card.value / dashboardStats.totalCertificates) * 100 : 0}
								<div class="stat-desc">
									<div class="flex items-center gap-2">
										<progress class="progress progress-error w-20" value={percentage} max="100"></progress>
										<span class="text-xs">{percentage.toFixed(1)}%</span>
									</div>
								</div>
							{/if}
						</div>
					</div>
				{/each}
			</div>
		{/if}

		<!-- Expiring Certificates Section -->
		<Card title={t('dashboard.expiringCertificates', $language)} glass={true} class={mounted ? 'animate-in slide-in-from-bottom-8 duration-700' : ''}>
			{#if expiringCertificates.length > 0}
				<div class="space-y-4">
					{#each expiringCertificates.slice(0, 5) as certificate, index}
						{@const status = getCertificateStatus(certificate.notAfter)}
						<div 
							class="card card-compact bg-base-200/50 shadow-sm hover:shadow-md transition-all duration-200 hover:scale-[1.01] {mounted ? 'animate-in slide-in-from-left-8 duration-500' : ''}"
							style="animation-delay: {mounted ? index * 100 + 500 : 0}ms"
						>
							<div class="card-body">
								<div class="flex items-center justify-between">
									<div class="flex items-center gap-4">
										<div class="avatar placeholder">
											<div class="bg-primary text-primary-content rounded-lg w-12 h-12">
												{#if certificate.isBundle}
													<svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
														<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6" />
													</svg>
												{:else}
													<svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
														<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
													</svg>
												{/if}
											</div>
										</div>
										<div class="flex-1">
											<h3 class="font-semibold text-base-content truncate">
												{certificate.subject}
											</h3>
											<p class="text-sm text-base-content/60 truncate">
												{certificate.originalFileName} • {formatDate(certificate.notAfter)}
											</p>
										</div>
									</div>
									<div class="flex items-center gap-3">
										<div class="{getStatusBadgeClass(status)} gap-2">
											{getStatusText(status)}
											{#if certificate.daysUntilExpiry > 0}
												<div class="badge badge-neutral badge-sm">
													{certificate.daysUntilExpiry} {t('certificates.days', $language)}
												</div>
											{/if}
										</div>
										<Button 
											variant="ghost" 
											size="sm" 
											onclick={() => goto(`/certificates/${certificate.id}`)}
										>
											<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
												<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
											</svg>
										</Button>
									</div>
								</div>
							</div>
						</div>
					{/each}
				</div>

				{#if expiringCertificates.length > 5}
					<div class="card-actions justify-center mt-6">
						<Button variant="outline" onclick={() => goto('/certificates')}>
							View all {expiringCertificates.length} certificates
							<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
							</svg>
						</Button>
					</div>
				{/if}
			{:else}
				<div class="hero min-h-[300px]">
					<div class="hero-content text-center">
						<div class="max-w-md">
							<div class="relative inline-flex mb-6">
								<svg class="h-16 w-16 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
								</svg>
								<div class="absolute inset-0 animate-pulse rounded-full bg-success/20 blur-md"></div>
							</div>
							<h3 class="text-lg font-bold text-base-content">All certificates are healthy!</h3>
							<p class="py-6 text-base-content/60">No certificates are expiring in the next 30 days.</p>
							<Button onclick={() => goto('/certificates')}>
								View All Certificates
							</Button>
						</div>
					</div>
				</div>
			{/if}
		</Card>
	{/if}
</div>