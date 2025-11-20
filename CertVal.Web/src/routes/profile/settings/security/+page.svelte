<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
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
			errors.password = t('errors.passwordsNotMatch', language.current);
			return;
		}

		isChangingPassword = true;
		try {
			const response = await api.post('/users/change-password', passwordForm);
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
			label={t('profile.currentPassword', language.current)}
			type="password"
			bind:value={passwordForm.currentPassword}
			required
		/>
		<Input
			label={t('profile.newPassword', language.current)}
			type="password"
			bind:value={passwordForm.newPassword}
			required
		/>
		<Input
			label={t('profile.confirmNewPassword', language.current)}
			type="password"
			bind:value={passwordForm.confirmNewPassword}
			required
		/>
		<div class="card-actions justify-end">
			<Button type="submit" variant="primary" loading={isChangingPassword}>
				{t('profile.changePassword', language.current)}
			</Button>
		</div>
	</form>
</Card>
