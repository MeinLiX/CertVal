<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { DashboardService } from '$lib/services/DashboardService';
	import { t } from '$lib/i18n';
	import { formatDate, formatDateWithWeekday } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import FloatingActionBar from '$lib/components/layout/FloatingActionBar.svelte';
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

	function getDaysUntilExpiry(dateStr: string): number {
		return Math.ceil((new Date(dateStr).getTime() - Date.now()) / (1000 * 60 * 60 * 24));
	}

	function shortenSubject(subject: string): string {
		const cnMatch = subject.match(/CN=([^,]+)/i);
		if (cnMatch) return cnMatch[1].trim().replace(/^"+|"+$/g, '');
		const oMatch = subject.match(/O=([^,]+)/i);
		if (oMatch) return oMatch[1].trim().replace(/^"+|"+$/g, '');
		return subject;
	}

	const userFirstName = $derived(userSession.user?.firstName || '');
	const expiringPercent = $derived(
		stats && stats.totalCertificates > 0
			? Math.round((stats.expiringIn30Days / stats.totalCertificates) * 100)
			: 0
	);
	const activePercent = $derived(
		stats && stats.totalCertificates > 0
			? Math.round((stats.validCertificates / stats.totalCertificates) * 100)
			: 0
	);
</script>

<svelte:head>
	<title>{t('dashboard.title', language.current)} - CertVal</title>
	<link rel="canonical" href="/dashboard" />
</svelte:head>

<div class="dashboard" data-test-id="dashboard-page">
	{#if isLoading}
		<div class="dashboard__loading">
			<GlobalLoader variant="inline" />
		</div>
	{:else if stats}
		<!-- Hero -->
		<header class="hero">
			<div class="hero__intro">
				<span class="hero__eyebrow">
					<Icon name="calendar" size="sm" />
					{formatDateWithWeekday(new Date())}
				</span>
				<h1 class="hero__title">
					{t('dashboard.welcome', language.current)}{userFirstName ? `, ${userFirstName}` : ''}.
				</h1>
				<p class="hero__subtitle">{t('dashboard.heroSubtitle', language.current)}</p>
			</div>
			<div class="hero__metrics">
				<div class="metric" data-test-id="dashboard-stat-card-workspaces">
					<span class="metric__label">{t('dashboard.stats.totalWorkspaces', language.current)}</span>
					<span class="metric__value metric__value--primary">{stats.totalWorkspaces}</span>
				</div>
				<div class="metric__divider" aria-hidden="true"></div>
				<div class="metric" data-test-id="dashboard-stat-card-certificates">
					<span class="metric__label">{t('dashboard.stats.totalCertificates', language.current)}</span>
					<span class="metric__value">{stats.totalCertificates}</span>
				</div>
				<div class="metric__divider" aria-hidden="true"></div>
				<div class="metric" data-test-id="dashboard-stat-card-active">
					<span class="metric__label">{t('dashboard.stats.activeCertificates', language.current)}</span>
					<span class="metric__value metric__value--success">{stats.validCertificates}</span>
					<span class="metric__aux">{activePercent}%</span>
				</div>
				<div class="metric__divider" aria-hidden="true"></div>
				<div class="metric" data-test-id="dashboard-stat-card-expiring">
					<span class="metric__label">{t('dashboard.stats.expiringSoon', language.current)}</span>
					<span class="metric__value metric__value--warning">{stats.expiringIn30Days}</span>
					<span class="metric__aux">{expiringPercent}%</span>
				</div>
			</div>
		</header>

		<!-- Main Grid -->
		<section class="grid">
			<!-- Expiring Certificates -->
			<article class="panel panel--wide">
				<div class="panel__head">
					<div class="panel__title-group">
						<Icon name="time" size="sm" />
						<h2 class="panel__title">{t('dashboard.expiringSoon', language.current)}</h2>
					</div>
					<Button
						variant="ghost"
						size="sm"
						onclick={() => goto('/certificates?status=Expiring&page=1')}
						data-test-id="dashboard-view-all-expiring-button"
					>
						{t('common.viewAll', language.current)}
						<Icon name="rightArrow" size="sm" />
					</Button>
				</div>

				{#if expiringCerts.length > 0}
					<ul class="cert-list">
						{#each expiringCerts as cert}
							{@const daysLeft = getDaysUntilExpiry(cert.notAfter)}
							{@const urgent = daysLeft <= 7}
							<li>
								<button
									class="cert-item"
									onclick={() => goto(`/certificates/${cert.id}`)}
									data-test-id={`dashboard-expiring-cert-item-${cert.id}`}
								>
									<span class="cert-item__mark" class:cert-item__mark--urgent={urgent} aria-hidden="true"></span>
									<div class="cert-item__content">
										<span class="cert-item__name" title={cert.subject}>{shortenSubject(cert.subject)}</span>
										<span class="cert-item__date">
											{t('certificates.expires', language.current)} {formatDate(cert.notAfter)}
										</span>
									</div>
									<span class="cert-item__days" class:cert-item__days--urgent={urgent}>
										<span class="cert-item__days-value">{daysLeft}</span>
										<span class="cert-item__days-unit">{t('common.days', language.current)}</span>
									</span>
									<Icon name="rightArrow" size="sm" />
								</button>
							</li>
						{/each}
					</ul>
				{:else}
					<div class="empty-state">
						<div class="empty-state__icon">
							<Icon name="checkCircle" />
						</div>
						<h3 class="empty-state__title">{t('dashboard.noExpiring', language.current)}</h3>
						<p class="empty-state__text">{t('dashboard.allGood', language.current)}</p>
					</div>
				{/if}
			</article>

			<!-- Portfolio Health -->
			<article class="panel">
				<div class="panel__head">
					<div class="panel__title-group">
						<Icon name="security" size="sm" />
						<h2 class="panel__title">{t('dashboard.portfolioHealth', language.current)}</h2>
					</div>
				</div>
				<div class="health">
					<div class="health__bar" role="img" aria-label="Certificate distribution">
						{#if stats.totalCertificates > 0}
							<span class="health__seg health__seg--active" style="width: {activePercent}%"></span>
							<span class="health__seg health__seg--warning" style="width: {expiringPercent}%"></span>
						{/if}
					</div>
					<dl class="health__legend">
						<div class="health__row">
							<dt><span class="dot dot--success"></span>{t('dashboard.stats.activeCertificates', language.current)}</dt>
							<dd>{stats.validCertificates}</dd>
						</div>
						<div class="health__row">
							<dt><span class="dot dot--warning"></span>{t('dashboard.stats.expiringSoon', language.current)}</dt>
							<dd>{stats.expiringIn30Days}</dd>
						</div>
						<div class="health__row">
							<dt><span class="dot dot--muted"></span>{t('dashboard.stats.totalCertificates', language.current)}</dt>
							<dd>{stats.totalCertificates}</dd>
						</div>
					</dl>
				</div>
			</article>
		</section>
	{/if}

	<FloatingActionBar label={t('dashboard.title', language.current)}>
		{#snippet trailing()}
			<Button variant="secondary" onclick={() => goto('/workspaces')} data-test-id="dashboard-workspaces-button">
				<Icon name="workspaces" size="sm" />
				{t('workspaces.title', language.current)}
			</Button>
			<Button variant="secondary" onclick={() => goto('/certificates?action=upload')} data-test-id="dashboard-quick-action-upload">
				<Icon name="upload" size="sm" />
				{t('certificates.upload', language.current)}
			</Button>
			<Button onclick={() => goto('/workspaces?action=create')} data-test-id="dashboard-quick-action-create-workspace">
				<Icon name="plus" size="sm" />
				{t('workspaces.create', language.current)}
			</Button>
		{/snippet}
	</FloatingActionBar>
</div>

<style>
	.dashboard {
		display: flex;
		flex-direction: column;
		gap: var(--space-10);
		animation: fadeIn 0.5s ease-out;
	}

	@keyframes fadeIn {
		from { opacity: 0; }
		to { opacity: 1; }
	}

	.dashboard__loading {
		display: flex;
		justify-content: center;
		padding: var(--space-12);
	}

	/* Hero */
	.hero {
		display: flex;
		flex-direction: column;
		gap: var(--space-8);
		padding-bottom: var(--space-6);
		border-bottom: 1px solid var(--color-border);
	}

	.hero__eyebrow {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		font-family: var(--font-mono);
		font-size: var(--text-xs);
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
		color: var(--color-text-muted);
	}

	.hero__title {
		font-family: var(--font-display);
		font-size: var(--text-5xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: 1.05;
		color: var(--color-text);
		margin: var(--space-3) 0 var(--space-2);
	}

	.hero__subtitle {
		color: var(--color-text-secondary);
		font-size: var(--text-base);
		max-width: 48ch;
		margin: 0;
	}

	.hero__metrics {
		display: grid;
		grid-template-columns: 1fr auto 1fr auto 1fr auto 1fr;
		gap: var(--space-6);
		align-items: flex-end;
	}

	@media (max-width: 820px) {
		.hero__metrics {
			grid-template-columns: repeat(2, 1fr);
			gap: var(--space-5);
		}
		.metric__divider { display: none; }
	}

	@media (max-width: 480px) {
		.hero__metrics {
			gap: var(--space-4);
		}
		.metric__value {
			font-size: var(--text-3xl);
		}
	}

	.metric {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
		min-width: 0;
		position: relative;
	}

	.metric__label {
		font-family: var(--font-mono);
		font-size: var(--text-xs);
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
		color: var(--color-text-muted);
	}

	.metric__value {
		font-family: var(--font-display);
		font-size: var(--text-4xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: 1;
		color: var(--color-text);
	}

	.metric__value--primary { color: var(--color-primary); }
	.metric__value--success { color: var(--color-success); }
	.metric__value--warning { color: var(--color-warning); }

	.metric__aux {
		position: absolute;
		right: 0;
		bottom: 4px;
		font-family: var(--font-mono);
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.metric__divider {
		width: 1px;
		height: 36px;
		background-color: var(--color-border);
		align-self: flex-end;
	}

	/* Grid */
	.grid {
		display: grid;
		grid-template-columns: 1fr;
		gap: var(--space-6);
	}

	@media (min-width: 980px) {
		.grid { grid-template-columns: minmax(0, 2fr) minmax(0, 1fr); }
	}

	.panel {
		background-color: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		padding: var(--space-5);
		min-width: 0;
	}

	.panel__head {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: var(--space-4);
	}

	.panel__title-group {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		color: var(--color-text-muted);
		min-width: 0;
		flex-wrap: nowrap;
	}

	.panel__title {
		font-family: var(--font-display);
		font-size: var(--text-lg);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		color: var(--color-text);
		margin: 0;
		white-space: nowrap;
	}

	/* Cert list */
	.cert-list {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
		list-style: none;
		margin: 0;
		padding: 0;
	}

	.cert-item {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		width: 100%;
		padding: var(--space-3);
		background: none;
		border: 1px solid transparent;
		border-radius: var(--radius-md);
		cursor: pointer;
		text-align: left;
		transition: background-color var(--transition-fast), border-color var(--transition-fast);
		min-width: 0;
	}

	.cert-item:hover {
		background-color: var(--color-surface-hover);
		border-color: var(--color-border);
	}

	.cert-item__mark {
		width: 3px;
		align-self: stretch;
		border-radius: var(--radius-full);
		background-color: var(--color-warning);
		flex-shrink: 0;
	}

	.cert-item__mark--urgent {
		background-color: var(--color-error);
	}

	.cert-item__content {
		flex: 1;
		min-width: 0;
		display: flex;
		flex-direction: column;
		gap: 2px;
	}

	.cert-item__name {
		font-weight: var(--font-semibold);
		color: var(--color-text);
		font-size: var(--text-sm);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.cert-item__date {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
		font-family: var(--font-mono);
	}

	.cert-item__days {
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		padding: var(--space-1) var(--space-3);
		background-color: var(--color-warning-light);
		color: var(--color-warning);
		border-radius: var(--radius-sm);
		flex-shrink: 0;
	}

	.cert-item__days--urgent {
		background-color: var(--color-error-light);
		color: var(--color-error);
	}

	.cert-item__days-value {
		font-family: var(--font-display);
		font-weight: var(--font-semibold);
		font-size: var(--text-base);
		line-height: 1;
	}

	.cert-item__days-unit {
		font-family: var(--font-mono);
		font-size: 10px;
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
	}

	/* Health panel */
	.health {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.health__bar {
		display: flex;
		height: 10px;
		background-color: var(--color-surface-inset);
		border-radius: var(--radius-full);
		overflow: hidden;
	}

	.health__seg {
		display: block;
		height: 100%;
		transition: width var(--transition-medium);
	}

	.health__seg--active { background-color: var(--color-success); }
	.health__seg--warning { background-color: var(--color-warning); }

	.health__legend {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
		margin: 0;
	}

	.health__row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		font-size: var(--text-sm);
	}

	.health__row dt {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		color: var(--color-text-secondary);
	}

	.health__row dd {
		margin: 0;
		font-family: var(--font-mono);
		color: var(--color-text);
		font-weight: var(--font-semibold);
	}

	.dot {
		display: inline-block;
		width: 8px;
		height: 8px;
		border-radius: var(--radius-full);
	}

	.dot--success { background-color: var(--color-success); }
	.dot--warning { background-color: var(--color-warning); }
	.dot--muted { background-color: var(--color-text-muted); }

	/* Empty state */
	.empty-state {
		text-align: center;
		padding: var(--space-8) var(--space-4);
	}

	.empty-state__icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 48px;
		height: 48px;
		border-radius: var(--radius-full);
		background-color: var(--color-success-light);
		color: var(--color-success);
		margin-bottom: var(--space-3);
	}

	.empty-state__title {
		font-family: var(--font-display);
		font-size: var(--text-lg);
		font-weight: var(--font-semibold);
		color: var(--color-text);
		margin: 0 0 var(--space-1);
	}

	.empty-state__text {
		color: var(--color-text-secondary);
		margin: 0;
		font-size: var(--text-sm);
	}
</style>
