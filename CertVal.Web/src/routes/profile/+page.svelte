<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { withMinDelay } from '$lib/utils/loading';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import type { User } from '$lib/types';

	let user = $state<User | null>(null);
	let isLoading = $state(true);

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
			const response = await withMinDelay(api.get<User>('/users/me'));
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
	<title>{t('profile.title', language.current)}</title>
</svelte:head>

<div class="space-y-6">
	<div class="flex flex-col items-start justify-between gap-4 md:flex-row md:items-center">
		<div>
			<h1 class="text-3xl font-bold">{t('profile.title', language.current)}</h1>
			<p class="text-base-content/70 mt-1">{t('profile.subtitle', language.current)}</p>
		</div>
		<Button onclick={() => goto('/profile/settings')}
			>{t('profile.editProfile', language.current)}</Button
		>
	</div>

	<div class="relative min-h-[400px]">
		{#if isLoading}
			<GlobalLoader variant="overlay" class="bg-transparent backdrop-blur-none" />
		{:else if user}
			<Card>
				<div class="flex flex-col items-center gap-6 p-4 text-center md:flex-row md:text-left">
					<UserAvatar firstName={user.firstName} lastName={user.lastName} />
					<div class="flex-1">
						<h2 class="text-2xl font-bold">{user.fullName}</h2>
						<p class="text-base-content/60">{user.email}</p>
						<div class="mt-4 grid grid-cols-1 gap-4 text-sm sm:grid-cols-3">
							<div>
								<div class="font-semibold opacity-70">{t('profile.status', language.current)}</div>
								<div>{user.status}</div>
							</div>
							<div>
								<div class="font-semibold opacity-70">
									{t('profile.lastLogin', language.current)}
								</div>
								<div>{formatDateTime(user.lastLoginAt)}</div>
							</div>
							<div>
								<div class="font-semibold opacity-70">
									{t('profile.joinedOn', language.current)}
								</div>
								<div>{formatDateTime(user.createdAt)}</div>
							</div>
						</div>
					</div>
				</div>
			</Card>
		{/if}
	</div>
</div>
