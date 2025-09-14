<script lang="ts">
	import { page } from '$app/stores';
	import { language } from '$lib/stores/language';
	import { auth } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import { goto } from '$app/navigation';
	import logoUrl from '$lib/assets/favicon.svg?url';

	const menuItems = [
		{ label: 'nav.dashboard', href: '/', icon: 'M4 6h16M4 12h16M4 18h7' },
		{
			label: 'nav.workspaces',
			href: '/workspaces',
			icon: 'M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2z'
		},
		{
			label: 'nav.certificates',
			href: '/certificates',
			icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622'
		},
		{
			label: 'nav.notifications',
			href: '/notifications',
			icon: 'M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9'
		}
	];

	const currentPath = $derived($page.url.pathname);
	const user = $derived($auth.user);

	function isActive(href: string): boolean {
		if (href === '/') return currentPath === '/';
		return currentPath.startsWith(href);
	}

	function handleLogout() {
		auth.logout();
		goto('/auth/login');
	}
</script>

<aside class="drawer-side z-40 h-screen">
	<label for="drawer-toggle" class="drawer-overlay lg:hidden"></label>
	<div
		class="flex min-h-full w-64 flex-col border-r border-base-content/10 bg-base-100/95 text-base-content backdrop-blur-lg"
	>
		<a href="/" class="flex items-center gap-3 border-b border-base-content/10 p-4">
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
							<svg
								class="h-5 w-5"
								fill="none"
								viewBox="0 0 24 24"
								stroke="currentColor"
								stroke-width="2"
							>
								<path stroke-linecap="round" stroke-linejoin="round" d={item.icon} />
							</svg>
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
						<div class="placeholder avatar">
							<div class="w-10 rounded-full bg-primary text-primary-content">
								<span class="text-sm font-semibold">
									{user.firstName?.charAt(0)}{user.lastName?.charAt(0)}
								</span>
							</div>
						</div>
						<div class="text-left">
							<div class="text-sm font-semibold">{user.fullName}</div>
							<div class="text-xs text-base-content/60">{user.email}</div>
						</div>
					</div>
					<ul
						class="dropdown-content menu w-full rounded-box border border-base-content/10 bg-base-100 p-2 shadow-lg"
					>
						<li>
							<a href="/profile">
								<svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"
									><path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
									></path></svg
								>
								{t('nav.profile', $language)}
							</a>
						</li>
						<div class="divider my-1"></div>
						<li>
							<button onclick={handleLogout} class="text-error">
								<svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"
									><path
										stroke-linecap="round"
										stroke-linejoin="round"
										stroke-width="2"
										d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
									></path></svg
								>
								{t('nav.logout', $language)}
							</button>
						</li>
					</ul>
				</div>
			</div>
		{/if}
	</div>
</aside>
