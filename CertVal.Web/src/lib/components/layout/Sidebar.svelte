<script lang="ts">
	import { page } from '$app/stores';
	import { language } from '$lib/stores/language';
	import { auth } from '$lib/stores/auth';
	import { t } from '$lib/i18n';
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';

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

<aside class="drawer-side h-[calc(100vh-4rem)] top-16">
	<label for="drawer-toggle" class="drawer-overlay"></label>
	<div class="menu bg-base-100/90 backdrop-blur-xl min-h-full w-64 border-r border-base-300 transition-all duration-300">
		<!-- Decorative gradient overlay -->
		<div class="pointer-events-none absolute inset-0 bg-gradient-to-b from-primary/5 via-transparent to-secondary/5"></div>

		<!-- Main Navigation -->
		<div class="relative flex h-full flex-col py-4">
			<ul class="menu-compact flex-1 space-y-1 px-2">
				{#each menuItems as item, index}
					{@const active = isActive(item.href)}
					<li
						class="{mounted ? 'animate-in slide-in-from-left-8' : ''} transition-all duration-300"
						style="animation-delay: {mounted ? index * 100 : 0}ms"
					>
						<a
							href={item.href}
							class="group relative flex items-center gap-4 rounded-xl p-3 transition-all duration-300 hover:scale-[1.02] {active
								? 'bg-primary/10 text-primary border border-primary/20 shadow-sm' 
								: 'hover:bg-base-200'}"
							onmouseenter={() => (hoveredItem = item.href)}
							onmouseleave={() => (hoveredItem = null)}
						>
							<!-- Active indicator -->
							{#if active}
								<div class="absolute -left-2 top-1/2 h-8 w-1 -translate-y-1/2 rounded-r-full bg-primary shadow-sm"></div>
							{/if}

							<!-- Icon container -->
							<div class="relative flex h-6 w-6 items-center justify-center">
								<svg
									class="h-5 w-5 transition-all duration-300 {active
										? 'text-primary'
										: 'text-base-content/60 group-hover:text-base-content'} {hoveredItem === item.href ? 'scale-110' : ''}"
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
								<span class="block font-medium transition-all duration-300 {active ? 'text-primary' : ''} truncate">
									{t(item.label, $language)}
								</span>
								{#if item.description && (hoveredItem === item.href || active)}
									<span class="mt-0.5 block truncate text-xs text-base-content/60 {mounted ? 'animate-in slide-in-from-top-2 duration-200' : ''}">
										{item.description}
									</span>
								{/if}
							</div>

							<!-- Badge if present -->
							{#if item.badge}
								<div class="badge badge-primary badge-sm">{item.badge}</div>
							{/if}
						</a>
					</li>
				{/each}
			</ul>
		</div>
	</div>
</aside>
											