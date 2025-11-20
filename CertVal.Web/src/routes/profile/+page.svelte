<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { UserService } from '$lib/services/UserService';
	import { t } from '$lib/i18n';
	import { formatDateTime } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import Loader from '$lib/components/ui/Loader.svelte';
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

<div class="animate-in fade-in slide-in-from-bottom-4 mx-auto max-w-5xl space-y-8 duration-500">
	<div class="flex flex-col items-start justify-between gap-4 md:flex-row md:items-center">
		<div>
			<h1
				class="from-primary to-secondary bg-gradient-to-r bg-clip-text text-4xl font-bold text-transparent"
			>
				{t('profile.title', language.current)}
			</h1>
			<p class="text-base-content/60 mt-2 text-lg font-light">
				{t('profile.subtitle', language.current)}
			</p>
		</div>
		<Button onclick={() => goto('/profile/settings/personal')} variant="outline" class="shadow-sm">
			<Icon name="settings" class="mr-2 h-4 w-4" />
			{t('profile.editProfile', language.current)}
		</Button>
	</div>

	<div class="relative min-h-[400px]">
		{#if isLoading}
			<div class="flex h-64 items-center justify-center">
				<Loader size="lg" />
			</div>
		{:else if user}
			<Card variant="glass" class="border-primary/10 relative overflow-hidden">
				<div
					class="bg-primary/5 pointer-events-none absolute right-0 top-0 -mr-32 -mt-32 h-64 w-64 rounded-full blur-3xl"
				></div>

				<div class="relative z-10 p-8">
					<div class="mb-8 flex flex-col items-center gap-8 md:flex-row">
						<div
							class="ring-primary/10 bg-base-100/50 shrink-0 rounded-full p-6 shadow-xl ring-[12px] backdrop-blur-sm"
						>
							<UserAvatar
								firstName={user.firstName}
								lastName={user.lastName}
								size="w-32"
								textSize="text-5xl"
							/>
						</div>
						<div class="flex-1 space-y-2 text-center md:text-left">
							<h2 class="text-4xl font-bold tracking-tight">{user.fullName}</h2>
							<div
								class="text-base-content/60 flex items-center justify-center gap-2 text-lg font-light md:justify-start"
							>
								<Icon name="mail" class="h-5 w-5 opacity-70" />
								{user.email}
							</div>
						</div>
					</div>

					<div class="divider opacity-50"></div>

					<div class="mt-8 grid grid-cols-1 gap-6 text-sm sm:grid-cols-3">
						<div
							class="bg-base-100/50 border-base-content/5 rounded-2xl border p-5 shadow-sm transition-all duration-300 hover:scale-[1.02] hover:shadow-md"
						>
							<div class="mb-2 flex items-center gap-2 font-medium opacity-70">
								<Icon name="checkCircle" class="text-success h-4 w-4" />
								{t('profile.status', language.current)}
							</div>
							<div class="badge badge-success badge-lg gap-2 shadow-sm">
								<span class="h-2 w-2 animate-pulse rounded-full bg-white"></span>
								{user.status}
							</div>
						</div>

						<div
							class="bg-base-100/50 border-base-content/5 rounded-2xl border p-5 shadow-sm transition-all duration-300 hover:scale-[1.02] hover:shadow-md"
						>
							<div class="mb-2 flex items-center gap-2 font-medium opacity-70">
								<Icon name="time" class="text-primary h-4 w-4" />
								{t('profile.lastLogin', language.current)}
							</div>
							<div class="font-mono text-base font-semibold">
								{formatDateTime(user.lastLoginAt)}
							</div>
						</div>

						<div
							class="bg-base-100/50 border-base-content/5 rounded-2xl border p-5 shadow-sm transition-all duration-300 hover:scale-[1.02] hover:shadow-md"
						>
							<div class="mb-2 flex items-center gap-2 font-medium opacity-70">
								<Icon name="calendar" class="text-secondary h-4 w-4" />
								{t('profile.joinedOn', language.current)}
							</div>
							<div class="font-mono text-base font-semibold">{formatDateTime(user.createdAt)}</div>
						</div>
					</div>
				</div>
			</Card>
		{/if}
	</div>
</div>
