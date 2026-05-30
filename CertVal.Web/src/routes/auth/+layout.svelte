<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { userSession } from '$lib/stores/userSession.svelte';
	import { language } from '$lib/stores/language.svelte';
	import { theme } from '$lib/stores/theme.svelte';
	import type { Language, Theme } from '$lib/types';
	import logoUrl from '$lib/assets/favicon.svg?url';
	import Icon from '$lib/components/ui/Icon.svelte';
	import { REPO_URL, COMMIT_URL, COMMIT_HASH, HAS_COMMIT } from '$lib/config/appInfo';

	let { children } = $props();

	$effect(() => {
		if (userSession.isAuthenticated && !page.url.pathname.includes('/auth/confirm-email')) {
			goto('/dashboard');
		}
	});

	function toggleLanguage() {
		const next: Language = language.current === 'uk' ? 'en' : 'uk';
		language.set(next);
	}

	function toggleTheme() {
		const next: Theme = theme.current === 'dark' ? 'light' : 'dark';
		theme.set(next);
	}
</script>

<div class="auth">
	<aside class="auth__aside" aria-hidden="true">
		<a class="auth__brand" href="/">
			<img src={logoUrl} alt="" class="auth__brand-mark" />
			<span class="auth__brand-name">CertVal</span>
		</a>

		<blockquote class="auth__quote">
			<p>
				&ldquo;Quiet infrastructure for certificates. One dashboard, zero surprises at
				3&nbsp;a.m.&rdquo;
			</p>
			<footer>
				<span class="auth__quote-author">Systems team notes</span>
			</footer>
		</blockquote>

		<ul class="auth__points">
			<li>
				<span class="auth__points-num">01</span>
				<span>Discovery and tracking of every certificate you depend on.</span>
			</li>
			<li>
				<span class="auth__points-num">02</span>
				<span>Quiet alerts before expiry &mdash; email, webhook, or both.</span>
			</li>
			<li>
				<span class="auth__points-num">03</span>
				<span>Shared workspaces with role-based access for the team.</span>
			</li>
		</ul>
	</aside>

	<section class="auth__main">
		<header class="auth__topbar">
			<a class="auth__brand auth__brand--compact" href="/">
				<img src={logoUrl} alt="" class="auth__brand-mark" />
				<span class="auth__brand-name">CertVal</span>
			</a>
			<div class="auth__toolbar">
				<button type="button" class="auth__tool" onclick={toggleLanguage} aria-label="Toggle language">
					<span class="auth__tool-text">{language.current === 'uk' ? 'UK' : 'EN'}</span>
				</button>
				<button type="button" class="auth__tool" onclick={toggleTheme} aria-label="Toggle theme">
					<Icon name={theme.current === 'dark' ? 'sun' : 'moon'} size="md" />
				</button>
			</div>
		</header>

		<div class="auth__content">
			{@render children()}
		</div>

		<footer class="auth__footer">
			<span>&copy; {new Date().getFullYear()} CertVal</span>
			<nav class="auth__footer-nav">
				<a href="/">Home</a>
				<span aria-hidden="true">&middot;</span>
				<a href={REPO_URL} target="_blank" rel="noopener">GitHub</a>
				<span aria-hidden="true">&middot;</span>
				<a
					class="auth__build"
					href={COMMIT_URL}
					target="_blank"
					rel="noopener"
					title={HAS_COMMIT ? `Build ${COMMIT_HASH}` : 'Local development build'}
				>
					<Icon name="hash" size="sm" />
					<span class="auth__build-hash">{COMMIT_HASH}</span>
				</a>
			</nav>
		</footer>
	</section>
</div>

<style>
	.auth {
		display: grid;
		grid-template-columns: 1fr;
		min-height: 100vh;
		background-color: var(--color-bg);
	}

	@media (min-width: 1024px) {
		.auth {
			grid-template-columns: minmax(420px, 0.9fr) 1.1fr;
		}
	}

	/* Aside (left editorial panel) */
	.auth__aside {
		display: none;
		flex-direction: column;
		justify-content: space-between;
		padding: var(--space-10) var(--space-10);
		background-color: var(--color-text);
		color: var(--color-bg);
		min-height: 100vh;
	}

	@media (min-width: 1024px) {
		.auth__aside {
			display: flex;
		}
	}

	.auth__brand {
		display: inline-flex;
		align-items: center;
		gap: var(--space-3);
		color: inherit;
		text-decoration: none;
	}

	.auth__brand-mark {
		width: 28px;
		height: 28px;
		filter: invert(1);
	}

	.auth__brand-name {
		font-family: var(--font-display);
		font-size: 1.25rem;
		font-weight: 600;
		letter-spacing: var(--tracking-tight);
	}

	.auth__quote {
		margin: var(--space-16) 0;
	}

	.auth__quote p {
		font-family: var(--font-display);
		font-size: 1.875rem;
		line-height: 1.25;
		font-weight: 400;
		font-style: italic;
		color: inherit;
		max-width: 24ch;
	}

	.auth__quote footer {
		margin-top: var(--space-6);
	}

	.auth__quote-author {
		font-size: var(--text-xs);
		font-weight: var(--font-medium);
		letter-spacing: var(--tracking-wide);
		text-transform: uppercase;
		opacity: 0.6;
	}

	.auth__points {
		list-style: none;
		padding: 0;
		margin: 0;
		display: grid;
		gap: var(--space-4);
	}

	.auth__points li {
		display: grid;
		grid-template-columns: 44px 1fr;
		align-items: baseline;
		gap: var(--space-3);
		font-size: var(--text-sm);
		line-height: var(--leading-snug);
		color: inherit;
		opacity: 0.8;
	}

	.auth__points-num {
		font-family: var(--font-mono);
		font-size: var(--text-xs);
		letter-spacing: var(--tracking-wide);
		opacity: 0.5;
	}

	/* Main (right panel with form) */
	.auth__main {
		display: flex;
		flex-direction: column;
		padding: var(--space-8) var(--space-6);
		min-height: 100vh;
	}

	@media (min-width: 720px) {
		.auth__main {
			padding: var(--space-10);
		}
	}

	.auth__topbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--space-4);
	}

	.auth__brand--compact {
		color: var(--color-text);
	}

	.auth__brand--compact .auth__brand-mark {
		filter: none;
		width: 24px;
		height: 24px;
	}

	@media (min-width: 1024px) {
		.auth__brand--compact {
			visibility: hidden;
		}
	}

	.auth__toolbar {
		display: flex;
		align-items: center;
		gap: var(--space-1);
	}

	.auth__tool {
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
		transition: background-color var(--transition-fast), color var(--transition-fast);
	}

	.auth__tool:hover {
		background-color: var(--color-surface-hover);
		color: var(--color-text);
	}

	.auth__tool-text {
		font-size: var(--text-xs);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-wide);
	}

	.auth__content {
		flex: 1;
		display: flex;
		align-items: center;
		justify-content: center;
		padding: var(--space-8) 0;
	}

	.auth__footer {
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: var(--space-4);
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.auth__footer-nav {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
	}

	.auth__footer a {
		color: inherit;
		text-decoration: none;
	}

	.auth__footer a:hover {
		color: var(--color-text);
	}

	.auth__build {
		display: inline-flex;
		align-items: center;
		gap: var(--space-1);
		font-family: var(--font-mono, monospace);
		font-size: var(--text-xs);
		opacity: 0.85;
	}

	.auth__build:hover {
		opacity: 1;
	}

	.auth__build-hash {
		letter-spacing: 0.02em;
	}
</style>
