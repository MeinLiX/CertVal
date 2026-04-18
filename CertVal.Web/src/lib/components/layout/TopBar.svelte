<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { language } from '$lib/stores/language.svelte';
	import { theme } from '$lib/stores/theme.svelte';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { AuthService } from '$lib/services/AuthService';
	import { t } from '$lib/i18n';
	import logoUrl from '$lib/assets/favicon.svg?url';
	import Icon from '$lib/components/ui/Icon.svelte';
	import UserAvatar from '$lib/components/ui/UserAvatar.svelte';
	import type { IconName } from '$lib/icons';
	import type { Language } from '$lib/types';

	interface NavItem {
		labelKey: string;
		href: string;
		icon: IconName;
	}

	const navItems: NavItem[] = [
		{ labelKey: 'nav.dashboard', href: '/dashboard', icon: 'menu' },
		{ labelKey: 'nav.workspaces', href: '/workspaces', icon: 'workspaces' },
		{ labelKey: 'nav.certificates', href: '/certificates', icon: 'certificates' },
		{ labelKey: 'nav.notifications', href: '/notifications', icon: 'notifications' }
	];

	const currentPath = $derived(page.url.pathname);
	const user = $derived(userSession.user);
	const isAuthenticated = $derived(userSession.isAuthenticated);

	let userMenuOpen = $state(false);
	let mobileNavOpen = $state(false);

	function isActive(href: string): boolean {
		if (href === '/dashboard') return currentPath === '/dashboard' || currentPath === '/';
		return currentPath.startsWith(href);
	}

	function navigate(href: string) {
		goto(href);
		mobileNavOpen = false;
		userMenuOpen = false;
	}

	function toggleTheme() {
		theme.set(theme.current === 'dark' ? 'light' : 'dark');
	}

	function toggleLanguage() {
		const next: Language = language.current === 'uk' ? 'en' : 'uk';
		language.set(next);
	}

	function handleLogout() {
		AuthService.logout();
		goto('/auth/login');
	}

	function closeOnOutside(node: HTMLElement, onOutside: () => void) {
		function handle(event: MouseEvent) {
			if (!node.contains(event.target as Node)) onOutside();
		}
		document.addEventListener('click', handle, true);
		return {
			destroy() {
				document.removeEventListener('click', handle, true);
			}
		};
	}
</script>

<header class="topbar" data-test-id="topbar">
	<div class="topbar__inner">
		<!-- Logo -->
		<button class="topbar__brand" onclick={() => navigate(isAuthenticated ? '/dashboard' : '/')} data-test-id="topbar-logo">
			<img src={logoUrl} alt="" class="topbar__brand-mark" />
			<span class="topbar__brand-name">CertVal</span>
		</button>

		<!-- Primary nav (desktop, only when authenticated) -->
		{#if isAuthenticated}
			<nav class="topbar__nav" aria-label="Primary">
				{#each navItems as item}
					{@const active = isActive(item.href)}
					<button
						class="topbar__nav-link"
						class:topbar__nav-link--active={active}
						onclick={() => navigate(item.href)}
						data-test-id={`topbar-nav-${item.href.replace('/', '') || 'home'}`}
					>
						<span>{t(item.labelKey, language.current)}</span>
					</button>
				{/each}
			</nav>
		{/if}

		<!-- Right cluster -->
		<div class="topbar__actions">
			<button
				type="button"
				class="topbar__icon-btn"
				onclick={toggleLanguage}
				aria-label="Toggle language"
				data-test-id="language-switcher"
			>
				<span class="topbar__lang">{language.current === 'uk' ? 'UK' : 'EN'}</span>
			</button>

			<button
				type="button"
				class="topbar__icon-btn"
				onclick={toggleTheme}
				aria-label="Toggle theme"
				data-test-id="theme-switcher"
			>
				<Icon name={theme.current === 'dark' ? 'sun' : 'moon'} size="md" />
			</button>

			{#if isAuthenticated && user}
				<div class="topbar__user" use:closeOnOutside={() => (userMenuOpen = false)}>
					<button
						type="button"
						class="topbar__user-btn"
						onclick={() => (userMenuOpen = !userMenuOpen)}
						aria-haspopup="menu"
						aria-expanded={userMenuOpen}
						data-test-id="topbar-user-menu-toggle"
					>
						<UserAvatar firstName={user.firstName} lastName={user.lastName} size="sm" />
					</button>

					{#if userMenuOpen}
						<div class="topbar__user-menu animate-slideDown" role="menu">
							<div class="topbar__user-head">
								<span class="topbar__user-name">{user.firstName} {user.lastName}</span>
								<span class="topbar__user-email">{user.email}</span>
							</div>
							<button class="topbar__user-item" onclick={() => navigate('/profile')} role="menuitem" data-test-id="topbar-profile-link">
								<Icon name="profile" size="sm" />
								<span>{t('nav.profile', language.current)}</span>
							</button>
							<button class="topbar__user-item" onclick={() => navigate('/profile/settings')} role="menuitem" data-test-id="topbar-settings-link">
								<Icon name="settings" size="sm" />
								<span>{t('nav.settings', language.current)}</span>
							</button>
							<div class="topbar__user-divider"></div>
							<button class="topbar__user-item topbar__user-item--danger" onclick={handleLogout} role="menuitem" data-test-id="topbar-logout-button">
								<Icon name="logout" size="sm" />
								<span>{t('nav.logout', language.current)}</span>
							</button>
						</div>
					{/if}
				</div>

				<button
					type="button"
					class="topbar__icon-btn topbar__icon-btn--mobile"
					onclick={() => (mobileNavOpen = !mobileNavOpen)}
					aria-label="Open navigation"
					aria-expanded={mobileNavOpen}
					data-test-id="topbar-mobile-toggle"
				>
					<Icon name={mobileNavOpen ? 'close' : 'menu'} size="md" />
				</button>
			{/if}
		</div>
	</div>

	<!-- Mobile nav drawer -->
	{#if isAuthenticated && mobileNavOpen}
		<nav class="topbar__mobile-nav animate-slideDown" aria-label="Mobile">
			{#each navItems as item}
				{@const active = isActive(item.href)}
				<button
					class="topbar__mobile-link"
					class:topbar__mobile-link--active={active}
					onclick={() => navigate(item.href)}
				>
					<Icon name={item.icon} size="sm" />
					<span>{t(item.labelKey, language.current)}</span>
				</button>
			{/each}
		</nav>
	{/if}
</header>

<style>
	.topbar {
		position: sticky;
		top: 0;
		left: 0;
		right: 0;
		height: var(--topbar-height);
		background-color: color-mix(in srgb, var(--color-bg) 92%, transparent);
		backdrop-filter: saturate(140%) blur(8px);
		-webkit-backdrop-filter: saturate(140%) blur(8px);
		border-bottom: 1px solid var(--color-border);
		z-index: var(--z-sticky);
	}

	.topbar__inner {
		display: flex;
		align-items: center;
		gap: var(--space-8);
		height: 100%;
		max-width: var(--container-max);
		margin: 0 auto;
		padding: 0 var(--space-6);
	}

	.topbar__brand {
		display: inline-flex;
		align-items: center;
		gap: var(--space-3);
		background: none;
		border: 0;
		padding: 0;
		cursor: pointer;
		color: var(--color-text);
	}

	.topbar__brand-mark {
		width: 22px;
		height: 22px;
	}

	.topbar__brand-name {
		font-family: var(--font-display);
		font-size: 1.125rem;
		font-weight: 600;
		letter-spacing: var(--tracking-tight);
	}

	.topbar__nav {
		display: none;
		align-items: center;
		gap: var(--space-1);
		flex: 1;
	}

	@media (min-width: 900px) {
		.topbar__nav {
			display: flex;
		}
	}

	.topbar__nav-link {
		position: relative;
		background: none;
		border: 0;
		padding: var(--space-2) var(--space-4);
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text-secondary);
		cursor: pointer;
		border-radius: var(--radius-md);
		transition: color var(--transition-fast), background-color var(--transition-fast);
	}

	.topbar__nav-link:hover {
		color: var(--color-text);
		background-color: var(--color-surface-hover);
	}

	.topbar__nav-link--active {
		color: var(--color-primary);
		font-weight: var(--font-semibold);
		background-color: var(--color-primary-light);
	}

	.topbar__nav-link--active:hover {
		color: var(--color-primary);
		background-color: var(--color-primary-light);
	}

	.topbar__nav-link--active::after {
		content: '';
		position: absolute;
		left: 50%;
		bottom: -18px;
		width: 24px;
		height: 3px;
		transform: translateX(-50%);
		background-color: var(--color-primary);
		border-radius: var(--radius-full);
	}

	.topbar__actions {
		margin-left: auto;
		display: flex;
		align-items: center;
		gap: var(--space-2);
	}

	.topbar__icon-btn {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		width: 36px;
		height: 36px;
		background: none;
		border: 1px solid transparent;
		border-radius: var(--radius-md);
		color: var(--color-text-secondary);
		cursor: pointer;
		transition: all var(--transition-fast);
	}

	.topbar__icon-btn:hover {
		background-color: var(--color-surface-hover);
		color: var(--color-text);
	}

	.topbar__lang {
		font-size: var(--text-xs);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-wide);
	}

	.topbar__icon-btn--mobile {
		display: inline-flex;
	}

	@media (min-width: 900px) {
		.topbar__icon-btn--mobile {
			display: none;
		}
	}

	/* User menu */
	.topbar__user {
		position: relative;
	}

	.topbar__user-btn {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		background: none;
		border: 1px solid transparent;
		border-radius: var(--radius-full);
		padding: 2px;
		cursor: pointer;
		transition: border-color var(--transition-fast);
	}

	.topbar__user-btn:hover {
		border-color: var(--color-border);
	}

	.topbar__user-menu {
		position: absolute;
		right: 0;
		top: calc(100% + var(--space-2));
		min-width: 240px;
		background-color: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xl);
		box-shadow: var(--shadow-pop);
		padding: var(--space-1);
		z-index: var(--z-dropdown);
	}

	.topbar__user-head {
		padding: var(--space-3) var(--space-3) var(--space-2);
		border-bottom: 1px solid var(--color-border);
		margin-bottom: var(--space-1);
	}

	.topbar__user-name {
		display: block;
		font-size: var(--text-sm);
		font-weight: var(--font-semibold);
		color: var(--color-text);
	}

	.topbar__user-email {
		display: block;
		font-size: var(--text-xs);
		color: var(--color-text-muted);
		margin-top: 2px;
	}

	.topbar__user-item {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		width: 100%;
		padding: var(--space-2) var(--space-3);
		font-size: var(--text-sm);
		color: var(--color-text);
		background: none;
		border: 0;
		border-radius: var(--radius-md);
		cursor: pointer;
		text-align: left;
		transition: background-color var(--transition-fast);
	}

	.topbar__user-item:hover {
		background-color: var(--color-surface-hover);
	}

	.topbar__user-item--danger {
		color: var(--color-error);
	}

	.topbar__user-divider {
		height: 1px;
		background-color: var(--color-border);
		margin: var(--space-1) var(--space-1);
	}

	/* Mobile drawer */
	.topbar__mobile-nav {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
		padding: var(--space-3) var(--space-4);
		background-color: var(--color-bg);
		border-bottom: 1px solid var(--color-border);
	}

	@media (min-width: 900px) {
		.topbar__mobile-nav {
			display: none;
		}
	}

	.topbar__mobile-link {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		padding: var(--space-3);
		background: none;
		border: 0;
		border-radius: var(--radius-md);
		font-size: var(--text-sm);
		font-weight: var(--font-medium);
		color: var(--color-text-secondary);
		cursor: pointer;
		text-align: left;
	}

	.topbar__mobile-link:hover {
		background-color: var(--color-surface-hover);
		color: var(--color-text);
	}

	.topbar__mobile-link--active {
		color: var(--color-text);
		background-color: var(--color-surface-hover);
	}
</style>
