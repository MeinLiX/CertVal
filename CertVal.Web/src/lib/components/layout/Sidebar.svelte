<script lang="ts">
	import { page } from '$app/state';
	import { language } from '$lib/stores/language';
	import { auth } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import { goto } from '$app/navigation';
	import logoUrl from '$lib/assets/favicon.svg?url';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { IconName } from '$lib/icons';
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';

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
	const user = $derived($auth.user);

	function isActive(href: string): boolean {
		if (href === '/dashboard') return currentPath === '/dashboard' || currentPath === '/';
		return currentPath.startsWith(href);
	}

	function handleLogout() {
		auth.logout();
		goto('/auth/login');
	}
</script>

<aside class="drawer-side z-40">
	<label for="drawer-toggle" class="drawer-overlay lg:hidden"></label>
	<div
		class="flex min-h-full w-64 flex-col border-r border-base-content/10 bg-base-100/95 text-base-content backdrop-blur-lg"
	>
	<a href="/dashboard" class="flex items-center gap-3 border-b border-base-content/10 p-4">
			<img src={logoUrl} alt="CertVal logo" class="h-10 w-10" />
			<div>
				<span class="text-xl font-bold">CertVal</span>
				<span class="block text-xs text-base-content/60">Certificate Monitor</span>
			</div>
		</a>

		<nav class="flex-1 p-2">
			<ul class="space-y-1">
				{#each menuItems as item}
					{@const active = isActive(item.href)}
					<li>
						<a
							href={item.href}
							class="btn w-full justify-start text-base font-medium
								   {active ? 'btn-primary' : 'btn-ghost'}"
						>
							<Icon name={item.icon} />
							{t(item.label, $language)}
						</a>
					</li>
				{/each}
			</ul>
		</nav>

		{#if user}
			<div class="border-t border-base-content/10 p-2">
				<div class="dropdown dropdown-top w-full">
					<div tabindex="0" role="button" class="btn h-auto w-full justify-start p-2 btn-ghost">
						<UserAvatar
							firstName={user.firstName}
							lastName={user.lastName}
							size="w-10"
							textSize="text-1xl"
						/>
						<div class="text-left">
							<div class="text-sm font-semibold">{user.fullName}</div>
							<div class="text-xs text-base-content/60">{user.email}</div>
						</div>
					</div>
					<ul
						class="dropdown-content menu w-full rounded-box
border border-base-content/10 bg-base-100 p-2 shadow-lg"
					>
						<li>
							<a href="/profile">
								<Icon name="profile" class="h-4 w-4" />
								{t('nav.profile', $language)}
							</a>
						</li>
						<li>
							<a href="/profile/settings">
								<Icon name="settings" class="h-4 w-4" />
								{t('nav.settings', $language)}
							</a>
						</li>
						<div class="divider my-1"></div>
						<li>
							<button onclick={handleLogout} class="text-error">
								<Icon name="logout" class="h-4 w-4" />
								{t('nav.logout', $language)}
							</button>
						</li>
					</ul>
				</div>
			</div>
		{/if}
	</div>
</aside>
