<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { DashboardService } from '$lib/services/DashboardService';
	import { t } from '$lib/i18n';
	import { formatDate, formatDateWithWeekday } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import type { DashboardStats, Certificate } from '$lib/types';

	let stats = $state<DashboardStats | null>(null);
	let expiringCerts = $state<Certificate[]>([]);
	let isLoading = $state(true);

	onMount(async () => {
		if (!userSession.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadDashboardData();
	});

	async function loadDashboardData() {
		isLoading = true;
		try {
			const [statsRes, certsRes] = await Promise.all([
				DashboardService.getStats(),
				DashboardService.getExpiringCertificates(30, 5)
			]);

			if (statsRes.data) stats = statsRes.data;
			if (certsRes.data) expiringCerts = certsRes.data;
		} catch (error) {
			console.error('Failed to load dashboard data:', error);
		} finally {
			isLoading = false;
		}
	}

	const userFirstName = $derived(userSession.user?.firstName || '');
</script>

<svelte:head>
	<title>{t('dashboard.title', language.current)} - CertVal</title>
	<link rel="canonical" href="/dashboard" />
</svelte:head>

<div
	class="animate-in fade-in slide-in-from-bottom-4 min-h-[80vh] space-y-8 duration-500"
	data-test-id="dashboard-page"
>
	<div class="flex flex-col items-start justify-between gap-6 md:flex-row md:items-center">
		<div>
			<h1
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-4xl font-bold tracking-tight text-transparent"
			>
				{t('dashboard.welcome', language.current)}{userFirstName ? `, ${userFirstName}` : ''}!
			</h1>
			<p class="text-base-content/60 mt-2 flex items-center gap-2 text-lg font-light">
				<Icon name="calendar" class="h-5 w-5" />
				{formatDateWithWeekday(new Date())}
			</p>
		</div>
		<div class="flex gap-3">
			<Button
				variant="outline"
				onclick={() => goto('/workspaces')}
				data-test-id="dashboard-workspaces-button"
			>
				<Icon name="workspaces" class="mr-2 h-5 w-5" />
				{t('workspaces.title', language.current)}
			</Button>
		</div>
	</div>

	<div class="relative min-h-[200px]">
		{#if isLoading}
			<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-4">
				{#each Array(4) as _}
					<div class="card bg-base-100 h-26 animate-pulse shadow-xl">
						<div class="card-body flex flex-row items-center gap-4">
							<div class="bg-base-300 h-12 w-12 rounded-xl"></div>
							<div class="flex-1 space-y-2">
								<div class="bg-base-300 h-4 w-24 rounded"></div>
								<div class="bg-base-300 h-8 w-16 rounded"></div>
							</div>
						</div>
					</div>
				{/each}
			</div>
			<div class="mt-8 grid grid-cols-1 gap-8 lg:grid-cols-3">
				<div class="space-y-6 order-2 lg:col-span-2 lg:order-1">
					<div class="bg-base-300 h-8 w-48 rounded"></div>
					<div class="grid gap-4">
						{#each Array(3) as _}
							<div class="card bg-base-100 h-24 animate-pulse shadow-xl">
								<div class="card-body p-4">
									<div class="flex items-center gap-4">
										<div class="bg-base-300 h-10 w-10 rounded-xl"></div>
										<div class="flex-1 space-y-2">
											<div class="bg-base-300 h-4 w-3/4 rounded"></div>
											<div class="bg-base-300 h-3 w-1/2 rounded"></div>
										</div>
									</div>
								</div>
							</div>
						{/each}
					</div>
				</div>
				<div class="space-y-6 order-1 lg:order-2">
					<div class="bg-base-300 h-8 w-32 rounded"></div>
					<div class="card bg-base-100 h-48 animate-pulse shadow-xl"></div>
				</div>
			</div>
		{:else if stats}
			<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-4">
			<Card
				variant="glass"
				class="from-primary/5 to-primary/10 border-primary/20 bg-gradient-to-br transition-transform hover:scale-[1.02]"
				data-test-id="dashboard-stat-card-workspaces"
			>
				<div class="flex items-center gap-4">
					<div class="bg-primary/20 text-primary shadow-primary/10 rounded-xl p-3 shadow-inner">
						<Icon name="workspaces" class="h-8 w-8" />
					</div>
					<div>
						<h3 class="text-base-content/70 text-sm font-medium">
							{t('dashboard.stats.totalWorkspaces', language.current)}
						</h3>
						<p class="text-primary text-3xl font-bold">{stats.totalWorkspaces}</p>
					</div>
				</div>
			</Card>

			<Card
				variant="glass"
				class="transition-transform hover:scale-[1.02]"
				data-test-id="dashboard-stat-card-certificates"
			>
				<div class="flex items-center gap-4">
					<div class="bg-base-content/5 text-base-content rounded-xl p-3 shadow-inner">
						<Icon name="certificates" class="h-8 w-8" />
					</div>
					<div>
						<h3 class="text-base-content/70 text-sm font-medium">
							{t('dashboard.stats.totalCertificates', language.current)}
						</h3>
						<p class="text-3xl font-bold">{stats.totalCertificates}</p>
					</div>
				</div>
			</Card>

			<Card
				variant="glass"
				class="transition-transform hover:scale-[1.02]"
				data-test-id="dashboard-stat-card-active"
			>
				<div class="flex items-center gap-4">
					<div class="bg-success/10 text-success shadow-success/10 rounded-xl p-3 shadow-inner">
						<Icon name="checkCircle" class="h-8 w-8" />
					</div>
					<div>
						<h3 class="text-base-content/70 text-sm font-medium">
							{t('dashboard.stats.activeCertificates', language.current)}
						</h3>
						<p class="text-success text-3xl font-bold">{stats.validCertificates}</p>
					</div>
				</div>
			</Card>

			<Card
				variant="glass"
				class="transition-transform hover:scale-[1.02]"
				data-test-id="dashboard-stat-card-expiring"
			>
				<div class="flex items-center gap-4">
					<div class="bg-warning/10 text-warning shadow-warning/10 rounded-xl p-3 shadow-inner">
						<Icon name="alert" class="h-8 w-8" />
					</div>
					<div>
						<h3 class="text-base-content/70 text-sm font-medium">
							{t('dashboard.stats.expiringSoon', language.current)}
						</h3>
						<p class="text-warning text-3xl font-bold">{stats.expiringIn30Days}</p>
					</div>
				</div>
			</Card>
		</div>

		<div class="mt-8 grid grid-cols-1 gap-8 lg:grid-cols-3">
			<div class="space-y-6 order-2 lg:col-span-2 lg:order-1">
				<div class="flex items-center justify-between">
					<h2 class="flex items-center gap-2 text-2xl font-bold">
						<Icon name="time" class="text-warning h-6 w-6" />
						{t('dashboard.expiringSoon', language.current)}
					</h2>
					<Button
						variant="ghost"
						size="sm"
						onclick={() => goto('/certificates?status=Expiring&page=1')}
						data-test-id="dashboard-view-all-expiring-button"
					>
						{t('common.viewAll', language.current)}
						<Icon name="rightArrow" class="ml-2 h-4 w-4" />
					</Button>
				</div>

				{#if expiringCerts.length > 0}
					<div class="grid gap-4">
						{#each expiringCerts as cert}
							<div
								class="card bg-base-100/50 border-base-content/5 hover:border-warning/30 hover:shadow-warning/5 group border transition-all duration-300 hover:shadow-lg"
								data-test-id={`dashboard-expiring-cert-item-${cert.id}`}
							>
								<div class="card-body flex-row items-center gap-4 p-4">
									<div
										class="bg-warning/10 text-warning rounded-xl p-3 transition-transform group-hover:scale-110"
									>
										<Icon name="certificates" class="h-6 w-6" />
									</div>
									<div class="min-w-0 flex-1">
										<h4
											class="group-hover:text-warning max-w-[200px] truncate font-bold transition-colors sm:max-w-[300px] md:max-w-[400px]"
											title={cert.subject}
										>
											{cert.subject}
										</h4>
										<p class="text-base-content/60 truncate text-sm">
											{t('certificates.expires', language.current)}: {formatDate(cert.notAfter)}
										</p>
									</div>
									<div class="badge badge-warning gap-1">
										{Math.ceil(
											(new Date(cert.notAfter).getTime() - new Date().getTime()) /
												(1000 * 60 * 60 * 24)
										)}
										{t('common.days', language.current)}
									</div>
									<Button
										variant="ghost"
										size="sm"
										shape="circle"
										onclick={() => goto(`/certificates/${cert.id}`)}
										data-test-id={`dashboard-view-cert-button-${cert.id}`}
									>
										<Icon name="rightArrow" class="h-4 w-4" />
									</Button>
								</div>
							</div>
						{/each}
					</div>
				{:else}
					<div class="card bg-base-100/30 border-base-content/5 border p-8 text-center">
						<div class="mb-4 flex justify-center">
							<div class="bg-success/10 text-success rounded-full p-4">
								<Icon name="checkCircle" class="h-8 w-8" />
							</div>
						</div>
						<h3 class="text-lg font-bold">{t('dashboard.noExpiring', language.current)}</h3>
						<p class="text-base-content/60">{t('dashboard.allGood', language.current)}</p>
					</div>
				{/if}
			</div>

			<div class="space-y-6 order-1 lg:order-2">
				<h2 class="text-2xl font-bold">{t('common.quickActions', language.current)}</h2>
				<Card variant="glass" class="space-y-2">
					<button
						class="hover:bg-base-content/5 group flex w-full items-center gap-3 rounded-lg p-3 text-left transition-colors"
						onclick={() => goto('/certificates?action=upload')}
						data-test-id="dashboard-quick-action-upload"
					>
						<div
							class="bg-primary/10 text-primary group-hover:bg-primary group-hover:text-primary-content rounded-lg p-2 transition-colors"
						>
							<Icon name="upload" class="h-5 w-5" />
						</div>
						<span class="font-medium">{t('certificates.upload', language.current)}</span>
					</button>

					<button
						class="hover:bg-base-content/5 group flex w-full items-center gap-3 rounded-lg p-3 text-left transition-colors"
						onclick={() => goto('/workspaces?action=create')}
						data-test-id="dashboard-quick-action-create-workspace"
					>
						<div
							class="bg-secondary/10 text-secondary group-hover:bg-secondary group-hover:text-secondary-content rounded-lg p-2 transition-colors"
						>
							<Icon name="plus" class="h-5 w-5" />
						</div>
						<span class="font-medium">{t('workspaces.create', language.current)}</span>
					</button>

					<button
						class="hover:bg-base-content/5 group flex w-full items-center gap-3 rounded-lg p-3 text-left transition-colors"
						onclick={() => goto('/profile')}
						data-test-id="dashboard-quick-action-profile"
					>
						<div
							class="bg-accent/10 text-accent group-hover:bg-accent group-hover:text-accent-content rounded-lg p-2 transition-colors"
						>
							<Icon name="user" class="h-5 w-5" />
						</div>
						<span class="font-medium">{t('profile.title', language.current)}</span>
					</button>
				</Card>
			</div>
		</div>
	{/if}
	</div>
</div>
