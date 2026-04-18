<script lang="ts">
	import { onMount } from 'svelte';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { UserService } from '$lib/services/UserService';
	import { t } from '$lib/i18n';
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

<div class="page" data-test-id="personal-settings-page">
	<section class="section">
		<header class="section__header">
			<div class="section__header-content">
				<h2 class="section__title">{t('profile.personalInfo', language.current)}</h2>
				<p class="section__subtitle">{t('profile.personalInfoSubtitle', language.current)}</p>
			</div>
			<Button
				type="submit"
				form="profile-form"
				variant="primary"
				loading={isSavingProfile}
				data-test-id="save-profile-button"
			>
				<Icon name="save" />
				{t('common.save', language.current)}
			</Button>
		</header>

		<form id="profile-form" onsubmit={handleUpdateProfile} class="form">
			{#if successMessage && !isChangingPassword}
				<div class="alert alert--success">
					<Icon name="checkCircle" />
					<span>{successMessage}</span>
				</div>
			{/if}
			{#if errors.profile}
				<div class="alert alert--error">
					<Icon name="error" />
					<span>{errors.profile}</span>
				</div>
			{/if}

			<div class="form__grid">
				<div class="form__column">
					<div class="form__section-header">
						<div class="form__section-icon">
							<Icon name="profile" />
						</div>
						<h3 class="form__section-title">{t('profile.basicInformation', language.current)}</h3>
					</div>

					<div class="form__row">
						<Input
							label={t('auth.register.firstName', language.current)}
							bind:value={profileForm.firstName}
							required
							data-test-id="profile-firstname-input"
						/>
						<Input
							label={t('auth.register.lastName', language.current)}
							bind:value={profileForm.lastName}
							required
							data-test-id="profile-lastname-input"
						/>
					</div>

					<div class="form-group">
						<label class="form-label" for="email">{t('auth.login.email', language.current)}</label>
						<input
							id="email"
							type="email"
							value={user?.email}
							disabled
							class="form-input form-input--disabled"
							data-test-id="profile-email-input"
						/>
						<span class="form-hint">{t('profile.emailCannotBeChanged', language.current)}</span>
					</div>
				</div>

				<div class="form__column">
					<div class="form__section-header">
						<div class="form__section-icon form__section-icon--secondary">
							<Icon name="settings" />
						</div>
						<h3 class="form__section-title">{t('profile.preferences', language.current)}</h3>
					</div>

					<div class="form__row">
						<Select
							label={t('profile.language', language.current)}
							bind:value={profileForm.language}
							options={languages}
							data-test-id="profile-language-select"
						/>
						<Select
							label={t('profile.timezone', language.current)}
							bind:value={profileForm.timeZone}
							options={timezones}
							data-test-id="profile-timezone-select"
						/>
					</div>

					<label class="checkbox-card">
						<input
							type="checkbox"
							class="checkbox"
							bind:checked={profileForm.emailNotificationsEnabled}
							data-test-id="profile-notifications-checkbox"
						/>
						<div class="checkbox-card__content">
							<span class="checkbox-card__title">{t('profile.emailNotifications', language.current)}</span>
							<span class="checkbox-card__description">Receive updates about your certificates and account</span>
						</div>
					</label>
				</div>
			</div>
		</form>
	</section>

	<section class="section section--bordered">
		<form id="security-form" onsubmit={handleChangePassword} class="form">
			{#if successMessage && isChangingPassword}
				<div class="alert alert--success">
					<Icon name="checkCircle" />
					<span>{successMessage}</span>
				</div>
			{/if}
			{#if errors.password}
				<div class="alert alert--error">
					<Icon name="error" />
					<span>{errors.password}</span>
				</div>
			{/if}

			<header class="section__header">
				<div class="form__section-header">
					<div class="form__section-icon">
						<Icon name="lock" />
					</div>
					<h3 class="form__section-title">
						{t('profile.security', language.current)}: {t('profile.changePassword', language.current).toLowerCase()}
					</h3>
				</div>
				<Button
					type="submit"
					variant="primary"
					loading={isChangingPassword}
					data-test-id="change-password-button"
				>
					<Icon name="lock" />
					{t('profile.changePassword', language.current)}
				</Button>
			</header>

			<div class="form__row form__row--three">
				<Input
					label={t('profile.currentPassword', language.current)}
					type="password"
					bind:value={passwordForm.currentPassword}
					required
					data-test-id="current-password-input"
				/>
				<Input
					label={t('profile.newPassword', language.current)}
					type="password"
					bind:value={passwordForm.newPassword}
					required
					data-test-id="new-password-input"
				/>
				<Input
					label={t('profile.confirmNewPassword', language.current)}
					type="password"
					bind:value={passwordForm.confirmNewPassword}
					required
					data-test-id="confirm-new-password-input"
				/>
			</div>
		</form>
	</section>
</div>

<style>
	.page {
		display: flex;
		flex-direction: column;
		gap: var(--space-12);
	}

	.section {
		display: flex;
		flex-direction: column;
		gap: var(--space-8);
	}

	.section--bordered {
		padding-top: var(--space-8);
		border-top: 1px solid var(--color-border);
	}

	.section__header {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
		padding-bottom: var(--space-6);
		border-bottom: 1px solid var(--color-border);
	}

	@media (min-width: 768px) {
		.section__header {
			flex-direction: row;
			justify-content: space-between;
			align-items: flex-start;
		}
	}

	.section__header-content {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.section__title {
		font-family: var(--font-display);
		font-size: var(--text-3xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: var(--leading-tight);
		color: var(--color-text);
	}

	.section__subtitle {
		font-size: var(--text-md);
		color: var(--color-text-secondary);
	}

	.form {
		display: flex;
		flex-direction: column;
		gap: var(--space-8);
	}

	.form__grid {
		display: grid;
		grid-template-columns: 1fr;
		gap: var(--space-12);
	}

	@media (min-width: 1024px) {
		.form__grid {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	.form__column {
		display: flex;
		flex-direction: column;
		gap: var(--space-6);
	}

	.form__section-header {
		display: flex;
		align-items: center;
		gap: var(--space-3);
	}

	.form__section-icon {
		display: flex;
		align-items: center;
		justify-content: center;
		width: 36px;
		height: 36px;
		background: var(--color-primary-bg);
		color: var(--color-primary);
		border-radius: var(--radius-md);
	}

	.form__section-icon--secondary {
		background: var(--color-bg-secondary);
		color: var(--color-text-secondary);
	}

	.form__section-title {
		font-size: var(--text-xl);
		font-weight: 600;
		color: var(--color-text-primary);
	}

	.form__row {
		display: grid;
		grid-template-columns: 1fr;
		gap: var(--space-4);
	}

	@media (min-width: 640px) {
		.form__row {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	.form__row--three {
		grid-template-columns: 1fr;
	}

	@media (min-width: 768px) {
		.form__row--three {
			grid-template-columns: repeat(3, 1fr);
		}
	}

	.form-group {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.form-label {
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text-primary);
	}

	.form-input {
		width: 100%;
		padding: var(--space-3) var(--space-4);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		font-size: var(--text-base);
		color: var(--color-text-primary);
		background: var(--color-surface);
		transition: border-color 0.2s, box-shadow 0.2s;
	}

	.form-input:focus {
		outline: none;
		border-color: var(--color-primary);
		box-shadow: 0 0 0 3px var(--color-primary-bg);
	}

	.form-input--disabled {
		background: var(--color-bg-secondary);
		color: var(--color-text-muted);
		cursor: not-allowed;
	}

	.form-hint {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.checkbox-card {
		display: flex;
		align-items: flex-start;
		gap: var(--space-4);
		padding: var(--space-4);
		background: var(--color-bg-secondary);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		cursor: pointer;
		transition: background-color 0.2s;
	}

	.checkbox-card:hover {
		background: var(--color-surface);
	}

	.checkbox {
		width: 20px;
		height: 20px;
		accent-color: var(--color-primary);
		cursor: pointer;
	}

	.checkbox-card__content {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
	}

	.checkbox-card__title {
		font-size: var(--text-base);
		font-weight: 500;
		color: var(--color-text-primary);
	}

	.checkbox-card__description {
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
	}

	.alert {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		padding: var(--space-4);
		border-radius: var(--radius-lg);
		font-size: var(--text-sm);
	}

	.alert--success {
		background: var(--color-success-bg);
		color: var(--color-success);
		border: 1px solid var(--color-success);
	}

	.alert--error {
		background: var(--color-error-bg);
		color: var(--color-error);
		border: 1px solid var(--color-error);
	}
</style>
