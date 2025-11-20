<script lang="ts">
	import { page } from '$app/state';
	import { language } from '$lib/stores/language.svelte';
	import { theme } from '$lib/stores/theme.svelte';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { AuthService } from '$lib/services/AuthService';
	import { t } from '$lib/i18n';
	import { goto } from '$app/navigation';
	import logoUrl from '$lib/assets/favicon.svg?url';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { IconName } from '$lib/icons';
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import { fly } from 'svelte/transition';

	let isUserMenuOpen = $state(false);

	const menuItems: { label: string; href: string; icon: IconName }[] = [
		{ label: 'nav.dashboard', href: '/dashboard', icon: 'menu' },
		{
			label: 'nav.workspaces',
			href: '/workspaces',
			icon: 'workspaces'
		},
		{
			label: 'nav.certificates',
			href: '/certificates',
			icon: 'certificates'
		},
		{
			label: 'nav.notifications',
			href: '/notifications',
			icon: 'notifications'
		}
	];
	const currentPath = $derived(page.url.pathname);
	const user = $derived(userSession.user);

	function isActive(href: string): boolean {
		if (href === '/dashboard') return currentPath === '/dashboard' || currentPath === '/';
		return currentPath.startsWith(href);
	}

	function handleLogout() {
		AuthService.logout();
		goto('/auth/login');
	}

	function toggleUserMenu() {
		isUserMenuOpen = !isUserMenuOpen;
	}

	function closeUserMenu() {
		isUserMenuOpen = false;
	}
</script>

<aside class="drawer-side z-40">
	<label for="drawer-toggle" class="drawer-overlay lg:hidden"></label>
	<div class="text-base-content bg-base-200 flex min-h-full w-80 flex-col gap-2 p-4">
		<div class="flex flex-1 flex-col transition-all duration-300">
			<button
				onclick={() => goto('/dashboard')}
				class="group relative flex w-full items-center gap-4 p-6 text-left"
			>
				<div
					class="from-primary to-secondary shadow-primary/20 relative flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br shadow-lg transition-transform duration-500 group-hover:rotate-12 group-hover:scale-110"
				>
					<img
						src={logoUrl}
						alt="CertVal logo"
						class="h-7 w-7 transition-all duration-300 {theme.current === 'dark'
							? ''
							: 'invert filter'}"
					/>
					<div
						class="bg-primary/20 absolute -inset-1 animate-pulse rounded-2xl blur-lg transition-opacity group-hover:opacity-100"
					></div>
				</div>
				<div class="flex flex-col">
					<span
						class="from-base-content to-base-content/60 bg-gradient-to-r bg-clip-text text-xl font-bold tracking-wide text-transparent"
					>
						CertVal
					</span>
					<span class="text-base-content/50 text-xs font-medium uppercase tracking-wider"
						>Certificate Monitor</span
					>
				</div>
			</button>

			<nav class="flex-1 px-4 py-6">
				<ul class="space-y-2">
					{#each menuItems as item}
						{@const active = isActive(item.href)}
						<li>
							<button
								onclick={() => goto(item.href)}
								class="group relative flex w-full items-center gap-3 overflow-hidden rounded-xl px-4 py-3.5 text-left font-medium transition-all duration-300
									   {active
									? 'bg-primary text-primary-content shadow-primary/30 shadow-lg ring-1 ring-white/20'
									: 'text-base-content/70 hover:bg-base-content/5 hover:text-primary hover:shadow-md'}"
							>
								{#if active}
									<div
										class="absolute inset-0 bg-gradient-to-r from-white/20 to-transparent opacity-50"
									></div>
									<div
										class="absolute -left-1 top-1/2 h-8 w-1 -translate-y-1/2 rounded-r-full bg-white shadow-[0_0_10px_rgba(255,255,255,0.5)]"
									></div>
								{/if}
								<Icon
									name={item.icon}
									class="h-5 w-5 transition-transform duration-300 group-hover:scale-110 {active
										? 'animate-pulse'
										: ''}"
								/>
								<span class="relative z-10 tracking-wide">{t(item.label, language.current)}</span>
								{#if !active}
									<Icon
										name="chevronRight"
										class="absolute right-2 h-4 w-4 opacity-0 transition-all duration-300 group-hover:translate-x-0 group-hover:opacity-50"
									/>
								{/if}
							</button>
						</li>
					{/each}
				</ul>
			</nav>

			{#if user}
				<div class="relative mt-auto p-4">
					{#if isUserMenuOpen}
						<div class="fixed inset-0 z-30" onclick={closeUserMenu} role="presentation"></div>
						<ul
							class="border-base-content/10 bg-base-100/90 absolute bottom-full left-4 right-4 z-40 mb-2 rounded-2xl border p-2 shadow-xl backdrop-blur-xl"
							transition:fly={{ y: 10, duration: 200 }}
						>
							<li>
								<button
									onclick={() => {
										goto('/profile');
										closeUserMenu();
									}}
									class="hover:bg-base-content/5 group flex w-full items-center gap-3 rounded-xl px-4 py-3 text-left transition-colors"
								>
									<Icon name="profile" class="h-4 w-4 transition-transform group-hover:scale-110" />
									{t('nav.profile', language.current)}
								</button>
							</li>
							<li>
								<button
									onclick={() => {
										goto('/profile/settings');
										closeUserMenu();
									}}
									class="hover:bg-base-content/5 group flex w-full items-center gap-3 rounded-xl px-4 py-3 text-left transition-colors"
								>
									<Icon
										name="settings"
										class="h-4 w-4 transition-transform group-hover:rotate-90"
									/>
									{t('nav.settings', language.current)}
								</button>
							</li>
							<div class="bg-base-content/10 my-1 h-px"></div>
							<li>
								<button
									onclick={handleLogout}
									class="text-error hover:bg-error/10 group flex w-full items-center gap-3 rounded-xl px-4 py-3 text-left transition-colors"
								>
									<Icon
										name="logout"
										class="h-4 w-4 transition-transform group-hover:-translate-x-1"
									/>
									{t('nav.logout', language.current)}
								</button>
							</li>
						</ul>
					{/if}

					<div
						class="border-base-content/5 bg-base-content/5 hover:border-primary/20 hover:bg-base-content/10 group relative overflow-hidden rounded-2xl border p-1 transition-all duration-300 hover:shadow-lg"
					>
						<button
							onclick={toggleUserMenu}
							class="flex w-full items-center gap-3 rounded-xl p-2 text-left transition-colors"
						>
							<div class="relative">
								<UserAvatar
									firstName={user.firstName}
									lastName={user.lastName}
									size="w-10"
									textSize="text-lg"
									class="ring-base-content/10 group-hover:ring-primary/50 rounded-full ring-2 transition-all duration-300"
								/>
								<div
									class="border-base-100 bg-success absolute bottom-0 right-0 h-3 w-3 rounded-full border-2 shadow-sm"
								></div>
							</div>
							<div class="flex flex-1 flex-col">
								<span
									class="text-base-content group-hover:text-primary text-sm font-bold transition-colors"
									>{user.firstName} {user.lastName}</span
								>
								<span class="text-base-content/50 text-xs">{user.email}</span>
							</div>
							<div
								class="bg-base-content/5 flex h-8 w-8 items-center justify-center rounded-lg opacity-0 transition-all duration-300 group-hover:opacity-100"
							>
								<Icon name="settings" class="h-4 w-4" />
							</div>
						</button>
					</div>
				</div>
			{/if}
		</div>
	</div>
</aside>
