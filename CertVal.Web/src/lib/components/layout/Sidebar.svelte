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

<aside
	class="fixed top-16 left-0 z-30 h-[calc(100vh-4rem)] w-64 border-r border-gray-200/50 bg-white/90 backdrop-blur-xl transition-all duration-300 supports-[backdrop-filter]:bg-white/90 dark:border-gray-800/50 dark:bg-gray-950/90 dark:supports-[backdrop-filter]:bg-gray-950/90"
>
	<!-- Decorative gradient overlay -->
	<div
		class="pointer-events-none absolute inset-0 bg-gradient-to-b from-blue-50/20 via-transparent to-indigo-50/20 dark:from-blue-950/20 dark:via-transparent dark:to-indigo-950/20"
	></div>

	<nav class="relative flex h-full flex-col">
		<div class="py-1"> </div>

		<!-- Main Navigation -->
		<div class="flex-1 overflow-y-auto px-3 pb-6">
			<ul class="space-y-2">
				{#each menuItems as item, index}
					{@const active = isActive(item.href)}
					<li
						class="{mounted ? 'animate-in slide-in-from-left-8' : ''} transition-all duration-300"
						style="animation-delay: {mounted ? index * 100 : 0}ms"
					>
						<a
							href={item.href}
							class="group relative flex items-center rounded-xl px-4 py-3 text-sm font-medium transition-all duration-300 hover:scale-[1.02] {active
								? 'bg-gradient-to-r from-blue-50 to-indigo-50 text-blue-700 shadow-sm ring-1 ring-blue-200/50 dark:from-blue-950/30 dark:to-indigo-950/30 dark:text-blue-300 dark:ring-blue-800/30'
								: 'text-gray-600 hover:bg-gray-50/80 hover:text-gray-900 dark:text-gray-400 dark:hover:bg-gray-800/50 dark:hover:text-white'}"
							onmouseenter={() => (hoveredItem = item.href)}
							onmouseleave={() => (hoveredItem = null)}
						>
							<!-- Active indicator line -->
							{#if active}
								<div
									class="absolute top-1/2 left-0 h-8 w-1 -translate-y-1/2 rounded-r-full bg-gradient-to-b from-blue-500 to-indigo-600 shadow-sm transition-all duration-300"
								></div>
							{/if}

							<!-- Icon container -->
							<div class="relative mr-4 flex h-6 w-6 items-center justify-center">
								<svg
									class="h-5 w-5 transition-all duration-300 {active
										? 'text-blue-600 dark:text-blue-400'
										: 'text-gray-400 group-hover:text-gray-600 dark:group-hover:text-gray-300'} {hoveredItem ===
									item.href
										? 'scale-110'
										: ''}"
									fill="none"
									viewBox="0 0 24 24"
									stroke="currentColor"
									stroke-width="2"
								>
									<path stroke-linecap="round" stroke-linejoin="round" d={item.icon} />
								</svg>

								<!-- Glow effect for active items -->
								{#if active}
									<div
										class="absolute inset-0 scale-150 rounded-full bg-blue-400/20 blur-md transition-opacity duration-300"
									></div>
								{/if}
							</div>

							<!-- Label and description -->
							<div class="min-w-0 flex-1">
								<span
									class="block font-medium transition-all duration-300 {active
										? 'text-blue-700 dark:text-blue-300'
										: ''} truncate"
								>
									{t(item.label, $language)}
								</span>
								{#if item.description && (hoveredItem === item.href || active)}
									<span
										class="mt-0.5 block truncate text-xs text-gray-500 dark:text-gray-400 {mounted
											? 'animate-in slide-in-from-top-2 duration-200'
											: ''}"
									>
										{item.description}
									</span>
								{/if}
							</div>

							<!-- Hover effect background -->
							<div
								class="absolute inset-0 -z-10 rounded-xl bg-gradient-to-r from-transparent to-transparent transition-all duration-300 group-hover:from-gray-50/30 group-hover:to-gray-100/30 dark:group-hover:from-gray-800/20 dark:group-hover:to-gray-700/20 {hoveredItem ===
								item.href
									? 'opacity-100'
									: 'opacity-0'}"
							></div>
						</a>
					</li>
				{/each}
			</ul>
		</div>

		<!-- User Section -->
		{#if user}
			<div class="border-t border-gray-200/50 px-3 py-4 dark:border-gray-800/50">
				<div
					class="flex items-center space-x-3 rounded-xl bg-gradient-to-r from-gray-50/50 to-gray-100/50 p-3 backdrop-blur-sm dark:from-gray-800/30 dark:to-gray-700/30"
				>
					<div class="flex-shrink-0">
						<div
							class="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-blue-500 to-purple-600 text-white shadow-sm ring-2 ring-white/50 dark:ring-gray-900/50"
						>
							<span class="text-sm font-bold">
								{user.firstName?.charAt(0)}{user.lastName?.charAt(0)}
							</span>
						</div>
					</div>
					<div class="min-w-0 flex-1">
						<p class="truncate text-sm font-medium text-gray-900 dark:text-white">
							{user.fullName}
						</p>
						<p class="truncate text-xs text-gray-500 dark:text-gray-400">
							{user.email}
						</p>
					</div>
					<div class="flex-shrink-0">
						<a
							href="/profile"
							aria-label="Profile"
							class="flex h-8 w-8 items-center justify-center rounded-lg text-gray-400 transition-all duration-200 hover:bg-white/50 hover:text-gray-600 dark:hover:bg-gray-800/50 dark:hover:text-gray-300"
						>
							<svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
								/>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
								/>
							</svg>
						</a>
					</div>
					<div class="flex-shrink-0">
						<button
							aria-label="Logout"
							class="flex w-full items-center px-4 py-2.5 text-left text-sm text-gray-700 transition-colors duration-150 hover:bg-gray-50 focus:bg-gray-50 focus:outline-none dark:text-gray-200 dark:hover:bg-gray-800 dark:focus:bg-gray-800"
							onclick={(e) => {
								e.stopPropagation();
								handleLogout();
							}}
						>
							<svg class="mr-3 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
								/>
							</svg>
						</button>
					</div>
				</div>
			</div>
			<div class="py-1"> </div>
		{/if}
	</nav>

	<!-- Resize handle (optional) -->
	<div
		class="absolute top-0 right-0 h-full w-1 cursor-col-resize bg-transparent opacity-0 transition-colors duration-200 hover:bg-blue-200 hover:opacity-100 dark:hover:bg-blue-800"
	></div>
</aside>
