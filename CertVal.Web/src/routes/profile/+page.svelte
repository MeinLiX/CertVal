<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { theme } from '$lib/stores/theme';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, formatDateTime } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import type { User, Language, Theme, UpdateUserRequest, ChangePasswordRequest } from '$lib/types';

	let user = $state<User | null>(null);
	let isLoading = $state(true);
	let isSavingProfile = $state(false);
	let isChangingPassword = $state(false);
	let successMessage = $state('');
	let errors = $state<Record<string, string>>({});

	let profileForm = $state<UpdateUserRequest>({
		firstName: '',
		lastName: '',
		timeZone: 'UTC',
		language: 'uk',
		emailNotificationsEnabled: true
	});
	let passwordForm = $state<ChangePasswordRequest>({
		currentPassword: '',
		newPassword: '',
		confirmNewPassword: ''
	});

	const languages = [
		{ value: 'uk', label: '🇺🇦 Українська' },
		{ value: 'en', label: '🇺🇸 English' }
	];
	const timezones = [
		{ value: 'UTC', label: 'UTC' },
		{ value: 'Europe/Kiev', label: 'Europe/Kyiv' },
		{ value: 'Europe/London', label: 'Europe/London' },
		{ value: 'America/New_York', label: 'America/New York' }
	];
	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadUserProfile();
	});

	async function loadUserProfile() {
		isLoading = true;
		try {
			const response = await api.get<User>('/v1/users/me');
			if (response.data) {
				user = response.data;
				profileForm = {
					firstName: user.firstName,
					lastName: user.lastName,
					timeZone: user.timeZone || 'UTC',
					language: user.language || 'uk',
					emailNotificationsEnabled: user.emailNotificationsEnabled
				};
			}
		} catch (error) {
			console.error('Failed to load profile:', error);
		} finally {
			isLoading = false;
		}
	}

	async function handleUpdateProfile(event: Event) {
		event.preventDefault();
		isSavingProfile = true;
		errors = {};
		successMessage = '';
		try {
			// Оновлюємо мову інтерфейсу одразу
			if (profileForm.language) {
				language.setLanguage(profileForm.language as Language);
			}

			const response = await api.put<User>('/v1/users/me', profileForm);
			if (response.data) {
				auth.login($auth.token!, response.data);
				user = response.data;
				successMessage = t('success.profileUpdated', $language);
			} else {
				errors.profile = response.message || t('errors.general', $language);
			}
		} catch (err) {
			errors.profile = t('errors.network', $language);
		} finally {
			isSavingProfile = false;
		}
	}

	async function handleChangePassword(event: Event) {
		event.preventDefault();
		errors = {};
		successMessage = '';
		if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
			errors.password = t('errors.passwordsNotMatch', $language);
			return;
		}

		isChangingPassword = true;
		try {
			const response = await api.post('/v1/users/change-password', passwordForm);
			if (response.data) {
				successMessage = t('success.passwordChanged', $language);
				passwordForm = { currentPassword: '', newPassword: '', confirmNewPassword: '' };
			} else {
				errors.password = response.message || t('errors.general', $language);
			}
		} catch (err) {
			errors.password = t('errors.network', $language);
		} finally {
			isChangingPassword = false;
		}
	}
</script>

<svelte:head>
	<title>{t('profile.title', $language)}</title>
</svelte:head>

<div class="space-y-6">
	<div>
		<h1 class="text-3xl font-bold">{t('profile.title', $language)}</h1>
		<p class="mt-1 text-base-content/70">{t('profile.subtitle', $language)}</p>
	</div>

	{#if isLoading}
		<div class="flex h-96 items-center justify-center">
			<span class="loading loading-lg loading-spinner"></span>
		</div>
	{:else if user}
		{#if successMessage}
			<div role="alert" class="alert alert-success"><span>{successMessage}</span></div>
		{/if}

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<div class="lg:col-span-1">
				<Card>
					<div class="flex flex-col items-center text-center">
						<div class="placeholder avatar">
							<div class="w-24 rounded-full bg-primary text-primary-content">
								<span class="text-3xl">{user.firstName?.charAt(0)}{user.lastName?.charAt(0)}</span>
							</div>
						</div>
						<h2 class="mt-4 text-xl font-bold">{user.fullName}</h2>
						<p class="text-sm text-base-content/60">{user.email}</p>
						<div class="mt-4 text-xs text-base-content/50">
							{t('profile.joinedOn', $language)}
							{formatDate(user.createdAt)}
						</div>
					</div>
				</Card>
			</div>

			<div class="space-y-6 lg:col-span-2">
				<Card title={t('profile.personalInfo', $language)}>
					<form onsubmit={handleUpdateProfile} class="space-y-4">
						{#if errors.profile}
							<div role="alert" class="alert alert-error text-sm">
								<span>{errors.profile}</span>
							</div>
						{/if}
						<div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
							<Input
								label={t('auth.register.firstName', $language)}
								bind:value={profileForm.firstName}
								required
							/>
							<Input
								label={t('auth.register.lastName', $language)}
								bind:value={profileForm.lastName}
								required
							/>
						</div>

						<div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
							<Select
								label={t('profile.language', $language)}
								bind:value={profileForm.language}
								options={languages}
							/>
							<Select
								label={t('profile.timezone', $language)}
								bind:value={profileForm.timeZone}
								options={timezones}
							/>
						</div>

						<div class="form-control">
							<label class="label cursor-pointer">
								<span class="label-text">{t('profile.emailNotifications', $language)}</span>
								<input
									type="checkbox"
									class="toggle toggle-primary"
									bind:checked={profileForm.emailNotificationsEnabled}
								/>
							</label>
						</div>
						<div class="card-actions justify-end">
							<Button type="submit" variant="primary" loading={isSavingProfile}
								>{t('profile.saveChanges', $language)}</Button
							>
						</div>
					</form>
				</Card>

				<Card title={t('profile.security', $language)}>
					<form onsubmit={handleChangePassword} class="space-y-4">
						{#if errors.password}
							<div role="alert" class="alert alert-error text-sm">
								<span>{errors.password}</span>
							</div>
						{/if}
						<Input
							label={t('profile.currentPassword', $language)}
							type="password"
							bind:value={passwordForm.currentPassword}
							required
						/>
						<Input
							label={t('profile.newPassword', $language)}
							type="password"
							bind:value={passwordForm.newPassword}
							required
						/>
						<Input
							label={t('profile.confirmNewPassword', $language)}
							type="password"
							bind:value={passwordForm.confirmNewPassword}
							required
						/>
						<div class="card-actions justify-end">
							<Button type="submit" variant="primary" loading={isChangingPassword}
								>{t('profile.changePassword', $language)}</Button
							>
						</div>
					</form>
				</Card>
			</div>
		</div>
	{/if}
</div>
