<script lang="ts">
	import { goto } from '$app/navigation';
	import { tick } from 'svelte';
	import { language } from '$lib/stores/language.svelte';
	import { commandPalette } from '$lib/stores/commandPalette.svelte';
	import { t } from '$lib/i18n';
	import { api } from '$lib/utils/api';
	import Icon from '$lib/components/ui/Icon.svelte';
	import type { IconName } from '$lib/icons';
	import type { Certificate, PagedResult } from '$lib/types';
	import { utilsBlocks } from '$lib/utils/utilsNav';

	interface Command {
		id: string;
		label: string;
		icon: IconName;
		href: string;
		group: string;
	}

	const lang = $derived(language.current);

	let query = $state('');
	let activeIndex = $state(0);
	let inputEl = $state<HTMLInputElement | null>(null);
	let certResults = $state<Certificate[]>([]);
	let searching = $state(false);
	let searchToken = 0;

	const staticCommands = $derived<Command[]>([
		{ id: 'nav-dashboard', label: t('nav.dashboard', lang), icon: 'menu', href: '/dashboard', group: t('commandPalette.navigation', lang) },
		{ id: 'nav-workspaces', label: t('nav.workspaces', lang), icon: 'workspaces', href: '/workspaces', group: t('commandPalette.navigation', lang) },
		{ id: 'nav-certificates', label: t('nav.certificates', lang), icon: 'certificates', href: '/certificates', group: t('commandPalette.navigation', lang) },
		{ id: 'nav-notifications', label: t('nav.notifications', lang), icon: 'notifications', href: '/notifications', group: t('commandPalette.navigation', lang) },
		{ id: 'nav-profile', label: t('nav.profile', lang), icon: 'profile', href: '/profile', group: t('commandPalette.navigation', lang) },
		{ id: 'nav-utils', label: t('nav.utils', lang), icon: 'settings', href: '/utils', group: t('commandPalette.navigation', lang) },
		...utilsBlocks.flatMap((block) =>
			block.tools.map((tool) => ({
				id: `tool-${tool.key}`,
				label: t(tool.labelKey, lang),
				icon: tool.icon,
				href: tool.href,
				group: t('commandPalette.tools', lang)
			}))
		)
	]);

	const filteredStatic = $derived(
		query.trim().length === 0
			? staticCommands
			: staticCommands.filter((c) => c.label.toLowerCase().includes(query.trim().toLowerCase()))
	);

	const certCommands = $derived<Command[]>(
		certResults.map((c) => ({
			id: `cert-${c.id}`,
			label: c.subject,
			icon: 'certificates' as IconName,
			href: `/certificates/${c.id}`,
			group: t('commandPalette.certificates', lang)
		}))
	);

	const allCommands = $derived([...filteredStatic, ...certCommands]);

	$effect(() => {
		if (commandPalette.open) {
			void focusInput();
		} else {
			query = '';
			certResults = [];
			activeIndex = 0;
		}
	});

	// Reset selection whenever the visible result set changes.
	$effect(() => {
		void allCommands.length;
		if (activeIndex >= allCommands.length) activeIndex = 0;
	});

	$effect(() => {
		const q = query.trim();
		if (q.length < 2) {
			certResults = [];
			searching = false;
			return;
		}
		const token = ++searchToken;
		searching = true;
		const handle = setTimeout(async () => {
			const res = await api.get<PagedResult<Certificate>>(
				`/certificates?subject=${encodeURIComponent(q)}&pageSize=6`
			);
			if (token !== searchToken) return;
			certResults = res.data?.items ?? [];
			searching = false;
		}, 250);
		return () => clearTimeout(handle);
	});

	async function focusInput() {
		await tick();
		inputEl?.focus();
	}

	function run(cmd: Command | undefined) {
		if (!cmd) return;
		commandPalette.hide();
		goto(cmd.href);
	}

	function onKeydown(e: KeyboardEvent) {
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			activeIndex = Math.min(activeIndex + 1, allCommands.length - 1);
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			activeIndex = Math.max(activeIndex - 1, 0);
		} else if (e.key === 'Enter') {
			e.preventDefault();
			run(allCommands[activeIndex]);
		} else if (e.key === 'Escape') {
			e.preventDefault();
			commandPalette.hide();
		}
	}

	// Group consecutive commands for rendering with headers.
	const groups = $derived(
		allCommands.reduce<Array<{ name: string; items: Command[] }>>((acc, cmd) => {
			const last = acc[acc.length - 1];
			if (last && last.name === cmd.group) last.items.push(cmd);
			else acc.push({ name: cmd.group, items: [cmd] });
			return acc;
		}, [])
	);

	function indexOf(cmd: Command): number {
		return allCommands.findIndex((c) => c.id === cmd.id);
	}
</script>

{#if commandPalette.open}
	<div
		class="palette-overlay"
		role="button"
		tabindex="-1"
		aria-label="Close"
		onclick={() => commandPalette.hide()}
		onkeydown={(e) => e.key === 'Escape' && commandPalette.hide()}
	>
		<!-- svelte-ignore a11y_click_events_have_key_events, a11y_no_static_element_interactions -->
		<div class="palette" role="dialog" aria-modal="true" tabindex="-1" onclick={(e) => e.stopPropagation()}>
			<div class="palette__search">
				<Icon name="search" size="sm" />
				<input
					bind:this={inputEl}
					bind:value={query}
					onkeydown={onKeydown}
					placeholder={t('commandPalette.placeholder', lang)}
					spellcheck="false"
					autocomplete="off"
				/>
			</div>

			<div class="palette__results">
				{#if searching}
					<div class="palette__status">{t('commandPalette.searching', lang)}</div>
				{/if}
				{#if allCommands.length === 0 && !searching}
					<div class="palette__status">{t('commandPalette.empty', lang)}</div>
				{/if}
				{#each groups as group (group.name)}
					<div class="palette__group">{group.name}</div>
					{#each group.items as cmd (cmd.id)}
						<button
							type="button"
							class="palette__item"
							class:palette__item--active={indexOf(cmd) === activeIndex}
							onmousemove={() => (activeIndex = indexOf(cmd))}
							onclick={() => run(cmd)}
						>
							<Icon name={cmd.icon} size="sm" />
							<span class="palette__label">{cmd.label}</span>
						</button>
					{/each}
				{/each}
			</div>

			<div class="palette__footer">
				<kbd>↑</kbd><kbd>↓</kbd>
				<span>{t('commandPalette.hint', lang)}</span>
				<kbd>↵</kbd>
				<kbd>esc</kbd>
			</div>
		</div>
	</div>
{/if}

<style>
	.palette-overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.4);
		display: flex;
		align-items: flex-start;
		justify-content: center;
		padding-top: 12vh;
		z-index: var(--z-modal, 200);
	}
	.palette {
		width: min(640px, 92vw);
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		box-shadow: var(--shadow-xl);
		overflow: hidden;
		display: flex;
		flex-direction: column;
		max-height: 70vh;
	}
	.palette__search {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-3) var(--space-4);
		border-bottom: 1px solid var(--color-border);
		color: var(--color-text-muted);
	}
	.palette__search input {
		flex: 1;
		border: 0;
		background: transparent;
		color: var(--color-text);
		font-size: var(--text-md);
		outline: none;
	}
	.palette__results {
		overflow-y: auto;
		padding: var(--space-2);
	}
	.palette__group {
		padding: var(--space-2) var(--space-2) var(--space-1);
		font-size: var(--text-xs);
		font-weight: var(--font-semibold);
		text-transform: uppercase;
		letter-spacing: 0.05em;
		color: var(--color-text-muted);
	}
	.palette__item {
		width: 100%;
		display: flex;
		align-items: center;
		gap: var(--space-3);
		padding: var(--space-2) var(--space-3);
		border: 0;
		border-radius: var(--radius-md);
		background: transparent;
		color: var(--color-text);
		cursor: pointer;
		text-align: left;
		font-size: var(--text-sm);
	}
	.palette__item--active {
		background: var(--color-primary-light);
		color: var(--color-primary);
	}
	.palette__label {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.palette__status {
		padding: var(--space-4);
		text-align: center;
		color: var(--color-text-muted);
		font-size: var(--text-sm);
	}
	.palette__footer {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		padding: var(--space-2) var(--space-4);
		border-top: 1px solid var(--color-border);
		color: var(--color-text-muted);
		font-size: var(--text-xs);
	}
	.palette__footer kbd {
		font-family: var(--font-mono);
		background: var(--color-surface-inset);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-xs);
		padding: 0 0.35rem;
		font-size: 0.7rem;
	}
</style>
