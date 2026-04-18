<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { UserService } from '$lib/services/UserService';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import type { User } from '$lib/types';

	let user = $state<User | null>(null);
	let isLoading = $state(true);

	onMount(async () => {
		if (!userSession.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadUserProfile();
	});

	async function loadUserProfile() {
		isLoading = true;
		try {
			const response = await UserService.getProfile();
			if (response.data) {
				user = response.data;
			}
		} catch (error) {
			console.error('Failed to load profile:', error);
		} finally {
			isLoading = false;
		}
	}
</script>

<svelte:head>
	<title>{t('profile.title', language.current)} - CertVal</title>
</svelte:head>

<div class="page" data-test-id="profile-page">
	<header class="page__header">
		<div class="page__header-content">
			<h1 class="page__title">{t('profile.title', language.current)}</h1>
			<p class="page__subtitle">{t('profile.subtitle', language.current)}</p>
		</div>
		<Button
			onclick={() => goto('/profile/settings/personal')}
			variant="outline"
			data-test-id="edit-profile-button"
		>
			<Icon name="settings" />
			{t('profile.editProfile', language.current)}
		</Button>
	</header>

	<div class="profile-content">
		{#if isLoading}
			<GlobalLoader variant="overlay" />
		{/if}
		
		{#if user}
			<div class="profile-card" data-test-id="profile-card">
				<div class="profile-card__header">
					<div class="profile-card__avatar-wrapper">
						<UserAvatar
							firstName={user.firstName}
							lastName={user.lastName}
							size="2xl"
						/>
					</div>
					<div class="profile-card__info">
						<h2 class="profile-card__name">{user.fullName}</h2>
						<div class="profile-card__email">
							<Icon name="mail" />
							<span>{user.email}</span>
						</div>
					</div>
				</div>

				<div class="profile-card__divider"></div>

				<div class="profile-card__stats">
					<div class="stat-card">
						<div class="stat-card__header">
							<Icon name="checkCircle" />
							<span class="stat-card__label">{t('profile.status', language.current)}</span>
						</div>
						<span class="badge badge--success">
							<span class="badge__indicator"></span>
							{user.status}
						</span>
					</div>

					<div class="stat-card">
						<div class="stat-card__header">
							<Icon name="time" />
							<span class="stat-card__label">{t('profile.lastLogin', language.current)}</span>
						</div>
						<span class="stat-card__value">{formatDateTime(user.lastLoginAt)}</span>
					</div>

					<div class="stat-card">
						<div class="stat-card__header">
							<Icon name="calendar" />
							<span class="stat-card__label">{t('profile.joinedOn', language.current)}</span>
						</div>
						<span class="stat-card__value">{formatDateTime(user.createdAt)}</span>
					</div>
				</div>
			</div>
		{/if}
	</div>
</div>

<style>
	.page {
		max-width: 1000px;
		margin: 0 auto;
		display: flex;
		flex-direction: column;
		gap: var(--space-8);
	}

	.page__header {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	@media (min-width: 768px) {
		.page__header {
			flex-direction: row;
			justify-content: space-between;
			align-items: flex-start;
		}
	}

	.page__header-content {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.page__title {
		font-family: var(--font-display);
		font-size: var(--text-4xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: var(--leading-tight);
		color: var(--color-text);
		margin: 0;
	}

	.page__subtitle {
		font-size: var(--text-md);
		color: var(--color-text-secondary);
		font-weight: var(--font-normal);
	}

	.profile-content {
		position: relative;
		min-height: 400px;
	}

	.profile-card {
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xl);
		overflow: hidden;
	}

	.profile-card__header {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: var(--space-6);
		padding: var(--space-8);
	}

	@media (min-width: 768px) {
		.profile-card__header {
			flex-direction: row;
		}
	}

	.profile-card__avatar-wrapper {
		flex-shrink: 0;
		padding: var(--space-4);
		background: var(--color-bg-secondary);
		border-radius: 50%;
		box-shadow: var(--shadow-lg);
	}

	.profile-card__info {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
		text-align: center;
	}

	@media (min-width: 768px) {
		.profile-card__info {
			text-align: left;
		}
	}

	.profile-card__name {
		font-family: var(--font-display);
		font-size: var(--text-3xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: var(--leading-tight);
		color: var(--color-text);
	}

	.profile-card__email {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: var(--space-2);
		color: var(--color-text-secondary);
		font-size: var(--text-lg);
	}

	@media (min-width: 768px) {
		.profile-card__email {
			justify-content: flex-start;
		}
	}

	.profile-card__divider {
		height: 1px;
		background: var(--color-border);
		margin: 0 var(--space-8);
	}

	.profile-card__stats {
		display: grid;
		grid-template-columns: 1fr;
		gap: var(--space-4);
		padding: var(--space-8);
	}

	@media (min-width: 640px) {
		.profile-card__stats {
			grid-template-columns: repeat(3, 1fr);
		}
	}

	.stat-card {
		background: var(--color-bg-secondary);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		padding: var(--space-5);
		transition: border-color var(--transition-fast);
	}

	.stat-card:hover {
		border-color: var(--color-border-strong);
	}

	.stat-card__header {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		margin-bottom: var(--space-3);
		color: var(--color-text-muted);
		font-size: var(--text-sm);
		font-weight: 500;
	}

	.stat-card__label {
		opacity: 0.8;
	}

	.stat-card__value {
		font-family: var(--font-mono);
		font-size: var(--text-base);
		font-weight: 600;
		color: var(--color-text-primary);
	}

	.badge {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-1) var(--space-3);
		border-radius: var(--radius-full);
		font-size: var(--text-sm);
		font-weight: 500;
	}

	.badge--success {
		background: var(--color-success-bg);
		color: var(--color-success);
	}

	.badge__indicator {
		width: 8px;
		height: 8px;
		border-radius: 50%;
		background: currentColor;
		animation: pulse 2s infinite;
	}

	@keyframes pulse {
		0%, 100% {
			opacity: 1;
		}
		50% {
			opacity: 0.5;
		}
	}
</style>
