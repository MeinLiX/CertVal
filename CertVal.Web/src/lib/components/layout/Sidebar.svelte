<script lang="ts">
	import { page } from '$app/stores';
	import { language } from '$lib/stores/language';
	import { t } from '$lib/i18n';

	const menuItems = [
		{
			label: 'nav.dashboard',
			href: '/',
			icon: 'M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z'
		},
		{
			label: 'nav.workspaces',
			href: '/workspaces',
			icon: 'M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6'
		},
		{
			label: 'nav.certificates',
			href: '/certificates',
			icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z'
		},
		{
			label: 'nav.notifications',
			href: '/notifications',
			icon: 'M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9'
		}
	];

	$: currentPath = $page.url.pathname;
</script>

<aside class="fixed top-16 left-0 z-40 h-full w-64 border-r border-gray-200 bg-white shadow-sm">
	<nav class="mt-5 px-2">
		<ul class="space-y-1">
			{#each menuItems as item}
				<li>
					<a
						href={item.href}
						class="group flex items-center rounded-md px-2 py-2 text-sm font-medium transition-colors
						{currentPath === item.href
							? 'border-r-2 border-blue-700 bg-blue-50 text-blue-700'
							: 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'}"
					>
						{item.label}
					</a>
				</li>
			{/each}
		</ul>
	</nav>
</aside>
