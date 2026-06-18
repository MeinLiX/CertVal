<script lang="ts">
import '../app.css';
import { page } from '$app/state';
import { language } from '$lib/stores/language.svelte';
import { theme } from '$lib/stores/theme.svelte';
import { appState } from '$lib/stores/appState.svelte';
import { commandPalette } from '$lib/stores/commandPalette.svelte';
import TopBar from '$lib/components/layout/TopBar.svelte';
import CommandPalette from '$lib/components/layout/CommandPalette.svelte';
import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
import { onMount } from 'svelte';

let { children, data } = $props();
let isAppLoading = $state(true);

$effect(() => {
if (data.language) language.set(data.language);
if (data.theme) theme.set(data.theme);
});

onMount(() => {
const timer = setTimeout(() => {
isAppLoading = false;
appState.setLoading(false);
}, 600);

function onKeydown(e: KeyboardEvent) {
if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
e.preventDefault();
commandPalette.toggle();
}
}
window.addEventListener('keydown', onKeydown);

return () => {
clearTimeout(timer);
window.removeEventListener('keydown', onKeydown);
};
});

const isAuthPage = $derived(page.url.pathname.startsWith('/auth'));
</script>

<div class="app-root" data-test-id="app-root">
{#if isAppLoading}
<GlobalLoader variant="fullscreen" data-test-id="global-loader" />
{/if}

{#if isAuthPage}
<main class="app-main app-main--auth" data-test-id="auth-layout-main">
{@render children?.()}
</main>
{:else}
<TopBar />
<CommandPalette />
<main class="app-main app-main--content" data-test-id="app-layout-main">
<div class="app-content animate-fadeIn">
{@render children?.()}
</div>
</main>
{/if}
</div>

<style>
.app-root {
min-height: 100vh;
background-color: var(--color-bg);
color: var(--color-text);
font-family: var(--font-family);
}

.app-main {
width: 100%;
}

.app-main--auth {
position: relative;
overflow: hidden;
min-height: 100vh;
}

.app-main--content {
min-height: calc(100vh - var(--topbar-height));
padding: var(--space-5) var(--space-6) calc(var(--space-12) + 96px);
}

@media (max-width: 560px) {
.app-main--content {
padding: var(--space-4) var(--space-4) calc(var(--space-10) + 104px);
}
}

@media (min-width: 900px) {
.app-main--content {
padding: var(--space-6) var(--space-10) calc(var(--space-16) + 96px);
}
}

.app-content {
max-width: var(--container-max);
margin: 0 auto;
}
</style>