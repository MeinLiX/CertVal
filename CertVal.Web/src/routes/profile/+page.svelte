<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { theme } from '$lib/stores/theme';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import type { User, Language, Theme } from '$lib/types';

	interface UpdateUserRequest {
		firstName: string;
		lastName: string;
		timeZone?: string;
		language?: string;
		emailNotificationsEnabled: boolean;
	}

	interface ChangePasswordRequest {
		currentPassword: string;
		newPassword: string;
		confirmNewPassword: string;
	}

	let user = $state<User | null>($auth.user);
	let isLoading = $state(true);
	let isSavingProfile = $state(false);
	let isSavingPreferences = $state(false);
	let isChangingPassword = $state(false);
	let mounted = $state(false);

	// Forms
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

	let errors = $state<Record<string, string>>({});
	let successMessage = $state<string>('');

	// Available options
	const languages: { code: Language; label: string; flag: string }[] = [
		{ code: 'uk', label: 'Українська', flag: '🇺🇦' },
		{ code: 'en', label: 'English', flag: '🇺🇸' }
	];
	const themes: { value: Theme; label: string; icon: string }[] = [
		{ value: 'light', label: 'Light', icon: '☀️' },
		{ value: 'dark', label: 'Dark', icon: '🌙' },
		{ value: 'system', label: 'System', icon: '💻' }
	];
	const timezones = [
		{ value: 'UTC', label: 'UTC (Coordinated Universal Time)' },
		{ value: 'Europe/Kiev', label: 'Europe/Kiev (Eastern European Time)' },
		{ value: 'Europe/London', label: 'Europe/London (Greenwich Mean Time)' },
		{ value: 'Europe/Berlin', label: 'Europe/Berlin (Central European Time)' },
		{ value: 'America/New_York', label: 'America/New_York (Eastern Time)' },
		{ value: 'America/Chicago', label: 'America/Chicago (Central Time)' },
		{ value: 'America/Denver', label: 'America/Denver (Mountain Time)' },
		{ value: 'America/Los_Angeles', label: 'America/Los_Angeles (Pacific Time)' },
		{ value: 'Asia/Tokyo', label: 'Asia/Tokyo (Japan Standard Time)' },
		{ value: 'Australia/Sydney', label: 'Australia/Sydney (Australian Eastern Time)' }
	];
	const languageOptions = languages.map(lang => ({
		value: lang.code,
		label: `${lang.flag} ${lang.label}`
	}));

	const themeOptions = themes.map(theme => ({
		value: theme.value,
		label: `${theme.icon} ${theme.label}`
	}));

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}

		await loadUserProfile();
		mounted = true;
	});

	async function loadUserProfile() {
		try {
			const response = await api.get<User>('/v1/users/me');
			if (response.data) {
				user = response.data;
				auth.login($auth.token!, response.data);
				
				// Initialize forms with current user data
				profileForm = {
					firstName: user.firstName,
					lastName: user.lastName,
					timeZone: user.timeZone || 'UTC',
					language: user.language || 'uk',
					emailNotificationsEnabled: user.emailNotificationsEnabled
				};
			}
		} catch (error) {
			console.error('Failed to load user profile:', error);
		} finally {
			isLoading = false;
		}
	}

	async function handleUpdateProfile(event: Event) {
		event.preventDefault();
		errors = {};
		successMessage = '';
		isSavingProfile = true;
		try {
			const response = await api.put<User>('/v1/users/me', profileForm);
			if (response.data) {
				user = response.data;
				auth.login($auth.token!, response.data);
				successMessage = t('success.profileUpdated', $language);
			} else if (response.message) {
				errors.profile = response.message;
			}
		} catch (error) {
			errors.profile = t('errors.network', $language);
		} finally {
			isSavingProfile = false;
		}
	}

	async function handleChangePassword(event: Event) {
		event.preventDefault();
		errors = {};
		successMessage = '';

		// Validate passwords match
		if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
			errors.confirmNewPassword = t('errors.passwordsNotMatch', $language);
			return;
		}

		if (passwordForm.newPassword.length < 6) {
			errors.newPassword = t('errors.passwordTooShort', $language);
			return;
		}

		isChangingPassword = true;
		try {
			const response = await api.post('/v1/users/change-password', {
				currentPassword: passwordForm.currentPassword,
				newPassword: passwordForm.newPassword,
				confirmNewPassword: passwordForm.confirmNewPassword
			});

			if (response.data || response.message?.includes('success')) {
				successMessage = t('success.passwordChanged', $language);
				passwordForm = {
					currentPassword: '',
					newPassword: '',
					confirmNewPassword: ''
				};
			} else if (response.message) {
				errors.password = response.message;
			}
		} catch (error) {
			errors.password = t('errors.network', $language);
		} finally {
			isChangingPassword = false;
		}
	}

	function handleLanguageChange(newLang: Language) {
		profileForm.language = newLang;
		language.setLanguage(newLang);
		clearMessages();
	}

	function handleThemeChange(newTheme: Theme) {
		theme.setTheme(newTheme);
	}

	function clearMessages() {
		errors = {};
		successMessage = '';
	}

	const currentTheme = $derived($theme);
</script>

<svelte:head>
	<title>{t('profile.title', $language)} - CertVal</title>
	<meta name="description" content="Manage your user profile, account settings, and preferences" />
</svelte:head>

<div class="space-y-8 pb-8">
	<div class="relative overflow-hidden {mounted ? 'animate-in slide-in-from-top-4 duration-500' : ''}">
		<div class="relative z-10">
			<h1 class="text-3xl font-bold bg-gradient-to-r from-gray-900 to-gray-700 dark:from-white dark:to-gray-300 bg-clip-text text-transparent">
				{t('profile.title', $language)}
			</h1>
			<p class="mt-2 text-gray-600 dark:text-gray-400">
				Manage your account settings and preferences
			</p>
		</div>
		
		<div class="absolute -top-4 -right-4 h-24 w-24 rounded-full bg-gradient-to-br from-blue-400/20 to-indigo-400/20 blur-2xl"></div>
	</div>

	{#if isLoading}
		<div class="flex h-64 items-center justify-center">
			<div class="relative">
				<div class="h-16 w-16 animate-spin rounded-full border-4 border-blue-200 dark:border-blue-800"></div>
				<div class="absolute top-0 left-0 h-16 w-16 animate-spin rounded-full border-4 border-transparent border-t-blue-600"></div>
			</div>
		</div>
	{:else if user}
		{#if successMessage}
			<div class="animate-in slide-in-from-top-2 duration-300">
				<div class="rounded-xl border border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-900/30 p-4">
					<div class="flex">
						<svg class="h-5 w-5 text-green-400 dark:text-green-300" fill="currentColor" viewBox="0 0 20 20">
							<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
						</svg>
						<div class="ml-3">
							<p class="text-sm text-green-700 dark:text-green-300">{successMessage}</p>
						</div>
						<div class="ml-auto pl-3">
							<button
								onclick={() => successMessage = ''}
								class="text-green-400 dark:text-green-300 hover:text-green-600 dark:hover:text-green-400 transition-colors"
								aria-label="Dismiss"
							>
								<svg class="h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
								</svg>
							</button>
						</div>
					</div>
				</div>
			</div>
		{/if}

		<div class="grid grid-cols-1 gap-8 lg:grid-cols-3">
			<div class="lg:col-span-1">
				<Card glass={true} class={mounted ? 'animate-in slide-in-from-left-8 duration-700' : ''}>
					<div class="text-center">
						<div class="mx-auto flex h-24 w-24 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-purple-600 text-2xl font-bold text-white shadow-lg ring-4 ring-white dark:ring-gray-900">
							{user.firstName?.charAt(0)}{user.lastName?.charAt(0)}
						</div>
						<div class="mt-4">
							<h2 class="text-xl font-semibold text-gray-900 dark:text-white">{user.fullName}</h2>
							<p class="text-gray-600 dark:text-gray-400">{user.email}</p>
						</div>
						
						<div class="mt-6 grid grid-cols-2 gap-4 text-center">
							<div>
								<div class="text-sm font-medium text-gray-500 dark:text-gray-400">{t('profile.accountCreated', $language)}</div>
								<div class="text-sm text-gray-900 dark:text-white">{formatDateTime(user.createdAt).split(',')[0]}</div>
							</div>
							<div>
								<div class="text-sm font-medium text-gray-500 dark:text-gray-400">{t('profile.lastLogin', $language)}</div>
								<div class="text-sm text-gray-900 dark:text-white">{formatDateTime(user.lastLoginAt).split(',')[0]}</div>
							</div>
						</div>

						<div class="mt-6 flex items-center justify-center space-x-2">
							<div class="flex h-2 w-2 rounded-full bg-green-400"></div>
							<span class="text-xs text-gray-500 dark:text-gray-400">Active Account</span>
						</div>
					</div>
				</Card>
			</div>

			<div class="space-y-8 lg:col-span-2">
				<Card 
					title={t('profile.personalInfo', $language)}
					glass={true}
					class={mounted ? 'animate-in slide-in-from-bottom-8 duration-700' : ''}
				>
					<form onsubmit={handleUpdateProfile} class="space-y-6">
						{#if errors.profile}
							<div class="rounded-xl border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/30 p-4">
								<div class="flex">
									<svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
										<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
									</svg>
									<div class="ml-3">
										<p class="text-sm text-red-700 dark:text-red-300">{errors.profile}</p>
									</div>
								</div>
							</div>
						{/if}

						<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
							<Input
								label={t('auth.register.firstName', $language)}
								bind:value={profileForm.firstName}
								required
								oninput={clearMessages}
								placeholder="First Name"
								icon="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
							/>

							<Input
								label={t('auth.register.lastName', $language)}
								bind:value={profileForm.lastName}
								required
								oninput={clearMessages}
								placeholder="Last Name"
								icon="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
							/>
						</div>

						<Input
							type="email"
							label={t('auth.login.email', $language)}
							value={user.email}
							disabled
							placeholder="Email Address"
							icon="M3 8l7.89 7.89a1 1 0 001.415 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
						/>

						<Select
							label={t('profile.timezone', $language)}
							bind:value={profileForm.timeZone}
							options={timezones}
							onchange={clearMessages}
						/>

						<div class="flex items-center justify-between rounded-xl border border-gray-200 dark:border-gray-700 p-4">
							<div class="flex items-center">
								<svg class="h-5 w-5 text-gray-400 mr-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 7.89a1 1 0 001.415 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
								</svg>
								<div>
									<label for="email-notifications" class="text-sm font-medium text-gray-900 dark:text-white">
										{t('profile.emailNotifications', $language)}
									</label>
									<p class="text-xs text-gray-500 dark:text-gray-400">
										Receive email notifications about certificate expiry
									</p>
								</div>
							</div>
							<label class="relative inline-flex cursor-pointer items-center">
								<input
									id="email-notifications"
									type="checkbox"
									bind:checked={profileForm.emailNotificationsEnabled}
									onchange={clearMessages}
									class="peer sr-only"
								/>
								<div class="peer h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-0.5 after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all after:content-[''] peer-checked:bg-blue-600 peer-checked:after:translate-x-full peer-checked:after:border-white peer-focus:ring-4 peer-focus:ring-blue-300 dark:border-gray-600 dark:bg-gray-700 dark:peer-focus:ring-blue-800"></div>
							</label>
						</div>

						<div class="flex justify-end pt-4">
							<Button
								type="submit"
								loading={isSavingProfile}
								disabled={isSavingProfile}
								class="min-w-[120px]"
							>
								<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
								</svg>
								{t('common.save', $language)}
							</Button>
						</div>
					</form>
				</Card>

				<Card 
					title={t('profile.preferences', $language)}
					glass={true}
					class={mounted ? 'animate-in slide-in-from-bottom-8 duration-700' : ''}
				>
					<div class="space-y-6">
						<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
							<div>
								<label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
									{t('profile.language', $language)}
								</label>
								<div class="space-y-2">
									{#each languages as lang}
										<label class="flex items-center cursor-pointer">
											<input
												type="radio"
												name="language"
												value={lang.code}
												checked={profileForm.language === lang.code}
												onchange={() => handleLanguageChange(lang.code)}
												class="text-blue-600 focus:ring-blue-500 focus:ring-2"
											/>
											<span class="ml-3 text-lg">{lang.flag}</span>
											<span class="ml-2 text-sm text-gray-900 dark:text-white">{lang.label}</span>
										</label>
									{/each}
								</div>
							</div>

							<div>
								<label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
									Theme Preference
								</label>
								<div class="space-y-2">
									{#each themes as themeOption}
										<label class="flex items-center cursor-pointer">
											<input
												type="radio"
												name="theme"
												value={themeOption.value}
												checked={currentTheme.theme === themeOption.value}
												onchange={() => handleThemeChange(themeOption.value)}
												class="text-blue-600 focus:ring-blue-500 focus:ring-2"
											/>
											<span class="ml-3 text-lg">{themeOption.icon}</span>
											<span class="ml-2 text-sm text-gray-900 dark:text-white">{themeOption.label}</span>
										</label>
									{/each}
								</div>
							</div>
						</div>
					</div>
				</Card>

				<Card 
					title={t('profile.security', $language)}
					glass={true}
					class={mounted ? 'animate-in slide-in-from-bottom-8 duration-700' : ''}
				>
					<form onsubmit={handleChangePassword} class="space-y-6">
						{#if errors.password}
							<div class="rounded-xl border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/30 p-4">
								<div class="flex">
									<svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
										<path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
									</svg>
									<div class="ml-3">
										<p class="text-sm text-red-700 dark:text-red-300">{errors.password}</p>
									</div>
								</div>
							</div>
						{/if}

						<Input
							type="password"
							label={t('profile.currentPassword', $language)}
							bind:value={passwordForm.currentPassword}
							required
							oninput={clearMessages}
							placeholder="Enter current password"
							icon="M12 15v2a6 6 0 11-6-6c0-1.657.672-3.157 1.757-4.243L7.5 6.5a1.5 1.5 0 113 0v2.257A6.958 6.958 0 0112 8a6.958 6.958 0 011.243.757V6.5a1.5 1.5 0 113 0l.257.257A5.98 5.98 0 0118 11a6 6 0 11-6 4z"
						/>

						<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
							<Input
								type="password"
								label={t('profile.newPassword', $language)}
								bind:value={passwordForm.newPassword}
								required
								error={errors.newPassword}
								oninput={clearMessages}
								placeholder="Enter new password"
								icon="M12 15v2a6 6 0 11-6-6c0-1.657.672-3.157 1.757-4.243L7.5 6.5a1.5 1.5 0 113 0v2.257A6.958 6.958 0 0112 8a6.958 6.958 0 011.243.757V6.5a1.5 1.5 0 113 0l.257.257A5.98 5.98 0 0118 11a6 6 0 11-6 4z"
							/>

							<Input
								type="password"
								label={t('profile.confirmNewPassword', $language)}
								bind:value={passwordForm.confirmNewPassword}
								required
								error={errors.confirmNewPassword}
								oninput={clearMessages}
								placeholder="Confirm new password"
								icon="M12 15v2a6 6 0 11-6-6c0-1.657.672-3.157 1.757-4.243L7.5 6.5a1.5 1.5 0 113 0v2.257A6.958 6.958 0 0112 8a6.958 6.958 0 011.243.757V6.5a1.5 1.5 0 113 0l.257.257A5.98 5.98 0 0118 11a6 6 0 11-6 4z"
							/>
						</div>

						<div class="rounded-xl border border-yellow-200 dark:border-yellow-800 bg-yellow-50 dark:bg-yellow-900/30 p-4">
							<div class="flex">
								<svg class="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
									<path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
								</svg>
								<div class="ml-3">
									<p class="text-sm text-yellow-700 dark:text-yellow-300">
										<strong>Password Requirements:</strong> Your new password should be at least 6 characters long and contain a mix of letters, numbers, and special characters for better security.
									</p>
								</div>
							</div>
						</div>

						<div class="flex justify-end pt-4">
							<Button
								type="submit"
								loading={isChangingPassword}
								disabled={isChangingPassword || !passwordForm.currentPassword || !passwordForm.newPassword || !passwordForm.confirmNewPassword}
								class="min-w-[150px]"
								variant="secondary"
							>
								<svg class="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2a6 6 0 11-6-6c0-1.657.672-3.157 1.757-4.243L7.5 6.5a1.5 1.5 0 113 0v2.257A6.958 6.958 0 0112 8a6.958 6.958 0 011.243.757V6.5a1.5 1.5 0 113 0l.257.257A5.98 5.98 0 0118 11a6 6 0 11-6 4z" />
								</svg>
								{t('profile.changePassword', $language)}
							</Button>
						</div>
					</form>
				</Card>

				<Card 
					title={t('profile.accountSettings', $language)}
					glass={true}
					class={mounted ? 'animate-in slide-in-from-bottom-8 duration-700' : ''}
				>
					<div class="space-y-6">
						<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
							<div>
								<dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Account Status</dt>
								<dd class="mt-1 flex items-center text-sm text-gray-900 dark:text-white">
									<div class="mr-2 h-2 w-2 rounded-full bg-green-400"></div>
									Active
									{#if user.isEmailConfirmed}
										<span class="ml-2 inline-flex items-center rounded-full bg-green-100 dark:bg-green-900/30 px-2 py-0.5 text-xs font-medium text-green-800 dark:text-green-300">
											Verified
										</span>
									{:else}
										<span class="ml-2 inline-flex items-center rounded-full bg-yellow-100 dark:bg-yellow-900/30 px-2 py-0.5 text-xs font-medium text-yellow-800 dark:text-yellow-300">
											Unverified
										</span>
									{/if}
								</dd>
							</div>

							<div>
								<dt class="text-sm font-medium text-gray-500 dark:text-gray-400">Current Language</dt>
								<dd class="mt-1 text-sm text-gray-900 dark:text-white">
									{languages.find(l => l.code === $language)?.flag} {languages.find(l => l.code === $language)?.label}
								</dd>
							</div>
						</div>
					</div>
				</Card>
			</div>
		</div>
	{/if}
</div>