<script lang="ts">
	import { onMount } from 'svelte';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import type { User, Language, UpdateUserRequest } from '$lib/types';

	let user = $state<User | null>(null);
	let isSavingProfile = $state(false);
	let successMessage = $state('');
	let errors = $state<Record<string, string>>({});

	let profileForm = $state<UpdateUserRequest>({
		firstName: '',
		lastName: '',
		timeZone: 'UTC',
		language: 'uk',
		emailNotificationsEnabled: true
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
		const currentUser = $auth.user;
		if (currentUser) {
			user = currentUser;
			profileForm = {
				firstName: user.firstName,
				lastName: user.lastName,
				timeZone: user.timeZone || 'UTC',
				language: user.language || 'uk',
				emailNotificationsEnabled: user.emailNotificationsEnabled
			};
		}
	});

	async function handleUpdateProfile(event: Event) {
		event.preventDefault();
		isSavingProfile = true;
		errors = {};
		successMessage = '';
		try {
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
</script>

<Card>
	<form onsubmit={handleUpdateProfile} class="space-y-4">
		{#if successMessage}
			<div role="alert" class="alert alert-success"><span>{successMessage}</span></div>
		{/if}
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
			<Button type="submit" variant="primary" loading={isSavingProfile}>
				{t('profile.saveChanges', $language)}
			</Button>
		</div>
	</form>
</Card>
