<script lang="ts">
	import { onMount } from 'svelte';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { UserService } from '$lib/services/UserService';
	import { t } from '$lib/i18n';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { User, Language, UpdateUserRequest, ChangePasswordRequest } from '$lib/types';

	let user = $state<User | null>(null);
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
		{ value: 'Europe/Kyiv', label: 'Europe/Kyiv' },
		{ value: 'Europe/London', label: 'Europe/London' },
		{ value: 'America/New_York', label: 'America/New York' }
	];

	onMount(async () => {
		const currentUser = userSession.user;
		if (currentUser) {
			try {
				const res = await UserService.getProfile();
				if (res.data) {
					user = res.data;
					profileForm = {
						firstName: user.firstName,
						lastName: user.lastName,
						timeZone: user.timeZone || 'UTC',
						language: (user.language as Language) || 'uk',
						emailNotificationsEnabled: user.emailNotificationsEnabled
					};
				}
			} catch (e) {
				console.error('Failed to fetch profile', e);
			}
		}
	});

	async function handleUpdateProfile(event: Event) {
		event.preventDefault();
		isSavingProfile = true;
		errors = {};
		successMessage = '';
		try {
			if (profileForm.language) {
				language.set(profileForm.language as Language);
			}

			const response = await UserService.updateProfile(profileForm);
			if (response.data) {
				if (userSession.token) {
					userSession.login(userSession.token, response.data);
				}
				user = response.data;
				successMessage = t('success.profileUpdated', language.current);
			} else {
				errors.profile = response.message || t('errors.general', language.current);
			}
		} catch (err) {
			errors.profile = t('errors.network', language.current);
		} finally {
			isSavingProfile = false;
		}
	}

	async function handleChangePassword(event: Event) {
		event.preventDefault();
		errors = {};
		successMessage = '';
		if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
			errors.password = t('errors.passwordsNotMatch', language.current);
			return;
		}

		isChangingPassword = true;
		try {
			const response = await UserService.changePassword(passwordForm);
			if (response.data || !response.message) {
				successMessage = t('success.passwordChanged', language.current);
				passwordForm = { currentPassword: '', newPassword: '', confirmNewPassword: '' };
			} else {
				errors.password = response.message || t('errors.general', language.current);
			}
		} catch (err) {
			errors.password = t('errors.network', language.current);
		} finally {
			isChangingPassword = false;
		}
	}
</script>

<div class="animate-in fade-in slide-in-from-bottom-4 space-y-12 duration-500">
	<div class="space-y-8">
		<div
			class="border-base-content/10 flex flex-col items-start justify-between gap-4 border-b pb-6 md:flex-row md:items-center"
		>
			<div>
				<h1
					class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-3xl font-bold text-transparent"
				>
					{t('profile.personalInfo', language.current)}
				</h1>
				<p class="text-base-content/60 mt-2 text-lg font-light">
					{t('profile.personalInfoSubtitle', language.current)}
				</p>
			</div>
			<Button
				type="submit"
				form="profile-form"
				variant="primary"
				loading={isSavingProfile}
				class="shadow-primary/20 min-w-[150px] shadow-lg"
			>
				<Icon name="save" class="mr-2 h-5 w-5" />
				{t('common.save', language.current)}
			</Button>
		</div>

		<form id="profile-form" onsubmit={handleUpdateProfile} class="space-y-8">
			{#if successMessage && !isChangingPassword}
				<div role="alert" class="alert alert-success border-success/20 bg-success/10 shadow-lg">
					<Icon name="checkCircle" class="text-success h-6 w-6" />
					<span>{successMessage}</span>
				</div>
			{/if}
			{#if errors.profile}
				<div role="alert" class="alert alert-error border-error/20 bg-error/10 shadow-lg">
					<Icon name="error" class="text-error h-6 w-6" />
					<span>{errors.profile}</span>
				</div>
			{/if}

			<div class="grid grid-cols-1 gap-12 lg:grid-cols-2">
				<div class="space-y-6">
					<h3 class="flex items-center gap-2 text-xl font-semibold">
						<div class="bg-primary/10 text-primary rounded-lg p-2">
							<Icon name="profile" class="h-5 w-5" />
						</div>
						{t('profile.basicInformation', language.current)}
					</h3>

					<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
						<Input
							label={t('auth.register.firstName', language.current)}
							bind:value={profileForm.firstName}
							required
							class="bg-base-100/50"
						/>
						<Input
							label={t('auth.register.lastName', language.current)}
							bind:value={profileForm.lastName}
							required
							class="bg-base-100/50"
						/>
					</div>

					<div class="form-control w-full">
						<label class="label" for="email">
							<span class="label-text font-medium">{t('auth.login.email', language.current)}</span>
						</label>
						<input
							type="email"
							value={user?.email}
							disabled
							class="input input-bordered bg-base-200/50 text-base-content/60 w-full cursor-not-allowed"
						/>
						<label class="label" for="email">
							<span class="label-text-alt text-base-content/40"
								>{t('profile.emailCannotBeChanged', language.current)}</span
							>
						</label>
					</div>
				</div>

				<div class="space-y-6">
					<h3 class="flex items-center gap-2 text-xl font-semibold">
						<div class="bg-secondary/10 text-secondary rounded-lg p-2">
							<Icon name="settings" class="h-5 w-5" />
						</div>
						{t('profile.preferences', language.current)}
					</h3>

					<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
						<Select
							label={t('profile.language', language.current)}
							bind:value={profileForm.language}
							options={languages}
							class="bg-base-100/50"
						/>
						<Select
							label={t('profile.timezone', language.current)}
							bind:value={profileForm.timeZone}
							options={timezones}
							class="bg-base-100/50"
						/>
					</div>

					<div class="form-control">
						<label
							class="label border-base-content/10 bg-base-100/30 hover:bg-base-100/50 cursor-pointer justify-start gap-4 rounded-xl border p-4 transition-colors"
						>
							<input
								type="checkbox"
								class="checkbox checkbox-primary"
								bind:checked={profileForm.emailNotificationsEnabled}
							/>
							<div class="flex flex-col">
								<span class="label-text text-base font-medium"
									>{t('profile.emailNotifications', language.current)}</span
								>
								<span class="label-text-alt text-base-content/60"
									>Receive updates about your certificates and account</span
								>
							</div>
						</label>
					</div>
				</div>
			</div>
		</form>
	</div>

	<div class="border-base-content/10 space-y-8 border-t pt-8">
		<form id="security-form" onsubmit={handleChangePassword} class="space-y-8">
			{#if successMessage && isChangingPassword}
				<div role="alert" class="alert alert-success border-success/20 bg-success/10 shadow-lg">
					<Icon name="checkCircle" class="text-success h-6 w-6" />
					<span>{successMessage}</span>
				</div>
			{/if}
			{#if errors.password}
				<div role="alert" class="alert alert-error border-error/20 bg-error/10 shadow-lg">
					<Icon name="error" class="text-error h-6 w-6" />
					<span>{errors.password}</span>
				</div>
			{/if}

			<div class="space-y-6">
				<div class="flex flex-col items-start justify-between gap-4 md:flex-row md:items-center">
					<h3 class="flex items-center gap-2 text-xl font-semibold">
						<div class="bg-primary/10 text-primary rounded-lg p-2">
							<Icon name="lock" class="h-5 w-5" />
						</div>
						{t('profile.security', language.current)}: {t(
							'profile.changePassword',
							language.current
						).toLowerCase()}
					</h3>
					<Button
						type="submit"
						variant="primary"
						loading={isChangingPassword}
						class="shadow-primary/20 min-w-[150px] shadow-lg"
					>
						<Icon name="lock" class="mr-2 h-5 w-5" />
						{t('profile.changePassword', language.current)}
					</Button>
				</div>

				<div class="grid grid-cols-1 gap-6 md:grid-cols-3">
					<Input
						label={t('profile.currentPassword', language.current)}
						type="password"
						bind:value={passwordForm.currentPassword}
						required
						class="bg-base-100/50"
					/>
					<Input
						label={t('profile.newPassword', language.current)}
						type="password"
						bind:value={passwordForm.newPassword}
						required
						class="bg-base-100/50"
					/>
					<Input
						label={t('profile.confirmNewPassword', language.current)}
						type="password"
						bind:value={passwordForm.confirmNewPassword}
						required
						class="bg-base-100/50"
					/>
				</div>
			</div>
		</form>
	</div>
</div>
