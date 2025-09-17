<script lang="ts">
	import { language } from '$lib/stores/language';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import type { ChangePasswordRequest } from '$lib/types';

	let isChangingPassword = $state(false);
	let successMessage = $state('');
	let errors = $state<Record<string, string>>({});
	let passwordForm = $state<ChangePasswordRequest>({
		currentPassword: '',
		newPassword: '',
		confirmNewPassword: ''
	});

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
			if (response.data || !response.message) {
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

<Card>
	<form onsubmit={handleChangePassword} class="space-y-4">
		{#if successMessage}
			<div role="alert" class="alert alert-success"><span>{successMessage}</span></div>
		{/if}
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
			<Button type="submit" variant="primary" loading={isChangingPassword}>
				{t('profile.changePassword', $language)}
			</Button>
		</div>
	</form>
</Card>
