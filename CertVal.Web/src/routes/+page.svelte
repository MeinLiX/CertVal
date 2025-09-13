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

	function getStatusColor(status: 'expired' | 'expiring' | 'valid'): string {
		switch (status) {
			case 'expired':
				return 'text-red-600 bg-red-100 dark:text-red-400 dark:bg-red-900/30';
			case 'expiring':
				return 'text-yellow-600 bg-yellow-100 dark:text-yellow-400 dark:bg-yellow-900/30';
			case 'valid':
				return 'text-green-600 bg-green-100 dark:text-green-400 dark:bg-green-900/30';
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
			color: 'from-blue-500 to-cyan-500',
			bgColor: 'from-blue-50 to-cyan-50 dark:from-blue-950/30 dark:to-cyan-950/30'
		},
		{
			title: t('dashboard.stats.totalCertificates', $language),
			value: dashboardStats?.totalCertificates || 0,
			icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
			color: 'from-green-500 to-emerald-500',
			bgColor: 'from-green-50 to-emerald-50 dark:from-green-950/30 dark:to-emerald-950/30'
		},
		{
			title: t('dashboard.stats.expiredCertificates', $language),
			value: dashboardStats?.expiredCertificates || 0,
			icon: 'M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
			color: 'from-red-500 to-rose-500',
			bgColor: 'from-red-50 to-rose-50 dark:from-red-950/30 dark:to-rose-950/30'
		},
		{
			title: t('dashboard.stats.expiringIn7Days', $language),
			value: dashboardStats?.expiringIn7Days || 0,
			icon: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.865-.833-2.634 0L4.268 18.5c-.77.833.192 2.5 1.732 2.5z',
			color: 'from-yellow-500 to-orange-500',
			bgColor: 'from-yellow-50 to-orange-50 dark:from-yellow-950/30 dark:to-orange-950/30'
		},
		{
			title: t('dashboard.stats.expiringIn30Days', $language),
			value: dashboardStats?.expiringIn30Days || 0,
			icon: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
			color: 'from-orange-500 to-amber-500',
			bgColor: 'from-orange-50 to-amber-50 dark:from-orange-950/30 dark:to-amber-950/30'
		},
		{
			title: t('dashboard.stats.validCertificates', $language),
			value: dashboardStats?.validCertificates || 0,
			icon: 'M5 13l4 4L19 7',
			color: 'from-green-500 to-teal-500',
			bgColor: 'from-green-50 to-teal-50 dark:from-green-950/30 dark:to-teal-950/30'
		}
	]);
</script>

<svelte:head>
	<title>{t('dashboard.title', $language)} - CertVal</title>
</svelte:head>

<div
	class="min-h-screen bg-gradient-to-br from-gray-50 via-white to-gray-50 dark:from-gray-950 dark:via-gray-900 dark:to-gray-950"
>
	<div class="space-y-8 p-6">
		<!-- Header Section with Enhanced Styling -->
		<div class="relative overflow-hidden">
			<div class="relative z-10">
				<div
					class="flex flex-col space-y-4 sm:flex-row sm:items-center sm:justify-between sm:space-y-0"
				>
					<div class="space-y-2">
						<h1
							class="bg-gradient-to-r from-gray-900 via-blue-800 to-indigo-800 bg-clip-text text-4xl font-bold text-transparent dark:from-white dark:via-blue-200 dark:to-indigo-200"
						>
							{t('dashboard.welcome', $language)}{userFirstName ? `, ${userFirstName}` : ''}!
						</h1>
						<p class="text-lg text-gray-600 dark:text-gray-300">
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
					</div>

					<!-- Quick Actions -->
					<div class="flex space-x-3">
						<button
							class="inline-flex items-center rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all duration-200 hover:scale-105 hover:from-blue-700 hover:to-indigo-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:outline-none dark:focus:ring-offset-gray-900"
							onclick={() => goto('/certificates')}
						>
							<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M12 4v16m8-8H4"
								/>
							</svg>
							Upload Certificate
						</button>
						<button
							class="inline-flex items-center rounded-xl bg-white px-4 py-2.5 text-sm font-medium text-gray-700 shadow-sm ring-1 ring-gray-300 transition-all duration-200 hover:bg-gray-50 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:outline-none dark:bg-gray-800 dark:text-gray-200 dark:ring-gray-700 dark:hover:bg-gray-700 dark:focus:ring-offset-gray-900"
							onclick={() => goto('/workspaces')}
						>
							<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6"
								/>
							</svg>
							New Workspace
						</button>
					</div>
				</div>
			</div>

			<!-- Background Decoration -->
			<div
				class="absolute -top-4 -right-4 h-24 w-24 rounded-full bg-gradient-to-br from-blue-400/20 to-indigo-400/20 blur-2xl"
			></div>
			<div
				class="absolute -bottom-2 -left-2 h-16 w-16 rounded-full bg-gradient-to-br from-purple-400/20 to-pink-400/20 blur-xl"
			></div>
		</div>

		{#if isLoading}
			<div class="flex h-64 items-center justify-center">
				<div class="relative">
					<div
						class="h-16 w-16 animate-spin rounded-full border-4 border-blue-200 dark:border-blue-800"
					></div>
					<div
						class="absolute top-0 left-0 h-16 w-16 animate-spin rounded-full border-4 border-transparent border-t-blue-600"
					></div>
				</div>
			</div>
		{:else}
			<!-- Enhanced Stats Cards Grid -->
			{#if dashboardStats}
				<div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
					{#each statCards as card, index}
						<Card
							glass={true}
							hover={true}
							class="group relative overflow-hidden bg-gradient-to-br {card.bgColor} border-0 shadow-sm transition-all duration-300 hover:shadow-xl {mounted
								? 'animate-in slide-in-from-bottom-8 duration-500'
								: ''}"
						>
							<div class="flex items-center justify-between">
								<div class="min-w-0 flex-1">
									<p class="mb-2 truncate text-sm font-medium text-gray-600 dark:text-gray-400">
										{card.title}
									</p>
									<p
										class="bg-gradient-to-r text-3xl font-bold {card.color} bg-clip-text text-transparent"
									>
										{card.value.toLocaleString()}
									</p>
								</div>
								<div class="ml-4 flex-shrink-0">
									<div class="relative">
										<div
											class="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br {card.color} text-white shadow-sm transition-transform duration-300 group-hover:scale-110"
										>
											<svg
												class="h-6 w-6"
												fill="none"
												viewBox="0 0 24 24"
												stroke="currentColor"
												stroke-width="2"
											>
												<path stroke-linecap="round" stroke-linejoin="round" d={card.icon} />
											</svg>
										</div>
										<!-- Glow effect -->
										<div
											class="absolute inset-0 rounded-xl bg-gradient-to-br {card.color} scale-150 opacity-0 blur-xl transition-all duration-500 group-hover:opacity-20"
										></div>
									</div>
								</div>
							</div>

							<!-- Progress indicator for some cards -->
							{#if card.title.includes('Expired') && dashboardStats}
								{@const percentage =
									dashboardStats.totalCertificates > 0
										? (card.value / dashboardStats.totalCertificates) * 100
										: 0}
								<div class="mt-4 flex items-center">
									<div class="mr-3 h-2 flex-1 rounded-full bg-gray-200 dark:bg-gray-700">
										<div
											class="h-2 rounded-full bg-gradient-to-r {card.color} transition-all duration-1000 ease-out"
											style="width: {percentage}%"
										></div>
									</div>
									<span class="text-xs font-medium text-gray-500 dark:text-gray-400">
										{percentage.toFixed(1)}%
									</span>
								</div>
							{/if}
						</Card>
					{/each}
				</div>
			{/if}

			<!-- Expiring Certificates Section -->
			<div class="grid grid-cols-1 gap-8 lg:grid-cols-2">
				<div class="lg:col-span-2">
					<Card title={t('dashboard.expiringCertificates', $language)} glass={true}>
						{#if expiringCertificates.length > 0}
							<div class="space-y-4">
								{#each expiringCertificates.slice(0, 5) as certificate, index}
									{@const status = getCertificateStatus(certificate.notAfter)}
									<div
										class="group flex items-center justify-between rounded-xl border border-gray-200/50 p-4 transition-all duration-200 hover:scale-[1.02] hover:bg-gray-50/50 dark:border-gray-700/50 dark:hover:bg-gray-800/50 {mounted
											? 'animate-in slide-in-from-left-8 duration-500'
											: ''}"
									>
										<div class="flex min-w-0 flex-1 items-center space-x-4">
											<div class="flex-shrink-0">
												{#if certificate.isBundle}
													<div
														class="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 text-white"
													>
														<svg
															class="h-5 w-5"
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
												{:else}
													<div
														class="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-gray-500 to-gray-600 text-white"
													>
														<svg
															class="h-5 w-5"
															fill="none"
															viewBox="0 0 24 24"
															stroke="currentColor"
														>
															<path
																stroke-linecap="round"
																stroke-linejoin="round"
																stroke-width="2"
																d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
															/>
														</svg>
													</div>
												{/if}
											</div>
											<div class="min-w-0 flex-1">
												<p
													class="truncate text-sm font-semibold text-gray-900 transition-colors group-hover:text-blue-600 dark:text-white dark:group-hover:text-blue-400"
												>
													{certificate.subject}
												</p>
												<p class="truncate text-xs text-gray-500 dark:text-gray-400">
													{certificate.originalFileName} • {formatDate(certificate.notAfter)}
												</p>
											</div>
										</div>
										<div class="flex items-center space-x-3">
											<span
												class="inline-flex rounded-full px-3 py-1 text-xs font-semibold {getStatusColor(
													status
												)} transition-colors"
											>
												{getStatusText(status)}
												{#if certificate.daysUntilExpiry > 0}
													({certificate.daysUntilExpiry} {t('certificates.days', $language)})
												{/if}
											</span>
											<button
												aria-label="View Certificate"
												class="text-gray-400 transition-colors hover:text-gray-600 dark:hover:text-gray-300"
												onclick={() => goto(`/certificates/${certificate.id}`)}
											>
												<svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
													<path
														stroke-linecap="round"
														stroke-linejoin="round"
														stroke-width="2"
														d="M9 5l7 7-7 7"
													/>
												</svg>
											</button>
										</div>
									</div>
								{/each}
							</div>

							{#if expiringCertificates.length > 5}
								<div class="mt-6 text-center">
									<button
										class="inline-flex items-center text-sm font-medium text-blue-600 transition-colors hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300"
										onclick={() => goto('/certificates')}
									>
										View all {expiringCertificates.length} certificates
										<svg class="ml-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
											<path
												stroke-linecap="round"
												stroke-linejoin="round"
												stroke-width="2"
												d="M9 5l7 7-7 7"
											/>
										</svg>
									</button>
								</div>
							{/if}
						{:else}
							<div class="py-12 text-center">
								<div class="relative inline-flex">
									<svg
										class="mx-auto h-16 w-16 text-green-400 dark:text-green-500"
										fill="none"
										viewBox="0 0 24 24"
										stroke="currentColor"
									>
										<path
											stroke-linecap="round"
											stroke-linejoin="round"
											stroke-width="2"
											d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
										/>
									</svg>
									<div
										class="absolute inset-0 animate-pulse rounded-full bg-green-400 opacity-20 blur-md"
									></div>
								</div>
								<h3 class="mt-4 text-lg font-medium text-gray-900 dark:text-white">
									All certificates are healthy!
								</h3>
								<p class="mt-2 text-sm text-gray-500 dark:text-gray-400">
									No certificates are expiring in the next 30 days.
								</p>
							</div>
						{/if}
					</Card>
				</div>
			</div>
		{/if}
	</div>
</div>
