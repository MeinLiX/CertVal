<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import type { DashboardStats, Certificate } from '$lib/types';

	let dashboardStats = $state<DashboardStats | null>(null);
	let expiringCertificates = $state<Certificate[]>([]);
	let isLoading = $state(true);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		await loadDashboardData();
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

	function getStatusColor(status: 'expired' | 'expiring' | 'valid'): string {
		switch (status) {
			case 'expired':
				return 'text-red-600 bg-red-100';
			case 'expiring':
				return 'text-yellow-600 bg-yellow-100';
			case 'valid':
				return 'text-green-600 bg-green-100';
		}
	}

	function getStatusText(status: 'expired' | 'expiring' | 'valid'): string {
		return t(`certificates.${status}`, $language);
	}

	const userFirstName = $derived($auth.user?.firstName || '');
</script>

<svelte:head>
	<title>{t('dashboard.title', $language)} - CertVal</title>
</svelte:head>

<div class="space-y-6">
	<!-- Header -->
	<div>
		<h1 class="text-2xl font-bold text-gray-900">
			{t('dashboard.welcome', $language)}, {userFirstName}!
		</h1>
		<p class="text-gray-600">{t('dashboard.title', $language)}</p>
	</div>

	{#if isLoading}
		<div class="flex h-64 items-center justify-center">
			<div class="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600"></div>
		</div>
	{:else}
		<!-- Stats Cards -->
		{#if dashboardStats}
			<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
				<Card>
					<div class="flex items-center">
						<div class="flex-shrink-0">
							<div class="flex h-8 w-8 items-center justify-center rounded-full bg-blue-100">
								<svg
									class="h-5 w-5 text-blue-600"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6"
									/>
								</svg>
							</div>
						</div>
						<div class="ml-4">
							<p class="text-sm font-medium text-gray-600">
								{t('dashboard.stats.totalWorkspaces', $language)}
							</p>
							<p class="text-2xl font-semibold text-gray-900">{dashboardStats.totalWorkspaces}</p>
						</div>
					</div>
				</Card>

				<Card>
					<div class="flex items-center">
						<div class="flex-shrink-0">
							<div class="flex h-8 w-8 items-center justify-center rounded-full bg-green-100">
								<svg
									class="h-5 w-5 text-green-600"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
									/>
								</svg>
							</div>
						</div>
						<div class="ml-4">
							<p class="text-sm font-medium text-gray-600">
								{t('dashboard.stats.totalCertificates', $language)}
							</p>
							<p class="text-2xl font-semibold text-gray-900">{dashboardStats.totalCertificates}</p>
						</div>
					</div>
				</Card>

				<Card>
					<div class="flex items-center">
						<div class="flex-shrink-0">
							<div class="flex h-8 w-8 items-center justify-center rounded-full bg-red-100">
								<svg
									class="h-5 w-5 text-red-600"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
									/>
								</svg>
							</div>
						</div>
						<div class="ml-4">
							<p class="text-sm font-medium text-gray-600">
								{t('dashboard.stats.expiredCertificates', $language)}
							</p>
							<p class="text-2xl font-semibold text-gray-900">
								{dashboardStats.expiredCertificates}
							</p>
						</div>
					</div>
				</Card>

				<Card>
					<div class="flex items-center">
						<div class="flex-shrink-0">
							<div class="flex h-8 w-8 items-center justify-center rounded-full bg-yellow-100">
								<svg
									class="h-5 w-5 text-yellow-600"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.865-.833-2.634 0L4.268 18.5c-.77.833.192 2.5 1.732 2.5z"
									/>
								</svg>
							</div>
						</div>
						<div class="ml-4">
							<p class="text-sm font-medium text-gray-600">
								{t('dashboard.stats.expiringIn7Days', $language)}
							</p>
							<p class="text-2xl font-semibold text-gray-900">{dashboardStats.expiringIn7Days}</p>
						</div>
					</div>
				</Card>

				<Card>
					<div class="flex items-center">
						<div class="flex-shrink-0">
							<div class="flex h-8 w-8 items-center justify-center rounded-full bg-orange-100">
								<svg
									class="h-5 w-5 text-orange-600"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
									/>
								</svg>
							</div>
						</div>
						<div class="ml-4">
							<p class="text-sm font-medium text-gray-600">
								{t('dashboard.stats.expiringIn30Days', $language)}
							</p>
							<p class="text-2xl font-semibold text-gray-900">{dashboardStats.expiringIn30Days}</p>
						</div>
					</div>
				</Card>

				<Card>
					<div class="flex items-center">
						<div class="flex-shrink-0">
							<div class="flex h-8 w-8 items-center justify-center rounded-full bg-green-100">
								<svg
									class="h-5 w-5 text-green-600"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
								>
									<path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M5 13l4 4L19 7"
									/>
								</svg>
							</div>
						</div>
						<div class="ml-4">
							<p class="text-sm font-medium text-gray-600">
								{t('dashboard.stats.validCertificates', $language)}
							</p>
							<p class="text-2xl font-semibold text-gray-900">{dashboardStats.validCertificates}</p>
						</div>
					</div>
				</Card>
			</div>
		{/if}

		<!-- Expiring Certificates -->
		{#if expiringCertificates.length > 0}
			<Card title={t('dashboard.expiringCertificates', $language)}>
				<div class="overflow-hidden">
					<table class="min-w-full divide-y divide-gray-200">
						<thead class="bg-gray-50">
							<tr>
								<th
									class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
								>
									{t('certificates.subject', $language)}
								</th>
								<th
									class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
								>
									{t('certificates.expires', $language)}
								</th>
								<th
									class="px-6 py-3 text-left text-xs font-medium tracking-wider text-gray-500 uppercase"
								>
									{t('certificates.status', $language)}
								</th>
							</tr>
						</thead>
						<tbody class="divide-y divide-gray-200 bg-white">
							{#each expiringCertificates.slice(0, 10) as certificate}
								{@const status = getCertificateStatus(certificate.notAfter)}
								<tr>
									<td class="px-6 py-4 text-sm font-medium whitespace-nowrap text-gray-900">
										{certificate.subject}
									</td>
									<td class="px-6 py-4 text-sm whitespace-nowrap text-gray-500">
										{formatDate(certificate.notAfter)}
									</td>
									<td class="px-6 py-4 whitespace-nowrap">
										<span
											class="inline-flex rounded-full px-2 py-1 text-xs font-semibold {getStatusColor(
												status
											)}"
										>
											{getStatusText(status)}
											{#if certificate.daysUntilExpiry > 0}
												({certificate.daysUntilExpiry} {t('certificates.days', $language)})
											{/if}
										</span>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>

				{#if expiringCertificates.length > 10}
					<div class="mt-4 text-center">
						<a href="/certificates" class="text-sm font-medium text-blue-600 hover:text-blue-800">
							View all certificates →
						</a>
					</div>
				{/if}
			</Card>
		{:else}
			<Card>
				<div class="py-8 text-center">
					<svg
						class="mx-auto h-12 w-12 text-gray-400"
						fill="none"
						viewBox="0 0 24 24"
						stroke="currentColor"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
						/>
					</svg>
					<h3 class="mt-2 text-sm font-medium text-gray-900">No expiring certificates</h3>
					<p class="mt-1 text-sm text-gray-500">
						All your certificates are valid for more than 30 days.
					</p>
				</div>
			</Card>
		{/if}
	{/if}
</div>