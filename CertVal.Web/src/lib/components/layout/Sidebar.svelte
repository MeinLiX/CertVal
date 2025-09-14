<script lang="ts">
	import { page } from '$app/stores';
	import { language } from '$lib/stores/language';
	import { auth } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import logoUrl from '$lib/assets/favicon.svg?url';

	interface MenuItem {
		label: string;
		href: string;
		icon: string;
		badge?: number;
		description?: string;
	}

	let mounted = $state(false);
	let hoveredItem = $state<string | null>(null);

	const menuItems: MenuItem[] = [
		{
			label: 'nav.dashboard',
			href: '/',
			icon: 'M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a1 1 0 00-1-1H6a1 1 0 00-1-1V7a3 3 0 013-3h7a3 3 0 013 3v1',
			description: 'Overview and statistics'
		},
		{
			label: 'nav.workspaces',
			href: '/workspaces',
			icon: 'M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6',
			description: 'Manage your workspaces'
		},
		{
			label: 'nav.certificates',
			href: '/certificates',
			icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
			description: 'SSL/TLS certificate management'
		},
		{
			label: 'nav.notifications',
			href: '/notifications',
			icon: 'M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9',
			description: 'Alerts and notifications'
		}
	];

	const currentPath = $derived($page.url.pathname);
	const user = $derived($auth.user);

	function isActive(href: string): boolean {
		if (href === '/') {
			return currentPath === '/';
		}
		return currentPath.startsWith(href);
	}

	function handleLogout() {
		auth.logout();
		goto('/auth/login');
	}

	onMount(() => {
		mounted = true;
	});
</script>

<aside class="drawer-side h-screen">
	<label for="drawer-toggle" class="drawer-overlay lg:hidden"></label>
	<div class="min-h-full w-64 bg-base-100 flex flex-col border-r border-base-200">
		<!-- Logo Section -->
		<div class="flex items-center gap-3 p-6 border-b border-base-200">
			<div class="avatar">
				<div class="w-10 rounded-xl overflow-hidden">
					<img src="{logoUrl}" alt="CertVal logo" class="w-10 h-10 object-cover" />
				</div>
			</div>
			<div class="flex flex-col">
				<span class="font-bold text-xl text-base-content">CertVal</span>
				<span class="text-xs text-base-content/60">Certificate Monitor</span>
			</div>
		</div>

		<!-- Navigation -->
		<nav class="flex-1 p-4">
			<ul class="menu menu-vertical w-full gap-1">
				{#each menuItems as item, index}
					{@const active = isActive(item.href)}
					<li
						class="{mounted ? 'animate-in slide-in-from-left-8' : ''} transition-all duration-300"
						style="animation-delay: {mounted ? index * 100 : 0}ms"
					>
						<a
							href={item.href}
							class="group relative flex items-center gap-3 rounded-xl transition-all duration-300 hover:scale-[1.02] {active
								? 'bg-primary text-primary-content shadow-lg' 
								: 'hover:bg-base-200'}"
							onmouseenter={() => (hoveredItem = item.href)}
							onmouseleave={() => (hoveredItem = null)}
						>
							<!-- Icon container -->
							<div class="relative flex h-6 w-6 items-center justify-center">
								<svg
									class="h-5 w-5 transition-all duration-300 {hoveredItem === item.href ? 'scale-110' : ''}"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
									stroke-width="2"
								>
									<path stroke-linecap="round" stroke-linejoin="round" d={item.icon} />
								</svg>

								<!-- Glow effect for active items -->
								{#if active}
									<div class="absolute inset-0 scale-150 rounded-full bg-primary/20 blur-md transition-opacity duration-300"></div>
								{/if}
							</div>

							<!-- Label and description -->
							<div class="min-w-0 flex-1">
								<span class="block font-medium transition-all duration-300 truncate">
									{t(item.label, $language)}
								</span>
								{#if item.description && (hoveredItem === item.href || active)}
									<span class="mt-0.5 block truncate text-xs opacity-70 {mounted ? 'animate-in slide-in-from-top-2 duration-200' : ''}">
										{item.description}
									</span>
								{/if}
							</div>

							<!-- Badge if present -->
							{#if item.badge}
								<div class="badge badge-secondary badge-sm">{item.badge}</div>
							{/if}
						</a>
					</li>
				{/each}
			</ul>
		</nav>

		<!-- User Section -->
		{#if user}
			<div class="p-4 border-t border-base-200">
				<div class="flex items-center gap-3 p-3 rounded-xl bg-base-200/50">
					<div class="avatar placeholder">
						<div class="bg-primary text-primary-content rounded-full w-10">
							<span class="text-sm font-semibold">
								{user.firstName?.charAt(0)}{user.lastName?.charAt(0)}
							</span>
						</div>
					</div>
					<div class="min-w-0 flex-1">
						<div class="font-medium text-sm truncate">{user.fullName}</div>
						<div class="text-xs text-base-content/60 truncate">{user.email}</div>
					</div>
					<div class="dropdown dropdown-top dropdown-end">
						<div tabindex="0" role="button" class="btn btn-ghost btn-xs btn-circle">
							<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v.01M12 12v.01M12 19v.01M12 6a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2zm0 7a1 1 0 110-2 1 1 0 010 2z"></path>
							</svg>
						</div>
						<ul class="dropdown-content menu bg-base-100 rounded-box z-[1] w-52 p-2 shadow-lg border border-base-200">
							<li>
								<a href="/profile" class="flex items-center gap-3">
									<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
									</svg>
									{t('nav.profile', $language)}
								</a>
							</li>
							<div class="divider my-0"></div>
							<li>
								<button onclick={handleLogout} class="flex items-center gap-3 text-error">
									<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
										<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
									</svg>
									{t('nav.logout', $language)}
								</button>
							</li>
						</ul>
					</div>
				</div>
			</div>
		{/if}
	</div>
</aside>